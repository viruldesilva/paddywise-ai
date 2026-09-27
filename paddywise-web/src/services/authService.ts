import axios, { type AxiosInstance } from 'axios';
import { axiosInstance } from '../api/axiosInstance';
import { tokenStorage, type ITokenStorage } from './tokenStorage';
import type {
  AuthResponseDto,
  CurrentUserDto,
  LoginRequestDto,
  RefreshRequestDto,
  RegisterRequestDto,
  RegisterResponseDto,
} from '../types/auth';

export interface IAuthService {
  login(dto: LoginRequestDto): Promise<AuthResponseDto>;
  register(dto: RegisterRequestDto): Promise<RegisterResponseDto>;
  refreshToken(dto: RefreshRequestDto): Promise<AuthResponseDto>;
  getCurrentUser(): Promise<CurrentUserDto>;
  logout(): void;
}

export class AuthService implements IAuthService {
  private readonly http: AxiosInstance;
  private readonly storage: ITokenStorage;

  constructor(http: AxiosInstance = axiosInstance, storage: ITokenStorage = tokenStorage) {
    this.http = http;
    this.storage = storage;
  }

  async login(dto: LoginRequestDto): Promise<AuthResponseDto> {
    const response = await this.http.post<AuthResponseDto>('/auth/login', dto);
    const authData = response.data;
    this.storage.saveSession(authData);
    return authData;
  }

  async register(dto: RegisterRequestDto): Promise<RegisterResponseDto> {
    const response = await this.http.post<RegisterResponseDto>('/auth/register', dto);
    const data = response.data;
    if (!data.requiresApproval && data.accessToken && data.refreshToken) {
      this.storage.saveSession({
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        name: data.name || '',
        email: data.email || '',
        role: data.role || '',
      });
    }
    return data;
  }

  async refreshToken(dto: RefreshRequestDto): Promise<AuthResponseDto> {
    const response = await this.http.post<AuthResponseDto>('/auth/refresh', dto);
    const authData = response.data;
    this.storage.saveSession(authData);
    return authData;
  }

  async getCurrentUser(): Promise<CurrentUserDto> {
    const response = await this.http.get<CurrentUserDto>('/auth/me');
    return response.data;
  }

  logout(): void {
    this.storage.clearSession();
  }
}

/**
 * Extracts a human-readable error message from backend API responses.
 * Handles ASP.NET Core exception payloads, custom JSON { message: ... },
 * and ValidationProblemDetails { errors: { ... } }.
 */
export function extractApiErrorMessage(
  error: unknown,
  fallbackMessage: string = 'An unexpected error occurred. Please try again.'
): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;

    // Direct string message
    if (typeof data === 'string' && data.trim().length > 0) {
      return data;
    }

    if (data && typeof data === 'object') {
      // Backend custom message: { message: "Invalid email or password." }
      if ('message' in data && typeof data.message === 'string' && data.message.length > 0) {
        return data.message;
      }

      // ASP.NET Core Validation Problem: { errors: { Field: ["error message"] } }
      if ('errors' in data && data.errors && typeof data.errors === 'object') {
        const errorsObj = data.errors as Record<string, string[]>;
        const firstKey = Object.keys(errorsObj)[0];
        if (firstKey && Array.isArray(errorsObj[firstKey]) && errorsObj[firstKey].length > 0) {
          return errorsObj[firstKey][0];
        }
      }

      // ProblemDetails title: { title: "One or more validation errors occurred." }
      if ('title' in data && typeof data.title === 'string') {
        return data.title;
      }
    }

    if (error.message) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallbackMessage;
}

export const authService: IAuthService = new AuthService();
