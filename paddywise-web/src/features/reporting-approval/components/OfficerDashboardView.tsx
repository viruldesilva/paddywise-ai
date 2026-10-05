import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { ShieldCheck, FileCheck, CheckCircle2, AlertCircle, RefreshCw, Loader2 } from 'lucide-react';
import { useOfficerDashboard } from '../hooks/useOfficerDashboard';
import { extractApiErrorMessage } from '../../../services/authService';

export const OfficerDashboardView: React.FC = () => {
  const {
    data,
    isLoading,
    isError,
    error,
    refetch,
    approveTreatment,
    requestDetails,
  } = useOfficerDashboard();

  const [actionLoadingId, setActionLoadingId] = useState<number | null>(null);
  const [actionMessage, setActionMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  const handleApprove = async (id: number) => {
    try {
      setActionLoadingId(id);
      setActionMessage(null);
      await approveTreatment(id);
      setActionMessage({ text: 'Treatment plan approved and recorded.', type: 'success' });
    } catch (err: unknown) {
      setActionMessage({
        text: extractApiErrorMessage(err, 'Failed to approve treatment.'),
        type: 'error',
      });
    } finally {
      setActionLoadingId(null);
    }
  };

  const handleRequestDetails = async (id: number) => {
    const comment = window.prompt(
      'Enter specific reason or instructions for requesting field details from the farmer:',
      'Please provide additional field photos and confirm water depth.'
    );
    if (!comment || !comment.trim()) return;

    try {
      setActionLoadingId(id);
      setActionMessage(null);
      await requestDetails({ reportId: id, comment });
      setActionMessage({ text: 'Revision requested and sent back to farmer.', type: 'success' });
    } catch (err: unknown) {
      setActionMessage({
        text: extractApiErrorMessage(err, 'Failed to request details.'),
        type: 'error',
      });
    } finally {
      setActionLoadingId(null);
    }
  };

  if (isError) {
    return (
      <div
        style={{
          background: '#fff',
          border: '1px solid var(--line)',
          borderRadius: '12px',
          padding: '2rem',
          textAlign: 'center',
          color: 'var(--ink)',
        }}
      >
        <AlertCircle size={32} color="#dc2626" style={{ margin: '0 auto 0.5rem' }} />
        <h3 style={{ fontSize: '1.25rem', marginBottom: '0.5rem' }}>Failed to load officer dashboard</h3>
        <p style={{ color: 'var(--ink-soft)', marginBottom: '1rem' }}>
          {extractApiErrorMessage(error, 'Could not retrieve data from the backend.')}
        </p>
        <button className="btn btn-primary btn-sm" onClick={() => refetch()}>
          <RefreshCw size={14} style={{ marginRight: '0.4rem' }} />
          Retry
        </button>
      </div>
    );
  }

  return (
    <div>
      {/* Top action feedback toast */}
      {actionMessage && (
        <div
          style={{
            marginBottom: '1rem',
            padding: '0.75rem 1rem',
            borderRadius: '8px',
            background: actionMessage.type === 'success' ? '#ecfdf5' : '#fef2f2',
            color: actionMessage.type === 'success' ? '#065f46' : '#991b1b',
            border: `1px solid ${actionMessage.type === 'success' ? '#a7f3d0' : '#fecaca'}`,
            fontSize: '0.875rem',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
          }}
        >
          {actionMessage.type === 'success' ? <CheckCircle2 size={16} /> : <AlertCircle size={16} />}
          {actionMessage.text}
        </div>
      )}

      {/* Metrics Row */}
      <div className="metrics-grid">
        {/* Card 1: Assigned Division */}
        <div className="metric-card">
          <span className="metric-label">Assigned Division</span>
          {isLoading ? (
            <div style={{ height: '2rem', background: 'var(--cream-deep)', borderRadius: '4px', animation: 'pulse 1.5s infinite' }} />
          ) : (
            <span className="metric-value" style={{ fontSize: '1.25rem' }}>
              {data?.assignedDivision.name || 'Not Assigned'}
            </span>
          )}
          <span className="metric-sub">
            {isLoading ? '…' : data?.assignedDivision.centre || 'Agrarian Services Centre'}
          </span>
        </div>

        {/* Card 2: Plans Awaiting Approval (Guaranteed to match queue count) */}
        <div className="metric-card">
          <span className="metric-label">Plans Awaiting Approval</span>
          {isLoading ? (
            <div style={{ height: '2rem', background: 'var(--cream-deep)', borderRadius: '4px', animation: 'pulse 1.5s infinite' }} />
          ) : (
            <span className="metric-value" style={{ color: '#d97706' }}>
              {data?.pendingApprovalCount ?? 0}
            </span>
          )}
          <Link className="metric-sub" to="/plans/pending">
            Review the queue
          </Link>
        </div>

        {/* Card 3: Approved Treatments */}
        <div className="metric-card">
          <span className="metric-label">Approved Treatments</span>
          {isLoading ? (
            <div style={{ height: '2rem', background: 'var(--cream-deep)', borderRadius: '4px', animation: 'pulse 1.5s infinite' }} />
          ) : (
            <span className="metric-value">{data?.approvedTreatmentsThisSeason ?? 0}</span>
          )}
          <span className="metric-sub">
            {isLoading ? '…' : data?.cultivationSeason || 'This cultivation season'}
          </span>
        </div>

        {/* Card 4: Registered Farmers */}
        <div className="metric-card">
          <span className="metric-label">Registered Farmers</span>
          {isLoading ? (
            <div style={{ height: '2rem', background: 'var(--cream-deep)', borderRadius: '4px', animation: 'pulse 1.5s infinite' }} />
          ) : (
            <span className="metric-value">{data?.registeredFarmerCount ?? 0}</span>
          )}
          <span className="metric-sub">
            {isLoading
              ? '…'
              : data?.gnDivisionCount != null
              ? `Across ${data.gnDivisionCount} GN divisions`
              : 'Registered in division'}
          </span>
        </div>
      </div>

      {/* Panels Grid */}
      <div className="dashboard-panels-grid">
        {/* Panel 1: Pending Treatment Approval Queue */}
        <div className="dashboard-panel">
          <div className="panel-header">
            <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <ShieldCheck size={20} color="var(--forest)" />
              Pending Treatment Approval Queue
            </h3>
          </div>

          {isLoading ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {[1, 2].map((i) => (
                <div
                  key={i}
                  style={{
                    padding: '1.25rem',
                    border: '1px solid var(--line)',
                    borderRadius: '8px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '0.75rem',
                  }}
                >
                  <div style={{ height: '1.25rem', width: '40%', background: 'var(--cream-deep)', borderRadius: '4px' }} />
                  <div style={{ height: '1rem', width: '80%', background: 'var(--cream-deep)', borderRadius: '4px' }} />
                  <div style={{ height: '1rem', width: '60%', background: 'var(--cream-deep)', borderRadius: '4px' }} />
                </div>
              ))}
            </div>
          ) : data?.pendingQueue && data.pendingQueue.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {data.pendingQueue.map((item) => (
                <div
                  key={item.id}
                  style={{ padding: '1rem', border: '1px solid var(--line)', borderRadius: '8px' }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                    <strong>Farmer: {item.farmerName}</strong>
                    <span className="badge-outline badge-farmer">{item.gnDivision}</span>
                  </div>
                  <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)' }}>
                    <strong>Symptom:</strong> {item.symptom}
                  </p>
                  <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)' }}>
                    <strong>AI Diagnosis:</strong> {item.aiDiagnosis} with {Math.round(item.confidence * 100)}% confidence.
                  </p>
                  <p style={{ fontSize: '0.875rem', color: 'var(--shoot)', marginTop: '0.25rem' }}>
                    <strong>Proposed Plan:</strong> {item.proposedPlan}
                  </p>
                  <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
                    <button
                      className="btn btn-primary btn-sm"
                      disabled={actionLoadingId === item.id}
                      onClick={() => handleApprove(item.id)}
                    >
                      {actionLoadingId === item.id ? (
                        <>
                          <Loader2 size={14} className="animate-spin" style={{ marginRight: '0.3rem' }} />
                          Approving…
                        </>
                      ) : (
                        'Approve Treatment'
                      )}
                    </button>
                    <button
                      className="btn btn-secondary btn-sm"
                      disabled={actionLoadingId === item.id}
                      onClick={() => handleRequestDetails(item.id)}
                    >
                      Request Details
                    </button>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ textAlign: 'center', padding: '2.5rem 1rem', color: 'var(--ink-soft)' }}>
              <CheckCircle2 size={36} color="var(--shoot)" style={{ margin: '0 auto 0.75rem' }} />
              <h4 style={{ fontWeight: 600, color: 'var(--ink)', marginBottom: '0.25rem' }}>
                No treatments awaiting approval
              </h4>
              <p style={{ fontSize: '0.875rem' }}>
                All AI-flagged pest and disease diagnoses in this division have been reviewed.
              </p>
            </div>
          )}
        </div>

        {/* Panel 2: Division Advisory Feed */}
        <div className="dashboard-panel">
          <div className="panel-header">
            <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <FileCheck size={18} />
              Division Advisory Feed
            </h3>
          </div>
          <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', lineHeight: 1.6 }}>
            {isLoading
              ? 'Checking division advisories…'
              : `No active disease alerts or weather advisories currently issued for the ${
                  data?.assignedDivision.name || 'selected'
                } division. Routine field inspection and telemetry monitoring remain active.`}
          </p>
        </div>
      </div>
    </div>
  );
};
