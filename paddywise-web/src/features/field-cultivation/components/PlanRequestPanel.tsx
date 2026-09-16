import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle, Loader2, Sparkles } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { requestPlan } from '../services/fieldApi';
import { PLAN_RULES } from '../types';
import type { CultivationPlan } from '../types';

/** Two objectives a farmer can start from instead of facing an empty box. */
const EXAMPLE_OBJECTIVES: readonly string[] = [
  'Get a good yield this season without spending more on fertiliser than last time.',
  'Keep water use low — the tank fills late this year and I want to plan around it.',
];

/**
 * What the agent is working through while the single POST is in flight.
 *
 * This is a description, not progress: nothing here is reported by the server.
 * The list advances on a timer and then holds on the last entry until the real
 * request settles, so it can never claim the plan is ready before it is.
 */
const PROGRESS_MESSAGES: readonly string[] = [
  'Reading your field…',
  'Aligning with the growth-stage timeline…',
  'Checking the plan…',
];

const PROGRESS_STEP_MS = 9_000;

interface PlanRequestPanelProps {
  cycleId: number;
  /** The newest plan for this cycle, or null when none has been asked for yet. */
  latestPlan: CultivationPlan | null;
  onPlanCreated: (plan: CultivationPlan) => void;
}

/**
 * The farmer's side of the Cultivation Planning Agent: an objective in their own
 * words, one request, and an honest wait. POST /api/cycles/{id}/plans is Farmer
 * only, so the page renders this panel only for a farmer.
 */
export function PlanRequestPanel({ cycleId, latestPlan, onPlanCreated }: PlanRequestPanelProps) {
  const [objective, setObjective] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [progressIndex, setProgressIndex] = useState(0);

  // The request outlives a fast navigation away from the page, so its result
  // must not be pushed into an unmounted component.
  const isMountedRef = useRef(true);
  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  // The index is reset by the submit handler, not here: an effect that sets state
  // as it runs costs a cascading render for something the click already knows.
  useEffect(() => {
    if (!isSubmitting) return;

    const timer = window.setInterval(() => {
      // Clamped, not wrapped: falling back to "Reading your field…" after half a
      // minute would read as a restart that never happened.
      setProgressIndex((previous) => Math.min(previous + 1, PROGRESS_MESSAGES.length - 1));
    }, PROGRESS_STEP_MS);

    return () => window.clearInterval(timer);
  }, [isSubmitting]);

  // The backend refuses a second plan while one is with an officer or signed off.
  const blockedReason =
    latestPlan === null
      ? null
      : latestPlan.status === 'PendingOfficerApproval'
        ? 'This cycle already has a plan with an agricultural officer. You can ask for another once they approve it, reject it or send it back.'
        : latestPlan.status === 'Approved'
          ? 'This cycle already has an approved plan. Talk to your agricultural officer before replacing it.'
          : null;

  const trimmed = objective.trim();
  const isBlocked = blockedReason !== null;
  const canSubmit = trimmed.length > 0 && !isSubmitting && !isBlocked;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!canSubmit) return;

    setError(null);
    setProgressIndex(0);
    setIsSubmitting(true);

    try {
      const plan = await requestPlan(cycleId, trimmed);
      if (!isMountedRef.current) return;
      setObjective('');
      onPlanCreated(plan);
    } catch (err: unknown) {
      if (!isMountedRef.current) return;
      setError(
        extractApiErrorMessage(err, 'Could not reach the planning assistant. Please try again.')
      );
    } finally {
      if (isMountedRef.current) setIsSubmitting(false);
    }
  };

  return (
    <section className="fc-section">
      <div className="fc-section-head">
        <div>
          <h2 className="fc-section-title">Ask for a cultivation plan</h2>
          <p className="fc-section-sub">
            The Cultivation Planning Agent reads this cycle and drafts a stage-by-stage plan for an
            agricultural officer to approve.
          </p>
        </div>
      </div>

      <form className="fc-plan-request" onSubmit={handleSubmit} noValidate>
        {error && (
          <p className="fc-alert" role="alert">
            <AlertCircle size={18} />
            <span>{error}</span>
          </p>
        )}

        <div className="fc-form-group">
          <label htmlFor="plan-objective">What do you want to achieve this season?</label>
          <textarea
            id="plan-objective"
            className="fc-input fc-textarea fc-objective"
            value={objective}
            rows={4}
            maxLength={PLAN_RULES.objectiveMaxLength}
            placeholder="Say it the way you would to an officer standing in the field."
            disabled={isSubmitting || isBlocked}
            onChange={(event) => setObjective(event.target.value)}
          />
          <span className="fc-counter">
            {objective.length}/{PLAN_RULES.objectiveMaxLength}
          </span>
        </div>

        <div className="fc-chips">
          <span className="fc-chips-label">For example</span>
          {EXAMPLE_OBJECTIVES.map((example) => (
            <button
              type="button"
              key={example}
              className="fc-chip"
              disabled={isSubmitting || isBlocked}
              onClick={() => setObjective(example)}
            >
              {example}
            </button>
          ))}
        </div>

        {isBlocked && <p className="fc-note">{blockedReason}</p>}

        <div className="fc-form-actions">
          <button type="submit" className="fc-btn fc-btn-primary" disabled={!canSubmit}>
            {isSubmitting ? (
              <>
                <Loader2 size={18} className="fc-spin" />
                Generating…
              </>
            ) : (
              <>
                <Sparkles size={18} />
                Generate plan
              </>
            )}
          </button>
        </div>

        {isSubmitting && (
          <div className="fc-progress" role="status" aria-live="polite">
            <ol className="fc-progress-list">
              {PROGRESS_MESSAGES.map((message, index) => (
                <li
                  key={message}
                  className={
                    index === progressIndex
                      ? 'fc-progress-item fc-progress-item-active'
                      : 'fc-progress-item'
                  }
                >
                  {index === progressIndex ? (
                    <Loader2 size={15} className="fc-spin" aria-hidden="true" />
                  ) : (
                    <span className="fc-progress-dot" aria-hidden="true" />
                  )}
                  <span>{message}</span>
                </li>
              ))}
            </ol>
            <p className="fc-progress-note">
              This normally takes 20–40 seconds. Keep the page open — the plan appears here as soon
              as the agent answers.
            </p>
          </div>
        )}
      </form>
    </section>
  );
}
