import { axiosInstance } from '../../../api/axiosInstance';

export interface CreateCropActivityRequest {
  activityType: string;
  date: string;
  detailsJson: string;
}

export interface CropActivityDto {
  id: number;
  cultivationCycleId: number;
  activityType: string;
  date: string;
  detailsJson: string;
  loggedByUserId: number;
  loggedByUserName: string;
  createdAt: string;
  fieldName?: string;
  farmerName?: string;
  farmerId?: number;
  cycleName?: string;
}

export const activityApi = {
  getActivitiesForCycle: async (cycleId: number): Promise<CropActivityDto[]> => {
    const response = await axiosInstance.get(`/cycles/${cycleId}/activities`);
    return response.data;
  },

  getAllActivities: async (params?: { farmerId?: number; cycleId?: number; activityType?: string }): Promise<CropActivityDto[]> => {
    const response = await axiosInstance.get('/activities', { params });
    return response.data;
  },

  createActivity: async (cycleId: number, data: CreateCropActivityRequest): Promise<CropActivityDto> => {
    const response = await axiosInstance.post(`/cycles/${cycleId}/activities`, data);
    return response.data;
  },

  updateActivity: async (activityId: number, data: CreateCropActivityRequest): Promise<CropActivityDto> => {
    const response = await axiosInstance.put(`/activities/${activityId}`, data);
    return response.data;
  },

  deleteActivity: async (activityId: number): Promise<void> => {
    await axiosInstance.delete(`/activities/${activityId}`);
  }
};
