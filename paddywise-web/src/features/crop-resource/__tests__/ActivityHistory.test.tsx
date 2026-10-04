import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ActivityHistory } from '../components/ActivityHistory';
import { activityApi, type CropActivityDto } from '../services/activityApi';
import type { CultivationCycle } from '../../field-cultivation/types';

describe('ActivityHistory Component Tests', () => {
  const sampleCycles: CultivationCycle[] = [
    {
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
    }
  ];

  const sampleActivities: CropActivityDto[] = [
    {
      id: 1,
      cultivationCycleId: 4,
      activityType: 'Fertilizer',
      date: '2026-10-02T08:00:00Z',
      detailsJson: JSON.stringify({
        date: '2026-10-02',
        type: 'Urea',
        quantity: 50,
        cropStage: 'Tillering',
        region: 'Wet',
        method: 'Broadcasting'
      }),
      loggedByUserId: 12,
      loggedByUserName: 'Sunil Farmer',
      createdAt: '2026-10-02T08:30:00Z',
      fieldName: 'Maha Kumbura (Plot 04)',
      farmerName: 'Sunil Bandara',
      cycleName: 'Maha 2026'
    },
    {
      id: 2,
      cultivationCycleId: 4,
      activityType: 'Irrigation',
      date: '2026-10-01T06:00:00Z',
      detailsJson: JSON.stringify({
        date: '2026-10-01',
        waterLevel: 5,
        duration: 4,
        source: 'Canal'
      }),
      loggedByUserId: 12,
      loggedByUserName: 'Sunil Farmer',
      createdAt: '2026-10-01T06:30:00Z',
      fieldName: 'Maha Kumbura (Plot 04)',
      farmerName: 'Sunil Bandara',
      cycleName: 'Maha 2026'
    },
    {
      id: 3,
      cultivationCycleId: 4,
      activityType: 'Pesticide',
      date: '2026-09-28T09:00:00Z',
      detailsJson: JSON.stringify({
        date: '2026-09-28',
        product: 'Chlorantraniliprole',
        targetPest: 'Stem Borer',
        quantity: 100,
        method: 'Spraying'
      }),
      loggedByUserId: 12,
      loggedByUserName: 'Sunil Farmer',
      createdAt: '2026-09-28T09:30:00Z',
      fieldName: 'Maha Kumbura (Plot 04)',
      farmerName: 'Sunil Bandara',
      cycleName: 'Maha 2026'
    }
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Loading and Rendering Activities', () => {
    it('shows loading indicator while fetching activities', () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockImplementation(
        () => new Promise(() => {})
      );

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(screen.getByText('Loading history...')).toBeInTheDocument();
    });

    it('renders list of activities with type badges, details, and analytics', async () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockResolvedValueOnce(sampleActivities);

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(await screen.findByText('Past Activities')).toBeInTheDocument();
      expect(screen.getByText('Fertilizer')).toBeInTheDocument();
      expect(screen.getByText('Irrigation')).toBeInTheDocument();
      expect(screen.getByText('Pesticide')).toBeInTheDocument();

      // Check rendered details
      expect(screen.getByText(/50 kg\/ha/)).toBeInTheDocument();
      expect(screen.getByText(/5 cm/)).toBeInTheDocument();
      expect(screen.getByText(/Chlorantraniliprole/)).toBeInTheDocument();
    });

    it('shows empty state when no activities have been logged', async () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockResolvedValueOnce([]);

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(await screen.findByText('No activities recorded for this cycle yet.')).toBeInTheDocument();
    });

    it('shows error message when loading activities fails', async () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockRejectedValueOnce(
        new Error('Network error 500')
      );

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(await screen.findByText('Failed to load activity history.')).toBeInTheDocument();
    });
  });

  describe('Filtering Activities', () => {
    it('filters activities by category dropdown', async () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockResolvedValueOnce(sampleActivities);

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(await screen.findByText('Past Activities')).toBeInTheDocument();

      // Select "Irrigation" from category filter
      const categorySelect = screen.getByDisplayValue('All Categories');
      fireEvent.change(categorySelect, { target: { value: 'Irrigation' } });

      // Should show Irrigation, but not Fertilizer or Pesticide cards
      expect(screen.getByText(/4 hrs/)).toBeInTheDocument();
      expect(screen.queryByText(/Chlorantraniliprole/)).not.toBeInTheDocument();
    });

    it('shows "No activities match the selected filters" when filter yields no results', async () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockResolvedValueOnce(sampleActivities);

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(await screen.findByText('Past Activities')).toBeInTheDocument();

      // Select "Other" which has no items
      const categorySelect = screen.getByDisplayValue('All Categories');
      fireEvent.change(categorySelect, { target: { value: 'Other' } });

      expect(screen.getByText('No activities match the selected filters.')).toBeInTheDocument();
    });
  });

  describe('Activity Actions: Edit and Delete', () => {
    it('shows Edit and Delete buttons for Farmer role and opens confirmation on delete', async () => {
      vi.spyOn(activityApi, 'getActivitiesForCycle').mockResolvedValueOnce(sampleActivities);
      const deleteSpy = vi.spyOn(activityApi, 'deleteActivity').mockResolvedValueOnce();

      render(
        <ActivityHistory
          selectedCycleId={4}
          cycles={sampleCycles}
          userRole="Farmer"
        />
      );

      expect(await screen.findByText('Past Activities')).toBeInTheDocument();

      const deleteBtns = screen.getAllByRole('button', { name: /Delete/i });
      expect(deleteBtns.length).toBeGreaterThanOrEqual(1);

      // Click delete on the first item
      fireEvent.click(deleteBtns[0]);

      // Check confirmation modal appears
      expect(screen.getByText('Delete Activity')).toBeInTheDocument();
      expect(screen.getByText(/Are you sure you want to delete this/i)).toBeInTheDocument();

      // Confirm deletion
      const confirmDeleteBtn = screen.getByRole('button', { name: /Delete Activity/i });
      fireEvent.click(confirmDeleteBtn);

      await waitFor(() => {
        expect(deleteSpy).toHaveBeenCalledWith(1);
      });
    });

    it('does not render Edit/Delete buttons when user role is not authorized (e.g. Officer viewing history)', async () => {
      vi.spyOn(activityApi, 'getAllActivities').mockResolvedValueOnce(sampleActivities);

      render(
        <ActivityHistory
          selectedCycleId="all"
          cycles={sampleCycles}
          userRole="AgriculturalOfficer"
        />
      );

      expect(await screen.findByText('All Farmers Past Activities')).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /Edit/i })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /Delete/i })).not.toBeInTheDocument();
    });
  });
});
