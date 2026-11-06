import { randomBytes, createHash } from 'node:crypto';
import {
  BadRequestException,
  Injectable,
  ServiceUnavailableException,
  UnauthorizedException,
} from '@nestjs/common';
import { createRemoteJWKSet, jwtVerify } from 'jose';
import { Database } from './database.js';
import { UserRow, UsersService } from './users.service.js';

type Provider = 'google' | 'github';

function randomToken(): string {
  return randomBytes(32).toString('base64url');
}

function digest(value: string): string {
  return createHash('sha256').update(value).digest('hex');
}

function providerConfig(provider: Provider) {
  const prefix = provider.toUpperCase();
  const clientId = process.env[`${prefix}_CLIENT_ID`];
  const clientSecret = process.env[`${prefix}_CLIENT_SECRET`];
  if (!clientId || !clientSecret) {
    throw new ServiceUnavailableException(`${provider} no está configurado.`);
  }
  return { clientId, clientSecret };
}

@Injectable()
export class OAuthService {
  private readonly googleKeys = createRemoteJWKSet(
    new URL('https://www.googleapis.com/oauth2/v3/certs'),
  );

  constructor(
    private readonly database: Database,
    private readonly users: UsersService,
  ) {}

  providers() {
    return {
      google: Boolean(process.env.GOOGLE_CLIENT_ID && process.env.GOOGLE_CLIENT_SECRET),
      github: Boolean(process.env.GITHUB_CLIENT_ID && process.env.GITHUB_CLIENT_SECRET),
    };
  }

  private callbackUrl(provider: Provider): string {
    const base = process.env.OAUTH_CALLBACK_BASE_URL;
    if (!base) throw new ServiceUnavailableException('Falta OAUTH_CALLBACK_BASE_URL.');
    return `${base.replace(/\/$/, '')}/api/auth/callback/${provider}`;
  }

  async start(provider: Provider): Promise<string> {
    const { clientId } = providerConfig(provider);
    const state = randomToken();
    const verifier = randomToken();
    const challenge = createHash('sha256').update(verifier).digest('base64url');
    const nonce = randomToken();
    await this.database.query('DELETE FROM oauth_requests WHERE expires_at < now()');
    await this.database.query(
      `INSERT INTO oauth_requests (state_hash, provider, code_verifier, nonce, expires_at)
       VALUES ($1, $2, $3, $4, now() + interval '10 minutes')`,
      [digest(state), provider, verifier, nonce],
    );
    const params = new URLSearchParams({
      client_id: clientId,
      redirect_uri: this.callbackUrl(provider),
      response_type: 'code',
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256',
    });
    if (provider === 'google') {
      params.set('scope', 'openid email profile');
      params.set('nonce', nonce);
      return `https://accounts.google.com/o/oauth2/v2/auth?${params}`;
    }
    params.delete('response_type');
    params.set('scope', 'user:email');
    return `https://github.com/login/oauth/authorize?${params}`;
  }

  async callback(provider: Provider, code: string, state: string): Promise<string> {
    if (!code || !state) throw new BadRequestException('Código o estado ausente.');
    const request = await this.database.query<{
      provider: string;
      code_verifier: string;
      nonce: string;
      expires_at: Date;
    }>(
      'DELETE FROM oauth_requests WHERE state_hash = $1 RETURNING *',
      [digest(state)],
    );
    const saved = request.rows[0];
    if (!saved || saved.provider !== provider || saved.expires_at <= new Date()) {
      throw new UnauthorizedException('La solicitud de acceso expiró o es inválida.');
    }
    const identity = provider === 'google'
      ? await this.googleIdentity(code, saved.code_verifier, saved.nonce)
      : await this.githubIdentity(code, saved.code_verifier);
    const user = await this.findOrCreateUser(provider, identity.subject, identity.email, identity.name);
    const oneTimeCode = randomToken();
    await this.database.query('DELETE FROM login_codes WHERE expires_at < now()');
    await this.database.query(
      `INSERT INTO login_codes (code_hash, user_id, provider, expires_at)
       VALUES ($1, $2, $3, now() + interval '90 seconds')`,
      [digest(oneTimeCode), user.id, provider],
    );
    const frontend = process.env.FRONTEND_URL ?? 'http://localhost:4200';
    return `${frontend.replace(/\/$/, '')}/oauth/callback?code=${encodeURIComponent(oneTimeCode)}`;
  }

  async exchange(code: string) {
    if (!code) throw new BadRequestException('Falta el código de acceso.');
    const result = await this.database.query<{ user_id: number; provider: string; expires_at: Date }>(
      'DELETE FROM login_codes WHERE code_hash = $1 RETURNING *',
      [digest(code)],
    );
    const saved = result.rows[0];
    if (!saved || saved.expires_at <= new Date()) {
      throw new UnauthorizedException('El código de acceso expiró o ya fue utilizado.');
    }
    const user = await this.users.findById(saved.user_id);
    if (!user?.is_active) throw new UnauthorizedException('La cuenta está inactiva.');
    return this.users.issueToken(user, saved.provider);
  }

  private async googleIdentity(code: string, verifier: string, nonce: string) {
    const { clientId, clientSecret } = providerConfig('google');
    const response = await fetch('https://oauth2.googleapis.com/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        code,
        client_id: clientId,
        client_secret: clientSecret,
        redirect_uri: this.callbackUrl('google'),
        grant_type: 'authorization_code',
        code_verifier: verifier,
      }),
    });
    if (!response.ok) throw new UnauthorizedException('Google rechazó el acceso.');
    const data = await response.json() as { id_token?: string };
    if (!data.id_token) throw new UnauthorizedException('Google no devolvió una identidad.');
    const verified = await jwtVerify(data.id_token, this.googleKeys, {
      issuer: ['https://accounts.google.com', 'accounts.google.com'],
      audience: clientId,
    });
    const claims = verified.payload;
    if (claims.nonce !== nonce || claims.email_verified !== true ||
        typeof claims.email !== 'string' || !claims.sub) {
      throw new UnauthorizedException('Google no confirmó el correo.');
    }
    return {
      subject: claims.sub,
      email: claims.email.toLowerCase(),
      name: typeof claims.name === 'string' ? claims.name : claims.email,
    };
  }

  private async githubIdentity(code: string, verifier: string) {
    const { clientId, clientSecret } = providerConfig('github');
    const response = await fetch('https://github.com/login/oauth/access_token', {
      method: 'POST',
      headers: { Accept: 'application/json', 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        client_id: clientId,
        client_secret: clientSecret,
        code,
        redirect_uri: this.callbackUrl('github'),
        code_verifier: verifier,
      }),
    });
    if (!response.ok) throw new UnauthorizedException('GitHub rechazó el acceso.');
    const data = await response.json() as { access_token?: string };
    if (!data.access_token) throw new UnauthorizedException('GitHub no devolvió un acceso.');
    const headers = {
      Authorization: `Bearer ${data.access_token}`,
      Accept: 'application/vnd.github+json',
      'User-Agent': 'SAT-Rural',
    };
    const [userResponse, emailsResponse] = await Promise.all([
      fetch('https://api.github.com/user', { headers }),
      fetch('https://api.github.com/user/emails', { headers }),
    ]);
    if (!userResponse.ok || !emailsResponse.ok) {
      throw new UnauthorizedException('No se pudo confirmar la identidad de GitHub.');
    }
    const profile = await userResponse.json() as { id: number; name?: string; login: string };
    const emails = await emailsResponse.json() as Array<{
      email: string; verified: boolean; primary: boolean;
    }>;
    const confirmed = emails.find(item => item.primary && item.verified) ??
      emails.find(item => item.verified);
    if (!confirmed) throw new UnauthorizedException('GitHub no confirmó un correo.');
    return {
      subject: String(profile.id),
      email: confirmed.email.toLowerCase(),
      name: profile.name || profile.login,
    };
  }

  private async findOrCreateUser(
    provider: Provider, subject: string, email: string, name: string,
  ): Promise<UserRow> {
    const linked = await this.database.query<UserRow>(
      `SELECT u.* FROM users u JOIN external_identities e ON e.user_id = u.id
       WHERE e.provider = $1 AND e.subject = $2`,
      [provider, subject],
    );
    if (linked.rows[0]) {
      if (!linked.rows[0].is_active) throw new UnauthorizedException('La cuenta está inactiva.');
      return linked.rows[0];
    }
    let user = await this.users.findByUsername(email);
    if (user && !user.is_active) throw new UnauthorizedException('La cuenta está inactiva.');
    if (!user) {
      await this.database.query(
        `INSERT INTO users (username, full_name, role_id) VALUES ($1, $2, 3)
         ON CONFLICT (username) DO NOTHING`,
        [email, name],
      );
      user = await this.users.findByUsername(email);
    }
    if (!user) throw new UnauthorizedException('No se pudo crear la cuenta.');
    await this.database.query(
      `INSERT INTO external_identities (provider, subject, user_id)
       VALUES ($1, $2, $3) ON CONFLICT (provider, subject) DO NOTHING`,
      [provider, subject, user.id],
    );
    return user;
  }
}
