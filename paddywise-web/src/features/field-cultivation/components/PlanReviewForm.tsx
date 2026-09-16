import { useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle, Loader2, Send } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { reviewPlan } from '../services/fieldApi';
import {
  PLAN_REVIEW_DECISIONS,
  PLAN_REVIEW_DECISION_LABELS,
  PLAN_REVIEW_RULES,
  reviewNeedsComment,
} from '../types';
import type { CultivationPlan, PlanReviewDecision } from '../types';

/** What the officer is telling the farmer with each decision. */
const DECISION_HINT: Record<PlanReviewDecision, string> = {
  Approve:
    'The plan stands as written, and a cycle still marked Planned becomes Active. No comment needed.',
  RequestRevision:
    'The farmer may ask the agent for another plan. Say what has to change — the comment is all they get.',
  Reject:
    'The plan is closed for good. Say why, so the farmer knows what not to repeat.',
};

interface PlanReviewFormProps {
  planId: number;
  /** Handed the reviewed plan the server returns, so the caller can refresh. */
  onReviewed: (plan: CultivationPlan, decision: PlanReviewDecision) => void;
  onCancel: () => void;
}

/**
 * An officer's verdict on one plan. The comment rule is the server's —
 * CultivationPlanService refuses a reject or a revision request without one —
 * so it is enforced here too rather than waiting for the 400.
 */
export function PlanReviewForm({ planId, onReviewed, onCancel }: PlanReviewFormProps) {
  const [decision, setDecision] = useState<PlanReviewDecision>('Approve');
  const [comment, setComment] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [commentError, setCommentError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const trimmed = comment.trim();
  const needsComment = reviewNeedsComment(decision);

  const handleDecisionChange = (next: PlanReviewDecision) => {
    setDecision(next);
    // The old warning describes a rule the new decision may not have.
    setCommentError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (isSubmitting) return;

    if (needsComment && trimmed.length === 0) {
      setCommentError(
        decision === 'Reject'
          ? 'A comment is required when rejecting a plan.'
          : 'A comment is required when asking for a revision.'
      );
      return;
    }

    setError(null);
    setCommentError(null);
    setIsSubmitting(true);

    try {
      // An approval carries no comment: the server stores null either way.
      const reviewed = await reviewPlan(planId, {
        decision,
        comment: trimmed.length > 0 ? trimmed : null,
      });
      onReviewed(reviewed, decision);
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, 'Could not record your review. Please try again.'));
      setIsSubmitting(false);
    }
  };

  return (
    <form className="fc-review-form" onSubmit={handleSubmit} noValidate>
      <h3 className="fc-subsection-title">Your decision</h3>

      {error && (
        <p className="fc-alert" role="alert">
          <AlertCircle size={18} />
          <span>{error}</span>
        </p>
      )}

      <fieldset className="fc-review-choices" disabled={isSubmitting}>
        <legend className="fc-review-legend">What happens to this plan</legend>

        {PLAN_REVIEW_DECISIONS.map((option) => (
          <label
            className={
              option === decision ? 'fc-review-choice fc-review-choice-on' : 'fc-review-choice'
            }
            key={option}
          >
            <input
              type="radio"
              name="plan-review-decision"
              value={option}
              checked={option === decision}
              onChange={() => handleDecisionChange(option)}
            />
            <span className="fc-review-choice-body">
              <span className="fc-review-choice-name">{PLAN_REVIEW_DECISION_LABELS[option]}</span>
              <span className="fc-review-choice-hint">{DECISION_HINT[option]}</span>
            </span>
          </label>
        ))}
      </fieldset>

      <div className="fc-form-group">
        <label htmlFor="plan-review-comment">
          Comment {needsComment ? '' : '(optional)'}
        </label>
        <textarea
          id="plan-review-comment"
          className="fc-input fc-textarea"
          value={comment}
          rows={4}
          maxLength={PLAN_REVIEW_RULES.commentMaxLength}
          disabled={isSubmitting}
          aria-invalid={commentError !== null}
          placeholder={
            needsComment
              ? 'Tell the farmer what is wrong and what to do about it.'
              : 'Anything you want on the record.'
          }
          onChange={(event) => {
            setComment(event.target.value);
            if (commentError !== null) setCommentError(null);
          }}
        />
        <span className="fc-counter">
          {comment.length}/{PLAN_REVIEW_RULES.commentMaxLength}
        </span>
        {commentError && <span className="fc-field-error">{commentError}</span>}
      </div>

      <div className="fc-form-actions">
        <button
          type="button"
          className="fc-btn fc-btn-quiet"
          disabled={isSubmitting}
          onClick={onCancel}
        >
          Cancel
        </button>
        <button type="submit" className="fc-btn fc-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <Loader2 size={18} className="fc-spin" />
              Submitting…
            </>
          ) : (
            <>
              <Send size={18} />
              Submit review
            </>
          )}
        </button>
      </div>
    </form>
  );
}
