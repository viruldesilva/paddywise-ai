import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, Briefcase, Phone, AlertCircle, CheckCircle, Clock } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { extractApiErrorMessage } from '../services/authService';
import { getRoleDashboardRoute } from '../utils/roleRoutes';
import type { UserRole } from '../types/auth';
import '../styles/Auth.css';

export default function RegisterPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<UserRole | ''>('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isPendingApproval, setIsPendingApproval] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (!name || !email || !role || !password) {
      setError('Please fill in all required fields (Name, Email, Role, Password).');
      return;
    }

    if (password.length < 6) {
      setError('Password must be at least 6 characters long.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match. Please re-enter.');
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await register({
        name: name.trim(),
        email: email.trim().toLowerCase(),
        password,
        role: role as UserRole,
        phone: phone.trim() ? phone.trim() : undefined,
      });

      if (response.requiresApproval) {
        setIsPendingApproval(true);
        setSuccess(response.message || 'Your account is pending admin verification. You will be able to log in once approved.');
        setIsSubmitting(false);
        return;
      }

      setSuccess('Account created successfully! Redirecting to your dashboard...');
      const targetRoute = getRoleDashboardRoute((response.role as UserRole) || (role as UserRole));
      setTimeout(() => {
        navigate(targetRoute, { replace: true });
      }, 1000);
    } catch (err: unknown) {
      const serverMessage = extractApiErrorMessage(err, 'Failed to create account. Please try again.');
      setError(serverMessage);
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
            Ready to see a <br />
            <span className="auth-quote-highlight">season end differently?</span>
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
            Join farmers, extension officers, and field specialists collaborating on Sri Lanka's paddy ecosystem.
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

          <h1 className="auth-title">Request Access</h1>
          <p className="auth-subtitle">Join the connected paddy ecosystem.</p>

          {error && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                backgroundColor: '#fee2e2',
                color: '#991b1b',
                padding: '0.75rem 1rem',
                borderRadius: '8px',
                fontSize: '0.875rem',
                marginBottom: '1.25rem',
              }}
            >
              <AlertCircle size={18} />
              <span>{error}</span>
            </div>
          )}

          {isPendingApproval ? (
            <div
              style={{
                backgroundColor: '#fffbeb',
                border: '1px solid #fde68a',
                padding: '1.75rem',
                borderRadius: '12px',
                textAlign: 'center',
                margin: '1.5rem 0',
              }}
            >
              <div
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '56px',
                  height: '56px',
                  borderRadius: '50%',
                  backgroundColor: '#fef3c7',
                  color: '#d97706',
                  marginBottom: '1rem',
                }}
              >
                <Clock size={28} />
              </div>
              <h2 style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--ink)', marginBottom: '0.5rem' }}>
                Account Pending Verification
              </h2>
              <p style={{ fontSize: '0.925rem', color: 'var(--ink-soft)', lineHeight: '1.5', marginBottom: '1.5rem' }}>
                {success}
              </p>
              <Link
                to="/login"
                className="btn btn-primary"
                style={{ display: 'inline-block', width: '100%', textAlign: 'center', textDecoration: 'none' }}
              >
                Go to Sign In
              </Link>
            </div>
          ) : (
            <>
              {success && (
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.5rem',
                    backgroundColor: '#dcfce7',
                    color: '#166534',
                    padding: '0.75rem 1rem',
                    borderRadius: '8px',
                    fontSize: '0.875rem',
                    marginBottom: '1.25rem',
                  }}
                >
                  <CheckCircle size={18} />
                  <span>{success}</span>
                </div>
              )}

              <form className="auth-form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="name">Full Name *</label>
              <div className="auth-input-wrapper">
                <User className="auth-input-icon" size={20} />
                <input
                  type="text"
                  id="name"
                  className="auth-input-with-icon"
                  placeholder="Amara Silva"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  disabled={isSubmitting}
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="email">Email Address *</label>
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
              <label htmlFor="role">Your Role *</label>
              <div className="auth-input-wrapper">
                <Briefcase className="auth-input-icon" size={20} />
                <select
                  id="role"
                  className="auth-input-with-icon"
                  value={role}
                  onChange={(e) => setRole(e.target.value as UserRole)}
                  disabled={isSubmitting}
                  required
                >
                  <option value="">Select your role</option>
                  <option value="Farmer">Farmer</option>
                  <option value="AgriculturalOfficer">Agricultural Officer</option>
                  <option value="FieldOfficer">Field Officer</option>
                  <option value="Admin">System Admin</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="phone">Phone Number (Optional)</label>
              <div className="auth-input-wrapper">
                <Phone className="auth-input-icon" size={20} />
                <input
                  type="tel"
                  id="phone"
                  className="auth-input-with-icon"
                  placeholder="+94 77 123 4567"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  disabled={isSubmitting}
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="password">Password *</label>
              <div className="auth-input-wrapper">
                <Lock className="auth-input-icon" size={20} />
                <input
                  type="password"
                  id="password"
                  className="auth-input-with-icon"
                  placeholder="•••••••• (min 6 characters)"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={isSubmitting}
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="confirmPassword">Confirm Password *</label>
              <div className="auth-input-wrapper">
                <Lock className="auth-input-icon" size={20} />
                <input
                  type="password"
                  id="confirmPassword"
                  className="auth-input-with-icon"
                  placeholder="••••••••"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
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
              {isSubmitting ? 'Creating Account...' : 'Create Account'}
            </button>
          </form>
          </>
          )}

          <p className="auth-footer">
            Already have an account?{' '}
            <Link to="/login" className="auth-link">
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
