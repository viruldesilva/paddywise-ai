import { axiosInstance } from '../../../api/axiosInstance';

export interface DraftRevisionCommentResponse {
  draftComment: string;
}

/**
 * Calls Component 4's AI RevisionDraftAgent to generate a suggested revision comment
 * for a CultivationPlan based on its automated validation failures.
 */
export async function getPlanDraftRevisionComment(planId: number): Promise<string> {
  const response = await axiosInstance.get<DraftRevisionCommentResponse>(
    `/reviews/plans/${planId}/draft-revision-comment`
  );
  return response.data.draftComment;
}

/**
 * Calls Component 4's AI RevisionDraftAgent to generate a suggested revision comment
 * for a PestDiseaseReport based on observed symptoms and diagnosis confidence.
 */
export async function getReportDraftRevisionComment(reportId: number): Promise<string> {
  const response = await axiosInstance.get<DraftRevisionCommentResponse>(
    `/reviews/reports/${reportId}/draft-revision-comment`
  );
  return response.data.draftComment;
}
