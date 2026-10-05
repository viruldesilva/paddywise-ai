export interface AssignedDivision {
  id: number;
  name: string;
  centre: string;
  district: string;
  province: string;
}

export interface PendingTreatmentPlan {
  id: number;
  farmerName: string;
  gnDivision: string;
  symptom: string;
  aiDiagnosis: string;
  confidence: number;
  proposedPlan: string;
  createdAt: string;
}

export interface OfficerDashboardData {
  assignedDivision: AssignedDivision;
  pendingApprovalCount: number;
  approvedTreatmentsThisSeason: number;
  cultivationSeason: string;
  registeredFarmerCount: number;
  gnDivisionCount: number | null;
  pendingQueue: PendingTreatmentPlan[];
}
