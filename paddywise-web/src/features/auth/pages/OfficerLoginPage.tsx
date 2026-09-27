import { Sprout } from 'lucide-react';
import { LoginForm } from '../components/LoginForm';

export default function OfficerLoginPage() {
  return (
    <LoginForm
      expectedRole="AgriculturalOfficer"
      portalTitle="Agricultural Officer Sign in"
      portalSubtitle="Access field management, cycle approvals, pest reports, and advisory services."
      roleBadgeText="Agricultural Officer Portal"
      roleBadgeClass="auth-role-badge-officer"
      roleIcon={<Sprout size={14} />}
      redirectPath="/dashboard/officer"
      alternateLoginPath="/login/admin"
      alternateLoginLabel="Administrator Login"
      visualQuote="Empowering sustainable"
      visualHighlight="harvests across fields."
      visualDescription="Agronomic decision support, field cultivation approvals, pest surveillance, and farmer advisory workflows."
    />
  );
}
