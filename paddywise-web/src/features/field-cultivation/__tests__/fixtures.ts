import { vi } from 'vitest';
import type {
  CultivationCycle,
  CultivationPlan,
  Division,
  Field,
  PendingPlanSummary,
} from '../types';
import type { AuthContextType } from '../../../context/AuthContext';
import type { UserRole } from '../../../types/auth';

/** Component 1 web test fixtures, shaped like the backend's camelCase DTOs. */

export const divisions: Division[] = [
  { id: 1, name: 'Polonnaruwa', district: 'Polonnaruwa', province: 'North Central' },
  { id: 2, name: 'Ampara', district: 'Ampara', province: 'Eastern' },
];

export function makeField(overrides: Partial<Field> = {}): Field {
  return {
    id: 4,
    name: 'Lower paddy',
    area: 2.5,
    soilType: 'Alluvial',
    irrigationType: 'Major tank',
    latitude: null,
    longitude: null,
    divisionId: 1,
    divisionName: 'Polonnaruwa',
    farmerId: 9,
    farmerName: 'Sunil Perera',
    isActive: true,
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: '2026-09-01T00:00:00Z',
    ...overrides,
  };
}

export function makeCycle(overrides: Partial<CultivationCycle> = {}): CultivationCycle {
  return {
    id: 12,
    fieldId: 4,
    fieldName: 'Lower paddy',
    varietyId: 2,
    varietyName: 'Bg 352',
    durationDays: 105,
    season: 'Maha',
    year: 2026,
    method: 'Transplanting',
    sowingDate: '2026-09-01',
    expectedHarvestDate: '2026-12-15',
    actualHarvestDate: null,
    currentStage: 'Tillering',
    expectedStageToday: 'Tillering',
    status: 'Active',
    notes: null,
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: '2026-09-01T00:00:00Z',
    timeline: [
      { stage: 'Nursery', start: '2026-09-01', end: '2026-09-16' },
      { stage: 'Tillering', start: '2026-09-17', end: '2026-10-12' },
      { stage: 'PanicleInitiation', start: '2026-10-13', end: '2026-10-28' },
      { stage: 'Flowering', start: '2026-10-29', end: '2026-11-13' },
      { stage: 'GrainFilling', start: '2026-11-14', end: '2026-12-09' },
      { stage: 'Harvest', start: '2026-12-10', end: '2026-12-15' },
    ],
    stageLogs: [],
    ...overrides,
  };
}

export function makePlan(overrides: Partial<CultivationPlan> = {}): CultivationPlan {
  return {
    id: 31,
    cycleId: 12,
    objective: 'A good yield without extra fertiliser cost.',
    status: 'PendingOfficerApproval',
    plan: {
      summary: 'Rest of the season, from Tillering.',
      steps: [
        {
          stage: 'Tillering',
          windowStart: '2026-09-20',
          windowEnd: '2026-10-12',
          task: 'Top-dress following the ResourceAnalysisAgent recommendation.',
          rationale: 'Tillering needs nitrogen.',
          category: 'Nutrient',
        },
      ],
      delegations: [
        {
          targetAgent: 'SchedulingValidationAgent',
          instruction: 'Validate the plan.',
          payload: {},
        },
      ],
      assumptions: ['Canal water is available.'],
    },
    validationErrors: [],
    officerComment: null,
    reviewedAt: null,
    createdAt: '2026-09-19T08:00:00Z',
    agentRuns: [
      {
        agentName: 'CultivationPlanningAgent',
        success: true,
        error: null,
        durationMs: 4200,
        toolCalls: [{ tool: 'get_cycle', argsJson: '{"cycleId":12}', resultJson: '{}', at: '2026-09-19T08:00:01Z' }],
        createdAt: '2026-09-19T08:00:05Z',
      },
    ],
    ...overrides,
  };
}

export function makePending(overrides: Partial<PendingPlanSummary> = {}): PendingPlanSummary {
  return {
    planId: 31,
    cycleId: 12,
    farmerName: 'Sunil Perera',
    fieldName: 'Lower paddy',
    divisionName: 'Polonnaruwa',
    season: 'Maha',
    year: 2026,
    objective: 'A good yield without extra fertiliser cost.',
    createdAt: '2026-09-19T08:00:00Z',
    ...overrides,
  };
}

/** What useAuth() returns for a signed-in user of the given role. */
export function authAs(role: UserRole): AuthContextType {
  return {
    user: { name: `Test ${role}`, email: 'test@paddywise.lk', role },
    token: 'test-token',
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    clearAuth: vi.fn(),
  };
}

/** An axios-shaped rejection carrying a backend body, as extractApiErrorMessage reads it. */
export function apiError(status: number, data: unknown) {
  return Object.assign(new Error(`Request failed with status code ${status}`), {
    isAxiosError: true,
    response: { status, data },
  });
}
