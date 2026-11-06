import { Module } from '@nestjs/common';
import { JwtModule } from '@nestjs/jwt';
import { AuthController, HealthController } from './auth.controller.js';
import { Database } from './database.js';
import { AdminGuard, JwtGuard } from './guards.js';
import { OAuthService } from './oauth.service.js';
import { UsersService } from './users.service.js';
import { AuditService } from './audit.service.js';

const jwtKey = process.env.JWT_KEY;
if (!jwtKey || jwtKey.length < 32) {
  throw new Error('JWT_KEY debe tener al menos 32 caracteres.');
}

@Module({
  imports: [
    JwtModule.register({
      secret: jwtKey,
      signOptions: {
        algorithm: 'HS256',
        issuer: process.env.JWT_ISSUER,
        audience: process.env.JWT_AUDIENCE,
        expiresIn: Number(process.env.JWT_EXPIRES_MINUTES ?? '60') * 60,
      },
    }),
  ],
  controllers: [AuthController, HealthController],
  providers: [Database, UsersService, OAuthService, AuditService, JwtGuard, AdminGuard],
})
export class AppModule {}
