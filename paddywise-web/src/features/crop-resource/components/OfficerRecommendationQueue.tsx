import React, { useState, useEffect, useMemo } from 'react';
import {
  CheckCircle2,
  XCircle,
  Search,
  Layers,
  Calendar,
  BookOpen,
  Info,
  ShieldAlert,
  Loader2,
  RefreshCw,
  MessageSquare,
  UserCheck
} from 'lucide-react';
import {
  cropAnalysisApi,
  type OfficerRecommendationReviewDto
} from '../services/cropAnalysisApi';
import './OfficerRecommendationQueue.css';

interface OfficerRecommendationQueueProps {
  officerName: string;
  onReviewed?: () => void;
}

export const OfficerRecommendationQueue: React.FC<OfficerRecommendationQueueProps> = ({
  officerName: _officerName,
  onReviewed
}) => {
  const [recommendations, setRecommendations] = useState<OfficerRecommendationReviewDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('ALL');
  const [priorityFilter, setPriorityFilter] = useState('ALL');
  const [statusTab, setStatusTab] = useState<'PENDING' | 'ALL'>('PENDING');

  // Input states for each recommendation card (keyed by rec.id)
  const [comments, setComments] = useState<Record<number, string>>({});
  const [submittingId, setSubmittingId] = useState<number | null>(null);

  const fetchRecommendations = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await cropAnalysisApi.getPendingOfficerRecommendations();
      setRecommendations(data);
    } catch (err: any) {
      console.error('Failed to load pending recommendations', err);
      setError(err?.response?.data?.message || 'Failed to fetch recommendations queue.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchRecommendations();
  }, []);

  const handlePresetComment = (id: number, text: string) => {
    setComments(prev => ({
      ...prev,
      [id]: prev[id] ? `${prev[id]}. ${text}` : text
    }));
  };

  const handleReviewDecision = async (
    id: number,
    decision: 'Approve' | 'Reject'
  ) => {
    try {
      setSubmittingId(id);
      const commentText = comments[id] || (decision === 'Approve' ? 'Approved by Agricultural Officer.' : 'Rejected by Agricultural Officer.');
      const updated = await cropAnalysisApi.officerReviewRecommendation(id, decision, commentText);

      // Update in local state
      setRecommendations(prev =>
        prev.map(r => (r.id === id ? updated : r))
      );

      if (onReviewed) {
        onReviewed();
      }
    } catch (err: any) {
      console.error('Failed to submit officer review', err);
      alert(err?.response?.data?.message || 'Failed to submit review. Please try again.');
    } finally {
      setSubmittingId(null);
    }
  };

  // Filter recommendations
  const filteredList = useMemo(() => {
    return recommendations.filter(rec => {
      // Status filter
      if (statusTab === 'PENDING' && rec.status !== 'PENDING_OFFICER_REVIEW') {
        return false;
      }

      // Category filter
      if (categoryFilter !== 'ALL' && rec.category.toLowerCase() !== categoryFilter.toLowerCase()) {
        return false;
      }

      // Priority filter
      if (priorityFilter !== 'ALL' && rec.priority.toUpperCase() !== priorityFilter.toUpperCase()) {
        return false;
      }

      // Search query
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase();
        const matchesFarmer = (rec.farmerName || '').toLowerCase().includes(q);
        const matchesField = (rec.fieldName || '').toLowerCase().includes(q);
        const matchesAction = (rec.action || '').toLowerCase().includes(q);
        const matchesDivision = (rec.divisionName || '').toLowerCase().includes(q);
        const matchesVariety = (rec.varietyName || '').toLowerCase().includes(q);
        if (!matchesFarmer && !matchesField && !matchesAction && !matchesDivision && !matchesVariety) {
          return false;
        }
      }

      return true;
    });
  }, [recommendations, statusTab, categoryFilter, priorityFilter, searchQuery]);

  const pendingCount = recommendations.filter(r => r.status === 'PENDING_OFFICER_REVIEW').length;
  const approvedCount = recommendations.filter(r => r.status === 'APPROVED').length;
  const rejectedCount = recommendations.filter(r => r.status === 'REJECTED').length;

  return (
    <div className="officer-queue-container">
      {/* Header Banner */}
      <div className="officer-queue-header">
        <div className="officer-queue-title-group">
          <div className="queue-badge-count">
            <UserCheck size={14} /> Official Officer Approvals
          </div>
          <h2>Agricultural Officer Recommendation Review</h2>
          <p>
            Review and validate Agentic AI recommendations generated for farmers in your division.
            Approved instructions are sent directly to the farmer's portal for field execution.
          </p>
        </div>

        <div className="queue-stats-pills">
          <div className="queue-stat-pill">
            <span className="queue-stat-num">{pendingCount}</span>
            <span className="queue-stat-label">Pending Review</span>
          </div>
          <div className="queue-stat-pill">
            <span className="queue-stat-num">{approvedCount}</span>
            <span className="queue-stat-label">Approved</span>
          </div>
          <div className="queue-stat-pill">
            <span className="queue-stat-num">{rejectedCount}</span>
            <span className="queue-stat-label">Rejected</span>
          </div>
        </div>
      </div>

      {/* Controls Bar */}
      <div className="officer-queue-controls">
        <div className="queue-search-input">
          <Search size={16} className="queue-search-icon" />
          <input
            type="text"
            placeholder="Search by farmer, field, variety, or action..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
          />
        </div>

        <div className="queue-filters">
          <div style={{ display: 'flex', gap: '0.4rem' }}>
            <button
              type="button"
              className={`preset-chip ${statusTab === 'PENDING' ? 'active' : ''}`}
              style={{
                background: statusTab === 'PENDING' ? 'var(--forest)' : 'white',
                color: statusTab === 'PENDING' ? 'white' : '#475569',
                borderColor: statusTab === 'PENDING' ? '#059669' : '#cbd5e1',
                padding: '0.45rem 0.9rem',
                fontSize: '0.82rem',
                fontWeight: 600
              }}
              onClick={() => setStatusTab('PENDING')}
            >
              Pending ({pendingCount})
            </button>
            <button
              type="button"
              className={`preset-chip ${statusTab === 'ALL' ? 'active' : ''}`}
              style={{
                background: statusTab === 'ALL' ? 'var(--forest)' : 'white',
                color: statusTab === 'ALL' ? 'white' : '#475569',
                borderColor: statusTab === 'ALL' ? '#059669' : '#cbd5e1',
                padding: '0.45rem 0.9rem',
                fontSize: '0.82rem',
                fontWeight: 600
              }}
              onClick={() => setStatusTab('ALL')}
            >
              All Records ({recommendations.length})
            </button>
          </div>

          <select
            className="queue-filter-select"
            value={categoryFilter}
            onChange={e => setCategoryFilter(e.target.value)}
          >
            <option value="ALL">All Categories</option>
            <option value="Irrigation">Irrigation</option>
            <option value="Fertilizer">Fertilizer</option>
            <option value="Pest">Pest & Disease</option>
            <option value="General">General Agronomy</option>
          </select>

          <select
            className="queue-filter-select"
            value={priorityFilter}
            onChange={e => setPriorityFilter(e.target.value)}
          >
            <option value="ALL">All Priorities</option>
            <option value="HIGH">High Priority</option>
            <option value="MEDIUM">Medium Priority</option>
            <option value="LOW">Low Priority</option>
          </select>

          <button
            type="button"
            className="preset-chip"
            style={{ padding: '0.55rem 0.75rem', display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}
            onClick={fetchRecommendations}
            title="Refresh recommendations"
          >
            <RefreshCw size={14} className={isLoading ? 'spinner' : ''} />
            Refresh
          </button>
        </div>
      </div>

      {/* Content Area */}
      {isLoading ? (
        <div className="queue-empty-card">
          <Loader2 size={36} className="spinner" style={{ color: '#059669' }} />
          <h3>Fetching Pending AI Recommendations...</h3>
          <p>Connecting to agrarian division records and auditing AI recommendations queue.</p>
        </div>
      ) : error ? (
        <div className="advisor-error-banner">
          <ShieldAlert size={20} />
          <div>{error}</div>
        </div>
      ) : filteredList.length === 0 ? (
        <div className="queue-empty-card">
          <CheckCircle2 size={44} style={{ color: 'var(--forest)' }} />
          <h3>No Recommendations In Queue</h3>
          <p>
            {statusTab === 'PENDING'
              ? 'All AI agronomic recommendations have been reviewed and approved or rejected for this division!'
              : 'No recommendation records match your current filter settings.'}
          </p>
        </div>
      ) : (
        <div className="queue-cards-list">
          {filteredList.map(rec => {
            const isSubmitting = submittingId === rec.id;
            const isPending = rec.status === 'PENDING_OFFICER_REVIEW';
            const isApproved = rec.status === 'APPROVED';
            const isRejected = rec.status === 'REJECTED';

            return (
              <div
                key={rec.id}
                className={`queue-card ${isPending ? 'card-pending' : isApproved ? 'card-approved' : 'card-rejected'}`}
              >
                {/* Farmer & Field Meta Header */}
                <div className="queue-card-meta">
                  <div className="farmer-info-group">
                    <div className="farmer-avatar-badge">
                      {(rec.farmerName || 'F').charAt(0).toUpperCase()}
                    </div>
                    <div className="farmer-names">
                      <h4>{rec.farmerName || '—'}</h4>
                      <div className="farmer-sub-details">
                        <span>
                          <Layers size={12} style={{ display: 'inline', marginRight: '3px' }} />
                          <strong>{rec.fieldName || '—'}</strong>
                        </span>
                        <span>•</span>
                        <span>{rec.divisionName || '—'}</span>
                        <span>•</span>
                        <span>{rec.varietyName || '—'}</span>
                        <span>•</span>
                        <span>
                          <Calendar size={12} style={{ display: 'inline', marginRight: '3px' }} />
                          DAS: {rec.daysAfterSowing}d
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="queue-pills-row">
                    <span className={`badge-tag priority-${(rec.priority || 'low').toLowerCase()}`}>
                      {rec.priority} Priority
                    </span>
                    <span className="badge-tag category-pill">{rec.category}</span>
                    <span
                      className={`badge-tag ${isPending
                        ? 'status-pending'
                        : isApproved
                          ? 'status-approved'
                          : 'status-rejected'
                        }`}
                    >
                      {isPending
                        ? 'Pending Officer Review'
                        : isApproved
                          ? 'Approved'
                          : 'Rejected'}
                    </span>
                  </div>
                </div>

                {/* Recommendation Details */}
                <div className="queue-card-content">
                  <h3 className="queue-action-title">{rec.action}</h3>
                  <p className="queue-reason-text">{rec.reason}</p>

                  {rec.evidence && (
                    <div className="queue-evidence-box">
                      <Info size={16} style={{ flexShrink: 0, marginTop: '2px', color: '#0284c7' }} />
                      <div>
                        <strong>Activity Log Evidence: </strong>
                        <span>{rec.evidence}</span>
                      </div>
                    </div>
                  )}

                  {rec.citations && rec.citations.length > 0 && (
                    <div className="queue-citations-row">
                      <BookOpen size={14} />
                      <span>
                        <strong>DOA Reference: </strong>
                        {rec.citations.map(c => `${c.document} (${c.section})`).join('; ')}
                      </span>
                    </div>
                  )}
                </div>

                {/* Officer Decision Box or Summary */}
                {isPending ? (
                  <div className="officer-review-box">
                    <h5>
                      <MessageSquare size={15} /> Officer Technical Guidance & Approval Decision
                    </h5>

                    {/* Presets */}
                    <div className="preset-comments">
                      <span style={{ fontSize: '0.75rem', color: '#78716c', alignSelf: 'center' }}>
                        Quick Presets:
                      </span>
                      <button
                        type="button"
                        className="preset-chip"
                        onClick={() =>
                          handlePresetComment(rec.id, 'Approved according to Sri Lanka DOA handbook')
                        }
                      >
                        + Approved as per DOA
                      </button>
                      <button
                        type="button"
                        className="preset-chip"
                        onClick={() =>
                          handlePresetComment(rec.id, 'Broadcast on moist soil in early morning; avoid standing water')
                        }
                      >
                        + Early Morning / Moist Soil
                      </button>
                      <button
                        type="button"
                        className="preset-chip"
                        onClick={() =>
                          handlePresetComment(rec.id, 'Postpone until rainfall ceases')
                        }
                      >
                        + Postpone for Rain
                      </button>
                    </div>

                    <textarea
                      className="officer-comment-input"
                      placeholder="Add technical instructions or remarks for the farmer (optional)..."
                      value={comments[rec.id] || ''}
                      onChange={e =>
                        setComments(prev => ({ ...prev, [rec.id]: e.target.value }))
                      }
                      disabled={isSubmitting}
                    />

                    <div className="officer-btn-group">
                      <button
                        type="button"
                        className="btn-reject"
                        onClick={() => handleReviewDecision(rec.id, 'Reject')}
                        disabled={isSubmitting}
                      >
                        {isSubmitting ? (
                          <Loader2 size={16} className="spinner" />
                        ) : (
                          <XCircle size={16} />
                        )}
                        Reject Recommendation
                      </button>

                      <button
                        type="button"
                        className="btn-approve"
                        onClick={() => handleReviewDecision(rec.id, 'Approve')}
                        disabled={isSubmitting}
                      >
                        {isSubmitting ? (
                          <Loader2 size={16} className="spinner" />
                        ) : (
                          <CheckCircle2 size={16} />
                        )}
                        Approve & Send to Farmer
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className={`reviewed-summary-box ${isRejected ? 'rejected' : ''}`}>
                    {isApproved ? (
                      <CheckCircle2 size={18} style={{ color: '#16a34a', flexShrink: 0, marginTop: '2px' }} />
                    ) : (
                      <XCircle size={18} style={{ color: '#dc2626', flexShrink: 0, marginTop: '2px' }} />
                    )}
                    <div>
                      <strong>
                        {isApproved ? 'Approved by ' : 'Rejected by '}
                        {rec.officerName || 'Agricultural Officer'}
                      </strong>
                      {rec.reviewedAt && (
                        <span style={{ fontSize: '0.8rem', opacity: 0.8, marginLeft: '0.5rem' }}>
                          on {new Date(rec.reviewedAt).toLocaleDateString()} at{' '}
                          {new Date(rec.reviewedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                      )}
                      <p style={{ margin: '0.25rem 0 0', fontStyle: 'italic' }}>
                        "{rec.officerComment || (isApproved ? 'Approved for execution.' : 'Rejected.')}"
                      </p>
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
