import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { OfficerActivityReportPage } from '../pages/OfficerActivityReportPage';
import { AuthContext, type AuthContextType } from '../../../context/AuthContext';
import { activityApi, type CropActivityDto } from '../services/activityApi';

describe('OfficerActivityReportPage Page Tests', () => {
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
    }
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders report generation layout and fetches all activities on mount', async () => {
    const fetchSpy = vi.spyOn(activityApi, 'getAllActivities').mockResolvedValueOnce(sampleActivities);

    render(
      <AuthContext.Provider value={createAuthContext(mockOfficerUser)}>
        <MemoryRouter>
          <OfficerActivityReportPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );

    expect(fetchSpy).toHaveBeenCalled();
    expect(screen.getByText('AGRICULTURAL EXTENSION & MONITORING')).toBeInTheDocument();
    expect(screen.getByText('Crop Activities Agronomic Report')).toBeInTheDocument();
    expect(
      screen.getByText(/Generate filtered, printable audit reports across farmers/i)
    ).toBeInTheDocument();
    expect(screen.getByText('Dr. Nilmini Perera')).toBeInTheDocument();
  });

  it('returns null when user is unauthenticated', () => {
    const { container } = render(
      <AuthContext.Provider value={createAuthContext(null)}>
        <MemoryRouter>
          <OfficerActivityReportPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );

    expect(container.firstChild).toBeNull();
  });
});
