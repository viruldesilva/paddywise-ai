import React, { useEffect, useState } from 'react';
import { getFertilizerRule, type FertilizerRule } from '../data/fertilizerRules';

export interface FertilizerData {
  type: string;
  quantity: string;
  date: string;
  cropStage: string;
  region: string;
  method: string;
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
      <div className="validation-header">
        <div className="validation-icon">
          <svg fill="none" viewBox="0 0 24 24" stroke="currentColor" width="24" height="24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <div>
          <h3>Validation in Progress</h3>
          <p style={{ margin: 0, fontSize: '0.9rem', color: 'var(--text-muted)' }}>
            Processing fertilizer application request
          </p>
        </div>
      </div>
      
      <div className="step-list">
        {steps.map((step, index) => {
          const isActive = index === currentStep;
          const isCompleted = index < currentStep;
          
          return (
            <div key={index} className={`step-item ${isActive ? 'active' : ''} ${isCompleted ? 'completed' : ''}`}>
              <div className="step-indicator"></div>
              <div className="step-content">
                <div className="step-title">{step.title}</div>
                <div className="step-desc">{step.desc}</div>
              </div>
            </div>
          );
        })}
      </div>

      {result && (
        <div className={`validation-result result-${result}`}>
          {result === 'accept' && '✅ Application Accepted: Quantity is within recommended limits.'}
          {result === 'review' && '⚠️ Officer Review Requested: Quantity exceeds baseline recommendation slightly.'}
          {result === 'reject' && '❌ Application Rejected: Quantity critically exceeds safety/efficiency limits.'}
          
          {matchedRule ? (
            <div className="rule-source" style={{ marginTop: '10px', fontSize: '0.85rem', color: 'var(--text-muted)', borderTop: '1px solid #ccc', paddingTop: '10px' }}>
              <strong>Knowledge Source:</strong><br />
              {matchedRule.sourceReference}<br />
              <span style={{ color: 'var(--primary-dark)' }}>
                Recommended Max: {matchedRule.recommendedAmountKgPerHa} kg/ha
              </span>
            </div>
          ) : (
            <div className="rule-source" style={{ marginTop: '10px', fontSize: '0.85rem', color: 'var(--text-muted)', borderTop: '1px solid #ccc', paddingTop: '10px' }}>
              ⚠️ No specific DOA rule found for {data.region} zone, {data.cropStage} stage, {data.type}.
            </div>
          )}

          <div style={{ marginTop: '15px' }}>
            <button className="btn" style={{ background: 'white', color: 'inherit', border: '1px solid currentColor' }} onClick={onClose}>
              Close & Continue
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
