import React, { useEffect, useState } from 'react';
import { activityApi, type CropActivityDto } from '../services/activityApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import { ActivityAnalytics } from './ActivityAnalytics';
import { EditActivityModal } from './EditActivityModal';
import { User, Search, Pencil, Trash2, CheckCircle2, AlertCircle, X } from 'lucide-react';
import '../../field-cultivation/styles/fieldCultivation.css';

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

  const [editingActivity, setEditingActivity] = useState<CropActivityDto | null>(null);
  const [deletingActivity, setDeletingActivity] = useState<CropActivityDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [statusModal, setStatusModal] = useState<{ isOpen: boolean; success: boolean; message: string } | null>(null);

  const isOfficer = userRole === 'AgriculturalOfficer' || userRole === 'FieldOfficer' || userRole === 'Admin';
  const canModify = userRole === 'Farmer' || userRole === 'Admin';

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

  const handleUpdateSuccess = (updated: CropActivityDto) => {
    setActivities(prev => prev.map(a => a.id === updated.id ? updated : a));
    setEditingActivity(null);
    setStatusModal({
      isOpen: true,
      success: true,
      message: `${updated.activityType} activity updated successfully!`
    });
  };

  const handleConfirmDelete = async () => {
    if (!deletingActivity) return;

    try {
      setIsDeleting(true);
      await activityApi.deleteActivity(deletingActivity.id);
      setActivities(prev => prev.filter(a => a.id !== deletingActivity.id));
      const deletedType = deletingActivity.activityType;
      setDeletingActivity(null);
      setStatusModal({
        isOpen: true,
        success: true,
        message: `${deletedType} activity deleted successfully!`
      });
    } catch (err: any) {
      console.error('Failed to delete activity', err);
      const msg = err?.response?.data?.message || err?.message || 'Failed to delete activity. Please try again.';
      setStatusModal({
        isOpen: true,
        success: false,
        message: msg
      });
    } finally {
      setIsDeleting(false);
    }
  };

  const formatDisplayDate = (dateVal?: string) => {
    if (!dateVal) return '—';
    const clean = String(dateVal).split('T')[0];
    const parts = clean.split('-');
    if (parts.length === 3) {
      const [y, m, d] = parts.map(Number);
      return new Date(y, m - 1, d).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });
    }
    return dateVal;
  };

  const renderDetails = (activity: CropActivityDto) => {
    try {
      const data = JSON.parse(activity.detailsJson || '{}');
      const formattedDate = formatDisplayDate(data.date || activity.date);

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

                        <div className="activity-card-actions">
                          <div className="activity-logger">Logged by: {activity.loggedByUserName}</div>
                          {canModify && (
                            <>
                              <button 
                                type="button"
                                className="activity-action-btn edit" 
                                title="Edit Activity"
                                onClick={() => setEditingActivity(activity)}
                              >
                                <Pencil size={13} /> Edit
                              </button>
                              <button 
                                type="button"
                                className="activity-action-btn delete" 
                                title="Delete Activity"
                                onClick={() => setDeletingActivity(activity)}
                              >
                                <Trash2 size={13} /> Delete
                              </button>
                            </>
                          )}
                        </div>
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

      {/* Edit Activity Modal */}
      {editingActivity && (
        <EditActivityModal
          activity={editingActivity}
          cycle={cycles.find(c => c.id === editingActivity.cultivationCycleId)}
          onClose={() => setEditingActivity(null)}
          onSuccess={handleUpdateSuccess}
        />
      )}

      {/* Delete Confirmation Modal */}
      {deletingActivity && (
        <div 
          className="fc-modal-backdrop" 
          onClick={() => !isDeleting && setDeletingActivity(null)} 
          role="dialog" 
          aria-modal="true"
        >
          <div 
            className="fc-modal" 
            style={{ maxWidth: '30rem', textAlign: 'center', padding: '2rem 1.75rem' }} 
            onClick={e => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '0.25rem' }}>
              <button 
                type="button" 
                className="fc-modal-close" 
                onClick={() => setDeletingActivity(null)} 
                disabled={isDeleting} 
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </div>

            <div style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              margin: '-0.5rem auto 1.25rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: 'rgba(217, 83, 79, 0.15)',
              color: '#d9534f'
            }}>
              <Trash2 size={36} />
            </div>

            <h2 className="fc-modal-title" style={{ fontSize: '1.4rem', marginBottom: '0.5rem' }}>
              Delete Activity?
            </h2>

            <p style={{ color: 'var(--ink-soft)', marginBottom: '1.5rem', fontSize: '0.95rem', lineHeight: '1.5' }}>
              Are you sure you want to delete this <strong>{deletingActivity.activityType}</strong> activity recorded on <strong>{formatDisplayDate(deletingActivity.date)}</strong>? This action cannot be undone.
            </p>

            <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center' }}>
              <button 
                type="button" 
                className="fc-btn fc-btn-quiet" 
                onClick={() => setDeletingActivity(null)}
                disabled={isDeleting}
              >
                Cancel
              </button>
              <button 
                type="button" 
                className="fc-btn" 
                style={{ backgroundColor: '#d9534f', color: '#ffffff', borderColor: '#d9534f' }}
                onClick={handleConfirmDelete}
                disabled={isDeleting}
              >
                {isDeleting ? 'Deleting...' : 'Delete Activity'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Status Notification Modal */}
      {statusModal && (
        <div 
          className="fc-modal-backdrop" 
          onClick={() => setStatusModal(null)} 
          role="dialog" 
          aria-modal="true"
        >
          <div 
            className="fc-modal" 
            style={{ maxWidth: '28rem', textAlign: 'center', padding: '2rem 1.75rem' }} 
            onClick={e => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '0.25rem' }}>
              <button 
                type="button" 
                className="fc-modal-close" 
                onClick={() => setStatusModal(null)} 
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </div>

            <div style={{
              width: '60px',
              height: '60px',
              borderRadius: '50%',
              margin: '-0.5rem auto 1.25rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: statusModal.success ? 'rgba(74, 124, 89, 0.15)' : 'rgba(217, 83, 79, 0.15)',
              color: statusModal.success ? 'var(--forest)' : '#d9534f'
            }}>
              {statusModal.success ? <CheckCircle2 size={36} /> : <AlertCircle size={36} />}
            </div>

            <h2 className="fc-modal-title" style={{ fontSize: '1.3rem', marginBottom: '0.5rem' }}>
              {statusModal.success ? 'Success' : 'Error'}
            </h2>

            <p style={{ color: 'var(--ink-soft)', marginBottom: '1.5rem', fontSize: '0.95rem' }}>
              {statusModal.message}
            </p>

            <button 
              type="button" 
              className="fc-btn fc-btn-primary" 
              onClick={() => setStatusModal(null)}
            >
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
