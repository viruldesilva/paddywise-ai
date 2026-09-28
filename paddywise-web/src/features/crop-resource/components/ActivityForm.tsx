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
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [hasSubmitted, setHasSubmitted] = useState(false);

  const today = new Date().toISOString().split('T')[0];
  const sowingDate = selectedCycle?.sowingDate ? selectedCycle.sowingDate.split('T')[0] : undefined;
  const harvestDate = selectedCycle?.actualHarvestDate ? selectedCycle.actualHarvestDate.split('T')[0] : undefined;

  // Last week (past 7 days)
  const oneWeekAgo = new Date();
  oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);
  const oneWeekAgoStr = oneWeekAgo.toISOString().split('T')[0];

  // Allowed range: within the last week, and not before sowing date
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

  const validateForm = (data: typeof formData): Record<string, string> => {
    const errs: Record<string, string> = {};

    // Date validation
    const dateErr = validateDate(data.date);
    if (dateErr) errs.date = dateErr;

    // Fertilizer validation
    if (activityType === 'Fertilizer') {
      if (!data.type || !data.type.trim()) {
        errs.type = 'Please select a fertilizer type.';
      }
      if (data.quantity === undefined || data.quantity === null || (data.quantity as any) === '') {
        errs.quantity = 'Quantity is required.';
      } else {
        const qty = Number(data.quantity);
        if (isNaN(qty) || qty <= 0) {
          errs.quantity = 'Quantity must be a positive number greater than 0.';
        } else if (qty > 5000) {
          errs.quantity = 'Quantity exceeds maximum limit (5,000 kg/ha).';
        }
      }
      if (!data.cropStage || !data.cropStage.trim()) {
        errs.cropStage = 'Please select the crop growth stage.';
      }
      if (!data.region || !data.region.trim()) {
        errs.region = 'Please select a climatic zone / region.';
      }
      if (!data.method || !data.method.trim()) {
        errs.method = 'Please select an application method.';
      }
    }

    // Irrigation validation
    if (activityType === 'Irrigation') {
      if (data.waterLevel === undefined || data.waterLevel === null || (data.waterLevel as any) === '') {
        errs.waterLevel = 'Water level is required.';
      } else {
        const wl = Number(data.waterLevel);
        if (isNaN(wl) || wl < 0) {
          errs.waterLevel = 'Water level must be 0 or greater.';
        } else if (wl > 150) {
          errs.waterLevel = 'Water level cannot exceed 150 cm.';
        }
      }
      if (data.duration === undefined || data.duration === null || (data.duration as any) === '') {
        errs.duration = 'Duration is required.';
      } else {
        const dur = Number(data.duration);
        if (isNaN(dur) || dur <= 0) {
          errs.duration = 'Duration must be greater than 0 hours.';
        } else if (dur > 72) {
          errs.duration = 'Duration cannot exceed 72 hours.';
        }
      }
      if (!data.source || !data.source.trim()) {
        errs.source = 'Please select a water source.';
      }
    }

    // Pesticide validation
    if (activityType === 'Pesticide') {
      if (!data.product || !data.product.trim()) {
        errs.product = 'Product name is required.';
      } else if (data.product.trim().length < 2) {
        errs.product = 'Product name must be at least 2 characters.';
      } else if (data.product.trim().length > 100) {
        errs.product = 'Product name cannot exceed 100 characters.';
      }

      if (!data.targetPest || !data.targetPest.trim()) {
        errs.targetPest = 'Target pest / disease is required.';
      } else if (data.targetPest.trim().length < 2) {
        errs.targetPest = 'Target pest/disease must be at least 2 characters.';
      } else if (data.targetPest.trim().length > 100) {
        errs.targetPest = 'Target pest/disease cannot exceed 100 characters.';
      }

      if (data.quantity === undefined || data.quantity === null || (data.quantity as any) === '') {
        errs.quantity = 'Quantity is required.';
      } else {
        const qty = Number(data.quantity);
        if (isNaN(qty) || qty <= 0) {
          errs.quantity = 'Quantity must be greater than 0.';
        } else if (qty > 100000) {
          errs.quantity = 'Quantity exceeds maximum limit.';
        }
      }

      if (!data.method || !data.method.trim()) {
        errs.method = 'Please select an application method.';
      }
    }

    // Other validation
    if (activityType === 'Other') {
      if (!data.specificActivity || !data.specificActivity.trim()) {
        errs.specificActivity = 'Please select an activity type.';
      }
      if (data.notes && data.notes.length > 500) {
        errs.notes = 'Notes cannot exceed 500 characters.';
      }
    }

    return errs;
  };

  useEffect(() => {
    const initialDate = formData.date || today;
    const updated = {
      ...formData,
      date: initialDate,
      cropStage: activityType === 'Fertilizer' ? (formData.cropStage || selectedCycle?.currentStage || '') : formData.cropStage
    };
    setFormData(updated);
    setErrors({});
    setTouched({});
    setHasSubmitted(false);
  }, [selectedCycle, activityType]);

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

    if (touched[name] || hasSubmitted) {
      const errs = validateForm(nextFormData);
      setErrors(errs);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setHasSubmitted(true);

    const touchedFields: Record<string, boolean> = { date: true };
    if (activityType === 'Fertilizer') {
      touchedFields.type = true;
      touchedFields.quantity = true;
      touchedFields.cropStage = true;
      touchedFields.region = true;
      touchedFields.method = true;
    } else if (activityType === 'Irrigation') {
      touchedFields.waterLevel = true;
      touchedFields.duration = true;
      touchedFields.source = true;
    } else if (activityType === 'Pesticide') {
      touchedFields.product = true;
      touchedFields.targetPest = true;
      touchedFields.quantity = true;
      touchedFields.method = true;
    } else if (activityType === 'Other') {
      touchedFields.specificActivity = true;
      touchedFields.notes = true;
    }
    setTouched(touchedFields);

    const validationErrors = validateForm(formData);
    setErrors(validationErrors);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    onSubmit({ 
      ...(formData as ActivityData), 
      activityType,
      cycleId: selectedCycle ? selectedCycle.id : undefined 
    });
  };

  const renderFieldError = (field: string) => {
    if ((touched[field] || hasSubmitted) && errors[field]) {
      return (
        <span className="field-error-text">
          ⚠️ {errors[field]}
        </span>
      );
    }
    return null;
  };

  const isFieldInvalid = (field: string) => (touched[field] || hasSubmitted) && !!errors[field];

  const renderDateField = () => (
    <div className="form-group">
      <label className="form-label" htmlFor={`activity-date-${activityType}`}>
        Activity Date <span style={{ color: '#d9534f' }}>*</span>
      </label>
      <input 
        id={`activity-date-${activityType}`}
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
  );

  const hasAnyErrors = hasSubmitted && Object.keys(errors).length > 0;

  return (
    <form onSubmit={handleSubmit} className="activity-form" noValidate>
      {hasAnyErrors && (
        <div className="form-validation-summary" role="alert">
          <span>⚠️ Please correct the highlighted fields before saving the activity.</span>
        </div>
      )}

      {activityType === 'Irrigation' && (
        <>
          {renderDateField()}
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
              max="150"
              value={formData.waterLevel ?? ''} 
              onChange={handleChange}
              onBlur={() => handleBlur('waterLevel')}
              placeholder="e.g. 5"
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
              max="72"
              step="any"
              value={formData.duration ?? ''} 
              onChange={handleChange}
              onBlur={() => handleBlur('duration')}
              placeholder="e.g. 2"
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

      {activityType === 'Fertilizer' && (
        <>
          {renderDateField()}
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
              placeholder="e.g. 50"
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

      {activityType === 'Pesticide' && (
        <>
          {renderDateField()}
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
              placeholder="e.g. Chlorantraniliprole"
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
              placeholder="e.g. Stem Borer"
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
              placeholder="e.g. 100"
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

      {activityType === 'Other' && (
        <>
          {renderDateField()}
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
              className={`form-input ${isFieldInvalid('notes') ? 'input-error' : ''}`}
              rows={3} 
              value={formData.notes || ''} 
              onChange={handleChange}
              onBlur={() => handleBlur('notes')}
              placeholder="Add optional activity notes..."
            ></textarea>
            {renderFieldError('notes')}
          </div>
        </>
      )}

      <button type="submit" className="btn-submit" disabled={isSubmitting}>
        {isSubmitting ? 'Saving Activity...' : `Save ${activityType} Activity`}
      </button>
    </form>
  );
};
