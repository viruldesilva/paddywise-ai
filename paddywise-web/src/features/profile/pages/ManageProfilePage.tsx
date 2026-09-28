import React, { useState, useEffect } from 'react';
import { useAuth } from '../../../hooks/useAuth';
import { Sidebar } from '../../../components/Sidebar';
import { profileApi, type UserProfile } from '../services/profileApi';
import {
  User,
  Mail,
  Phone,
  Shield,
  Lock,
  CheckCircle2,
  AlertTriangle,
  Calendar,
  Layers,
  MapPin,
  Eye,
  EyeOff,
  Save,
  KeyRound,
  Menu,
  LogOut,
  Loader2,
  Check
} from 'lucide-react';
import '../../../styles/Dashboard.css';
import './ManageProfilePage.css';

export const ManageProfilePage: React.FC = () => {
  const { user, logout, updateUser } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  // Profile data
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  // Profile edit form
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [profileErrors, setProfileErrors] = useState<{ name?: string; phone?: string }>({});
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [profileStatus, setProfileStatus] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Password change form
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [passwordErrors, setPasswordErrors] = useState<{
    currentPassword?: string;
    newPassword?: string;
    confirmPassword?: string;
  }>({});
  const [isSavingPassword, setIsSavingPassword] = useState(false);
  const [passwordStatus, setPasswordStatus] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  useEffect(() => {
    async function loadProfile() {
      try {
        setIsLoading(true);
        setPageError(null);
        const data = await profileApi.getProfile();
        setProfile(data);
        setName(data.name || '');
        setPhone(data.phone || '');
      } catch (err: any) {
        console.warn('Backend profile API not yet responding, falling back to authenticated session info.', err);
        if (user) {
          const fallback: UserProfile = {
            id: 1,
            name: user.name,
            email: user.email,
            phone: user.phone || '',
            role: user.role,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            totalFields: 0,
            totalAcreage: 0,
            activeCycles: 0,
            divisions: ['General Division'],
          };
          setProfile(fallback);
          setName(user.name || '');
          setPhone(user.phone || '');
        } else {
          const msg = err?.response?.data?.message || err?.message || 'Failed to load profile details.';
          setPageError(msg);
        }
      } finally {
        setIsLoading(false);
      }
    }

    loadProfile();
  }, []);

  // Sri Lankan phone validation helper
  const validatePhone = (val: string): boolean => {
    if (!val) return true; // phone is optional
    const cleaned = val.replace(/[\s-]/g, '');
    const slPattern = /^(?:0|94|\+94)?(7[0-9]{8})$/;
    return slPattern.test(cleaned);
  };

  const handleProfileSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setProfileStatus(null);
    const errors: { name?: string; phone?: string } = {};

    if (!name.trim()) {
      errors.name = 'Full name is required.';
    } else if (name.trim().length < 2) {
      errors.name = 'Name must be at least 2 characters.';
    }

    if (phone.trim() && !validatePhone(phone.trim())) {
      errors.phone = 'Please enter a valid Sri Lankan mobile number (e.g. 0771234567).';
    }

    if (Object.keys(errors).length > 0) {
      setProfileErrors(errors);
      return;
    }

    setProfileErrors({});
    setIsSavingProfile(true);

    try {
      const updated = await profileApi.updateProfile({
        name: name.trim(),
        phone: phone.trim() ? phone.trim() : undefined
      });

      setProfile(updated);
      setName(updated.name);
      setPhone(updated.phone || '');

      // Sync user in global auth context so header greeting updates
      if (updateUser) {
        updateUser({ name: updated.name, phone: updated.phone || undefined });
      }

      setProfileStatus({
        type: 'success',
        message: 'Your profile information has been successfully updated.'
      });
    } catch (err: any) {
      console.error('Failed to update profile', err);
      const msg = err?.response?.data?.message || 'Failed to update profile. Please try again.';
      setProfileStatus({ type: 'error', message: msg });
    } finally {
      setIsSavingProfile(false);
    }
  };

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordStatus(null);
    const errors: { currentPassword?: string; newPassword?: string; confirmPassword?: string } = {};

    if (!currentPassword) {
      errors.currentPassword = 'Current password is required.';
    }

    if (!newPassword) {
      errors.newPassword = 'New password is required.';
    } else if (newPassword.length < 6) {
      errors.newPassword = 'New password must be at least 6 characters.';
    }

    if (newPassword !== confirmPassword) {
      errors.confirmPassword = 'Passwords do not match.';
    }

    if (Object.keys(errors).length > 0) {
      setPasswordErrors(errors);
      return;
    }

    setPasswordErrors({});
    setIsSavingPassword(true);

    try {
      const res = await profileApi.changePassword({
        currentPassword,
        newPassword,
        confirmPassword
      });

      setPasswordStatus({
        type: 'success',
        message: res.message || 'Password changed successfully.'
      });

      // Clear password fields
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: any) {
      console.error('Failed to change password', err);
      const msg = err?.response?.data?.message || 'Password update failed. Please verify your current password.';
      setPasswordStatus({ type: 'error', message: msg });
    } finally {
      setIsSavingPassword(false);
    }
  };

  if (!user) return null;

  // Get Initials for avatar
  const getInitials = (fullName: string) => {
    const parts = (fullName || 'Farmer').trim().split(/\s+/);
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    }
    return parts[0].slice(0, 2).toUpperCase();
  };

  const memberSince = profile?.createdAt
    ? new Date(profile.createdAt).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
    : '2026';

  return (
    <div className="dashboard-layout">
      <Sidebar role={user.role} isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      <div className="dashboard-main-wrapper">
        <header className="dashboard-header">
          <div className="container dashboard-header-inner">
            <div className="dashboard-header-title">
              <button
                className="mobile-menu-btn"
                onClick={() => setIsSidebarOpen(true)}
                aria-label="Open menu"
              >
                <Menu size={24} />
              </button>
            </div>

            <div className="dashboard-user-meta">
              <div className="dashboard-user-greeting">
                <span className="dashboard-user-name">{user.name}</span>
                <span className="dashboard-user-sub">{user.email}</span>
              </div>
              <button
                onClick={logout}
                className="btn btn-secondary btn-sm"
                title="Sign Out"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}
              >
                <LogOut size={16} />
                Sign Out
              </button>
            </div>
          </div>
        </header>

        <main className="dashboard-content container profile-page-container">
          {/* Header Banner */}
          <div className="profile-header-section">
            <span className="eyebrow">ACCOUNT SETTINGS & AGRONOMIC PROFILE</span>
            <h1>Manage Profile</h1>
            <p>Review and update your personal details, contact preferences, and security credentials.</p>
          </div>

          {pageError && (
            <div className="profile-alert error" style={{ marginBottom: '2rem' }}>
              <AlertTriangle size={18} />
              <span>{pageError}</span>
            </div>
          )}

          {isLoading ? (
            <div style={{ textAlign: 'center', padding: '4rem 0', color: 'var(--ink-soft)' }}>
              <Loader2 size={36} className="spinner" style={{ margin: '0 auto 1rem' }} />
              <p>Loading your farm profile and credentials...</p>
            </div>
          ) : profile ? (
            <>
              {/* Hero Identity Banner */}
              <div className="profile-hero-card">
                <div className="profile-hero-identity">
                  <div className="profile-avatar-large">
                    {getInitials(profile.name)}
                  </div>
                  <div className="profile-hero-meta">
                    <h2>{profile.name}</h2>
                    <div className="profile-badges-row">
                      <span className="role-pill">
                        <Check size={12} /> Verified {profile.role}
                      </span>
                      <span className="farmer-id-badge">
                        ID: #FARMER-{profile.id}
                      </span>
                    </div>
                    <span className="member-since-text">
                      <Calendar size={14} /> Member since {memberSince}
                    </span>
                  </div>
                </div>
              </div>

              {/* Farm Footprint Telemetry (For Farmers) */}
              {profile.role === 'Farmer' && (
                <div className="profile-footprint-grid">
                  <div className="footprint-stat-card">
                    <div className="footprint-icon-box icon-fields">
                      <Layers size={22} />
                    </div>
                    <div className="footprint-info">
                      <span className="footprint-val">{profile.totalFields}</span>
                      <span className="footprint-label">Registered Fields</span>
                    </div>
                  </div>

                  <div className="footprint-stat-card">
                    <div className="footprint-icon-box icon-acreage">
                      <MapPin size={22} />
                    </div>
                    <div className="footprint-info">
                      <span className="footprint-val">{profile.totalAcreage} ac</span>
                      <span className="footprint-label">Total Paddy Acreage</span>
                    </div>
                  </div>

                  <div className="footprint-stat-card">
                    <div className="footprint-icon-box icon-cycles">
                      <Calendar size={22} />
                    </div>
                    <div className="footprint-info">
                      <span className="footprint-val">{profile.activeCycles}</span>
                      <span className="footprint-label">Active Cycles</span>
                    </div>
                  </div>

                  <div className="footprint-stat-card">
                    <div className="footprint-icon-box icon-division">
                      <Shield size={22} />
                    </div>
                    <div className="footprint-info">
                      <span className="footprint-val" title={profile.divisions.join(', ') || 'Not Assigned'}>
                        {profile.divisions.length > 0 ? profile.divisions[0] : 'General Zone'}
                      </span>
                      <span className="footprint-label">Agrarian Division</span>
                    </div>
                  </div>
                </div>
              )}

              {/* Dual Forms Layout */}
              <div className="profile-forms-grid">
                {/* Form 1: Personal & Contact Information */}
                <div className="profile-card">
                  <div className="profile-card-header">
                    <div className="profile-card-icon">
                      <User size={20} />
                    </div>
                    <div>
                      <h3>Personal & Contact Details</h3>
                      <p>Update your contact phone number and legal farmer name</p>
                    </div>
                  </div>

                  {profileStatus && (
                    <div className={`profile-alert ${profileStatus.type}`}>
                      {profileStatus.type === 'success' ? <CheckCircle2 size={18} /> : <AlertTriangle size={18} />}
                      <span>{profileStatus.message}</span>
                    </div>
                  )}

                  <form onSubmit={handleProfileSubmit}>
                    <div className="form-group">
                      <label htmlFor="farmer-name">Full Name *</label>
                      <div className="form-input-wrapper">
                        <User size={16} className="form-input-icon" />
                        <input
                          id="farmer-name"
                          type="text"
                          className="profile-input"
                          placeholder="e.g. Kamal Gunaratne"
                          value={name}
                          onChange={(e) => setName(e.target.value)}
                        />
                      </div>
                      {profileErrors.name && (
                        <span className="form-error-msg">{profileErrors.name}</span>
                      )}
                    </div>

                    <div className="form-group">
                      <label htmlFor="farmer-email">Email Address</label>
                      <div className="form-input-wrapper">
                        <Mail size={16} className="form-input-icon" />
                        <input
                          id="farmer-email"
                          type="email"
                          className="profile-input disabled"
                          value={profile.email}
                          disabled
                        />
                        <span className="input-suffix-badge">Verified</span>
                      </div>
                      <span className="form-help-text">Email address is your primary account identifier and cannot be modified directly.</span>
                    </div>

                    <div className="form-group">
                      <label htmlFor="farmer-phone">Mobile Phone Number</label>
                      <div className="form-input-wrapper">
                        <Phone size={16} className="form-input-icon" />
                        <input
                          id="farmer-phone"
                          type="tel"
                          className="profile-input"
                          placeholder="e.g. 077 123 4567"
                          value={phone}
                          onChange={(e) => setPhone(e.target.value)}
                        />
                      </div>
                      {profileErrors.phone && (
                        <span className="form-error-msg">{profileErrors.phone}</span>
                      )}
                      <span className="form-help-text">Used for receiving urgent weather and pest outbreak alerts.</span>
                    </div>

                    <button
                      type="submit"
                      className="profile-btn-primary"
                      disabled={isSavingProfile}
                    >
                      {isSavingProfile ? (
                        <>
                          <Loader2 size={16} className="spinner" /> Saving Changes...
                        </>
                      ) : (
                        <>
                          <Save size={16} /> Save Profile Changes
                        </>
                      )}
                    </button>
                  </form>
                </div>

                {/* Form 2: Password & Security */}
                <div className="profile-card">
                  <div className="profile-card-header">
                    <div className="profile-card-icon">
                      <KeyRound size={20} />
                    </div>
                    <div>
                      <h3>Password & Security</h3>
                      <p>Ensure your account remains safe with a strong password</p>
                    </div>
                  </div>

                  {passwordStatus && (
                    <div className={`profile-alert ${passwordStatus.type}`}>
                      {passwordStatus.type === 'success' ? <CheckCircle2 size={18} /> : <AlertTriangle size={18} />}
                      <span>{passwordStatus.message}</span>
                    </div>
                  )}

                  <form onSubmit={handlePasswordSubmit}>
                    <div className="form-group">
                      <label htmlFor="current-password">Current Password *</label>
                      <div className="form-input-wrapper">
                        <Lock size={16} className="form-input-icon" />
                        <input
                          id="current-password"
                          type={showCurrentPassword ? 'text' : 'password'}
                          className="profile-input has-toggle"
                          placeholder="Enter your current password"
                          value={currentPassword}
                          onChange={(e) => setCurrentPassword(e.target.value)}
                        />
                        <button
                          type="button"
                          className="password-toggle-btn"
                          onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                          aria-label="Toggle password visibility"
                        >
                          {showCurrentPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                        </button>
                      </div>
                      {passwordErrors.currentPassword && (
                        <span className="form-error-msg">{passwordErrors.currentPassword}</span>
                      )}
                    </div>

                    <div className="form-group">
                      <label htmlFor="new-password">New Password *</label>
                      <div className="form-input-wrapper">
                        <Lock size={16} className="form-input-icon" />
                        <input
                          id="new-password"
                          type={showNewPassword ? 'text' : 'password'}
                          className="profile-input has-toggle"
                          placeholder="Enter new password (min. 6 chars)"
                          value={newPassword}
                          onChange={(e) => setNewPassword(e.target.value)}
                        />
                        <button
                          type="button"
                          className="password-toggle-btn"
                          onClick={() => setShowNewPassword(!showNewPassword)}
                          aria-label="Toggle password visibility"
                        >
                          {showNewPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                        </button>
                      </div>
                      {passwordErrors.newPassword && (
                        <span className="form-error-msg">{passwordErrors.newPassword}</span>
                      )}
                    </div>

                    <div className="form-group">
                      <label htmlFor="confirm-password">Confirm New Password *</label>
                      <div className="form-input-wrapper">
                        <Lock size={16} className="form-input-icon" />
                        <input
                          id="confirm-password"
                          type={showConfirmPassword ? 'text' : 'password'}
                          className="profile-input has-toggle"
                          placeholder="Re-type your new password"
                          value={confirmPassword}
                          onChange={(e) => setConfirmPassword(e.target.value)}
                        />
                        <button
                          type="button"
                          className="password-toggle-btn"
                          onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                          aria-label="Toggle password visibility"
                        >
                          {showConfirmPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                        </button>
                      </div>
                      {passwordErrors.confirmPassword && (
                        <span className="form-error-msg">{passwordErrors.confirmPassword}</span>
                      )}
                    </div>

                    <div className="password-rules">
                      <strong>Password Requirements:</strong>
                      <ul>
                        <li className={newPassword.length >= 6 ? 'valid' : ''}>
                          At least 6 characters in length
                        </li>
                        <li className={newPassword && newPassword === confirmPassword ? 'valid' : ''}>
                          New password and confirm password must match
                        </li>
                      </ul>
                    </div>

                    <button
                      type="submit"
                      className="profile-btn-primary"
                      disabled={isSavingPassword}
                    >
                      {isSavingPassword ? (
                        <>
                          <Loader2 size={16} className="spinner" /> Updating Password...
                        </>
                      ) : (
                        <>
                          <KeyRound size={16} /> Update Password
                        </>
                      )}
                    </button>
                  </form>
                </div>
              </div>
            </>
          ) : null}
        </main>
      </div>
    </div>
  );
};
