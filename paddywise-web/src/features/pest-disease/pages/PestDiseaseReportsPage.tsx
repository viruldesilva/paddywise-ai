import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
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
import { ReportReviewForm } from '../components/ReportReviewForm';
import { getPestDiseaseReports } from '../services/pestDiseaseApi';
import {
  formatConfidence,
  OBSERVATION_TYPE_LABELS,
  REPORT_REVIEW_DECISION_LABELS,
  SEVERITY_LABELS,
} from '../types';
import type { PestDiseaseReport, PestDiseaseReportStatus, ReportReviewDecision } from '../types';
import { formatDateTime } from '../utils/dates';
import '../../../styles/Dashboard.css';
import '../styles/pestDisease.css';

/** The filter's "every status" option, kept out of the status string union. */
const ALL_STATUSES = 'all';
type StatusFilter = PestDiseaseReportStatus | typeof ALL_STATUSES;

const STATUS_OPTIONS: readonly { value: StatusFilter; label: string }[] = [
  { value: 'PendingOfficerReview', label: 'Awaiting review' },
  { value: ALL_STATUSES, label: 'All statuses' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'RevisionRequested', label: 'Revision requested' },
];

const TOAST_MS = 5_000;

/**
 * The agricultural officer's diagnosis review queue: every candidate the Crop
 * Analysis Agent produced and the deterministic validator passed, waiting on a
 * human verdict. GET /api/pest-disease-reports is open to any authenticated
 * caller, but POST .../review is AgriculturalOfficer only, so the route is
 * scoped to that role.
 */
export default function PestDiseaseReportsPage() {
  const { user, logout } = useAuth();

  const [rows, setRows] = useState<PestDiseaseReport[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('PendingOfficerReview');

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const [reviewing, setReviewing] = useState<PestDiseaseReport | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  const isLoading = rows === null;

  useEffect(() => {
    let isMounted = true;

    getPestDiseaseReports(
      statusFilter === ALL_STATUSES ? undefined : { status: statusFilter }
    )
      .then((result) => {
        if (!isMounted) return;
        setRows(result);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setRows([]);
        setError(
          extractApiErrorMessage(err, 'Could not load the review queue. Please try again.')
        );
      });

    return () => {
      isMounted = false;
    };
  }, [statusFilter, reloadToken]);

  useEffect(() => {
    if (toast === null) return;

    const timer = window.setTimeout(() => setToast(null), TOAST_MS);
    return () => window.clearTimeout(timer);
  }, [toast]);

  const closeDrawer = useCallback(() => setReviewing(null), []);

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
    (report: PestDiseaseReport, decision: ReportReviewDecision) => {
      const farmer = reviewing?.reportedByUserName ?? 'the farmer';
      closeDrawer();
      handleRefresh();
      setToast(
        `${REPORT_REVIEW_DECISION_LABELS[decision]} recorded for report #${report.id} — ${farmer}.`
      );
    },
    [closeDrawer, handleRefresh, reviewing]
  );

  const rowsToShow = rows ?? [];

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
          <div className="pd-page-head">
            <div>
              <span className="eyebrow">PEST &amp; DISEASE MONITORING</span>
              <h1 className="pd-page-title">Diagnosis review queue</h1>
              <p className="pd-page-sub">
                Every pest or disease candidate the diagnosis agent found and the validator
                passed. Nothing here reaches a farmer as confirmed until you sign it off.
              </p>
            </div>

            <button className="pd-btn pd-btn-outline" onClick={handleRefresh}>
              <RefreshCw size={18} />
              Refresh
            </button>
          </div>

          <div className="pd-queue-controls">
            <div className="pd-form-group pd-queue-filter">
              <label htmlFor="pd-status-filter">Status</label>
              <select
                id="pd-status-filter"
                className="pd-input"
                value={statusFilter}
                onChange={(event) => {
                  setRows(null);
                  setError(null);
                  setStatusFilter(event.target.value as StatusFilter);
                }}
              >
                {STATUS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>

            {!isLoading && !error && (
              <p className="pd-queue-count">
                {rowsToShow.length} {rowsToShow.length === 1 ? 'report' : 'reports'}
              </p>
            )}
          </div>

          {isLoading && (
            <div className="pd-state">
              <p className="pd-state-text">Loading the review queue…</p>
            </div>
          )}

          {!isLoading && error && (
            <div className="pd-state pd-state-error" role="alert">
              <AlertCircle size={22} />
              <p className="pd-state-text">{error}</p>
              <button className="pd-btn pd-btn-quiet" onClick={handleRefresh}>
                Try again
              </button>
            </div>
          )}

          {!isLoading && !error && rowsToShow.length === 0 && (
            <div className="pd-state">
              <ClipboardCheck size={28} className="pd-state-icon" />
              <h2 className="pd-state-title">Nothing here</h2>
              <p className="pd-state-text">
                {statusFilter === 'PendingOfficerReview'
                  ? 'No diagnosis is awaiting review right now.'
                  : 'No report matches this filter right now.'}
              </p>
            </div>
          )}

          {!isLoading && !error && rowsToShow.length > 0 && (
            <div className="pd-table-wrap">
              <table className="pd-table">
                <caption className="pd-table-caption">Pest and disease diagnoses.</caption>
                <thead>
                  <tr>
                    <th scope="col">Farmer</th>
                    <th scope="col">Field</th>
                    <th scope="col">Possible issue</th>
                    <th scope="col">Confidence</th>
                    <th scope="col">Reported</th>
                    <th scope="col">
                      <span className="pd-sr-only">Review</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {rowsToShow.map((row) => (
                    <tr key={row.id}>
                      <td>
                        <span className="pd-table-strong">{row.reportedByUserName || '—'}</span>
                        <span className="pd-table-sub">
                          {OBSERVATION_TYPE_LABELS[row.observationType]} ·{' '}
                          {SEVERITY_LABELS[row.severity]} · {row.cropStage}
                        </span>
                      </td>
                      <td>{row.fieldName || '—'}</td>
                      <td>{row.possibleIssue}</td>
                      <td>{formatConfidence(row.confidence)}</td>
                      <td>{formatDateTime(row.createdAt)}</td>
                      <td className="pd-table-action">
                        <button
                          type="button"
                          className="pd-btn pd-btn-ghost"
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
        <div className="pd-drawer-backdrop" onClick={closeDrawer}>
          <aside
            className="pd-drawer"
            role="dialog"
            aria-modal="true"
            aria-labelledby="pd-drawer-title"
            onClick={(event) => event.stopPropagation()}
          >
            <header className="pd-drawer-head">
              <div>
                <span className="eyebrow">REVIEW</span>
                <h2 className="pd-drawer-title" id="pd-drawer-title">
                  {reviewing.reportedByUserName || 'Report'} · {reviewing.fieldName}
                </h2>
                <p className="pd-state-text" style={{ maxWidth: 'none' }}>
                  reported {formatDateTime(reviewing.createdAt)}
                </p>
              </div>

              <button className="pd-modal-close" onClick={closeDrawer} aria-label="Close">
                <X size={20} />
              </button>
            </header>

            <div className="pd-drawer-body">
              <dl className="pd-summary-grid">
                <div className="pd-summary-item">
                  <dt>Type</dt>
                  <dd>{OBSERVATION_TYPE_LABELS[reviewing.observationType]}</dd>
                </div>
                <div className="pd-summary-item">
                  <dt>Crop stage</dt>
                  <dd>{reviewing.cropStage}</dd>
                </div>
                <div className="pd-summary-item">
                  <dt>Severity</dt>
                  <dd>{SEVERITY_LABELS[reviewing.severity]}</dd>
                </div>
                <div className="pd-summary-item">
                  <dt>Possible issue</dt>
                  <dd>{reviewing.possibleIssue}</dd>
                </div>
                <div className="pd-summary-item">
                  <dt>Confidence</dt>
                  <dd>{formatConfidence(reviewing.confidence)}</dd>
                </div>
              </dl>

              <p className="pd-confidence-note">
                A possible match, not a confirmed diagnosis — the agent's confidence is never
                certainty.
              </p>

              <p className="pd-card-symptoms" style={{ marginTop: '1rem' }}>
                {reviewing.symptoms}
              </p>

              {reviewing.imageUrl && (
                <img
                  src={reviewing.imageUrl}
                  alt="Photo of the reported symptoms"
                  className="pd-image-preview"
                  style={{ marginTop: '1rem' }}
                />
              )}

              {reviewing.status === 'PendingOfficerReview' ? (
                <ReportReviewForm
                  reportId={reviewing.id}
                  onReviewed={handleReviewed}
                  onCancel={closeDrawer}
                />
              ) : (
                <p className="pd-card-symptoms" style={{ marginTop: '1.25rem' }}>
                  This report has already been reviewed, so there is nothing left to decide.
                  Refresh the queue to see what is still waiting.
                </p>
              )}
            </div>
          </aside>
        </div>
      )}

      {toast !== null && (
        <div className="pd-toast" role="status" aria-live="polite">
          <CheckCircle2 size={18} />
          <span>{toast}</span>
          <button className="pd-toast-close" onClick={() => setToast(null)} aria-label="Dismiss">
            <X size={16} />
          </button>
        </div>
      )}
    </div>
  );
}
