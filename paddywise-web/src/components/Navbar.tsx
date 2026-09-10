import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Menu, X, LogOut, LayoutDashboard } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import './Navbar.css';
import logoImage from '../assets/logo2.png';

export default function Navbar() {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const toggleMenu = () => setIsMobileMenuOpen(!isMobileMenuOpen);

  const handleLogout = async () => {
    await logout();
    setIsMobileMenuOpen(false);
    navigate('/');
  };

  return (
    <header className="navbar">
      <div className="container navbar-container">
        <div className="navbar-left">
          <Link to="/" className="navbar-brand">
            <img src={logoImage} alt="Kumburu Logo" className="navbar-logo-img" />
          </Link>
        </div>

        <nav className="navbar-desktop">
          <ul className="navbar-links">
            <li><a href="/#features">Components</a></li>
            <li><a href="/#workflow">How it works</a></li>
            <li><a href="/#roles">Roles</a></li>
            <li><a href="/#contact">Contact</a></li>
          </ul>
        </nav>

        <div className="navbar-right">
          {isAuthenticated && user ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <Link
                to="/dashboard"
                className="btn btn-primary"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem', padding: '0.5rem 1rem', fontSize: '0.875rem' }}
              >
                <LayoutDashboard size={16} />
                Dashboard ({user.name.split(' ')[0]})
              </Link>
              <button
                onClick={handleLogout}
                className="navbar-link-btn"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.3rem', cursor: 'pointer' }}
                title="Sign Out"
              >
                <LogOut size={16} />
                Sign out
              </button>
            </div>
          ) : (
            <>
              <Link to="/login" className="navbar-link-btn">Sign in</Link>
              <Link to="/register" className="btn btn-primary">Request access</Link>
            </>
          )}
        </div>

        <button
          className="mobile-menu-btn"
          onClick={toggleMenu}
          aria-expanded={isMobileMenuOpen}
          aria-label="Toggle menu"
        >
          {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
        </button>
      </div>

      {isMobileMenuOpen && (
        <div className="mobile-panel">
          <nav>
            <ul className="mobile-links">
              <li><a href="/#features" onClick={toggleMenu}>Components</a></li>
              <li><a href="/#workflow" onClick={toggleMenu}>How it works</a></li>
              <li><a href="/#roles" onClick={toggleMenu}>Roles</a></li>
              <li><a href="/#contact" onClick={toggleMenu}>Contact</a></li>
              {isAuthenticated && user ? (
                <>
                  <li>
                    <Link to="/dashboard" onClick={toggleMenu} style={{ fontWeight: 600 }}>
                      Dashboard ({user.name})
                    </Link>
                  </li>
                  <li>
                    <button onClick={handleLogout} style={{ color: '#ef4444', textAlign: 'left', padding: 0 }}>
                      Sign Out
                    </button>
                  </li>
                </>
              ) : (
                <>
                  <li><Link to="/login" onClick={toggleMenu}>Sign in</Link></li>
                  <li><Link to="/register" className="btn btn-primary" onClick={toggleMenu}>Request access</Link></li>
                </>
              )}
            </ul>
          </nav>
        </div>
      )}
    </header>
  );
}
