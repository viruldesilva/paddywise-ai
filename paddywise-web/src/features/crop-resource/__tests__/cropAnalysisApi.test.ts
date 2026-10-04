import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  cropAnalysisApi,
  type CropActivityAnalysisOutput,
  type AiChatResponse,
  type OfficerRecommendationReviewDto,
  type ReviewRecommendationResponseDto
} from '../services/cropAnalysisApi';
import { axiosInstance } from '../../../api/axiosInstance';

describe('Crop Analysis & Advisor API (cropAnalysisApi) Service Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockAnalysisOutput: CropActivityAnalysisOutput = {
    fieldOverview: {
      cycleId: 4,
      fieldName: 'Maha Kumbura (Plot 04)',
      varietyName: 'Bg 352',
      ageGroup: '3.5 Months',
      daysAfterSowing: 52,
      currentStage: 'Tillering',
      season: 'Maha',
      year: 2026,
      divisionName: 'Polonnaruwa Central',
      district: 'Polonnaruwa'
    },
    diagnostics: {
      water: {
        status: 'Adequate',
        latestWaterLevelCm: 5,
        totalIrrigationEvents: 6,
        totalDurationHours: 18,
        daysSinceLastIrrigation: 2,
        assessment: 'Water hydrology is within optimal threshold for tillering stage.'
      },
      fertilizer: {
        status: 'DueSoon',
        totalUreaKgPerHa: 100,
        totalTspKgPerHa: 55,
        totalMopKgPerHa: 30,
        applicationsCount: 2,
        daysSinceLastFertilizer: 14,
        splitCompliance: 'Good',
        assessment: 'Top dressing II due within 3 days.'
      },
      pest: {
        status: 'Safe',
        treatmentsCount: 1,
        productsUsed: [],
        targetPests: [],
        daysSinceLastTreatment: 20,
        bannedChemicalDetected: false,
        assessment: 'No active pest infestation logged.'
      },
      other: {
        activitiesCount: 2,
        activityTypes: ['Weeding'],
        weedingStatus: 'Completed',
        assessment: 'Manual weeding completed at 21 DAS.'
      }
    },
    recommendations: [
      {
        id: 'rec-001',
        dbId: 101,
        category: 'Fertilizer',
        priority: 'HIGH',
        action: 'Apply Top Dressing II: Urea at 50 kg/ha',
        reason: 'Bg 352 at 52 DAS is approaching panicle initiation; nitrogen replenishment is critical.',
        evidence: 'Last fertilizer logged 14 days ago. RRDI guidelines require 2nd top dressing at 7-8 weeks.',
        confidenceScore: 0.94,
        citations: [
          { document: 'DOA Paddy Fertilizer Guideline', section: 'Split Application Schedule for 3.5M Varieties' }
        ],
        requiresOfficerReview: false,
        status: 'APPROVED'
      }
    ],
    warnings: [],
    requiresOfficerReview: false,
    executiveSummary: 'Field hydrology and vegetative development are optimal. Top dressing 2 recommended.',
    analyzedAt: '2026-10-04T10:00:00Z'
  };

  describe('runAnalysis', () => {
    it('calls POST /cycles/{cycleId}/analysis with question and focusArea', async () => {
      const postSpy = vi.spyOn(axiosInstance, 'post').mockResolvedValueOnce({
        data: mockAnalysisOutput
      });

      const result = await cropAnalysisApi.runAnalysis(4, 'Check fertilizer status', 'Fertilizer');

      expect(postSpy).toHaveBeenCalledWith('/cycles/4/analysis', {
        cultivationCycleId: 4,
        farmerQuestion: 'Check fertilizer status',
        focusArea: 'Fertilizer'
      });
      expect(result.fieldOverview.varietyName).toBe('Bg 352');
      expect(result.recommendations).toHaveLength(1);
    });

    it('uses default focusArea="All" when omitted', async () => {
      const postSpy = vi.spyOn(axiosInstance, 'post').mockResolvedValueOnce({
        data: mockAnalysisOutput
      });

      await cropAnalysisApi.runAnalysis(4);

      expect(postSpy).toHaveBeenCalledWith('/cycles/4/analysis', {
        cultivationCycleId: 4,
        farmerQuestion: undefined,
        focusArea: 'All'
      });
    });
  });

  describe('getLatestAnalysis', () => {
    it('calls GET /cycles/{cycleId}/analysis/latest', async () => {
      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
        data: mockAnalysisOutput
      });

      const result = await cropAnalysisApi.getLatestAnalysis(4);

      expect(getSpy).toHaveBeenCalledWith('/cycles/4/analysis/latest');
      expect(result.diagnostics.water.status).toBe('Adequate');
    });
  });

  describe('askAiQuestion', () => {
    it('calls POST /cycles/{cycleId}/ai-chat with user question', async () => {
      const mockChatResponse: AiChatResponse = {
        answer: 'For Bg 352 at 52 DAS, Top Dressing II is due within the next 3 days.',
        suggestedFollowUps: ['How much MOP is required?', 'Should I drain the field before applying?'],
        requiresOfficerReview: false,
        answeredAt: '2026-10-04T10:01:00Z'
      };

      const postSpy = vi.spyOn(axiosInstance, 'post').mockResolvedValueOnce({
        data: mockChatResponse
      });

      const result = await cropAnalysisApi.askAiQuestion(4, 'When is my next fertilizer split due?');

      expect(postSpy).toHaveBeenCalledWith('/cycles/4/ai-chat', {
        cultivationCycleId: 4,
        question: 'When is my next fertilizer split due?'
      });
      expect(result.answer).toContain('Bg 352');
      expect(result.suggestedFollowUps).toHaveLength(2);
    });
  });

  describe('getPendingOfficerRecommendations', () => {
    it('calls GET /recommendations/pending without divisionId when not provided', async () => {
      const mockPending: OfficerRecommendationReviewDto[] = [
        {
          id: 201,
          recommendationUid: 'rec-002',
          cultivationCycleId: 4,
          fieldName: 'Maha Kumbura',
          farmerName: 'Sunil Bandara',
          varietyName: 'Bg 352',
          daysAfterSowing: 52,
          divisionName: 'Polonnaruwa Central',
          category: 'Pest',
          priority: 'HIGH',
          action: 'Apply Thiamethoxam 25% WG',
          reason: 'BPH threshold exceeded',
          evidence: 'Hopper count > 10 per hill observed',
          confidenceScore: 0.91,
          citations: [{ document: 'IPM Sri Lanka', section: 'Planthopper Management' }],
          status: 'PENDING_OFFICER_REVIEW',
          createdAt: '2026-10-04T08:00:00Z'
        }
      ];

      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({
        data: mockPending
      });

      const result = await cropAnalysisApi.getPendingOfficerRecommendations();

      expect(getSpy).toHaveBeenCalledWith('/recommendations/pending', { params: undefined });
      expect(result).toHaveLength(1);
      expect(result[0].status).toBe('PENDING_OFFICER_REVIEW');
    });

    it('calls GET /recommendations/pending with divisionId param when provided', async () => {
      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({ data: [] });

      await cropAnalysisApi.getPendingOfficerRecommendations(5);

      expect(getSpy).toHaveBeenCalledWith('/recommendations/pending', {
        params: { divisionId: 5 }
      });
    });
  });

  describe('officerReviewRecommendation', () => {
    it('calls POST /recommendations/{id}/officer-review with decision and comment', async () => {
      const mockReviewed: OfficerRecommendationReviewDto = {
        id: 201,
        recommendationUid: 'rec-002',
        cultivationCycleId: 4,
        fieldName: 'Maha Kumbura',
        farmerName: 'Sunil Bandara',
        varietyName: 'Bg 352',
        daysAfterSowing: 52,
        divisionName: 'Polonnaruwa Central',
        category: 'Pest',
        priority: 'HIGH',
        action: 'Apply Thiamethoxam 25% WG',
        reason: 'BPH threshold exceeded',
        evidence: 'Hopper count > 10 per hill observed',
        confidenceScore: 0.91,
        citations: [],
        status: 'APPROVED',
        officerName: 'Dr. Nilmini Perera',
        officerComment: 'Approved as per DOA IPM threshold.',
        reviewedAt: '2026-10-04T10:30:00Z',
        createdAt: '2026-10-04T08:00:00Z'
      };

      const postSpy = vi.spyOn(axiosInstance, 'post').mockResolvedValueOnce({
        data: mockReviewed
      });

      const result = await cropAnalysisApi.officerReviewRecommendation(
        201,
        'Approve',
        'Approved as per DOA IPM threshold.'
      );

      expect(postSpy).toHaveBeenCalledWith('/recommendations/201/officer-review', {
        decision: 'Approve',
        comment: 'Approved as per DOA IPM threshold.'
      });
      expect(result.status).toBe('APPROVED');
    });
  });

  describe('getCycleRecommendations', () => {
    it('calls GET /cycles/{cycleId}/recommendations', async () => {
      const getSpy = vi.spyOn(axiosInstance, 'get').mockResolvedValueOnce({ data: [] });

      await cropAnalysisApi.getCycleRecommendations(4);

      expect(getSpy).toHaveBeenCalledWith('/cycles/4/recommendations');
    });
  });

  describe('executeFarmerRecommendation', () => {
    it('calls POST /cycles/{cycleId}/recommendations/review with recommendationId and decision', async () => {
      const mockExecResponse: ReviewRecommendationResponseDto = {
        success: true,
        message: 'Recommendation executed and recorded in field log.',
        executedActivityId: 305,
        recommendationId: 101
      };

      const postSpy = vi.spyOn(axiosInstance, 'post').mockResolvedValueOnce({
        data: mockExecResponse
      });

      const result = await cropAnalysisApi.executeFarmerRecommendation(4, 101, 'Execute', 'Executed this morning');

      expect(postSpy).toHaveBeenCalledWith('/cycles/4/recommendations/review', {
        recommendationId: '101',
        decision: 'Execute',
        notes: 'Executed this morning'
      });
      expect(result.success).toBe(true);
      expect(result.executedActivityId).toBe(305);
    });
  });
});
