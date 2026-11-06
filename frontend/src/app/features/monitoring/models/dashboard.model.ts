export interface DashboardData {
  temperature: number | null;
  humidity: number | null;
  windSpeed: number | null;
  rainfall: number | null;
  riverLevel: number | null;
  humiditySource: string | null;
  humidityCode: string | null;
  sensorStates: DashboardSensorState[];

  status: string;
  statusMessage: string;

  updatedAt: string;
}

export interface DashboardSensorState {
  id: number;
  communityId: number;
  code: string;
  name: string;
  type: string;
  unit: string;
  value: number | null;
  source: string | null;
  recordedAt: string | null;
}
