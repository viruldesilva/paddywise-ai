import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle, Crosshair, Loader2 } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { createField, getDivisions, updateField } from '../services/fieldApi';
import { FIELD_RULES } from '../types';
import type { CreateFieldRequest, Division, Field } from '../types';

/** Every control is a text/select input, so the draft is all strings. */
interface FieldFormValues {
  name: string;
  area: string;
  soilType: string;
  irrigationType: string;
  divisionId: string;
  latitude: string;
  longitude: string;
}

type FieldFormErrors = Partial<Record<keyof FieldFormValues, string>>;

const EMPTY_VALUES: FieldFormValues = {
  name: '',
  area: '',
  soilType: '',
  irrigationType: '',
  divisionId: '',
  latitude: '',
  longitude: '',
};

/** The form is in edit mode when it is handed a field, create mode otherwise. */
function toValues(field: Field): FieldFormValues {
  return {
    name: field.name,
    area: String(field.area),
    soilType: field.soilType,
    irrigationType: field.irrigationType,
    divisionId: String(field.divisionId),
    latitude: field.latitude === null ? '' : String(field.latitude),
    longitude: field.longitude === null ? '' : String(field.longitude),
  };
}

interface FieldFormProps {
  /** Omit to register a new field; pass one to edit it in place. */
  field?: Field;
  onSaved: (field: Field) => void;
  onCancel: () => void;
}

/**
 * Mirrors the DataAnnotations on CreateFieldRequestDto so the farmer sees the
 * same verdict the server would give, without a round trip.
 */
function validate(values: FieldFormValues): FieldFormErrors {
  const errors: FieldFormErrors = {};

  const name = values.name.trim();
  if (name.length === 0) {
    errors.name = 'Field name is required.';
  } else if (name.length > FIELD_RULES.nameMaxLength) {
    errors.name = `Field name cannot exceed ${FIELD_RULES.nameMaxLength} characters.`;
  }

  const area = Number(values.area);
  if (values.area.trim().length === 0) {
    errors.area = 'Area is required.';
  } else if (!Number.isFinite(area) || area < FIELD_RULES.areaMin || area > FIELD_RULES.areaMax) {
    errors.area = `Area must be between ${FIELD_RULES.areaMin} and ${FIELD_RULES.areaMax} acres.`;
  }

  if (values.soilType.trim().length > FIELD_RULES.soilTypeMaxLength) {
    errors.soilType = `Soil type cannot exceed ${FIELD_RULES.soilTypeMaxLength} characters.`;
  }

  if (values.irrigationType.trim().length > FIELD_RULES.irrigationTypeMaxLength) {
    errors.irrigationType = `Irrigation type cannot exceed ${FIELD_RULES.irrigationTypeMaxLength} characters.`;
  }

  const divisionId = Number(values.divisionId);
  if (values.divisionId.trim().length === 0 || !Number.isInteger(divisionId) || divisionId < 1) {
    errors.divisionId = 'Division is required.';
  }

  // Latitude and longitude are optional, but must be in range when given.
  if (values.latitude.trim().length > 0) {
    const latitude = Number(values.latitude);
    if (
      !Number.isFinite(latitude) ||
      latitude < FIELD_RULES.latitudeMin ||
      latitude > FIELD_RULES.latitudeMax
    ) {
      errors.latitude = `Latitude must be between ${FIELD_RULES.latitudeMin} and ${FIELD_RULES.latitudeMax}.`;
    }
  }

  if (values.longitude.trim().length > 0) {
    const longitude = Number(values.longitude);
    if (
      !Number.isFinite(longitude) ||
      longitude < FIELD_RULES.longitudeMin ||
      longitude > FIELD_RULES.longitudeMax
    ) {
      errors.longitude = `Longitude must be between ${FIELD_RULES.longitudeMin} and ${FIELD_RULES.longitudeMax}.`;
    }
  }

  return errors;
}

export function FieldForm({ field, onSaved, onCancel }: FieldFormProps) {
  const isEditing = field !== undefined;

  const [values, setValues] = useState<FieldFormValues>(
    field === undefined ? EMPTY_VALUES : toValues(field)
  );
  const [errors, setErrors] = useState<FieldFormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [divisions, setDivisions] = useState<Division[]>([]);
  const [isLoadingDivisions, setIsLoadingDivisions] = useState(true);
  const [divisionsError, setDivisionsError] = useState<string | null>(null);

  const [isLocating, setIsLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    getDivisions()
      .then((result) => {
        if (!isMounted) return;
        setDivisions(result);
        setDivisionsError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setDivisionsError(
          extractApiErrorMessage(err, 'Could not load agrarian divisions. Please try again.')
        );
      })
      .finally(() => {
        if (isMounted) setIsLoadingDivisions(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const setValue = <K extends keyof FieldFormValues>(key: K, value: FieldFormValues[K]) => {
    setValues((previous) => ({ ...previous, [key]: value }));
    setErrors((previous) => ({ ...previous, [key]: undefined }));
  };

  const handleUseMyLocation = () => {
    setLocationError(null);

    if (!('geolocation' in navigator)) {
      setLocationError('This browser cannot report a location.');
      return;
    }

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setIsLocating(false);
        setValues((previous) => ({
          ...previous,
          latitude: position.coords.latitude.toFixed(6),
          longitude: position.coords.longitude.toFixed(6),
        }));
        setErrors((previous) => ({ ...previous, latitude: undefined, longitude: undefined }));
      },
      (error: GeolocationPositionError) => {
        setIsLocating(false);
        setLocationError(
          error.code === error.PERMISSION_DENIED
            ? 'Location permission was denied. Enter the coordinates by hand.'
            : 'Could not read your location. Enter the coordinates by hand.'
        );
      },
      { enableHighAccuracy: true, timeout: 15000 }
    );
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    const nextErrors = validate(values);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    const request: CreateFieldRequest = {
      name: values.name.trim(),
      area: Number(values.area),
      soilType: values.soilType.trim(),
      irrigationType: values.irrigationType.trim(),
      divisionId: Number(values.divisionId),
      latitude: values.latitude.trim().length > 0 ? Number(values.latitude) : null,
      longitude: values.longitude.trim().length > 0 ? Number(values.longitude) : null,
    };

    setIsSubmitting(true);
    try {
      // PUT is a full replace, so the same request body serves both modes.
      const saved = field === undefined
        ? await createField(request)
        : await updateField(field.id, request);
      onSaved(saved);
    } catch (err: unknown) {
      setSubmitError(extractApiErrorMessage(err, 'Could not save this field. Please try again.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form className="fc-form" onSubmit={handleSubmit} noValidate>
      {submitError && (
        <p className="fc-alert" role="alert">
          <AlertCircle size={18} />
          <span>{submitError}</span>
        </p>
      )}

      <div className="fc-form-row">
        <div className="fc-form-group">
          <label htmlFor="field-name">Field name</label>
          <input
            id="field-name"
            type="text"
            className="fc-input"
            value={values.name}
            maxLength={FIELD_RULES.nameMaxLength}
            placeholder="Lower paddy, north bund"
            onChange={(event) => setValue('name', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.name && <span className="fc-field-error">{errors.name}</span>}
        </div>

        <div className="fc-form-group">
          <label htmlFor="field-area">Area (acres)</label>
          <input
            id="field-area"
            type="number"
            className="fc-input"
            value={values.area}
            step="0.01"
            min={FIELD_RULES.areaMin}
            max={FIELD_RULES.areaMax}
            placeholder="2.5"
            onChange={(event) => setValue('area', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.area && <span className="fc-field-error">{errors.area}</span>}
        </div>
      </div>

      <div className="fc-form-group">
        <label htmlFor="field-division">Agrarian division</label>
        <select
          id="field-division"
          className="fc-input"
          value={values.divisionId}
          onChange={(event) => setValue('divisionId', event.target.value)}
          disabled={isSubmitting || isLoadingDivisions || divisions.length === 0}
        >
          <option value="">
            {isLoadingDivisions ? 'Loading divisions…' : 'Select a division'}
          </option>
          {divisions.map((division) => (
            <option key={division.id} value={division.id}>
              {division.name} — {division.district}, {division.province}
            </option>
          ))}
        </select>
        {divisionsError && <span className="fc-field-error">{divisionsError}</span>}
        {errors.divisionId && <span className="fc-field-error">{errors.divisionId}</span>}
      </div>

      <div className="fc-form-row">
        <div className="fc-form-group">
          <label htmlFor="field-soil">Soil type</label>
          <input
            id="field-soil"
            type="text"
            className="fc-input"
            value={values.soilType}
            maxLength={FIELD_RULES.soilTypeMaxLength}
            placeholder="Low humic gley"
            onChange={(event) => setValue('soilType', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.soilType && <span className="fc-field-error">{errors.soilType}</span>}
        </div>

        <div className="fc-form-group">
          <label htmlFor="field-irrigation">Irrigation type</label>
          <input
            id="field-irrigation"
            type="text"
            className="fc-input"
            value={values.irrigationType}
            maxLength={FIELD_RULES.irrigationTypeMaxLength}
            placeholder="Major tank"
            onChange={(event) => setValue('irrigationType', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.irrigationType && (
            <span className="fc-field-error">{errors.irrigationType}</span>
          )}
        </div>
      </div>

      <div className="fc-form-row">
        <div className="fc-form-group">
          <label htmlFor="field-latitude">Latitude (optional)</label>
          <input
            id="field-latitude"
            type="number"
            className="fc-input"
            value={values.latitude}
            step="any"
            min={FIELD_RULES.latitudeMin}
            max={FIELD_RULES.latitudeMax}
            placeholder="7.873054"
            onChange={(event) => setValue('latitude', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.latitude && <span className="fc-field-error">{errors.latitude}</span>}
        </div>

        <div className="fc-form-group">
          <label htmlFor="field-longitude">Longitude (optional)</label>
          <input
            id="field-longitude"
            type="number"
            className="fc-input"
            value={values.longitude}
            step="any"
            min={FIELD_RULES.longitudeMin}
            max={FIELD_RULES.longitudeMax}
            placeholder="80.771797"
            onChange={(event) => setValue('longitude', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.longitude && <span className="fc-field-error">{errors.longitude}</span>}
        </div>
      </div>

      <div className="fc-locate">
        <button
          type="button"
          className="fc-btn fc-btn-ghost"
          onClick={handleUseMyLocation}
          disabled={isSubmitting || isLocating}
        >
          {isLocating ? <Loader2 size={16} className="fc-spin" /> : <Crosshair size={16} />}
          {isLocating ? 'Finding you…' : 'Use my location'}
        </button>
        {locationError && <span className="fc-field-error">{locationError}</span>}
      </div>

      <div className="fc-form-actions">
        <button
          type="button"
          className="fc-btn fc-btn-quiet"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </button>
        <button type="submit" className="fc-btn fc-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : isEditing ? 'Save changes' : 'Save field'}
        </button>
      </div>
    </form>
  );
}
