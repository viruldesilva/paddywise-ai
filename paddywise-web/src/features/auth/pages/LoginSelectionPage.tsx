import { Link, useNavigate } from 'react-router-dom';
import { Sprout, ShieldCheck, ArrowRight, Smartphone } from 'lucide-react';
import '../../../styles/Auth.css';

export default function LoginSelectionPage() {
  const navigate = useNavigate();

  return (
    <div className="auth-layout">
      {/* Left visual panel */}
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
            Welcome to the <br />
            <span className="auth-quote-highlight">connected field.</span>
          </h2>
          <p
            style={{
              color: 'var(--cream-deep)',
              fontSize: '0.95rem',
              maxWidth: '24rem',
              opacity: 0.85,
              marginTop: '1rem',
            }}
          >
            A dedicated decision-support and governance web portal for officers and system administrators.
          </p>
        </div>
      </div>

      {/* Right selection panel */}
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

          <h1 className="auth-title">Choose Portal</h1>
          <p className="auth-subtitle">Select your role to access your dedicated workspace.</p>

          <div className="auth-selection-grid">
            {/* Officer Login Card */}
            <div
              className="auth-selection-card"
              role="button"
              tabIndex={0}
              onClick={() => navigate('/login/officer')}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  navigate('/login/officer');
                }
              }}
            >
              <div className="auth-selection-icon-wrapper auth-selection-icon-officer">
                <Sprout size={24} />
              </div>
              <div className="auth-selection-card-content">
                <div className="auth-selection-card-title">
                  <span>Agricultural Officer</span>
                  <ArrowRight size={18} />
                </div>
                <p className="auth-selection-card-desc">
                  Field evaluations, cycle plan approvals, crop recommendations, and pest surveillance.
                </p>
                <span className="btn btn-secondary btn-sm" style={{ pointerEvents: 'none' }}>
                  Agricultural Officer Login
                </span>
              </div>
            </div>

            {/* Admin Login Card */}
            <div
              className="auth-selection-card"
              role="button"
              tabIndex={0}
              onClick={() => navigate('/login/admin')}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  navigate('/login/admin');
                }
              }}
            >
              <div className="auth-selection-icon-wrapper auth-selection-icon-admin">
                <ShieldCheck size={24} />
              </div>
              <div className="auth-selection-card-content">
                <div className="auth-selection-card-title">
                  <span>Administrator</span>
                  <ArrowRight size={18} />
                </div>
                <p className="auth-selection-card-desc">
                  User account management, security roles, system configurations, and national analytics.
                </p>
                <span className="btn btn-secondary btn-sm" style={{ pointerEvents: 'none' }}>
                  Admin Login
                </span>
              </div>
            </div>
          </div>

          {/* Farmer mobile notice */}
          <div className="auth-notice-box">
            <Smartphone size={18} style={{ flexShrink: 0, marginTop: '2px', color: 'var(--ink-soft)' }} />
            <div>
              <strong>Are you a farmer?</strong> The web portal is reserved for officers and administrators. Please access Kumburu through the mobile application.
            </div>
          </div>

          <p className="auth-footer">
            Don't have an account?{' '}
            <Link to="/register" className="auth-link">
              Request access
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
