import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { OfficerRecommendationQueue } from '../components/OfficerRecommendationQueue';
import { cropAnalysisApi, type OfficerRecommendationReviewDto } from '../services/cropAnalysisApi';
// Reused unchanged from Component 1's web suite.
import { apiError } from '../../field-cultivation/__tests__/fixtures';

vi.mock('../services/cropAnalysisApi', () => ({
  cropAnalysisApi: {
    getPendingOfficerRecommendations: vi.fn(),
    officerReviewRecommendation: vi.fn(),
  },
}));

const api = vi.mocked(cropAnalysisApi);

function rec(overrides: Partial<OfficerRecommendationReviewDto> = {}): OfficerRecommendationReviewDto {
  return {
    id: 31,
    recommendationUid: 'a1b2',
    cultivationCycleId: 12,
    fieldName: 'Lower paddy',
    farmerName: 'Sunil Perera',
    varietyName: 'Bg 352',
    daysAfterSowing: 20,
    divisionName: 'Polonnaruwa',
    category: 'Fertilizer',
    priority: 'HIGH',
    action: 'Apply 1st Top Dressing of Urea at 50 kg/ha.',
    reason: 'Tillering needs nitrogen.',
    evidence: 'No Urea logged.',
    confidenceScore: 0.9,
    citations: [],
    status: 'PENDING_OFFICER_REVIEW',
    createdAt: '2026-10-01T05:00:00Z',
    ...overrides,
  };
}

describe('OfficerRecommendationQueue', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows a loading state, then the pending recommendation', async () => {
    api.getPendingOfficerRecommendations.mockResolvedValue([rec()]);
    render(<OfficerRecommendationQueue officerName="Officer Silva" />);

    expect(screen.getByText('Fetching Pending AI Recommendations...')).toBeInTheDocument();
    expect(await screen.findByText('Apply 1st Top Dressing of Urea at 50 kg/ha.')).toBeInTheDocument();
  });

  it('shows the empty state when nothing is pending', async () => {
    api.getPendingOfficerRecommendations.mockResolvedValue([rec({ status: 'APPROVED' })]);
    render(<OfficerRecommendationQueue officerName="Officer Silva" />);

    expect(await screen.findByText('No Recommendations In Queue')).toBeInTheDocument();
  });

  it('shows the server message when the queue cannot be read', async () => {
    api.getPendingOfficerRecommendations.mockRejectedValue(apiError(500, { message: 'Database unavailable.' }));
    render(<OfficerRecommendationQueue officerName="Officer Silva" />);

    expect(await screen.findByText('Database unavailable.')).toBeInTheDocument();
  });

  it.each([
    ['Approve & Send to Farmer', 'Approve', 'Approved by Agricultural Officer.'],
    ['Reject Recommendation', 'Reject', 'Rejected by Agricultural Officer.'],
  ] as const)('"%s" with no comment posts %s and the default comment', async (button, decision, comment) => {
    api.getPendingOfficerRecommendations.mockResolvedValue([rec()]);
    api.officerReviewRecommendation.mockResolvedValue(rec({ status: decision === 'Approve' ? 'APPROVED' : 'REJECTED' }));
    const onReviewed = vi.fn();
    const user = userEvent.setup();
    render(<OfficerRecommendationQueue officerName="Officer Silva" onReviewed={onReviewed} />);

    await user.click(await screen.findByRole('button', { name: new RegExp(button) }));

    await waitFor(() => expect(api.officerReviewRecommendation).toHaveBeenCalledWith(31, decision, comment));
    expect(onReviewed).toHaveBeenCalledTimes(1);
  });

  it('a typed comment is sent instead of the default', async () => {
    api.getPendingOfficerRecommendations.mockResolvedValue([rec()]);
    api.officerReviewRecommendation.mockResolvedValue(rec({ status: 'REJECTED' }));
    const user = userEvent.setup();
    render(<OfficerRecommendationQueue officerName="Officer Silva" />);

    await user.type(
      await screen.findByPlaceholderText('Add technical instructions or remarks for the farmer (optional)...'),
      'Soil test first.'
    );
    await user.click(screen.getByRole('button', { name: /Reject Recommendation/ }));

    await waitFor(() => expect(api.officerReviewRecommendation).toHaveBeenCalledWith(31, 'Reject', 'Soil test first.'));
  });

  it('search narrows the list by farmer, field or action', async () => {
    api.getPendingOfficerRecommendations.mockResolvedValue([
      rec(),
      rec({ id: 32, farmerName: 'Kamala Dissanayake', action: 'Irrigate to establish a 2 - 4 cm standing water depth.' }),
    ]);
    const user = userEvent.setup();
    render(<OfficerRecommendationQueue officerName="Officer Silva" />);
    await screen.findByText('Kamala Dissanayake');

    await user.type(screen.getByPlaceholderText('Search by farmer, field, variety, or action...'), 'irrigate');

    expect(screen.queryByText('Sunil Perera')).not.toBeInTheDocument();
    expect(within(document.body).getByText('Kamala Dissanayake')).toBeInTheDocument();
  });
});
