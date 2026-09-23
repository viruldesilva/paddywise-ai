import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  ArrowDown,
  ArrowUp,
  CheckCircle2,
  ClipboardCheck,
  Menu,
  RefreshCw,
  X,
  LogOut,
} from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { AgentActivity } from '../components/AgentActivity';
import { PlanReviewForm } from '../components/PlanReviewForm';
import { PlanView } from '../components/PlanView';
import { getDivisions, getPendingPlans, getPlan } from '../services/fieldApi';
import { PLAN_REVIEW_DECISION_LABELS } from '../types';
import type {
  CultivationPlan,
  Division,
  PendingPlanSummary,
  PlanReviewDecision,
} from '../types';
import { formatDateTime } from '../utils/dates';
import '../../../styles/Dashboard.css';
import '../styles/fieldCultivation.css';

/** Newest first is what the queue arrives in, so it is the default here too. */
type SortDirection = 'desc' | 'asc';

/** The filter's "every division" option, kept out of the numeric id space. */
const ALL_DIVISIONS = 'all';

/** What one completed read of GET /api/plans/{id} produced, tagged with its id. */
interface PlanLoad {
  planId: number;
  plan: CultivationPlan | null;
  error: string | null;
}

const TOAST_MS = 5_000;

/**
 * The agricultural officer's approval queue: every plan the Cultivation Planning
 * Agent produced and the validator passed, waiting on a human verdict.
 *
 * GET /api/plans/pending and POST /api/plans/{id}/review are both
 * AgriculturalOfficer only, so the route is scoped to that role.
 */
export default function PlanApprovalPage() {
  const { user, logout } = useAuth();

  const [rows, setRows] = useState<PendingPlanSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [divisions, setDivisions] = useState<Division[]>([]);

  const [divisionFilter, setDivisionFilter] = useState<string>(ALL_DIVISIONS);
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  // The row whose drawer is open, and the full plan read for it.
  const [reviewing, setReviewing] = useState<PendingPlanSummary | null>(null);
  const [planLoad, setPlanLoad] = useState<PlanLoad | null>(null);

  // Bumped to ask the queue effect for a fresh read — after a review, a failure
  // or a division change.
  const [reloadToken, setReloadToken] = useState(0);

  const divisionId = divisionFilter === ALL_DIVISIONS ? undefined : Number(divisionFilter);
  const isLoading = rows === null;

  useEffect(() => {
    let isMounted = true;

    getPendingPlans(divisionId)
      .then((result) => {
        if (!isMounted) return;
        setRows(result);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setRows([]);
        setError(
          extractApiErrorMessage(err, 'Could not load the approval queue. Please try again.')
        );
      });

    return () => {
      isMounted = false;
    };
  }, [divisionId, reloadToken]);

  // The filter needs ids, and the queue rows carry only division names. Losing
  // this read costs the page its filter, not its table.
  useEffect(() => {
    let isMounted = true;

    getDivisions()
      .then((result) => {
        if (isMounted) setDivisions(result);
      })
      .catch(() => {
        if (isMounted) setDivisions([]);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  // The drawer's own read: the queue row is a summary, and PlanView needs the
  // whole plan with its agent runs.
  useEffect(() => {
    if (reviewing === null) return;

    const planId = reviewing.planId;
    let isMounted = true;

    getPlan(planId)
      .then((result) => {
        if (!isMounted) return;
        setPlanLoad({ planId, plan: result, error: null });
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setPlanLoad({
          planId,
          plan: null,
          error: extractApiErrorMessage(err, 'Could not load this plan. Please try again.'),
        });
      });

    return () => {
      isMounted = false;
    };
  }, [reviewing]);

  useEffect(() => {
    if (toast === null) return;

    const timer = window.setTimeout(() => setToast(null), TOAST_MS);
    return () => window.clearTimeout(timer);
  }, [toast]);

  const sorted = useMemo(() => {
    if (rows === null) return [];

    // Ties on the instant keep the server's tie-break — the higher id first —
    // so the order never shuffles between two reads of the same queue.
    return [...rows].sort((a, b) => {
      const byDate = Date.parse(a.createdAt) - Date.parse(b.createdAt);
      const decided = byDate !== 0 ? byDate : a.planId - b.planId;
      return sortDirection === 'asc' ? decided : -decided;
    });
  }, [rows, sortDirection]);

  const closeDrawer = useCallback(() => {
    setReviewing(null);
    setPlanLoad(null);
  }, []);

  // The drawer covers the table, so Escape has to get back out of it.
  useEffect(() => {
    if (reviewing === null) return;

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') closeDrawer();
    };

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [reviewing, closeDrawer]);

  const handleRefresh = useCallback(() => {
    setRows(null);
    setError(null);
    setReloadToken((previous) => previous + 1);
  }, []);

  const handleReviewed = useCallback(
    (plan: CultivationPlan, decision: PlanReviewDecision) => {
      const farmer = reviewing?.farmerName ?? 'the farmer';
      closeDrawer();
      // The reviewed plan leaves the queue, so the table is re-read rather than
      // patched: another officer may have cleared rows in the meantime.
      handleRefresh();
      setToast(
        `${PLAN_REVIEW_DECISION_LABELS[decision]} recorded for plan #${plan.id} — ${farmer}.`
      );
    },
    [closeDrawer, handleRefresh, reviewing]
  );

  const currentPlan =
    reviewing !== null && planLoad !== null && planLoad.planId === reviewing.planId
      ? planLoad
      : null;
  const isPlanLoading = reviewing !== null && currentPlan === null;

  if (!user) return null;

  return (
    <div className="dashboard-layout">
      <Sidebar role={user.role} isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      <div className="dashboard-main-wrapper">
        <header className="dashboard-header">
          <div className="container dashboard-header-inner">
            <div className="dashboard-header-title">
              <button
                className="mobile-menu-btn"
                onClick={() => setIsSidebarOpen(true)}
                aria-label="Open menu"
              >
                <Menu size={24} />
              </button>
            </div>

            <div className="dashboard-user-meta">
              <div className="dashboard-user-greeting">
                <span className="dashboard-user-name">{user.name}</span>
                <span className="dashboard-user-sub">{user.email}</span>
              </div>
              <button 
                onClick={logout} 
                className="btn btn-secondary btn-sm"
                title="Sign Out"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}
              >
                <LogOut size={16} />
                Sign Out
              </button>
            </div>
          </div>
        </header>

        <main className="dashboard-content container">
          <div className="fc-page-head">
            <div>
              <span className="eyebrow">FIELD &amp; CULTIVATION MANAGEMENT</span>
              <h1 className="fc-page-title">Plans awaiting approval</h1>
              <p className="fc-page-sub">
                Every cultivation plan the planning agent drafted and the validator passed. Nothing
                here reaches a farmer until you sign it off.
              </p>
            </div>

            <button className="fc-btn fc-btn-outline" onClick={handleRefresh}>
              <RefreshCw size={18} />
              Refresh
            </button>
          </div>

          <div className="fc-queue-controls">
            <div className="fc-form-group fc-queue-filter">
              <label htmlFor="fc-division-filter">Division</label>
              <select
                id="fc-division-filter"
                className="fc-input"
                value={divisionFilter}
                onChange={(event) => {
                  // Dropped, not kept: the rows on screen belong to the division
                  // being navigated away from.
                  setRows(null);
                  setError(null);
                  setDivisionFilter(event.target.value);
                }}
              >
                <option value={ALL_DIVISIONS}>All divisions</option>
                {divisions.map((division) => (
                  <option key={division.id} value={String(division.id)}>
                    {division.name} · {division.district}
                  </option>
                ))}
              </select>
            </div>

            {!isLoading && !error && (
              <p className="fc-queue-count">
                {sorted.length} {sorted.length === 1 ? 'plan' : 'plans'} waiting
              </p>
            )}
          </div>

          {isLoading && (
            <div className="fc-state">
              <p className="fc-state-text">Loading the approval queue…</p>
            </div>
          )}

          {!isLoading && error && (
            <div className="fc-state fc-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="fc-state-text">{error}</p>
              <button className="fc-btn fc-btn-quiet" onClick={handleRefresh}>
                Try again
              </button>
            </div>
          )}

          {!isLoading && !error && sorted.length === 0 && (
            <div className="fc-state">
              <ClipboardCheck size={28} className="fc-state-icon" />
              <h2 className="fc-state-title">Nothing waiting on you</h2>
              <p className="fc-state-text">
                {divisionFilter === ALL_DIVISIONS
                  ? 'No plan is awaiting approval right now.'
                  : 'No plan from this division is awaiting approval right now.'}
              </p>
            </div>
          )}

          {!isLoading && !error && sorted.length > 0 && (
            <div className="fc-table-wrap">
              <table className="fc-table">
                <caption className="fc-table-caption">
                  Cultivation plans awaiting your approval.
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Farmer</th>
                    <th scope="col">Field</th>
                    <th scope="col">Division</th>
                    <th scope="col">Cycle</th>
                    <th
                      scope="col"
                      aria-sort={sortDirection === 'asc' ? 'ascending' : 'descending'}
                    >
                      <button
                        type="button"
                        className="fc-table-sort"
                        onClick={() =>
                          setSortDirection((previous) => (previous === 'desc' ? 'asc' : 'desc'))
                        }
                      >
                        Requested
                        {sortDirection === 'asc' ? (
                          <ArrowUp size={14} aria-hidden="true" />
                        ) : (
                          <ArrowDown size={14} aria-hidden="true" />
                        )}
                        <span className="fc-sr-only">
                          {sortDirection === 'asc'
                            ? 'Sorted oldest first. Activate to show newest first.'
                            : 'Sorted newest first. Activate to show oldest first.'}
                        </span>
                      </button>
                    </th>
                    <th scope="col">
                      <span className="fc-sr-only">Review</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {sorted.map((row) => (
                    <tr key={row.planId}>
                      <td>
                        <span className="fc-table-strong">{row.farmerName || '—'}</span>
                        <span className="fc-table-sub">{row.objective}</span>
                      </td>
                      <td>{row.fieldName || '—'}</td>
                      <td>{row.divisionName || '—'}</td>
                      <td>
                        {row.season} {row.year}
                      </td>
                      <td>{formatDateTime(row.createdAt)}</td>
                      <td className="fc-table-action">
                        <button
                          type="button"
                          className="fc-btn fc-btn-ghost"
                          onClick={() => setReviewing(row)}
                        >
                          Review
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </main>
      </div>

      {reviewing !== null && (
        <div className="fc-drawer-backdrop" onClick={closeDrawer}>
          {/* The click that closes the drawer belongs to the backdrop alone. */}
          <aside
            className="fc-drawer"
            role="dialog"
            aria-modal="true"
            aria-labelledby="fc-drawer-title"
            onClick={(event) => event.stopPropagation()}
          >
            <header className="fc-drawer-head">
              <div>
                <span className="eyebrow">REVIEW</span>
                <h2 className="fc-drawer-title" id="fc-drawer-title">
                  {reviewing.farmerName || 'Plan'} · {reviewing.fieldName}
                </h2>
                <p className="fc-section-sub">
                  {reviewing.season} {reviewing.year} · requested{' '}
                  {formatDateTime(reviewing.createdAt)}
                </p>
              </div>

              <button className="fc-modal-close" onClick={closeDrawer} aria-label="Close">
                <X size={20} />
              </button>
            </header>

            <div className="fc-drawer-body">
              {isPlanLoading && <p className="fc-state-text">Loading this plan…</p>}

              {currentPlan?.error && (
                <p className="fc-alert" role="alert">
                  <AlertCircle size={18} />
                  <span>{currentPlan.error}</span>
                </p>
              )}

              {currentPlan?.plan && (
                <>
                  <PlanView plan={currentPlan.plan} />
                  <AgentActivity runs={currentPlan.plan.agentRuns} />

                  {currentPlan.plan.status === 'PendingOfficerApproval' ? (
                    <PlanReviewForm
                      planId={currentPlan.plan.id}
                      onReviewed={handleReviewed}
                      onCancel={closeDrawer}
                    />
                  ) : (
                    <p className="fc-note">
                      This plan has already been reviewed, so there is nothing left to decide.
                      Refresh the queue to see what is still waiting.
                    </p>
                  )}
                </>
              )}
            </div>
          </aside>
        </div>
      )}

      {toast !== null && (
        <div className="fc-toast" role="status" aria-live="polite">
          <CheckCircle2 size={18} />
          <span>{toast}</span>
          <button className="fc-toast-close" onClick={() => setToast(null)} aria-label="Dismiss">
            <X size={16} />
          </button>
        </div>
      )}
    </div>
  );
}
