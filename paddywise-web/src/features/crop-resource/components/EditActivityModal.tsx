import React, { useState } from 'react';
import { X } from 'lucide-react';
import { activityApi, type CropActivityDto } from '../services/activityApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import '../../field-cultivation/styles/fieldCultivation.css';
import '../pages/ActivityDashboard.css';

interface EditActivityModalProps {
  activity: CropActivityDto;
  cycle?: CultivationCycle | null;
  onClose: () => void;
  onSuccess: (updated: CropActivityDto) => void;
}

export const EditActivityModal: React.FC<EditActivityModalProps> = ({
  activity,
  cycle,
  onClose,
  onSuccess
}) => {
  const initialDetails = (() => {
    try {
      return JSON.parse(activity.detailsJson || '{}');
    } catch {
      return {};
    }
  })();

  const initialDate = (() => {
    const raw = initialDetails.date || activity.date;
    return raw ? String(raw).split('T')[0] : '';
  })();

  const [formData, setFormData] = useState<Record<string, any>>({
    ...initialDetails,
    date: initialDate
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const today = new Date().toISOString().split('T')[0];
  const sowingDate = cycle?.sowingDate ? cycle.sowingDate.split('T')[0] : undefined;
  const harvestDate = cycle?.actualHarvestDate ? cycle.actualHarvestDate.split('T')[0] : undefined;

  // Last week (past 7 days)
  const oneWeekAgo = new Date();
  oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);
  const oneWeekAgoStr = oneWeekAgo.toISOString().split('T')[0];

  const minDate = sowingDate && sowingDate > oneWeekAgoStr ? sowingDate : oneWeekAgoStr;
  const maxDate = harvestDate && harvestDate < today ? harvestDate : today;

  const validateDate = (dateVal?: string): string | null => {
    if (!dateVal || !dateVal.trim()) {
      return 'Activity date is required.';
    }
    if (dateVal > today) {
      return 'Activity date cannot be in the future.';
    }
    if (dateVal < oneWeekAgoStr) {
      return `Activity date must be within the last week (from ${oneWeekAgoStr} to today).`;
    }
    if (sowingDate && dateVal < sowingDate) {
      return `Activity date cannot be earlier than cycle sowing date (${sowingDate}).`;
    }
    if (harvestDate && dateVal > harvestDate) {
      return `Activity date cannot be after harvest date (${harvestDate}).`;
    }
    return null;
  };

  const validateForm = (data: Record<string, any>): Record<string, string> => {
    const errs: Record<string, string> = {};

    const dateErr = validateDate(data.date);
    if (dateErr) errs.date = dateErr;

    if (activity.activityType === 'Fertilizer') {
      if (!data.type || !String(data.type).trim()) {
        errs.type = 'Please select a fertilizer type.';
      }
      if (data.quantity === undefined || data.quantity === null || data.quantity === '') {
        errs.quantity = 'Quantity is required.';
      } else {
        const qty = Number(data.quantity);
        if (isNaN(qty) || qty <= 0) {
          errs.quantity = 'Quantity must be greater than 0.';
        } else if (qty > 5000) {
          errs.quantity = 'Quantity exceeds maximum limit (5,000 kg/ha).';
        }
      }
      if (!data.cropStage || !String(data.cropStage).trim()) {
        errs.cropStage = 'Please select crop stage.';
      }
      if (!data.region || !String(data.region).trim()) {
        errs.region = 'Please select climatic zone.';
      }
      if (!data.method || !String(data.method).trim()) {
        errs.method = 'Please select application method.';
      }
    } else if (activity.activityType === 'Irrigation') {
      if (data.waterLevel === undefined || data.waterLevel === null || data.waterLevel === '') {
        errs.waterLevel = 'Water level is required.';
      } else {
        const wl = Number(data.waterLevel);
        if (isNaN(wl) || wl < 0) {
          errs.waterLevel = 'Water level must be 0 or greater.';
        }
      }
      if (data.duration === undefined || data.duration === null || data.duration === '') {
        errs.duration = 'Duration is required.';
      } else {
        const dur = Number(data.duration);
        if (isNaN(dur) || dur <= 0) {
          errs.duration = 'Duration must be greater than 0.';
        }
      }
      if (!data.source || !String(data.source).trim()) {
        errs.source = 'Please select water source.';
      }
    } else if (activity.activityType === 'Pesticide') {
      if (!data.product || !String(data.product).trim()) {
        errs.product = 'Product name is required.';
      }
      if (!data.targetPest || !String(data.targetPest).trim()) {
        errs.targetPest = 'Target pest/disease is required.';
      }
      if (data.quantity === undefined || data.quantity === null || data.quantity === '') {
        errs.quantity = 'Quantity is required.';
      } else {
        const qty = Number(data.quantity);
        if (isNaN(qty) || qty <= 0) {
          errs.quantity = 'Quantity must be greater than 0.';
        }
      }
      if (!data.method || !String(data.method).trim()) {
        errs.method = 'Please select application method.';
      }
    } else if (activity.activityType === 'Other') {
      if (!data.specificActivity || !String(data.specificActivity).trim()) {
        errs.specificActivity = 'Please select activity type.';
      }
    }

    return errs;
  };

  const handleBlur = (field: string) => {
    setTouched(prev => ({ ...prev, [field]: true }));
    const errs = validateForm(formData);
    setErrors(errs);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    const nextFormData = {
      ...formData,
      [name]: value
    };
    setFormData(nextFormData);

    if (touched[name]) {
      const errs = validateForm(nextFormData);
      setErrors(errs);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    const validationErrors = validateForm(formData);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    try {
      setIsSubmitting(true);
      const cleanDetails: Record<string, any> = { ...formData };
      if (activity.activityType === 'Fertilizer' && cleanDetails.quantity !== undefined) {
        cleanDetails.quantity = Number(cleanDetails.quantity);
      } else if (activity.activityType === 'Irrigation') {
        if (cleanDetails.waterLevel !== undefined) cleanDetails.waterLevel = Number(cleanDetails.waterLevel);
        if (cleanDetails.duration !== undefined) cleanDetails.duration = Number(cleanDetails.duration);
      } else if (activity.activityType === 'Pesticide' && cleanDetails.quantity !== undefined) {
        cleanDetails.quantity = Number(cleanDetails.quantity);
      }

      const payload = {
        activityType: activity.activityType,
        date: formData.date,
        detailsJson: JSON.stringify(cleanDetails)
      };

      const updated = await activityApi.updateActivity(activity.id, payload);
      onSuccess(updated);
    } catch (err: any) {
      console.error('Failed to update activity', err);
      const msg = err?.response?.data?.message || err?.message || 'Failed to update activity. Please try again.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const isFieldInvalid = (field: string) => touched[field] && !!errors[field];

  const renderFieldError = (field: string) => {
    if (touched[field] && errors[field]) {
      return <span className="field-error-text">⚠️ {errors[field]}</span>;
    }
    return null;
  };

  return (
    <div className="fc-modal-backdrop" onClick={onClose} role="dialog" aria-modal="true">
      <div 
        className="fc-modal" 
        style={{ maxWidth: '38rem', maxHeight: '90vh' }} 
        onClick={(e) => e.stopPropagation()}
      >
        <header className="fc-modal-head">
          <div>
            <span className="eyebrow" style={{ fontSize: '0.8rem', color: 'var(--ink-soft)' }}>UPDATE OPERATION</span>
            <h2 className="fc-modal-title" style={{ fontSize: '1.4rem' }}>
              Edit {activity.activityType} Activity
            </h2>
          </div>
          <button type="button" className="fc-modal-close" onClick={onClose} aria-label="Close">
            <X size={20} />
          </button>
        </header>

        {errorMessage && (
          <div className="form-validation-summary" role="alert">
            <span>⚠️ {errorMessage}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          {/* Date field */}
          <div className="form-group">
            <label className="form-label" htmlFor="edit-activity-date">
              Activity Date <span style={{ color: '#d9534f' }}>*</span>
            </label>
            <input 
              id="edit-activity-date"
              type="date" 
              name="date" 
              className={`form-input ${isFieldInvalid('date') ? 'input-error' : ''}`}
              required 
              min={minDate}
              max={maxDate}
              value={formData.date || ''} 
              onChange={handleChange}
              onBlur={() => handleBlur('date')}
            />
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '0.35rem', fontSize: '0.8rem', color: 'var(--ink-soft)' }}>
              <span>Allowed: Within the last week ({minDate} to {maxDate === today ? `Today ${today}` : maxDate})</span>
            </div>
            {renderFieldError('date')}
          </div>

          {/* Activity-specific fields */}
          {activity.activityType === 'Fertilizer' && (
            <>
              <div className="form-group">
                <label className="form-label">
                  Fertilizer Type <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="type" 
                  className={`form-input ${isFieldInvalid('type') ? 'input-error' : ''}`}
                  required 
                  value={formData.type || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('type')}
                >
                  <option value="">Select fertilizer type</option>
                  <option value="Urea">Urea</option>
                  <option value="TSP">TSP</option>
                  <option value="MOP">MOP</option>
                  <option value="Organic">Organic Compost</option>
                </select>
                {renderFieldError('type')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Quantity (kg/ha) <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <input 
                  type="number" 
                  step="any" 
                  name="quantity" 
                  className={`form-input ${isFieldInvalid('quantity') ? 'input-error' : ''}`}
                  required 
                  min="1"
                  value={formData.quantity ?? ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('quantity')}
                />
                {renderFieldError('quantity')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Crop Stage <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="cropStage" 
                  className={`form-input ${isFieldInvalid('cropStage') ? 'input-error' : ''}`}
                  required 
                  onChange={handleChange} 
                  onBlur={() => handleBlur('cropStage')}
                  value={formData.cropStage || ''}
                >
                  <option value="">Select crop stage</option>
                  <option value="Nursery">Nursery</option>
                  <option value="Tillering">Tillering</option>
                  <option value="PanicleInitiation">Panicle Initiation</option>
                  <option value="Flowering">Flowering</option>
                  <option value="GrainFilling">Grain Filling</option>
                  <option value="Harvest">Harvest</option>
                </select>
                {renderFieldError('cropStage')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Region / Zone <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="region" 
                  className={`form-input ${isFieldInvalid('region') ? 'input-error' : ''}`}
                  required 
                  onChange={handleChange} 
                  onBlur={() => handleBlur('region')}
                  value={formData.region || ''}
                >
                  <option value="">Select climatic zone</option>
                  <option value="Wet">Wet</option>
                  <option value="Intermediate">Intermediate</option>
                  <option value="Dry">Dry</option>
                </select>
                {renderFieldError('region')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Application Method <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="method" 
                  className={`form-input ${isFieldInvalid('method') ? 'input-error' : ''}`}
                  required 
                  value={formData.method || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('method')}
                >
                  <option value="">Select application method</option>
                  <option value="Broadcasting">Broadcasting</option>
                  <option value="Top Dressing">Top Dressing</option>
                </select>
                {renderFieldError('method')}
              </div>
            </>
          )}

          {activity.activityType === 'Irrigation' && (
            <>
              <div className="form-group">
                <label className="form-label">
                  Water Level (cm) <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <input 
                  type="number" 
                  name="waterLevel" 
                  className={`form-input ${isFieldInvalid('waterLevel') ? 'input-error' : ''}`}
                  required 
                  min="0"
                  value={formData.waterLevel ?? ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('waterLevel')}
                />
                {renderFieldError('waterLevel')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Duration (hours) <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <input 
                  type="number" 
                  name="duration" 
                  className={`form-input ${isFieldInvalid('duration') ? 'input-error' : ''}`}
                  required 
                  min="0.5"
                  step="any"
                  value={formData.duration ?? ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('duration')}
                />
                {renderFieldError('duration')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Source <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="source" 
                  className={`form-input ${isFieldInvalid('source') ? 'input-error' : ''}`}
                  required 
                  value={formData.source || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('source')}
                >
                  <option value="">Select source</option>
                  <option value="Canal">Canal</option>
                  <option value="Rain">Rain</option>
                  <option value="Well">Well</option>
                </select>
                {renderFieldError('source')}
              </div>
            </>
          )}

          {activity.activityType === 'Pesticide' && (
            <>
              <div className="form-group">
                <label className="form-label">
                  Product Name <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <input 
                  type="text" 
                  name="product" 
                  className={`form-input ${isFieldInvalid('product') ? 'input-error' : ''}`}
                  required 
                  value={formData.product || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('product')}
                />
                {renderFieldError('product')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Target Pest / Disease <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <input 
                  type="text" 
                  name="targetPest" 
                  className={`form-input ${isFieldInvalid('targetPest') ? 'input-error' : ''}`}
                  required 
                  value={formData.targetPest || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('targetPest')}
                />
                {renderFieldError('targetPest')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Quantity (ml/g) <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <input 
                  type="number" 
                  step="any" 
                  name="quantity" 
                  className={`form-input ${isFieldInvalid('quantity') ? 'input-error' : ''}`}
                  required 
                  min="0.1"
                  value={formData.quantity ?? ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('quantity')}
                />
                {renderFieldError('quantity')}
              </div>
              <div className="form-group">
                <label className="form-label">
                  Method <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="method" 
                  className={`form-input ${isFieldInvalid('method') ? 'input-error' : ''}`}
                  required 
                  value={formData.method || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('method')}
                >
                  <option value="">Select application method</option>
                  <option value="Spraying">Spraying</option>
                  <option value="Dusting">Dusting</option>
                </select>
                {renderFieldError('method')}
              </div>
            </>
          )}

          {activity.activityType === 'Other' && (
            <>
              <div className="form-group">
                <label className="form-label">
                  Activity Type <span style={{ color: '#d9534f' }}>*</span>
                </label>
                <select 
                  name="specificActivity" 
                  className={`form-input ${isFieldInvalid('specificActivity') ? 'input-error' : ''}`}
                  required 
                  value={formData.specificActivity || ''} 
                  onChange={handleChange}
                  onBlur={() => handleBlur('specificActivity')}
                >
                  <option value="">Select activity type</option>
                  <option value="Land Preparation">Land Preparation</option>
                  <option value="Seeding">Seeding</option>
                  <option value="Transplanting">Transplanting</option>
                  <option value="Weeding">Weeding</option>
                  <option value="Harvesting">Harvesting</option>
                </select>
                {renderFieldError('specificActivity')}
              </div>
              <div className="form-group">
                <label className="form-label">Notes</label>
                <textarea 
                  name="notes" 
                  className="form-input" 
                  rows={3} 
                  value={formData.notes || ''} 
                  onChange={handleChange}
                  placeholder="Optional notes..."
                ></textarea>
              </div>
            </>
          )}

          <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '2rem' }}>
            <button 
              type="button" 
              className="fc-btn fc-btn-quiet" 
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button 
              type="submit" 
              className="fc-btn fc-btn-primary" 
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Saving Changes...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
