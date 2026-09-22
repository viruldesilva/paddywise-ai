import { Link, useLocation } from 'react-router-dom';
import type { UserRole } from '../types/auth';
import { useAuth } from '../hooks/useAuth';
import './Navbar.css';
import logoImage from '../assets/logo2.png';
import {
  LayoutDashboard,
  User,
  Map,
  Activity,
  Bug,
  Cloud,
  Brain,
  FileText,
  CheckSquare,
  MessageSquare,
  BarChart2,
  Users,
  Database,
  Settings,
  MapPin,
  Camera,
  CheckCircle,
  X,
  LogOut
} from 'lucide-react';
import '../styles/Sidebar.css';

interface SidebarProps {
  role: UserRole;
  isOpen: boolean;
  onClose: () => void;
}

export function Sidebar({ role, isOpen, onClose }: SidebarProps) {
  const location = useLocation();
  const { logout } = useAuth();

  const farmerLinks = [
    { name: 'Dashboard', path: '/dashboard', icon: LayoutDashboard },
    { name: 'Manage Profile', path: '#profile', icon: User },
    { name: 'Fields & Cultivation Cycles', path: '/fields', icon: Map },
    { name: 'Record Activities', path: '/activities', icon: Activity },
    { name: 'Report Pests/Diseases', path: '#report', icon: Bug },
    { name: 'Weather & History', path: '#weather', icon: Cloud },
    { name: 'AI Analysis & Recommendations', path: '#ai', icon: Brain },
  ];

  const agriculturalOfficerLinks = [
    { name: 'Dashboard', path: '/dashboard', icon: LayoutDashboard },
    { name: 'Past Crop Activities', path: '/activities', icon: Activity },
    { name: 'View Farmer Fields & Reports', path: '#fields', icon: FileText },
    { name: 'Review AI Recommendations', path: '/plans/pending', icon: CheckSquare },
    { name: 'Add Expert Recommendations', path: '#expert', icon: MessageSquare },
    { name: 'Monitor Disease & Statistics', path: '#stats', icon: BarChart2 },
  ];

  const adminLinks = [
    { name: 'Dashboard', path: '/dashboard/admin', icon: LayoutDashboard },
    { name: 'Past Crop Activities', path: '/activities', icon: Activity },
    { name: 'Manage Users', path: '/admin/users', icon: Users },
    { name: 'Manage Knowledge Base', path: '#knowledge', icon: Database },
    { name: 'System Settings & Audit Logs', path: '#settings', icon: Settings },
  ];

  const fieldOfficerLinks = [
    { name: 'Dashboard', path: '/dashboard', icon: LayoutDashboard },
    { name: 'Past Crop Activities', path: '/activities', icon: Activity },
    { name: 'Visit Farms & Inspections', path: '#inspections', icon: MapPin },
    { name: 'Upload Field Images', path: '#upload', icon: Camera },
    { name: 'Verify Problems & Feedback', path: '#feedback', icon: CheckCircle },
  ];

  let links = farmerLinks;
  if (role === 'AgriculturalOfficer') links = agriculturalOfficerLinks;
  if (role === 'Admin') links = adminLinks;
  if (role === 'FieldOfficer') links = fieldOfficerLinks;

  return (
    <>
      {/* Mobile backdrop */}
      {isOpen && (
        <div className="sidebar-backdrop" onClick={onClose}></div>
      )}

      <aside className={`sidebar ${isOpen ? 'open' : ''}`}>
        <div className="sidebar-brand">
          <div className="navbar-left">
            <Link to="/" className="navbar-brand">
              <img src={logoImage} alt="Kumburu Logo" className="navbar-logo-img" />
            </Link>
          </div>

          <button className="sidebar-close-btn" onClick={onClose} aria-label="Close menu">
            <X size={24} />
          </button>
        </div>

        <nav className="sidebar-nav">
          <ul>
            {links.map((link, index) => {
              const Icon = link.icon;
              const isActive = location.pathname === link.path;

              return (
                <li key={index}>
                  <Link
                    to={link.path}
                    className={`sidebar-link ${isActive ? 'active' : ''}`}
                  >
                    <Icon size={20} className="sidebar-icon" />
                    <span>{link.name}</span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>

        <div className="sidebar-footer">
          <button
            type="button"
            onClick={logout}
            className="sidebar-logout-btn"
            title="Sign Out"
          >
            <LogOut size={18} className="sidebar-logout-icon" />
            <span>Sign Out</span>
          </button>

          <div className="sidebar-help">
            <h4>Need Help?</h4>
            <p>Contact support for assistance.</p>
          </div>
        </div>
      </aside>
    </>
  );
}
