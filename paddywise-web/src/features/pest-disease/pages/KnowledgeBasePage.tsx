import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  BookOpen,
  CheckCircle2,
  Menu,
  Pencil,
  Plus,
  RefreshCw,
  Trash2,
  X,
  LogOut,
} from 'lucide-react';
import { Sidebar } from '../../../components/Sidebar';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import { KnowledgeEntryForm } from '../components/KnowledgeEntryForm';
import { deleteKnowledgeEntry, getKnowledgeEntries } from '../services/pestDiseaseApi';
import { PEST_DISEASE_CATEGORY_LABELS } from '../types';
import type { PestDiseaseCategory, PestDiseaseKnowledgeEntry } from '../types';
import { formatDateTime } from '../utils/dates';
import '../../../styles/Dashboard.css';
import '../styles/pestDisease.css';

const TOAST_MS = 5_000;

/** The filter's "every category" option, kept out of the category string union. */
const ALL_CATEGORIES = 'all';
type CategoryFilter = PestDiseaseCategory | typeof ALL_CATEGORIES;

const CATEGORY_FILTER_OPTIONS: readonly { value: CategoryFilter; label: string }[] = [
  { value: ALL_CATEGORIES, label: 'All categories' },
  { value: 'Pest', label: 'Pest' },
  { value: 'Disease', label: 'Disease' },
];

/**
 * Admin CRUD over the PestDiseaseKnowledge reference table — the same data
 * CropAnalysisAgent's get_pest_knowledge tool reads (read-only) when diagnosing an
 * observation. GET /api/pest-disease-knowledge is open to any authenticated caller,
 * but the mutating endpoints (POST/PUT/DELETE) are Admin only, so this page is too.
 */
export default function KnowledgeBasePage() {
  const { user, logout } = useAuth();

  const [entries, setEntries] = useState<PestDiseaseKnowledgeEntry[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState<CategoryFilter>(ALL_CATEGORIES);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  // Omitted = closed; {} = create; an entry = edit that entry.
  const [editing, setEditing] = useState<PestDiseaseKnowledgeEntry | 'new' | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const [reloadToken, setReloadToken] = useState(0);
  const isLoading = entries === null;

  useEffect(() => {
    let isMounted = true;

    getKnowledgeEntries()
      .then((result) => {
        if (!isMounted) return;
        setEntries(result);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setEntries([]);
        setError(
          extractApiErrorMessage(err, 'Could not load the knowledge base. Please try again.')
        );
      });

    return () => {
      isMounted = false;
    };
  }, [reloadToken]);

  useEffect(() => {
    if (toast === null) return;
    const timer = window.setTimeout(() => setToast(null), TOAST_MS);
    return () => window.clearTimeout(timer);
  }, [toast]);

  const handleRefresh = useCallback(() => {
    setEntries(null);
    setError(null);
    setReloadToken((previous) => previous + 1);
  }, []);

  const handleSaved = useCallback(
    (saved: PestDiseaseKnowledgeEntry) => {
      const wasEditing = editing !== 'new' && editing !== null;
      setEditing(null);
      handleRefresh();
      setToast(wasEditing ? `Updated "${saved.name}".` : `Added "${saved.name}".`);
    },
    [editing, handleRefresh]
  );

  const handleDelete = async (entry: PestDiseaseKnowledgeEntry) => {
    if (!window.confirm(`Delete "${entry.name}"? The diagnosis agent will no longer be able to match it.`)) {
      return;
    }

    setDeletingId(entry.id);
    try {
      await deleteKnowledgeEntry(entry.id);
      handleRefresh();
      setToast(`Deleted "${entry.name}".`);
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, 'Could not delete this entry. Please try again.'));
    } finally {
      setDeletingId(null);
    }
  };

  const rows = entries ?? [];
  const rowsToShow =
    categoryFilter === ALL_CATEGORIES ? rows : rows.filter((entry) => entry.category === categoryFilter);

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
              <h1 className="pd-page-title">Knowledge Base</h1>
              <p className="pd-page-sub">
                The pest and disease reference data the diagnosis agent matches against —
                it never names anything that isn't listed here. Keep entries sourced from
                the Department of Agriculture.
              </p>
            </div>

            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button className="pd-btn pd-btn-outline" onClick={handleRefresh}>
                <RefreshCw size={18} />
                Refresh
              </button>
              <button className="pd-btn pd-btn-primary" onClick={() => setEditing('new')}>
                <Plus size={18} />
                Add entry
              </button>
            </div>
          </div>

          {!isLoading && !error && rows.length > 0 && (
            <div className="pd-queue-controls">
              <div className="pd-form-group pd-queue-filter">
                <label htmlFor="pd-kb-category-filter">Category</label>
                <select
                  id="pd-kb-category-filter"
                  className="pd-input"
                  value={categoryFilter}
                  onChange={(event) => setCategoryFilter(event.target.value as CategoryFilter)}
                >
                  {CATEGORY_FILTER_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          {isLoading && (
            <div className="pd-state">
              <p className="pd-state-text">Loading the knowledge base…</p>
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

          {!isLoading && !error && rows.length === 0 && (
            <div className="pd-state">
              <BookOpen size={28} className="pd-state-icon" />
              <h2 className="pd-state-title">No entries yet</h2>
              <p className="pd-state-text">
                The diagnosis agent has nothing to match against until at least one entry
                exists.
              </p>
              <button className="pd-btn pd-btn-primary" onClick={() => setEditing('new')}>
                <Plus size={18} />
                Add the first entry
              </button>
            </div>
          )}

          {!isLoading && !error && rows.length > 0 && rowsToShow.length === 0 && (
            <div className="pd-state">
              <BookOpen size={28} className="pd-state-icon" />
              <h2 className="pd-state-title">Nothing here</h2>
              <p className="pd-state-text">No entry matches this filter.</p>
            </div>
          )}

          {!isLoading && !error && rowsToShow.length > 0 && (
            <div className="pd-table-wrap">
              <table className="pd-table">
                <caption className="pd-table-caption">
                  Pest and disease knowledge base entries.
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Name</th>
                    <th scope="col">Category</th>
                    <th scope="col">Symptoms</th>
                    <th scope="col">Source</th>
                    <th scope="col">Updated</th>
                    <th scope="col">
                      <span className="pd-sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {rowsToShow.map((entry) => (
                    <tr key={entry.id}>
                      <td>
                        <span className="pd-table-strong">{entry.name}</span>
                        {entry.cropStages && (
                          <span className="pd-table-sub">{entry.cropStages}</span>
                        )}
                      </td>
                      <td>{PEST_DISEASE_CATEGORY_LABELS[entry.category]}</td>
                      <td>
                        <span className="pd-table-sub" style={{ whiteSpace: 'normal' }}>
                          {entry.symptoms}
                        </span>
                      </td>
                      <td>{entry.source}</td>
                      <td>{formatDateTime(entry.updatedAt)}</td>
                      <td className="pd-table-action">
                        <button
                          type="button"
                          className="pd-btn pd-btn-ghost"
                          onClick={() => setEditing(entry)}
                        >
                          <Pencil size={14} />
                          Edit
                        </button>
                        <button
                          type="button"
                          className="pd-btn pd-btn-ghost"
                          onClick={() => handleDelete(entry)}
                          disabled={deletingId === entry.id}
                          style={{ marginLeft: '0.5rem', color: 'var(--clay)' }}
                        >
                          <Trash2 size={14} />
                          {deletingId === entry.id ? 'Deleting…' : 'Delete'}
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

      {editing !== null && (
        <div
          className="pd-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-labelledby="pd-kb-modal-title"
        >
          <div className="pd-modal">
            <header className="pd-modal-head">
              <h2 className="pd-modal-title" id="pd-kb-modal-title">
                {editing === 'new' ? 'Add a knowledge base entry' : `Edit "${editing.name}"`}
              </h2>
              <button
                className="pd-modal-close"
                onClick={() => setEditing(null)}
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </header>

            <KnowledgeEntryForm
              entry={editing === 'new' ? undefined : editing}
              onSaved={handleSaved}
              onCancel={() => setEditing(null)}
            />
          </div>
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
