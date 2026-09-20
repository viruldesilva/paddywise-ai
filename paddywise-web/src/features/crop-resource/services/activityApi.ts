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
}

export const activityApi = {
  getActivitiesForCycle: async (cycleId: number): Promise<CropActivityDto[]> => {
    const response = await axiosInstance.get(`/cycles/${cycleId}/activities`);
    return response.data;
  },

  createActivity: async (cycleId: number, data: CreateCropActivityRequest): Promise<CropActivityDto> => {
    const response = await axiosInstance.post(`/cycles/${cycleId}/activities`, data);
    return response.data;
  }
};
