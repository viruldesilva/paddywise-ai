import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import App from '../../../App';
import { useAuth } from '../../../context/AuthContext';
import type { AuthContextType } from '../../../context/AuthContext';
import type { UserRole } from '../../../types/auth';
import { authAs } from './fixtures';

/**
 * The real App.tsx route table and the real ProtectedRoute. Only the auth state and the
 * page components are stubbed, so these tests prove which roles App.tsx lets through to
 * Component 1's pages — not what the pages render.
 */
vi.mock('../../../context/AuthContext', () => ({
  AuthProvider: ({ children }: { children: ReactNode }) => children,
  useAuth: vi.fn(),
}));
vi.mock('../../../services/tokenStorage', () => ({
  tokenStorage: { getAccessToken: () => 'test-token' },
}));
vi.mock('../../../hooks/useReveal', () => ({ useReveal: () => {} }));
vi.mock('../pages/DivisionFieldsPage', () => ({ default: () => <p>PAGE: division fields</p> }));
vi.mock('../pages/FieldDetailPage', () => ({ default: () => <p>PAGE: field detail</p> }));
vi.mock('../pages/CycleDetailPage', () => ({ default: () => <p>PAGE: cycle detail</p> }));
vi.mock('../pages/PlanApprovalPage', () => ({ default: () => <p>PAGE: plan approval</p> }));
vi.mock('../../../pages/DashboardPage', () => ({ default: () => <p>PAGE: dashboard</p> }));
vi.mock('../../auth/pages/LoginSelectionPage', () => ({ default: () => <p>PAGE: login</p> }));

function visit(path: string, auth: AuthContextType) {
  vi.mocked(useAuth).mockReturnValue(auth);
  window.history.pushState({}, '', path);
  render(<App />);
}

const signedOut: AuthContextType = { ...authAs('Farmer'), user: null, isAuthenticated: false, token: null };

describe('Component 1 routes in App.tsx', () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    ['/officer/fields', 'AgriculturalOfficer', 'PAGE: division fields'],
    ['/officer/fields', 'Admin', 'PAGE: division fields'],
    ['/fields/4', 'AgriculturalOfficer', 'PAGE: field detail'],
    ['/fields/4', 'Admin', 'PAGE: field detail'],
    ['/cycles/12', 'AgriculturalOfficer', 'PAGE: cycle detail'],
    ['/cycles/12', 'Admin', 'PAGE: cycle detail'],
    ['/plans/pending', 'AgriculturalOfficer', 'PAGE: plan approval'],
  ] as [string, UserRole, string][])('%s lets %s through', (path, role, page) => {
    visit(path, authAs(role));
    expect(screen.getByText(page)).toBeInTheDocument();
  });

  it.each([
    ['/officer/fields', 'Farmer'],
    ['/fields/4', 'Farmer'],
    ['/cycles/12', 'Farmer'],
    ['/plans/pending', 'Farmer'],
    ['/plans/pending', 'Admin'],
    ['/officer/fields', 'FieldOfficer'],
  ] as [string, UserRole][])('%s turns %s away to the dashboard', (path, role) => {
    visit(path, authAs(role));
    expect(screen.queryByText(/PAGE: (division|field detail|cycle|plan approval)/)).not.toBeInTheDocument();
    expect(screen.getByText('PAGE: dashboard')).toBeInTheDocument();
  });

  it('a signed-out visitor is sent to /login', () => {
    visit('/plans/pending', signedOut);
    expect(screen.getByText('PAGE: login')).toBeInTheDocument();
  });
});
