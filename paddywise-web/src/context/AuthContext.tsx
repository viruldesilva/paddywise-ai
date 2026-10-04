import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type {
  AuthResponseDto,
  AuthUser,
  LoginRequestDto,
  RegisterRequestDto,
  RegisterResponseDto,
  UserRole,
} from '../types/auth';
import { authService } from '../services/authService';
import { tokenStorage } from '../services/tokenStorage';

export interface AuthContextType {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (emailOrDto: string | LoginRequestDto, password?: string) => Promise<AuthResponseDto>;
  register: (data: RegisterRequestDto) => Promise<RegisterResponseDto>;
  logout: (shouldRedirect?: boolean) => void;
  clearAuth: () => void;
  updateUser?: (data: Partial<AuthUser>) => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        const storedToken = tokenStorage.getAccessToken();
        const storedUser = tokenStorage.getUser();

        if (storedToken && storedUser) {
          setUser(storedUser);
          setToken(storedToken);

          // Verify session validity with backend /api/auth/me
          try {
            const me = await authService.getCurrentUser();
            if (me && me.name && me.role) {
              const updatedUser: AuthUser = {
                name: me.name,
                email: storedUser.email,
                role: me.role as UserRole,
                phone: storedUser.phone,
              };
              setUser(updatedUser);
              tokenStorage.saveSession({
                accessToken: storedToken,
                refreshToken: tokenStorage.getRefreshToken() || '',
                name: me.name,
                email: storedUser.email,
                role: me.role,
              });
            }
          } catch {
            // If token verification fails and refresh fails, interceptor clears storage
            if (!tokenStorage.getAccessToken()) {
              setUser(null);
              setToken(null);
            }
          }
        }
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, []);

  const login = async (
    emailOrDto: string | LoginRequestDto,
    password?: string
  ): Promise<AuthResponseDto> => {
    setIsLoading(true);
    try {
      const credentials: LoginRequestDto =
        typeof emailOrDto === 'string'
          ? { email: emailOrDto, password: password || '' }
          : emailOrDto;

      const response = await authService.login(credentials);
      const currentUser: AuthUser = {
        name: response.name,
        email: response.email,
        role: response.role as UserRole,
      };

      setUser(currentUser);
      setToken(response.accessToken);
      return response;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: RegisterRequestDto): Promise<RegisterResponseDto> => {
    setIsLoading(true);
    try {
      const response = await authService.register(data);
      if (!response.requiresApproval && response.accessToken) {
        const currentUser: AuthUser = {
          name: response.name || data.name,
          email: response.email || data.email,
          role: (response.role as UserRole) || data.role,
          phone: data.phone,
        };

        setUser(currentUser);
        setToken(response.accessToken);
      }
      return response;
    } finally {
      setIsLoading(false);
    }
  };

  const clearAuth = () => {
    authService.logout();
    setUser(null);
    setToken(null);
  };

  const logout = (shouldRedirect: boolean = true) => {
    clearAuth();
    if (shouldRedirect && !window.location.pathname.startsWith('/login')) {
      window.location.href = '/login';
    }
  };

  const updateUser = (data: Partial<AuthUser>) => {
    setUser((prev) => {
      if (!prev) return null;
      const updated = { ...prev, ...data };
      const currentToken = tokenStorage.getAccessToken() || '';
      const refreshToken = tokenStorage.getRefreshToken() || '';
      tokenStorage.saveSession({
        accessToken: currentToken,
        refreshToken: refreshToken,
        name: updated.name,
        email: updated.email,
        role: updated.role,
      });
      return updated;
    });
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!user && !!token,
        isLoading,
        login,
        register,
        logout,
        clearAuth,
        updateUser,
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
