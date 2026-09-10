import type { AuthResponseDto, AuthUser, UserRole } from '../types/auth';

export interface ITokenStorage {
  getAccessToken(): string | null;
  getRefreshToken(): string | null;
  getUser(): AuthUser | null;
  saveSession(authResponse: AuthResponseDto): void;
  saveTokens(accessToken: string, refreshToken: string): void;
  clearSession(): void;
}

const ACCESS_TOKEN_KEY = 'paddywise_access_token';
const REFRESH_TOKEN_KEY = 'paddywise_refresh_token';
const USER_KEY = 'paddywise_user';

// Old legacy keys to remove
const LEGACY_USERS_KEY = 'kumburu_users';
const LEGACY_SESSION_KEY = 'kumburu_session';

export class LocalStorageTokenStorage implements ITokenStorage {
  constructor() {
    this.cleanLegacyStorage();
  }

  private cleanLegacyStorage(): void {
    try {
      localStorage.removeItem(LEGACY_USERS_KEY);
      localStorage.removeItem(LEGACY_SESSION_KEY);
    } catch {
      // Ignore if localStorage is restricted
    }
  }

  getAccessToken(): string | null {
    try {
      return localStorage.getItem(ACCESS_TOKEN_KEY);
    } catch {
      return null;
    }
  }

  getRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      return null;
    }
  }

  getUser(): AuthUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      if (!raw) return null;
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }

  saveSession(authResponse: AuthResponseDto): void {
    try {
      localStorage.setItem(ACCESS_TOKEN_KEY, authResponse.accessToken);
      localStorage.setItem(REFRESH_TOKEN_KEY, authResponse.refreshToken);

      const user: AuthUser = {
        name: authResponse.name,
        email: authResponse.email,
        role: authResponse.role as UserRole,
      };
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } catch {
      // LocalStorage write failure handling
    }
  }

  saveTokens(accessToken: string, refreshToken: string): void {
    try {
      localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    } catch {
      // LocalStorage write failure handling
    }
  }

  clearSession(): void {
    try {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      this.cleanLegacyStorage();
    } catch {
      // LocalStorage clearance handling
    }
  }
}

export const tokenStorage: ITokenStorage = new LocalStorageTokenStorage();
