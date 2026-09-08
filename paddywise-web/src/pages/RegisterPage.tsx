import { Link } from 'react-router-dom';
import { Mail, Lock, User, Briefcase } from 'lucide-react';
import '../styles/Auth.css';

export default function RegisterPage() {
  return (
    <div className="auth-layout">
      {/* Left visual panel (hidden on mobile) */}
      <div className="auth-visual">
        <Link to="/" className="auth-visual-logo">
          <svg
            width="24"
            height="24"
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
          Kumburu
        </Link>
        
        <div className="auth-visual-content">
          <h2 className="auth-quote">
            Ready to see a <br />
            <span className="auth-quote-highlight">season end differently?</span>
          </h2>
        </div>
      </div>

      {/* Right form panel */}
      <div className="auth-form-container">
        <div className="auth-form-wrapper">
          <Link to="/" className="auth-mobile-logo">
            <svg
              width="24"
              height="24"
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
            Kumburu
          </Link>

          <h1 className="auth-title">Request Access</h1>
          <p className="auth-subtitle">Join the connected paddy ecosystem.</p>

          <form className="auth-form" onSubmit={(e) => e.preventDefault()}>
            <div className="form-group">
              <label htmlFor="name">Full Name</label>
              <div className="auth-input-wrapper">
                <User className="auth-input-icon" size={20} />
                <input 
                  type="text" 
                  id="name" 
                  className="auth-input-with-icon" 
                  placeholder="Amara Silva" 
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="email">Email Address</label>
              <div className="auth-input-wrapper">
                <Mail className="auth-input-icon" size={20} />
                <input 
                  type="email" 
                  id="email" 
                  className="auth-input-with-icon" 
                  placeholder="name@example.com" 
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="role">Your Role</label>
              <div className="auth-input-wrapper">
                <Briefcase className="auth-input-icon" size={20} />
                <select id="role" className="auth-input-with-icon">
                  <option value="">Select your role</option>
                  <option value="farmer">Farmer</option>
                  <option value="extension_officer">Extension Officer</option>
                  <option value="buyer">Buyer / Miller</option>
                  <option value="admin">System Admin</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="password">Password</label>
              <div className="auth-input-wrapper">
                <Lock className="auth-input-icon" size={20} />
                <input 
                  type="password" 
                  id="password" 
                  className="auth-input-with-icon" 
                  placeholder="••••••••" 
                />
              </div>
            </div>

            <button type="submit" className="btn btn-primary auth-submit-btn">
              Create Account
            </button>
          </form>

          <p className="auth-footer">
            Already have an account? <Link to="/login" className="auth-link">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
