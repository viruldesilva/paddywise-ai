import React, { useEffect, useState } from 'react';
import { activityApi, type CropActivityDto } from '../services/activityApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import { ActivityAnalytics } from './ActivityAnalytics';
import { User, Search } from 'lucide-react';

interface ActivityHistoryProps {
  selectedCycleId?: number | 'all';
  cycles?: CultivationCycle[];
  refreshTrigger?: number;
  userRole?: string;
}

export const ActivityHistory: React.FC<ActivityHistoryProps> = ({ 
  selectedCycleId = 'all', 
  cycles = [], 
  refreshTrigger = 0,
  userRole 
}) => {
  const [activities, setActivities] = useState<CropActivityDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const [categoryFilter, setCategoryFilter] = useState<string>('All');
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState<string>('');

  const isOfficer = userRole === 'AgriculturalOfficer' || userRole === 'FieldOfficer' || userRole === 'Admin';

  useEffect(() => {
    async function fetchActivities() {
      setIsLoading(true);
      setError('');
      try {
        if (isOfficer) {
          const data = await activityApi.getAllActivities();
          setActivities(data);
        } else {
          if (cycles.length === 0) {
            setActivities([]);
            setIsLoading(false);
            return;
          }
          if (selectedCycleId === 'all') {
            const promises = cycles.map(c => activityApi.getActivitiesForCycle(c.id));
            const results = await Promise.all(promises);
            const allActivities = results.flat().sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
            setActivities(allActivities);
          } else {
            const data = await activityApi.getActivitiesForCycle(selectedCycleId);
            setActivities(data);
          }
        }
      } catch (err) {
        console.error('Failed to load activity history', err);
        setError('Failed to load activity history.');
      } finally {
        setIsLoading(false);
      }
    }
    fetchActivities();
  }, [selectedCycleId, cycles, refreshTrigger, isOfficer]);

  const renderDetails = (activity: CropActivityDto) => {
    try {
      const data = JSON.parse(activity.detailsJson || '{}');
      const formattedDate = new Date(data.date || activity.date).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });

      switch (activity.activityType) {
        case 'Irrigation':
          return (
            <>
              <div><strong>Date:</strong> {formattedDate}</div>
              <div><strong>Water Level (cm):</strong> {data.waterLevel !== undefined ? `${data.waterLevel} cm` : '—'}</div>
              <div><strong>Duration (hours):</strong> {data.duration !== undefined ? `${data.duration} hrs` : '—'}</div>
              <div><strong>Source:</strong> {data.source || '—'}</div>
            </>
          );
        case 'Fertilizer':
          return (
            <>
              <div><strong>Date:</strong> {formattedDate}</div>
              <div><strong>Fertilizer Type:</strong> {data.type || '—'}</div>
              <div><strong>Quantity (kg/ha):</strong> {data.quantity !== undefined ? `${data.quantity} kg/ha` : '—'}</div>
              <div><strong>Crop Stage:</strong> {data.cropStage || '—'}</div>
              <div><strong>Region / Zone:</strong> {data.region || '—'}</div>
              <div><strong>Application Method:</strong> {data.method || '—'}</div>
            </>
          );
        case 'Pesticide':
          return (
            <>
              <div><strong>Date:</strong> {formattedDate}</div>
              <div><strong>Product Name:</strong> {data.product || '—'}</div>
              <div><strong>Target Pest / Disease:</strong> {data.targetPest || '—'}</div>
              <div><strong>Quantity (ml/g):</strong> {data.quantity !== undefined ? `${data.quantity} ml/g` : '—'}</div>
              <div><strong>Method:</strong> {data.method || '—'}</div>
            </>
          );
        case 'Other':
          return (
            <>
              <div><strong>Date:</strong> {formattedDate}</div>
              <div><strong>Activity Type:</strong> {data.specificActivity || '—'}</div>
              <div style={{ gridColumn: '1 / -1' }}><strong>Notes:</strong> {data.notes || 'None'}</div>
            </>
          );
        default:
          return (
            <>
              <div><strong>Date:</strong> {formattedDate}</div>
              <div><strong>Activity Type:</strong> {activity.activityType}</div>
              <div style={{ gridColumn: '1 / -1' }}>{activity.detailsJson}</div>
            </>
          );
      }
    } catch (e) {
      return <div>Invalid details format</div>;
    }
  };

  if (!isOfficer && cycles.length === 0) {
    return null;
  }

  const filteredActivities = activities.filter(activity => {
    if (categoryFilter !== 'All' && activity.activityType !== categoryFilter) return false;
    if (startDate && new Date(activity.date) < new Date(startDate)) return false;
    if (endDate && new Date(activity.date) > new Date(endDate)) return false;
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase().trim();
      const farmer = (activity.farmerName || activity.loggedByUserName || '').toLowerCase();
      const field = (activity.fieldName || '').toLowerCase();
      const cycle = (activity.cycleName || '').toLowerCase();
      const details = (activity.detailsJson || '').toLowerCase();
      if (!farmer.includes(q) && !field.includes(q) && !cycle.includes(q) && !details.includes(q)) {
        return false;
      }
    }
    return true;
  });

  return (
    <div className="activity-history-wrapper" style={{ marginTop: '1rem' }}>

      {isLoading ? (
        <p style={{ color: 'var(--ink-soft)' }}>Loading history...</p>
      ) : error ? (
        <p style={{ color: 'var(--earth-deep)' }}>{error}</p>
      ) : activities.length === 0 ? (
        <div style={{ marginTop: '1rem' }}>
          <h2 className="activity-title" style={{ fontSize: '1.25rem', marginBottom: '0.5rem' }}>Past Activities</h2>
          <p style={{ color: 'var(--clay)' }}>
            {isOfficer ? 'No activities recorded by any farmers yet.' : 'No activities recorded for this cycle yet.'}
          </p>
        </div>
      ) : (
        <>
          <ActivityAnalytics activities={activities} />

          <div style={{ marginTop: '2.5rem' }}>
            <h2 className="activity-title" style={{ fontSize: '1.25rem', marginBottom: '1rem' }}>
              {isOfficer ? 'All Farmers Past Activities' : 'Past Activities'}
            </h2>
            <div className="activity-filters" style={{ display: 'flex', gap: '1rem', marginBottom: '1.5rem', flexWrap: 'wrap', background: 'var(--cream-deep)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--line)' }}>
              {isOfficer && (
                <div className="form-group" style={{ marginBottom: 0, flex: 2, minWidth: '220px' }}>
                  <label className="form-label" style={{ fontSize: '0.8rem', marginBottom: '0.35rem' }}>Search Farmer / Field</label>
                  <div style={{ position: 'relative' }}>
                    <input 
                      type="text" 
                      className="form-input" 
                      style={{ padding: '0.55rem 0.75rem 0.55rem 2.25rem' }} 
                      placeholder="Search farmer, field, product..." 
                      value={searchQuery} 
                      onChange={e => setSearchQuery(e.target.value)} 
                    />
                    <Search size={15} style={{ position: 'absolute', left: '0.75rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--ink-soft)' }} />
                  </div>
                </div>
              )}
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: '180px' }}>
                <label className="form-label" style={{ fontSize: '0.8rem', marginBottom: '0.35rem' }}>Category</label>
                <select className="form-input" style={{ padding: '0.55rem 0.75rem' }} value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)}>
                  <option value="All">All Categories</option>
                  <option value="Irrigation">Irrigation</option>
                  <option value="Fertilizer">Fertilizer</option>
                  <option value="Pesticide">Pesticide</option>
                  <option value="Other">Other</option>
                </select>
              </div>
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: '150px' }}>
                <label className="form-label" style={{ fontSize: '0.8rem', marginBottom: '0.35rem' }}>Start Date</label>
                <input type="date" className="form-input" style={{ padding: '0.5rem' }} value={startDate} onChange={e => setStartDate(e.target.value)} />
              </div>
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: '150px' }}>
                <label className="form-label" style={{ fontSize: '0.8rem', marginBottom: '0.35rem' }}>End Date</label>
                <input type="date" className="form-input" style={{ padding: '0.5rem' }} value={endDate} onChange={e => setEndDate(e.target.value)} />
              </div>
            </div>

            {filteredActivities.length === 0 ? (
              <p style={{ color: 'var(--clay)' }}>No activities match the selected filters.</p>
            ) : (
              <div className="activity-list">
                {filteredActivities.map(activity => {
                  const cycle = cycles.find(c => c.id === activity.cultivationCycleId);
                  const displayFarmer = activity.farmerName || (activity.loggedByUserName && activity.loggedByUserName !== 'Unknown' ? activity.loggedByUserName : null);
                  const displayField = activity.fieldName || cycle?.fieldName;
                  const displayCycle = activity.cycleName || (cycle ? `${cycle.season} ${cycle.year}` : null);

                  return (
                    <div key={activity.id} className="activity-card">
                      <div className="activity-card-header">
                        <div className="activity-card-title" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
                          <span className={`activity-badge badge-${activity.activityType.toLowerCase()}`}>
                            {activity.activityType}
                          </span>

                          {displayFarmer && (
                            <span style={{ 
                              display: 'inline-flex', 
                              alignItems: 'center', 
                              gap: '0.35rem', 
                              background: 'rgba(34, 57, 42, 0.08)', 
                              color: 'var(--forest-deep)', 
                              padding: '0.2rem 0.6rem', 
                              borderRadius: '6px', 
                              fontSize: '0.85rem', 
                              fontWeight: 600 
                            }}>
                              <User size={13} /> {displayFarmer}
                            </span>
                          )}

                          {displayField && (
                            <span style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', fontWeight: 500 }}>
                              {displayField} {displayCycle ? `· ${displayCycle}` : ''}
                            </span>
                          )}
                        </div>
                        <div className="activity-logger">Logged by: {activity.loggedByUserName}</div>
                      </div>
                      <div className="activity-card-details">
                        {renderDetails(activity)}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};
