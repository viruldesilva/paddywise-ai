import React from 'react';
import { Link } from 'react-router-dom';
import { useAdminDashboard } from '../hooks/useAdminDashboard';
import { useAuth } from '../hooks/useAuth';
import { API_BASE_URL } from '../api/axiosInstance';
import {
  Server,
  KeyRound,
  Database,
  Users,
  UserCheck,
  Activity,
  ArrowRight,
  ShieldCheck,
  RefreshCw,
} from 'lucide-react';

export const AdminDashboardView: React.FC = () => {
  const { user } = useAuth();
  const {
    health,
    isHealthLoading,
    isHealthError,
    dashboard,
    isDashboardLoading,
    refetchHealth,
    refetchDashboard,
  } = useAdminDashboard();

  const isHealthy = !isHealthError && health?.status === 'Healthy';
  const isDbHealthy = !isHealthError && health?.database === 'Healthy';

  const getStatusColor = (healthy: boolean | undefined, loading: boolean) => {
    if (loading) return 'var(--ink-soft)';
    return healthy ? '#2e7d32' : '#dc2626';
  };

  const handleRefreshAll = () => {
    refetchHealth();
    refetchDashboard();
  };

  return (
    <div>
      {/* Real Backend & DB Status Metrics */}
      <div className="metrics-grid">
        {/* Metric 1: Backend API Health */}
        <div className="metric-card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <span className="metric-label">Backend Health</span>
            <Activity size={18} color="var(--forest)" />
          </div>
          <span
            className="metric-value"
            style={{
              fontSize: '1.25rem',
              color: getStatusColor(isHealthy, isHealthLoading),
            }}
          >
            {isHealthLoading
              ? 'Checking...'
              : isHealthy
              ? 'Healthy (Ready)'
              : 'Unavailable'}
          </span>
          <span className="metric-sub">
            {isHealthLoading
              ? 'Polling /api/health'
              : health?.totalDurationMs !== undefined
              ? `${Math.round(health.totalDurationMs)}ms probe latency`
              : 'ASP.NET Core Web API'}
          </span>
        </div>

        {/* Metric 2: PostgreSQL Database */}
        <div className="metric-card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <span className="metric-label">Target Database</span>
            <Database size={18} color="var(--forest)" />
          </div>
          <span
            className="metric-value"
            style={{
              fontSize: '1.25rem',
              color: getStatusColor(isDbHealthy, isHealthLoading),
            }}
          >
            {isHealthLoading
              ? 'Checking...'
              : isDbHealthy
              ? 'PostgreSQL Connected'
              : 'Disconnected'}
          </span>
          <span className="metric-sub">
            {isDbHealthy ? 'Neon DB / Cloud PostgreSQL' : 'Database connection error'}
          </span>
        </div>

        {/* Metric 3: Total Registered Users */}
        <div className="metric-card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <span className="metric-label">Registered Accounts</span>
            <Users size={18} color="var(--forest)" />
          </div>
          <span className="metric-value" style={{ fontSize: '1.25rem' }}>
            {isDashboardLoading ? '...' : `${dashboard?.totalUsers ?? 0} Users`}
          </span>
          <span className="metric-sub">
            {isDashboardLoading
              ? 'Loading stats...'
              : `${dashboard?.activeUsers ?? 0} active • ${dashboard?.inactiveUsers ?? 0} inactive`}
          </span>
        </div>

        {/* Metric 4: Pending Officer Approvals */}
        <div className="metric-card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <span className="metric-label">Officer Queue</span>
            <UserCheck size={18} color="var(--forest)" />
          </div>
          <span
            className="metric-value"
            style={{
              fontSize: '1.25rem',
              color: (dashboard?.pendingOfficerApprovals ?? 0) > 0 ? '#d97706' : '#2e7d32',
            }}
          >
            {isDashboardLoading
              ? '...'
              : `${dashboard?.pendingOfficerApprovals ?? 0} Pending`}
          </span>
          <span className="metric-sub">
            {(dashboard?.pendingOfficerApprovals ?? 0) > 0
              ? 'Awaiting review'
              : 'All officers verified'}
          </span>
        </div>
      </div>

      {/* Role Distribution Panel & Fast Actions */}
      <div className="dashboard-panel" style={{ marginTop: '1.5rem', marginBottom: '1.5rem' }}>
        <div className="panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Users size={20} color="var(--forest)" />
            Live Platform User Breakdown
          </h3>
          <button
            onClick={handleRefreshAll}
            className="btn btn-secondary btn-sm"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}
            title="Refresh system metrics"
          >
            <RefreshCw size={14} />
            Refresh
          </button>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem', marginTop: '1rem' }}>
          <div style={{ padding: '1rem', background: 'var(--cream-deep)', borderRadius: '8px', border: '1px solid var(--line)' }}>
            <span style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', fontWeight: 500 }}>FARMERS</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--ink)', marginTop: '0.25rem' }}>
              {isDashboardLoading ? '...' : (dashboard?.usersPerRole?.Farmer ?? 0)}
            </div>
            <span style={{ fontSize: '0.75rem', color: 'var(--ink-soft)' }}>Field decision workspace</span>
          </div>

          <div style={{ padding: '1rem', background: 'var(--cream-deep)', borderRadius: '8px', border: '1px solid var(--line)' }}>
            <span style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', fontWeight: 500 }}>AGRI OFFICERS</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--ink)', marginTop: '0.25rem' }}>
              {isDashboardLoading ? '...' : (dashboard?.usersPerRole?.AgriculturalOfficer ?? 0)}
            </div>
            <span style={{ fontSize: '0.75rem', color: 'var(--ink-soft)' }}>Pest & disease approval</span>
          </div>

          <div style={{ padding: '1rem', background: 'var(--cream-deep)', borderRadius: '8px', border: '1px solid var(--line)' }}>
            <span style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', fontWeight: 500 }}>FIELD OFFICERS</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--ink)', marginTop: '0.25rem' }}>
              {isDashboardLoading ? '...' : (dashboard?.usersPerRole?.FieldOfficer ?? 0)}
            </div>
            <span style={{ fontSize: '0.75rem', color: 'var(--ink-soft)' }}>Agrarian tract monitoring</span>
          </div>

          <div style={{ padding: '1rem', background: 'var(--cream-deep)', borderRadius: '8px', border: '1px solid var(--line)' }}>
            <span style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', fontWeight: 500 }}>ADMINISTRATORS</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--ink)', marginTop: '0.25rem' }}>
              {isDashboardLoading ? '...' : (dashboard?.usersPerRole?.Admin ?? 0)}
            </div>
            <span style={{ fontSize: '0.75rem', color: 'var(--ink-soft)' }}>System & security console</span>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '1rem', marginTop: '1.25rem', flexWrap: 'wrap' }}>
          <Link
            to="/admin/users"
            className="btn btn-primary btn-sm"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem', textDecoration: 'none' }}
          >
            Manage Users Console
            <ArrowRight size={16} />
          </Link>
          <Link
            to="/admin/officer-requests"
            className="btn btn-secondary btn-sm"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem', textDecoration: 'none' }}
          >
            Review Officer Requests ({dashboard?.pendingOfficerApprovals ?? 0})
          </Link>
        </div>
      </div>

      {/* System Technical Architecture Panels */}
      <div className="dashboard-panels-grid">
        <div className="dashboard-panel">
          <div className="panel-header">
            <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Server size={20} color="var(--forest)" />
              ASP.NET Core Backend Auth Configuration
            </h3>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', fontSize: '0.875rem' }}>
            <div style={{ padding: '0.75rem 1rem', background: 'var(--cream-deep)', borderRadius: '6px' }}>
              <strong>API Base URL:</strong> <code>{API_BASE_URL}</code>
            </div>
            <div style={{ padding: '0.75rem 1rem', background: 'var(--cream-deep)', borderRadius: '6px' }}>
              <strong>Authentication & Health Endpoints:</strong>
              <ul style={{ paddingLeft: '1.2rem', marginTop: '0.25rem', lineHeight: '1.6' }}>
                <li><code>GET /health</code> (ASP.NET Core health probes & DB status)</li>
                <li><code>GET /api/admin/dashboard</code> (Live aggregated user & queue metrics)</li>
                <li><code>GET /api/admin/users</code> (Server-side paged user management)</li>
                <li><code>POST /api/auth/register</code> (JWT & Refresh token issue)</li>
                <li><code>POST /api/auth/login</code> (Credentials validation & token issue)</li>
                <li><code>POST /api/auth/refresh</code> (Sliding token pair rotation)</li>
                <li><code>GET /api/auth/me</code> (Bearer token verification)</li>
              </ul>
            </div>
          </div>
        </div>

        <div className="dashboard-panel">
          <div className="panel-header">
            <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <KeyRound size={18} color="var(--gold-deep)" />
              Active Session Security
            </h3>
          </div>
          <div className="info-notice">
            Authenticated as <strong>{user?.name}</strong> ({user?.email}) with role <strong>{user?.role}</strong>.
          </div>
          <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', lineHeight: 1.6, marginTop: '0.75rem' }}>
            All outgoing Axios HTTP requests are intercepted to dynamically attach the Authorization Bearer header. If an access token expires, the Axios interceptor transparently exchanges the refresh token and re-executes pending requests without interrupting administrator workflow.
          </p>
          <div style={{ marginTop: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.8125rem', color: '#2e7d32' }}>
            <ShieldCheck size={16} />
            <span>Role-Based Access Control Active ([Authorize(Roles = "Admin")])</span>
          </div>
        </div>
      </div>
    </div>
  );
};
