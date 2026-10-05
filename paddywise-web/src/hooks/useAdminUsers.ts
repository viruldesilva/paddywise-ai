import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { adminService } from '../services/adminService';
import type { GetAdminUsersParams } from '../types/admin';
import { ADMIN_DASHBOARD_QUERY_KEY } from './useAdminDashboard';

export const ADMIN_USERS_QUERY_KEY = ['admin-users'];

export function useAdminUsers(params: GetAdminUsersParams) {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: [...ADMIN_USERS_QUERY_KEY, params],
    queryFn: () => adminService.getUsers(params),
    placeholderData: keepPreviousData,
    staleTime: 10_000,
  });

  const deleteMutation = useMutation({
    mutationFn: (userId: number) => adminService.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ADMIN_USERS_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: ADMIN_DASHBOARD_QUERY_KEY });
    },
  });

  return {
    ...query,
    deleteUser: deleteMutation.mutateAsync,
    isDeleting: deleteMutation.isPending,
  };
}
