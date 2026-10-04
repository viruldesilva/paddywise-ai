import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ActivityDashboard } from '../pages/ActivityDashboard';
import { AuthContext, type AuthContextType } from '../../../context/AuthContext';
import { fieldApi } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';

describe('ActivityDashboard Page Tests', () => {
  const mockFarmerUser = {
    name: 'Sunil Bandara',
    email: 'sunil@farmer.lk',
    role: 'Farmer' as const,
  };

  const mockOfficerUser = {
    name: 'Dr. Nilmini Perera',
    email: 'nilmini@gov.lk',
    role: 'AgriculturalOfficer' as const,
  };

  const createAuthContext = (user: any): AuthContextType => ({
    user,
    token: 'jwt_token',
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    clearAuth: vi.fn(),
  });

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

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Role-Based Navigation & Officer Redirects', () => {
    it('redirects AgriculturalOfficer to /officer/approvals by default', () => {
      render(
        <AuthContext.Provider value={createAuthContext(mockOfficerUser)}>
          <MemoryRouter initialEntries={['/activities']}>
            <Routes>
              <Route path="/activities" element={<ActivityDashboard />} />
              <Route path="/officer/approvals" element={<div>Officer Approvals Queue Page</div>} />
            </Routes>
          </MemoryRouter>
        </AuthContext.Provider>
      );

      expect(screen.getByText('Officer Approvals Queue Page')).toBeInTheDocument();
    });

    it('redirects AgriculturalOfficer to /officer/reports when tab=report', () => {
      render(
        <AuthContext.Provider value={createAuthContext(mockOfficerUser)}>
          <MemoryRouter initialEntries={['/activities?tab=report']}>
            <Routes>
              <Route path="/activities" element={<ActivityDashboard />} />
              <Route path="/officer/reports" element={<div>Officer Reports Page</div>} />
            </Routes>
          </MemoryRouter>
        </AuthContext.Provider>
      );

      expect(screen.getByText('Officer Reports Page')).toBeInTheDocument();
    });
  });

  describe('Farmer View and Tab Navigation', () => {
    it('renders dashboard with title and default History tab for Farmer', async () => {
      vi.spyOn(fieldApi, 'getMyCycles').mockResolvedValueOnce(sampleCycles);

      render(
        <AuthContext.Provider value={createAuthContext(mockFarmerUser)}>
          <MemoryRouter initialEntries={['/activities']}>
            <ActivityDashboard />
          </MemoryRouter>
        </AuthContext.Provider>
      );

      expect(screen.getByText('CROP RESOURCE & MANAGEMENT')).toBeInTheDocument();
      expect(screen.getByText('Record & Monitor Activities')).toBeInTheDocument();
      expect(screen.getByText('Activity Log & History')).toBeInTheDocument();
      expect(screen.getByText('AI Field Advisor & Recommendations')).toBeInTheDocument();
    });

    it('switches to AI Field Advisor tab when advisor tab button is clicked', async () => {
      vi.spyOn(fieldApi, 'getMyCycles').mockResolvedValueOnce(sampleCycles);

      render(
        <AuthContext.Provider value={createAuthContext(mockFarmerUser)}>
          <MemoryRouter initialEntries={['/activities']}>
            <ActivityDashboard />
          </MemoryRouter>
        </AuthContext.Provider>
      );

      const advisorTabBtn = screen.getByRole('button', { name: /AI Field Advisor & Recommendations/i });
      fireEvent.click(advisorTabBtn);

      expect(await screen.findByText('AI Paddy Field Advisor')).toBeInTheDocument();
      expect(
        screen.getByText(/Evidence-based agronomic intelligence synthesizing field activities/i)
      ).toBeInTheDocument();
    });

    it('displays cycle selector and "Record Activity" button when a cycle is selected', async () => {
      vi.spyOn(fieldApi, 'getMyCycles').mockResolvedValueOnce(sampleCycles);

      render(
        <AuthContext.Provider value={createAuthContext(mockFarmerUser)}>
          <MemoryRouter initialEntries={['/activities']}>
            <ActivityDashboard />
          </MemoryRouter>
        </AuthContext.Provider>
      );

      // Wait for cycle options to load
      expect(await screen.findByText(/Maha 2026 - Maha Kumbura/i)).toBeInTheDocument();

      // Select cycle 4
      const cycleSelect = screen.getByRole('combobox');
      fireEvent.change(cycleSelect, { target: { value: '4' } });

      // "Record Activity" link button should appear
      const recordBtn = screen.getByRole('link', { name: /Record Activity/i });
      expect(recordBtn).toBeInTheDocument();
      expect(recordBtn).toHaveAttribute('href', '/cycles/4/activities/new');
    });
  });
});
