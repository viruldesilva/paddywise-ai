import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle, CalendarCheck } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { getVarieties, startCultivation } from '../services/fieldApi';
import {
  CULTIVATION_METHODS,
  CULTIVATION_METHOD_LABELS,
  CYCLE_RULES,
  SEASONS,
} from '../types';
import type {
  CreateCycleRequest,
  CultivationCycle,
  CultivationMethod,
  Season,
  Variety,
} from '../types';
import { addDays, formatDate, isValidIsoDate, todayIso } from '../utils/dates';

/** Every control is a text/select/date input, so the draft is all strings. */
interface CycleFormValues {
  varietyId: string;
  season: string;
  year: string;
  method: string;
  sowingDate: string;
  notes: string;
}

type CycleFormErrors = Partial<Record<keyof CycleFormValues, string>>;

interface CycleFormProps {
  fieldId: number;
  onCreated: (cycle: CultivationCycle) => void;
  onCancel: () => void;
}

/**
 * Yala is the April–September season and Maha the October–March one, so the
 * month the farmer is sitting in is the better default of the two.
 */
function defaultSeason(): Season {
  const month = new Date().getUTCMonth() + 1;
  return month >= 4 && month <= 9 ? 'Yala' : 'Maha';
}

function emptyValues(): CycleFormValues {
  return {
    varietyId: '',
    season: defaultSeason(),
    year: String(new Date().getUTCFullYear()),
    method: 'Transplanting',
    sowingDate: todayIso(),
    notes: '',
  };
}

/**
 * Mirrors the DataAnnotations on CreateCycleRequestDto and the calendar rules
 * CycleService enforces, so the farmer sees the same verdict without a round trip.
 */
function validate(values: CycleFormValues): CycleFormErrors {
  const errors: CycleFormErrors = {};

  const varietyId = Number(values.varietyId);
  if (values.varietyId.trim().length === 0 || !Number.isInteger(varietyId) || varietyId < 1) {
    errors.varietyId = 'Variety is required.';
  }

  if (!SEASONS.includes(values.season as Season)) {
    errors.season = 'Season must be either Yala or Maha.';
  }

  const year = Number(values.year);
  if (
    values.year.trim().length === 0 ||
    !Number.isInteger(year) ||
    year < CYCLE_RULES.yearMin ||
    year > CYCLE_RULES.yearMax
  ) {
    errors.year = `Year must be between ${CYCLE_RULES.yearMin} and ${CYCLE_RULES.yearMax}.`;
  }

  if (!CULTIVATION_METHODS.includes(values.method as CultivationMethod)) {
    errors.method = 'Cultivation method is required.';
  }

  if (values.sowingDate.trim().length === 0) {
    errors.sowingDate = 'Sowing date is required.';
  } else if (!isValidIsoDate(values.sowingDate)) {
    errors.sowingDate = 'Enter the sowing date as a calendar date.';
  } else {
    const today = todayIso();
    if (values.sowingDate < addDays(today, -CYCLE_RULES.maxBackdateDays)) {
      errors.sowingDate = `Sowing date cannot be more than ${CYCLE_RULES.maxBackdateDays} days in the past.`;
    } else if (values.sowingDate > addDays(today, CYCLE_RULES.maxLookaheadDays)) {
      errors.sowingDate = `Sowing date cannot be more than ${CYCLE_RULES.maxLookaheadDays} days in the future.`;
    }
  }

  if (values.notes.trim().length > CYCLE_RULES.notesMaxLength) {
    errors.notes = `Notes cannot exceed ${CYCLE_RULES.notesMaxLength} characters.`;
  }

  return errors;
}

export function CycleForm({ fieldId, onCreated, onCancel }: CycleFormProps) {
  const [values, setValues] = useState<CycleFormValues>(emptyValues);
  const [errors, setErrors] = useState<CycleFormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [varieties, setVarieties] = useState<Variety[]>([]);
  const [isLoadingVarieties, setIsLoadingVarieties] = useState(true);
  const [varietiesError, setVarietiesError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    getVarieties()
      .then((result) => {
        if (!isMounted) return;
        setVarieties(result);
        setVarietiesError(null);
      })
      .catch((err: unknown) => {
        if (!isMounted) return;
        setVarietiesError(
          extractApiErrorMessage(err, 'Could not load paddy varieties. Please try again.')
        );
      })
      .finally(() => {
        if (isMounted) setIsLoadingVarieties(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const selectedVariety = useMemo(
    () => varieties.find((variety) => String(variety.id) === values.varietyId) ?? null,
    [varieties, values.varietyId]
  );

  /**
   * The same arithmetic the server does on save — sowing + the variety's
   * duration — so the farmer sees the harvest date before committing.
   */
  const expectedHarvestDate = useMemo(() => {
    if (selectedVariety === null) return null;
    if (!isValidIsoDate(values.sowingDate)) return null;

    return addDays(values.sowingDate, selectedVariety.durationDays);
  }, [selectedVariety, values.sowingDate]);

  const setValue = <K extends keyof CycleFormValues>(key: K, value: CycleFormValues[K]) => {
    setValues((previous) => ({ ...previous, [key]: value }));
    setErrors((previous) => ({ ...previous, [key]: undefined }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    const nextErrors = validate(values);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    const notes = values.notes.trim();
    const request: CreateCycleRequest = {
      varietyId: Number(values.varietyId),
      season: values.season as Season,
      year: Number(values.year),
      method: values.method as CultivationMethod,
      sowingDate: values.sowingDate,
      notes: notes.length > 0 ? notes : null,
    };

    setIsSubmitting(true);
    try {
      const created = await startCultivation(fieldId, request);
      onCreated(created);
    } catch (err: unknown) {
      setSubmitError(
        extractApiErrorMessage(err, 'Could not start this cultivation cycle. Please try again.')
      );
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

      <div className="fc-form-group">
        <label htmlFor="cycle-variety">Paddy variety</label>
        <select
          id="cycle-variety"
          className="fc-input"
          value={values.varietyId}
          onChange={(event) => setValue('varietyId', event.target.value)}
          disabled={isSubmitting || isLoadingVarieties || varieties.length === 0}
        >
          <option value="">
            {isLoadingVarieties ? 'Loading varieties…' : 'Select a variety'}
          </option>
          {varieties.map((variety) => (
            <option key={variety.id} value={variety.id}>
              {variety.name} — {variety.durationDays} days ({variety.ageGroup})
            </option>
          ))}
        </select>
        {varietiesError && <span className="fc-field-error">{varietiesError}</span>}
        {errors.varietyId && <span className="fc-field-error">{errors.varietyId}</span>}
      </div>

      <div className="fc-form-row">
        <div className="fc-form-group">
          <label htmlFor="cycle-season">Season</label>
          <select
            id="cycle-season"
            className="fc-input"
            value={values.season}
            onChange={(event) => setValue('season', event.target.value)}
            disabled={isSubmitting}
          >
            {SEASONS.map((season) => (
              <option key={season} value={season}>
                {season}
              </option>
            ))}
          </select>
          {errors.season && <span className="fc-field-error">{errors.season}</span>}
        </div>

        <div className="fc-form-group">
          <label htmlFor="cycle-year">Year</label>
          <input
            id="cycle-year"
            type="number"
            className="fc-input"
            value={values.year}
            step="1"
            min={CYCLE_RULES.yearMin}
            max={CYCLE_RULES.yearMax}
            onChange={(event) => setValue('year', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.year && <span className="fc-field-error">{errors.year}</span>}
        </div>
      </div>

      <div className="fc-form-row">
        <div className="fc-form-group">
          <label htmlFor="cycle-method">Cultivation method</label>
          <select
            id="cycle-method"
            className="fc-input"
            value={values.method}
            onChange={(event) => setValue('method', event.target.value)}
            disabled={isSubmitting}
          >
            {CULTIVATION_METHODS.map((method) => (
              <option key={method} value={method}>
                {CULTIVATION_METHOD_LABELS[method]}
              </option>
            ))}
          </select>
          {errors.method && <span className="fc-field-error">{errors.method}</span>}
        </div>

        <div className="fc-form-group">
          <label htmlFor="cycle-sowing">Sowing date</label>
          <input
            id="cycle-sowing"
            type="date"
            className="fc-input"
            value={values.sowingDate}
            min={addDays(todayIso(), -CYCLE_RULES.maxBackdateDays)}
            max={addDays(todayIso(), CYCLE_RULES.maxLookaheadDays)}
            onChange={(event) => setValue('sowingDate', event.target.value)}
            disabled={isSubmitting}
          />
          {errors.sowingDate && <span className="fc-field-error">{errors.sowingDate}</span>}
        </div>
      </div>

      <p className="fc-projection">
        <CalendarCheck size={18} />
        {expectedHarvestDate === null ? (
          <span>Pick a variety and a sowing date to see the expected harvest.</span>
        ) : (
          <span>
            Expected harvest <strong>{formatDate(expectedHarvestDate)}</strong> —{' '}
            {selectedVariety?.durationDays} days after sowing.
          </span>
        )}
      </p>

      <div className="fc-form-group">
        <label htmlFor="cycle-notes">Notes (optional)</label>
        <textarea
          id="cycle-notes"
          className="fc-input fc-textarea"
          value={values.notes}
          rows={3}
          maxLength={CYCLE_RULES.notesMaxLength}
          placeholder="Seed paddy source, land preparation, anything worth remembering."
          onChange={(event) => setValue('notes', event.target.value)}
          disabled={isSubmitting}
        />
        {errors.notes && <span className="fc-field-error">{errors.notes}</span>}
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
          {isSubmitting ? 'Starting…' : 'Start cycle'}
        </button>
      </div>
    </form>
  );
}
