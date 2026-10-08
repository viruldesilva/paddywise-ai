import { describe, it, expect, vi, beforeEach } from 'vitest';
import { axiosInstance } from '../../../api/axiosInstance';
import { activityApi } from '../services/activityApi';
import { cropAnalysisApi } from '../services/cropAnalysisApi';

vi.mock('../../../api/axiosInstance', () => ({
  axiosInstance: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const http = vi.mocked(axiosInstance);

/** Every Component 2 web call goes through axiosInstance (never bare axios), to these URLs. */
describe('activityApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    http.get.mockResolvedValue({ data: 'GET' });
    http.post.mockResolvedValue({ data: 'POST' });
    http.put.mockResolvedValue({ data: 'PUT' });
    http.delete.mockResolvedValue({ data: undefined });
  });

  const body = { activityType: 'Irrigation', date: '2026-10-01', detailsJson: '{}' };

  it('reads a cycle\'s activities and the filtered list', async () => {
    await expect(activityApi.getActivitiesForCycle(12)).resolves.toBe('GET');
    await activityApi.getAllActivities({ cycleId: 12, activityType: 'Fertilizer' });
    expect(http.get).toHaveBeenNthCalledWith(1, '/cycles/12/activities');
    expect(http.get).toHaveBeenNthCalledWith(2, '/activities', { params: { cycleId: 12, activityType: 'Fertilizer' } });
  });

  it('creates, updates and deletes at the right URLs', async () => {
    await expect(activityApi.createActivity(12, body)).resolves.toBe('POST');
    await expect(activityApi.updateActivity(7, body)).resolves.toBe('PUT');
    await activityApi.deleteActivity(7);
    expect(http.post).toHaveBeenCalledWith('/cycles/12/activities', body);
    expect(http.put).toHaveBeenCalledWith('/activities/7', body);
    expect(http.delete).toHaveBeenCalledWith('/activities/7');
  });
});

describe('cropAnalysisApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    http.get.mockResolvedValue({ data: 'GET' });
    http.post.mockResolvedValue({ data: 'POST' });
  });

  it('runs and reads the analysis, and asks the advisor', async () => {
    await cropAnalysisApi.runAnalysis(12, 'Water?');
    await cropAnalysisApi.getLatestAnalysis(12);
    await cropAnalysisApi.askAiQuestion(12, 'Water?');
    expect(http.post).toHaveBeenNthCalledWith(1, '/cycles/12/analysis', {
      cultivationCycleId: 12,
      farmerQuestion: 'Water?',
      focusArea: 'All',
    });
    expect(http.get).toHaveBeenCalledWith('/cycles/12/analysis/latest');
    expect(http.post).toHaveBeenNthCalledWith(2, '/cycles/12/ai-chat', { cultivationCycleId: 12, question: 'Water?' });
  });

  it('reads the officer queue, with a division filter only when given', async () => {
    await cropAnalysisApi.getPendingOfficerRecommendations();
    await cropAnalysisApi.getPendingOfficerRecommendations(3);
    expect(http.get).toHaveBeenNthCalledWith(1, '/recommendations/pending', { params: undefined });
    expect(http.get).toHaveBeenNthCalledWith(2, '/recommendations/pending', { params: { divisionId: 3 } });
  });

  it('posts an officer review and a farmer execution', async () => {
    await cropAnalysisApi.officerReviewRecommendation(31, 'Reject', 'Too early.');
    await cropAnalysisApi.executeFarmerRecommendation(12, 31);
    await cropAnalysisApi.getCycleRecommendations(12);
    expect(http.post).toHaveBeenNthCalledWith(1, '/recommendations/31/officer-review', { decision: 'Reject', comment: 'Too early.' });
    expect(http.post).toHaveBeenNthCalledWith(2, '/cycles/12/recommendations/review', {
      recommendationId: '31',
      decision: 'Execute',
      notes: undefined,
    });
    expect(http.get).toHaveBeenCalledWith('/cycles/12/recommendations');
  });
});
