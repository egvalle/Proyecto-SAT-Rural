export interface Event {
  id: number;
  alertId: number | null;
  communityId: number;
  communityName: string;
  eventType: string;
  description: string;
  occurredAt: string;
  sensorId: number | null;
  level: string | null;
  isActive: boolean | null;
}