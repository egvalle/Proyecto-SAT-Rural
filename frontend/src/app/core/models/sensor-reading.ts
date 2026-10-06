export interface SensorReading {
  id: number;
  sensorId: number;
  sensorCode: string;
  sensorName: string;
  sensorType: string;
  communityId: number;
  communityName: string;
  value: number;
  unit: string;
  sensorIsActive: boolean;
  recordedAt: string;
}

export interface SensorReadingPagedResult {
  items: SensorReading[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}