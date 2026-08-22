export interface RecentAlert {
  id: number;
  type: string;
  level: string;
  message: string;
  createdAt: string;
  isActive: boolean;
}