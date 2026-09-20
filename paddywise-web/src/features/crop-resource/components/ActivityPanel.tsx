import React, { useState } from 'react';
import { ActivityForm, type ActivityType } from './ActivityForm';
import { FertilizerValidationFlow, type FertilizerData } from './FertilizerValidationFlow';
import type { CultivationCycle } from '../../field-cultivation/types';
import '../pages/ActivityDashboard.css';
import { activityApi } from '../services/activityApi';

interface ActivityPanelProps {
  selectedCycle?: CultivationCycle | null;
}

export const ActivityPanel: React.FC<ActivityPanelProps> = ({ selectedCycle }) => {
  const [activeTab, setActiveTab] = useState<ActivityType>('Fertilizer');
  const [validationData, setValidationData] = useState<FertilizerData | null>(null);

  const handleActivitySubmit = async (data: any) => {
    if (!selectedCycle) {
      alert("No cycle selected.");
      return;
    }

    try {
      const requestPayload = {
        activityType: data.activityType,
        date: data.date,
        detailsJson: JSON.stringify(data)
      };

      await activityApi.createActivity(selectedCycle.id, requestPayload);

      if (data.activityType === 'Fertilizer') {
        setValidationData({
          ...data,
          cycleName: `${selectedCycle.season} ${selectedCycle.year} - ${selectedCycle.fieldName}`
        } as FertilizerData);
      } else {
        alert(`${data.activityType} activity recorded successfully!`);
      }
    } catch (error) {
      console.error("Failed to save activity", error);
      alert("Failed to save activity. Please try again.");
    }
  };

  const handleCloseValidation = () => {
    setValidationData(null);
  };

  return (
    <div className={`activity-grid ${validationData ? 'with-validation' : ''}`}>
      <div className="activity-panel" style={{ opacity: validationData ? 0.7 : 1, transition: 'opacity 0.3s' }}>
        
        <div className="activity-selector">
          {['Irrigation', 'Fertilizer', 'Pesticide', 'Other'].map((type) => (
            <div
              key={type}
              className={`activity-tab ${activeTab === type ? 'active' : ''}`}
              onClick={() => {
                if (!validationData) setActiveTab(type as ActivityType);
              }}
              style={{ cursor: validationData ? 'not-allowed' : 'pointer' }}
            >
              {type}
            </div>
          ))}
        </div>

        <h2 className="activity-title">
          Record {activeTab} Activity
        </h2>

        <ActivityForm
          activityType={activeTab}
          selectedCycle={selectedCycle}
          onSubmit={handleActivitySubmit}
          isSubmitting={!!validationData}
        />
      </div>

      {validationData && (
        <div className="activity-panel" style={{ animation: 'fadeInRight 0.5s ease-out' }}>
          <FertilizerValidationFlow
            data={validationData}
            onClose={handleCloseValidation}
          />
        </div>
      )}
    </div>
  );
};
