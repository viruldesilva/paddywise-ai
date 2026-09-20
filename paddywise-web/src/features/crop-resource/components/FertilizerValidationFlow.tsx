import React, { useEffect, useState } from 'react';
import { getFertilizerRule, type FertilizerRule } from '../data/fertilizerRules';

export interface FertilizerData {
  type: string;
  quantity: string;
  date: string;
  cropStage: string;
  region: string;
  method: string;
  cycleName?: string;
}

interface ValidationFlowProps {
  data: FertilizerData;
  onClose: () => void;
}

export const FertilizerValidationFlow: React.FC<ValidationFlowProps> = ({ data, onClose }) => {
  const [currentStep, setCurrentStep] = useState(0);
  const [result, setResult] = useState<'accept' | 'review' | 'reject' | null>(null);
  const [matchedRule, setMatchedRule] = useState<FertilizerRule | null>(null);
  
  const steps = [
    { title: 'Checking Cultivation Stage', desc: `Verifying stage: ${data.cropStage}` },
    { title: 'Checking Field / Region', desc: `Validating for ${data.region} zone` },
    { title: 'Checking Allowed Recommendation', desc: `Fetching Dept of Agriculture rules for ${data.type}` },
    { title: 'Calculating Recommended Range', desc: 'Comparing requested quantity with recommended limits' }
  ];

  useEffect(() => {
    // 1. Validate based on real DOA Rules
    const rule = getFertilizerRule(data.region, data.cropStage, data.type);

    // Simulate validation flow steps visually
    if (currentStep < steps.length) {
      const timer = setTimeout(() => {
        setCurrentStep(prev => prev + 1);
      }, 1000); // 1s per step
      return () => clearTimeout(timer);
    } else if (currentStep === steps.length && !result) {
      // Calculate final outcome
      let calculatedResult: 'accept' | 'review' | 'reject' = 'reject';
      
      if (rule) {
        setMatchedRule(rule);
        const requestedQty = parseFloat(data.quantity);
        const maxRecommended = rule.recommendedAmountKgPerHa;
        
        if (requestedQty <= maxRecommended) {
          calculatedResult = 'accept';
        } else if (requestedQty <= maxRecommended * 1.2) { // Allow up to 20% over for review
          calculatedResult = 'review';
        } else {
          calculatedResult = 'reject';
        }
      } else {
        // If no explicit rule found, we reject or request review (fallback)
        calculatedResult = 'review';
      }

      setTimeout(() => {
        setResult(calculatedResult);
      }, 500);
    }
  }, [currentStep, result, data]);

  return (
    <div className="validation-flow">
      <div className="validation-header" style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '2rem' }}>
        <div className="validation-icon" style={{ padding: '1rem', background: 'var(--cream)', borderRadius: '50%', color: 'var(--shoot)' }}>
          <svg fill="none" viewBox="0 0 24 24" stroke="currentColor" width="32" height="32">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <div>
          <h3 style={{ color: 'var(--ink)', fontFamily: 'var(--font-heading)', fontSize: '1.5rem', marginBottom: '0.25rem' }}>Validation in Progress</h3>
          <p style={{ margin: 0, fontSize: '0.9rem', color: 'var(--ink-soft)' }}>
            Processing fertilizer application request {data.cycleName ? `for ${data.cycleName}` : ''}
          </p>
        </div>
      </div>
      
      <div className="step-list" style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {steps.map((step, index) => {
          const isActive = index === currentStep;
          const isCompleted = index < currentStep;
          
          return (
            <div key={index} style={{
              display: 'flex',
              gap: '1rem',
              opacity: isActive || isCompleted ? 1 : 0.4,
              transform: isActive ? 'translateX(10px)' : 'none',
              transition: 'all 0.3s ease'
            }}>
              <div style={{
                width: '12px',
                height: '12px',
                borderRadius: '50%',
                marginTop: '6px',
                background: isCompleted ? 'var(--shoot)' : isActive ? 'var(--gold)' : 'var(--line)'
              }}></div>
              <div className="step-content">
                <div style={{ fontWeight: 600, color: 'var(--ink)', marginBottom: '0.25rem' }}>{step.title}</div>
                <div style={{ fontSize: '0.9rem', color: 'var(--ink-soft)' }}>{step.desc}</div>
              </div>
            </div>
          );
        })}
      </div>

      {result && (
        <div style={{
          marginTop: '2rem',
          padding: '1.5rem',
          borderRadius: '12px',
          background: result === 'accept' ? 'rgba(127, 166, 108, 0.1)' : result === 'review' ? 'rgba(225, 166, 59, 0.1)' : 'rgba(200, 50, 50, 0.05)',
          border: `1px solid ${result === 'accept' ? 'var(--shoot)' : result === 'review' ? 'var(--gold)' : '#ffcccc'}`
        }}>
          <div style={{ fontWeight: 600, color: 'var(--ink)', marginBottom: '1rem' }}>
            {result === 'accept' && '✅ Application Accepted: Quantity is within recommended limits.'}
            {result === 'review' && '⚠️ Officer Review Requested: Quantity exceeds baseline recommendation slightly.'}
            {result === 'reject' && '❌ Application Rejected: Quantity critically exceeds safety/efficiency limits.'}
          </div>
          
          {matchedRule ? (
            <div style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', borderTop: '1px solid var(--line)', paddingTop: '1rem' }}>
              <strong>Knowledge Source:</strong><br />
              {matchedRule.sourceReference}<br />
              <span style={{ color: 'var(--forest-deep)', fontWeight: 600, marginTop: '0.5rem', display: 'block' }}>
                Recommended Max: {matchedRule.recommendedAmountKgPerHa} kg/ha
              </span>
            </div>
          ) : (
            <div style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', borderTop: '1px solid var(--line)', paddingTop: '1rem' }}>
              ⚠️ No specific DOA rule found for {data.region} zone, {data.cropStage} stage, {data.type}.
            </div>
          )}

          <div style={{ marginTop: '1.5rem' }}>
            <button className="btn btn-secondary" onClick={onClose} style={{ width: '100%' }}>
              Close & Continue
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
