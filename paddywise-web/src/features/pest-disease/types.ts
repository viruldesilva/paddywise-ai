/**
 * TypeScript mirrors of the backend DTOs in
 * paddywise-backend/DTOs/PestDisease/.
 *
 * The API serialises property names as camelCase and enum-typed DTO
 * properties as their C# member name (a string, not the ordinal).
 */

/** Entities/PestDisease/ObservationType.cs */
export type ObservationType = 'Pest' | 'Disease' | 'Unknown';

/** Entities/PestDisease/ObservationSeverity.cs */
export type ObservationSeverity = 'Low' | 'Moderate' | 'Severe';

/** Entities/PestDisease/PestDiseaseReportStatus.cs */
export type PestDiseaseReportStatus =
  | 'PendingOfficerReview'
  | 'Approved'
  | 'Rejected'
  | 'RevisionRequested';

export const OBSERVATION_TYPES: readonly ObservationType[] = ['Pest', 'Disease', 'Unknown'];

export const OBSERVATION_SEVERITIES: readonly ObservationSeverity[] = [
  'Low',
  'Moderate',
  'Severe',
];

export const OBSERVATION_TYPE_LABELS: Record<ObservationType, string> = {
  Pest: 'Pest',
  Disease: 'Disease',
  Unknown: "Not sure — let the agent decide",
};

export const SEVERITY_LABELS: Record<ObservationSeverity, string> = {
  Low: 'Low',
  Moderate: 'Moderate',
  Severe: 'Severe',
};

export const REPORT_STATUS_LABELS: Record<PestDiseaseReportStatus, string> = {
  PendingOfficerReview: 'Awaiting officer review',
  Approved: 'Approved',
  Rejected: 'Rejected',
  RevisionRequested: 'Revision requested',
};

/** DTOs/PestDisease/PestDiseaseReportSummaryDto.cs — one candidate on an observation. */
export interface PestDiseaseReportSummary {
  id: number;
  possibleIssue: string;
  confidence: number;
  status: PestDiseaseReportStatus;
  officerComment: string | null;
  reviewedAt: string | null;
  createdAt: string;
}

/** DTOs/PestDisease/ObservationResponseDto.cs */
export interface Observation {
  id: number;
  cultivationCycleId: number;
  fieldId: number;
  fieldName: string;
  reportedByUserId: number;
  reportedByUserName: string;
  observationType: ObservationType;
  cropStage: string;
  symptoms: string;
  severity: ObservationSeverity;
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string;
  reports: PestDiseaseReportSummary[];
}

/** DTOs/PestDisease/CreateObservationRequestDto.cs */
export interface CreateObservationRequest {
  cultivationCycleId: number;
  observationType: ObservationType;
  symptoms: string;
  severity: ObservationSeverity;
  imageUrl: string | null;
}

/** DTOs/PestDisease/UpdateObservationRequestDto.cs — the cycle cannot change. */
export interface UpdateObservationRequest {
  observationType: ObservationType;
  symptoms: string;
  severity: ObservationSeverity;
  imageUrl: string | null;
}

/**
 * CreateObservationRequestDto's/UpdateObservationRequestDto's DataAnnotations, so the
 * farmer sees the same verdict the server would give, without a round trip.
 */
export const OBSERVATION_RULES = {
  symptomsMaxLength: 2000,
} as const;

/** DTOs/PestDisease/PestDiseaseReportResponseDto.cs — full detail, for the officer queue. */
export interface PestDiseaseReport {
  id: number;
  cropObservationId: number;
  cultivationCycleId: number;
  fieldId: number;
  fieldName: string;
  reportedByUserId: number;
  reportedByUserName: string;
  observationType: ObservationType;
  cropStage: string;
  symptoms: string;
  severity: ObservationSeverity;
  imageUrl: string | null;
  possibleIssue: string;
  confidence: number;
  status: PestDiseaseReportStatus;
  officerId: number | null;
  officerName: string | null;
  officerComment: string | null;
  reviewedAt: string | null;
  createdAt: string;
}

/** DTOs/PestDisease/ReviewPestDiseaseReportDto.cs — PestDiseaseReportReviewDecision, by member name. */
export type ReportReviewDecision = 'Approve' | 'Reject' | 'RequestRevision';

export const REPORT_REVIEW_DECISIONS: readonly ReportReviewDecision[] = [
  'Approve',
  'Reject',
  'RequestRevision',
];

export const REPORT_REVIEW_DECISION_LABELS: Record<ReportReviewDecision, string> = {
  Approve: 'Approve',
  Reject: 'Reject',
  RequestRevision: 'Request revision',
};

/** The status a report lands in once each decision is taken. */
export const REPORT_REVIEW_OUTCOME: Record<ReportReviewDecision, PestDiseaseReportStatus> = {
  Approve: 'Approved',
  Reject: 'Rejected',
  RequestRevision: 'RevisionRequested',
};

/** DTOs/PestDisease/ReviewPestDiseaseReportDto.cs */
export interface ReviewReportRequest {
  decision: ReportReviewDecision;
  comment: string | null;
}

/** ReviewPestDiseaseReportDto's Comment [MaxLength(1000)] — required for anything but Approve. */
export const REPORT_REVIEW_RULES = {
  commentMaxLength: 1000,
} as const;

export function reportReviewNeedsComment(decision: ReportReviewDecision): boolean {
  return decision !== 'Approve';
}

/** A confidence of 0.82 reads as "82%" — never presented as certainty. */
export function formatConfidence(confidence: number): string {
  return `${Math.round(confidence * 100)}%`;
}
