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
 alertRuleId: number | null;
 detectedValue: number | null;
 thresholdValue: number | null;
 status: 'ACTIVE' | 'ATTENDED' | 'CLOSED';
 handledByUserId: number | null;
 handledAt: string | null;
 closedByUserId: number | null;
}
