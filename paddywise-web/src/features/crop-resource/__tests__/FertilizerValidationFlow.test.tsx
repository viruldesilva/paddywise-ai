import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import { FertilizerValidationFlow } from '../components/FertilizerValidationFlow';

const ACCEPT = '✅ Application Accepted: Quantity is within recommended limits.';
const REVIEW = '⚠️ Officer Review Requested: Quantity exceeds baseline recommendation slightly.';
const REJECT = '❌ Application Rejected: Quantity critically exceeds safety/efficiency limits.';

/** The flow walks 4 one-second steps, then shows its verdict 0.5 s later. */
function runFlow(quantity: string, overrides: Partial<{ region: string; cropStage: string; type: string }> = {}) {
  render(
    <FertilizerValidationFlow
      data={{ type: 'Urea', quantity, date: '2026-10-01', cropStage: 'Tillering', region: 'Dry', method: 'Broadcasting', ...overrides }}
      onClose={vi.fn()}
    />
  );
  for (let i = 0; i < 4; i++) act(() => vi.advanceTimersByTime(1000));
  act(() => vi.advanceTimersByTime(500));
}

describe('FertilizerValidationFlow (Dry / Tillering / Urea = 80 kg/ha)', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it.each([
    ['80', ACCEPT],    // exactly the recommendation
    ['80.1', REVIEW],  // just over
    ['96', REVIEW],    // exactly 120 %
    ['96.1', REJECT],  // just over 120 %
    ['0.5', ACCEPT],
  ])('%s kg/ha → %s', (quantity, verdict) => {
    runFlow(quantity);
    expect(screen.getByText(verdict)).toBeInTheDocument();
  });

  it('a combination with no DOA rule asks for officer review', () => {
    runFlow('10', { cropStage: 'Heading', type: 'TSP' });
    expect(screen.getByText(REVIEW)).toBeInTheDocument();
  });

  it('shows no verdict before the steps finish', () => {
    render(
      <FertilizerValidationFlow
        data={{ type: 'Urea', quantity: '80', date: '2026-10-01', cropStage: 'Tillering', region: 'Dry', method: 'Broadcasting' }}
        onClose={vi.fn()}
      />
    );
    act(() => vi.advanceTimersByTime(2000));
    expect(screen.queryByText(ACCEPT)).not.toBeInTheDocument();
  });
});
