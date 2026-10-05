import { axiosInstance } from '../api/axiosInstance';
import type { OfficerRequestDto } from '../types/auth';
import type {
  AdminDashboardDto,
  AdminUsersPagedResponseDto,
  GetAdminUsersParams,
  HealthCheckResponse,
} from '../types/admin';

export interface IAdminService {
  getPendingOfficerRequests(): Promise<OfficerRequestDto[]>;
  approveOfficerRequest(userId: number): Promise<{ message: string }>;
  rejectOfficerRequest(userId: number, reason?: string): Promise<{ message: string }>;
  getHealthCheck(): Promise<HealthCheckResponse>;
  getDashboard(): Promise<AdminDashboardDto>;
  getUsers(params?: GetAdminUsersParams): Promise<AdminUsersPagedResponseDto>;
  deleteUser(userId: number): Promise<{ message: string }>;
}

export class AdminService implements IAdminService {
  async getPendingOfficerRequests(): Promise<OfficerRequestDto[]> {
    const response = await axiosInstance.get<OfficerRequestDto[]>('/admin/officer-requests');
    return response.data;
  }

  async approveOfficerRequest(userId: number): Promise<{ message: string }> {
    const response = await axiosInstance.post<{ message: string }>(`/admin/officer-requests/${userId}/approve`);
    return response.data;
  }

  async rejectOfficerRequest(userId: number, reason?: string): Promise<{ message: string }> {
    const response = await axiosInstance.post<{ message: string }>(`/admin/officer-requests/${userId}/reject`, {
      reason: reason || undefined,
    });
    return response.data;
  }

  async getHealthCheck(): Promise<HealthCheckResponse> {
    const response = await axiosInstance.get<HealthCheckResponse>('/health');
    return response.data;
  }

  async getDashboard(): Promise<AdminDashboardDto> {
    const response = await axiosInstance.get<AdminDashboardDto>('/admin/dashboard');
    return response.data;
  }

  async getUsers(params?: GetAdminUsersParams): Promise<AdminUsersPagedResponseDto> {
    const response = await axiosInstance.get<AdminUsersPagedResponseDto>('/admin/users', {
      params,
    });
    return response.data;
  }

  async deleteUser(userId: number): Promise<{ message: string }> {
    const response = await axiosInstance.delete<{ message: string }>(`/admin/users/${userId}`);
    return response.data;
  }
}

export const adminService = new AdminService();
