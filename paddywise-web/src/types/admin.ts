export interface HealthCheckResponse {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  database: 'Healthy' | 'Degraded' | 'Unhealthy' | 'Unknown';
  totalDurationMs: number;
}

export interface AdminDashboardDto {
  totalUsers: number;
  usersPerRole: Record<string, number>;
  activeUsers: number;
  inactiveUsers: number;
  pendingOfficerApprovals: number;
}

export interface AdminUserDto {
  id: number;
  fullName: string;
  email: string;
  role: string;
  assignedLocation: string;
  status: string;
  createdAt: string;
}

export interface AdminUsersPagedResponseDto {
  page: number;
  pageSize: number;
  totalCount: number;
  items: AdminUserDto[];
}

export interface GetAdminUsersParams {
  search?: string;
  role?: string;
  page?: number;
  pageSize?: number;
}
