import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Home from './pages/Home';
import LoginSelectionPage from './features/auth/pages/LoginSelectionPage';
import AdminLoginPage from './features/auth/pages/AdminLoginPage';
import OfficerLoginPage from './features/auth/pages/OfficerLoginPage';
import RegisterPage from './pages/RegisterPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AuthProvider, useAuth } from './context/AuthContext';
import { useReveal } from './hooks/useReveal';
import { getRoleDashboardRoute } from './utils/roleRoutes';
import { ActivityDashboard } from './features/crop-resource/pages/ActivityDashboard';
import DashboardPage from './pages/DashboardPage';
import UserManagementPage from './pages/UserManagementPage';
import OfficerApprovalPage from './pages/OfficerApprovalPage';
import DivisionFieldsPage from './features/field-cultivation/pages/DivisionFieldsPage';
import FieldDetailPage from './features/field-cultivation/pages/FieldDetailPage';
import CycleDetailPage from './features/field-cultivation/pages/CycleDetailPage';
import PlanApprovalPage from './features/field-cultivation/pages/PlanApprovalPage';
import { NewActivityPage } from './features/crop-resource/pages/NewActivityPage';
import ObservationsPage from './features/pest-disease/pages/ObservationsPage';
import PestDiseaseReportsPage from './features/pest-disease/pages/PestDiseaseReportsPage';
import KnowledgeBasePage from './features/pest-disease/pages/KnowledgeBasePage';

function RoleRedirect() {
  const { user } = useAuth();
  const destination = getRoleDashboardRoute(user?.role);
  return <Navigate to={destination} replace />;
}

function AppContent() {
  // Initialize scroll reveal animations globally
  useReveal();

  return (
    <Router>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<LoginSelectionPage />} />
        <Route path="/login/admin" element={<AdminLoginPage />} />
        <Route path="/login/officer" element={<OfficerLoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* Generic dashboard route redirects to role-specific dashboard */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <RoleRedirect />
            </ProtectedRoute>
          }
        />
        
        {/* Role-specific dashboard routes */}
        <Route
          path="/dashboard/farmer"
          element={
            <ProtectedRoute>
              <DashboardPage roleView="Farmer" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard/officer"
          element={
            <ProtectedRoute>
              <DashboardPage roleView="AgriculturalOfficer" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard/admin"
          element={
            <ProtectedRoute>
              <DashboardPage roleView="Admin" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <UserManagementPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/officer-requests"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <OfficerApprovalPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard/field-officer"
          element={
            <ProtectedRoute>
              <DashboardPage roleView="FieldOfficer" />
            </ProtectedRoute>
          }
        />

        <Route
          path="/officer/fields"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer', 'Admin']}>
              <DivisionFieldsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/fields/:id"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer', 'Admin']}>
              <FieldDetailPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/cycles/:id"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer', 'Admin']}>
              <CycleDetailPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/cycles/:id/activities/new"
          element={
            <ProtectedRoute allowedRoles={['Farmer']}>
              <NewActivityPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/plans/pending"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer']}>
              <PlanApprovalPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/activities"
          element={
            <ProtectedRoute>
              <ActivityDashboard />
            </ProtectedRoute>
          }
        />

        <Route
          path="/observations"
          element={
            <ProtectedRoute allowedRoles={['Farmer']}>
              <ObservationsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/pest-disease-reports"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer']}>
              <PestDiseaseReportsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/pest-disease-knowledge"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <KnowledgeBasePage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </Router>
  );
}

function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

export default App;
