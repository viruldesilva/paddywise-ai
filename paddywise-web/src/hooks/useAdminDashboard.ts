import { useQuery } from '@tanstack/react-query';
import { adminService } from '../services/adminService';

export const ADMIN_DASHBOARD_QUERY_KEY = ['admin-dashboard'];
export const ADMIN_HEALTH_QUERY_KEY = ['admin-health'];

export function useAdminDashboard() {
  const healthQuery = useQuery({
    queryKey: ADMIN_HEALTH_QUERY_KEY,
    queryFn: () => adminService.getHealthCheck(),
    refetchInterval: 30_000,
    retry: 1,
  });

  const dashboardQuery = useQuery({
    queryKey: ADMIN_DASHBOARD_QUERY_KEY,
    queryFn: () => adminService.getDashboard(),
    staleTime: 30_000,
    retry: 1,
  });

  return {
    health: healthQuery.data,
    isHealthLoading: healthQuery.isLoading,
    isHealthError: healthQuery.isError,
    healthError: healthQuery.error,
    refetchHealth: healthQuery.refetch,

    dashboard: dashboardQuery.data,
    isDashboardLoading: dashboardQuery.isLoading,
    isDashboardError: dashboardQuery.isError,
    dashboardError: dashboardQuery.error,
    refetchDashboard: dashboardQuery.refetch,

    isLoading: healthQuery.isLoading || dashboardQuery.isLoading,
  };
}
