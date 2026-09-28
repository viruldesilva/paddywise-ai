import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Sidebar } from '../components/Sidebar';
import { useAuth } from '../hooks/useAuth';
import { Menu, LogOut, Search, Plus, Filter, MoreVertical, Edit2, Trash2, UserCheck } from 'lucide-react';
import '../styles/Dashboard.css';
import '../styles/UserManagement.css';

export default function UserManagementPage() {
  const { user, logout } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [activeFilter, setActiveFilter] = useState('All');

  if (!user) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: 'red' }}>
        <h2>Error: User context is null</h2>
        <p>The page cannot load because the user object is not available.</p>
      </div>
    );
  }
  // Mock users
  const mockUsers = [
    { id: 1, name: 'Bandara Wanninayake', email: 'bandara.w@example.com', role: 'Farmer', location: 'Medirigiriya', status: 'Active' },
    { id: 2, name: 'Kamal Perera', email: 'kamal.ag@example.com', role: 'AgriculturalOfficer', location: 'Polonnaruwa Central', status: 'Active' },
    { id: 3, name: 'Saman Kumara', email: 'saman.f@example.com', role: 'FieldOfficer', location: 'District Sector 4', status: 'Active' },
    { id: 4, name: 'Sunil Shantha', email: 'sunil.f@example.com', role: 'Farmer', location: 'Hingurakgoda', status: 'Inactive' },
    { id: 5, name: 'Ruwan Rajapaksha', email: 'ruwan.admin@example.com', role: 'Admin', location: 'System Console', status: 'Active' },
  ];

  const filteredUsers = activeFilter === 'All' ? mockUsers : mockUsers.filter(u => u.role === activeFilter);

  const getRoleBadgeClass = (role: string) => {
    switch (role) {
      case 'Farmer': return 'badge-farmer';
      case 'AgriculturalOfficer': return 'badge-officer';
      case 'FieldOfficer': return 'badge-officer';
      case 'Admin': return 'badge-admin';
      default: return 'badge-farmer';
    }
  };

  const getRoleDisplayName = (role: string) => {
    switch (role) {
      case 'AgriculturalOfficer': return 'Agri. Officer';
      case 'FieldOfficer': return 'Field Officer';
      default: return role;
    }
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
              <h2 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--ink)' }}>User Management Console</h2>
            </div>

            <div className="dashboard-user-meta">
              <div className="dashboard-user-greeting">
                <span className="dashboard-user-name">{user.name}</span>
                <span className="dashboard-user-sub">{user.email}</span>
              </div>
              <span className="role-badge-tag badge-admin">Admin</span>
              <button 
                onClick={logout} 
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
              <p className="dashboard-welcome-desc">View, edit, and control access for all farmers, officers, and administrators.</p>
            </div>
            <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
              <Link
                to="/admin/officer-requests"
                className="btn btn-secondary"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem', textDecoration: 'none' }}
              >
                <UserCheck size={18} />
                <span>Officer Approvals</span>
              </Link>
              <button className="btn btn-primary" onClick={() => alert('Add User UI modal would open here.')}>
                <Plus size={18} style={{ marginRight: '0.5rem' }} />
                Add New User
              </button>
            </div>
          </div>

          <div className="dashboard-panel">
            <div className="um-toolbar">
              <div className="um-search-box">
                <Search size={18} className="um-search-icon" />
                <input type="text" placeholder="Search by name, email, or location..." className="um-search-input" />
              </div>
              <div className="um-filter-group">
                <Filter size={18} style={{ color: 'var(--ink-soft)' }} />
                <select 
                  className="um-select"
                  value={activeFilter}
                  onChange={(e) => setActiveFilter(e.target.value)}
                >
                  <option value="All">All Roles</option>
                  <option value="Farmer">Farmers</option>
                  <option value="AgriculturalOfficer">Agricultural Officers</option>
                  <option value="FieldOfficer">Field Officers</option>
                  <option value="Admin">Admins</option>
                </select>
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
                  {filteredUsers.map((u) => (
                    <tr key={u.id}>
                      <td>
                        <div style={{ fontWeight: 500, color: 'var(--ink)' }}>{u.name}</div>
                        <div style={{ fontSize: '0.8125rem', color: 'var(--ink-soft)', marginTop: '0.2rem' }}>{u.email}</div>
                      </td>
                      <td>
                        <span className={`badge-outline ${getRoleBadgeClass(u.role)}`}>
                          {getRoleDisplayName(u.role)}
                        </span>
                      </td>
                      <td>{u.location}</td>
                      <td>
                        <span style={{ 
                          display: 'inline-flex', 
                          alignItems: 'center', 
                          gap: '0.3rem',
                          color: u.status === 'Active' ? '#2e7d32' : '#d97706',
                          fontSize: '0.875rem',
                          fontWeight: 500
                        }}>
                          <span style={{ 
                            width: '8px', 
                            height: '8px', 
                            borderRadius: '50%', 
                            backgroundColor: u.status === 'Active' ? '#2e7d32' : '#d97706' 
                          }}></span>
                          {u.status}
                        </span>
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <div className="um-actions">
                          <button className="um-action-btn" title="Edit User">
                            <Edit2 size={16} />
                          </button>
                          <button className="um-action-btn delete" title="Delete User">
                            <Trash2 size={16} />
                          </button>
                          <button className="um-action-btn" title="More Options">
                            <MoreVertical size={16} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="um-pagination">
              <span className="um-pagination-info">Showing {filteredUsers.length} of {mockUsers.length} users</span>
              <div className="um-pagination-controls">
                <button className="btn btn-secondary btn-sm" disabled>Previous</button>
                <button className="btn btn-secondary btn-sm" disabled>Next</button>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
