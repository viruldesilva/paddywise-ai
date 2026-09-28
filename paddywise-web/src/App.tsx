import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Home from './pages/Home';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AuthProvider, useAuth } from './context/AuthContext';
import { useReveal } from './hooks/useReveal';
import { getRoleDashboardRoute } from './utils/roleRoutes';
import { ActivityDashboard } from './features/crop-resource/pages/ActivityDashboard';
import { OfficerApprovalsPage } from './features/crop-resource/pages/OfficerApprovalsPage';
import { OfficerActivityReportPage } from './features/crop-resource/pages/OfficerActivityReportPage';
import DashboardPage from './pages/DashboardPage';
import UserManagementPage from './pages/UserManagementPage';
import FieldsPage from './features/field-cultivation/pages/FieldsPage';
import FieldDetailPage from './features/field-cultivation/pages/FieldDetailPage';
import CycleDetailPage from './features/field-cultivation/pages/CycleDetailPage';
import PlanApprovalPage from './features/field-cultivation/pages/PlanApprovalPage';
import { NewActivityPage } from './features/crop-resource/pages/NewActivityPage';
import { ManageProfilePage } from './features/profile/pages/ManageProfilePage';

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
        <Route path="/login" element={<LoginPage />} />
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
            <ProtectedRoute>
              <UserManagementPage />
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
          path="/fields"
          element={
            <ProtectedRoute allowedRoles={['Farmer']}>
              <FieldsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/fields/:id"
          element={
            <ProtectedRoute allowedRoles={['Farmer', 'AgriculturalOfficer', 'FieldOfficer']}>
              <FieldDetailPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/cycles/:id"
          element={
            <ProtectedRoute allowedRoles={['Farmer', 'AgriculturalOfficer', 'FieldOfficer']}>
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
          path="/officer/approvals"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer', 'FieldOfficer', 'Admin']}>
              <OfficerApprovalsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/officer/reports"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer', 'FieldOfficer', 'Admin']}>
              <OfficerActivityReportPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/profile"
          element={
            <ProtectedRoute>
              <ManageProfilePage />
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
