import React, { useState, useEffect } from 'react';
import type { CultivationCycle } from '../../field-cultivation/types';

export type ActivityType = 'Irrigation' | 'Fertilizer' | 'Pesticide' | 'Other';

export interface IrrigationData {
  date: string;
  waterLevel: number;
  duration: number;
  source: string;
}

export interface FertilizerData {
  date: string;
  type: string;
  quantity: number;
  cropStage: string;
  region: string;
  method: string;
  cycleName?: string; // Passed down for validation UI
}

export interface PesticideData {
  date: string;
  product: string;
  targetPest: string;
  quantity: number;
  method: string;
}

export interface OtherData {
  date: string;
  specificActivity: string;
  notes: string;
}

export type ActivityData = IrrigationData | FertilizerData | PesticideData | OtherData;

interface ActivityFormProps {
  activityType: ActivityType;
  selectedCycle?: CultivationCycle | null;
  onSubmit: (data: ActivityData & { activityType: ActivityType, cycleId?: number }) => void;
  isSubmitting?: boolean;
}

export const ActivityForm: React.FC<ActivityFormProps> = ({ activityType, selectedCycle, onSubmit, isSubmitting }) => {
  const [formData, setFormData] = useState<Partial<IrrigationData & FertilizerData & PesticideData & OtherData>>({});

  useEffect(() => {
    if (selectedCycle && activityType === 'Fertilizer') {
      setFormData(prev => ({ ...prev, cropStage: selectedCycle.currentStage }));
    }
  }, [selectedCycle, activityType]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ 
      ...(formData as ActivityData), 
      activityType,
      cycleId: selectedCycle ? selectedCycle.id : undefined 
    });
  };

  return (
    <form onSubmit={handleSubmit} className="activity-form">
      {activityType === 'Irrigation' && (
        <>
          <div className="form-group">
            <label className="form-label">Date</label>
            <input type="date" name="date" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Water Level (cm)</label>
            <input type="number" name="waterLevel" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Duration (hours)</label>
            <input type="number" name="duration" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Source</label>
            <select name="source" className="form-input" required onChange={handleChange}>
              <option value="">Select source</option>
              <option value="Canal">Canal</option>
              <option value="Rain">Rain</option>
              <option value="Well">Well</option>
            </select>
          </div>
        </>
      )}

      {activityType === 'Fertilizer' && (
        <>
          <div className="form-group">
            <label className="form-label">Date</label>
            <input type="date" name="date" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Fertilizer Type</label>
            <select name="type" className="form-input" required onChange={handleChange}>
              <option value="">Select type</option>
              <option value="Urea">Urea</option>
              <option value="TSP">TSP</option>
              <option value="MOP">MOP</option>
              <option value="Organic">Organic Compost</option>
            </select>
          </div>
          <div className="form-group">
            <label className="form-label">Quantity (kg/ha)</label>
            <input type="number" name="quantity" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Crop Stage</label>
            <select name="cropStage" className="form-input" required onChange={handleChange} value={formData.cropStage || ''}>
              <option value="">Select stage</option>
              <option value="Nursery">Nursery</option>
              <option value="Tillering">Tillering</option>
              <option value="PanicleInitiation">Panicle Initiation</option>
              <option value="Flowering">Flowering</option>
              <option value="GrainFilling">Grain Filling</option>
              <option value="Harvest">Harvest</option>
            </select>
          </div>
          <div className="form-group">
            <label className="form-label">Region / Zone (For Recommendation)</label>
            <select name="region" className="form-input" required onChange={handleChange} value={formData.region || ''}>
              <option value="">Select climatic zone</option>
              <option value="Wet">Wet</option>
              <option value="Intermediate">Intermediate</option>
              <option value="Dry">Dry</option>
            </select>
          </div>
          <div className="form-group">
            <label className="form-label">Application Method</label>
            <select name="method" className="form-input" required onChange={handleChange}>
              <option value="">Select method</option>
              <option value="Broadcasting">Broadcasting</option>
              <option value="Top Dressing">Top Dressing</option>
            </select>
          </div>
        </>
      )}

      {activityType === 'Pesticide' && (
        <>
          <div className="form-group">
            <label className="form-label">Date</label>
            <input type="date" name="date" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Product Name</label>
            <input type="text" name="product" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Target Pest / Disease</label>
            <input type="text" name="targetPest" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Quantity (ml/g)</label>
            <input type="number" name="quantity" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Method</label>
            <select name="method" className="form-input" required onChange={handleChange}>
              <option value="">Select method</option>
              <option value="Spraying">Spraying</option>
              <option value="Dusting">Dusting</option>
            </select>
          </div>
        </>
      )}

      {activityType === 'Other' && (
        <>
          <div className="form-group">
            <label className="form-label">Date</label>
            <input type="date" name="date" className="form-input" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label className="form-label">Activity Type</label>
            <select name="specificActivity" className="form-input" required onChange={handleChange}>
              <option value="">Select activity</option>
              <option value="Land Preparation">Land Preparation</option>
              <option value="Seeding">Seeding</option>
              <option value="Transplanting">Transplanting</option>
              <option value="Weeding">Weeding</option>
              <option value="Harvesting">Harvesting</option>
            </select>
          </div>
          <div className="form-group">
            <label className="form-label">Notes</label>
            <textarea name="notes" className="form-input" rows={3} onChange={handleChange}></textarea>
          </div>
        </>
      )}

      <button type="submit" className="btn-submit" disabled={isSubmitting}>
        {isSubmitting ? 'Processing...' : `Record ${activityType}`}
      </button>

      {activityType === 'Fertilizer' && (
        <div className="info-note">
          <i>ℹ️ Fertilizer recommendations are validated against Department of Agriculture guidelines for your zone.</i>
        </div>
      )}
    </form>
  );
};
