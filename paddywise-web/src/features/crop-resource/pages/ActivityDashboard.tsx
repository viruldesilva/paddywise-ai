import React, { useState, useEffect } from 'react';
import { useAuth } from '../../../hooks/useAuth';
import { fieldApi } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import { ActivityPanel } from '../components/ActivityPanel';
import './ActivityDashboard.css';
import Navbar from '../../../components/Navbar';

export const ActivityDashboard: React.FC = () => {
  const { user } = useAuth();
  
  const [cycles, setCycles] = useState<CultivationCycle[]>([]);
  const [selectedCycleId, setSelectedCycleId] = useState<number | ''>('');
  const [isLoadingCycles, setIsLoadingCycles] = useState(true);

  useEffect(() => {
    async function loadCycles() {
      if (user?.role === 'Farmer') {
        try {
          const myCycles = await fieldApi.getMyCycles();
          // Filter to only show active or planned cycles
          const activeOrPlanned = myCycles.filter(c => c.status === 'Active' || c.status === 'Planned');
          setCycles(activeOrPlanned);
          if (activeOrPlanned.length > 0) {
            setSelectedCycleId(activeOrPlanned[0].id);
          }
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

  const selectedCycle = cycles.find(c => c.id === selectedCycleId) || null;

  return (
    <>
      <Navbar />
      <div className="activity-dashboard-wrapper">
        <div className="activity-header-section">
          <h1>Crop Activity & Resource Management</h1>
          <p>Record and monitor your agricultural operations</p>
        </div>

        <div className={`activity-grid ${validationData ? 'with-validation' : ''}`}>
          <div className="activity-panel" style={{ opacity: validationData ? 0.7 : 1, transition: 'opacity 0.3s' }}>
            
            {user?.role === 'Farmer' && (
              <div style={{ marginBottom: '2rem', padding: '1.5rem', background: 'var(--cream)', borderRadius: '12px', border: '1px solid var(--line)' }}>
                <label className="form-label" style={{ marginBottom: '0.75rem', display: 'block' }}>Select Cultivation Cycle</label>
                {isLoadingCycles ? (
                  <p style={{ color: 'var(--ink-soft)', margin: 0 }}>Loading cycles...</p>
                ) : cycles.length === 0 ? (
                  <p style={{ color: 'var(--clay)', margin: 0 }}>No active or planned cycles found. Please create one in Field Management first.</p>
                ) : (
                  <select 
                    className="form-input" 
                    value={selectedCycleId} 
                    onChange={e => setSelectedCycleId(Number(e.target.value))}
                  >
                    {cycles.map(c => (
                      <option key={c.id} value={c.id}>
                        {c.season} {c.year} - {c.fieldName} ({c.varietyName})
                      </option>
                    ))}
                  </select>
                )}
              </div>
            )}

            <ActivityPanel selectedCycle={selectedCycle} />
          </div>
        </div>
      </div>
    </>
  );
};
