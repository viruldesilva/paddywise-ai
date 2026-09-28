/**
 * Every /api/fields, /api/divisions, /api/cycles and /api/varieties route,
 * typed. All traffic goes through axiosInstance so the bearer token, the 401
 * handler and the single-flight refresh queue apply. No React in this file.
 */
import { axiosInstance } from '../../../api/axiosInstance';
import type {
  CultivationCycle,
  CultivationPlan,
  Division,
  Field,
  LogStageRequest,
  PendingPlanSummary,
  ReviewPlanRequest,
  Variety,
} from '../types';

/* ------------------------------------------------------------------ fields */

/**
 * GET /api/fields/division/{divisionId} — every active field in a division.
 * AgriculturalOfficer, FieldOfficer and Admin only.
 */
export async function getFieldsByDivision(divisionId: number): Promise<Field[]> {
  const response = await axiosInstance.get<Field[]>(`/fields/division/${divisionId}`);
  return response.data;
}

/** GET /api/fields/{id}. */
export async function getFieldById(id: number): Promise<Field> {
  const response = await axiosInstance.get<Field>(`/fields/${id}`);
  return response.data;
}

/* --------------------------------------------------------------- divisions */

/** GET /api/divisions — every agrarian division. Any authenticated caller. */
export async function getDivisions(): Promise<Division[]> {
  const response = await axiosInstance.get<Division[]>('/divisions');
  return response.data;
}

/* --------------------------------------------------------------- varieties */

/** GET /api/varieties — the paddy varieties a cycle can be sown with. */
export async function getVarieties(): Promise<Variety[]> {
  const response = await axiosInstance.get<Variety[]>('/varieties');
  return response.data;
}

/* ------------------------------------------------------------------ cycles */

/**
 * GET /api/cycles — a farmer's own cycles, optionally narrowed to one field.
 * Officers must pass fieldId; the backend answers 400 without it.
 */
export async function getMyCycles(fieldId?: number): Promise<CultivationCycle[]> {
  const response = await axiosInstance.get<CultivationCycle[]>('/cycles', {
    params: fieldId === undefined ? undefined : { fieldId },
  });
  return response.data;
}

/** GET /api/cycles/{id}. */
export async function getCycleById(id: number): Promise<CultivationCycle> {
  const response = await axiosInstance.get<CultivationCycle>(`/cycles/${id}`);
  return response.data;
}

/** POST /api/cycles/{id}/stages — record that a cycle reached a growth stage. */
export async function logStage(
  cycleId: number,
  request: LogStageRequest
): Promise<CultivationCycle> {
  const response = await axiosInstance.post<CultivationCycle>(
    `/cycles/${cycleId}/stages`,
    request
  );
  return response.data;
}

/* ------------------------------------------------------------------- plans */

/** GET /api/plans/{id} — the plan with its agent runs. */
export async function getPlan(id: number): Promise<CultivationPlan> {
  const response = await axiosInstance.get<CultivationPlan>(`/plans/${id}`);
  return response.data;
}

/** GET /api/cycles/{cycleId}/plans — every plan for a cycle, newest first. */
export async function getPlansForCycle(cycleId: number): Promise<CultivationPlan[]> {
  const response = await axiosInstance.get<CultivationPlan[]>(`/cycles/${cycleId}/plans`);
  return response.data;
}

/**
 * GET /api/plans/pending — the officer's approval queue, newest first.
 * AgriculturalOfficer only. A divisionId narrows it to one division; omitting it
 * returns every division's plans.
 */
export async function getPendingPlans(divisionId?: number): Promise<PendingPlanSummary[]> {
  const response = await axiosInstance.get<PendingPlanSummary[]>('/plans/pending', {
    params: divisionId === undefined ? undefined : { divisionId },
  });
  return response.data;
}

/**
 * POST /api/plans/{id}/review — approve, reject or send a plan back.
 * AgriculturalOfficer only. 400 when the plan is no longer awaiting approval, or
 * when a reject / revision carries no comment.
 */
export async function reviewPlan(
  id: number,
  request: ReviewPlanRequest
): Promise<CultivationPlan> {
  const response = await axiosInstance.post<CultivationPlan>(`/plans/${id}/review`, request);
  return response.data;
}

export const fieldApi = {
  getFieldsByDivision,
  getFieldById,
  getDivisions,
  getVarieties,
  getMyCycles,
  getCycleById,
  logStage,
  getPlan,
  getPlansForCycle,
  getPendingPlans,
  reviewPlan,
};
