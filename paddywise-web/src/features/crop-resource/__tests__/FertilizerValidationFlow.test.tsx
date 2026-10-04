import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { FertilizerValidationFlow, type FertilizerData } from '../components/FertilizerValidationFlow';

describe('FertilizerValidationFlow Component Tests', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  const validData: FertilizerData = {
    type: 'Urea',
    quantity: '45',
    date: '2026-10-04',
    cropStage: 'Tillering',
    region: 'Wet',
    method: 'Broadcasting',
    cycleName: 'Maha 2026'
  };

  it('renders initial steps for validation flow', () => {
    render(<FertilizerValidationFlow data={validData} onClose={vi.fn()} />);

    expect(screen.getByText('Validation in Progress')).toBeInTheDocument();
    expect(screen.getByText(/Processing fertilizer application request for Maha 2026/i)).toBeInTheDocument();
    expect(screen.getByText('Checking Cultivation Stage')).toBeInTheDocument();
    expect(screen.getByText('Checking Field / Region')).toBeInTheDocument();
    expect(screen.getByText('Checking Allowed Recommendation')).toBeInTheDocument();
    expect(screen.getByText('Calculating Recommended Range')).toBeInTheDocument();
  });

  it('accepts application when quantity is within recommended DOA threshold', async () => {
    render(<FertilizerValidationFlow data={validData} onClose={vi.fn()} />);

    // Fast-forward through all simulation timer steps (4 steps * 1000ms + 500ms outcome)
    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(
      screen.getByText(/Application Accepted: Quantity is within recommended limits\./i)
    ).toBeInTheDocument();
    expect(screen.getByText(/DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy/i)).toBeInTheDocument();
    expect(screen.getByText(/Recommended Max: 50 kg\/ha/i)).toBeInTheDocument();
  });

  it('flags for review when quantity exceeds limit within 20% margin', () => {
    const slightlyExcessData: FertilizerData = {
      ...validData,
      quantity: '55' // 50 * 1.2 = 60 max for review; 55 is between 50 and 60
    };

    render(<FertilizerValidationFlow data={slightlyExcessData} onClose={vi.fn()} />);

    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(
      screen.getByText(/Officer Review Requested: Quantity exceeds baseline recommendation slightly\./i)
    ).toBeInTheDocument();
  });

  it('rejects application when quantity severely exceeds safety limits', () => {
    const dangerousData: FertilizerData = {
      ...validData,
      quantity: '120' // severely over 50 kg/ha limit
    };

    render(<FertilizerValidationFlow data={dangerousData} onClose={vi.fn()} />);

    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(
      screen.getByText(/Application Rejected: Quantity critically exceeds safety\/efficiency limits\./i)
    ).toBeInTheDocument();
  });
});
