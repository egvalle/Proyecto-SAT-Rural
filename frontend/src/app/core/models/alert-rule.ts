export interface AlertRule {
  id: number;
  sensorType: string;
  operator: string;
  thresholdValue: number;
  riskLevel: string;
  phenomenon: string;
  description: string;
}

export interface CreateAlertRuleRequest {
  sensorType: string;
  operator: string;
  thresholdValue: number;
  riskLevel: string;
  phenomenon: string;
  description: string;
}

export interface UpdateAlertRuleRequest {
  sensorType: string;
  operator: string;
  thresholdValue: number;
  riskLevel: string;
  phenomenon: string;
  description: string;
}