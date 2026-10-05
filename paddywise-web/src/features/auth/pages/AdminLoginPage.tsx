import { ShieldCheck } from 'lucide-react';
import { LoginForm } from '../components/LoginForm';

export default function AdminLoginPage() {
  return (
    <LoginForm
      expectedRole="Admin"
      portalTitle="Administrator Sign in"
      portalSubtitle="Enter your credentials to access system controls and user management."
      roleBadgeText="System Administration"
      roleBadgeClass="auth-role-badge-admin"
      roleIcon={<ShieldCheck size={14} />}
      redirectPath="/dashboard/admin"
      alternateLoginPath="/login/officer"
      alternateLoginLabel="Agricultural Officer Login"
      visualQuote="Securing our agricultural"
      visualHighlight="governance & operations."
      visualDescription="Centralized administration for user lifecycle, access control, audit verification, and system settings."
    />
  );
}
