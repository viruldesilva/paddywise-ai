import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { User, LoginCredentials, RegisterCredentials } from '../types/auth';
import { authService } from '../services/authService';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  register: (data: RegisterCredentials) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    // Initialize seed accounts and restore any active session
    authService.init();
    const session = authService.getCurrentSession();
    if (session) {
      setUser(session.user);
      setToken(session.token);
    }
    setIsLoading(false);
  }, []);

  const login = async (credentials: LoginCredentials) => {
    setIsLoading(true);
    try {
      const session = await authService.login(credentials);
      setUser(session.user);
      setToken(session.token);
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: RegisterCredentials) => {
    setIsLoading(true);
    try {
      const session = await authService.register(data);
      setUser(session.user);
      setToken(session.token);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
    setToken(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
