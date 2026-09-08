import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { authService } from '../services/authService';
import type { User } from '../types/auth';
import { 
  Sprout, 
  ShieldCheck, 
  ShoppingBag, 
  Users, 
  LogOut, 
  CheckCircle, 
  RefreshCw, 
  Database,
  Calendar,
  FileCheck
} from 'lucide-react';
import '../styles/Dashboard.css';

export default function DashboardPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [usersList, setUsersList] = useState<User[]>([]);
  const [notification, setNotification] = useState<string | null>(null);

  useEffect(() => {
    if (user?.role === 'admin') {
      setUsersList(authService.getAllUsers());
    }
  }, [user]);

  const handleLogout = async () => {
    await logout();
    navigate('/');
  };

  const handleResetDefaults = () => {
    if (window.confirm('Reset local storage users to default seed accounts?')) {
      authService.resetToDefaults();
      setUsersList(authService.getAllUsers());
      setNotification('Users reset to initial seed accounts.');
      setTimeout(() => setNotification(null), 3000);
    }
  };

  if (!user) return null;

  return (
    <div className="dashboard-container">
      {/* Top Bar */}
      <header className="dashboard-header">
        <div className="container dashboard-header-inner">
          <Link to="/" className="dashboard-brand">
            <svg
              width="24"
              height="24"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M12 22v-8" />
              <path d="M12 14c-3-2-6-3-6-6 0-3 3-4 6-4" />
              <path d="M12 14c3-2 6-3 6-6 0-3 3-4 6-4" />
              <path d="M12 4v10" />
            </svg>
            Kumburu
          </Link>

          <div className="dashboard-user-meta">
            <div className="dashboard-user-greeting">
              <span className="dashboard-user-name">{user.fullName}</span>
              <span className="dashboard-user-sub">
                {user.division ? `${user.division} • ` : ''}{user.email}
              </span>
            </div>

            <span className={`role-badge-tag badge-${user.role === 'extension_officer' ? 'officer' : user.role}`}>
              {user.role.replace('_', ' ')}
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
        {notification && (
          <div className="info-notice" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1.5rem' }}>
            <CheckCircle size={18} color="var(--shoot)" />
            <span>{notification}</span>
          </div>
        )}

        <div className="dashboard-welcome-banner">
          <span className="eyebrow">
            {user.role === 'farmer' && 'FIELD DECISION WORKSPACE'}
            {user.role === 'extension_officer' && 'AGRICULTURAL SERVICE DIVISION'}
            {user.role === 'buyer' && 'HARVEST PROCUREMENT PORTAL'}
            {user.role === 'admin' && 'SYSTEM ADMINISTRATION & USERS'}
          </span>
          <h1 className="dashboard-welcome-title">
            {user.role === 'farmer' && `Ayubowan, ${user.fullName.split(' ')[0]}!`}
            {user.role === 'extension_officer' && `Officer Workspace: ${user.fullName}`}
            {user.role === 'buyer' && `Procurement Hub: ${user.fullName}`}
            {user.role === 'admin' && `System Administration Console`}
          </h1>
          <p className="dashboard-welcome-desc">
            {user.role === 'farmer' && 'Monitor your crop cycle, diagnose leaf issues, and receive extension-approved treatment.'}
            {user.role === 'extension_officer' && 'Review AI-flagged pest & disease symptoms and approve safe pesticide recommendations.'}
            {user.role === 'buyer' && 'Discover upcoming paddy harvests across divisions and connect with local farmers.'}
            {user.role === 'admin' && 'Manage registered accounts in local storage, review audit logs, and prepare for PostgreSQL connection.'}
          </p>
        </div>

        {/* 1. FARMER ROLE VIEW */}
        {user.role === 'farmer' && (
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
                  Notice yellowing, spots, or stunted tillers? Submit photos for AI analysis. An Extension Officer will review and verify the treatment recommendation.
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem', marginTop: '1rem' }}>
                  <div style={{ border: '2px dashed var(--line)', padding: '2rem', textAlign: 'center', borderRadius: '8px' }}>
                    <p style={{ fontWeight: 500, color: 'var(--ink)' }}>Upload Leaf or Pest Photo</p>
                    <p style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>
                      Supports JPEG, PNG from field mobile camera
                    </p>
                    <button className="btn btn-primary btn-sm" style={{ marginTop: '1rem' }} onClick={() => alert('Photo diagnosis is connected to the 4-agent pipeline in the mobile Flutter app and backend API.')}>
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
              </div>
            </div>
          </div>
        )}

        {/* 2. EXTENSION OFFICER ROLE VIEW */}
        {user.role === 'extension_officer' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Assigned Division</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>{user.division || 'North Central'}</span>
                <span className="metric-sub">Agrarian Services Centre</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Pending Reviews</span>
                <span className="metric-value" style={{ color: '#d97706' }}>3</span>
                <span className="metric-sub">Requires officer sign-off</span>
              </div>
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

        {/* 3. BUYER / MILLER ROLE VIEW */}
        {user.role === 'buyer' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Live Field Listings</span>
                <span className="metric-value">12 Lots</span>
                <span className="metric-sub">Polonnaruwa & Anuradhapura</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Active Purchase Offers</span>
                <span className="metric-value">3</span>
                <span className="metric-sub">Pending farmer confirmation</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Total Procured</span>
                <span className="metric-value">85 MT</span>
                <span className="metric-sub">Current season</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Avg Buying Rate</span>
                <span className="metric-value">LKR 115/kg</span>
                <span className="metric-sub">Samba & Nadu paddy</span>
              </div>
            </div>

            <div className="dashboard-panel">
              <div className="panel-header">
                <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <ShoppingBag size={20} color="var(--gold-deep)" />
                  Available Harvest Listings Nearby
                </h3>
              </div>
              <div className="user-table-wrapper">
                <table className="user-table">
                  <thead>
                    <tr>
                      <th>Farmer</th>
                      <th>Variety</th>
                      <th>Estimated Quantity</th>
                      <th>Location</th>
                      <th>Harvest Date</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>Bandara Wanninayake</td>
                      <td>Bg 352 (Nadu)</td>
                      <td>14 Metric Tons</td>
                      <td>Medirigiriya</td>
                      <td>Aug 20, 2026</td>
                      <td>
                        <button className="btn btn-primary btn-sm" onClick={() => alert('Offer modal will open in integrated procurement module.')}>
                          Place Offer
                        </button>
                      </td>
                    </tr>
                    <tr>
                      <td>Sunil Jayasuriya</td>
                      <td>At 362 (Red Samba)</td>
                      <td>9.5 Metric Tons</td>
                      <td>Tambuttegama</td>
                      <td>Aug 28, 2026</td>
                      <td>
                        <button className="btn btn-primary btn-sm" onClick={() => alert('Offer modal will open in integrated procurement module.')}>
                          Place Offer
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}

        {/* 4. ADMIN ROLE VIEW */}
        {user.role === 'admin' && (
          <div>
            <div className="metrics-grid">
              <div className="metric-card">
                <span className="metric-label">Registered Accounts</span>
                <span className="metric-value">{usersList.length}</span>
                <span className="metric-sub">Saved in LocalStorage</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Target Database</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>PostgreSQL</span>
                <span className="metric-sub">Schema ready in database/</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">Backend Target</span>
                <span className="metric-value" style={{ fontSize: '1.25rem' }}>ASP.NET Core</span>
                <span className="metric-sub">PaddyWise.Api ready</span>
              </div>
              <div className="metric-card">
                <span className="metric-label">System Health</span>
                <span className="metric-value" style={{ color: '#2e7d32' }}>Online</span>
                <span className="metric-sub">Mock Auth active</span>
              </div>
            </div>

            <div className="dashboard-panels-grid">
              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Users size={20} color="var(--forest)" />
                    Current User Accounts in Storage ({usersList.length})
                  </h3>
                  <button onClick={handleResetDefaults} className="btn btn-secondary btn-sm" style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                    <RefreshCw size={14} /> Reset Seed Users
                  </button>
                </div>

                <div className="user-table-wrapper">
                  <table className="user-table">
                    <thead>
                      <tr>
                        <th>Name</th>
                        <th>Email</th>
                        <th>Role</th>
                        <th>Division</th>
                        <th>Registered</th>
                      </tr>
                    </thead>
                    <tbody>
                      {usersList.map((u) => (
                        <tr key={u.id}>
                          <td style={{ fontWeight: 600, color: 'var(--ink)' }}>{u.fullName}</td>
                          <td>{u.email}</td>
                          <td>
                            <span className={`badge-outline badge-${u.role === 'extension_officer' ? 'officer' : u.role}`}>
                              {u.role.replace('_', ' ')}
                            </span>
                          </td>
                          <td>{u.division || '—'}</td>
                          <td style={{ fontSize: '0.8rem' }}>
                            {new Date(u.createdAt).toLocaleDateString()}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="dashboard-panel">
                <div className="panel-header">
                  <h3 className="panel-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Database size={18} />
                    PostgreSQL Migration Ready
                  </h3>
                </div>
                <div className="info-notice">
                  <strong>Backend Status:</strong> When you start PostgreSQL and ASP.NET Core:
                </div>
                <ol style={{ fontSize: '0.875rem', color: 'var(--ink-soft)', lineHeight: 1.8, paddingLeft: '1.25rem' }}>
                  <li>Run <code>database/schema.sql</code> in PostgreSQL.</li>
                  <li>Configure your connection string in <code>paddywise-backend/appsettings.json</code>.</li>
                  <li>In <code>paddywise-web</code>, the <code>authService</code> endpoints will swap directly to <code>/api/auth/*</code> without rewriting frontend components.</li>
                </ol>
              </div>
            </div>
          </div>
        )}
      </main>
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
