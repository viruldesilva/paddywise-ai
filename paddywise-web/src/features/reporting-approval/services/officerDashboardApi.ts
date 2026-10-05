import { axiosInstance } from '../../../api/axiosInstance';
import type { OfficerDashboardData } from '../types';

export async function getOfficerDashboard(): Promise<OfficerDashboardData> {
  const response = await axiosInstance.get<OfficerDashboardData>('/officer/dashboard');
  return response.data;
}

export async function approveTreatment(reportId: number, comment?: string): Promise<void> {
  await axiosInstance.post(`/pest-disease-reports/${reportId}/review`, {
    decision: 'Approve',
    comment: comment || null,
  });
}

export async function requestDetailsTreatment(reportId: number, comment: string): Promise<void> {
  await axiosInstance.post(`/pest-disease-reports/${reportId}/review`, {
    decision: 'RevisionRequested',
    comment: comment.trim() || 'Requesting secondary field inspection and symptom details.',
  });
}
