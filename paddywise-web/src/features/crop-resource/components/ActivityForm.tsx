import React, { useState } from 'react';

type ActivityType = 'Irrigation' | 'Fertilizer' | 'Pesticide' | 'Other';

interface ActivityFormProps {
  activityType: ActivityType;
  onSubmit: (data: any) => void;
  isSubmitting?: boolean;
}

export const ActivityForm: React.FC<ActivityFormProps> = ({ activityType, onSubmit, isSubmitting }) => {
  const [formData, setFormData] = useState<any>({});

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ ...formData, activityType });
  };

  return (
    <form onSubmit={handleSubmit} className="activity-form">
      {activityType === 'Irrigation' && (
        <>
          <div className="form-group">
            <label>Date</label>
            <input type="date" name="date" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Water Level (cm)</label>
            <input type="number" name="waterLevel" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Duration (hours)</label>
            <input type="number" name="duration" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Source</label>
            <select name="source" className="form-control" required onChange={handleChange}>
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
            <label>Date</label>
            <input type="date" name="date" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Fertilizer Type</label>
            <select name="type" className="form-control" required onChange={handleChange}>
              <option value="">Select type</option>
              <option value="Urea">Urea</option>
              <option value="TSP">TSP</option>
              <option value="MOP">MOP</option>
              <option value="Organic">Organic Compost</option>
            </select>
          </div>
          <div className="form-group">
            <label>Quantity (kg/ha)</label>
            <input type="number" name="quantity" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Crop Stage</label>
            <select name="cropStage" className="form-control" required onChange={handleChange}>
              <option value="">Select stage</option>
              <option value="Basal">Basal</option>
              <option value="Tillering">Tillering</option>
              <option value="Panicle Initiation">Panicle Initiation</option>
              <option value="Heading">Heading</option>
            </select>
          </div>
          <div className="form-group">
            <label>Region / Zone (For Recommendation)</label>
            <select name="region" className="form-control" required onChange={handleChange}>
              <option value="">Select climatic zone</option>
              <option value="Wet">Wet</option>
              <option value="Intermediate">Intermediate</option>
              <option value="Dry">Dry</option>
            </select>
          </div>
          <div className="form-group">
            <label>Application Method</label>
            <select name="method" className="form-control" required onChange={handleChange}>
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
            <label>Date</label>
            <input type="date" name="date" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Product Name</label>
            <input type="text" name="product" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Target Pest / Disease</label>
            <input type="text" name="targetPest" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Quantity (ml/g)</label>
            <input type="number" name="quantity" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Method</label>
            <select name="method" className="form-control" required onChange={handleChange}>
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
            <label>Date</label>
            <input type="date" name="date" className="form-control" required onChange={handleChange} />
          </div>
          <div className="form-group">
            <label>Activity Type</label>
            <select name="specificActivity" className="form-control" required onChange={handleChange}>
              <option value="">Select activity</option>
              <option value="Land Preparation">Land Preparation</option>
              <option value="Seeding">Seeding</option>
              <option value="Transplanting">Transplanting</option>
              <option value="Weeding">Weeding</option>
              <option value="Harvesting">Harvesting</option>
            </select>
          </div>
          <div className="form-group">
            <label>Notes</label>
            <textarea name="notes" className="form-control" rows={3} onChange={handleChange}></textarea>
          </div>
        </>
      )}

      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? 'Processing...' : `Record ${activityType}`}
      </button>

      {activityType === 'Fertilizer' && (
        <div className="info-note" style={{ marginTop: '15px', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
          <i>ℹ️ Fertilizer recommendations are validated against Department of Agriculture guidelines for your zone.</i>
        </div>
      )}
    </form>
  );
};
