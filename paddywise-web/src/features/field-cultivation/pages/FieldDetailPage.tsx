import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  AlertCircle,
  ArrowLeft,
  ChevronRight,
  Droplets,
  Layers,
  MapPin,
  Menu,
  Pencil,
  Plus,
  Ruler,
  Sprout,
  X,
} from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { CycleForm } from '../components/CycleForm';
import { CycleStatusBadge } from '../components/CycleStatusBadge';
import { FieldForm } from '../components/FieldForm';
import { getFieldById, getMyCycles } from '../services/fieldApi';
import { CULTIVATION_METHOD_LABELS, GROWTH_STAGE_LABELS } from '../types';
import type { CultivationCycle, Field } from '../types';
import { formatDate } from '../utils/dates';
import '../../../styles/Dashboard.css';
import '../styles/fieldCultivation.css';

/** Newest sowing first, with the id as a stable tie-break. */
function byNewestFirst(a: CultivationCycle, b: CultivationCycle): number {
  if (a.sowingDate !== b.sowingDate) return a.sowingDate < b.sowingDate ? 1 : -1;
  return b.id - a.id;
}

/** What one completed read of the field and its cycles produced, tagged with its id. */
interface FieldLoad {
  id: number;
  field: Field | null;
  error: string | null;
  cycles: CultivationCycle[];
  cyclesError: string | null;
}

export default function FieldDetailPage() {
  const { user } = useAuth();
  const { id } = useParams<{ id: string }>();

  const fieldId = Number(id);
  const isValidId = Number.isInteger(fieldId) && fieldId > 0;

  const isFarmer = user?.role === 'Farmer';

  const [loaded, setLoaded] = useState<FieldLoad | null>(null);

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isCycleFormOpen, setIsCycleFormOpen] = useState(false);

  // Bumped to ask the effect below for a fresh read after a failure.
  const [reloadToken, setReloadToken] = useState(0);

  // Everything the page renders is derived from the load, so nothing has to be
  // pushed into state from inside the effect. A result carrying another id is
  // ignored, which covers navigating straight from one field to the next.
  const current = loaded !== null && loaded.id === fieldId ? loaded : null;
  const field = current?.field ?? null;
  const cycles = current?.cycles ?? [];
  const cyclesError = current?.cyclesError ?? null;
  const idError = isValidId ? null : 'That field id is not a number.';
  const error = idError ?? current?.error ?? null;
  const isLoading = isValidId && current === null;

  useEffect(() => {
    if (!isValidId) return;

    let isMounted = true;

    // GET /api/cycles is farmer-scoped on the backend, so an officer would only
    // get a 403 out of it. The field itself is readable by either. The rejection
    // is carried through as a value so one failing list cannot sink the field.
    const cyclesRequest = isFarmer ? getMyCycles(fieldId) : Promise.resolve<CultivationCycle[]>([]);

    Promise.all([getFieldById(fieldId), cyclesRequest.catch((err: unknown) => err)])
      .then(([loadedField, loadedCycles]) => {
        if (!isMounted) return;

        const isList = Array.isArray(loadedCycles);
        setLoaded({
          id: fieldId,
          field: loadedField,
          error: null,
          cycles: isList ? [...loadedCycles].sort(byNewestFirst) : [],
          cyclesError: isList
            ? null
            : extractApiErrorMessage(
                loadedCycles,
                'Could not load the cultivation cycles for this field.'
              ),
        });
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setLoaded({
          id: fieldId,
          field: null,
          error: extractApiErrorMessage(err, 'Could not load this field. Please try again.'),
          cycles: [],
          cyclesError: null,
        });
      });

    return () => {
      isMounted = false;
    };
  }, [fieldId, isValidId, isFarmer, reloadToken]);

  const handleRetry = useCallback(() => {
    setLoaded(null);
    setReloadToken((previous) => previous + 1);
  }, []);

  const handleFieldSaved = (saved: Field) => {
    setIsEditOpen(false);
    setLoaded((previous) =>
      previous === null ? previous : { ...previous, field: saved, error: null }
    );
  };

  const handleCycleCreated = (created: CultivationCycle) => {
    setIsCycleFormOpen(false);
    setLoaded((previous) =>
      previous === null
        ? previous
        : { ...previous, cycles: [created, ...previous.cycles].sort(byNewestFirst) }
    );
  };

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
            </div>
          </div>
        </header>

        <main className="dashboard-content container">
          {isFarmer && (
            <Link className="fc-back" to="/fields">
              <ArrowLeft size={16} />
              All fields
            </Link>
          )}

          {isLoading && (
            <div className="fc-state">
              <p className="fc-state-text">Loading this field…</p>
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

          {!isLoading && !error && field && (
            <>
              <div className="fc-page-head">
                <div>
                  <span className="eyebrow">FIELD &amp; CULTIVATION MANAGEMENT</span>
                  <h1 className="fc-page-title">{field.name}</h1>
                  <p className="fc-page-sub">
                    {field.divisionName || 'No division recorded'} · farmed by {field.farmerName}
                  </p>
                </div>

                {isFarmer && (
                  <button className="fc-btn fc-btn-outline" onClick={() => setIsEditOpen(true)}>
                    <Pencil size={16} />
                    Edit field
                  </button>
                )}
              </div>

              <section className="fc-summary">
                <dl className="fc-summary-grid">
                  <div className="fc-summary-item">
                    <dt>
                      <Ruler size={14} /> Area
                    </dt>
                    <dd>
                      {field.area} {field.area === 1 ? 'acre' : 'acres'}
                    </dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>
                      <MapPin size={14} /> Division
                    </dt>
                    <dd>{field.divisionName || '—'}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>
                      <Layers size={14} /> Soil
                    </dt>
                    <dd>{field.soilType || 'Not recorded'}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>
                      <Droplets size={14} /> Irrigation
                    </dt>
                    <dd>{field.irrigationType || 'Not recorded'}</dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>
                      <MapPin size={14} /> Coordinates
                    </dt>
                    <dd>
                      {field.latitude === null || field.longitude === null
                        ? 'Not recorded'
                        : `${field.latitude}, ${field.longitude}`}
                    </dd>
                  </div>
                  <div className="fc-summary-item">
                    <dt>
                      <Sprout size={14} /> Status
                    </dt>
                    <dd>{field.isActive ? 'Active' : 'Retired'}</dd>
                  </div>
                </dl>
              </section>

              <section className="fc-section">
                <div className="fc-section-head">
                  <div>
                    <h2 className="fc-section-title">Cultivation cycles</h2>
                    <p className="fc-section-sub">Newest season first.</p>
                  </div>

                  {isFarmer && (
                    <button
                      className="fc-btn fc-btn-primary"
                      onClick={() => setIsCycleFormOpen(true)}
                    >
                      <Plus size={18} />
                      Start new cycle
                    </button>
                  )}
                </div>

                {cyclesError && (
                  <p className="fc-alert" role="alert">
                    <AlertCircle size={18} />
                    <span>{cyclesError}</span>
                  </p>
                )}

                {!isFarmer && (
                  <p className="fc-note">
                    The cycle list is farmer-scoped on the API today, so it is not shown here.
                    Open a cycle directly at <code>/cycles/&lt;id&gt;</code> to review its
                    timeline.
                  </p>
                )}

                {isFarmer && !cyclesError && cycles.length === 0 && (
                  <div className="fc-state">
                    <Sprout size={28} className="fc-state-icon" />
                    <h3 className="fc-state-title">No cycles on this field yet</h3>
                    <p className="fc-state-text">
                      Start a cultivation cycle to get a stage-by-stage plan from the sowing date.
                    </p>
                    <button
                      className="fc-btn fc-btn-primary"
                      onClick={() => setIsCycleFormOpen(true)}
                    >
                      <Plus size={18} />
                      Start the first cycle
                    </button>
                  </div>
                )}

                {cycles.length > 0 && (
                  <ul className="fc-cycle-list">
                    {cycles.map((cycle) => (
                      <li key={cycle.id}>
                        <Link className="fc-cycle-row" to={`/cycles/${cycle.id}`}>
                          <div className="fc-cycle-main">
                            <span className="fc-cycle-season">
                              {cycle.season} {cycle.year}
                            </span>
                            <span className="fc-cycle-variety">
                              {cycle.varietyName} · {CULTIVATION_METHOD_LABELS[cycle.method]}
                            </span>
                          </div>

                          <dl className="fc-cycle-meta">
                            <div>
                              <dt>Sown</dt>
                              <dd>{formatDate(cycle.sowingDate)}</dd>
                            </div>
                            <div>
                              <dt>Expected harvest</dt>
                              <dd>{formatDate(cycle.expectedHarvestDate)}</dd>
                            </div>
                            <div>
                              <dt>Current stage</dt>
                              <dd>{GROWTH_STAGE_LABELS[cycle.currentStage]}</dd>
                            </div>
                          </dl>

                          <div className="fc-cycle-end">
                            <CycleStatusBadge status={cycle.status} />
                            <ChevronRight size={18} />
                          </div>
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            </>
          )}
        </main>
      </div>

      {isEditOpen && field && (
        <div
          className="fc-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-labelledby="fc-edit-title"
        >
          <div className="fc-modal">
            <header className="fc-modal-head">
              <h2 className="fc-modal-title" id="fc-edit-title">
                Edit field
              </h2>
              <button
                className="fc-modal-close"
                onClick={() => setIsEditOpen(false)}
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </header>

            <FieldForm
              field={field}
              onSaved={handleFieldSaved}
              onCancel={() => setIsEditOpen(false)}
            />
          </div>
        </div>
      )}

      {isCycleFormOpen && field && (
        <div
          className="fc-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-labelledby="fc-cycle-title"
        >
          <div className="fc-modal">
            <header className="fc-modal-head">
              <h2 className="fc-modal-title" id="fc-cycle-title">
                Start a cultivation cycle
              </h2>
              <button
                className="fc-modal-close"
                onClick={() => setIsCycleFormOpen(false)}
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </header>

            <CycleForm
              fieldId={field.id}
              onCreated={handleCycleCreated}
              onCancel={() => setIsCycleFormOpen(false)}
            />
          </div>
        </div>
      )}
    </div>
  );
}
