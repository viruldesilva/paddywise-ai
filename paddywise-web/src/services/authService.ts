import type {
  User,
  StoredUser,
  LoginCredentials,
  RegisterCredentials,
  AuthResponse
} from '../types/auth';

const STORAGE_KEY_USERS = 'kumburu_users';
const STORAGE_KEY_SESSION = 'kumburu_session';

// Pre-seeded demo accounts for each role
const DEFAULT_USERS: StoredUser[] = [
  {
    id: 'usr_farmer_01',
    fullName: 'Bandara Wanninayake',
    email: 'farmer@kumburu.lk',
    role: 'farmer',
    phone: '+94 77 123 4567',
    division: 'Polonnaruwa - Medirigiriya',
    passwordHash: 'Password123!',
    createdAt: new Date('2026-01-15').toISOString(),
  },
  {
    id: 'usr_officer_01',
    fullName: 'Dr. Nilmini Perera',
    email: 'officer@kumburu.lk',
    role: 'extension_officer',
    phone: '+94 71 987 6543',
    division: 'Anuradhapura - Nuwaragam Palatha',
    passwordHash: 'Password123!',
    createdAt: new Date('2026-01-10').toISOString(),
  },
  {
    id: 'usr_buyer_01',
    fullName: 'Roshan Fernando (Lanka Rice Mills)',
    email: 'buyer@kumburu.lk',
    role: 'buyer',
    phone: '+94 76 345 6789',
    division: 'Kurunegala',
    passwordHash: 'Password123!',
    createdAt: new Date('2026-01-20').toISOString(),
  },
  {
    id: 'usr_admin_01',
    fullName: 'System Administrator',
    email: 'admin@kumburu.lk',
    role: 'admin',
    phone: '+94 11 234 5678',
    division: 'Colombo HQ',
    passwordHash: 'Password123!',
    createdAt: new Date('2026-01-01').toISOString(),
  },
];

function getStoredUsers(): StoredUser[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_USERS);
    if (!raw) {
      localStorage.setItem(STORAGE_KEY_USERS, JSON.stringify(DEFAULT_USERS));
      return DEFAULT_USERS;
    }
    return JSON.parse(raw);
  } catch {
    return DEFAULT_USERS;
  }
}

function saveStoredUsers(users: StoredUser[]): void {
  localStorage.setItem(STORAGE_KEY_USERS, JSON.stringify(users));
}

function sanitizeUser(stored: StoredUser): User {
  const { passwordHash, ...user } = stored;
  return user;
}

export const authService = {
  // Initialize storage if needed
  init(): void {
    getStoredUsers();
  },

  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    // Artificial small delay to simulate network call
    await new Promise((resolve) => setTimeout(resolve, 300));

    const users = getStoredUsers();
    const normalizedEmail = credentials.email.trim().toLowerCase();
    
    const user = users.find(
      (u) => u.email.toLowerCase() === normalizedEmail
    );

    if (!user || user.passwordHash !== credentials.password) {
      throw new Error('Invalid email or password. Please try again.');
    }

    const cleanUser = sanitizeUser(user);
    const token = `mock_jwt_${user.id}_${Date.now()}`;

    const session: AuthResponse = { user: cleanUser, token };
    localStorage.setItem(STORAGE_KEY_SESSION, JSON.stringify(session));

    return session;
  },

  async register(data: RegisterCredentials): Promise<AuthResponse> {
    await new Promise((resolve) => setTimeout(resolve, 350));

    const users = getStoredUsers();
    const normalizedEmail = data.email.trim().toLowerCase();

    if (users.some((u) => u.email.toLowerCase() === normalizedEmail)) {
      throw new Error('An account with this email address already exists.');
    }

    const newUser: StoredUser = {
      id: `usr_${Date.now().toString(36)}`,
      fullName: data.fullName.trim(),
      email: normalizedEmail,
      role: data.role,
      phone: data.phone?.trim() || undefined,
      division: data.division?.trim() || undefined,
      passwordHash: data.password,
      createdAt: new Date().toISOString(),
    };

    users.push(newUser);
    saveStoredUsers(users);

    const cleanUser = sanitizeUser(newUser);
    const token = `mock_jwt_${newUser.id}_${Date.now()}`;

    const session: AuthResponse = { user: cleanUser, token };
    localStorage.setItem(STORAGE_KEY_SESSION, JSON.stringify(session));

    return session;
  },

  async logout(): Promise<void> {
    localStorage.removeItem(STORAGE_KEY_SESSION);
  },

  getCurrentSession(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY_SESSION);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  },

  getAllUsers(): User[] {
    return getStoredUsers().map(sanitizeUser);
  },

  // Reset local storage to initial seed state
  resetToDefaults(): void {
    localStorage.setItem(STORAGE_KEY_USERS, JSON.stringify(DEFAULT_USERS));
  },
};
