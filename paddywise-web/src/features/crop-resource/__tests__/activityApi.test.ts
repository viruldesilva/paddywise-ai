import { describe, it, expect, vi, beforeEach } from 'vitest';
import { activityApi, type CreateCropActivityRequest, type CropActivityDto } from '../services/activityApi';
import { axiosInstance } from '../../../api/axiosInstance';

describe('Crop Activity API (activityApi) Service Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockActivityDto: CropActivityDto = {
    id: 10,
    cultivationCycleId: 4,
    activityType: 'Fertilizer',
    date: '2026-10-04T10:00:00Z',
    detailsJson: JSON.stringify({
      type: 'Urea',
      quantity: 50,
      cropStage: 'Tillering',
      region: 'Wet',
      method: 'Broadcasting'
    }),
    loggedByUserId: 12,
    loggedByUserName: 'Farmer Bandara',
    createdAt: '2026-10-04T10:05:00Z',
    fieldName: 'Maha Kumbura',
    cycleName: 'Maha 2026'
  };

  describe('getActivitiesForCycle', () => {
    it('calls GET /cycles/{cycleId}/activities and returns activity array', async () => {
      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
        data: [mockActivityDto]
      });

      const result = await activityApi.getActivitiesForCycle(4);

      expect(getSpy).toHaveBeenCalledWith('/cycles/4/activities');
      expect(result).toHaveLength(1);
      expect(result[0].id).toBe(10);
      expect(result[0].activityType).toBe('Fertilizer');
    });

    it('propagates error when GET activities for cycle fails', async () => {
      vi.spyOn(axiosInstance, 'get').mockRejectedValueOnce(new Error('Network Error 500'));

      await expect(activityApi.getActivitiesForCycle(99)).rejects.toThrow('Network Error 500');
    });
  });

  describe('getAllActivities', () => {
    it('calls GET /activities without params when none provided', async () => {
      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
        data: [mockActivityDto]
      });

      const result = await activityApi.getAllActivities();

      expect(getSpy).toHaveBeenCalledWith('/activities', { params: undefined });
      expect(result).toHaveLength(1);
    });

    it('calls GET /activities with query parameters when filtered', async () => {
      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
        data: [mockActivityDto]
      });

      const params = { farmerId: 12, cycleId: 4, activityType: 'Fertilizer' };
      const result = await activityApi.getAllActivities(params);

      expect(getSpy).toHaveBeenCalledWith('/activities', { params });
      expect(result[0].activityType).toBe('Fertilizer');
    });
  });

  describe('createActivity', () => {
    it('calls POST /cycles/{cycleId}/activities with activity payload', async () => {
      const payload: CreateCropActivityRequest = {
        activityType: 'Irrigation',
        date: '2026-10-04',
        detailsJson: JSON.stringify({
          waterLevel: 5,
          duration: 3,
          source: 'Canal'
        })
      };

      const postSpy = vi.spyOn(axiosInstance, 'post').mockResolvedValueOnce({
        data: { ...mockActivityDto, ...payload, id: 11, activityType: 'Irrigation' }
      });

      const result = await activityApi.createActivity(4, payload);

      expect(postSpy).toHaveBeenCalledWith('/cycles/4/activities', payload);
      expect(result.id).toBe(11);
      expect(result.activityType).toBe('Irrigation');
    });

    it('propagates error when activity creation fails', async () => {
      vi.spyOn(axiosInstance, 'post').mockRejectedValueOnce(new Error('Validation error: date in future'));

      const payload: CreateCropActivityRequest = {
        activityType: 'Irrigation',
        date: '2027-01-01',
        detailsJson: '{}'
      };

      await expect(activityApi.createActivity(4, payload)).rejects.toThrow('Validation error: date in future');
    });
  });

  describe('updateActivity', () => {
    it('calls PUT /activities/{activityId} with updated data', async () => {
      const updatePayload: CreateCropActivityRequest = {
        activityType: 'Fertilizer',
        date: '2026-10-03',
        detailsJson: JSON.stringify({
          type: 'Urea',
          quantity: 60,
          cropStage: 'Tillering',
          region: 'Wet',
          method: 'Broadcasting'
        })
      };

      const putSpy = vi.spyOn(axiosInstance, 'put').mockResolvedValueOnce({
        data: { ...mockActivityDto, detailsJson: updatePayload.detailsJson }
      });

      const result = await activityApi.updateActivity(10, updatePayload);

      expect(putSpy).toHaveBeenCalledWith('/activities/10', updatePayload);
      expect(result.id).toBe(10);
    });

    it('propagates error when updating activity fails', async () => {
      vi.spyOn(axiosInstance, 'put').mockRejectedValueOnce(new Error('Activity not found'));

      await expect(
        activityApi.updateActivity(999, {
          activityType: 'Fertilizer',
          date: '2026-10-03',
          detailsJson: '{}'
        })
      ).rejects.toThrow('Activity not found');
    });
  });

  describe('deleteActivity', () => {
    it('calls DELETE /activities/{activityId}', async () => {
      const deleteSpy = vi.spyOn(axiosInstance, 'delete').mockResolvedValueOnce({ data: null });

      await activityApi.deleteActivity(10);

      expect(deleteSpy).toHaveBeenCalledWith('/activities/10');
    });

    it('propagates error when deleting activity fails', async () => {
      vi.spyOn(axiosInstance, 'delete').mockRejectedValueOnce(new Error('Unauthorized'));

      await expect(activityApi.deleteActivity(10)).rejects.toThrow('Unauthorized');
    });
  });
});
