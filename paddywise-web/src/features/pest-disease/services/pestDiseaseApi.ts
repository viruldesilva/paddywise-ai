/**
 * Every /api/observations and /api/pest-disease-reports route, typed. All
 * traffic goes through axiosInstance so the bearer token, the 401 handler and
 * the single-flight refresh queue apply. No React in this file.
 */
import { axiosInstance } from '../../../api/axiosInstance';
import type {
  CreateObservationRequest,
  Observation,
  PestDiseaseReport,
  PestDiseaseReportStatus,
  ReviewReportRequest,
  UpdateObservationRequest,
} from '../types';

/* ------------------------------------------------------------- observations */

/** GET /api/observations — the caller's own reports, or every report for officers/admins. */
export async function getObservations(params?: {
  cultivationCycleId?: number;
  fieldId?: number;
}): Promise<Observation[]> {
  const response = await axiosInstance.get<Observation[]>('/observations', { params });
  return response.data;
}

/** GET /api/observations/{id}. */
export async function getObservationById(id: number): Promise<Observation> {
  const response = await axiosInstance.get<Observation>(`/observations/${id}`);
  return response.data;
}

/** POST /api/observations. Farmer only; the owner comes from the access token. */
export async function createObservation(request: CreateObservationRequest): Promise<Observation> {
  const response = await axiosInstance.post<Observation>('/observations', request);
  return response.data;
}

/** PUT /api/observations/{id} — only while the observation has no diagnosis yet. */
export async function updateObservation(
  id: number,
  request: UpdateObservationRequest
): Promise<Observation> {
  const response = await axiosInstance.put<Observation>(`/observations/${id}`, request);
  return response.data;
}

/**
 * POST /api/observations/{id}/request-analysis — runs the Crop Analysis Agent.
 * Farmer only, and slow: the Gemini call plus tool loop plus validator can take
 * a while, so axiosInstance sets no timeout, which is what lets the request
 * stand. 400 when the observation already has a diagnosis, or when the run
 * failed validation.
 */
export async function requestAnalysis(id: number): Promise<Observation> {
  const response = await axiosInstance.post<Observation>(`/observations/${id}/request-analysis`);
  return response.data;
}

/* ---------------------------------------------------------- pest/disease reports */

/**
 * GET /api/pest-disease-reports — the officer review queue, or — for a farmer
 * — only diagnoses on their own observations.
 */
export async function getPestDiseaseReports(params?: {
  status?: PestDiseaseReportStatus;
  observationId?: number;
  cultivationCycleId?: number;
}): Promise<PestDiseaseReport[]> {
  const response = await axiosInstance.get<PestDiseaseReport[]>('/pest-disease-reports', {
    params,
  });
  return response.data;
}

/** GET /api/pest-disease-reports/{id}. */
export async function getPestDiseaseReportById(id: number): Promise<PestDiseaseReport> {
  const response = await axiosInstance.get<PestDiseaseReport>(`/pest-disease-reports/${id}`);
  return response.data;
}

/**
 * POST /api/pest-disease-reports/{id}/review — approve, reject or send a
 * diagnosis back. AgriculturalOfficer only. 400 when the report is no longer
 * awaiting review, or when a reject / revision carries no comment.
 */
export async function reviewPestDiseaseReport(
  id: number,
  request: ReviewReportRequest
): Promise<PestDiseaseReport> {
  const response = await axiosInstance.post<PestDiseaseReport>(
    `/pest-disease-reports/${id}/review`,
    request
  );
  return response.data;
}

export const pestDiseaseApi = {
  getObservations,
  getObservationById,
  createObservation,
  updateObservation,
  requestAnalysis,
  getPestDiseaseReports,
  getPestDiseaseReportById,
  reviewPestDiseaseReport,
};
