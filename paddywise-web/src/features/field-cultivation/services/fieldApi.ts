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
  CycleStatus,
  Division,
  Field,
  LogStageRequest,
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
};
