import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../../hooks/useAuth';
import { fieldApi } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import { ActivityHistory } from '../components/ActivityHistory';
import { Sidebar } from '../../../components/Sidebar';
import { Menu, Plus, LogOut } from 'lucide-react';
import '../../../styles/Dashboard.css';
import './ActivityDashboard.css';

export const ActivityDashboard: React.FC = () => {
  const { user, logout } = useAuth();
  
  const [cycles, setCycles] = useState<CultivationCycle[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState<number | 'all'>('all');
  const [isLoadingCycles, setIsLoadingCycles] = useState(true);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  useEffect(() => {
    async function loadCycles() {
      if (user?.role === 'Farmer') {
        try {
          const myCycles = await fieldApi.getMyCycles();
          // Filter to only show active or planned cycles
          const activeOrPlanned = myCycles.filter(c => c.status === 'Active' || c.status === 'Planned');
          setCycles(activeOrPlanned);
        } catch (err) {
          console.error("Failed to load cycles", err);
        } finally {
          setIsLoadingCycles(false);
        }
      } else {
        setIsLoadingCycles(false);
      }
    }
    loadCycles();
  }, [user]);

  const isOfficer = user?.role === 'AgriculturalOfficer' || user?.role === 'FieldOfficer' || user?.role === 'Admin';
  const selectedCycle = cycles.find(c => c.id === selectedCycleId) || null;

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
          <div className="activity-header-section" style={{ textAlign: 'left', marginBottom: '2rem' }}>
            <span className="eyebrow" style={{ color: 'var(--ink-soft)', fontSize: '0.85rem', fontWeight: 600, letterSpacing: '1px', textTransform: 'uppercase', marginBottom: '0.5rem', display: 'block' }}>
              {isOfficer ? 'AGRICULTURAL EXTENSION & MONITORING' : 'CROP MANAGEMENT'}
            </span>
            <h1 className="fc-page-title" style={{ fontFamily: 'var(--font-heading)', fontSize: '2.5rem', color: 'var(--ink)', margin: 0 }}>
              {isOfficer ? 'All Farmers Crop Activities' : 'Record Activities'}
            </h1>
            <p className="fc-page-sub" style={{ color: 'var(--ink-soft)', fontSize: '1.1rem', marginTop: '0.5rem' }}>
              {isOfficer ? 'Monitor and review past agricultural operations logged by all farmers across cultivation cycles' : 'Record and monitor your agricultural operations'}
            </p>
          </div>

          <div style={{ width: '100%' }}>
            {user?.role === 'Farmer' && (
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
            )}
            <ActivityHistory 
              selectedCycleId={selectedCycleId} 
              cycles={cycles} 
              refreshTrigger={refreshTrigger} 
              userRole={user.role} 
            />
          </div>
        </main>
      </div>
    </div>
  );
};
