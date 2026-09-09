import React, { useState } from 'react';
import { ActivityForm } from '../components/ActivityForm';
import { FertilizerValidationFlow, type FertilizerData } from '../components/FertilizerValidationFlow';
import '../assets/Dashboard.css';

type ActivityType = 'Irrigation' | 'Fertilizer' | 'Pesticide' | 'Other';

export const ActivityDashboard: React.FC = () => {
  const [activeTab, setActiveTab] = useState<ActivityType>('Fertilizer');
  const [validationData, setValidationData] = useState<FertilizerData | null>(null);

  const handleActivitySubmit = (data: any) => {
    console.log('Submitted Activity:', data);

    if (data.activityType === 'Fertilizer') {
      // Trigger validation flow
      setValidationData(data as FertilizerData);
    } else {
      // For other activities, just show a success alert for now
      alert(`${data.activityType} activity recorded successfully!`);
    }
  };

  const handleCloseValidation = () => {
    setValidationData(null);
  };

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <h1>Crop Activity & Resource Management</h1>
        <p>Record and monitor your agricultural operations</p>
      </div>

      <div className={`activity-grid ${validationData ? 'with-validation' : ''}`}>
        <div className="glass-panel" style={{ opacity: validationData ? 0.7 : 1, transition: 'opacity 0.3s' }}>
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

          <h2 style={{ marginBottom: '20px', color: 'var(--primary-dark)', fontSize: '1.5rem' }}>
            Record {activeTab} Activity
          </h2>

          <ActivityForm
            activityType={activeTab}
            onSubmit={handleActivitySubmit}
            isSubmitting={!!validationData}
          />
        </div>

        {validationData && (
          <div className="glass-panel" style={{ animation: 'fadeInRight 0.5s ease-out' }}>
            <FertilizerValidationFlow
              data={validationData}
              onClose={handleCloseValidation}
            />
          </div>
        )}
      </div>

      <style dangerouslySetInnerHTML={{
        __html: `
        @keyframes fadeInRight {
          from { opacity: 0; transform: translateX(20px); }
          to { opacity: 1; transform: translateX(0); }
        }
      `}} />
    </div>
  );
};
