import React, { useState, useEffect } from 'react';
import {
  Sparkles,
  AlertTriangle,
  CheckCircle2,
  Droplets,
  Leaf,
  ShieldAlert,
  Send,
  HelpCircle,
  BookOpen,
  PhoneCall,
  Loader2,
  Calendar,
  Layers,
  Info
} from 'lucide-react';
import {
  cropAnalysisApi,
  type CropActivityAnalysisOutput,
  type ActivityRecommendation
} from '../services/cropAnalysisApi';
import type { CultivationCycle } from '../../field-cultivation/types';
import './AiAdvisorPanel.css';

interface AiAdvisorPanelProps {
  cycles: CultivationCycle[];
  selectedCycleId: number | 'all';
  onCycleSelect?: (cycleId: number) => void;
}

export const AiAdvisorPanel: React.FC<AiAdvisorPanelProps> = ({
  cycles,
  selectedCycleId,
  onCycleSelect
}) => {
  const activeCycleId = selectedCycleId === 'all' ? cycles[0]?.id : selectedCycleId;
  const activeCycle = cycles.find(c => c.id === activeCycleId);

  const [analysis, setAnalysis] = useState<CropActivityAnalysisOutput | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Chat console states
  const [chatQuestion, setChatQuestion] = useState('');
  const [chatMessages, setChatMessages] = useState<Array<{ role: 'user' | 'assistant'; content: string }>>([]);
  const [isChatLoading, setIsChatLoading] = useState(false);

  const runAnalysis = async (cycleIdToAnalyze?: number) => {
    const targetId = cycleIdToAnalyze || activeCycleId;
    if (!targetId) return;

    try {
      setIsLoading(true);
      setError(null);
      const data = await cropAnalysisApi.runAnalysis(targetId);
      setAnalysis(data);
    } catch (err: any) {
      console.error('Failed to run AI analysis', err);
      const msg = err?.response?.data?.message || err?.message || 'Failed to complete AI activity analysis.';
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (activeCycleId) {
      runAnalysis(activeCycleId);
    }
  }, [activeCycleId]);

  const handleAskQuestion = async (questionText?: string) => {
    const q = (questionText || chatQuestion).trim();
    if (!q || !activeCycleId || isChatLoading) return;

    const newHistory = [...chatMessages, { role: 'user' as const, content: q }];
    setChatMessages(newHistory);
    setChatQuestion('');
    setIsChatLoading(true);

    try {
      const response = await cropAnalysisApi.askAiQuestion(activeCycleId, q);
      setChatMessages([...newHistory, { role: 'assistant', content: response.answer }]);
    } catch (err) {
      setChatMessages([
        ...newHistory,
        { role: 'assistant', content: 'Unable to process your question at this moment. Please check your network or try again.' }
      ]);
    } finally {
      setIsChatLoading(false);
    }
  };

  const getPriorityClass = (priority: string) => {
    switch (priority) {
      case 'HIGH': return 'priority-high';
      case 'MEDIUM': return 'priority-medium';
      default: return 'priority-low';
    }
  };

  const formatStatusBadge = (status: string | null | undefined): string => {
    if (!status) return 'Recorded';
    const clean = status.trim();
    const lower = clean.toLowerCase();
    if (lower === 'duesoon') return 'Due Soon';
    if (lower === 'notrecorded') return 'Not Recorded';
    if (lower === 'monitoringrequired') return 'Monitoring Req.';
    if (lower === 'droughtrisk') return 'Drought Risk';
    if (lower === 'highrisk') return 'High Risk';
    if (lower === 'safetyalert') return 'Safety Alert';
    return clean.replace(/([a-z])([A-Z])/g, '$1 $2');
  };

  const getStatusBadgeClass = (status: string) => {
    const s = (status || '').toLowerCase().replace(/[\s_]+/g, '');
    switch (s) {
      case 'adequate':
      case 'balanced':
      case 'safe':
      case 'optimal':
      case 'standard':
        return 'badge-good';
      case 'due':
      case 'duesoon':
      case 'monitoringrequired':
      case 'warning':
        return 'badge-warning';
      case 'flooded':
      case 'droughtrisk':
      case 'excessive':
      case 'highrisk':
      case 'safetyalert':
      case 'deficit':
        return 'badge-danger';
      default:
        return 'badge-neutral';
    }
  };

  if (cycles.length === 0) {
    return (
      <div className="advisor-card empty-state">
        <Sparkles size={36} className="advisor-sparkle-icon" />
        <h3>No Cultivation Cycles Available</h3>
        <p>Please establish an active cultivation cycle to enable AI activity analysis.</p>
      </div>
    );
  }

  return (
    <div className="ai-advisor-container">
      {/* Top Banner & Trigger */}
      <div className="advisor-hero-header">
        <div className="advisor-hero-text">
          <div className="advisor-badge">
            <Sparkles size={14} /> AGENTIC AI ADVISOR
          </div>
          <h2>Crop Activity & Resource Analysis</h2>
          <p>
            Evidence-based recommendations derived from Sri Lankan Department of Agriculture (DOA) guidelines,
            analyzing your real-time irrigation, fertilizer, and crop protection logs.
          </p>

          {activeCycle && (
            <div className="advisor-cycle-pills">
              <span className="cycle-meta-pill">
                <Layers size={13} /> {activeCycle.fieldName} ({activeCycle.season} {activeCycle.year})
              </span>
              <span className="cycle-meta-pill">
                <Calendar size={13} /> Sown: {activeCycle.sowingDate}
              </span>
              <span className="cycle-meta-pill highlight">
                {activeCycle.varietyName}
              </span>
            </div>
          )}
        </div>

        <div className="advisor-hero-actions">
          <button
            type="button"
            className="advisor-analyze-btn"
            onClick={() => runAnalysis()}
            disabled={isLoading || !activeCycleId}
          >
            {isLoading ? (
              <>
                <Loader2 size={18} className="spinner" />
                Analyzing Activities...
              </>
            ) : (
              <>
                <Sparkles size={18} />
                Re-Analyze Activities
              </>
            )}
          </button>
        </div>
      </div>

      {error && (
        <div className="advisor-error-banner">
          <AlertTriangle size={20} />
          <div>
            <strong>Analysis Failed:</strong> {error}
          </div>
        </div>
      )}

      {isLoading && !analysis ? (
        <div className="advisor-loading-card">
          <Loader2 size={40} className="spinner" />
          <h3>Synthesizing Agronomic Intelligence...</h3>
          <p>Analyzing historical field logs, calculating crop growth stage, and auditing against DOA safe limits.</p>
        </div>
      ) : analysis ? (
        <>
          {/* Executive Summary */}
          <div className="advisor-summary-card">
            <div className="summary-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <CheckCircle2 size={20} style={{ color: 'var(--forest)' }} />
                <h3 style={{ margin: 0 }}>Agronomic Executive Summary</h3>
              </div>
              <span className="analysis-timestamp">
                Analyzed at {new Date(analysis.analyzedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
              </span>
            </div>
            <p className="summary-body">{analysis.executiveSummary}</p>
          </div>

          {/* Safety Warnings Banner if any */}
          {analysis.warnings.length > 0 && (
            <div className="advisor-warnings-banner">
              <div className="warning-title">
                <ShieldAlert size={22} />
                <span>Critical Agronomic & Safety Warnings ({analysis.warnings.length})</span>
              </div>
              <ul className="warning-list">
                {analysis.warnings.map((w, idx) => (
                  <li key={idx}>{w}</li>
                ))}
              </ul>
              {analysis.requiresOfficerReview && (
                <div className="officer-escalation-box">
                  <PhoneCall size={16} />
                  <span>
                    <strong>Officer Review Advised:</strong> Potential regulatory or crop risk detected.
                    Please consult with your nearest Agrarian Services Center Agricultural Instructor (AI).
                  </span>
                </div>
              )}
            </div>
          )}

          {/* 4 Diagnostics Cards Grid */}
          <div className="diagnostics-section-header">
            <div>
              <h3 className="section-subtitle">Real-Time Field Diagnostics</h3>
              <p className="section-subtext">Telemetry and agronomic condition evaluated against stage-specific DOA & RRDI thresholds</p>
            </div>
          </div>

          <div className="diagnostics-grid">
            {/* 1. Water Diagnostic */}
            <div className="diagnostic-card water-card">
              <div className="diagnostic-head">
                <div className="diag-title">
                  <div className="diag-icon-wrapper water-icon">
                    <Droplets size={18} />
                  </div>
                  <div>
                    <h4>Water Management</h4>
                    <span className="diag-category-sub">Hydrology & Depth</span>
                  </div>
                </div>
                <span className={`status-pill ${getStatusBadgeClass(analysis.diagnostics.water.status)}`}>
                  <span className="status-dot"></span>
                  {formatStatusBadge(analysis.diagnostics.water.status)}
                </span>
              </div>

              <div className="diagnostic-metrics-box">
                <div className="diag-hero-metric">
                  <div className="hero-value-group">
                    <span className="hero-number">
                      {analysis.diagnostics.water.latestWaterLevelCm !== null && analysis.diagnostics.water.latestWaterLevelCm !== undefined
                        ? analysis.diagnostics.water.latestWaterLevelCm
                        : '—'}
                    </span>
                    {analysis.diagnostics.water.latestWaterLevelCm !== null && analysis.diagnostics.water.latestWaterLevelCm !== undefined && (
                      <span className="hero-unit">cm</span>
                    )}
                  </div>
                  <span className="hero-label">Current Water Depth</span>
                </div>

                <div className="diag-sub-metrics-grid">
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Irrigations</span>
                    <span className="sub-metric-value">{analysis.diagnostics.water.totalIrrigationEvents} events</span>
                  </div>
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Last Logged</span>
                    <span className="sub-metric-value">
                      {analysis.diagnostics.water.daysSinceLastIrrigation !== null && analysis.diagnostics.water.daysSinceLastIrrigation !== undefined
                        ? `${analysis.diagnostics.water.daysSinceLastIrrigation}d ago`
                        : 'None'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="diagnostic-assessment">
                <span className="diag-assessment-label">DOA Threshold Assessment</span>
                <p className="diagnostic-text">{analysis.diagnostics.water.assessment}</p>
              </div>
            </div>

            {/* 2. Fertilizer Diagnostic */}
            <div className="diagnostic-card fert-card">
              <div className="diagnostic-head">
                <div className="diag-title">
                  <div className="diag-icon-wrapper fert-icon">
                    <Leaf size={18} />
                  </div>
                  <div>
                    <h4>Nutrient Balance</h4>
                    <span className="diag-category-sub">Urea & N-P-K Splits</span>
                  </div>
                </div>
                <span className={`status-pill ${getStatusBadgeClass(analysis.diagnostics.fertilizer.status)}`}>
                  <span className="status-dot"></span>
                  {formatStatusBadge(analysis.diagnostics.fertilizer.status)}
                </span>
              </div>

              <div className="diagnostic-metrics-box">
                <div className="diag-hero-metric">
                  <div className="hero-value-group">
                    <span className="hero-number">
                      {analysis.diagnostics.fertilizer.totalUreaKgPerHa ?? analysis.diagnostics.fertilizer.TotalUreaKgPerHa ?? 0}
                    </span>
                    <span className="hero-unit">kg/ha</span>
                  </div>
                  <span className="hero-label">Total Urea Logged</span>
                </div>

                <div className="diag-sub-metrics-grid">
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Splits</span>
                    <span className="sub-metric-value">{analysis.diagnostics.fertilizer.applicationsCount} split(s)</span>
                  </div>
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Requirement</span>
                    <span className="sub-metric-value" title={analysis.diagnostics.fertilizer.splitCompliance || 'Active'}>
                      {analysis.diagnostics.fertilizer.splitCompliance || 'Active'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="diagnostic-assessment">
                <span className="diag-assessment-label">DOA Split Assessment</span>
                <p className="diagnostic-text">{analysis.diagnostics.fertilizer.assessment}</p>
              </div>
            </div>

            {/* 3. Crop Protection Diagnostic */}
            <div className="diagnostic-card pest-card">
              <div className="diagnostic-head">
                <div className="diag-title">
                  <div className="diag-icon-wrapper pest-icon">
                    <ShieldAlert size={18} />
                  </div>
                  <div>
                    <h4>Crop Protection</h4>
                    <span className="diag-category-sub">Chemicals & Pests</span>
                  </div>
                </div>
                <span className={`status-pill ${getStatusBadgeClass(analysis.diagnostics.pest.status)}`}>
                  <span className="status-dot"></span>
                  {formatStatusBadge(analysis.diagnostics.pest.status)}
                </span>
              </div>

              <div className="diagnostic-metrics-box">
                <div className="diag-hero-metric">
                  <div className="hero-value-group">
                    <span className="hero-number">
                      {analysis.diagnostics.pest.treatmentsCount}
                    </span>
                    <span className="hero-unit">{analysis.diagnostics.pest.treatmentsCount === 1 ? 'spray' : 'sprays'}</span>
                  </div>
                  <span className="hero-label">Sprays Logged</span>
                </div>

                <div className="diag-sub-metrics-grid">
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Products</span>
                    <span className="sub-metric-value" title={analysis.diagnostics.pest.productsUsed?.join(', ') || 'None'}>
                      {analysis.diagnostics.pest.productsUsed && analysis.diagnostics.pest.productsUsed.length > 0
                        ? analysis.diagnostics.pest.productsUsed.join(', ')
                        : 'None'}
                    </span>
                  </div>
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">ROP Safety</span>
                    <span className={`sub-metric-value ${analysis.diagnostics.pest.bannedChemicalDetected ? 'metric-alert' : 'metric-safe'}`}>
                      {analysis.diagnostics.pest.bannedChemicalDetected ? 'Banned Alert' : 'Standard (Safe)'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="diagnostic-assessment">
                <span className="diag-assessment-label">ROP Safety Assessment</span>
                <p className="diagnostic-text">{analysis.diagnostics.pest.assessment}</p>
              </div>
            </div>

            {/* 4. Field Operations Diagnostic */}
            <div className="diagnostic-card other-card">
              <div className="diagnostic-head">
                <div className="diag-title">
                  <div className="diag-icon-wrapper other-icon">
                    <Layers size={18} />
                  </div>
                  <div>
                    <h4>Field Operations</h4>
                    <span className="diag-category-sub">Weeding & Agronomy</span>
                  </div>
                </div>
                <span className={`status-pill ${getStatusBadgeClass(analysis.diagnostics.other.weedingStatus || 'neutral')}`}>
                  <span className="status-dot"></span>
                  {formatStatusBadge(analysis.diagnostics.other.weedingStatus || 'Recorded')}
                </span>
              </div>

              <div className="diagnostic-metrics-box">
                <div className="diag-hero-metric">
                  <div className="hero-value-group">
                    <span className="hero-number">
                      {analysis.diagnostics.other.activitiesCount}
                    </span>
                    <span className="hero-unit">{analysis.diagnostics.other.activitiesCount === 1 ? 'task' : 'tasks'}</span>
                  </div>
                  <span className="hero-label">Field Tasks Recorded</span>
                </div>

                <div className="diag-sub-metrics-grid">
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Weeding</span>
                    <span className="sub-metric-value">
                      {analysis.diagnostics.other.weedingStatus || 'Not Recorded'}
                    </span>
                  </div>
                  <div className="sub-metric-item">
                    <span className="sub-metric-label">Growth Stage</span>
                    <span className="sub-metric-value" title={analysis.fieldOverview.currentStage}>
                      {analysis.fieldOverview.currentStage}
                    </span>
                  </div>
                </div>
              </div>

              <div className="diagnostic-assessment">
                <span className="diag-assessment-label">Operational Status</span>
                <p className="diagnostic-text">{analysis.diagnostics.other.assessment}</p>
              </div>
            </div>
          </div>

          {/* Evidence-Based Recommendations */}
          <div className="recommendations-section">
            <h3 className="section-subtitle" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span>Actionable Agronomic Recommendations ({analysis.recommendations.length})</span>
              <span style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', fontWeight: 'normal' }}>
                Prioritized by crop impact & urgency
              </span>
            </h3>

            {analysis.recommendations.length === 0 ? (
              <div className="empty-recommendations-card">
                <CheckCircle2 size={32} style={{ color: 'var(--forest)' }} />
                <h4>No Immediate Corrective Actions Needed</h4>
                <p>Your logged activities align well with Sri Lankan Department of Agriculture guidelines. Continue regular monitoring.</p>
              </div>
            ) : (
              <div className="recommendations-list">
                {analysis.recommendations.map((rec, index) => (
                  <div key={rec.id || index} className="recommendation-card">
                    <div className="recommendation-header">
                      <div className="rec-badges">
                        <span className={`priority-badge ${getPriorityClass(rec.priority)}`}>
                          {rec.priority} PRIORITY
                        </span>
                        <span className="category-badge">{rec.category}</span>
                        <span className="confidence-pill" title="AI confidence calculated against official rule benchmarks">
                          {Math.round(rec.confidenceScore * 100)}% Confidence
                        </span>
                      </div>
                      {rec.requiresOfficerReview && (
                        <span className="officer-review-tag">
                          <PhoneCall size={12} /> Officer Review
                        </span>
                      )}
                    </div>

                    <h4 className="rec-action">{rec.action}</h4>
                    <p className="rec-reason">{rec.reason}</p>

                    {rec.evidence && (
                      <div className="rec-evidence-box">
                        <Info size={15} style={{ flexShrink: 0, marginTop: '2px' }} />
                        <span><strong>Triggered by Activity Log:</strong> {rec.evidence}</span>
                      </div>
                    )}

                    {rec.citations && rec.citations.length > 0 && (
                      <div className="rec-citations">
                        <BookOpen size={14} />
                        <span>
                          <strong>Evidence Source:</strong> {rec.citations.map(c => `${c.document} (${c.section})`).join('; ')}
                        </span>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Interactive "Ask AI" Field Assistant */}
          <div className="advisor-chat-section">
            <div className="chat-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Sparkles size={18} style={{ color: 'var(--gold-deep)' }} />
                <h4 style={{ margin: 0 }}>Ask AI About This Field's Activities</h4>
              </div>
              <span style={{ fontSize: '0.8rem', color: 'var(--ink-soft)' }}>
                Grounded in your actual recorded water, nutrient, and chemical history
              </span>
            </div>

            {chatMessages.length > 0 && (
              <div className="chat-messages-container">
                {chatMessages.map((msg, i) => (
                  <div key={i} className={`chat-bubble ${msg.role}`}>
                    <strong>{msg.role === 'user' ? 'You' : 'PaddyWise AI'}:</strong>
                    <p style={{ margin: '0.25rem 0 0' }}>{msg.content}</p>
                  </div>
                ))}
                {isChatLoading && (
                  <div className="chat-bubble assistant" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <Loader2 size={16} className="spinner" /> PaddyWise AI is reviewing your logs...
                  </div>
                )}
              </div>
            )}

            <div className="chat-input-wrapper">
              <input
                type="text"
                className="chat-input"
                placeholder="Ask e.g. 'Can I apply urea tomorrow?' or 'Is my water level safe?'..."
                value={chatQuestion}
                onChange={e => setChatQuestion(e.target.value)}
                onKeyDown={e => {
                  if (e.key === 'Enter') handleAskQuestion();
                }}
                disabled={isChatLoading}
              />
              <button
                type="button"
                className="chat-send-btn"
                onClick={() => handleAskQuestion()}
                disabled={!chatQuestion.trim() || isChatLoading}
              >
                <Send size={16} />
              </button>
            </div>

            <div className="chat-suggestions">
              <span style={{ fontSize: '0.8rem', color: 'var(--ink-soft)', marginRight: '0.5rem' }}>Suggested:</span>
              <button
                type="button"
                className="suggestion-chip"
                onClick={() => handleAskQuestion('Is my water level appropriate for this stage?')}
              >
                Is my water level appropriate?
              </button>
              <button
                type="button"
                className="suggestion-chip"
                onClick={() => handleAskQuestion('When should I apply my next fertilizer split?')}
              >
                Next fertilizer split?
              </button>
              <button
                type="button"
                className="suggestion-chip"
                onClick={() => handleAskQuestion('Are there any chemical safety risks in my logs?')}
              >
                Chemical safety check
              </button>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
};
