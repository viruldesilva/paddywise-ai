import { useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle, Loader2, Send, Sparkles } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { getReportDraftRevisionComment } from '../../reporting-approval/services/reviewsApi';
import { reviewPestDiseaseReport } from '../services/pestDiseaseApi';
import {
  REPORT_REVIEW_DECISIONS,
  REPORT_REVIEW_DECISION_LABELS,
  REPORT_REVIEW_RULES,
  reportReviewNeedsComment,
} from '../types';
import type { PestDiseaseReport, ReportReviewDecision } from '../types';

/** What the officer is telling the farmer with each decision. */
const DECISION_HINT: Record<ReportReviewDecision, string> = {
  Approve: 'The diagnosis stands as written and reaches the farmer. No comment needed.',
  RequestRevision:
    'The farmer may ask for a fresh analysis. Say what needs another look — the comment is all they get.',
  Reject: 'The diagnosis is discarded. Say why, so the farmer understands.',
};

interface ReportReviewFormProps {
  reportId: number;
  onReviewed: (report: PestDiseaseReport, decision: ReportReviewDecision) => void;
  onCancel: () => void;
}

/**
 * An officer's verdict on one diagnosis. The comment rule is the server's —
 * PestDiseaseReportService refuses a reject or a revision request without one
 * — so it is enforced here too rather than waiting for the 400.
 */
export function ReportReviewForm({ reportId, onReviewed, onCancel }: ReportReviewFormProps) {
  const [decision, setDecision] = useState<ReportReviewDecision>('Approve');
  const [comment, setComment] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [commentError, setCommentError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDrafting, setIsDrafting] = useState(false);

  const trimmed = comment.trim();
  const needsComment = reportReviewNeedsComment(decision);

  const handleDecisionChange = (next: ReportReviewDecision) => {
    setDecision(next);
    setCommentError(null);
  };

  const handleDraftWithAi = async () => {
    if (isDrafting || isSubmitting) return;
    setIsDrafting(true);
    setError(null);
    try {
      const draft = await getReportDraftRevisionComment(reportId);
      setComment(draft);
      setCommentError(null);
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, 'Could not draft revision comment. Please try again.'));
    } finally {
      setIsDrafting(false);
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (isSubmitting) return;

    if (needsComment && trimmed.length === 0) {
      setCommentError(
        decision === 'Reject'
          ? 'A comment is required when rejecting a diagnosis.'
          : 'A comment is required when asking for a revision.'
      );
      return;
    }

    setError(null);
    setCommentError(null);
    setIsSubmitting(true);

    try {
      const reviewed = await reviewPestDiseaseReport(reportId, {
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
    <form className="pd-review-form" onSubmit={handleSubmit} noValidate>
      <h3 className="pd-review-legend" style={{ fontSize: '1rem' }}>
        Your decision
      </h3>

      {error && (
        <p className="pd-alert" role="alert">
          <AlertCircle size={18} />
          <span>{error}</span>
        </p>
      )}

      <fieldset className="pd-review-choices" disabled={isSubmitting}>
        <legend className="pd-review-legend">What happens to this diagnosis</legend>

        {REPORT_REVIEW_DECISIONS.map((option) => (
          <label
            className={
              option === decision ? 'pd-review-choice pd-review-choice-on' : 'pd-review-choice'
            }
            key={option}
          >
            <input
              type="radio"
              name="report-review-decision"
              value={option}
              checked={option === decision}
              onChange={() => handleDecisionChange(option)}
            />
            <span style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem' }}>
              <span className="pd-review-choice-name">
                {REPORT_REVIEW_DECISION_LABELS[option]}
              </span>
              <span className="pd-state-text" style={{ maxWidth: 'none', textAlign: 'left' }}>
                {DECISION_HINT[option]}
              </span>
            </span>
          </label>
        ))}
      </fieldset>

      <div className="pd-form-group">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.4rem' }}>
          <label htmlFor="pd-review-comment" style={{ margin: 0 }}>
            Comment {needsComment ? '' : '(optional)'}
          </label>
          <button
            type="button"
            className="fc-btn fc-btn-outline"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              fontSize: '0.78rem',
              padding: '0.2rem 0.65rem',
              borderRadius: '6px',
            }}
            disabled={isSubmitting || isDrafting}
            onClick={handleDraftWithAi}
            title="Use AI to automatically draft a revision comment based on observation symptoms"
          >
            {isDrafting ? (
              <>
                <Loader2 size={13} className="fc-spin" />
                <span>Drafting with AI…</span>
              </>
            ) : (
              <>
                <Sparkles size={13} />
                <span>Draft with AI</span>
              </>
            )}
          </button>
        </div>
        <textarea
          id="pd-review-comment"
          className="pd-input pd-textarea"
          value={comment}
          rows={4}
          maxLength={REPORT_REVIEW_RULES.commentMaxLength}
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
        <span className="pd-counter">
          {comment.length}/{REPORT_REVIEW_RULES.commentMaxLength}
        </span>
        {commentError && <span className="pd-field-error">{commentError}</span>}
      </div>

      <div className="pd-form-actions">
        <button
          type="button"
          className="pd-btn pd-btn-quiet"
          disabled={isSubmitting}
          onClick={onCancel}
        >
          Cancel
        </button>
        <button type="submit" className="pd-btn pd-btn-primary" disabled={isSubmitting}>
          {isSubmitting ? (
            <>
              <Loader2 size={18} className="pd-spin" />
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
