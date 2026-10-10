export interface AdminUser {
  id: number;
  username: string;
  fullName: string;
  roleId: number;
  roleDescription: string;
  isActive: boolean;
  createdAt: string;
}