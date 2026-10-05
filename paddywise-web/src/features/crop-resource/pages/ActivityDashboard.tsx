import React, { useState, useEffect } from 'react';
import { Link, useSearchParams, Navigate } from 'react-router-dom';
import { useAuth } from '../../../hooks/useAuth';
import { fieldApi } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import { ActivityHistory } from '../components/ActivityHistory';
import { AiAdvisorPanel } from '../components/AiAdvisorPanel';
import { Sidebar } from '../../../components/Sidebar';
import { Menu, Plus, LogOut, Sparkles, Activity as ActivityIcon } from 'lucide-react';
import '../../../styles/Dashboard.css';
import './ActivityDashboard.css';

export const ActivityDashboard: React.FC = () => {
  const { user, logout } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const isOfficer = user?.role === 'AgriculturalOfficer' || user?.role === 'FieldOfficer' || user?.role === 'Admin';

  // Redirect officers to their dedicated separate pages
  if (isOfficer) {
    if (searchParams.get('tab') === 'report') {
      return <Navigate to="/officer/reports" replace />;
    }
    return <Navigate to="/officer/approvals" replace />;
  }

  const tabParam = searchParams.get('tab');
  const currentTab = tabParam === 'advisor' ? 'advisor' : 'history';

  const [cycles, setCycles] = useState<CultivationCycle[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState<number | 'all'>('all');
  const [isLoadingCycles, setIsLoadingCycles] = useState(true);
  const [refreshTrigger] = useState(0);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  const selectedCycle = cycles.find(c => c.id === selectedCycleId) || null;

  useEffect(() => {
    async function loadCycles() {
      if (user?.role === 'Farmer') {
        try {
          const myCycles = await fieldApi.getMyCycles();
          // Filter to only show active or planned cycles
          const activeOrPlanned = myCycles.filter(c => c.status === 'Active' || c.status === 'Planned');
          setCycles(activeOrPlanned);
        } catch (err) {
          console.error('Failed to load cycles', err);
        } finally {
          setIsLoadingCycles(false);
        }
      } else {
        setIsLoadingCycles(false);
      }
    }
    loadCycles();
  }, [user]);

  const handleTabChange = (tab: 'history' | 'advisor') => {
    if (tab === 'advisor') {
      setSearchParams({ tab: 'advisor' });
    } else {
      setSearchParams({});
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
              CROP RESOURCE & MANAGEMENT
            </span>
            <h1 className="fc-page-title" style={{ fontFamily: 'var(--font-heading)', fontSize: '2.5rem', color: 'var(--ink)', margin: 0 }}>
              {currentTab === 'advisor'
                ? 'AI Paddy Field Advisor'
                : 'Record & Monitor Activities'}
            </h1>
            <p className="fc-page-sub" style={{ color: 'var(--ink-soft)', fontSize: '1.1rem', marginTop: '0.5rem' }}>
              {currentTab === 'advisor'
                ? 'Evidence-based agronomic intelligence synthesizing field activities with RRDI guidelines & DOA safety standards.'
                : 'Record and monitor your agricultural operations'}
            </p>
          </div>

          {/* Navigation Tabs for Farmer */}
          <div className="activity-nav-tabs">
            <button
              type="button"
              className={`activity-nav-tab ${currentTab === 'history' ? 'active' : ''}`}
              onClick={() => handleTabChange('history')}
            >
              <ActivityIcon size={18} />
              <span>Activity Log & History</span>
            </button>

            <button
              type="button"
              className={`activity-nav-tab ${currentTab === 'advisor' ? 'active' : ''}`}
              onClick={() => handleTabChange('advisor')}
            >
              <Sparkles size={18} color="#059669" />
              <span>AI Field Advisor & Recommendations</span>
              <span className="advisor-tab-badge">Agentic AI</span>
            </button>
          </div>

          <div style={{ width: '100%' }}>
            {currentTab === 'advisor' ? (
              <AiAdvisorPanel
                cycles={cycles}
                selectedCycleId={selectedCycleId}
                onCycleSelect={(cycleId) => setSelectedCycleId(cycleId)}
              />
            ) : (
              <>
                <div style={{ marginBottom: '2rem' }}>
                  <label className="form-label" style={{ marginBottom: '0.5rem', display: 'block', fontWeight: 600 }}>Select Cultivation Cycle</label>
                  {isLoadingCycles ? (
                    <p style={{ color: 'var(--ink-soft)', margin: 0 }}>Loading cycles...</p>
                  ) : cycles.length === 0 ? (
                    <p style={{ color: 'var(--clay)', margin: 0 }}>No active or planned cycles found. Please create one in Field Management first.</p>
                  ) : (
                    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
                      <select
                        className="form-input"
                        value={selectedCycleId}
                        onChange={e => setSelectedCycleId(e.target.value === 'all' ? 'all' : Number(e.target.value))}
                        style={{ maxWidth: '420px' }}
                      >
                        <option value="all">All Cultivation Cycles</option>
                        {cycles.map(c => (
                          <option key={c.id} value={c.id}>
                            {c.season} {c.year} - {c.fieldName} ({c.varietyName})
                          </option>
                        ))}
                      </select>

                      {selectedCycle && (
                        <Link
                          to={`/cycles/${selectedCycle.id}/activities/new`}
                          className="fc-btn fc-btn-primary"
                          style={{ textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: '0.5rem', padding: '0.7rem 1.25rem' }}
                        >
                          <Plus size={16} /> Record Activity
                        </Link>
                      )}
                    </div>
                  )}
                </div>

                <ActivityHistory
                  selectedCycleId={selectedCycleId}
                  cycles={cycles}
                  refreshTrigger={refreshTrigger}
                  userRole={user.role}
                />
              </>
            )}
          </div>
        </main>
      </div>
    </div>
  );
};
