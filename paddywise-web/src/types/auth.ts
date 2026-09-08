export type UserRole = 'farmer' | 'extension_officer' | 'buyer' | 'admin';

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  phone?: string;
  division?: string;
  createdAt: string;
}

export interface StoredUser extends User {
  passwordHash: string; // Plaintext or simulated hash in localStorage for now
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  fullName: string;
  email: string;
  role: UserRole;
  password: string;
  phone?: string;
  division?: string;
}

export interface AuthResponse {
  user: User;
  token: string;
}
