import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Mail, Lock, AlertCircle, Sparkles } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import '../styles/Auth.css';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  // Redirect destination after login
  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/dashboard';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!email || !password) {
      setError('Please fill in both your email address and password.');
      return;
    }

    setIsSubmitting(true);
    try {
      await login({ email, password });
      navigate(from, { replace: true });
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Failed to sign in. Please check your credentials.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDemoLogin = async (demoEmail: string) => {
    setEmail(demoEmail);
    setPassword('Password123!');
    setError(null);
    setIsSubmitting(true);
    try {
      await login({ email: demoEmail, password: 'Password123!' });
      navigate('/dashboard', { replace: true });
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

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
          <p style={{ color: 'var(--cream-deep)', fontSize: '0.95rem', maxWidth: '24rem', opacity: 0.85, marginTop: '1rem' }}>
            A coordinated decision support workflow for farmers, officers, buyers, and administrators.
          </p>
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

          {/* Quick Demo Logins Pill Box */}
          <div style={{
            background: 'var(--cream-deep)',
            borderRadius: '8px',
            padding: '0.875rem 1rem',
            marginBottom: '1.5rem',
            fontSize: '0.85rem'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontWeight: 600, color: 'var(--ink)', marginBottom: '0.5rem' }}>
              <Sparkles size={14} color="var(--gold-deep)" />
              <span>1-Click Demo Accounts (Test Each Role):</span>
            </div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.4rem' }}>
              <button 
                type="button" 
                onClick={() => handleDemoLogin('farmer@kumburu.lk')} 
                className="btn btn-secondary btn-sm"
                style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem', background: '#fff' }}
              >
                🌾 Farmer
              </button>
              <button 
                type="button" 
                onClick={() => handleDemoLogin('officer@kumburu.lk')} 
                className="btn btn-secondary btn-sm"
                style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem', background: '#fff' }}
              >
                🛡️ Officer
              </button>
              <button 
                type="button" 
                onClick={() => handleDemoLogin('buyer@kumburu.lk')} 
                className="btn btn-secondary btn-sm"
                style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem', background: '#fff' }}
              >
                🏪 Buyer
              </button>
              <button 
                type="button" 
                onClick={() => handleDemoLogin('admin@kumburu.lk')} 
                className="btn btn-secondary btn-sm"
                style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem', background: '#fff' }}
              >
                ⚙️ Admin
              </button>
            </div>
          </div>

          {error && (
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              backgroundColor: '#fee2e2',
              color: '#991b1b',
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              fontSize: '0.875rem',
              marginBottom: '1.25rem'
            }}>
              <AlertCircle size={18} />
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

          <p className="auth-footer">
            Don't have an account? <Link to="/register" className="auth-link">Request access</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
