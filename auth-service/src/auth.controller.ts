import {
  Body,
  BadRequestException,
  Controller,
  Get,
  HttpCode,
  HttpStatus,
  Param,
  ParseIntPipe,
  Patch,
  Post,
  Put,
  Query,
  Req,
  Res,
  UseGuards,
} from '@nestjs/common';
import { AdminGuard, AuthenticatedRequest, JwtGuard } from './guards.js';
import { OAuthService } from './oauth.service.js';
import { UsersService, publicUser } from './users.service.js';
import { AuditService } from './audit.service.js';

function validProvider(value: string): 'google' | 'github' {
  if (value === 'google' || value === 'github') return value;
  throw new BadRequestException('Proveedor desconocido.');
}

@Controller('api/auth')
export class AuthController {
  constructor(
    private readonly users: UsersService,
    private readonly oauth: OAuthService,
    private readonly audit: AuditService,
  ) {}

  @Get('providers')
  providers() {
    return this.oauth.providers();
  }

  @Get('oauth/:provider')
  async startOAuth(
    @Param('provider') provider: string,
    @Res() response: { redirect(url: string): void },
  ) {
    response.redirect(await this.oauth.start(validProvider(provider)));
  }

  @Get('callback/:provider')
  async oauthCallback(
    @Param('provider') provider: string,
    @Query('code') code: string,
    @Query('state') state: string,
    @Res() response: { redirect(url: string): void },
  ) {
    response.redirect(await this.oauth.callback(validProvider(provider), code, state));
  }

  @Post('exchange')
  async exchange(@Body() body: { code: string }) {
    const result = await this.oauth.exchange(body?.code);
    const actor = await this.users.get(result.user.id);
    await this.audit.record(actor, 'LOGIN', actor.id,
      'Inicio de sesión con proveedor externo.');
    return result;
  }

  @Post('login')
  async login(@Body() body: { username: string; password: string }) {
    const result = await this.users.login(body?.username, body?.password);
    const actor = await this.users.get(result.user.id);
    await this.audit.record(actor, 'LOGIN', actor.id, 'Inicio de sesión.');
    return result;
  }

  @Post('register')
  register(@Body() body: { username: string; password: string; fullName: string }) {
    return this.users.register(body?.username, body?.password, body?.fullName);
  }

  @UseGuards(JwtGuard)
  @Get('me')
  me(@Req() request: AuthenticatedRequest) {
    return publicUser(request.user);
  }

  @UseGuards(JwtGuard)
  @Post('logout')
  @HttpCode(HttpStatus.NO_CONTENT)
  async logout(@Req() request: AuthenticatedRequest) {
    await this.audit.record(request.user, 'LOGOUT', request.user.id,
      'Cierre de sesión.');
  }

  @UseGuards(JwtGuard, AdminGuard)
  @Get('admin/users')
  listUsers(@Query('roleId') roleId?: string) {
    return this.users.list(roleId === undefined ? undefined : Number(roleId));
  }

  @UseGuards(JwtGuard, AdminGuard)
  @Get('admin/users/:id')
  async getUser(@Param('id', ParseIntPipe) id: number) {
    return publicUser(await this.users.get(id));
  }

  @UseGuards(JwtGuard, AdminGuard)
  @Put('admin/users/:id')
  async updateUser(
    @Param('id', ParseIntPipe) id: number,
    @Req() request: AuthenticatedRequest,
    @Body() body: { fullName: string; roleId: number; password?: string },
  ) {
    const updated = await this.users.update(
      id, request.user.id, body.fullName, body.roleId, body.password,
    );
    await this.audit.record(request.user, 'UPDATE', id,
      `Usuario actualizado: ${updated.username}`);
    return updated;
  }

  @UseGuards(JwtGuard, AdminGuard)
  @Patch('admin/users/:id/status')
  async changeStatus(
    @Param('id', ParseIntPipe) id: number,
    @Req() request: AuthenticatedRequest,
    @Body() body: { isActive: boolean },
  ) {
    const updated = await this.users.changeStatus(id, request.user.id, body.isActive);
    await this.audit.record(request.user, 'STATUS', id,
      `Estado activo: ${updated.isActive}`);
    return updated;
  }
}

@Controller()
export class HealthController {
  @Get('health')
  health() {
    return { status: 'healthy' };
  }
}
