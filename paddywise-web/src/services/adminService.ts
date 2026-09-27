import { axiosInstance } from '../api/axiosInstance';
import type { OfficerRequestDto } from '../types/auth';

export interface IAdminService {
  getPendingOfficerRequests(): Promise<OfficerRequestDto[]>;
  approveOfficerRequest(userId: number): Promise<{ message: string }>;
  rejectOfficerRequest(userId: number, reason?: string): Promise<{ message: string }>;
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
}

export const adminService = new AdminService();
