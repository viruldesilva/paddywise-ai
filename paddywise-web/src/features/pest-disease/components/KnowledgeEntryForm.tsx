import { useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { createKnowledgeEntry, updateKnowledgeEntry } from '../services/pestDiseaseApi';
import {
  KNOWLEDGE_ENTRY_RULES,
  PEST_DISEASE_CATEGORIES,
  PEST_DISEASE_CATEGORY_LABELS,
} from '../types';
import type {
  PestDiseaseCategory,
  PestDiseaseKnowledgeEntry,
  SaveKnowledgeEntryRequest,
} from '../types';

interface FormValues {
  name: string;
  category: PestDiseaseCategory;
  symptoms: string;
  favorableConditions: string;
  cropStages: string;
  managementGuidance: string;
  source: string;
}

type FormErrors = Partial<Record<keyof FormValues, string>>;

const EMPTY_VALUES: FormValues = {
  name: '',
  category: 'Pest',
  symptoms: '',
  favorableConditions: '',
  cropStages: '',
  managementGuidance: '',
  source: 'Sri Lanka Department of Agriculture',
};

function toValues(entry: PestDiseaseKnowledgeEntry): FormValues {
  return {
    name: entry.name,
    category: entry.category,
    symptoms: entry.symptoms,
    favorableConditions: entry.favorableConditions ?? '',
    cropStages: entry.cropStages ?? '',
    managementGuidance: entry.managementGuidance,
    source: entry.source,
  };
}

/** Mirrors SavePestDiseaseKnowledgeRequestDto's DataAnnotations so the admin sees the
 * same verdict the server would give, without a round trip. */
function validate(values: FormValues): FormErrors {
  const errors: FormErrors = {};

  const name = values.name.trim();
  if (name.length === 0) {
    errors.name = 'Name is required.';
  } else if (name.length > KNOWLEDGE_ENTRY_RULES.nameMaxLength) {
    errors.name = `Name cannot exceed ${KNOWLEDGE_ENTRY_RULES.nameMaxLength} characters.`;
  }

  const symptoms = values.symptoms.trim();
  if (symptoms.length === 0) {
    errors.symptoms = 'Symptoms are required.';
  } else if (symptoms.length > KNOWLEDGE_ENTRY_RULES.symptomsMaxLength) {
    errors.symptoms = `Symptoms cannot exceed ${KNOWLEDGE_ENTRY_RULES.symptomsMaxLength} characters.`;
  }

  if (values.favorableConditions.trim().length > KNOWLEDGE_ENTRY_RULES.favorableConditionsMaxLength) {
    errors.favorableConditions = `Favorable conditions cannot exceed ${KNOWLEDGE_ENTRY_RULES.favorableConditionsMaxLength} characters.`;
  }

  if (values.cropStages.trim().length > KNOWLEDGE_ENTRY_RULES.cropStagesMaxLength) {
    errors.cropStages = `Crop stages cannot exceed ${KNOWLEDGE_ENTRY_RULES.cropStagesMaxLength} characters.`;
  }

  const managementGuidance = values.managementGuidance.trim();
  if (managementGuidance.length === 0) {
    errors.managementGuidance = 'Management guidance is required.';
  } else if (managementGuidance.length > KNOWLEDGE_ENTRY_RULES.managementGuidanceMaxLength) {
    errors.managementGuidance = `Management guidance cannot exceed ${KNOWLEDGE_ENTRY_RULES.managementGuidanceMaxLength} characters.`;
  }

  const source = values.source.trim();
  if (source.length === 0) {
    errors.source = 'Source is required.';
  } else if (source.length > KNOWLEDGE_ENTRY_RULES.sourceMaxLength) {
    errors.source = `Source cannot exceed ${KNOWLEDGE_ENTRY_RULES.sourceMaxLength} characters.`;
  }

  return errors;
}

interface KnowledgeEntryFormProps {
  /** Omit to create a new entry; pass one to replace it in place. */
  entry?: PestDiseaseKnowledgeEntry;
  onSaved: (entry: PestDiseaseKnowledgeEntry) => void;
  onCancel: () => void;
}

export function KnowledgeEntryForm({ entry, onSaved, onCancel }: KnowledgeEntryFormProps) {
  const isEditing = entry !== undefined;

  const [values, setValues] = useState<FormValues>(
    entry === undefined ? EMPTY_VALUES : toValues(entry)
  );
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const setValue = <K extends keyof FormValues>(key: K, value: FormValues[K]) => {
    setValues((previous) => ({ ...previous, [key]: value }));
    setErrors((previous) => ({ ...previous, [key]: undefined }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    const nextErrors = validate(values);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    const request: SaveKnowledgeEntryRequest = {
      name: values.name.trim(),
      category: values.category,
      symptoms: values.symptoms.trim(),
      favorableConditions:
        values.favorableConditions.trim().length > 0 ? values.favorableConditions.trim() : null,
      cropStages: values.cropStages.trim().length > 0 ? values.cropStages.trim() : null,
      managementGuidance: values.managementGuidance.trim(),
      source: values.source.trim(),
    };

    setIsSubmitting(true);
    try {
      // PUT is a full replace, so the same request body serves both modes.
      const saved =
        entry === undefined
          ? await createKnowledgeEntry(request)
          : await updateKnowledgeEntry(entry.id, request);
      onSaved(saved);
    } catch (err: unknown) {
      setSubmitError(
        extractApiErrorMessage(err, 'Could not save this entry. Please try again.')
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

      <div className="pd-form-group">
        <label htmlFor="pd-kb-name">Name</label>
        <input
          id="pd-kb-name"
          type="text"
          className="pd-input"
          value={values.name}
          maxLength={KNOWLEDGE_ENTRY_RULES.nameMaxLength}
          placeholder="Rice Blast"
          onChange={(event) => setValue('name', event.target.value)}
          disabled={isSubmitting}
        />
        {errors.name && <span className="pd-field-error">{errors.name}</span>}
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-kb-category">Category</label>
        <select
          id="pd-kb-category"
          className="pd-input"
          value={values.category}
          onChange={(event) => setValue('category', event.target.value as PestDiseaseCategory)}
          disabled={isSubmitting}
        >
          {PEST_DISEASE_CATEGORIES.map((category) => (
            <option key={category} value={category}>
              {PEST_DISEASE_CATEGORY_LABELS[category]}
            </option>
          ))}
        </select>
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-kb-symptoms">Symptoms</label>
        <textarea
          id="pd-kb-symptoms"
          className="pd-input pd-textarea"
          value={values.symptoms}
          maxLength={KNOWLEDGE_ENTRY_RULES.symptomsMaxLength}
          placeholder="What a farmer or officer would actually see on the plant…"
          onChange={(event) => setValue('symptoms', event.target.value)}
          disabled={isSubmitting}
        />
        <span className="pd-counter">
          {values.symptoms.length}/{KNOWLEDGE_ENTRY_RULES.symptomsMaxLength}
        </span>
        {errors.symptoms && <span className="pd-field-error">{errors.symptoms}</span>}
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-kb-conditions">Favorable conditions (optional)</label>
        <textarea
          id="pd-kb-conditions"
          className="pd-input pd-textarea"
          value={values.favorableConditions}
          maxLength={KNOWLEDGE_ENTRY_RULES.favorableConditionsMaxLength}
          placeholder="High humidity, excess nitrogen, dense planting…"
          onChange={(event) => setValue('favorableConditions', event.target.value)}
          disabled={isSubmitting}
        />
        {errors.favorableConditions && (
          <span className="pd-field-error">{errors.favorableConditions}</span>
        )}
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-kb-stages">Crop stages affected (optional)</label>
        <input
          id="pd-kb-stages"
          type="text"
          className="pd-input"
          value={values.cropStages}
          maxLength={KNOWLEDGE_ENTRY_RULES.cropStagesMaxLength}
          placeholder="Tillering, PanicleInitiation"
          onChange={(event) => setValue('cropStages', event.target.value)}
          disabled={isSubmitting}
        />
        {errors.cropStages && <span className="pd-field-error">{errors.cropStages}</span>}
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-kb-guidance">Management guidance</label>
        <textarea
          id="pd-kb-guidance"
          className="pd-input pd-textarea"
          value={values.managementGuidance}
          maxLength={KNOWLEDGE_ENTRY_RULES.managementGuidanceMaxLength}
          placeholder="What an officer would actually recommend…"
          onChange={(event) => setValue('managementGuidance', event.target.value)}
          disabled={isSubmitting}
        />
        <span className="pd-counter">
          {values.managementGuidance.length}/{KNOWLEDGE_ENTRY_RULES.managementGuidanceMaxLength}
        </span>
        {errors.managementGuidance && (
          <span className="pd-field-error">{errors.managementGuidance}</span>
        )}
      </div>

      <div className="pd-form-group">
        <label htmlFor="pd-kb-source">Source</label>
        <input
          id="pd-kb-source"
          type="text"
          className="pd-input"
          value={values.source}
          maxLength={KNOWLEDGE_ENTRY_RULES.sourceMaxLength}
          placeholder="Sri Lanka Department of Agriculture"
          onChange={(event) => setValue('source', event.target.value)}
          disabled={isSubmitting}
        />
        {errors.source && <span className="pd-field-error">{errors.source}</span>}
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
          {isSubmitting ? 'Saving…' : isEditing ? 'Save changes' : 'Add entry'}
        </button>
      </div>
    </form>
  );
}
