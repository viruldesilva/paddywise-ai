import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { OfficerRecommendationQueue } from '../components/OfficerRecommendationQueue';
import { cropAnalysisApi, type OfficerRecommendationReviewDto } from '../services/cropAnalysisApi';

describe('OfficerRecommendationQueue Component Tests', () => {
  const sampleRecommendations: OfficerRecommendationReviewDto[] = [
    {
      id: 201,
      recommendationUid: 'rec-001',
      cultivationCycleId: 4,
      fieldName: 'Maha Kumbura (Plot 04)',
      farmerName: 'Sunil Bandara',
      varietyName: 'Bg 352',
      daysAfterSowing: 52,
      divisionName: 'Polonnaruwa Central',
      category: 'Fertilizer',
      priority: 'HIGH',
      action: 'Apply Top Dressing II: Urea at 50 kg/ha',
      reason: 'Panicle initiation stage approaching.',
      evidence: 'Last fertilizer logged 14 days ago.',
      confidenceScore: 0.94,
      citations: [{ document: 'DOA Guideline', section: 'Split Application' }],
      status: 'PENDING_OFFICER_REVIEW',
      createdAt: '2026-10-04T08:00:00Z'
    },
    {
      id: 202,
      recommendationUid: 'rec-002',
      cultivationCycleId: 5,
      fieldName: 'Goda Kumbura (Plot 02)',
      farmerName: 'Kamal Perera',
      varietyName: 'At 362',
      daysAfterSowing: 30,
      divisionName: 'Polonnaruwa Central',
      category: 'Irrigation',
      priority: 'MEDIUM',
      action: 'Maintain saturated condition for tillering',
      reason: 'Water depth too low during tillering.',
      evidence: 'Last water level recorded 1 cm.',
      confidenceScore: 0.88,
      citations: [],
      status: 'PENDING_OFFICER_REVIEW',
      createdAt: '2026-10-04T08:30:00Z'
    }
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering Pending Recommendations Queue', () => {
    it('fetches and displays pending recommendations with farmer and plot details', async () => {
      vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockResolvedValueOnce(sampleRecommendations);

      render(<OfficerRecommendationQueue officerName="Dr. Nilmini Perera" />);

      expect(screen.getByText('Fetching Pending AI Recommendations...')).toBeInTheDocument();

      expect(await screen.findByText('Agricultural Officer Recommendation Review')).toBeInTheDocument();
      expect(screen.getByText('Sunil Bandara')).toBeInTheDocument();
      expect(screen.getByText('Maha Kumbura (Plot 04)')).toBeInTheDocument();
      expect(screen.getByText('Kamal Perera')).toBeInTheDocument();
      expect(screen.getByText('Apply Top Dressing II: Urea at 50 kg/ha')).toBeInTheDocument();
    });

    it('shows empty queue message when no pending recommendations exist', async () => {
      vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockResolvedValueOnce([]);

      render(<OfficerRecommendationQueue officerName="Dr. Nilmini Perera" />);

      expect(await screen.findByText('No Recommendations In Queue')).toBeInTheDocument();
    });

    it('shows error banner when API fails', async () => {
      vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockRejectedValueOnce(
        new Error('Failed to load pending queue')
      );

      render(<OfficerRecommendationQueue officerName="Dr. Nilmini Perera" />);

      expect(await screen.findByText(/Failed to load pending queue/i)).toBeInTheDocument();
    });
  });

  describe('Officer Review Actions: Approve & Reject', () => {
    it('approves recommendation with technical comment and updates item status', async () => {
      vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockResolvedValueOnce(sampleRecommendations);
      const approveSpy = vi.spyOn(cropAnalysisApi, 'officerReviewRecommendation').mockResolvedValueOnce({
        ...sampleRecommendations[0],
        status: 'APPROVED',
        officerName: 'Dr. Nilmini Perera',
        officerComment: 'Approved according to Sri Lanka DOA handbook',
        reviewedAt: '2026-10-04T10:00:00Z'
      });

      render(<OfficerRecommendationQueue officerName="Dr. Nilmini Perera" />);

      expect(await screen.findByText('Sunil Bandara')).toBeInTheDocument();

      // Click preset chip "+ Approved as per DOA"
      const presetChip = screen.getAllByRole('button', { name: /\+ Approved as per DOA/i })[0];
      fireEvent.click(presetChip);

      // Click "Approve & Send to Farmer" button
      const approveBtn = screen.getAllByRole('button', { name: /Approve & Send to Farmer/i })[0];
      fireEvent.click(approveBtn);

      await waitFor(() => {
        expect(approveSpy).toHaveBeenCalledWith(
          201,
          'Approve',
          'Approved according to Sri Lanka DOA handbook'
        );
      });
    });

    it('rejects recommendation with comment', async () => {
      vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockResolvedValueOnce(sampleRecommendations);
      const rejectSpy = vi.spyOn(cropAnalysisApi, 'officerReviewRecommendation').mockResolvedValueOnce({
        ...sampleRecommendations[0],
        status: 'REJECTED',
        officerName: 'Dr. Nilmini Perera',
        officerComment: 'Heavy rains forecasted; do not broadcast today.',
        reviewedAt: '2026-10-04T10:00:00Z'
      });

      render(<OfficerRecommendationQueue officerName="Dr. Nilmini Perera" />);

      expect(await screen.findByText('Sunil Bandara')).toBeInTheDocument();

      // Enter comment into textarea
      const commentInput = screen.getAllByPlaceholderText(/Add technical instructions or remarks/i)[0];
      fireEvent.change(commentInput, {
        target: { value: 'Heavy rains forecasted; do not broadcast today.' }
      });

      // Click "Reject Recommendation"
      const rejectBtn = screen.getAllByRole('button', { name: /Reject Recommendation/i })[0];
      fireEvent.click(rejectBtn);

      await waitFor(() => {
        expect(rejectSpy).toHaveBeenCalledWith(
          201,
          'Reject',
          'Heavy rains forecasted; do not broadcast today.'
        );
      });
    });
  });

  describe('Search and Category Filtering', () => {
    it('filters recommendations by search query', async () => {
      vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockResolvedValueOnce(sampleRecommendations);

      render(<OfficerRecommendationQueue officerName="Dr. Nilmini Perera" />);

      expect(await screen.findByText('Sunil Bandara')).toBeInTheDocument();
      expect(screen.getByText('Kamal Perera')).toBeInTheDocument();

      const searchInput = screen.getByPlaceholderText(/Search by farmer, field, variety/i);
      fireEvent.change(searchInput, { target: { value: 'Kamal' } });

      expect(screen.queryByText('Sunil Bandara')).not.toBeInTheDocument();
      expect(screen.getByText('Kamal Perera')).toBeInTheDocument();
    });
  });
});
