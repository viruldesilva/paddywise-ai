import React, { useState, type ReactNode } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Mail, Lock, AlertCircle, ArrowLeft, ArrowRight } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { extractApiErrorMessage } from '../../../services/authService';
import '../../../styles/Auth.css';

export interface LoginFormProps {
  expectedRole: 'Admin' | 'AgriculturalOfficer';
  portalTitle: string;
  portalSubtitle: string;
  roleBadgeText: string;
  roleBadgeClass: 'auth-role-badge-admin' | 'auth-role-badge-officer';
  roleIcon?: ReactNode;
  redirectPath: string;
  alternateLoginPath: string;
  alternateLoginLabel: string;
  visualQuote: string;
  visualHighlight: string;
  visualDescription: string;
}

export const LoginForm: React.FC<LoginFormProps> = ({
  expectedRole,
  portalTitle,
  portalSubtitle,
  roleBadgeText,
  roleBadgeClass,
  roleIcon,
  redirectPath,
  alternateLoginPath,
  alternateLoginLabel,
  visualQuote,
  visualHighlight,
  visualDescription,
}) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login, clearAuth } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!email || !password) {
      setError('Please fill in both your email address and password.');
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await login({ email, password });

      // Strict role verification gate
      if (response.role !== expectedRole) {
        // Clear stored token and authentication session immediately
        clearAuth();

        if (expectedRole === 'Admin') {
          setError('This account is not an Admin account. Please use the Agricultural Officer login.');
        } else {
          setError('This account is not an Agricultural Officer account. Please use the Admin login.');
        }
        return;
      }

      // Role matched: determine destination (prioritize deep link if valid)
      const stateFrom = (location.state as { from?: { pathname: string } })?.from?.pathname;
      const targetDestination =
        stateFrom && !stateFrom.startsWith('/login') && stateFrom !== '/dashboard'
          ? stateFrom
          : redirectPath;

      navigate(targetDestination, { replace: true });
    } catch (err: unknown) {
      const serverMessage = extractApiErrorMessage(err, 'Failed to sign in. Please check your credentials.');
      setError(serverMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-layout">
      {/* Left visual branding panel */}
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
            {visualQuote} <br />
            <span className="auth-quote-highlight">{visualHighlight}</span>
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
            {visualDescription}
          </p>
        </div>
      </div>

      {/* Right form panel */}
      <div className="auth-form-container">
        <div className="auth-form-wrapper">
          <Link to="/login" className="auth-nav-back">
            <ArrowLeft size={16} />
            <span>Choose different portal</span>
          </Link>

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

          <div className={`auth-role-badge ${roleBadgeClass}`}>
            {roleIcon}
            <span>{roleBadgeText}</span>
          </div>

          <h1 className="auth-title">{portalTitle}</h1>
          <p className="auth-subtitle">{portalSubtitle}</p>

          {error && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.625rem',
                backgroundColor: error.toLowerCase().includes('pending admin verification') ? '#fef3c7' : '#fee2e2',
                color: error.toLowerCase().includes('pending admin verification') ? '#92400e' : '#991b1b',
                padding: '0.875rem 1rem',
                borderRadius: '8px',
                fontSize: '0.875rem',
                marginBottom: '1.25rem',
                border: `1px solid ${error.toLowerCase().includes('pending admin verification') ? '#fde68a' : '#fecaca'}`,
              }}
            >
              <AlertCircle size={18} style={{ flexShrink: 0 }} />
              <span>{error}</span>
            </div>
          )}

          <form className="auth-form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="email">Email Address</label>
              <div className="auth-input-wrapper">
                <Mail className="auth-input-icon" size={20} />
                <input
                  type="email"
                  id="email"
                  className="auth-input-with-icon"
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={isSubmitting}
                  required
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
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={isSubmitting}
                  required
                />
              </div>
            </div>

            <button
              type="submit"
              className="btn btn-primary auth-submit-btn"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Signing in...' : 'Sign In'}
            </button>
          </form>

          <div className="auth-portal-switch">
            <span>Wrong portal? </span>
            <Link to={alternateLoginPath} className="auth-link" style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem' }}>
              <span>{alternateLoginLabel}</span>
              <ArrowRight size={14} />
            </Link>
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
};
