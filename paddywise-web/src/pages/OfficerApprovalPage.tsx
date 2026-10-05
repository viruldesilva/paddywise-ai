import { useState, useEffect, useCallback } from 'react';
import { Sidebar } from '../components/Sidebar';
import { useAuth } from '../hooks/useAuth';
import { adminService } from '../services/adminService';
import type { OfficerRequestDto } from '../types/auth';
import {
  Menu,
  LogOut,
  Search,
  Check,
  X,
  Clock,
  ShieldCheck,
  RefreshCw,
  AlertCircle,
  CheckCircle,
  Mail,
  Phone,
  Calendar,
} from 'lucide-react';
import '../styles/Dashboard.css';
import '../styles/UserManagement.css';

export default function OfficerApprovalPage() {
  const { user, logout } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [requests, setRequests] = useState<OfficerRequestDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const [processingId, setProcessingId] = useState<number | null>(null);

  // Reject modal state
  const [rejectingOfficer, setRejectingOfficer] = useState<OfficerRequestDto | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const fetchRequests = useCallback(async () => {
    setIsLoading(true);
    setActionError(null);
    try {
      const data = await adminService.getPendingOfficerRequests();
      setRequests(data);
    } catch {
      setActionError('Failed to load pending officer requests. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRequests();
  }, [fetchRequests]);

  const handleApprove = async (officer: OfficerRequestDto) => {
    setProcessingId(officer.id);
    setActionError(null);
    setActionSuccess(null);
    try {
      await adminService.approveOfficerRequest(officer.id);
      setActionSuccess(`Officer ${officer.name} (${officer.email}) has been approved. A confirmation email has been dispatched to ${officer.email}.`);
      setRequests((prev) => prev.filter((r) => r.id !== officer.id));
    } catch {
      setActionError(`Failed to approve officer ${officer.name}. Please try again.`);
    } finally {
      setProcessingId(null);
    }
  };

  const handleOpenReject = (officer: OfficerRequestDto) => {
    setRejectingOfficer(officer);
    setRejectReason('');
  };

  const handleConfirmReject = async () => {
    if (!rejectingOfficer) return;
    setProcessingId(rejectingOfficer.id);
    setActionError(null);
    setActionSuccess(null);
    try {
      await adminService.rejectOfficerRequest(rejectingOfficer.id, rejectReason.trim());
      setActionSuccess(`Application for ${rejectingOfficer.name} has been rejected.`);
      setRequests((prev) => prev.filter((r) => r.id !== rejectingOfficer.id));
      setRejectingOfficer(null);
    } catch {
      setActionError(`Failed to reject application for ${rejectingOfficer.name}.`);
    } finally {
      setProcessingId(null);
    }
  };

  const filteredRequests = requests.filter(
    (r) =>
      r.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      r.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (r.phone && r.phone.toLowerCase().includes(searchQuery.toLowerCase()))
  );

  return (
    <div className="dashboard-layout">
      <Sidebar role="Admin" isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

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
              <h2 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--ink)' }}>
                Admin Security & Approval Gate
              </h2>
            </div>

            <div className="dashboard-user-meta">
              <div className="dashboard-user-greeting">
                <span className="dashboard-user-name">{user?.name}</span>
                <span className="dashboard-user-sub">{user?.email}</span>
              </div>
              <span className="role-badge-tag badge-admin">Admin</span>
              <button
                onClick={() => logout()}
                className="btn btn-secondary btn-sm"
                title="Sign Out"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}
              >
                <LogOut size={16} />
                <span className="hide-mobile">Sign Out</span>
              </button>
            </div>
          </div>
        </header>

        <main className="dashboard-content container">
          <div className="um-header-actions">
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.25rem' }}>
                <ShieldCheck size={26} color="var(--forest)" />
                <h1 className="dashboard-welcome-title" style={{ margin: 0 }}>
                  Agricultural Officer Approvals
                </h1>
              </div>
              <p className="dashboard-welcome-desc">
                Review and approve or reject pending Agricultural Officer registrations. Approving an account immediately grants system access and dispatches an official confirmation email.
              </p>
            </div>
            <button
              className="btn btn-secondary"
              onClick={fetchRequests}
              disabled={isLoading}
              style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem' }}
            >
              <RefreshCw size={16} className={isLoading ? 'spin-icon' : ''} />
              Refresh
            </button>
          </div>

          {actionSuccess && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.75rem',
                backgroundColor: '#dcfce7',
                border: '1px solid #bbf7d0',
                color: '#166534',
                padding: '0.875rem 1.25rem',
                borderRadius: '8px',
                fontSize: '0.9rem',
                marginBottom: '1.5rem',
              }}
            >
              <CheckCircle size={20} style={{ flexShrink: 0 }} />
              <span>{actionSuccess}</span>
            </div>
          )}

          {actionError && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.75rem',
                backgroundColor: '#fee2e2',
                border: '1px solid #fecaca',
                color: '#991b1b',
                padding: '0.875rem 1.25rem',
                borderRadius: '8px',
                fontSize: '0.9rem',
                marginBottom: '1.5rem',
              }}
            >
              <AlertCircle size={20} style={{ flexShrink: 0 }} />
              <span>{actionError}</span>
            </div>
          )}

          <div className="dashboard-panel">
            <div className="um-toolbar">
              <div className="um-search-box">
                <Search size={18} className="um-search-icon" />
                <input
                  type="text"
                  placeholder="Search requests by officer name, email, or phone..."
                  className="um-search-input"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--ink-soft)', fontSize: '0.875rem' }}>
                <Clock size={16} />
                <span>{requests.length} pending request{requests.length === 1 ? '' : 's'}</span>
              </div>
            </div>

            {isLoading ? (
              <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--ink-soft)' }}>
                <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem' }}>
                  <RefreshCw size={20} className="spin-icon" />
                  <span>Loading pending officer requests...</span>
                </div>
              </div>
            ) : filteredRequests.length === 0 ? (
              <div style={{ padding: '3.5rem 1.5rem', textAlign: 'center' }}>
                <div
                  style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    width: '64px',
                    height: '64px',
                    borderRadius: '50%',
                    backgroundColor: '#f0fdf4',
                    color: '#16a34a',
                    marginBottom: '1rem',
                  }}
                >
                  <CheckCircle size={32} />
                </div>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--ink)', marginBottom: '0.4rem' }}>
                  {searchQuery ? 'No matching requests found' : 'All clear! No pending officer applications'}
                </h3>
                <p style={{ color: 'var(--ink-soft)', fontSize: '0.9rem', maxWidth: '420px', margin: '0 auto' }}>
                  {searchQuery
                    ? `No requests match "${searchQuery}". Clear your search term to see all pending applications.`
                    : 'Any new Agricultural Officer registrations requiring verification will appear here for review.'}
                </p>
              </div>
            ) : (
              <div className="user-table-wrapper">
                <table className="user-table">
                  <thead>
                    <tr>
                      <th>Officer Name / Email</th>
                      <th>Contact Phone</th>
                      <th>Application Date</th>
                      <th>Account Status</th>
                      <th style={{ textAlign: 'right' }}>Review Decision</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredRequests.map((officer) => (
                      <tr key={officer.id}>
                        <td>
                          <div style={{ fontWeight: 600, color: 'var(--ink)' }}>{officer.name}</div>
                          <div style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', marginTop: '0.2rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                            <Mail size={13} />
                            <span>{officer.email}</span>
                          </div>
                        </td>
                        <td>
                          {officer.phone ? (
                            <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem', color: 'var(--ink)' }}>
                              <Phone size={13} color="var(--ink-soft)" />
                              {officer.phone}
                            </span>
                          ) : (
                            <span style={{ color: 'var(--ink-soft)', fontStyle: 'italic', fontSize: '0.85rem' }}>Not provided</span>
                          )}
                        </td>
                        <td>
                          <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.875rem', color: 'var(--ink)' }}>
                            <Calendar size={13} color="var(--ink-soft)" />
                            {new Date(officer.createdAt).toLocaleDateString(undefined, {
                              year: 'numeric',
                              month: 'short',
                              day: 'numeric',
                            })}
                          </div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--ink-soft)', marginTop: '0.15rem' }}>
                            {new Date(officer.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </div>
                        </td>
                        <td>
                          <span
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '0.35rem',
                              backgroundColor: '#fef3c7',
                              color: '#92400e',
                              padding: '0.25rem 0.65rem',
                              borderRadius: '9999px',
                              fontSize: '0.8125rem',
                              fontWeight: 500,
                              border: '1px solid #fde68a',
                            }}
                          >
                            <Clock size={12} />
                            Pending Approval
                          </span>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                            <button
                              className="btn btn-sm"
                              style={{
                                backgroundColor: '#15803d',
                                color: '#ffffff',
                                border: 'none',
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: '0.35rem',
                                padding: '0.4rem 0.85rem',
                                borderRadius: '6px',
                                cursor: processingId === officer.id ? 'not-allowed' : 'pointer',
                                opacity: processingId === officer.id ? 0.7 : 1,
                              }}
                              onClick={() => handleApprove(officer)}
                              disabled={processingId === officer.id}
                              title="Approve officer account and send confirmation email"
                            >
                              <Check size={14} />
                              <span>{processingId === officer.id ? 'Approving...' : 'Approve'}</span>
                            </button>

                            <button
                              className="btn btn-sm"
                              style={{
                                backgroundColor: '#fee2e2',
                                color: '#991b1b',
                                border: '1px solid #fecaca',
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: '0.35rem',
                                padding: '0.4rem 0.85rem',
                                borderRadius: '6px',
                                cursor: processingId === officer.id ? 'not-allowed' : 'pointer',
                              }}
                              onClick={() => handleOpenReject(officer)}
                              disabled={processingId === officer.id}
                              title="Reject officer account application"
                            >
                              <X size={14} />
                              <span>Reject</span>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </main>
      </div>

      {/* Reject Confirmation Modal */}
      {rejectingOfficer && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '1rem',
          }}
        >
          <div
            style={{
              backgroundColor: '#ffffff',
              borderRadius: '12px',
              maxWidth: '480px',
              width: '100%',
              padding: '1.75rem',
              boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
              <div
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '40px',
                  height: '40px',
                  borderRadius: '50%',
                  backgroundColor: '#fee2e2',
                  color: '#dc2626',
                }}
              >
                <X size={20} />
              </div>
              <div>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 600, color: 'var(--ink)', margin: 0 }}>
                  Reject Officer Application
                </h3>
                <p style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', margin: 0 }}>
                  {rejectingOfficer.name} ({rejectingOfficer.email})
                </p>
              </div>
            </div>

            <p style={{ fontSize: '0.9rem', color: 'var(--ink)', lineHeight: '1.5', marginBottom: '1rem' }}>
              Are you sure you want to reject this Agricultural Officer registration? The account will remain blocked from logging in.
            </p>

            <div style={{ marginBottom: '1.5rem' }}>
              <label
                htmlFor="reject-reason"
                style={{ display: 'block', fontSize: '0.85rem', fontWeight: 500, color: 'var(--ink)', marginBottom: '0.35rem' }}
              >
                Reason for Rejection (Optional)
              </label>
              <textarea
                id="reject-reason"
                rows={3}
                placeholder="e.g. Invalid DOA extension credentials or unverified division..."
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                style={{
                  width: '100%',
                  padding: '0.65rem 0.85rem',
                  border: '1px solid var(--line)',
                  borderRadius: '8px',
                  fontSize: '0.875rem',
                  fontFamily: 'inherit',
                  resize: 'vertical',
                }}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => setRejectingOfficer(null)}
                disabled={processingId === rejectingOfficer.id}
              >
                Cancel
              </button>
              <button
                type="button"
                className="btn"
                style={{
                  backgroundColor: '#dc2626',
                  color: '#ffffff',
                  border: 'none',
                  padding: '0.5rem 1.25rem',
                  borderRadius: '8px',
                  fontWeight: 500,
                  cursor: 'pointer',
                }}
                onClick={handleConfirmReject}
                disabled={processingId === rejectingOfficer.id}
              >
                {processingId === rejectingOfficer.id ? 'Rejecting...' : 'Confirm Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
