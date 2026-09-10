export type UserRole = 'Farmer' | 'AgriculturalOfficer' | 'Admin' | 'FieldOfficer';

export interface AuthUser {
  name: string;
  email: string;
  role: UserRole;
  phone?: string;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface RegisterRequestDto {
  name: string;
  email: string;
  password: string;
  role: UserRole;
  phone?: string;
}

export interface RefreshRequestDto {
  refreshToken: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  name: string;
  email: string;
  role: string;
}

export interface CurrentUserDto {
  name: string | null;
  role: string | null;
}
