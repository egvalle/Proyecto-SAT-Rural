import {
  CanActivate,
  ExecutionContext,
  ForbiddenException,
  Injectable,
  UnauthorizedException,
} from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { UsersService, UserRow } from './users.service.js';

export interface AuthenticatedRequest {
  headers: { authorization?: string };
  user: UserRow;
}

@Injectable()
export class JwtGuard implements CanActivate {
  constructor(
    private readonly jwt: JwtService,
    private readonly users: UsersService,
  ) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest<AuthenticatedRequest>();
    const authorization = request.headers.authorization ?? '';
    if (!authorization.startsWith('Bearer ')) {
      throw new UnauthorizedException();
    }

    try {
      const claims = await this.jwt.verifyAsync<{ sub: string }>(
        authorization.slice(7),
        {
          issuer: process.env.JWT_ISSUER,
          audience: process.env.JWT_AUDIENCE,
        },
      );
      const userId = Number(claims.sub);
      if (!Number.isInteger(userId)) throw new UnauthorizedException();
      const user = await this.users.findById(userId);
      if (!user?.is_active) throw new UnauthorizedException();
      request.user = user;
      return true;
    } catch {
      throw new UnauthorizedException();
    }
  }
}

@Injectable()
export class AdminGuard implements CanActivate {
  canActivate(context: ExecutionContext): boolean {
    const request = context.switchToHttp().getRequest<AuthenticatedRequest>();
    if (request.user?.role_id !== 1) throw new ForbiddenException();
    return true;
  }
}
