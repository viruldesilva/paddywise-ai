import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  getPlanDraftRevisionComment,
  getReportDraftRevisionComment,
} from '../services/reviewsApi';
import { axiosInstance } from '../../../api/axiosInstance';

describe('Component 4 RevisionDraftAgent API Service Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getPlanDraftRevisionComment calls GET /reviews/plans/{planId}/draft-revision-comment', async () => {
    const mockDraft =
      'Reduce basal nitrogen application by 10% to prevent excessive vegetative growth.';
    const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
      data: { draftComment: mockDraft },
    });

    const result = await getPlanDraftRevisionComment(42);

    expect(getSpy).toHaveBeenCalledWith('/reviews/plans/42/draft-revision-comment');
    expect(result).toBe(mockDraft);
  });

  it('getReportDraftRevisionComment calls GET /reviews/reports/{reportId}/draft-revision-comment', async () => {
    const mockReportDraft =
      'Observed symptoms align with Rice Blast. Recommend prophylactic tricyclazole spray as per DOA circular.';
    const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
      data: { draftComment: mockReportDraft },
    });

    const result = await getReportDraftRevisionComment(88);

    expect(getSpy).toHaveBeenCalledWith('/reviews/reports/88/draft-revision-comment');
    expect(result).toBe(mockReportDraft);
  });

  it('propagates error when draft revision endpoint fails', async () => {
    vi.spyOn(axiosInstance, 'get').mockRejectedValueOnce(
      new Error('Revision drafting service temporarily unavailable.')
    );

    await expect(getPlanDraftRevisionComment(99)).rejects.toThrow(
      'Revision drafting service temporarily unavailable.'
    );
  });
});
