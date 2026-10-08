import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PlanReviewForm } from '../components/PlanReviewForm';
import { reviewPlan } from '../services/fieldApi';
import { getPlanDraftRevisionComment } from '../../reporting-approval/services/reviewsApi';
import { apiError, makePlan } from './fixtures';

vi.mock('../services/fieldApi', () => ({ reviewPlan: vi.fn() }));
vi.mock('../../reporting-approval/services/reviewsApi', () => ({
  getPlanDraftRevisionComment: vi.fn(),
}));

const mockedReview = vi.mocked(reviewPlan);
const mockedDraft = vi.mocked(getPlanDraftRevisionComment);

function renderForm() {
  const onReviewed = vi.fn();
  const onCancel = vi.fn();
  render(<PlanReviewForm planId={31} onReviewed={onReviewed} onCancel={onCancel} />);
  return { onReviewed, onCancel, user: userEvent.setup() };
}

describe('PlanReviewForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('Approve needs no comment and posts decision Approve with a null comment', async () => {
    const reviewed = makePlan({ status: 'Approved' });
    mockedReview.mockResolvedValue(reviewed);
    const { onReviewed, user } = renderForm();

    expect(screen.getByLabelText('Comment (optional)')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /submit review/i }));

    await waitFor(() => expect(mockedReview).toHaveBeenCalledWith(31, { decision: 'Approve', comment: null }));
    expect(onReviewed).toHaveBeenCalledWith(reviewed, 'Approve');
  });

  it.each([
    ['Reject', /^Reject/, 'A comment is required when rejecting a plan.'],
    ['RequestRevision', /^Request revision/, 'A comment is required when asking for a revision.'],
  ])('%s without a comment shows the rule and never calls the API', async (_decision, label, message) => {
    const { user } = renderForm();

    await user.click(screen.getByRole('radio', { name: label }));
    await user.type(screen.getByRole('textbox'), '   ');
    await user.click(screen.getByRole('button', { name: /submit review/i }));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole('textbox')).toHaveAttribute('aria-invalid', 'true');
    expect(mockedReview).not.toHaveBeenCalled();
  });

  it.each([
    ['Reject', /^Reject/],
    ['RequestRevision', /^Request revision/],
  ] as const)('%s with a comment posts that decision and the trimmed comment', async (decision, label) => {
    mockedReview.mockResolvedValue(makePlan({ status: decision === 'Reject' ? 'Rejected' : 'RevisionRequested' }));
    const { onReviewed, user } = renderForm();

    await user.click(screen.getByRole('radio', { name: label }));
    await user.type(screen.getByRole('textbox'), '  Add a water step at flowering.  ');
    await user.click(screen.getByRole('button', { name: /submit review/i }));

    await waitFor(() =>
      expect(mockedReview).toHaveBeenCalledWith(31, { decision, comment: 'Add a water step at flowering.' })
    );
    expect(onReviewed).toHaveBeenCalledWith(expect.anything(), decision);
  });

  it('switching decision clears a stale comment warning', async () => {
    const { user } = renderForm();
    await user.click(screen.getByRole('radio', { name: /^Reject/ }));
    await user.click(screen.getByRole('button', { name: /submit review/i }));
    expect(screen.getByText('A comment is required when rejecting a plan.')).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: /^Approve/ }));

    expect(screen.queryByText('A comment is required when rejecting a plan.')).not.toBeInTheDocument();
  });

  it('a server { message } is shown and the form stays usable', async () => {
    mockedReview.mockRejectedValue(
      apiError(400, { message: 'This plan is Approved — only a plan awaiting officer approval can be reviewed.' })
    );
    const { onReviewed, user } = renderForm();

    await user.click(screen.getByRole('button', { name: /submit review/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This plan is Approved — only a plan awaiting officer approval can be reviewed.'
    );
    expect(onReviewed).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: /submit review/i })).toBeEnabled();
  });

  it('the comment box is capped at 1000 characters', () => {
    renderForm();
    expect(screen.getByRole('textbox')).toHaveAttribute('maxLength', '1000');
  });

  it('Draft with AI fills the comment from Component 4', async () => {
    mockedDraft.mockResolvedValue('Please move the top-dressing into the Tillering window.');
    const { user } = renderForm();

    await user.click(screen.getByRole('button', { name: /draft with ai/i }));

    await waitFor(() =>
      expect(screen.getByRole('textbox')).toHaveValue('Please move the top-dressing into the Tillering window.')
    );
    expect(mockedDraft).toHaveBeenCalledWith(31);
  });

  it('Cancel calls onCancel', async () => {
    const { onCancel, user } = renderForm();
    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });
});
