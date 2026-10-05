import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Sidebar } from '../components/Sidebar';
import { useAuth } from '../hooks/useAuth';
import { useAdminUsers } from '../hooks/useAdminUsers';
import type { AdminUserDto } from '../types/admin';
import {
  Menu,
  LogOut,
  Search,
  Plus,
  Filter,
  MoreVertical,
  Edit2,
  Trash2,
  UserCheck,
  AlertCircle,
  CheckCircle,
  Info,
  X,
  RefreshCw,
} from 'lucide-react';
import '../styles/Dashboard.css';
import '../styles/UserManagement.css';

interface ActionNotice {
  type: 'success' | 'error' | 'info';
  text: string;
}

export default function UserManagementPage() {
  const { user, logout } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [activeFilter, setActiveFilter] = useState('All');
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const [actionNotice, setActionNotice] = useState<ActionNotice | null>(null);

  // Debounce search input by 350ms
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchInput);
      setPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const {
    data,
    isLoading,
    isFetching,
    isError,
    error,
    refetch,
    deleteUser,
    isDeleting,
  } = useAdminUsers({
    search: debouncedSearch || undefined,
    role: activeFilter === 'All' ? undefined : activeFilter,
    page,
    pageSize,
  });

  if (!user) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: 'red' }}>
        <h2>Error: User context is null</h2>
        <p>The page cannot load because the user object is not available.</p>
      </div>
    );
  }

  const users = data?.items || [];
  const totalCount = data?.totalCount ?? 0;
  const startCount = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const endCount = Math.min(page * pageSize, totalCount);

  const getRoleBadgeClass = (role: string) => {
    switch (role) {
      case 'Farmer':
        return 'badge-farmer';
      case 'AgriculturalOfficer':
      case 'FieldOfficer':
        return 'badge-officer';
      case 'Admin':
        return 'badge-admin';
      default:
        return 'badge-farmer';
    }
  };

  const getRoleDisplayName = (role: string) => {
    switch (role) {
      case 'AgriculturalOfficer':
        return 'Agri. Officer';
      case 'FieldOfficer':
        return 'Field Officer';
      default:
        return role;
    }
  };

  const handleFilterChange = (newRole: string) => {
    setActiveFilter(newRole);
    setPage(1);
  };

  const handleDeleteUser = async (u: AdminUserDto) => {
    if (user.email.toLowerCase() === u.email.toLowerCase()) {
      setActionNotice({
        type: 'error',
        text: 'You cannot delete your own administrator account.',
      });
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete user "${u.fullName}" (${u.email})?\nThis action cannot be undone.`
    );
    if (!confirmed) return;

    try {
      const res = await deleteUser(u.id);
      setActionNotice({
        type: 'success',
        text: res.message || `User "${u.fullName}" was processed successfully.`,
      });
    } catch (err: unknown) {
      const errorMessage =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Failed to delete user. Please check your connection and try again.';
      setActionNotice({
        type: 'error',
        text: errorMessage,
      });
    }
  };

  const handleAddUser = () => {
    setActionNotice({
      type: 'info',
      text: 'User creation is currently pending backend endpoint implementation (POST /api/admin/users). Users may register through the public registration portal.',
    });
  };

  const handleEditUser = (u: AdminUserDto) => {
    setActionNotice({
      type: 'info',
      text: `Editing details for "${u.fullName}" is pending backend endpoint implementation (PUT /api/admin/users/${u.id}).`,
    });
  };

  const handleMoreOptions = (u: AdminUserDto) => {
    const formattedDate = new Date(u.createdAt).toLocaleString();
    setActionNotice({
      type: 'info',
      text: `User ID: #${u.id} | Registered: ${formattedDate} | Status: ${u.status} | Location: ${u.assignedLocation}`,
    });
  };

  return (
    <div className="dashboard-layout">
      <Sidebar role="Admin" isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      <div className="dashboard-main-wrapper">
        <header className="dashboard-header">
          <div className="container dashboard-header-inner">
            <div className="dashboard-header-title">
              <button
                className="mobile-menu-btn"
                onClick={() => setIsSidebarOpen(true)}
                aria-label="Open menu"
              >
                <Menu size={24} />
              </button>
              <h2 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--ink)' }}>
                User Management Console
              </h2>
            </div>

            <div className="dashboard-user-meta">
              <div className="dashboard-user-greeting">
                <span className="dashboard-user-name">{user.name}</span>
                <span className="dashboard-user-sub">{user.email}</span>
              </div>
              <span className="role-badge-tag badge-admin">Admin</span>
              <button
                onClick={() => logout()}
                className="btn btn-secondary btn-sm"
                title="Sign Out"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}
              >
                <LogOut size={16} />
                <span className="hide-mobile">Sign Out</span>
              </button>
            </div>
          </div>
        </header>

        <main className="dashboard-content container">
          <div className="um-header-actions">
            <div>
              <h1 className="dashboard-welcome-title">Manage System Users</h1>
              <p className="dashboard-welcome-desc">
                View, manage, and audit authenticated accounts across farmers, extension officers, and administrators.
              </p>
            </div>
            <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
              <Link
                to="/admin/officer-requests"
                className="btn btn-secondary"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem', textDecoration: 'none' }}
              >
                <UserCheck size={18} />
                <span>Officer Approvals</span>
              </Link>
              <button className="btn btn-primary" onClick={handleAddUser}>
                <Plus size={18} style={{ marginRight: '0.5rem' }} />
                Add New User
              </button>
            </div>
          </div>

          {/* Action Feedback Banner */}
          {actionNotice && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '0.875rem 1.25rem',
                borderRadius: '8px',
                marginBottom: '1.25rem',
                border: '1px solid',
                backgroundColor:
                  actionNotice.type === 'success'
                    ? 'rgba(46, 125, 50, 0.08)'
                    : actionNotice.type === 'error'
                    ? 'rgba(220, 38, 38, 0.08)'
                    : 'rgba(2, 132, 199, 0.08)',
                borderColor:
                  actionNotice.type === 'success'
                    ? '#2e7d32'
                    : actionNotice.type === 'error'
                    ? '#dc2626'
                    : '#0284c7',
                color:
                  actionNotice.type === 'success'
                    ? '#2e7d32'
                    : actionNotice.type === 'error'
                    ? '#dc2626'
                    : '#0369a1',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', fontSize: '0.9rem' }}>
                {actionNotice.type === 'success' && <CheckCircle size={18} />}
                {actionNotice.type === 'error' && <AlertCircle size={18} />}
                {actionNotice.type === 'info' && <Info size={18} />}
                <span>{actionNotice.text}</span>
              </div>
              <button
                onClick={() => setActionNotice(null)}
                style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'inherit', padding: '0.2rem' }}
                aria-label="Dismiss notice"
              >
                <X size={16} />
              </button>
            </div>
          )}

          <div className="dashboard-panel">
            <div className="um-toolbar">
              <div className="um-search-box">
                <Search size={18} className="um-search-icon" />
                <input
                  type="text"
                  placeholder="Search by name, email, or division..."
                  className="um-search-input"
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                />
              </div>
              <div className="um-filter-group">
                <Filter size={18} style={{ color: 'var(--ink-soft)' }} />
                <select
                  className="um-select"
                  value={activeFilter}
                  onChange={(e) => handleFilterChange(e.target.value)}
                >
                  <option value="All">All Roles</option>
                  <option value="Farmer">Farmers</option>
                  <option value="AgriculturalOfficer">Agricultural Officers</option>
                  <option value="FieldOfficer">Field Officers</option>
                  <option value="Admin">Admins</option>
                </select>
                <button
                  onClick={() => refetch()}
                  className="btn btn-secondary btn-sm"
                  style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem' }}
                  title="Reload users"
                >
                  <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
                  <span className="hide-mobile">Refresh</span>
                </button>
              </div>
            </div>

            <div className="user-table-wrapper">
              <table className="user-table">
                <thead>
                  <tr>
                    <th>Name / Email</th>
                    <th>Role</th>
                    <th>Assigned Location</th>
                    <th>Status</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {/* Loading State */}
                  {isLoading && (
                    <>
                      {[1, 2, 3, 4, 5].map((idx) => (
                        <tr key={idx} style={{ opacity: 0.6 }}>
                          <td>
                            <div style={{ height: '1rem', width: '140px', backgroundColor: 'var(--cream-deep)', borderRadius: '4px', marginBottom: '0.4rem' }}></div>
                            <div style={{ height: '0.75rem', width: '180px', backgroundColor: 'var(--cream-deep)', borderRadius: '4px' }}></div>
                          </td>
                          <td>
                            <div style={{ height: '1.25rem', width: '80px', backgroundColor: 'var(--cream-deep)', borderRadius: '12px' }}></div>
                          </td>
                          <td>
                            <div style={{ height: '1rem', width: '120px', backgroundColor: 'var(--cream-deep)', borderRadius: '4px' }}></div>
                          </td>
                          <td>
                            <div style={{ height: '1rem', width: '60px', backgroundColor: 'var(--cream-deep)', borderRadius: '4px' }}></div>
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <div style={{ height: '1.5rem', width: '70px', backgroundColor: 'var(--cream-deep)', borderRadius: '4px', marginLeft: 'auto' }}></div>
                          </td>
                        </tr>
                      ))}
                    </>
                  )}

                  {/* Error State */}
                  {!isLoading && isError && (
                    <tr>
                      <td colSpan={5} style={{ textAlign: 'center', padding: '2.5rem', color: '#dc2626' }}>
                        <AlertCircle size={24} style={{ margin: '0 auto 0.5rem auto' }} />
                        <div style={{ fontWeight: 600 }}>Failed to load users from backend</div>
                        <div style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>
                          {(error as Error)?.message || 'Please check your connection and try again.'}
                        </div>
                        <button
                          onClick={() => refetch()}
                          className="btn btn-secondary btn-sm"
                          style={{ marginTop: '0.75rem' }}
                        >
                          Retry
                        </button>
                      </td>
                    </tr>
                  )}

                  {/* Empty State */}
                  {!isLoading && !isError && users.length === 0 && (
                    <tr>
                      <td colSpan={5} style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--ink-soft)' }}>
                        <p style={{ fontSize: '1rem', fontWeight: 500, color: 'var(--ink)' }}>No users found</p>
                        <p style={{ fontSize: '0.875rem', marginTop: '0.25rem' }}>
                          No users match your search query &quot;{searchInput}&quot; and filter criteria.
                        </p>
                      </td>
                    </tr>
                  )}

                  {/* User Rows */}
                  {!isLoading && !isError && users.map((u) => {
                    const isSelf = user.email.toLowerCase() === u.email.toLowerCase();
                    const isApproved = u.status === 'Active';

                    return (
                      <tr key={u.id}>
                        <td>
                          <div style={{ fontWeight: 500, color: 'var(--ink)', display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                            {u.fullName}
                            {isSelf && (
                              <span
                                style={{
                                  fontSize: '0.7rem',
                                  padding: '0.1rem 0.4rem',
                                  borderRadius: '4px',
                                  backgroundColor: 'var(--cream-deep)',
                                  color: 'var(--ink-soft)',
                                  fontWeight: 600,
                                }}
                              >
                                You
                              </span>
                            )}
                          </div>
                          <div style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', marginTop: '0.2rem' }}>
                            {u.email}
                          </div>
                        </td>
                        <td>
                          <span className={`badge-outline ${getRoleBadgeClass(u.role)}`}>
                            {getRoleDisplayName(u.role)}
                          </span>
                        </td>
                        <td>{u.assignedLocation}</td>
                        <td>
                          <span
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '0.35rem',
                              color: isApproved ? '#2e7d32' : '#d97706',
                              fontSize: '0.875rem',
                              fontWeight: 500,
                            }}
                          >
                            <span
                              style={{
                                width: '8px',
                                height: '8px',
                                borderRadius: '50%',
                                backgroundColor: isApproved ? '#2e7d32' : '#d97706',
                              }}
                            ></span>
                            {u.status}
                          </span>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <div className="um-actions">
                            <button
                              className="um-action-btn"
                              title="Edit User"
                              onClick={() => handleEditUser(u)}
                            >
                              <Edit2 size={16} />
                            </button>
                            <button
                              className={`um-action-btn delete`}
                              title={isSelf ? 'Cannot delete your own admin account' : 'Delete User'}
                              disabled={isSelf || isDeleting}
                              onClick={() => handleDeleteUser(u)}
                              style={isSelf ? { opacity: 0.35, cursor: 'not-allowed' } : undefined}
                            >
                              <Trash2 size={16} />
                            </button>
                            <button
                              className="um-action-btn"
                              title="More Options"
                              onClick={() => handleMoreOptions(u)}
                            >
                              <MoreVertical size={16} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            <div className="um-pagination">
              <span className="um-pagination-info">
                Showing {startCount} - {endCount} of {totalCount} users
              </span>
              <div className="um-pagination-controls">
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1 || isFetching}
                >
                  Previous
                </button>
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={page * pageSize >= totalCount || isFetching}
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
