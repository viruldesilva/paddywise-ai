import { useState } from 'react';
import { Menu, X } from 'lucide-react';
import './Navbar.css';

export default function Navbar() {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const toggleMenu = () => setIsMobileMenuOpen(!isMobileMenuOpen);

  return (
    <header className="navbar">
      <div className="container navbar-container">
        <div className="navbar-left">
          <a href="#" className="navbar-brand">
            <svg
              className="navbar-logo"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M12 22v-8" />
              <path d="M12 14c-3-2-6-3-6-6 0-3 3-4 6-4" />
              <path d="M12 14c3-2 6-3 6-6 0-3-3-4-6-4" />
              <path d="M12 4v10" />
            </svg>
            <div className="navbar-title-group">
              <span className="navbar-title">Kumburu</span>
              <span className="navbar-subtitle">Paddy field, connected</span>
            </div>
          </a>
        </div>

        <nav className="navbar-desktop">
          <ul className="navbar-links">
            <li><a href="#features">Components</a></li>
            <li><a href="#workflow">How it works</a></li>
            <li><a href="#roles">Roles</a></li>
            <li><a href="#contact">Contact</a></li>
          </ul>
        </nav>

        <div className="navbar-right">
          <a href="#signin" className="navbar-link-btn">Sign in</a>
          <a href="#request-access" className="btn btn-primary">Request access</a>
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
              <li><a href="#features" onClick={toggleMenu}>Components</a></li>
              <li><a href="#workflow" onClick={toggleMenu}>How it works</a></li>
              <li><a href="#roles" onClick={toggleMenu}>Roles</a></li>
              <li><a href="#contact" onClick={toggleMenu}>Contact</a></li>
              <li><a href="#signin" onClick={toggleMenu}>Sign in</a></li>
              <li><a href="#request-access" className="btn btn-primary" onClick={toggleMenu}>Request access</a></li>
            </ul>
          </nav>
        </div>
      )}
    </header>
  );
}
