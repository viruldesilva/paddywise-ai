import { describe, it, expect, vi, beforeEach } from 'vitest';
import { axiosInstance } from '../../../api/axiosInstance';
import * as api from '../services/fieldApi';

vi.mock('../../../api/axiosInstance', () => ({
  axiosInstance: { get: vi.fn(), post: vi.fn() },
}));

const get = vi.mocked(axiosInstance.get);
const post = vi.mocked(axiosInstance.post);

describe('fieldApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    get.mockResolvedValue({ data: 'GET-DATA' });
    post.mockResolvedValue({ data: 'POST-DATA' });
  });

  it.each([
    ['getFieldsByDivision', '/fields/division/3', () => api.getFieldsByDivision(3)],
    ['getFieldById', '/fields/4', () => api.getFieldById(4)],
    ['getDivisions', '/divisions', () => api.getDivisions()],
    ['getVarieties', '/varieties', () => api.getVarieties()],
    ['getCycleById', '/cycles/12', () => api.getCycleById(12)],
    ['getPlan', '/plans/31', () => api.getPlan(31)],
    ['getPlansForCycle', '/cycles/12/plans', () => api.getPlansForCycle(12)],
  ] as const)('%s GETs %s through axiosInstance and returns the body', async (_name, url, call) => {
    await expect(call()).resolves.toBe('GET-DATA');
    expect(get).toHaveBeenCalledWith(url);
  });

  it('getMyCycles sends fieldId only when given', async () => {
    await api.getMyCycles();
    await api.getMyCycles(4);
    expect(get).toHaveBeenNthCalledWith(1, '/cycles', { params: undefined });
    expect(get).toHaveBeenNthCalledWith(2, '/cycles', { params: { fieldId: 4 } });
  });

  it('getPendingPlans sends divisionId only when given', async () => {
    await api.getPendingPlans();
    await api.getPendingPlans(2);
    expect(get).toHaveBeenNthCalledWith(1, '/plans/pending', { params: undefined });
    expect(get).toHaveBeenNthCalledWith(2, '/plans/pending', { params: { divisionId: 2 } });
  });

  it('logStage POSTs to the cycle stages endpoint', async () => {
    const request = { stage: 'Tillering' as const, observedOn: '2026-09-20', notes: null };
    await expect(api.logStage(12, request)).resolves.toBe('POST-DATA');
    expect(post).toHaveBeenCalledWith('/cycles/12/stages', request);
  });

  it('reviewPlan POSTs the decision to the review endpoint', async () => {
    await api.reviewPlan(31, { decision: 'RequestRevision', comment: 'Add water.' });
    expect(post).toHaveBeenCalledWith('/plans/31/review', { decision: 'RequestRevision', comment: 'Add water.' });
  });

  it('keeps the functions other components import (crop-resource, pest-disease)', () => {
    expect(typeof api.getCycleById).toBe('function');
    expect(typeof api.getMyCycles).toBe('function');
    expect(api.fieldApi.getMyCycles).toBe(api.getMyCycles);
  });
});
