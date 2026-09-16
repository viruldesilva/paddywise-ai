/**
 * Every /api/fields, /api/divisions, /api/cycles and /api/varieties route,
 * typed. All traffic goes through axiosInstance so the bearer token, the 401
 * handler and the single-flight refresh queue apply. No React in this file.
 */
import { axiosInstance } from '../../../api/axiosInstance';
import type {
  CreateCycleRequest,
  CreateFieldRequest,
  CultivationCycle,
  CultivationPlan,
  CycleStatus,
  Division,
  Field,
  LogStageRequest,
  PendingPlanSummary,
  RequestPlanRequest,
  ReviewPlanRequest,
  UpdateCycleStatusRequest,
  UpdateFieldRequest,
  Variety,
} from '../types';

/* ------------------------------------------------------------------ fields */

/** GET /api/fields — the caller's own active fields. Farmer only. */
export async function getMyFields(): Promise<Field[]> {
  const response = await axiosInstance.get<Field[]>('/fields');
  return response.data;
}

/**
 * GET /api/fields/division/{divisionId} — every active field in a division.
 * AgriculturalOfficer, FieldOfficer and Admin only.
 */
export async function getFieldsByDivision(divisionId: number): Promise<Field[]> {
  const response = await axiosInstance.get<Field[]>(`/fields/division/${divisionId}`);
  return response.data;
}

/** GET /api/fields/{id} — a farmer may read only their own field. */
export async function getFieldById(id: number): Promise<Field> {
  const response = await axiosInstance.get<Field>(`/fields/${id}`);
  return response.data;
}

/** POST /api/fields. Farmer only; the owner comes from the access token. */
export async function createField(request: CreateFieldRequest): Promise<Field> {
  const response = await axiosInstance.post<Field>('/fields', request);
  return response.data;
}

/** PUT /api/fields/{id} — a full replace of every editable value. */
export async function updateField(id: number, request: UpdateFieldRequest): Promise<Field> {
  const response = await axiosInstance.put<Field>(`/fields/${id}`, request);
  return response.data;
}

/** DELETE /api/fields/{id} — a soft delete; the row stays, IsActive goes false. */
export async function deleteField(id: number): Promise<void> {
  await axiosInstance.delete<void>(`/fields/${id}`);
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

/** GET /api/cycles — the caller's cycles, optionally narrowed to one field. */
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

/**
 * POST /api/fields/{fieldId}/start-cultivation — Start Cultivation Cycle.
 * The route is the authority on the field, so fieldId is not in the body.
 */
export async function startCultivation(
  fieldId: number,
  request: CreateCycleRequest
): Promise<CultivationCycle> {
  const response = await axiosInstance.post<CultivationCycle>(
    `/fields/${fieldId}/start-cultivation`,
    request
  );
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

/** PATCH /api/cycles/{id}/status. Farmer only. */
export async function updateCycleStatus(
  cycleId: number,
  status: CycleStatus
): Promise<CultivationCycle> {
  const body: UpdateCycleStatusRequest = { status };
  const response = await axiosInstance.patch<CultivationCycle>(
    `/cycles/${cycleId}/status`,
    body
  );
  return response.data;
}

/* ------------------------------------------------------------------- plans */

/**
 * POST /api/cycles/{cycleId}/plans — ask the Cultivation Planning Agent for a
 * plan. Farmer only, and slow: the agent's tool loop plus the deterministic
 * validator take 20–40 seconds, so the caller must keep its UI honest for that
 * long. axiosInstance sets no timeout, which is what lets the request stand.
 *
 * 400 when the cycle already has a plan awaiting approval or approved.
 */
export async function requestPlan(cycleId: number, objective: string): Promise<CultivationPlan> {
  const body: RequestPlanRequest = { objective };
  const response = await axiosInstance.post<CultivationPlan>(`/cycles/${cycleId}/plans`, body);
  return response.data;
}

/** GET /api/plans/{id} — the plan with its agent runs. A farmer reads only their own. */
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
  getMyFields,
  getFieldsByDivision,
  getFieldById,
  createField,
  updateField,
  deleteField,
  getDivisions,
  getVarieties,
  getMyCycles,
  getCycleById,
  startCultivation,
  logStage,
  updateCycleStatus,
  requestPlan,
  getPlan,
  getPlansForCycle,
  getPendingPlans,
  reviewPlan,
};
