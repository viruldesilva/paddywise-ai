import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { AiAdvisorPanel } from '../components/AiAdvisorPanel';
import { cropAnalysisApi, type CropActivityAnalysisOutput } from '../services/cropAnalysisApi';
import type { CultivationCycle } from '../../field-cultivation/types';

describe('AiAdvisorPanel Component Tests', () => {
  const sampleCycles: CultivationCycle[] = [
    {
      id: 4,
      fieldId: 1,
      fieldName: 'Maha Kumbura (Plot 04)',
      varietyId: 2,
      varietyName: 'Bg 352',
      durationDays: 105,
      season: 'Maha',
      year: 2026,
      method: 'Broadcasting',
      sowingDate: '2026-09-01',
      expectedHarvestDate: '2026-12-15',
      actualHarvestDate: null,
      currentStage: 'Tillering',
      expectedStageToday: 'Tillering',
      status: 'Active',
      notes: null,
      createdAt: '2026-09-01T00:00:00Z',
      updatedAt: '2026-09-01T00:00:00Z',
      timeline: [],
      stageLogs: []
    }
  ];

  const mockAnalysisData: CropActivityAnalysisOutput = {
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
        productsUsed: ['Chlorantraniliprole'],
        targetPests: ['Stem Borer'],
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
        reason: 'Bg 352 at 52 DAS is approaching panicle initiation.',
        evidence: 'Last fertilizer logged 14 days ago.',
        confidenceScore: 0.94,
        citations: [
          { document: 'DOA Paddy Fertilizer Guideline', section: 'Split Application Schedule for 3.5M' }
        ],
        requiresOfficerReview: false,
        status: 'APPROVED',
        officerName: 'Dr. Nilmini Perera',
        officerComment: 'Approved as per DOA guidelines.'
      }
    ],
    warnings: ['Avoid water drainage immediately after fertilizer application.'],
    requiresOfficerReview: false,
    executiveSummary: 'Field hydrology and vegetative development are optimal. Top dressing 2 recommended within 3 days.',
    analyzedAt: '2026-10-04T10:00:00Z'
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Initial Rendering and Analysis Execution', () => {
    it('executes analysis on mount and displays executive summary and diagnostics', async () => {
      const runSpy = vi.spyOn(cropAnalysisApi, 'runAnalysis').mockResolvedValueOnce(mockAnalysisData);

      render(
        <AiAdvisorPanel
          cycles={sampleCycles}
          selectedCycleId={4}
        />
      );

      expect(runSpy).toHaveBeenCalledWith(4);

      // Hero banner
      expect(screen.getByText('AGENTIC AI ADVISOR')).toBeInTheDocument();
      expect(screen.getByText('Crop Activity & Resource Analysis')).toBeInTheDocument();

      // Executive Summary
      expect(await screen.findByText('Agronomic Executive Summary')).toBeInTheDocument();
      expect(screen.getByText(/Field hydrology and vegetative development are optimal/i)).toBeInTheDocument();

      // Diagnostics Cards
      expect(screen.getByText('Water Management')).toBeInTheDocument();
      expect(screen.getByText('Nutrient Balance')).toBeInTheDocument();
      expect(screen.getByText('Crop Protection')).toBeInTheDocument();
      expect(screen.getByText('Field Operations')).toBeInTheDocument();

      // Metrics in diagnostics
      expect(screen.getByText('5')).toBeInTheDocument(); // 5 cm water depth
      expect(screen.getByText('100')).toBeInTheDocument(); // 100 kg/ha urea
    });

    it('shows empty state when no cultivation cycles are present', () => {
      render(
        <AiAdvisorPanel
          cycles={[]}
          selectedCycleId="all"
        />
      );

      expect(screen.getByText('No Cultivation Cycles Available')).toBeInTheDocument();
    });

    it('shows error banner when AI analysis fails', async () => {
      vi.spyOn(cropAnalysisApi, 'runAnalysis').mockRejectedValueOnce(
        new Error('Agronomic AI engine unavailable')
      );

      render(
        <AiAdvisorPanel
          cycles={sampleCycles}
          selectedCycleId={4}
        />
      );

      expect(await screen.findByText(/Agronomic AI engine unavailable/i)).toBeInTheDocument();
    });
  });

  describe('Recommendations & Farmer Execution Flow', () => {
    it('renders recommendation with confidence score, officer approval note, and execute button', async () => {
      vi.spyOn(cropAnalysisApi, 'runAnalysis').mockResolvedValueOnce(mockAnalysisData);

      render(
        <AiAdvisorPanel
          cycles={sampleCycles}
          selectedCycleId={4}
        />
      );

      expect(await screen.findByText('Apply Top Dressing II: Urea at 50 kg/ha')).toBeInTheDocument();
      expect(screen.getByText(/94% Confidence/i)).toBeInTheDocument();
      expect(screen.getByText('HIGH PRIORITY')).toBeInTheDocument();
      expect(screen.getByText(/Verified & Approved by Agricultural Officer/i)).toBeInTheDocument();

      // Execute button
      const executeBtn = screen.getByRole('button', { name: /Approve & Execute into Field Ledger/i });
      expect(executeBtn).toBeInTheDocument();
    });

    it('clicking Execute button calls executeFarmerRecommendation and updates status', async () => {
      vi.spyOn(cropAnalysisApi, 'runAnalysis').mockResolvedValueOnce(mockAnalysisData);
      const executeSpy = vi.spyOn(cropAnalysisApi, 'executeFarmerRecommendation').mockResolvedValueOnce({
        success: true,
        message: 'Activity successfully executed and recorded in field activity log!',
        recommendationId: 101,
        executedActivityId: 45
      });

      render(
        <AiAdvisorPanel
          cycles={sampleCycles}
          selectedCycleId={4}
        />
      );

      const executeBtn = await screen.findByRole('button', { name: /Approve & Execute into Field Ledger/i });
      fireEvent.click(executeBtn);

      await waitFor(() => {
        expect(executeSpy).toHaveBeenCalledWith(4, 101, 'Execute');
      });

      expect(
        await screen.findByText('Activity successfully executed and recorded in field activity log!')
      ).toBeInTheDocument();
    });
  });

  describe('Ask AI Chat Console', () => {
    it('sends question on chip click and renders user message and AI response', async () => {
      vi.spyOn(cropAnalysisApi, 'runAnalysis').mockResolvedValueOnce(mockAnalysisData);
      const chatSpy = vi.spyOn(cropAnalysisApi, 'askAiQuestion').mockResolvedValueOnce({
        answer: 'For Bg 352 at 52 DAS, Top Dressing II is due within the next 3 days.',
        suggestedFollowUps: ['How much MOP is required?'],
        requiresOfficerReview: false,
        answeredAt: '2026-10-04T10:05:00Z'
      });

      render(
        <AiAdvisorPanel
          cycles={sampleCycles}
          selectedCycleId={4}
        />
      );

      await screen.findByText('Agronomic Executive Summary');

      // Click suggested question chip
      const chip = screen.getByRole('button', { name: /Next fertilizer split\?/i });
      fireEvent.click(chip);

      await waitFor(() => {
        expect(chatSpy).toHaveBeenCalledWith(4, 'When should I apply my next fertilizer split?');
      });

      // Verify conversation renders
      expect(screen.getByText('When should I apply my next fertilizer split?')).toBeInTheDocument();
      expect(
        await screen.findByText('For Bg 352 at 52 DAS, Top Dressing II is due within the next 3 days.')
      ).toBeInTheDocument();
    });

    it('submits typed question using the input field and send button', async () => {
      vi.spyOn(cropAnalysisApi, 'runAnalysis').mockResolvedValueOnce(mockAnalysisData);
      const chatSpy = vi.spyOn(cropAnalysisApi, 'askAiQuestion').mockResolvedValueOnce({
        answer: 'Your current 5cm water level is safe for tillering.',
        requiresOfficerReview: false,
        answeredAt: '2026-10-04T10:06:00Z'
      });

      render(
        <AiAdvisorPanel
          cycles={sampleCycles}
          selectedCycleId={4}
        />
      );

      await screen.findByText('Agronomic Executive Summary');

      const chatInput = screen.getByPlaceholderText(/Ask e\.g\. 'Can I apply urea tomorrow\?'/i);
      fireEvent.change(chatInput, { target: { value: 'Is 5cm water depth safe?' } });

      const sendBtn = screen.getByRole('button', { name: '' }); // Send icon button in chat
      fireEvent.click(sendBtn);

      await waitFor(() => {
        expect(chatSpy).toHaveBeenCalledWith(4, 'Is 5cm water depth safe?');
      });

      expect(await screen.findByText('Your current 5cm water level is safe for tillering.')).toBeInTheDocument();
    });
  });
});
