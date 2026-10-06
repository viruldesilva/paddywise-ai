import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import DashboardPage from '../../../pages/DashboardPage';
import { useAuth } from '../../../hooks/useAuth';
import type { AuthContextType } from '../../../context/AuthContext';
import type { UserRole } from '../../../types/auth';
// Reused unchanged from Component 1's web suite.
import { authAs } from '../../field-cultivation/__tests__/fixtures';
import { useOfficerDashboard } from '../hooks/useOfficerDashboard';
import { useAdminDashboard } from '../../../hooks/useAdminDashboard';

type OfficerHook = ReturnType<typeof useOfficerDashboard>;
type AdminHook = ReturnType<typeof useAdminDashboard>;

/** Backend figures deliberately different from the old hard-coded samples (42, 186, …). */
function officerHook(): OfficerHook {
  return {
    data: {
      assignedDivision: { id: 3, name: 'Hingurakgoda', centre: 'ASC Hingurakgoda', district: 'Polonnaruwa', province: 'North Central' },
      pendingApprovalCount: 7,
      approvedTreatmentsThisSeason: 13,
      cultivationSeason: 'Maha 2026',
      registeredFarmerCount: 58,
      gnDivisionCount: 4,
      pendingQueue: [
        {
          id: 91,
          farmerName: 'Kamala Dissanayake',
          gnDivision: 'Minneriya',
          symptom: 'Yellow leaf tips.',
          aiDiagnosis: 'Rice Blast',
          confidence: 0.81,
          proposedPlan: 'Officer to confirm with a field visit.',
          createdAt: '2026-10-05T05:00:00Z',
        },
      ],
    },
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
    approveTreatment: vi.fn(),
    requestDetails: vi.fn(),
  } as unknown as OfficerHook;
}

function adminHook(): AdminHook {
  return {
    health: { status: 'Healthy', database: 'Healthy', totalDurationMs: 12.4 },
    isHealthLoading: false,
    isHealthError: false,
    dashboard: {
      totalUsers: 23,
      usersPerRole: { Farmer: 17, AgriculturalOfficer: 3, FieldOfficer: 2, Admin: 1 },
      activeUsers: 21,
      inactiveUsers: 2,
      pendingOfficerApprovals: 4,
    },
    isDashboardLoading: false,
    refetchHealth: vi.fn(),
    refetchDashboard: vi.fn(),
  } as unknown as AdminHook;
}

vi.mock('../../../hooks/useAuth', () => ({ useAuth: vi.fn() }));
vi.mock('../../../components/Sidebar', () => ({ Sidebar: ({ role }: { role: string }) => <nav>SIDEBAR: {role}</nav> }));
// Since the deploy merge the officer and admin dashboards load live data through React Query
// hooks. The hooks are mocked (not the views), so the real views render that data.
vi.mock('../hooks/useOfficerDashboard', () => ({ useOfficerDashboard: vi.fn() }));
vi.mock('../../../hooks/useAdminDashboard', () => ({ useAdminDashboard: vi.fn() }));

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
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useOfficerDashboard).mockReturnValue(officerHook());
    vi.mocked(useAdminDashboard).mockReturnValue(adminHook());
  });

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

  // PendingPlansCard (Component 1) is no longer rendered on any dashboard since the deploy
  // merge — the officer view replaced it. These check which live view each role gets.
  it.each([
    ['AgriculturalOfficer', 'Pending Treatment Approval Queue', true],
    ['FieldOfficer', 'Pending Treatment Approval Queue', false],
    ['Admin', 'Healthy (Ready)', true],
    ['Farmer', 'Healthy (Ready)', false],
  ] as [UserRole, string, boolean][])('%s → shows "%s": %s', (role, marker, shown) => {
    renderAs(role);
    expect(screen.queryByText(marker) !== null).toBe(shown);
  });

  it('the officer dashboard shows the figures and queue from the backend', () => {
    renderAs('AgriculturalOfficer');

    expect(screen.getByText('Hingurakgoda')).toBeInTheDocument();
    expect(screen.getByText('7')).toBeInTheDocument();
    expect(screen.getByText('13')).toBeInTheDocument();
    expect(screen.getByText('58')).toBeInTheDocument();
    expect(screen.getByText('Farmer: Kamala Dissanayake')).toBeInTheDocument();
    expect(screen.getByText(/Rice Blast with 81% confidence/)).toBeInTheDocument();
  });

  it('the admin dashboard shows health and user counts from the backend', () => {
    renderAs('Admin');

    expect(screen.getByText('Healthy (Ready)')).toBeInTheDocument();
    expect(screen.getByText('12ms probe latency')).toBeInTheDocument();
    expect(screen.getByText('23 Users')).toBeInTheDocument();
    expect(screen.getByText('4 Pending')).toBeInTheDocument();
  });

  it.each([
    ['AgriculturalOfficer', ['42', '186', 'Bandara Wanninayake', 'Thiamethoxam 25% WG', 'Polonnaruwa Central']],
  ] as [UserRole, string[]][])(
    // Finding 8, fixed by the deploy merge for these two roles.
    '%s no longer shows the old hard-coded samples',
    (role, constants) => {
      renderAs(role);
      for (const constant of constants) {
        expect(screen.queryByText((text) => text === constant || text.includes(constant))).not.toBeInTheDocument();
      }
    }
  );

  it('the admin status follows the real health check — no longer always "Ready" (finding 8, fixed)', () => {
    vi.mocked(useAdminDashboard).mockReturnValue({ ...adminHook(), health: undefined, isHealthError: true } as AdminHook);
    renderAs('Admin');

    expect(screen.getByText('Unavailable')).toBeInTheDocument();
    expect(screen.getByText('Database connection error')).toBeInTheDocument();
    expect(screen.queryByText('Healthy (Ready)')).not.toBeInTheDocument();
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
    ['FieldOfficer', ['5 Today', '89', 'Paddy Tract #14 - Jayanthipura']],
    ['Farmer', ['Yala 2026', '3.5 Acres', '14.0 MT']],
  ] as [UserRole, string[]][])(
    // Known bug (finding 8, partly fixed): the Farmer and FieldOfficer dashboards are still hard-coded
    // constants. The AgriculturalOfficer and Admin dashboards now load live data (tested above).
    '%s sees figures from the backend, not hard-coded samples',
    (role, constants) => {
      renderAs(role);
      for (const constant of constants) {
        expect(screen.queryByText((text) => text.includes(constant))).not.toBeInTheDocument();
      }
    }
  );
});
