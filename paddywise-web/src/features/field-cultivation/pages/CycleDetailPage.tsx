import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { AlertCircle, ArrowLeft, History, Menu, LogOut } from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { AgentActivity } from '../components/AgentActivity';
import { CycleStatusBadge } from '../components/CycleStatusBadge';
import { PlanStatusBadge, PlanView } from '../components/PlanView';
import { StageTimeline } from '../components/StageTimeline';
import { getCycleById, getPlansForCycle } from '../services/fieldApi';
import { CULTIVATION_METHOD_LABELS, GROWTH_STAGE_LABELS } from '../types';
import type { CultivationCycle, CultivationPlan } from '../types';
import { formatDate, formatDateTime } from '../utils/dates';
import '../../../styles/Dashboard.css';
import '../styles/fieldCultivation.css';

/** What one completed read of /api/cycles/{id} produced, tagged with its id. */
interface CycleLoad {
  id: number;
  cycle: CultivationCycle | null;
  error: string | null;
}

/**
 * The same for /api/cycles/{id}/plans, kept separate so a plan read that fails
 * costs the page its plan section and not the cycle itself.
 */
interface PlanLoad {
  id: number;
  plans: CultivationPlan[];
  error: string | null;
}

export default function CycleDetailPage() {
  const { user, logout } = useAuth();
  const { id } = useParams<{ id: string }>();

  const cycleId = Number(id);
  const isValidId = Number.isInteger(cycleId) && cycleId > 0;

  const [loaded, setLoaded] = useState<CycleLoad | null>(null);
  const [plansLoaded, setPlansLoaded] = useState<PlanLoad | null>(null);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  // Bumped to ask the effect below for a fresh read after a failure.
  const [reloadToken, setReloadToken] = useState(0);

  // Everything the page renders is derived from the load, so nothing has to be
  // pushed into state from inside the effect. A result carrying another id is
  // ignored, which covers navigating straight from one cycle to the next.
  const current = loaded !== null && loaded.id === cycleId ? loaded : null;
  const cycle = current?.cycle ?? null;
  const idError = isValidId ? null : 'That cycle id is not a number.';
  const error = idError ?? current?.error ?? null;
  const isLoading = isValidId && current === null;

  const currentPlans = plansLoaded !== null && plansLoaded.id === cycleId ? plansLoaded : null;
  const arePlansLoading = isValidId && currentPlans === null;

  // GetForCycleAsync orders by CreatedAt descending, so the newest plan leads.
  const plans = currentPlans?.plans ?? [];
  const latestPlan = plans.length > 0 ? plans[0] : null;
  const earlierPlans = plans.slice(1);

  useEffect(() => {
    if (!isValidId) return;

    let isMounted = true;

    getCycleById(cycleId)
      .then((result) => {
        if (!isMounted) return;
        setLoaded({ id: cycleId, cycle: result, error: null });
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setLoaded({
          id: cycleId,
          cycle: null,
          error: extractApiErrorMessage(
            err,
            'Could not load this cultivation cycle. Please try again.'
          ),
        });
      });

    return () => {
      isMounted = false;
    };
  }, [cycleId, isValidId, reloadToken]);

  useEffect(() => {
    if (!isValidId) return;

    let isMounted = true;

    getPlansForCycle(cycleId)
      .then((result) => {
        if (!isMounted) return;
        setPlansLoaded({ id: cycleId, plans: result, error: null });
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setPlansLoaded({
          id: cycleId,
          plans: [],
          error: extractApiErrorMessage(
            err,
            'Could not load the plans for this cycle. Please try again.'
          ),
        });
      });

    return () => {
      isMounted = false;
    };
  }, [cycleId, isValidId, reloadToken]);

  const handleRetry = useCallback(() => {
    setLoaded(null);
    setPlansLoaded(null);
    setReloadToken((previous) => previous + 1);
  }, []);

  /** A stage log comes back as the whole refreshed cycle. */
  const handleLogged = useCallback(
    (updated: CultivationCycle) => {
      setLoaded({ id: cycleId, cycle: updated, error: null });
    },
    [cycleId]
  );

  if (!user) return null;

  // Farmers log stages from the mobile app; on the web only an agricultural
  // officer may, since POST /api/cycles/{id}/stages refuses Admin.
  const canLog = user.role === 'AgriculturalOfficer';

  // The farmer's reports may have drifted from what they have actually reported.
  const isBehindPlan = cycle !== null && cycle.currentStage !== cycle.expectedStageToday;

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
                onClick={() => logout()} 
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
          {cycle && (
            <Link className="fc-back" to={`/fields/${cycle.fieldId}`}>
              <ArrowLeft size={16} />
              {cycle.fieldName || 'Back to field'}
            </Link>
          )}

          {isLoading && (
            <div className="fc-state">
              <p className="fc-state-text">Loading this cultivation cycle…</p>
            </div>
          )}

          {!isLoading && error && (
            <div className="fc-state fc-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="fc-state-text">{error}</p>
              {isValidId && (
                <button className="fc-btn fc-btn-quiet" onClick={handleRetry}>
                  Try again
                </button>
              )}
            </div>
          )}

          {!isLoading && !error && cycle && (
            <>
              <div className="fc-page-head">
                <div>
                  <span className="eyebrow">CULTIVATION CYCLE</span>
                  <h1 className="fc-page-title">
                    {cycle.season} {cycle.year} · {cycle.varietyName}
                  </h1>
                  <p className="fc-page-sub">
                    {cycle.fieldName} · sown {formatDate(cycle.sowingDate)} ·{' '}
                    {CULTIVATION_METHOD_LABELS[cycle.method]}
                  </p>
                </div>

                <CycleStatusBadge status={cycle.status} />
              </div>

              <section className="fc-summary">
                <dl className="fc-summary-grid">
                  <div className="fc-summary-item">
                    <dt>Sowing date</dt>
                    <dd>{formatDate(cycle.sowingDate)}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>Expected harvest</dt>
                    <dd>{formatDate(cycle.expectedHarvestDate)}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>Actual harvest</dt>
                    <dd>{formatDate(cycle.actualHarvestDate)}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>Season length</dt>
                    <dd>{cycle.durationDays} days</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>Reported stage</dt>
                    <dd>{GROWTH_STAGE_LABELS[cycle.currentStage]}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>Plan says today</dt>
                    <dd>{GROWTH_STAGE_LABELS[cycle.expectedStageToday]}</dd>
                  </div>
                </dl>

                {isBehindPlan && (
                  <p className="fc-note">
                    The plan expects {GROWTH_STAGE_LABELS[cycle.expectedStageToday]} today, but the
                    last reported stage is {GROWTH_STAGE_LABELS[cycle.currentStage]}.
                  </p>
                )}

                {cycle.notes && <p className="fc-note">{cycle.notes}</p>}
              </section>

              <StageTimeline cycle={cycle} canLog={canLog} onLogged={handleLogged} />

              {/* Farmers request plans from the mobile app; the web shows them. */}
              <section className="fc-section">
                {arePlansLoading && <p className="fc-state-text">Loading this cycle's plans…</p>}

                {!arePlansLoading && currentPlans?.error && (
                  <p className="fc-alert" role="alert">
                    <AlertCircle size={18} />
                    <span>{currentPlans.error}</span>
                  </p>
                )}

                {!arePlansLoading && !currentPlans?.error && latestPlan === null && (
                  <p className="fc-state-text">
                    No plan has been generated for this cycle yet.
                  </p>
                )}

                {!arePlansLoading && latestPlan !== null && (
                  <>
                    <PlanView plan={latestPlan} />
                    <AgentActivity runs={latestPlan.agentRuns} />
                  </>
                )}

                {earlierPlans.length > 0 && (
                  <div className="fc-plan-history">
                    <h3 className="fc-subsection-title">
                      <History size={16} />
                      Earlier plans
                    </h3>
                    <ul className="fc-plan-history-list">
                      {earlierPlans.map((earlier) => (
                        <li className="fc-plan-history-item" key={earlier.id}>
                          <PlanStatusBadge status={earlier.status} />
                          <span className="fc-plan-history-date">
                            {formatDateTime(earlier.createdAt)}
                          </span>
                          <span className="fc-plan-history-objective">{earlier.objective}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </section>
            </>
          )}
        </main>
      </div>
    </div>
  );
}
