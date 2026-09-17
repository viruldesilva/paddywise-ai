import { useMemo } from 'react';
import { AlertTriangle, MessageSquare, Share2 } from 'lucide-react';
import {
  GROWTH_STAGES,
  agentLabel,
  growthStageLabel,
  planStepCategoryLabel,
} from '../types';
import type { CultivationPlan, GrowthStage, PlanStatus, PlanStep } from '../types';
import { PLAN_STATUS_LABELS } from '../types';
import { formatDate, formatDateTime } from '../utils/dates';

/** One class per status so the colour comes from the stylesheet, not inline styles. */
const STATUS_CLASS: Record<PlanStatus, string> = {
  Draft: 'fc-badge fc-badge-draft',
  ValidationFailed: 'fc-badge fc-badge-failed',
  PendingOfficerApproval: 'fc-badge fc-badge-pending',
  Approved: 'fc-badge fc-badge-approved',
  Rejected: 'fc-badge fc-badge-rejected',
  RevisionRequested: 'fc-badge fc-badge-revision',
};

export function PlanStatusBadge({ status }: { status: PlanStatus }) {
  return (
    <span className={STATUS_CLASS[status] ?? 'fc-badge'}>
      {PLAN_STATUS_LABELS[status] ?? status}
    </span>
  );
}

interface StageGroup {
  stage: GrowthStage;
  steps: PlanStep[];
}

/**
 * The plan's steps, bucketed by stage and put back in timeline order. The model
 * is asked for chronological steps but nothing guarantees it; GrowthStage is the
 * ordering the cycle's own timeline uses, so it is the ordering here too. A stage
 * outside the enum (only reachable on a plan that failed validation) keeps its
 * place at the end rather than being dropped.
 */
function groupByStage(steps: PlanStep[]): StageGroup[] {
  const groups = new Map<GrowthStage, PlanStep[]>();

  for (const step of steps) {
    const existing = groups.get(step.stage);
    if (existing) existing.push(step);
    else groups.set(step.stage, [step]);
  }

  const known = GROWTH_STAGES.filter((stage) => groups.has(stage));
  const unknown = [...groups.keys()].filter((stage) => !GROWTH_STAGES.includes(stage));

  return [...known, ...unknown].map((stage) => ({ stage, steps: groups.get(stage) ?? [] }));
}

interface PlanViewProps {
  plan: CultivationPlan;
}

/** Everything the agent produced for one plan, and what the officer made of it. */
export function PlanView({ plan }: PlanViewProps) {
  const output = plan.plan;
  const groups = useMemo(() => groupByStage(output?.steps ?? []), [output]);

  const hasOfficerComment =
    plan.officerComment !== null &&
    (plan.status === 'Rejected' || plan.status === 'RevisionRequested');

  return (
    <article className="fc-plan">
      <header className="fc-plan-head">
        <div>
          <span className="eyebrow">CULTIVATION PLAN</span>
          <h2 className="fc-section-title">Plan #{plan.id}</h2>
          <p className="fc-section-sub">Requested {formatDateTime(plan.createdAt)}</p>
        </div>

        <PlanStatusBadge status={plan.status} />
      </header>

      <p className="fc-plan-objective">
        <span className="fc-plan-objective-label">Objective</span>
        {plan.objective}
      </p>

      {hasOfficerComment && (
        <div className="fc-officer-comment" role="note">
          <h3 className="fc-subsection-title">
            <MessageSquare size={16} />
            {plan.status === 'Rejected'
              ? 'Why the officer rejected this plan'
              : 'What the officer wants changed'}
          </h3>
          <p className="fc-officer-comment-text">{plan.officerComment}</p>
          <p className="fc-officer-comment-meta">Reviewed {formatDateTime(plan.reviewedAt)}</p>
        </div>
      )}

      {plan.status === 'ValidationFailed' && plan.validationErrors.length > 0 && (
        <div className="fc-validation" role="alert">
          <h3 className="fc-subsection-title">
            <AlertTriangle size={16} />
            The checks this plan did not pass
          </h3>
          <ul className="fc-validation-list">
            {plan.validationErrors.map((message, index) => (
              <li key={`${index}-${message}`}>{message}</li>
            ))}
          </ul>
          <p className="fc-note">
            Nothing was sent to an officer. Adjust what you asked for and generate a new plan.
          </p>
        </div>
      )}

      {output === null ? (
        <p className="fc-state-text">
          The agent did not return a plan this time, so there is nothing to show here.
        </p>
      ) : (
        <>
          {output.summary && <p className="fc-plan-summary">{output.summary}</p>}

          <div className="fc-plan-block">
            <h3 className="fc-subsection-title">Stage by stage</h3>

            {groups.length === 0 ? (
              <p className="fc-state-text">This plan has no steps.</p>
            ) : (
              <ol className="fc-plan-stages">
                {groups.map((group) => (
                  <li className="fc-plan-stage" key={group.stage}>
                    <h4 className="fc-plan-stage-name">{growthStageLabel(group.stage)}</h4>

                    <ul className="fc-plan-steps">
                      {group.steps.map((step, index) => (
                        <li className="fc-plan-step" key={`${step.task}-${index}`}>
                          <div className="fc-plan-step-head">
                            <span className="fc-plan-step-dates">
                              {formatDate(step.windowStart)} → {formatDate(step.windowEnd)}
                            </span>
                            <span className="fc-tag">{planStepCategoryLabel(step.category)}</span>
                          </div>
                          <p className="fc-plan-step-task">{step.task}</p>
                          <p className="fc-plan-step-rationale">{step.rationale}</p>
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ol>
            )}
          </div>

          {output.delegations.length > 0 && (
            <div className="fc-plan-block">
              <h3 className="fc-subsection-title">
                <Share2 size={16} />
                Delegated to other agents
              </h3>
              <ul className="fc-delegation-list">
                {output.delegations.map((delegation, index) => (
                  <li className="fc-delegation" key={`${delegation.targetAgent}-${index}`}>
                    <span className="fc-delegation-agent">{agentLabel(delegation.targetAgent)}</span>
                    <p className="fc-delegation-instruction">{delegation.instruction}</p>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {output.assumptions.length > 0 && (
            <div className="fc-plan-block">
              <h3 className="fc-subsection-title">Assumptions</h3>
              <ul className="fc-assumption-list">
                {output.assumptions.map((assumption, index) => (
                  <li key={`${index}-${assumption}`}>{assumption}</li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}
    </article>
  );
}
