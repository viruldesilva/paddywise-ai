import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Home from './pages/Home';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AuthProvider, useAuth } from './context/AuthContext';
import { useReveal } from './hooks/useReveal';
import { getRoleDashboardRoute } from './utils/roleRoutes';

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
            <ProtectedRoute allowedRoles={['Farmer']}>
              <DashboardPage roleView="Farmer" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard/officer"
          element={
            <ProtectedRoute allowedRoles={['AgriculturalOfficer']}>
              <DashboardPage roleView="AgriculturalOfficer" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard/field-officer"
          element={
            <ProtectedRoute allowedRoles={['FieldOfficer']}>
              <DashboardPage roleView="FieldOfficer" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard/admin"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <DashboardPage roleView="Admin" />
            </ProtectedRoute>
          }
        />

        {/* Fallback route */}
        <Route path="*" element={<Navigate to="/" replace />} />
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
