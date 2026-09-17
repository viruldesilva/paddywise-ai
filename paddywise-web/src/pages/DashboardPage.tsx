import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { Sidebar } from '../components/Sidebar';
import { PendingPlansCard } from '../features/field-cultivation/components/PendingPlansCard';
import type { UserRole } from '../types/auth';
import { 
  Sprout, 
  ShieldCheck, 
  LogOut, 
  CheckCircle, 
  Calendar,
  FileCheck,
  Compass,
  Radio,
  Server,
  KeyRound,
  Menu
} from 'lucide-react';
import '../styles/Dashboard.css';

interface DashboardPageProps {
  roleView?: UserRole;
}

export default function DashboardPage({ roleView }: DashboardPageProps) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  if (!user) return null;

  // Active role is either the route-specified roleView or the logged-in user's role
  const activeRole: UserRole = roleView || user.role;

  const handleLogout = () => {
    logout();
  };

  const getRoleDisplayName = (role: UserRole) => {
    switch (role) {
      case 'AgriculturalOfficer':
        return 'Agricultural Officer';
      case 'FieldOfficer':
        return 'Field Officer';
      default:
        return role;
    }
  };

  const getRoleBadgeClass = (role: UserRole) => {
    switch (role) {
      case 'Farmer':
        return 'badge-farmer';
      case 'AgriculturalOfficer':
      case 'FieldOfficer':
        return 'badge-officer';
      case 'Admin':
        return 'badge-admin';
      default:
        return 'badge-farmer';
    }
  };

  return (
    <div className="dashboard-layout">
      <Sidebar role={activeRole} isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />
      <div className="dashboard-main-wrapper">
        {/* Top Bar */}
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
              <span className="dashboard-user-sub">
                {user.email}
              </span>
            </div>

            <span className={`role-badge-tag ${getRoleBadgeClass(activeRole)}`}>
              {getRoleDisplayName(activeRole)}
            </span>

            <button 
              onClick={handleLogout} 
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

      {/* Main Content Area */}
      <main className="dashboard-content container">
        <div className="dashboard-welcome-banner">
          <span className="eyebrow">
            {activeRole === 'Farmer' && 'FIELD DECISION WORKSPACE'}
            {activeRole === 'AgriculturalOfficer' && 'AGRICULTURAL EXTENSION DIVISION'}
            {activeRole === 'FieldOfficer' && 'FIELD TECHNICAL & EXTENSION MONITORING'}
            {activeRole === 'Admin' && 'SYSTEM ADMINISTRATION & PLATFORM CONSOLE'}
          </span>
          <h1 className="dashboard-welcome-title">
            {activeRole === 'Farmer' && `Ayubowan, ${user.name.split(' ')[0]}!`}
            {activeRole === 'AgriculturalOfficer' && `Officer Workspace: ${user.name}`}
            {activeRole === 'FieldOfficer' && `Field Officer Hub: ${user.name}`}
            {activeRole === 'Admin' && `System Administration Console`}
          </h1>
          <p className="dashboard-welcome-desc">
            {activeRole === 'Farmer' && 'Monitor your crop cycle, diagnose leaf issues, and receive extension-approved treatment.'}
            {activeRole === 'AgriculturalOfficer' && 'Review AI-flagged pest & disease symptoms and approve safe pesticide recommendations.'}
            {activeRole === 'FieldOfficer' && 'Coordinate on-site field visits, inspect agrarian tracts, and dispatch instant diagnostic telemetry.'}
            {activeRole === 'Admin' && 'Manage authenticated accounts, monitor JWT security rotation, and observe backend system health.'}
          </p>
        </div>

        {/* 1. FARMER ROLE VIEW */}
        {activeRole === 'Farmer' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Cultivation Season</span>
                <span className="metric-value">Yala 2026</span>
                <span className="metric-sub">Day 45 (Tillering Stage)</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Registered Field</span>
                <span className="metric-value">3.5 Acres</span>
                <span className="metric-sub">Bg 352 (White Nadu)</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Crop Health Status</span>
                <span className="metric-value" style={{ color: '#2e7d32' }}>Normal</span>
                <span className="metric-sub">Last scan: 3 days ago</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Target Harvest</span>
                <span className="metric-value">14.0 MT</span>
                <span className="metric-sub">Expected: late August</span>
              </div>
            </div>

            <div className="dashboard-panels-grid">
              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Sprout size={20} color="var(--shoot)" />
                    Field Issue Diagnosis & Advisory
                  </h3>
                </div>
                <div className="info-notice">
                  Notice yellowing, spots, or stunted tillers? Submit photos for AI analysis. An Agricultural Officer will review and verify the treatment recommendation.
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem', marginTop: '1rem' }}>
                  <div style={{ border: '2px dashed var(--line)', padding: '2rem', textAlign: 'center', borderRadius: '8px' }}>
                    <p style={{ fontWeight: 500, color: 'var(--ink)' }}>Upload Leaf or Pest Photo</p>
                    <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>
                      Supports JPEG, PNG from field mobile camera
                    </p>
                    <button className="btn btn-primary btn-sm" style={{ marginTop: '1rem' }} onClick={() => alert('Photo diagnosis is connected to the backend API & AI agent pipeline.')}>
                      Simulate Symptom Scan
                    </button>
                  </div>
                </div>
              </div>

              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Calendar size={18} />
                    Current Schedule
                  </h3>
                </div>
                <ul style={{ display: 'flex', flexDirection: 'column', gap: '1rem', fontSize: '0.9rem' }}>
                  <li style={{ display: 'flex', alignItems: 'flex-start', gap: '0.5rem' }}>
                    <CheckCircle size={16} color="var(--shoot)" style={{ marginTop: '0.2rem' }} />
                    <div>
                      <strong>Basal Fertilizer applied</strong>
                      <p style={{ color: 'var(--ink-soft)', fontSize: '0.8rem' }}>Verified 14 days after sowing</p>
                    </div>
                  </li>
                  <li style={{ display: 'flex', alignItems: 'flex-start', gap: '0.5rem' }}>
                    <ClockIcon />
                    <div>
                      <strong>Top Dressing (Urea)</strong>
                      <p style={{ color: 'var(--ink-soft)', fontSize: '0.8rem' }}>Scheduled in 5 days</p>
                    </div>
                  </li>
                </ul>
                <div style={{ marginTop: '1.5rem', display: 'flex', gap: '1rem' }}>
                  <button className="btn btn-primary" onClick={() => navigate('/activities')}>
                    Manage Crop Activities
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* 2. AGRICULTURAL OFFICER ROLE VIEW */}
        {activeRole === 'AgriculturalOfficer' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Assigned Division</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>Polonnaruwa Central</span>
                <span className="metric-sub">Agrarian Services Centre</span>
              </div>
              <PendingPlansCard />
              <div className="metric-card">
                <span className="metric-label">Approved Treatments</span>
                <span className="metric-value">42</span>
                <span className="metric-sub">This cultivation season</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Registered Farmers</span>
                <span className="metric-value">186</span>
                <span className="metric-sub">Across 6 GN divisions</span>
              </div>
            </div>

            <div className="dashboard-panels-grid">
              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <ShieldCheck size={20} color="var(--forest)" />
                    Pending Treatment Approval Queue
                  </h3>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  <div style={{ padding: '1rem', border: '1px solid var(--line)', borderRadius: '8px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                      <strong>Farmer: Bandara Wanninayake</strong>
                      <span className="badge-outline badge-farmer">Medirigiriya</span>
                    </div>
                    <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)' }}>
                      <strong>Symptom:</strong> Hopper burn patches in 0.5 acre paddy patch.
                    </p>
                    <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)' }}>
                      <strong>AI Diagnosis:</strong> Brown Plant Hopper (Nilaparvata lugens) with 96% confidence.
                    </p>
                    <p style={{ fontSize: '0.875rem', color: 'var(--shoot)', marginTop: '0.25rem' }}>
                      <strong>Proposed Plan:</strong> Drain field water for 3 days; apply approved Thiamethoxam 25% WG at recommended dosage.
                    </p>
                    <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
                      <button className="btn btn-primary btn-sm" onClick={() => alert('Treatment plan approved and notification dispatched to farmer via SMS/App.')}>
                        Approve Treatment
                      </button>
                      <button className="btn btn-secondary btn-sm" onClick={() => alert('Plan returned for secondary field sample inspection.')}>
                        Request Details
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <FileCheck size={18} />
                    Division Advisory Feed
                  </h3>
                </div>
                <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', lineHeight: 1.6 }}>
                  High humidity detected over Medirigiriya and Polonnaruwa basin. Alert issued to watch for Rice Blast (Pyricularia oryzae) on high nitrogen plots.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* 3. FIELD OFFICER ROLE VIEW */}
        {activeRole === 'FieldOfficer' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Field Station</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>District Sector 4</span>
                <span className="metric-sub">Mobile Response Unit</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Scheduled Visits</span>
                <span className="metric-value" style={{ color: '#0284c7' }}>5 Today</span>
                <span className="metric-sub">Field inspections</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Scans Completed</span>
                <span className="metric-value">89</span>
                <span className="metric-sub">Geo-tagged submissions</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Active Outbreak Flags</span>
                <span className="metric-value" style={{ color: '#d97706' }}>1 Zone</span>
                <span className="metric-sub">Brown Plant Hopper</span>
              </div>
            </div>

            <div className="dashboard-panels-grid">
              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Compass size={20} color="var(--forest)" />
                    Today's Field Route & Verification Queue
                  </h3>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                  <div style={{ padding: '0.875rem 1rem', border: '1px solid var(--line)', borderRadius: '8px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <strong>Paddy Tract #14 - Jayanthipura</strong>
                      <span className="badge-outline badge-officer">In Progress</span>
                    </div>
                    <p style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>
                      Farmer: K. Dharmasena • Verification of Leaf Scald symptoms.
                    </p>
                  </div>
                  <div style={{ padding: '0.875rem 1rem', border: '1px solid var(--line)', borderRadius: '8px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <strong>Paddy Tract #22 - Hingurakgoda</strong>
                      <span className="badge-outline">Upcoming (2:00 PM)</span>
                    </div>
                    <p style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>
                      Farmer: S. Wickramasinghe • Soil salinity spot check.
                    </p>
                  </div>
                </div>
              </div>

              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Radio size={18} color="var(--shoot)" />
                    Field Telemetry Connection
                  </h3>
                </div>
                <div className="info-notice">
                  GPS location and offline sensor packets automatically sync with the ASP.NET Core API server once within cellular range.
                </div>
                <p style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.75rem' }}>
                  Connected to backend gateway: <code>http://localhost:5164/api</code>
                </p>
              </div>
            </div>
          </div>
        )}

        {/* 4. ADMIN ROLE VIEW */}
        {activeRole === 'Admin' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Backend Environment</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>ASP.NET Core</span>
                <span className="metric-sub">PaddyWise.Api active</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Target Database</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>PostgreSQL</span>
                <span className="metric-sub">Neon DB / Local</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Auth Architecture</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>JWT Bearer</span>
                <span className="metric-sub">Rotated Refresh Tokens</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Backend Status</span>
                <span className="metric-value" style={{ color: '#2e7d32' }}>Ready</span>
                <span className="metric-sub">http://localhost:5164/api</span>
              </div>
            </div>

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
                    <strong>API Base URL:</strong> <code>http://localhost:5164/api</code>
                  </div>
                  <div style={{ padding: '0.75rem 1rem', background: 'var(--cream-deep)', borderRadius: '6px' }}>
                    <strong>Authentication Endpoints:</strong>
                    <ul style={{ paddingLeft: '1.2rem', marginTop: '0.25rem', lineHeight: '1.6' }}>
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
                  Authenticated as <strong>{user.name}</strong> ({user.email}) with role <strong>{user.role}</strong>.
                </div>
                <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', lineHeight: 1.6, marginTop: '0.75rem' }}>
                  All outgoing Axios HTTP requests are intercepted to dynamically attach the Authorization Bearer header. If an access token expires, the Axios interceptor transparently exchanges the refresh token and re-executes pending requests without interrupting user workflow.
                </p>
              </div>
            </div>
          </div>
        )}
      </main>
      </div>
    </div>
  );
}

function ClockIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--ink-soft)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ marginTop: '0.2rem' }}>
      <circle cx="12" cy="12" r="10" />
      <polyline points="12 6 12 12 16 14" />
    </svg>
  );
}
