import { describe, it, expect, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { ActivityForm, type ActivityType } from '../components/ActivityForm';
// Reused unchanged from Component 1's web suite.
import { makeCycle } from '../../field-cultivation/__tests__/fixtures';

/** The form computes "today" and "a week ago" as UTC ISO dates; the tests do the same. */
function isoDaysFromToday(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().split('T')[0];
}

function renderForm(type: ActivityType, cycleOverrides: Parameters<typeof makeCycle>[0] = {}) {
  const onSubmit = vi.fn();
  const { container } = render(
    <ActivityForm
      activityType={type}
      selectedCycle={makeCycle({ sowingDate: isoDaysFromToday(-40), actualHarvestDate: null, ...cycleOverrides })}
      onSubmit={onSubmit}
    />
  );
  const set = (name: string, value: string) =>
    fireEvent.change(container.querySelector(`[name="${name}"]`)!, { target: { name, value } });
  const submit = () => fireEvent.click(screen.getByRole('button', { name: `Save ${type} Activity` }));
  return { onSubmit, set, submit };
}

function fillIrrigation(set: (n: string, v: string) => void, waterLevel = '3', duration = '2') {
  set('waterLevel', waterLevel);
  set('duration', duration);
  set('source', 'Canal');
}

describe('ActivityForm — date window', () => {
  it.each([
    [0, null],
    [-7, null],
    [-8, 'Activity date must be within the last week'],
    [1, 'Activity date cannot be in the future.'],
  ])('a date %i days from today → %s', (offset, error) => {
    const { onSubmit, set, submit } = renderForm('Irrigation');
    fillIrrigation(set);
    set('date', isoDaysFromToday(offset));
    submit();

    if (error === null) {
      expect(onSubmit).toHaveBeenCalledTimes(1);
    } else {
      expect(onSubmit).not.toHaveBeenCalled();
      expect(screen.getByText(new RegExp(error))).toBeInTheDocument();
    }
  });

  it('a date before a recent sowing is refused', () => {
    const sowing = isoDaysFromToday(-3);
    const { onSubmit, set, submit } = renderForm('Irrigation', { sowingDate: sowing });
    fillIrrigation(set);
    set('date', isoDaysFromToday(-4));
    submit();

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(`⚠️ Activity date cannot be earlier than cycle sowing date (${sowing}).`)).toBeInTheDocument();
  });

  it('a date after the actual harvest is refused', () => {
    const harvest = isoDaysFromToday(-3);
    const { onSubmit, set, submit } = renderForm('Irrigation', { actualHarvestDate: harvest });
    fillIrrigation(set);
    set('date', isoDaysFromToday(-2));
    submit();

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(`⚠️ Activity date cannot be after harvest date (${harvest}).`)).toBeInTheDocument();
  });
});

describe('ActivityForm — field rules', () => {
  it.each([
    ['0', '2', null],
    ['150', '2', null],
    ['-1', '2', 'Water level must be 0 or greater.'],
    ['151', '2', 'Water level cannot exceed 150 cm.'],
    ['3', '0', 'Duration must be greater than 0 hours.'],
    ['3', '72', null],
    ['3', '73', 'Duration cannot exceed 72 hours.'],
  ])('irrigation water %s cm for %s h → %s', (level, duration, error) => {
    const { onSubmit, set, submit } = renderForm('Irrigation');
    fillIrrigation(set, level, duration);
    submit();

    if (error === null) expect(onSubmit).toHaveBeenCalledTimes(1);
    else {
      expect(onSubmit).not.toHaveBeenCalled();
      expect(screen.getByText(`⚠️ ${error}`)).toBeInTheDocument();
    }
  });

  it.each([
    ['F', 'Stem borer', '1', 'Product name must be at least 2 characters.'],
    ['x'.repeat(101), 'Stem borer', '1', 'Product name cannot exceed 100 characters.'],
    ['Fipronil', '', '1', 'Target pest / disease is required.'],
    ['Fipronil', 'Stem borer', '0', 'Quantity must be greater than 0.'],
  ])('pesticide %s / %s / %s → %s', (product, targetPest, quantity, error) => {
    const { onSubmit, set, submit } = renderForm('Pesticide');
    set('product', product);
    set('targetPest', targetPest);
    set('quantity', quantity);
    set('method', 'Spraying');
    submit();

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(`⚠️ ${error}`)).toBeInTheDocument();
  });

  it('an empty fertilizer form lists every required field', () => {
    const { onSubmit, submit } = renderForm('Fertilizer');
    submit();

    expect(onSubmit).not.toHaveBeenCalled();
    for (const error of [
      'Please select a fertilizer type.',
      'Quantity is required.',
      'Please select a climatic zone / region.',
      'Please select an application method.',
    ]) {
      expect(screen.getByText(`⚠️ ${error}`)).toBeInTheDocument();
    }
  });

  it('other: notes over 500 characters are refused', () => {
    const { onSubmit, set, submit } = renderForm('Other');
    set('specificActivity', 'Weeding');
    set('notes', 'n'.repeat(501));
    submit();

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText('⚠️ Notes cannot exceed 500 characters.')).toBeInTheDocument();
  });
});

describe('ActivityForm — valid submit', () => {
  it('fertilizer posts every field, the activity type and the cycle id; the stage defaults from the cycle', () => {
    const { onSubmit, set, submit } = renderForm('Fertilizer', { currentStage: 'Tillering' });
    set('type', 'Urea');
    set('quantity', '50');
    set('region', 'Dry');
    set('method', 'Broadcasting');
    submit();

    expect(onSubmit).toHaveBeenCalledWith({
      date: isoDaysFromToday(0),
      cropStage: 'Tillering',
      type: 'Urea',
      quantity: '50',
      region: 'Dry',
      method: 'Broadcasting',
      activityType: 'Fertilizer',
      cycleId: 12,
    });
  });
});
