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
  category: 'Irrigation' | 'Fertilizer' | 'Pest' | 'General' | string;
  priority: 'HIGH' | 'MEDIUM' | 'LOW';
  action: string;
  reason: string;
  evidence: string;
  confidenceScore: number;
  citations: Citation[];
  requiresOfficerReview: boolean;
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
  }
};
