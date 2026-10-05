import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from '../ProtectedRoute';
import { AuthContext } from '../../context/AuthContext';
import type { AuthContextType } from '../../context/AuthContext';
import { tokenStorage } from '../../services/tokenStorage';

describe('ProtectedRoute RBAC Security Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  const renderRouteWithContext = (
    contextValue: Partial<AuthContextType>,
    targetUi: React.ReactNode,
    initialPath = '/protected'
  ) => {
    const fullContext: AuthContextType = {
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      clearAuth: vi.fn(),
      ...contextValue,
    };

    return render(
      <AuthContext.Provider value={fullContext}>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/login" element={<div>Login Page Target</div>} />
            <Route path="/dashboard" element={<div>Dashboard Fallback Target</div>} />
            <Route path="/protected" element={targetUi} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  it('redirects to /login when user has no stored token and is unauthenticated', () => {
    // Ensure no token in storage
    vi.spyOn(tokenStorage, 'getAccessToken').mockReturnValue(null);

    renderRouteWithContext(
      { isAuthenticated: false, user: null },
      <ProtectedRoute allowedRoles={['AgriculturalOfficer']}>
        <div>Officer Content</div>
      </ProtectedRoute>
    );

    expect(screen.getByText('Login Page Target')).toBeInTheDocument();
    expect(screen.queryByText('Officer Content')).not.toBeInTheDocument();
  });

  it('blocks Farmer-role user from accessing officer routes and redirects to /dashboard', () => {
    vi.spyOn(tokenStorage, 'getAccessToken').mockReturnValue('valid_token');

    renderRouteWithContext(
      {
        isAuthenticated: true,
        user: {
          id: 1,
          name: 'Farmer Bandara',
          email: 'farmer@kumburu.lk',
          role: 'Farmer',
        },
      },
      <ProtectedRoute allowedRoles={['AgriculturalOfficer', 'Admin']}>
        <div>Gated Officer Content</div>
      </ProtectedRoute>
    );

    // Should redirect to role / dashboard fallback
    expect(screen.getByText('Dashboard Fallback Target')).toBeInTheDocument();
    expect(screen.queryByText('Gated Officer Content')).not.toBeInTheDocument();
  });

  it('allows AgriculturalOfficer into officer routes but blocks from admin-only routes', () => {
    vi.spyOn(tokenStorage, 'getAccessToken').mockReturnValue('valid_token');

    // Case 1: Allowed into officer route
    const { unmount } = renderRouteWithContext(
      {
        isAuthenticated: true,
        user: {
          id: 2,
          name: 'Dr. Nilmini Perera',
          email: 'officer@kumburu.lk',
          role: 'AgriculturalOfficer',
        },
      },
      <ProtectedRoute allowedRoles={['AgriculturalOfficer']}>
        <div>Officer Secure Area</div>
      </ProtectedRoute>
    );

    expect(screen.getByText('Officer Secure Area')).toBeInTheDocument();
    unmount();

    // Case 2: Blocked from Admin-only route
    renderRouteWithContext(
      {
        isAuthenticated: true,
        user: {
          id: 2,
          name: 'Dr. Nilmini Perera',
          email: 'officer@kumburu.lk',
          role: 'AgriculturalOfficer',
        },
      },
      <ProtectedRoute allowedRoles={['Admin']}>
        <div>Admin Exclusive Area</div>
      </ProtectedRoute>
    );

    expect(screen.getByText('Dashboard Fallback Target')).toBeInTheDocument();
    expect(screen.queryByText('Admin Exclusive Area')).not.toBeInTheDocument();
  });

  it('allows Admin into admin routes but blocks from officer-only routes', () => {
    vi.spyOn(tokenStorage, 'getAccessToken').mockReturnValue('valid_token');

    // Case 1: Allowed into Admin route
    const { unmount } = renderRouteWithContext(
      {
        isAuthenticated: true,
        user: {
          id: 3,
          name: 'System Admin',
          email: 'admin@kumburu.lk',
          role: 'Admin',
        },
      },
      <ProtectedRoute allowedRoles={['Admin']}>
        <div>Admin Secure Area</div>
      </ProtectedRoute>
    );

    expect(screen.getByText('Admin Secure Area')).toBeInTheDocument();
    unmount();

    // Case 2: Blocked from Officer-only route
    renderRouteWithContext(
      {
        isAuthenticated: true,
        user: {
          id: 3,
          name: 'System Admin',
          email: 'admin@kumburu.lk',
          role: 'Admin',
        },
      },
      <ProtectedRoute allowedRoles={['AgriculturalOfficer']}>
        <div>Officer Exclusive Area</div>
      </ProtectedRoute>
    );

    expect(screen.getByText('Dashboard Fallback Target')).toBeInTheDocument();
    expect(screen.queryByText('Officer Exclusive Area')).not.toBeInTheDocument();
  });
});
