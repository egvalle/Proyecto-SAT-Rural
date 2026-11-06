import {
  BadRequestException,
  ConflictException,
  Injectable,
  NotFoundException,
  UnauthorizedException,
} from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { Database } from './database.js';
import { hashPassword, verifyPassword } from './password.js';

export interface UserRow {
  id: number;
  username: string;
  password_hash: string | null;
  full_name: string;
  role_id: number;
  is_active: boolean;
  created_at: Date;
}

export function roleName(roleId: number): string {
  return roleId === 1 ? 'ADMIN' : roleId === 2 ? 'OPERATOR' : 'VIEWER';
}

export function publicUser(user: UserRow) {
  return {
    id: user.id,
    username: user.username,
    fullName: user.full_name,
    roleId: user.role_id,
    roleDescription: roleName(user.role_id),
  };
}

export function adminUser(user: UserRow) {
  return {
    ...publicUser(user),
    isActive: user.is_active,
    createdAt: user.created_at,
  };
}

@Injectable()
export class UsersService {
  constructor(
    private readonly database: Database,
    private readonly jwt: JwtService,
  ) {}

  async findById(id: number): Promise<UserRow | null> {
    const result = await this.database.query<UserRow>(
      'SELECT * FROM users WHERE id = $1',
      [id],
    );
    return result.rows[0] ?? null;
  }

  async findByUsername(username: string): Promise<UserRow | null> {
    const result = await this.database.query<UserRow>(
      'SELECT * FROM users WHERE username = $1',
      [username.trim().toLowerCase()],
    );
    return result.rows[0] ?? null;
  }

  async issueToken(user: UserRow, provider: string) {
    const role = roleName(user.role_id);
    const token = await this.jwt.signAsync({
      sub: String(user.id),
      unique_name: user.username,
      email: user.username,
      name: user.full_name,
      role,
      roles: [role],
      provider,
    });
    return { token, user: publicUser(user) };
  }

  async login(username: string, password: string) {
    if (!username?.trim() || !password) {
      throw new UnauthorizedException({ message: 'Usuario o contraseña incorrectos.' });
    }
    const user = await this.findByUsername(username);
    if (!user?.is_active || !user.password_hash ||
        !verifyPassword(password, user.password_hash)) {
      throw new UnauthorizedException({ message: 'Usuario o contraseña incorrectos.' });
    }
    return this.issueToken(user, 'local');
  }

  async register(username: string, password: string, fullName: string) {
    const normalized = username?.trim().toLowerCase();
    const name = fullName?.trim();
    if (!normalized || !name || !password || password.length < 8) {
      throw new BadRequestException({
        message: 'Correo, nombre y contraseña de al menos 8 caracteres son obligatorios.',
      });
    }
    if (await this.findByUsername(normalized)) {
      throw new ConflictException({ message: 'El usuario ya está registrado.' });
    }
    const result = await this.database.query<UserRow>(
      `INSERT INTO users (username, password_hash, full_name, role_id)
       VALUES ($1, $2, $3, 3) RETURNING *`,
      [normalized, hashPassword(password), name],
    );
    return this.issueToken(result.rows[0], 'local');
  }

  async list(roleId?: number) {
    if (roleId !== undefined && ![1, 2, 3].includes(roleId)) {
      throw new BadRequestException({ message: 'El rol indicado no existe.' });
    }
    const result = await this.database.query<UserRow>(
      roleId === undefined
        ? 'SELECT * FROM users ORDER BY id'
        : 'SELECT * FROM users WHERE role_id = $1 ORDER BY id',
      roleId === undefined ? [] : [roleId],
    );
    return result.rows.map(adminUser);
  }

  async get(id: number): Promise<UserRow> {
    const user = await this.findById(id);
    if (!user) throw new NotFoundException({ message: 'Usuario no encontrado.' });
    return user;
  }

  async update(id: number, currentId: number, fullName: string, roleId: number, password?: string) {
    const user = await this.get(id);
    if (!fullName?.trim() || ![1, 2, 3].includes(roleId) ||
        (password !== undefined && password.length < 8)) {
      throw new BadRequestException({ message: 'Nombre, rol o contraseña inválidos.' });
    }
    if (id === currentId && roleId !== user.role_id) {
      throw new BadRequestException({ message: 'No puedes cambiar tu propio rol.' });
    }
    const result = await this.database.query<UserRow>(
      `UPDATE users SET full_name = $2, role_id = $3,
       password_hash = COALESCE($4, password_hash)
       WHERE id = $1 RETURNING *`,
      [id, fullName.trim(), roleId, password === undefined ? null : hashPassword(password)],
    );
    return adminUser(result.rows[0]);
  }

  async changeStatus(id: number, currentId: number, isActive: boolean) {
    await this.get(id);
    if (id === currentId && !isActive) {
      throw new BadRequestException({ message: 'No puedes desactivar tu cuenta.' });
    }
    const result = await this.database.query<UserRow>(
      'UPDATE users SET is_active = $2 WHERE id = $1 RETURNING *',
      [id, isActive],
    );
    return adminUser(result.rows[0]);
  }
}
