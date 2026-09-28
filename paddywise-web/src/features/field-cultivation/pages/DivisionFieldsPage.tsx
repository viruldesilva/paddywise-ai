import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  AlertCircle,
  Droplets,
  Layers,
  LogOut,
  MapPin,
  Menu,
  RefreshCw,
  Ruler,
  Sprout,
  User,
} from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { getDivisions, getFieldsByDivision } from '../services/fieldApi';
import type { Division, Field } from '../types';
import '../../../styles/Dashboard.css';
import '../styles/fieldCultivation.css';

/** The query parameter that carries the chosen division, so a back link can restore it. */
const DIVISION_PARAM = 'division';

/** What one completed read of GET /api/fields/division/{id} produced, tagged with its id. */
interface FieldsLoad {
  divisionId: number;
  fields: Field[];
  error: string | null;
}

/** Alphabetical by farmer, then by field, so one farmer's plots sit together. */
function byFarmerThenName(a: Field, b: Field): number {
  return a.farmerName.localeCompare(b.farmerName) || a.name.localeCompare(b.name);
}

/**
 * The officer's way into Component 1: every registered paddy field in one
 * agrarian division. Farmers register fields from the mobile app; here they
 * are read-only, and each opens onto its cycles, stage log and plans.
 *
 * GET /api/fields/division/{id} is AgriculturalOfficer, FieldOfficer and Admin.
 */
export default function DivisionFieldsPage() {
  const { user, logout } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  const [divisions, setDivisions] = useState<Division[] | null>(null);
  const [divisionsError, setDivisionsError] = useState<string | null>(null);
  const [loaded, setLoaded] = useState<FieldsLoad | null>(null);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  // Bumped to ask for a fresh read — after a failure or on Refresh.
  const [divisionsReload, setDivisionsReload] = useState(0);
  const [fieldsReload, setFieldsReload] = useState(0);

  // The URL wins; without one, the first division is shown so the page is never empty.
  const requested = Number(searchParams.get(DIVISION_PARAM));
  const selectedId =
    Number.isInteger(requested) && requested > 0 ? requested : (divisions?.[0]?.id ?? null);
  const selectedDivision = divisions?.find((division) => division.id === selectedId) ?? null;

  const current = loaded !== null && loaded.divisionId === selectedId ? loaded : null;
  const fields = current?.fields ?? [];
  const error = current?.error ?? null;
  const isLoadingFields = selectedId !== null && current === null;

  useEffect(() => {
    let isMounted = true;

    getDivisions()
      .then((result) => {
        if (!isMounted) return;
        setDivisions(result);
        setDivisionsError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setDivisions([]);
        setDivisionsError(
          extractApiErrorMessage(err, 'Could not load the divisions. Please try again.')
        );
      });

    return () => {
      isMounted = false;
    };
  }, [divisionsReload]);

  useEffect(() => {
    if (selectedId === null) return;

    let isMounted = true;

    getFieldsByDivision(selectedId)
      .then((result) => {
        if (!isMounted) return;
        setLoaded({ divisionId: selectedId, fields: [...result].sort(byFarmerThenName), error: null });
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setLoaded({
          divisionId: selectedId,
          fields: [],
          error: extractApiErrorMessage(
            err,
            'Could not load the fields in this division. Please try again.'
          ),
        });
      });

    return () => {
      isMounted = false;
    };
  }, [selectedId, fieldsReload]);

  const handleRefresh = useCallback(() => {
    setLoaded(null);
    if (divisionsError) {
      setDivisions(null);
      setDivisionsError(null);
      setDivisionsReload((previous) => previous + 1);
    }
    setFieldsReload((previous) => previous + 1);
  }, [divisionsError]);

  if (!user) return null;

  const farmerCount = new Set(fields.map((field) => field.farmerId)).size;

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
          <div className="fc-page-head">
            <div>
              <span className="eyebrow">FIELD &amp; CULTIVATION MANAGEMENT</span>
              <h1 className="fc-page-title">Farmer fields</h1>
              <p className="fc-page-sub">
                Every paddy field farmers have registered from the mobile app, one division at a
                time. Open a field to follow its cultivation cycles and plans.
              </p>
            </div>

            <button className="fc-btn fc-btn-outline" onClick={handleRefresh}>
              <RefreshCw size={18} />
              Refresh
            </button>
          </div>

          {divisionsError && (
            <div className="fc-state fc-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="fc-state-text">{divisionsError}</p>
              <button className="fc-btn fc-btn-quiet" onClick={handleRefresh}>
                Try again
              </button>
            </div>
          )}

          {divisions === null && (
            <div className="fc-state">
              <p className="fc-state-text">Loading divisions…</p>
            </div>
          )}

          {divisions !== null && !divisionsError && (
            <div className="fc-queue-controls">
              <div className="fc-form-group fc-queue-filter">
                <label htmlFor="fc-division-select">Division</label>
                <select
                  id="fc-division-select"
                  className="fc-input"
                  value={selectedId === null ? '' : String(selectedId)}
                  onChange={(event) =>
                    setSearchParams({ [DIVISION_PARAM]: event.target.value }, { replace: true })
                  }
                >
                  {divisions.map((division) => (
                    <option key={division.id} value={String(division.id)}>
                      {division.name} · {division.district}
                    </option>
                  ))}
                </select>
              </div>

              {!isLoadingFields && !error && selectedDivision && (
                <p className="fc-queue-count">
                  {fields.length} {fields.length === 1 ? 'field' : 'fields'} · {farmerCount}{' '}
                  {farmerCount === 1 ? 'farmer' : 'farmers'} in {selectedDivision.name}
                </p>
              )}
            </div>
          )}

          {isLoadingFields && (
            <div className="fc-state">
              <p className="fc-state-text">Loading fields…</p>
            </div>
          )}

          {!isLoadingFields && error && (
            <div className="fc-state fc-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="fc-state-text">{error}</p>
              <button className="fc-btn fc-btn-quiet" onClick={handleRefresh}>
                Try again
              </button>
            </div>
          )}

          {divisions !== null && divisions.length === 0 && !divisionsError && (
            <div className="fc-state">
              <MapPin size={28} className="fc-state-icon" />
              <h2 className="fc-state-title">No divisions set up</h2>
              <p className="fc-state-text">Agrarian divisions have not been seeded yet.</p>
            </div>
          )}

          {current !== null && !error && fields.length === 0 && (
            <div className="fc-state">
              <Sprout size={28} className="fc-state-icon" />
              <h2 className="fc-state-title">No fields in this division yet</h2>
              <p className="fc-state-text">
                Fields appear here once farmers register them in the mobile app.
              </p>
            </div>
          )}

          {current !== null && !error && fields.length > 0 && (
            <div className="fc-grid">
              {fields.map((field) => (
                <article className="fc-card" key={field.id}>
                  <header className="fc-card-head">
                    <h2 className="fc-card-title">{field.name}</h2>
                    <span className="fc-card-area">
                      <Ruler size={14} />
                      {field.area} {field.area === 1 ? 'acre' : 'acres'}
                    </span>
                  </header>

                  <dl className="fc-card-meta">
                    <div className="fc-card-meta-row">
                      <dt>
                        <User size={14} /> Farmer
                      </dt>
                      <dd>{field.farmerName || '—'}</dd>
                    </div>
                    <div className="fc-card-meta-row">
                      <dt>
                        <Layers size={14} /> Soil
                      </dt>
                      <dd>{field.soilType || 'Not recorded'}</dd>
                    </div>
                    <div className="fc-card-meta-row">
                      <dt>
                        <Droplets size={14} /> Irrigation
                      </dt>
                      <dd>{field.irrigationType || 'Not recorded'}</dd>
                    </div>
                  </dl>

                  <Link className="fc-btn fc-btn-outline fc-card-action" to={`/fields/${field.id}`}>
                    View cycles
                  </Link>
                </article>
              ))}
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
