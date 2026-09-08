import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, Briefcase, MapPin, Phone, AlertCircle, CheckCircle } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import type { UserRole } from '../types/auth';
import '../styles/Auth.css';

export default function RegisterPage() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<UserRole | ''>('');
  const [division, setDivision] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (!fullName || !email || !role || !password) {
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
      await register({
        fullName,
        email,
        role: role as UserRole,
        division,
        phone,
        password,
      });

      setSuccess('Account created successfully! Redirecting to your dashboard...');
      setTimeout(() => {
        navigate('/dashboard', { replace: true });
      }, 1200);
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Failed to create account. Please try again.');
      }
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
          <p style={{ color: 'var(--cream-deep)', fontSize: '0.95rem', maxWidth: '24rem', opacity: 0.85, marginTop: '1rem' }}>
            Join hundreds of farmers, extension officers, and buyers collaborating on Sri Lanka's paddy ecosystem.
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

          {success && (
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              backgroundColor: '#dcfce7',
              color: '#166534',
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              fontSize: '0.875rem',
              marginBottom: '1.25rem'
            }}>
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
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
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
                  <option value="farmer">Farmer</option>
                  <option value="extension_officer">Extension Officer</option>
                  <option value="buyer">Buyer / Miller</option>
                  <option value="admin">System Admin</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="division">Agrarian Division / District (Optional)</label>
              <div className="auth-input-wrapper">
                <MapPin className="auth-input-icon" size={20} />
                <input 
                  type="text" 
                  id="division" 
                  className="auth-input-with-icon" 
                  placeholder="e.g. Polonnaruwa - Medirigiriya"
                  value={division}
                  onChange={(e) => setDivision(e.target.value)}
                  disabled={isSubmitting}
                />
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

          <p className="auth-footer">
            Already have an account? <Link to="/login" className="auth-link">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
