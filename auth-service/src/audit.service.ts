import { Injectable, Logger } from '@nestjs/common';
import { UserRow } from './users.service.js';

@Injectable()
export class AuditService {
  private readonly logger = new Logger(AuditService.name);

  async record(actor: UserRow, action: string, entityId: number, details: string) {
    const baseUrl = process.env.AUDIT_SERVICE_URL;
    const key = process.env.AUDIT_SERVICE_API_KEY;
    if (!baseUrl || !key) return;

    try {
      const response = await fetch(new URL('/internal/audit-logs', baseUrl), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Service-Key': key },
        body: JSON.stringify({
          userId: actor.id,
          username: actor.username,
          fullName: actor.full_name,
          action,
          entityName: 'User',
          entityId: String(entityId),
          details,
        }),
        signal: AbortSignal.timeout(3000),
      });
      if (!response.ok) {
        this.logger.warn(`AuditService respondió HTTP ${response.status} para el usuario ${entityId}.`);
      }
    } catch (error) {
      this.logger.warn(`No se pudo registrar la auditoría del usuario ${entityId}: ${String(error)}`);
    }
  }
}
