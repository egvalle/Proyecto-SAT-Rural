export interface SensorHistoryReading {
  value: number;
  recordedAt: string;
}

export interface SensorHistory {
  type: string;
  readings: SensorHistoryReading[];
}