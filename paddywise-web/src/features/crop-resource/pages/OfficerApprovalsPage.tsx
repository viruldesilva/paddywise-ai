import React, { useState } from 'react';
import { useAuth } from '../../../hooks/useAuth';
import { Sidebar } from '../../../components/Sidebar';
import { OfficerRecommendationQueue } from '../components/OfficerRecommendationQueue';
import { Menu, LogOut } from 'lucide-react';
import '../../../styles/Dashboard.css';
import './ActivityDashboard.css';

export const OfficerApprovalsPage: React.FC = () => {
  const { user, logout } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

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
          <div className="activity-header-section" style={{ textAlign: 'left', marginBottom: '1.5rem' }}>
            <span
              className="eyebrow"
              style={{
                color: 'var(--ink-soft)',
                fontSize: '0.85rem',
                fontWeight: 600,
                letterSpacing: '1px',
                textTransform: 'uppercase',
                marginBottom: '0.5rem',
                display: 'block'
              }}
            >
              AGRICULTURAL EXTENSION & MONITORING
            </span>
            <h1
              className="fc-page-title"
              style={{ fontFamily: 'var(--font-heading)', fontSize: '2.5rem', color: 'var(--ink)', margin: 0 }}
            >
              AI Recommendation Approvals
            </h1>
            <p className="fc-page-sub" style={{ color: 'var(--ink-soft)', fontSize: '1.1rem', marginTop: '0.5rem' }}>
              Review, validate, and approve or reject Agentic AI agronomic recommendations before they are dispatched to farmers.
            </p>
          </div>

          <div style={{ width: '100%' }}>
            <OfficerRecommendationQueue officerName={user.name} />
          </div>
        </main>
      </div>
    </div>
  );
};
