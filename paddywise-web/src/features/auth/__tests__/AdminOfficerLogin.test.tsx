import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import AdminLoginPage from '../pages/AdminLoginPage';
import OfficerLoginPage from '../pages/OfficerLoginPage';
import { AuthContext } from '../../../context/AuthContext';
import type { AuthContextType } from '../../../context/AuthContext';
import type { LoginResponse } from '../../../types/auth';

describe('AdminLoginPage & OfficerLoginPage Tests', () => {
  let mockLogin: ReturnType<typeof vi.fn>;
  let mockClearAuth: ReturnType<typeof vi.fn>;
  let mockAuthContext: AuthContextType;

  beforeEach(() => {
    mockLogin = vi.fn();
    mockClearAuth = vi.fn();

    mockAuthContext = {
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
      login: mockLogin,
      register: vi.fn(),
      logout: vi.fn(),
      clearAuth: mockClearAuth,
    };
  });

  const renderWithAuth = (ui: React.ReactElement, initialRoute = '/login/admin') => {
    return render(
      <AuthContext.Provider value={mockAuthContext}>
        <MemoryRouter initialEntries={[initialRoute]}>
          <Routes>
            <Route path="/login/admin" element={<AdminLoginPage />} />
            <Route path="/login/officer" element={<OfficerLoginPage />} />
            <Route path="/dashboard/admin" element={<div>Admin Dashboard Target</div>} />
            <Route path="/dashboard/officer" element={<div>Officer Dashboard Target</div>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  describe('Component Rendering', () => {
    it('AdminLoginPage renders portal title, badge, inputs, and submit button', () => {
      renderWithAuth(<AdminLoginPage />, '/login/admin');

      expect(screen.getByRole('heading', { name: /Administrator Sign in/i })).toBeInTheDocument();
      expect(screen.getByText('System Administration')).toBeInTheDocument();
      expect(screen.getByLabelText(/Email Address/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Sign In/i })).toBeInTheDocument();
      expect(screen.getByText('Agricultural Officer Login')).toBeInTheDocument();
    });

    it('OfficerLoginPage renders portal title, badge, inputs, and submit button', () => {
      renderWithAuth(<OfficerLoginPage />, '/login/officer');

      expect(screen.getByRole('heading', { name: /Agricultural Officer Sign in/i })).toBeInTheDocument();
      expect(screen.getByText('Agricultural Officer Portal')).toBeInTheDocument();
      expect(screen.getByLabelText(/Email Address/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Sign In/i })).toBeInTheDocument();
      expect(screen.getByText('Administrator Login')).toBeInTheDocument();
    });
  });

  describe('Form Validation Testing', () => {
    it('shows inline validation error when submitting with empty email or password', async () => {
      renderWithAuth(<AdminLoginPage />, '/login/admin');

      const submitButton = screen.getByRole('button', { name: /Sign In/i });
      fireEvent.click(submitButton);

      expect(
        await screen.findByText('Please fill in both your email address and password.')
      ).toBeInTheDocument();
      expect(mockLogin).not.toHaveBeenCalled();
    });
  });

  describe('Client-Side Role Verification Gate (API Integration)', () => {
    it('AdminLoginPage: on login success with role != Admin, displays rejection message and does NOT redirect', async () => {
      // Mock successful login response, but with AgriculturalOfficer role instead of Admin
      const nonAdminResponse: LoginResponse = {
        token: 'valid_jwt_token',
        email: 'officer@paddywise.lk',
        name: 'Dr. Officer',
        role: 'AgriculturalOfficer',
      };
      mockLogin.mockResolvedValueOnce(nonAdminResponse);

      renderWithAuth(<AdminLoginPage />, '/login/admin');

      fireEvent.change(screen.getByLabelText(/Email Address/i), {
        target: { value: 'officer@paddywise.lk' },
      });
      fireEvent.change(screen.getByLabelText(/Password/i), {
        target: { value: 'Password123!' },
      });

      fireEvent.click(screen.getByRole('button', { name: /Sign In/i }));

      // Expect strict client-side role check error message
      expect(
        await screen.findByText(
          'This account is not an Admin account. Please use the Agricultural Officer login.'
        )
      ).toBeInTheDocument();

      // Ensure session was cleared immediately
      expect(mockClearAuth).toHaveBeenCalledTimes(1);

      // Ensure navigation did NOT redirect to admin dashboard
      expect(screen.queryByText('Admin Dashboard Target')).not.toBeInTheDocument();
    });

    it('OfficerLoginPage: on login attempt against a PendingApproval account, displays 403 pending verification message', async () => {
      // Simulate backend 403 Forbidden with exact pending approval message
      const pendingError = {
        response: {
          status: 403,
          data: {
            message: 'Your account is pending admin verification. Please check back later.',
          },
        },
      };
      mockLogin.mockRejectedValueOnce(pendingError);

      renderWithAuth(<OfficerLoginPage />, '/login/officer');

      fireEvent.change(screen.getByLabelText(/Email Address/i), {
        target: { value: 'pending_officer@paddywise.lk' },
      });
      fireEvent.change(screen.getByLabelText(/Password/i), {
        target: { value: 'Password123!' },
      });

      fireEvent.click(screen.getByRole('button', { name: /Sign In/i }));

      // Expect exact 403 verification message
      expect(
        await screen.findByText(
          'Your account is pending admin verification. Please check back later.'
        )
      ).toBeInTheDocument();

      // Ensure navigation did not redirect
      expect(screen.queryByText('Officer Dashboard Target')).not.toBeInTheDocument();
    });

    it('OfficerLoginPage: successful login with AgriculturalOfficer role redirects to officer dashboard', async () => {
      const validOfficerResponse: LoginResponse = {
        token: 'valid_jwt_token',
        email: 'officer@paddywise.lk',
        name: 'Dr. Officer',
        role: 'AgriculturalOfficer',
      };
      mockLogin.mockResolvedValueOnce(validOfficerResponse);

      renderWithAuth(<OfficerLoginPage />, '/login/officer');

      fireEvent.change(screen.getByLabelText(/Email Address/i), {
        target: { value: 'officer@paddywise.lk' },
      });
      fireEvent.change(screen.getByLabelText(/Password/i), {
        target: { value: 'Password123!' },
      });

      fireEvent.click(screen.getByRole('button', { name: /Sign In/i }));

      await waitFor(() => {
        expect(screen.getByText('Officer Dashboard Target')).toBeInTheDocument();
      });
    });
  });
});
