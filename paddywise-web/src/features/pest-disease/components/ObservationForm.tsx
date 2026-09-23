import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { getMyCycles } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import { createObservation, updateObservation } from '../services/pestDiseaseApi';
import {
  OBSERVATION_RULES,
  OBSERVATION_SEVERITIES,
  OBSERVATION_TYPES,
  OBSERVATION_TYPE_LABELS,
  SEVERITY_LABELS,
} from '../types';
import type { Observation, ObservationSeverity, ObservationType } from '../types';

interface FormValues {
  cultivationCycleId: string;
  observationType: ObservationType;
  symptoms: string;
  severity: ObservationSeverity;
  imageUrl: string;
}

type FormErrors = Partial<Record<keyof FormValues, string>>;

const EMPTY_VALUES: FormValues = {
  cultivationCycleId: '',
  observationType: 'Unknown',
  symptoms: '',
  severity: 'Low',
  imageUrl: '',
};

function toValues(observation: Observation): FormValues {
  return {
    cultivationCycleId: String(observation.cultivationCycleId),
    observationType: observation.observationType,
    symptoms: observation.symptoms,
    severity: observation.severity,
    imageUrl: observation.imageUrl ?? '',
  };
}

function validate(values: FormValues, isEditing: boolean): FormErrors {
  const errors: FormErrors = {};

  if (!isEditing && (values.cultivationCycleId.trim().length === 0 ||
    !Number.isInteger(Number(values.cultivationCycleId)))) {
    errors.cultivationCycleId = 'Cultivation cycle is required.';
  }

  const symptoms = values.symptoms.trim();
  if (symptoms.length === 0) {
    errors.symptoms = 'Describe what you see.';
  } else if (symptoms.length > OBSERVATION_RULES.symptomsMaxLength) {
    errors.symptoms = `Symptoms cannot exceed ${OBSERVATION_RULES.symptomsMaxLength} characters.`;
  }

  if (values.imageUrl.trim().length > 0) {
    try {
      new URL(values.imageUrl.trim());
    } catch {
      errors.imageUrl = 'Enter a valid image URL, or leave this blank.';
    }
  }

  return errors;
}

interface ObservationFormProps {
  /** Omit to submit a new observation; pass one to correct it in place. */
  observation?: Observation;
  onSaved: (observation: Observation) => void;
  onCancel: () => void;
}

export function ObservationForm({ observation, onSaved, onCancel }: ObservationFormProps) {
  const isEditing = observation !== undefined;

  const [values, setValues] = useState<FormValues>(
    observation === undefined ? EMPTY_VALUES : toValues(observation)
  );
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [cycles, setCycles] = useState<CultivationCycle[]>([]);
  const [isLoadingCycles, setIsLoadingCycles] = useState(!isEditing);
  const [cyclesError, setCyclesError] = useState<string | null>(null);

  useEffect(() => {
    if (isEditing) return;
    let isMounted = true;

    getMyCycles()
      .then((result) => {
        if (!isMounted) return;
        setCycles(result);
        setCyclesError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setCyclesError(
          extractApiErrorMessage(err, 'Could not load your cultivation cycles. Please try again.')
        );
      })
      .finally(() => {
        if (isMounted) setIsLoadingCycles(false);
      });

    return () => {
      isMounted = false;
    };
  }, [isEditing]);

  const setValue = <K extends keyof FormValues>(key: K, value: FormValues[K]) => {
    setValues((previous) => ({ ...previous, [key]: value }));
    setErrors((previous) => ({ ...previous, [key]: undefined }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    const nextErrors = validate(values, isEditing);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    const imageUrl = values.imageUrl.trim().length > 0 ? values.imageUrl.trim() : null;

    setIsSubmitting(true);
    try {
      const saved =
        observation === undefined
          ? await createObservation({
              cultivationCycleId: Number(values.cultivationCycleId),
              observationType: values.observationType,
              symptoms: values.symptoms.trim(),
              severity: values.severity,
              imageUrl,
            })
          : await updateObservation(observation.id, {
              observationType: values.observationType,
              symptoms: values.symptoms.trim(),
              severity: values.severity,
              imageUrl,
            });
      onSaved(saved);
    } catch (err: unknown) {
      setSubmitError(
        extractApiErrorMessage(err, 'Could not save this report. Please try again.')
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form className="pd-form" onSubmit={handleSubmit} noValidate>
      {submitError && (
        <p className="pd-alert" role="alert">
          <AlertCircle size={18} />
          <span>{submitError}</span>
        </p>
      )}

      {!isEditing && (
        <div className="pd-form-group">
          <label htmlFor="pd-cycle">Cultivation cycle</label>
          <select
            id="pd-cycle"
            className="pd-input"
            value={values.cultivationCycleId}
            onChange={(event) => setValue('cultivationCycleId', event.target.value)}
            disabled={isSubmitting || isLoadingCycles || cycles.length === 0}
          >
            <option value="">
              {isLoadingCycles ? 'Loading your cycles…' : 'Select the affected cycle'}
            </option>
            {cycles.map((cycle) => (
              <option key={cycle.id} value={cycle.id}>
                {cycle.fieldName} — {cycle.season} {cycle.year} ({cycle.currentStage})
              </option>
            ))}
          </select>
          {!isLoadingCycles && cycles.length === 0 && !cyclesError && (
            <span className="pd-field-error">
              You have no active cultivation cycles to report against yet.
            </span>
          )}
          {cyclesError && <span className="pd-field-error">{cyclesError}</span>}
          {errors.cultivationCycleId && (
            <span className="pd-field-error">{errors.cultivationCycleId}</span>
          )}
        </div>
      )}

      <div className="pd-form-group">
        <label htmlFor="pd-type">What are you seeing?</label>
        <select
          id="pd-type"
          className="pd-input"
          value={values.observationType}
          onChange={(event) => setValue('observationType', event.target.value as ObservationType)}
          disabled={isSubmitting}
        >
          {OBSERVATION_TYPES.map((type) => (
            <option key={type} value={type}>
              {OBSERVATION_TYPE_LABELS[type]}
            </option>
          ))}
        </select>
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-severity">Severity</label>
        <select
          id="pd-severity"
          className="pd-input"
          value={values.severity}
          onChange={(event) => setValue('severity', event.target.value as ObservationSeverity)}
          disabled={isSubmitting}
        >
          {OBSERVATION_SEVERITIES.map((severity) => (
            <option key={severity} value={severity}>
              {SEVERITY_LABELS[severity]}
            </option>
          ))}
        </select>
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-symptoms">Describe what you see</label>
        <textarea
          id="pd-symptoms"
          className="pd-input pd-textarea"
          value={values.symptoms}
          maxLength={OBSERVATION_RULES.symptomsMaxLength}
          placeholder="Yellowing leaf tips, stunted growth, curling leaves…"
          onChange={(event) => setValue('symptoms', event.target.value)}
          disabled={isSubmitting}
        />
        <span className="pd-counter">
          {values.symptoms.length}/{OBSERVATION_RULES.symptomsMaxLength}
        </span>
        {errors.symptoms && <span className="pd-field-error">{errors.symptoms}</span>}
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-image">Photo URL (optional)</label>
        <input
          id="pd-image"
          type="text"
          className="pd-input"
          value={values.imageUrl}
          placeholder="https://…"
          onChange={(event) => setValue('imageUrl', event.target.value)}
          disabled={isSubmitting}
        />
        {errors.imageUrl && <span className="pd-field-error">{errors.imageUrl}</span>}
      </div>

      <div className="pd-form-actions">
        <button
          type="button"
          className="pd-btn pd-btn-quiet"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </button>
        <button type="submit" className="pd-btn pd-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : isEditing ? 'Save changes' : 'Submit report'}
        </button>
      </div>
    </form>
  );
}
