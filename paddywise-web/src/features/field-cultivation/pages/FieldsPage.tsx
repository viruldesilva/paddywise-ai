import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AlertCircle,
  Droplets,
  Layers,
  MapPin,
  Menu,
  Plus,
  Ruler,
  Sprout,
  X,
} from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { FieldForm } from '../components/FieldForm';
import { getMyFields } from '../services/fieldApi';
import type { Field } from '../types';
import '../../../styles/Dashboard.css';
import '../styles/fieldCultivation.css';

export default function FieldsPage() {
  const { user } = useAuth();

  const [fields, setFields] = useState<Field[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);

  // Bumped to ask the effect below for a fresh read after a failure.
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let isMounted = true;

    getMyFields()
      .then((result) => {
        if (!isMounted) return;
        setFields(result);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setError(extractApiErrorMessage(err, 'Could not load your fields. Please try again.'));
      })
      .finally(() => {
        if (isMounted) setIsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [reloadToken]);

  const handleRetry = () => {
    setIsLoading(true);
    setError(null);
    setReloadToken((previous) => previous + 1);
  };

  const handleSaved = (created: Field) => {
    setIsFormOpen(false);
    // Keep the grid in the server's order — by name.
    setFields((previous) =>
      [...previous, created].sort((a, b) => a.name.localeCompare(b.name))
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
          <div className="fc-page-head">
            <div>
              <span className="eyebrow">FIELD &amp; CULTIVATION MANAGEMENT</span>
              <h1 className="fc-page-title">My Fields</h1>
              <p className="fc-page-sub">
                Every plot you have registered, with the division it sits in and how it is
                watered.
              </p>
            </div>

            <button className="fc-btn fc-btn-primary" onClick={() => setIsFormOpen(true)}>
              <Plus size={18} />
              Add field
            </button>
          </div>

          {isLoading && (
            <div className="fc-state">
              <p className="fc-state-text">Loading your fields…</p>
            </div>
          )}

          {!isLoading && error && (
            <div className="fc-state fc-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="fc-state-text">{error}</p>
              <button className="fc-btn fc-btn-quiet" onClick={handleRetry}>
                Try again
              </button>
            </div>
          )}

          {!isLoading && !error && fields.length === 0 && (
            <div className="fc-state">
              <Sprout size={28} className="fc-state-icon" />
              <h2 className="fc-state-title">No fields yet</h2>
              <p className="fc-state-text">
                Register your first plot to start planning a cultivation cycle on it.
              </p>
              <button className="fc-btn fc-btn-primary" onClick={() => setIsFormOpen(true)}>
                <Plus size={18} />
                Add your first field
              </button>
            </div>
          )}

          {!isLoading && !error && fields.length > 0 && (
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
                        <MapPin size={14} /> Division
                      </dt>
                      <dd>{field.divisionName || '—'}</dd>
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
                    View
                  </Link>
                </article>
              ))}
            </div>
          )}
        </main>
      </div>

      {isFormOpen && (
        <div
          className="fc-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-labelledby="fc-modal-title"
        >
          <div className="fc-modal">
            <header className="fc-modal-head">
              <h2 className="fc-modal-title" id="fc-modal-title">
                Register a field
              </h2>
              <button
                className="fc-modal-close"
                onClick={() => setIsFormOpen(false)}
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </header>

            <FieldForm onSaved={handleSaved} onCancel={() => setIsFormOpen(false)} />
          </div>
        </div>
      )}
    </div>
  );
}
