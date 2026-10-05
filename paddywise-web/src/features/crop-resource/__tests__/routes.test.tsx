import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import App from '../../../App';
import { useAuth } from '../../../context/AuthContext';
import type { UserRole } from '../../../types/auth';
// Reused unchanged from Component 1's web suite.
import { authAs } from '../../field-cultivation/__tests__/fixtures';

/** The real App.tsx route table and ProtectedRoute for Component 2's officer pages. */
vi.mock('../../../context/AuthContext', () => ({
  AuthProvider: ({ children }: { children: ReactNode }) => children,
  useAuth: vi.fn(),
}));
vi.mock('../../../services/tokenStorage', () => ({ tokenStorage: { getAccessToken: () => 'test-token' } }));
vi.mock('../../../hooks/useReveal', () => ({ useReveal: () => {} }));
vi.mock('../pages/OfficerApprovalsPage', () => ({ OfficerApprovalsPage: () => <p>PAGE: approvals</p> }));
vi.mock('../pages/OfficerActivityReportPage', () => ({ OfficerActivityReportPage: () => <p>PAGE: activity report</p> }));
vi.mock('../../../pages/DashboardPage', () => ({ default: () => <p>PAGE: dashboard</p> }));

function visit(path: string, role: UserRole) {
  vi.mocked(useAuth).mockReturnValue(authAs(role));
  window.history.pushState({}, '', path);
  render(<App />);
}

describe('Component 2 officer routes in App.tsx', () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    ['/officer/approvals', 'AgriculturalOfficer', 'PAGE: approvals'],
    ['/officer/approvals', 'FieldOfficer', 'PAGE: approvals'],
    ['/officer/reports', 'AgriculturalOfficer', 'PAGE: activity report'],
    ['/officer/reports', 'FieldOfficer', 'PAGE: activity report'],
    ['/officer/reports', 'Admin', 'PAGE: activity report'],
  ] as [string, UserRole, string][])('%s lets %s through', (path, role, page) => {
    visit(path, role);
    expect(screen.getByText(page)).toBeInTheDocument();
  });

  it.each([
    ['/officer/approvals', 'Farmer'],
    ['/officer/reports', 'Farmer'],
    ['/officer/approvals', 'Admin'], // Admin was removed from this route in the deploy merge
  ] as [string, UserRole][])('%s sends a %s to the dashboard', (path, role) => {
    visit(path, role);
    expect(screen.queryByText(/PAGE: (approvals|activity report)/)).not.toBeInTheDocument();
    expect(screen.getByText('PAGE: dashboard')).toBeInTheDocument();
  });
});
