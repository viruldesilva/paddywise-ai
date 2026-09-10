import type { UserRole } from '../types/auth';

export const ROLE_DASHBOARD_ROUTES: Record<UserRole, string> = {
  Farmer: '/dashboard/farmer',
  AgriculturalOfficer: '/dashboard/officer',
  Admin: '/dashboard/admin',
  FieldOfficer: '/dashboard/field-officer',
};

export function getRoleDashboardRoute(role?: string | null): string {
  if (!role) return '/dashboard';
  if (role in ROLE_DASHBOARD_ROUTES) {
    return ROLE_DASHBOARD_ROUTES[role as UserRole];
  }
  return '/dashboard';
}
