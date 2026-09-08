import { Link } from 'react-router-dom';
import { Mail, Lock } from 'lucide-react';
import '../styles/Auth.css';

export default function LoginPage() {
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
            Welcome back to the <br />
            <span className="auth-quote-highlight">connected field.</span>
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

          <h1 className="auth-title">Sign in</h1>
          <p className="auth-subtitle">Access your Kumburu dashboard.</p>

          <form className="auth-form" onSubmit={(e) => e.preventDefault()}>
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
              Sign In
            </button>
          </form>

          <p className="auth-footer">
            Don't have an account? <Link to="/register" className="auth-link">Request access</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
