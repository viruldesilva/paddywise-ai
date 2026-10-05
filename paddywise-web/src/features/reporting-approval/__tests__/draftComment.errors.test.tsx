import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PlanReviewForm } from '../../field-cultivation/components/PlanReviewForm';
import { ReportReviewForm } from '../../pest-disease/components/ReportReviewForm';
import { getPlanDraftRevisionComment, getReportDraftRevisionComment } from '../services/reviewsApi';
// Reused unchanged from Component 1's web suite.
import { apiError } from '../../field-cultivation/__tests__/fixtures';

vi.mock('../services/reviewsApi', () => ({
  getPlanDraftRevisionComment: vi.fn(),
  getReportDraftRevisionComment: vi.fn(),
}));
vi.mock('../../field-cultivation/services/fieldApi', () => ({ reviewPlan: vi.fn() }));
vi.mock('../../pest-disease/services/pestDiseaseApi', () => ({ reviewPestDiseaseReport: vi.fn() }));

const planDraft = vi.mocked(getPlanDraftRevisionComment);
const reportDraft = vi.mocked(getReportDraftRevisionComment);

/**
 * The two officer review forms that call Component 4's draft endpoints. Virul's
 * reviewsApi.test.ts checks the URLs; these check what the officer sees on success and on
 * a refused or failed call.
 */
describe('AI draft comments in the officer review forms', () => {
  beforeEach(() => vi.clearAllMocks());

  it('plan form: a draft fills the comment box', async () => {
    planDraft.mockResolvedValue('Move the top-dressing into Tillering.');
    const user = userEvent.setup();
    render(<PlanReviewForm planId={31} onReviewed={vi.fn()} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: /draft with ai/i }));

    await waitFor(() => expect(screen.getByRole('textbox')).toHaveValue('Move the top-dressing into Tillering.'));
  });

  it.each([
    [apiError(403, { message: 'Only agricultural officers can draft comments.' }), 'Only agricultural officers can draft comments.'],
    [new Error('Network Error'), 'Network Error'], // extractApiErrorMessage shows a plain Error's own message
  ])('plan form: a failed draft shows the reason and leaves the comment empty (%#)', async (failure, shown) => {
    planDraft.mockRejectedValue(failure);
    const user = userEvent.setup();
    render(<PlanReviewForm planId={31} onReviewed={vi.fn()} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: /draft with ai/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(shown);
    expect(screen.getByRole('textbox')).toHaveValue('');
  });

  it('report form: a draft fills the comment box, a 403 shows its message', async () => {
    reportDraft.mockResolvedValueOnce('Ask for a closer photo of the lesions.');
    const user = userEvent.setup();
    render(<ReportReviewForm reportId={50} onReviewed={vi.fn()} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: /draft with ai/i }));
    await waitFor(() => expect(screen.getByRole('textbox')).toHaveValue('Ask for a closer photo of the lesions.'));
    expect(reportDraft).toHaveBeenCalledWith(50);

    reportDraft.mockRejectedValueOnce(apiError(403, { message: 'Forbidden for this role.' }));
    await user.click(screen.getByRole('button', { name: /draft with ai/i }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Forbidden for this role.');
  });
});
