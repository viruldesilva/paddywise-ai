import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ActivityForm } from '../components/ActivityForm';
import type { CultivationCycle } from '../../field-cultivation/types';

describe('ActivityForm Component Tests', () => {
  const mockOnSubmit = vi.fn();

  const sampleCycle: CultivationCycle = {
    id: 4,
    fieldId: 1,
    fieldName: 'Maha Kumbura (Plot 04)',
    varietyId: 2,
    varietyName: 'Bg 352',
    durationDays: 105,
    season: 'Maha',
    year: 2026,
    method: 'Broadcasting',
    sowingDate: '2026-09-01',
    expectedHarvestDate: '2026-12-15',
    actualHarvestDate: null,
    currentStage: 'Tillering',
    expectedStageToday: 'Tillering',
    status: 'Active',
    notes: null,
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: '2026-09-01T00:00:00Z',
    timeline: [],
    stageLogs: []
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Form Field Rendering by Activity Type', () => {
    it('renders Irrigation form fields correctly', () => {
      render(
        <ActivityForm
          activityType="Irrigation"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      expect(screen.getByLabelText(/Activity Date/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/e\.g\. 5/i)).toBeInTheDocument(); // Water level
      expect(screen.getByPlaceholderText(/e\.g\. 2/i)).toBeInTheDocument(); // Duration
      expect(screen.getByRole('combobox', { name: '' })).toBeInTheDocument(); // Source select
      expect(screen.getByRole('button', { name: /Save Irrigation Activity/i })).toBeInTheDocument();
    });

    it('renders Fertilizer form fields with preset stage from cycle', () => {
      render(
        <ActivityForm
          activityType="Fertilizer"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      expect(screen.getByText('Fertilizer Type')).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/e\.g\. 50/i)).toBeInTheDocument(); // Quantity
      expect(screen.getByText('Crop Stage')).toBeInTheDocument();
      expect(screen.getByText('Region / Zone')).toBeInTheDocument();
      expect(screen.getByText('Application Method')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Save Fertilizer Activity/i })).toBeInTheDocument();
    });

    it('renders Pesticide form fields correctly', () => {
      render(
        <ActivityForm
          activityType="Pesticide"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      expect(screen.getByPlaceholderText(/e\.g\. Chlorantraniliprole/i)).toBeInTheDocument(); // Product name
      expect(screen.getByPlaceholderText(/e\.g\. Stem Borer/i)).toBeInTheDocument(); // Target pest
      expect(screen.getByPlaceholderText(/e\.g\. 100/i)).toBeInTheDocument(); // Quantity
      expect(screen.getByText('Method')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Save Pesticide Activity/i })).toBeInTheDocument();
    });

    it('renders Other activity form fields correctly', () => {
      render(
        <ActivityForm
          activityType="Other"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      expect(screen.getByText('Activity Type')).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/Add optional activity notes\.\.\./i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Save Other Activity/i })).toBeInTheDocument();
    });
  });

  describe('Form Validation Rules', () => {
    it('shows validation errors when submitting empty form', () => {
      render(
        <ActivityForm
          activityType="Irrigation"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      const submitBtn = screen.getByRole('button', { name: /Save Irrigation Activity/i });
      fireEvent.click(submitBtn);

      expect(mockOnSubmit).not.toHaveBeenCalled();
      expect(screen.getByRole('alert')).toHaveTextContent(/Please correct the highlighted fields/i);
      expect(screen.getByText(/Water level is required/i)).toBeInTheDocument();
      expect(screen.getByText(/Duration is required/i)).toBeInTheDocument();
      expect(screen.getByText(/Please select a water source/i)).toBeInTheDocument();
    });

    it('validates date boundaries: prevents future dates', () => {
      render(
        <ActivityForm
          activityType="Other"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      const dateInput = screen.getByLabelText(/Activity Date/i);
      // Set to future date
      fireEvent.change(dateInput, { target: { value: '2099-01-01' } });
      fireEvent.blur(dateInput);

      const submitBtn = screen.getByRole('button', { name: /Save Other Activity/i });
      fireEvent.click(submitBtn);

      expect(screen.getByText(/Activity date cannot be in the future/i)).toBeInTheDocument();
      expect(mockOnSubmit).not.toHaveBeenCalled();
    });

    it('validates fertilizer numeric constraints: rejects non-positive or excessive quantity', () => {
      render(
        <ActivityForm
          activityType="Fertilizer"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      const qtyInput = screen.getByPlaceholderText(/e\.g\. 50/i);
      fireEvent.change(qtyInput, { target: { value: '-10' } });
      fireEvent.blur(qtyInput);

      expect(screen.getByText(/Quantity must be a positive number greater than 0/i)).toBeInTheDocument();

      fireEvent.change(qtyInput, { target: { value: '6000' } });
      fireEvent.blur(qtyInput);

      expect(screen.getByText(/Quantity exceeds maximum limit \(5,000 kg\/ha\)/i)).toBeInTheDocument();
    });

    it('validates irrigation limits: rejects water level exceeding 150 cm and duration over 72h', () => {
      render(
        <ActivityForm
          activityType="Irrigation"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      const wlInput = screen.getByPlaceholderText(/e\.g\. 5/i);
      fireEvent.change(wlInput, { target: { value: '200' } });
      fireEvent.blur(wlInput);

      expect(screen.getByText(/Water level cannot exceed 150 cm/i)).toBeInTheDocument();

      const durInput = screen.getByPlaceholderText(/e\.g\. 2/i);
      fireEvent.change(durInput, { target: { value: '100' } });
      fireEvent.blur(durInput);

      expect(screen.getByText(/Duration cannot exceed 72 hours/i)).toBeInTheDocument();
    });
  });

  describe('Successful Form Submission', () => {
    it('submits valid Irrigation activity payload', () => {
      render(
        <ActivityForm
          activityType="Irrigation"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      const today = new Date().toISOString().split('T')[0];
      const dateInput = screen.getByLabelText(/Activity Date/i);
      fireEvent.change(dateInput, { target: { value: today } });

      const wlInput = screen.getByPlaceholderText(/e\.g\. 5/i);
      fireEvent.change(wlInput, { target: { value: '5' } });

      const durInput = screen.getByPlaceholderText(/e\.g\. 2/i);
      fireEvent.change(durInput, { target: { value: '3' } });

      const sourceSelect = screen.getByDisplayValue(/Select source/i);
      fireEvent.change(sourceSelect, { target: { value: 'Canal' } });

      const submitBtn = screen.getByRole('button', { name: /Save Irrigation Activity/i });
      fireEvent.click(submitBtn);

      expect(mockOnSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          activityType: 'Irrigation',
          cycleId: 4,
          waterLevel: '5',
          duration: '3',
          source: 'Canal'
        })
      );
    });

    it('submits valid Fertilizer activity payload', () => {
      render(
        <ActivityForm
          activityType="Fertilizer"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
        />
      );

      const today = new Date().toISOString().split('T')[0];
      const dateInput = screen.getByLabelText(/Activity Date/i);
      fireEvent.change(dateInput, { target: { value: today } });

      const typeSelect = screen.getByDisplayValue(/Select fertilizer type/i);
      fireEvent.change(typeSelect, { target: { value: 'Urea' } });

      const qtyInput = screen.getByPlaceholderText(/e\.g\. 50/i);
      fireEvent.change(qtyInput, { target: { value: '50' } });

      const stageSelect = screen.getByDisplayValue(/Tillering/i);
      fireEvent.change(stageSelect, { target: { value: 'Tillering' } });

      const regionSelect = screen.getByDisplayValue(/Select climatic zone/i);
      fireEvent.change(regionSelect, { target: { value: 'Wet' } });

      const methodSelect = screen.getByDisplayValue(/Select application method/i);
      fireEvent.change(methodSelect, { target: { value: 'Broadcasting' } });

      const submitBtn = screen.getByRole('button', { name: /Save Fertilizer Activity/i });
      fireEvent.click(submitBtn);

      expect(mockOnSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          activityType: 'Fertilizer',
          cycleId: 4,
          type: 'Urea',
          quantity: '50',
          cropStage: 'Tillering',
          region: 'Wet',
          method: 'Broadcasting'
        })
      );
    });

    it('disables submit button and shows loading text when isSubmitting is true', () => {
      render(
        <ActivityForm
          activityType="Irrigation"
          selectedCycle={sampleCycle}
          onSubmit={mockOnSubmit}
          isSubmitting={true}
        />
      );

      const submitBtn = screen.getByRole('button', { name: /Saving Activity\.\.\./i });
      expect(submitBtn).toBeDisabled();
    });
  });
});
