export interface Alert {
 id: number;
 communityId: number;
 sensorId: number | null;
 type: string;
 level: string;
 message: string;
 createdAt: string;
 resolvedAt: string | null;
 isActive: boolean;
}
