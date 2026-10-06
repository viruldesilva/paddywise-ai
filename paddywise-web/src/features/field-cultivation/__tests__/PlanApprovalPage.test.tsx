import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import PlanApprovalPage from '../pages/PlanApprovalPage';
import { getDivisions, getPendingPlans, getPlan, reviewPlan } from '../services/fieldApi';
import { useAuth } from '../../../hooks/useAuth';
import { apiError, authAs, divisions, makePending, makePlan } from './fixtures';

vi.mock('../services/fieldApi', () => ({
  getDivisions: vi.fn(),
  getPendingPlans: vi.fn(),
  getPlan: vi.fn(),
  reviewPlan: vi.fn(),
}));
vi.mock('../../../hooks/useAuth', () => ({ useAuth: vi.fn() }));
vi.mock('../../../components/Sidebar', () => ({ Sidebar: () => null }));
vi.mock('../../reporting-approval/services/reviewsApi', () => ({ getPlanDraftRevisionComment: vi.fn() }));

const api = {
  getDivisions: vi.mocked(getDivisions),
  getPendingPlans: vi.mocked(getPendingPlans),
  getPlan: vi.mocked(getPlan),
  reviewPlan: vi.mocked(reviewPlan),
};

function renderPage() {
  render(
    <MemoryRouter>
      <PlanApprovalPage />
    </MemoryRouter>
  );
  return userEvent.setup();
}

describe('PlanApprovalPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuth).mockReturnValue(authAs('AgriculturalOfficer'));
    api.getDivisions.mockResolvedValue(divisions);
  });

  it('shows a loading state while the queue is read', async () => {
    api.getPendingPlans.mockReturnValue(new Promise(() => {}));
    renderPage();

    expect(screen.getByText('Loading the approval queue…')).toBeInTheDocument();
  });

  it('shows the empty state when nothing is waiting', async () => {
    api.getPendingPlans.mockResolvedValue([]);
    renderPage();

    expect(await screen.findByText('Nothing waiting on you')).toBeInTheDocument();
    expect(screen.getByText('No plan is awaiting approval right now.')).toBeInTheDocument();
  });

  it('shows the server message when the queue cannot be read', async () => {
    api.getPendingPlans.mockRejectedValue(apiError(403, { message: 'Forbidden for this role.' }));
    renderPage();

    expect(await screen.findByRole('alert')).toHaveTextContent('Forbidden for this role.');
  });

  it('lists every pending plan, newest first, and the sort toggles', async () => {
    api.getPendingPlans.mockResolvedValue([
      makePending({ planId: 1, farmerName: 'Older Farmer', createdAt: '2026-09-18T08:00:00Z' }),
      makePending({ planId: 2, farmerName: 'Newer Farmer', createdAt: '2026-09-20T08:00:00Z' }),
    ]);
    const user = renderPage();

    await screen.findByText('Newer Farmer');
    const names = () => screen.getAllByRole('row').slice(1).map((r) => within(r).getAllByRole('cell')[0].textContent);
    expect(names()[0]).toContain('Newer Farmer');

    await user.click(screen.getByRole('button', { name: /requested/i }));
    expect(names()[0]).toContain('Older Farmer');
  });

  it('the division filter re-reads the queue for that division', async () => {
    api.getPendingPlans.mockResolvedValue([]);
    const user = renderPage();
    await screen.findByText('Nothing waiting on you');
    await screen.findByRole('option', { name: /Ampara/ });

    await user.selectOptions(screen.getByLabelText('Division'), '2');

    await waitFor(() => expect(api.getPendingPlans).toHaveBeenLastCalledWith(2));
    expect(api.getPendingPlans).toHaveBeenNthCalledWith(1, undefined);
  });

  it('Review opens the plan and a decision posts to the review endpoint, then the queue reloads', async () => {
    api.getPendingPlans.mockResolvedValueOnce([makePending()]).mockResolvedValue([]);
    api.getPlan.mockResolvedValue(makePlan());
    api.reviewPlan.mockResolvedValue(makePlan({ status: 'Rejected' }));
    const user = renderPage();

    await user.click(await screen.findByRole('button', { name: 'Review' }));
    const drawer = await screen.findByRole('dialog');
    await within(drawer).findByText('Rest of the season, from Tillering.');
    expect(api.getPlan).toHaveBeenCalledWith(31);

    await user.click(within(drawer).getByRole('radio', { name: /^Reject/ }));
    await user.type(within(drawer).getByRole('textbox'), 'Wrong variety for this soil.');
    await user.click(within(drawer).getByRole('button', { name: /submit review/i }));

    await waitFor(() =>
      expect(api.reviewPlan).toHaveBeenCalledWith(31, { decision: 'Reject', comment: 'Wrong variety for this soil.' })
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Reject recorded for plan #31 — Sunil Perera.');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(api.getPendingPlans).toHaveBeenCalledTimes(2);
  });

  it('a plan that is no longer pending shows no decision form', async () => {
    api.getPendingPlans.mockResolvedValue([makePending()]);
    api.getPlan.mockResolvedValue(makePlan({ status: 'Approved' }));
    const user = renderPage();

    await user.click(await screen.findByRole('button', { name: 'Review' }));

    expect(await screen.findByText(/already been reviewed/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /submit review/i })).not.toBeInTheDocument();
  });
});
