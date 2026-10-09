export interface Community {
  id: number;
  name: string;
  municipality: string;
  department: string;
  country: string;
  latitude: number | null;
  longitude: number | null;
  description: string | null;
  isActive: boolean;
  sensorCount: number;
  createdAt: string;
}

export interface CreateCommunityRequest {
  name: string;
  municipality: string;
  department: string;
  country: string;
  latitude: number | null;
  longitude: number | null;
  description: string | null;
  isActive: boolean;
}

export interface UpdateCommunityRequest {
  name: string;
  municipality: string;
  department: string;
  country: string;
  latitude: number | null;
  longitude: number | null;
  description: string | null;
}

export interface CommunityPagedResult {
  items: Community[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}