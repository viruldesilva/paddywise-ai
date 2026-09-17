import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertCircle, ClipboardList, Plus } from 'lucide-react';
import { extractApiErrorMessage } from '../../../services/authService';
import { logStage } from '../services/fieldApi';
import { GROWTH_STAGES, GROWTH_STAGE_LABELS, STAGE_LOG_RULES } from '../types';
import type { CultivationCycle, GrowthStage, LogStageRequest } from '../types';
import { daysBetween, formatDate, isValidIsoDate, todayIso } from '../utils/dates';

/** One planned stage, resolved into the geometry the bar draws. */
interface Segment {
  stage: GrowthStage;
  start: string;
  end: string;
  days: number;
  widthPercent: number;
  isExpectedToday: boolean;
}

interface TimelineGeometry {
  segments: Segment[];
  /** 0–100 along the bar, or null when today falls outside the planned season. */
  todayPercent: number | null;
  firstStart: string;
  lastEnd: string;
}

/**
 * The backend's windows are contiguous and inclusive at both ends, so a stage's
 * day count is end − start + 1 and the counts sum to the whole season.
 */
function buildGeometry(cycle: CultivationCycle): TimelineGeometry | null {
  const windows = cycle.timeline;
  if (windows.length === 0) return null;

  const firstStart = windows[0].start;
  const lastEnd = windows[windows.length - 1].end;

  const dayCounts = windows.map((window) => Math.max(daysBetween(window.start, window.end) + 1, 0));
  // An unparseable boundary makes every width NaN, so give up on the bar instead.
  if (dayCounts.some((days) => !Number.isFinite(days))) return null;

  const totalDays = dayCounts.reduce((sum, days) => sum + days, 0);
  if (totalDays <= 0) return null;

  const segments: Segment[] = windows.map((window, index) => ({
    stage: window.stage,
    start: window.start,
    end: window.end,
    days: dayCounts[index],
    widthPercent: (dayCounts[index] / totalDays) * 100,
    isExpectedToday: window.stage === cycle.expectedStageToday,
  }));

  const offset = daysBetween(firstStart, todayIso());
  const todayPercent =
    Number.isFinite(offset) && offset >= 0 && offset <= totalDays
      ? (offset / totalDays) * 100
      : null;

  return { segments, todayPercent, firstStart, lastEnd };
}

interface LogStageFormProps {
  cycleId: number;
  onLogged: (cycle: CultivationCycle) => void;
  onCancel: () => void;
}

function LogStageForm({ cycleId, onLogged, onCancel }: LogStageFormProps) {
  const [stage, setStage] = useState<GrowthStage>(GROWTH_STAGES[0]);
  const [observedOn, setObservedOn] = useState<string>(todayIso());
  const [notes, setNotes] = useState('');
  const [dateError, setDateError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    if (!isValidIsoDate(observedOn)) {
      setDateError('Enter the observation date as a calendar date.');
      return;
    }
    setDateError(null);

    const trimmed = notes.trim();
    const request: LogStageRequest = {
      stage,
      observedOn,
      notes: trimmed.length > 0 ? trimmed : null,
    };

    setIsSubmitting(true);
    try {
      const updated = await logStage(cycleId, request);
      onLogged(updated);
    } catch (err: unknown) {
      setSubmitError(extractApiErrorMessage(err, 'Could not record this stage. Please try again.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form className="fc-form fc-inline-form" onSubmit={handleSubmit} noValidate>
      {submitError && (
        <p className="fc-alert" role="alert">
          <AlertCircle size={18} />
          <span>{submitError}</span>
        </p>
      )}

      <div className="fc-form-row">
        <div className="fc-form-group">
          <label htmlFor="log-stage">Stage reached</label>
          <select
            id="log-stage"
            className="fc-input"
            value={stage}
            onChange={(event) => setStage(event.target.value as GrowthStage)}
            disabled={isSubmitting}
          >
            {GROWTH_STAGES.map((value) => (
              <option key={value} value={value}>
                {GROWTH_STAGE_LABELS[value]}
              </option>
            ))}
          </select>
        </div>

        <div className="fc-form-group">
          <label htmlFor="log-observed-on">Observed on</label>
          <input
            id="log-observed-on"
            type="date"
            className="fc-input"
            value={observedOn}
            max={todayIso()}
            onChange={(event) => {
              setObservedOn(event.target.value);
              setDateError(null);
            }}
            disabled={isSubmitting}
          />
          {dateError && <span className="fc-field-error">{dateError}</span>}
        </div>
      </div>

      <div className="fc-form-group">
        <label htmlFor="log-notes">Notes (optional)</label>
        <textarea
          id="log-notes"
          className="fc-input fc-textarea"
          value={notes}
          rows={2}
          maxLength={STAGE_LOG_RULES.notesMaxLength}
          placeholder="What the crop looked like in the field."
          onChange={(event) => setNotes(event.target.value)}
          disabled={isSubmitting}
        />
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
          {isSubmitting ? 'Saving…' : 'Save stage'}
        </button>
      </div>
    </form>
  );
}

interface StageTimelineProps {
  cycle: CultivationCycle;
  /** Farmers and officers may record an observation; Admin may not. */
  canLog: boolean;
  onLogged: (cycle: CultivationCycle) => void;
}

export function StageTimeline({ cycle, canLog, onLogged }: StageTimelineProps) {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const geometry = useMemo(() => buildGeometry(cycle), [cycle]);

  const handleLogged = (updated: CultivationCycle) => {
    setIsFormOpen(false);
    onLogged(updated);
  };

  return (
    <section className="fc-section">
      <div className="fc-section-head">
        <div>
          <h2 className="fc-section-title">Growth stages</h2>
          {geometry && (
            <p className="fc-section-sub">
              {formatDate(geometry.firstStart)} → {formatDate(geometry.lastEnd)} ·{' '}
              {cycle.durationDays} days planned
            </p>
          )}
        </div>

        {canLog && !isFormOpen && (
          <button className="fc-btn fc-btn-outline" onClick={() => setIsFormOpen(true)}>
            <Plus size={18} />
            Log stage
          </button>
        )}
      </div>

      {geometry === null ? (
        <p className="fc-state-text">No planned timeline for this cycle yet.</p>
      ) : (
        <div className="fc-timeline">
          <div
            className="fc-timeline-bar"
            role="img"
            aria-label={`Planned growth stages from ${formatDate(geometry.firstStart)} to ${formatDate(
              geometry.lastEnd
            )}. Expected stage today: ${GROWTH_STAGE_LABELS[cycle.expectedStageToday]}.`}
          >
            {geometry.segments.map((segment) => (
              <div
                key={segment.stage}
                className={
                  segment.isExpectedToday
                    ? 'fc-timeline-seg fc-timeline-seg-current'
                    : 'fc-timeline-seg'
                }
                style={{ width: `${segment.widthPercent}%` }}
                title={`${GROWTH_STAGE_LABELS[segment.stage]} · ${formatDate(
                  segment.start
                )} → ${formatDate(segment.end)} · ${segment.days} days`}
              >
                <span className="fc-timeline-seg-label">
                  {GROWTH_STAGE_LABELS[segment.stage]}
                </span>
              </div>
            ))}

            {geometry.todayPercent !== null && (
              <div
                className="fc-timeline-today"
                style={{ left: `${geometry.todayPercent}%` }}
                aria-hidden="true"
              >
                <span className="fc-timeline-today-flag">Today</span>
              </div>
            )}
          </div>

          <ol className="fc-timeline-legend">
            {geometry.segments.map((segment) => (
              <li
                key={segment.stage}
                className={segment.isExpectedToday ? 'fc-legend-item fc-legend-current' : 'fc-legend-item'}
              >
                <span className="fc-legend-stage">{GROWTH_STAGE_LABELS[segment.stage]}</span>
                <span className="fc-legend-dates">
                  {formatDate(segment.start)} → {formatDate(segment.end)}
                </span>
              </li>
            ))}
          </ol>
        </div>
      )}

      {canLog && isFormOpen && (
        <LogStageForm
          cycleId={cycle.id}
          onLogged={handleLogged}
          onCancel={() => setIsFormOpen(false)}
        />
      )}

      <div className="fc-logs">
        <h3 className="fc-subsection-title">
          <ClipboardList size={16} />
          Logged stages
        </h3>

        {cycle.stageLogs.length === 0 ? (
          <p className="fc-state-text">
            Nothing recorded yet. Log a stage as the crop reaches it so the plan stays honest.
          </p>
        ) : (
          <ul className="fc-log-list">
            {cycle.stageLogs.map((log) => (
              <li className="fc-log-item" key={log.id}>
                <div className="fc-log-head">
                  <span className="fc-log-stage">{GROWTH_STAGE_LABELS[log.stage]}</span>
                  <span className="fc-log-date">{formatDate(log.observedOn)}</span>
                </div>
                {log.notes && <p className="fc-log-notes">{log.notes}</p>}
                <p className="fc-log-by">Logged by {log.loggedByUserName || 'a team member'}</p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
