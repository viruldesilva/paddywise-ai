import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getOfficerDashboard,
  approveTreatment,
  requestDetailsTreatment,
} from '../services/officerDashboardApi';

export const OFFICER_DASHBOARD_QUERY_KEY = ['officer-dashboard'];

export function useOfficerDashboard() {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: OFFICER_DASHBOARD_QUERY_KEY,
    queryFn: getOfficerDashboard,
    staleTime: 30_000,
  });

  const approveMutation = useMutation({
    mutationFn: (reportId: number) => approveTreatment(reportId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: OFFICER_DASHBOARD_QUERY_KEY });
    },
  });

  const requestDetailsMutation = useMutation({
    mutationFn: ({ reportId, comment }: { reportId: number; comment: string }) =>
      requestDetailsTreatment(reportId, comment),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: OFFICER_DASHBOARD_QUERY_KEY });
    },
  });

  return {
    ...query,
    approveTreatment: approveMutation.mutateAsync,
    isApproving: approveMutation.isPending,
    requestDetails: requestDetailsMutation.mutateAsync,
    isRequestingDetails: requestDetailsMutation.isPending,
    invalidateDashboard: () =>
      queryClient.invalidateQueries({ queryKey: OFFICER_DASHBOARD_QUERY_KEY }),
  };
}
