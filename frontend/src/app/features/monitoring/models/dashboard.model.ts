export interface DashboardData {
  temperature: number;
  humidity: number;
  windSpeed: number;
  rainfall: number;
  riverLevel: number;

  status: string;
  statusMessage: string;

  updatedAt: string;
}