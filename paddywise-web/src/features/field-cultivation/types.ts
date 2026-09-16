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

/** CreateCycleRequestDto's Year [Range(2000, 2100)]. */
export const CYCLE_RULES = {
  yearMin: 2000,
  yearMax: 2100,
  notesMaxLength: 1000,
} as const;
