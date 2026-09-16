/**
 * TypeScript mirrors of the backend DTOs in
 * paddywise-backend/DTOs/FieldCultivation/.
 *
 * The API serialises property names as camelCase, enum-typed DTO properties as
 * their C# member name (a string, not the ordinal), and DateOnly as an
 * ISO "YYYY-MM-DD" string.
 */

/** Entities/FieldCultivation/Season.cs */
export type Season = 'Yala' | 'Maha';

/** Entities/FieldCultivation/CultivationMethod.cs */
export type CultivationMethod = 'Broadcasting' | 'Transplanting' | 'DirectSeeding';

/** Entities/FieldCultivation/CycleStatus.cs */
export type CycleStatus = 'Planned' | 'Active' | 'Harvested' | 'Abandoned';

/** Entities/FieldCultivation/GrowthStage.cs — in order. */
export type GrowthStage =
  | 'Nursery'
  | 'Tillering'
  | 'PanicleInitiation'
  | 'Flowering'
  | 'GrainFilling'
  | 'Harvest';

export const SEASONS: readonly Season[] = ['Yala', 'Maha'];

export const CULTIVATION_METHODS: readonly CultivationMethod[] = [
  'Broadcasting',
  'Transplanting',
  'DirectSeeding',
];

export const CYCLE_STATUSES: readonly CycleStatus[] = [
  'Planned',
  'Active',
  'Harvested',
  'Abandoned',
];

export const GROWTH_STAGES: readonly GrowthStage[] = [
  'Nursery',
  'Tillering',
  'PanicleInitiation',
  'Flowering',
  'GrainFilling',
  'Harvest',
];

/** An ISO calendar date, "YYYY-MM-DD" — the wire form of DateOnly. */
export type IsoDate = string;

/** DTOs/FieldCultivation/DivisionResponseDto.cs */
export interface Division {
  id: number;
  name: string;
  district: string;
  province: string;
}

/** DTOs/FieldCultivation/VarietyResponseDto.cs */
export interface Variety {
  id: number;
  name: string;
  durationDays: number;
  ageGroup: string;
  notes: string | null;
}

/** DTOs/FieldCultivation/FieldResponseDto.cs */
export interface Field {
  id: number;
  name: string;
  /** Acres. */
  area: number;
  soilType: string;
  irrigationType: string;
  latitude: number | null;
  longitude: number | null;
  divisionId: number;
  divisionName: string;
  farmerId: number;
  farmerName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

/** DTOs/FieldCultivation/CreateFieldRequestDto.cs */
export interface CreateFieldRequest {
  name: string;
  area: number;
  soilType: string;
  irrigationType: string;
  divisionId: number;
  latitude: number | null;
  longitude: number | null;
}

/** DTOs/FieldCultivation/UpdateFieldRequestDto.cs — a full replace, same shape. */
export type UpdateFieldRequest = CreateFieldRequest;

/** DTOs/FieldCultivation/StageWindowDto.cs — one planned stage of the timeline. */
export interface StageWindow {
  stage: GrowthStage;
  start: IsoDate;
  end: IsoDate;
}

/** DTOs/FieldCultivation/StageLogDto.cs */
export interface StageLog {
  id: number;
  stage: GrowthStage;
  observedOn: IsoDate;
  notes: string | null;
  loggedByUserId: number;
  loggedByUserName: string;
  createdAt: string;
}

/** DTOs/FieldCultivation/CycleResponseDto.cs */
export interface CultivationCycle {
  id: number;
  fieldId: number;
  fieldName: string;
  varietyId: number;
  varietyName: string;
  durationDays: number;
  season: Season;
  year: number;
  method: CultivationMethod;
  sowingDate: IsoDate;
  expectedHarvestDate: IsoDate;
  actualHarvestDate: IsoDate | null;
  /** The stage the farmer has actually reported. */
  currentStage: GrowthStage;
  /** The stage the plan says the cycle should be in today. */
  expectedStageToday: GrowthStage;
  status: CycleStatus;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
  timeline: StageWindow[];
  stageLogs: StageLog[];
}

/**
 * DTOs/FieldCultivation/CreateCycleRequestDto.cs
 * fieldId is omitted: POST /api/fields/{fieldId}/start-cultivation takes the
 * field from the route and overwrites whatever the body carries.
 */
export interface CreateCycleRequest {
  varietyId: number;
  season: Season;
  year: number;
  method: CultivationMethod;
  sowingDate: IsoDate;
  notes: string | null;
}

/** DTOs/FieldCultivation/LogStageRequestDto.cs */
export interface LogStageRequest {
  stage: GrowthStage;
  observedOn: IsoDate;
  notes: string | null;
}

/** DTOs/FieldCultivation/UpdateCycleStatusRequestDto.cs */
export interface UpdateCycleStatusRequest {
  status: CycleStatus;
}

/**
 * The validation attributes on CreateFieldRequestDto / UpdateFieldRequestDto,
 * so the client can reject the same values the server would.
 */
export const FIELD_RULES = {
  nameMaxLength: 100,
  areaMin: 0.01,
  areaMax: 1000,
  soilTypeMaxLength: 50,
  irrigationTypeMaxLength: 50,
  latitudeMin: -90,
  latitudeMax: 90,
  longitudeMin: -180,
  longitudeMax: 180,
} as const;

/**
 * CreateCycleRequestDto's Year [Range(2000, 2100)] and the calendar window
 * CycleService enforces around the sowing date.
 */
export const CYCLE_RULES = {
  yearMin: 2000,
  yearMax: 2100,
  notesMaxLength: 1000,
  maxBackdateDays: 30,
  maxLookaheadDays: 365,
} as const;

/** LogStageRequestDto's Notes [MaxLength(1000)]. */
export const STAGE_LOG_RULES = {
  notesMaxLength: 1000,
} as const;

/** Human labels for the enum member names the API sends. */
export const GROWTH_STAGE_LABELS: Record<GrowthStage, string> = {
  Nursery: 'Nursery',
  Tillering: 'Tillering',
  PanicleInitiation: 'Panicle initiation',
  Flowering: 'Flowering',
  GrainFilling: 'Grain filling',
  Harvest: 'Harvest',
};

export const CULTIVATION_METHOD_LABELS: Record<CultivationMethod, string> = {
  Broadcasting: 'Broadcasting',
  Transplanting: 'Transplanting',
  DirectSeeding: 'Direct seeding',
};

/* ------------------------------------------------- cultivation plan (agent) */

/** Entities/FieldCultivation/PlanStatus.cs — serialised as its member name. */
export type PlanStatus =
  | 'Draft'
  | 'ValidationFailed'
  | 'PendingOfficerApproval'
  | 'Approved'
  | 'Rejected'
  | 'RevisionRequested';

export const PLAN_STATUSES: readonly PlanStatus[] = [
  'Draft',
  'ValidationFailed',
  'PendingOfficerApproval',
  'Approved',
  'Rejected',
  'RevisionRequested',
];

/** Agents/FieldCultivation/CultivationPlanModels.cs — PlanStepCategories.All. */
export type PlanStepCategory =
  | 'LandPrep'
  | 'Water'
  | 'Nutrient'
  | 'Protection'
  | 'Monitoring'
  | 'Harvest';

/** Agents/Shared/DelegatedTask.cs — AgentNames, minus this component's own agent. */
export type DelegationTargetAgent =
  | 'ResourceAnalysisAgent'
  | 'PestDiseaseDiagnosisAgent'
  | 'SchedulingValidationAgent';

/**
 * Agents/FieldCultivation/CultivationPlanModels.cs — PlanStep.
 *
 * `stage` and `category` are the closed sets above for any plan that passed
 * validation. A ValidationFailed plan is stored verbatim as the model produced
 * it, so a value outside the set can still arrive: read both through
 * growthStageLabel / planStepCategoryLabel rather than indexing a Record.
 */
export interface PlanStep {
  stage: GrowthStage;
  windowStart: IsoDate;
  windowEnd: IsoDate;
  task: string;
  rationale: string;
  category: PlanStepCategory;
}

/** Agents/FieldCultivation/CultivationPlanModels.cs — PlanDelegation. */
export interface PlanDelegation {
  targetAgent: DelegationTargetAgent;
  instruction: string;
  /** Opaque: the receiving component owns the shape it expects. */
  payload: Record<string, unknown>;
}

/**
 * Agents/FieldCultivation/CultivationPlanModels.cs — CultivationPlanOutput,
 * the model's own answer, stored verbatim and re-served as `plan`.
 */
export interface CultivationPlanOutput {
  summary: string;
  steps: PlanStep[];
  delegations: PlanDelegation[];
  assumptions: string[];
}

/** Agents/Shared/IAgent.cs — ToolCallRecord. Args and result are JSON strings. */
export interface ToolCallRecord {
  tool: string;
  argsJson: string;
  resultJson: string;
  at: string;
}

/** DTOs/FieldCultivation/CultivationPlanResponseDto.cs — AgentRunSummaryDto. */
export interface AgentRunSummary {
  agentName: string;
  success: boolean;
  error: string | null;
  durationMs: number;
  toolCalls: ToolCallRecord[];
  createdAt: string;
}

/** DTOs/FieldCultivation/CultivationPlanResponseDto.cs */
export interface CultivationPlan {
  id: number;
  cycleId: number;
  objective: string;
  status: PlanStatus;
  /** Null when the agent never produced a parseable plan. */
  plan: CultivationPlanOutput | null;
  /** Why the plan is not usable — empty while it still is. */
  validationErrors: string[];
  officerComment: string | null;
  reviewedAt: string | null;
  createdAt: string;
  agentRuns: AgentRunSummary[];
}

/** DTOs/FieldCultivation/RequestPlanDto.cs */
export interface RequestPlanRequest {
  objective: string;
}

/** RequestPlanDto's Objective [Required] [MaxLength(1000)]. */
export const PLAN_RULES = {
  objectiveMaxLength: 1000,
} as const;

export const PLAN_STATUS_LABELS: Record<PlanStatus, string> = {
  Draft: 'Draft',
  ValidationFailed: 'Validation failed',
  PendingOfficerApproval: 'Awaiting officer approval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  RevisionRequested: 'Revision requested',
};

export const PLAN_STEP_CATEGORY_LABELS: Record<PlanStepCategory, string> = {
  LandPrep: 'Land prep',
  Water: 'Water',
  Nutrient: 'Nutrient',
  Protection: 'Protection',
  Monitoring: 'Monitoring',
  Harvest: 'Harvest',
};

/** Every AgentNames constant, so a run log and a delegation both read plainly. */
export const AGENT_LABELS: Record<string, string> = {
  CultivationPlanningAgent: 'Cultivation Planning Agent',
  ResourceAnalysisAgent: 'Resource Analysis & Action Agent',
  PestDiseaseDiagnosisAgent: 'Pest & Disease Diagnosis Agent',
  SchedulingValidationAgent: 'Scheduling, Validation & Approval Agent',
};

/** The agent's own name, falling back to the raw value for an unknown agent. */
export function agentLabel(agentName: string): string {
  return AGENT_LABELS[agentName] ?? agentName;
}

/** Safe for a stage a ValidationFailed plan invented. */
export function growthStageLabel(stage: GrowthStage): string {
  return GROWTH_STAGE_LABELS[stage] ?? String(stage);
}

/** Safe for a category a ValidationFailed plan invented. */
export function planStepCategoryLabel(category: PlanStepCategory): string {
  return PLAN_STEP_CATEGORY_LABELS[category] ?? String(category);
}

/* --------------------------------------------------- officer approval queue */

/** DTOs/FieldCultivation/PendingPlanSummaryDto.cs — one row of the queue. */
export interface PendingPlanSummary {
  planId: number;
  cycleId: number;
  farmerName: string;
  fieldName: string;
  divisionName: string;
  season: Season;
  year: number;
  objective: string;
  createdAt: string;
}

/** DTOs/FieldCultivation/ReviewPlanDto.cs — PlanReviewDecision, by member name. */
export type PlanReviewDecision = 'Approve' | 'Reject' | 'RequestRevision';

export const PLAN_REVIEW_DECISIONS: readonly PlanReviewDecision[] = [
  'Approve',
  'Reject',
  'RequestRevision',
];

export const PLAN_REVIEW_DECISION_LABELS: Record<PlanReviewDecision, string> = {
  Approve: 'Approve',
  Reject: 'Reject',
  RequestRevision: 'Request revision',
};

/** The status a plan lands in once each decision is taken. */
export const PLAN_REVIEW_OUTCOME: Record<PlanReviewDecision, PlanStatus> = {
  Approve: 'Approved',
  Reject: 'Rejected',
  RequestRevision: 'RevisionRequested',
};

/** DTOs/FieldCultivation/ReviewPlanDto.cs */
export interface ReviewPlanRequest {
  decision: PlanReviewDecision;
  comment: string | null;
}

/**
 * ReviewPlanDto's Comment [MaxLength(1000)], and the rule CultivationPlanService
 * enforces: everything but Approve needs the farmer told why.
 */
export const PLAN_REVIEW_RULES = {
  commentMaxLength: 1000,
} as const;

/** True when the server would refuse this decision without a comment. */
export function reviewNeedsComment(decision: PlanReviewDecision): boolean {
  return decision !== 'Approve';
}
