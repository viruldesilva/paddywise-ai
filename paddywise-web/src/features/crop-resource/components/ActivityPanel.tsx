import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { CheckCircle2, AlertCircle, X } from 'lucide-react';
import { ActivityForm, type ActivityType } from './ActivityForm';
import type { CultivationCycle } from '../../field-cultivation/types';
import '../pages/ActivityDashboard.css';
import '../../field-cultivation/styles/fieldCultivation.css';
import { activityApi } from '../services/activityApi';

interface ActivityPanelProps {
  selectedCycle?: CultivationCycle | null;
  onActivityRecorded?: () => void;
}

interface ModalStatus {
  isOpen: boolean;
  success: boolean;
  message: string;
  data?: any;
}

export const ActivityPanel: React.FC<ActivityPanelProps> = ({ selectedCycle, onActivityRecorded }) => {
  const [activeTab, setActiveTab] = useState<ActivityType>('Fertilizer');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [modalStatus, setModalStatus] = useState<ModalStatus | null>(null);
  const [formKey, setFormKey] = useState(0);
  const navigate = useNavigate();

  const handleActivitySubmit = async (data: any) => {
    if (!selectedCycle) {
      setModalStatus({
        isOpen: true,
        success: false,
        message: "No cultivation cycle selected. Please select a valid cycle before saving."
      });
      return;
    }

    const dateVal = data.date;
    const today = new Date().toISOString().split('T')[0];
    const sowing = selectedCycle.sowingDate ? selectedCycle.sowingDate.split('T')[0] : null;
    const harvest = selectedCycle.actualHarvestDate ? selectedCycle.actualHarvestDate.split('T')[0] : null;

    const oneWeekAgo = new Date();
    oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);
    const oneWeekAgoStr = oneWeekAgo.toISOString().split('T')[0];

    if (!dateVal) {
      setModalStatus({
        isOpen: true,
        success: false,
        message: "Activity date is required. Please enter a valid date."
      });
      return;
    }
    if (dateVal > today) {
      setModalStatus({
        isOpen: true,
        success: false,
        message: `Invalid date: Activity date (${dateVal}) cannot be in the future.`
      });
      return;
    }
    if (dateVal < oneWeekAgoStr) {
      setModalStatus({
        isOpen: true,
        success: false,
        message: `Invalid date: Activity date (${dateVal}) cannot be older than the past week (${oneWeekAgoStr}). Activities can only be recorded within the last 7 days.`
      });
      return;
    }
    if (sowing && dateVal < sowing) {
      setModalStatus({
        isOpen: true,
        success: false,
        message: `Invalid date: Activity date (${dateVal}) cannot be earlier than cycle sowing date (${sowing}).`
      });
      return;
    }
    if (harvest && dateVal > harvest) {
      setModalStatus({
        isOpen: true,
        success: false,
        message: `Invalid date: Activity date (${dateVal}) cannot be after cycle harvest date (${harvest}).`
      });
      return;
    }

    if (data.activityType === 'Fertilizer') {
      if (!data.type) {
        setModalStatus({ isOpen: true, success: false, message: "Please select a fertilizer type." });
        return;
      }
      if (!data.quantity || Number(data.quantity) <= 0) {
        setModalStatus({ isOpen: true, success: false, message: "Fertilizer quantity must be greater than 0 kg/ha." });
        return;
      }
      if (!data.cropStage) {
        setModalStatus({ isOpen: true, success: false, message: "Please select a crop stage." });
        return;
      }
      if (!data.region) {
        setModalStatus({ isOpen: true, success: false, message: "Please select a climatic region/zone." });
        return;
      }
      if (!data.method) {
        setModalStatus({ isOpen: true, success: false, message: "Please select an application method." });
        return;
      }
    } else if (data.activityType === 'Irrigation') {
      if (data.waterLevel === undefined || data.waterLevel === '' || Number(data.waterLevel) < 0) {
        setModalStatus({ isOpen: true, success: false, message: "Water level must be 0 cm or greater." });
        return;
      }
      if (!data.duration || Number(data.duration) <= 0) {
        setModalStatus({ isOpen: true, success: false, message: "Duration must be greater than 0 hours." });
        return;
      }
      if (!data.source) {
        setModalStatus({ isOpen: true, success: false, message: "Please select a water source." });
        return;
      }
    } else if (data.activityType === 'Pesticide') {
      if (!data.product || !data.product.trim()) {
        setModalStatus({ isOpen: true, success: false, message: "Product name is required." });
        return;
      }
      if (!data.targetPest || !data.targetPest.trim()) {
        setModalStatus({ isOpen: true, success: false, message: "Target pest or disease is required." });
        return;
      }
      if (!data.quantity || Number(data.quantity) <= 0) {
        setModalStatus({ isOpen: true, success: false, message: "Quantity must be greater than 0 ml/g." });
        return;
      }
      if (!data.method) {
        setModalStatus({ isOpen: true, success: false, message: "Please select an application method." });
        return;
      }
    } else if (data.activityType === 'Other') {
      if (!data.specificActivity) {
        setModalStatus({ isOpen: true, success: false, message: "Please select an activity type." });
        return;
      }
    }

    try {
      setIsSubmitting(true);

      const requestPayload = {
        activityType: data.activityType,
        date: data.date,
        detailsJson: JSON.stringify(data)
      };

      await activityApi.createActivity(selectedCycle.id, requestPayload);

      setModalStatus({
        isOpen: true,
        success: true,
        message: `${data.activityType} activity data was saved successfully!`,
        data
      });

      if (onActivityRecorded) {
        onActivityRecorded();
      }
    } catch (error: any) {
      console.error("Failed to save activity", error);
      const errorMsg = error?.response?.data?.message || error?.message || "Failed to save activity data. Please check your connection and try again.";
      setModalStatus({
        isOpen: true,
        success: false,
        message: errorMsg
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="activity-grid">
      <div className="activity-panel">
        <div className="activity-selector">
          {['Fertilizer', 'Irrigation', 'Pesticide', 'Other'].map((type) => (
            <div
              key={type}
              className={`activity-tab ${activeTab === type ? 'active' : ''}`}
              onClick={() => {
                setActiveTab(type as ActivityType);
              }}
            >
              {type}
            </div>
          ))}
        </div>

        <h2 className="activity-title">
          {activeTab} Activity Form
        </h2>

        <ActivityForm
          key={`${activeTab}-${formKey}`}
          activityType={activeTab}
          selectedCycle={selectedCycle}
          onSubmit={handleActivitySubmit}
          isSubmitting={isSubmitting}
        />
      </div>

      {/* Save Status Popup Modal */}
      {modalStatus && modalStatus.isOpen && (
        <div 
          className="fc-modal-backdrop" 
          role="dialog" 
          aria-modal="true"
          onClick={() => setModalStatus(null)}
        >
          <div 
            className="fc-modal" 
            style={{ maxWidth: '30rem', textAlign: 'center', padding: '2rem 1.75rem' }} 
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '0.25rem' }}>
              <button 
                type="button" 
                className="fc-modal-close" 
                onClick={() => setModalStatus(null)} 
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </div>

            <div style={{
              width: '64px',
              height: '64px',
              borderRadius: '50%',
              margin: '0 auto 1.25rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: modalStatus.success ? 'rgba(74, 124, 89, 0.15)' : 'rgba(217, 83, 79, 0.15)',
              color: modalStatus.success ? 'var(--forest)' : '#d9534f'
            }}>
              {modalStatus.success ? (
                <CheckCircle2 size={38} />
              ) : (
                <AlertCircle size={38} />
              )}
            </div>

            <h2 className="fc-modal-title" style={{ fontSize: '1.4rem', marginBottom: '0.5rem' }}>
              {modalStatus.success ? 'Data Saved Successfully' : 'Failed to Save Data'}
            </h2>

            <p style={{ color: 'var(--ink-soft)', marginBottom: '1.5rem', fontSize: '0.95rem', lineHeight: '1.5' }}>
              {modalStatus.message}
            </p>

            {modalStatus.success && modalStatus.data && (
              <div style={{
                background: 'var(--cream-deep)',
                border: '1px solid var(--line)',
                borderRadius: '10px',
                padding: '1rem',
                marginBottom: '1.5rem',
                textAlign: 'left',
                fontSize: '0.875rem'
              }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.6rem' }}>
                  <div><span style={{ color: 'var(--ink-soft)' }}>Type:</span> <strong>{modalStatus.data.activityType}</strong></div>
                  <div><span style={{ color: 'var(--ink-soft)' }}>Date:</span> <strong>{modalStatus.data.date}</strong></div>
                  {modalStatus.data.type && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Fertilizer:</span> <strong>{modalStatus.data.type}</strong></div>
                  )}
                  {modalStatus.data.quantity && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Quantity:</span> <strong>{modalStatus.data.quantity} kg/ha</strong></div>
                  )}
                  {modalStatus.data.cropStage && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Stage:</span> <strong>{modalStatus.data.cropStage}</strong></div>
                  )}
                  {modalStatus.data.region && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Region:</span> <strong>{modalStatus.data.region}</strong></div>
                  )}
                  {modalStatus.data.method && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Method:</span> <strong>{modalStatus.data.method}</strong></div>
                  )}
                  {modalStatus.data.waterLevel && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Water Level:</span> <strong>{modalStatus.data.waterLevel} cm</strong></div>
                  )}
                  {modalStatus.data.duration && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Duration:</span> <strong>{modalStatus.data.duration} hrs</strong></div>
                  )}
                  {modalStatus.data.source && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Source:</span> <strong>{modalStatus.data.source}</strong></div>
                  )}
                  {modalStatus.data.product && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Product:</span> <strong>{modalStatus.data.product}</strong></div>
                  )}
                  {modalStatus.data.targetPest && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Target:</span> <strong>{modalStatus.data.targetPest}</strong></div>
                  )}
                  {modalStatus.data.specificActivity && (
                    <div><span style={{ color: 'var(--ink-soft)' }}>Activity:</span> <strong>{modalStatus.data.specificActivity}</strong></div>
                  )}
                </div>
              </div>
            )}

            <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}>
              {modalStatus.success ? (
                <>
                  <button
                    type="button"
                    className="fc-btn fc-btn-primary"
                    onClick={() => {
                      setModalStatus(null);
                      navigate(`/cycles/${selectedCycle?.id}`);
                    }}
                  >
                    Back to Cycle
                  </button>
                  <button
                    type="button"
                    className="fc-btn fc-btn-outline"
                    onClick={() => {
                      setModalStatus(null);
                      navigate('/activities');
                    }}
                  >
                    View All Activities
                  </button>
                  <button
                    type="button"
                    className="fc-btn fc-btn-quiet"
                    onClick={() => {
                      setModalStatus(null);
                      setFormKey(prev => prev + 1);
                    }}
                  >
                    Add Another Activity
                  </button>
                </>
              ) : (
                <>
                  <button
                    type="button"
                    className="fc-btn fc-btn-primary"
                    onClick={() => setModalStatus(null)}
                  >
                    Try Again
                  </button>
                  <button
                    type="button"
                    className="fc-btn fc-btn-outline"
                    onClick={() => {
                      setModalStatus(null);
                      navigate(`/cycles/${selectedCycle?.id}`);
                    }}
                  >
                    Cancel & Return
                  </button>
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
