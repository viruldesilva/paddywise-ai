import { useState } from 'react';
import { Activity, CheckCircle2, ChevronDown, ChevronRight, XCircle } from 'lucide-react';
import { agentLabel } from '../types';
import type { AgentRunSummary } from '../types';
import { formatDateTime } from '../utils/dates';

/** "1.8 s" once a run is past a second — millisecond counts stop reading as time. */
function formatDuration(durationMs: number): string {
  if (!Number.isFinite(durationMs) || durationMs < 0) return '—';
  return durationMs >= 1000 ? `${(durationMs / 1000).toFixed(1)} s` : `${durationMs} ms`;
}

/**
 * The tool call's args and result are stored as JSON strings. Pretty-print what
 * parses and show the rest verbatim: a value the agent actually sent is worth
 * more on screen than a "could not display" message.
 */
function formatJson(value: string): string {
  const trimmed = value.trim();
  if (trimmed.length === 0) return '—';

  try {
    return JSON.stringify(JSON.parse(trimmed), null, 2);
  } catch {
    return trimmed;
  }
}

interface AgentActivityProps {
  /** AgentRunSummaryDto[] as the API ordered it: oldest run first. */
  runs: AgentRunSummary[];
}

/**
 * The audit trail behind a plan — every agent run and every tool call it made.
 * Nothing here is inferred: it renders the AgentRunLog rows the API returned.
 */
export function AgentActivity({ runs }: AgentActivityProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [expandedRuns, setExpandedRuns] = useState<ReadonlySet<number>>(new Set());

  const toggleRun = (index: number) => {
    setExpandedRuns((previous) => {
      const next = new Set(previous);
      if (next.has(index)) next.delete(index);
      else next.add(index);
      return next;
    });
  };

  const toolCallCount = runs.reduce((total, run) => total + run.toolCalls.length, 0);

  return (
    <section className="fc-activity">
      <button
        type="button"
        className="fc-activity-toggle"
        aria-expanded={isOpen}
        onClick={() => setIsOpen((previous) => !previous)}
      >
        {isOpen ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
        <Activity size={18} />
        <span className="fc-activity-title">Agent activity</span>
        <span className="fc-activity-count">
          {runs.length} {runs.length === 1 ? 'run' : 'runs'} · {toolCallCount}{' '}
          {toolCallCount === 1 ? 'tool call' : 'tool calls'}
        </span>
      </button>

      {isOpen && (
        <div className="fc-activity-body">
          {runs.length === 0 ? (
            <p className="fc-state-text">No agent runs were recorded for this plan.</p>
          ) : (
            <ol className="fc-run-list">
              {runs.map((run, index) => {
                const isExpanded = expandedRuns.has(index);

                return (
                  <li className="fc-run" key={`${run.agentName}-${run.createdAt}-${index}`}>
                    <button
                      type="button"
                      className="fc-run-head"
                      aria-expanded={isExpanded}
                      onClick={() => toggleRun(index)}
                    >
                      {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}

                      <span className="fc-run-agent">{agentLabel(run.agentName)}</span>

                      <span
                        className={
                          run.success ? 'fc-run-status fc-run-status-ok' : 'fc-run-status fc-run-status-bad'
                        }
                      >
                        {run.success ? <CheckCircle2 size={14} /> : <XCircle size={14} />}
                        {run.success ? 'Succeeded' : 'Failed'}
                      </span>

                      <span className="fc-run-meta">{formatDuration(run.durationMs)}</span>
                      <span className="fc-run-meta">
                        {run.toolCalls.length}{' '}
                        {run.toolCalls.length === 1 ? 'tool call' : 'tool calls'}
                      </span>
                    </button>

                    {isExpanded && (
                      <div className="fc-run-body">
                        <p className="fc-run-when">Ran {formatDateTime(run.createdAt)}</p>

                        {run.error && (
                          <p className="fc-alert" role="alert">
                            <XCircle size={16} />
                            <span>{run.error}</span>
                          </p>
                        )}

                        {run.toolCalls.length === 0 ? (
                          <p className="fc-state-text">This run made no tool calls.</p>
                        ) : (
                          <ol className="fc-tool-list">
                            {run.toolCalls.map((call, callIndex) => (
                              <li className="fc-tool" key={`${call.tool}-${call.at}-${callIndex}`}>
                                <div className="fc-tool-head">
                                  <span className="fc-tool-name">{call.tool}</span>
                                  <span className="fc-tool-when">{formatDateTime(call.at)}</span>
                                </div>

                                <div className="fc-tool-pane">
                                  <span className="fc-tool-label">Arguments</span>
                                  <pre className="fc-json">{formatJson(call.argsJson)}</pre>
                                </div>

                                <div className="fc-tool-pane">
                                  <span className="fc-tool-label">Result</span>
                                  <pre className="fc-json">{formatJson(call.resultJson)}</pre>
                                </div>
                              </li>
                            ))}
                          </ol>
                        )}
                      </div>
                    )}
                  </li>
                );
              })}
            </ol>
          )}
        </div>
      )}
    </section>
  );
}
