import { useEffect, useState } from 'react';
import {
  AlertCircle,
  Bug,
  Loader2,
  Menu,
  Plus,
  Sparkles,
  X,
  LogOut,
} from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { ObservationForm } from '../components/ObservationForm';
import { getObservations, requestAnalysis } from '../services/pestDiseaseApi';
import { formatConfidence, OBSERVATION_TYPE_LABELS, REPORT_STATUS_LABELS, SEVERITY_LABELS } from '../types';
import type { Observation, PestDiseaseReportStatus } from '../types';
import { formatDateTime } from '../utils/dates';
import '../../../styles/Dashboard.css';
import '../styles/pestDisease.css';

const STATUS_BADGE_CLASS: Record<PestDiseaseReportStatus, string> = {
  PendingOfficerReview: 'pd-badge pd-badge-pending',
  Approved: 'pd-badge pd-badge-approved',
  Rejected: 'pd-badge pd-badge-rejected',
  RevisionRequested: 'pd-badge pd-badge-revision',
};

export default function ObservationsPage() {
  const { user, logout } = useAuth();

  const [observations, setObservations] = useState<Observation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);

  // Which observation is mid request-analysis, and any error from that one call.
  const [analyzingId, setAnalyzingId] = useState<number | null>(null);
  const [analysisErrors, setAnalysisErrors] = useState<Record<number, string>>({});

  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let isMounted = true;

    getObservations()
      .then((result) => {
        if (!isMounted) return;
        setObservations(result);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setError(extractApiErrorMessage(err, 'Could not load your reports. Please try again.'));
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

  const handleSaved = (created: Observation) => {
    setIsFormOpen(false);
    setObservations((previous) => [created, ...previous]);
  };

  const handleRequestAnalysis = async (observationId: number) => {
    setAnalyzingId(observationId);
    setAnalysisErrors((previous) => {
      const next = { ...previous };
      delete next[observationId];
      return next;
    });

    try {
      const updated = await requestAnalysis(observationId);
      setObservations((previous) =>
        previous.map((observation) => (observation.id === observationId ? updated : observation))
      );
    } catch (err: unknown) {
      setAnalysisErrors((previous) => ({
        ...previous,
        [observationId]: extractApiErrorMessage(
          err,
          'The diagnosis agent could not analyze this report. Please try again.'
        ),
      }));
    } finally {
      setAnalyzingId(null);
    }
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
          <div className="pd-page-head">
            <div>
              <span className="eyebrow">PEST &amp; DISEASE MONITORING</span>
              <h1 className="pd-page-title">My Reports</h1>
              <p className="pd-page-sub">
                Report symptoms you notice on a cultivation cycle and ask the diagnosis agent for
                a read — every candidate it names is checked against the department&apos;s own
                pest and disease reference data before an officer signs off.
              </p>
            </div>

            <button className="pd-btn pd-btn-primary" onClick={() => setIsFormOpen(true)}>
              <Plus size={18} />
              Report a problem
            </button>
          </div>

          {isLoading && (
            <div className="pd-state">
              <p className="pd-state-text">Loading your reports…</p>
            </div>
          )}

          {!isLoading && error && (
            <div className="pd-state pd-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="pd-state-text">{error}</p>
              <button className="pd-btn pd-btn-quiet" onClick={handleRetry}>
                Try again
              </button>
            </div>
          )}

          {!isLoading && !error && observations.length === 0 && (
            <div className="pd-state">
              <Bug size={28} className="pd-state-icon" />
              <h2 className="pd-state-title">No reports yet</h2>
              <p className="pd-state-text">
                Noticed something odd on a crop? Report it and ask the agent for a diagnosis.
              </p>
              <button className="pd-btn pd-btn-primary" onClick={() => setIsFormOpen(true)}>
                <Plus size={18} />
                Report your first problem
              </button>
            </div>
          )}

          {!isLoading && !error && observations.length > 0 && (
            <div className="pd-list">
              {observations.map((observation) => (
                <article className="pd-card" key={observation.id}>
                  <header className="pd-card-head">
                    <div>
                      <h2 className="pd-card-title">
                        {OBSERVATION_TYPE_LABELS[observation.observationType]} ·{' '}
                        {observation.fieldName}
                      </h2>
                      <p className="pd-card-sub">
                        {observation.cropStage} stage · {SEVERITY_LABELS[observation.severity]}{' '}
                        severity · reported {formatDateTime(observation.createdAt)}
                      </p>
                    </div>
                  </header>

                  <p className="pd-card-symptoms">{observation.symptoms}</p>

                  {observation.imageUrl && (
                    <img
                      src={observation.imageUrl}
                      alt="Photo of the reported symptoms"
                      className="pd-image-preview"
                    />
                  )}

                  {observation.reports.length > 0 && (
                    <div className="pd-reports">
                      {observation.reports.map((report) => (
                        <div className="pd-report" key={report.id}>
                          <span className="pd-report-issue">{report.possibleIssue}</span>
                          <span className="pd-report-confidence">
                            {formatConfidence(report.confidence)} possible match
                          </span>
                          <span className={STATUS_BADGE_CLASS[report.status]}>
                            {REPORT_STATUS_LABELS[report.status]}
                          </span>
                          {report.officerComment && (
                            <p className="pd-report-comment">{report.officerComment}</p>
                          )}
                        </div>
                      ))}
                    </div>
                  )}

                  {observation.reports.length === 0 && observation.lastAnalyzedAt && (
                    <p className="pd-no-match">
                      The diagnosis agent found no likely match in the knowledge base as of{' '}
                      {formatDateTime(observation.lastAnalyzedAt)}. You can ask again if you have
                      more to add.
                    </p>
                  )}

                  {observation.reports.length === 0 && (
                    <div className="pd-card-actions">
                      <button
                        className="pd-btn pd-btn-outline"
                        disabled={analyzingId === observation.id}
                        onClick={() => handleRequestAnalysis(observation.id)}
                      >
                        {analyzingId === observation.id ? (
                          <>
                            <Loader2 size={16} className="pd-spin" />
                            Analyzing…
                          </>
                        ) : (
                          <>
                            <Sparkles size={16} />
                            {observation.lastAnalyzedAt
                              ? 'Ask the diagnosis agent again'
                              : 'Ask the diagnosis agent'}
                          </>
                        )}
                      </button>
                      {analysisErrors[observation.id] && (
                        <span className="pd-field-error">{analysisErrors[observation.id]}</span>
                      )}
                    </div>
                  )}
                </article>
              ))}
            </div>
          )}
        </main>
      </div>

      {isFormOpen && (
        <div
          className="pd-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-labelledby="pd-modal-title"
        >
          <div className="pd-modal">
            <header className="pd-modal-head">
              <h2 className="pd-modal-title" id="pd-modal-title">
                Report a pest or disease problem
              </h2>
              <button
                className="pd-modal-close"
                onClick={() => setIsFormOpen(false)}
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </header>

            <ObservationForm onSaved={handleSaved} onCancel={() => setIsFormOpen(false)} />
          </div>
        </div>
      )}
    </div>
  );
}
