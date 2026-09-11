import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Home from './pages/Home';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AuthProvider, useAuth } from './context/AuthContext';
import { useReveal } from './hooks/useReveal';
import { getRoleDashboardRoute } from './utils/roleRoutes';
import { ActivityDashboard } from './features/crop-resource/pages/ActivityDashboard';
import DashboardPage from './pages/DashboardPage';

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
          path="/dashboard/field-officer"
          element={
            <ProtectedRoute>
              <DashboardPage roleView="FieldOfficer" />
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
