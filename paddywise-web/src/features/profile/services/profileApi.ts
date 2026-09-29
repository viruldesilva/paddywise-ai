import { axiosInstance } from '../../../api/axiosInstance';

export interface UserProfile {
  id: number;
  name: string;
  email: string;
  phone?: string;
  role: string;
  createdAt: string;
  updatedAt: string;
  totalFields: number;
  totalAcreage: number;
  activeCycles: number;
  divisions: string[];
}

export interface UpdateProfileRequest {
  name: string;
  phone?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export const profileApi = {
  getProfile: async (): Promise<UserProfile> => {
    const res = await axiosInstance.get<UserProfile>('/profile');
    return res.data;
  },

  updateProfile: async (data: UpdateProfileRequest): Promise<UserProfile> => {
    const res = await axiosInstance.put<UserProfile>('/profile', data);
    return res.data;
  },

  changePassword: async (data: ChangePasswordRequest): Promise<{ message: string }> => {
    const res = await axiosInstance.put<{ message: string }>('/profile/password', data);
    return res.data;
  }
};
