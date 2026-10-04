import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { OfficerApprovalsPage } from '../pages/OfficerApprovalsPage';
import { AuthContext, type AuthContextType } from '../../../context/AuthContext';
import { cropAnalysisApi } from '../services/cropAnalysisApi';

describe('OfficerApprovalsPage Page Tests', () => {
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

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders page layout with officer header, title, and recommendation queue', async () => {
    vi.spyOn(cropAnalysisApi, 'getPendingOfficerRecommendations').mockResolvedValueOnce([]);

    render(
      <AuthContext.Provider value={createAuthContext(mockOfficerUser)}>
        <MemoryRouter>
          <OfficerApprovalsPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );

    expect(screen.getByText('AGRICULTURAL EXTENSION & MONITORING')).toBeInTheDocument();
    expect(screen.getByText('AI Recommendation Approvals')).toBeInTheDocument();
    expect(screen.getByText('Dr. Nilmini Perera')).toBeInTheDocument();
    expect(screen.getByText('nilmini@gov.lk')).toBeInTheDocument();
    expect(await screen.findByText('No Recommendations In Queue')).toBeInTheDocument();
  });

  it('returns null when user is not authenticated', () => {
    const { container } = render(
      <AuthContext.Provider value={createAuthContext(null)}>
        <MemoryRouter>
          <OfficerApprovalsPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );

    expect(container.firstChild).toBeNull();
  });
});
