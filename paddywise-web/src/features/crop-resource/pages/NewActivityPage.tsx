import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Menu } from 'lucide-react';
import { ActivityPanel } from '../components/ActivityPanel';
import { useAuth } from '../../../hooks/useAuth';
import { Sidebar } from '../../../components/Sidebar';
import { getCycleById } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import '../../../styles/Dashboard.css';
import '../../field-cultivation/styles/fieldCultivation.css';
import './ActivityDashboard.css';

export const NewActivityPage: React.FC = () => {
  const { user } = useAuth();
  const { id } = useParams<{ id: string }>();
  
  const [cycle, setCycle] = useState<CultivationCycle | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  useEffect(() => {
    if (id) {
      getCycleById(Number(id))
        .then(setCycle)
        .catch(console.error)
        .finally(() => setLoading(false));
    }
  }, [id]);

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
            </div>
          </div>
        </header>

        <main className="dashboard-content container">
          <Link className="fc-back" to={`/cycles/${id}`}>
            <ArrowLeft size={16} />
            Back to Cultivation Cycle
          </Link>
          
          <div className="fc-page-head" style={{ marginTop: '1rem', borderBottom: 'none' }}>
            <div>
              <span className="eyebrow">RECORD ACTIVITY</span>
              <h1 className="fc-page-title">
                {cycle ? `${cycle.season} ${cycle.year} · ${cycle.varietyName}` : 'Loading...'}
              </h1>
              <p className="fc-page-sub">
                {cycle ? `${cycle.fieldName}` : ''}
              </p>
            </div>
          </div>

          <div style={{ marginTop: '1rem' }}>
            {!loading && cycle ? (
              <ActivityPanel selectedCycle={cycle} />
            ) : (
              <div className="fc-state">
                <p className="fc-state-text">
                  {loading ? 'Loading cycle data...' : 'Cycle not found.'}
                </p>
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
};
