import { axiosInstance } from '../../../api/axiosInstance';

export interface FieldOverview {
  cycleId: number;
  fieldName: string;
  varietyName: string;
  ageGroup: string;
  daysAfterSowing: number;
  currentStage: string;
  season: string;
  year: number;
  divisionName: string;
  district: string;
}

export interface WaterDiagnostic {
  status: 'Adequate' | 'Low' | 'Flooded' | 'DroughtRisk' | 'DryDraining' | string;
  latestWaterLevelCm?: number;
  totalIrrigationEvents: number;
  totalDurationHours: number;
  daysSinceLastIrrigation: number;
  assessment: string;
}

export interface FertilizerDiagnostic {
  status: 'Balanced' | 'Deficient' | 'Excessive' | 'DueSoon' | string;
  totalUreaKgPerHa: number;
  totalTspKgPerHa: number;
  totalMopKgPerHa: number;
  applicationsCount: number;
  daysSinceLastFertilizer: number;
  splitCompliance: string;
  assessment: string;
}

export interface PestDiagnostic {
  status: 'Safe' | 'MonitoringRequired' | 'HighRisk' | 'SafetyAlert' | string;
  treatmentsCount: number;
  productsUsed: string[];
  targetPests: string[];
  daysSinceLastTreatment: number;
  bannedChemicalDetected: boolean;
  assessment: string;
}

export interface OtherDiagnostic {
  activitiesCount: number;
  activityTypes: string[];
  weedingStatus: string;
  assessment: string;
}

export interface DiagnosticsSummary {
  water: WaterDiagnostic;
  fertilizer: FertilizerDiagnostic;
  pest: PestDiagnostic;
  other: OtherDiagnostic;
}

export interface Citation {
  document: string;
  section: string;
}

export interface ActivityRecommendation {
  id: string;
  dbId?: number;
  category: 'Irrigation' | 'Fertilizer' | 'Pest' | 'General' | string;
  priority: 'HIGH' | 'MEDIUM' | 'LOW';
  action: string;
  reason: string;
  evidence: string;
  confidenceScore: number;
  citations: Citation[];
  requiresOfficerReview: boolean;
  status?: 'PENDING_OFFICER_REVIEW' | 'APPROVED' | 'REJECTED' | 'EXECUTED' | string;
  officerName?: string;
  officerComment?: string;
  reviewedAt?: string;
  executedAt?: string;
}

export interface OfficerRecommendationReviewDto {
  id: number;
  recommendationUid: string;
  cultivationCycleId: number;
  fieldName: string;
  farmerName: string;
  varietyName: string;
  daysAfterSowing: number;
  divisionName: string;
  category: string;
  priority: string;
  action: string;
  reason: string;
  evidence: string;
  confidenceScore: number;
  citations: Citation[];
  status: string;
  officerName?: string;
  officerComment?: string;
  reviewedAt?: string;
  createdAt: string;
}

export interface ReviewRecommendationResponseDto {
  success: boolean;
  message: string;
  executedActivityId?: number;
  recommendationId: number;
}

export interface CropActivityAnalysisOutput {
  fieldOverview: FieldOverview;
  diagnostics: DiagnosticsSummary;
  recommendations: ActivityRecommendation[];
  warnings: string[];
  requiresOfficerReview: boolean;
  executiveSummary: string;
  analyzedAt: string;
}

export interface AiChatResponse {
  answer: string;
  suggestedFollowUps?: string[];
  requiresOfficerReview: boolean;
  answeredAt: string;
}

export const cropAnalysisApi = {
  runAnalysis: async (cycleId: number, question?: string, focusArea: string = 'All'): Promise<CropActivityAnalysisOutput> => {
    const response = await axiosInstance.post(`/cycles/${cycleId}/analysis`, {
      cultivationCycleId: cycleId,
      farmerQuestion: question,
      focusArea
    });
    return response.data;
  },

  getLatestAnalysis: async (cycleId: number): Promise<CropActivityAnalysisOutput> => {
    const response = await axiosInstance.get(`/cycles/${cycleId}/analysis/latest`);
    return response.data;
  },

  askAiQuestion: async (cycleId: number, question: string): Promise<AiChatResponse> => {
    const response = await axiosInstance.post(`/cycles/${cycleId}/ai-chat`, {
      cultivationCycleId: cycleId,
      question
    });
    return response.data;
  },

  // Officer approval queue methods
  getPendingOfficerRecommendations: async (divisionId?: number): Promise<OfficerRecommendationReviewDto[]> => {
    const response = await axiosInstance.get('/recommendations/pending', {
      params: divisionId ? { divisionId } : undefined
    });
    return response.data;
  },

  officerReviewRecommendation: async (id: number, decision: 'Approve' | 'Reject', comment?: string): Promise<OfficerRecommendationReviewDto> => {
    const response = await axiosInstance.post(`/recommendations/${id}/officer-review`, {
      decision,
      comment
    });
    return response.data;
  },

  // Cycle recommendations and Farmer execution
  getCycleRecommendations: async (cycleId: number): Promise<OfficerRecommendationReviewDto[]> => {
    const response = await axiosInstance.get(`/cycles/${cycleId}/recommendations`);
    return response.data;
  },

  executeFarmerRecommendation: async (
    cycleId: number,
    recommendationId: string | number,
    decision: 'Approve' | 'Reject' | 'Revise' | 'Execute' = 'Execute',
    notes?: string
  ): Promise<ReviewRecommendationResponseDto> => {
    const response = await axiosInstance.post(`/cycles/${cycleId}/recommendations/review`, {
      recommendationId: String(recommendationId),
      decision,
      notes
    });
    return response.data;
  }
};
