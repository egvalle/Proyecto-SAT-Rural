export interface Sensor {
  id: number;
  communityId: number;
  communityName: string;
  code: string;
  name: string;
  type: string;
  unit: string;
  location: string | null;
  installationDate: string | null;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateSensorRequest {
  communityId: number;
  code: string;
  name: string;
  type: string;
  unit: string;
  location: string | null;
  installationDate: string | null;
  description: string | null;
  isActive: boolean;
}

export interface UpdateSensorRequest {
  communityId: number;
  code: string;
  name: string;
  type: string;
  unit: string;
  location: string | null;
  installationDate: string | null;
  description: string | null;
}

export interface SensorPagedResult {
  items: Sensor[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}