import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import DashboardPage from '../../../pages/DashboardPage';
import { useAuth } from '../../../hooks/useAuth';
import type { AuthContextType } from '../../../context/AuthContext';
import type { UserRole } from '../../../types/auth';
// Reused unchanged from Component 1's web suite.
import { authAs } from '../../field-cultivation/__tests__/fixtures';

vi.mock('../../../hooks/useAuth', () => ({ useAuth: vi.fn() }));
vi.mock('../../../components/Sidebar', () => ({ Sidebar: ({ role }: { role: string }) => <nav>SIDEBAR: {role}</nav> }));
vi.mock('../../field-cultivation/components/PendingPlansCard', () => ({ PendingPlansCard: () => <p>PENDING PLANS CARD</p> }));

function renderAs(role: UserRole, roleView?: UserRole) {
  const auth: AuthContextType = { ...authAs(role), user: { name: 'Nimal Perera', email: 'nimal@paddywise.lk', role } };
  vi.mocked(useAuth).mockReturnValue(auth);
  render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <Routes>
        <Route path="/dashboard" element={<DashboardPage roleView={roleView} />} />
        <Route path="/activities" element={<p>PAGE: activities</p>} />
      </Routes>
    </MemoryRouter>
  );
  return auth;
}

/** Component 4's role dashboard. Virul's web suite has no test for it. */
describe('DashboardPage', () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    ['Farmer', 'FIELD DECISION WORKSPACE', 'Ayubowan, Nimal!', 'Farmer'],
    ['AgriculturalOfficer', 'AGRICULTURAL EXTENSION DIVISION', 'Officer Workspace: Nimal Perera', 'Agricultural Officer'],
    ['FieldOfficer', 'FIELD TECHNICAL & EXTENSION MONITORING', 'Field Officer Hub: Nimal Perera', 'Field Officer'],
    ['Admin', 'SYSTEM ADMINISTRATION & PLATFORM CONSOLE', 'System Administration Console', 'Admin'],
  ] as [UserRole, string, string, string][])('%s sees its own workspace', (role, eyebrow, title, badge) => {
    renderAs(role);

    expect(screen.getByText(eyebrow)).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 1, name: title })).toBeInTheDocument();
    expect(screen.getByText(badge, { selector: '.role-badge-tag' })).toBeInTheDocument();
    expect(screen.getByText(`SIDEBAR: ${role}`)).toBeInTheDocument();
  });

  it('roleView (the /dashboard/<role> routes) overrides the signed-in role', () => {
    renderAs('Admin', 'Farmer');

    expect(screen.getByText('FIELD DECISION WORKSPACE')).toBeInTheDocument();
    expect(screen.queryByText('System Administration Console')).not.toBeInTheDocument();
    expect(screen.getByText('SIDEBAR: Farmer')).toBeInTheDocument();
  });

  it.each([
    ['AgriculturalOfficer', true],
    ['FieldOfficer', false],
    ['Admin', false],
    ['Farmer', false],
  ] as [UserRole, boolean][])('the live pending-plans card shows only for %s → %s', (role, shown) => {
    renderAs(role);
    expect(screen.queryByText('PENDING PLANS CARD') !== null).toBe(shown);
  });

  it('Sign Out calls logout', () => {
    const auth = renderAs('AgriculturalOfficer');
    fireEvent.click(screen.getByRole('button', { name: /sign out/i }));
    expect(auth.logout).toHaveBeenCalledTimes(1);
  });

  it('the farmer workspace links to crop activities', () => {
    renderAs('Farmer');
    fireEvent.click(screen.getByRole('button', { name: 'Manage Crop Activities' }));
    expect(screen.getByText('PAGE: activities')).toBeInTheDocument();
  });

  it('renders nothing without a signed-in user', () => {
    vi.mocked(useAuth).mockReturnValue({ ...authAs('Farmer'), user: null, isAuthenticated: false });
    const { container } = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    );
    expect(container).toBeEmptyDOMElement();
  });

  it.skip.each([
    ['AgriculturalOfficer', ['42', '186', 'Bandara Wanninayake', 'Thiamethoxam 25% WG']],
    ['FieldOfficer', ['5 Today', '89', 'Paddy Tract #14 - Jayanthipura']],
    ['Farmer', ['Yala 2026', '3.5 Acres', '14.0 MT']],
    ['Admin', ['Ready']],
  ] as [UserRole, string[]][])(
    // Known bug (finding 8): every dashboard figure except PendingPlansCard is a hard-coded constant —
    // the same counts, sample farmer, pesticide plan and "Backend Status: Ready" whatever the data.
    '%s sees figures from the backend, not hard-coded samples',
    (role, constants) => {
      renderAs(role);
      for (const constant of constants) {
        expect(screen.queryByText((text) => text.includes(constant))).not.toBeInTheDocument();
      }
    }
  );
});
