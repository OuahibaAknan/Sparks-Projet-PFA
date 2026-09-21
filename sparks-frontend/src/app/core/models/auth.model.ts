import { UserRole, UserStatus } from './user.model';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

export interface JwtClaims {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  initials: string;
  mustChangePassword: boolean;
  idStellantis: string | null;
  profilePhotoUrl: string | null;
  status: UserStatus;
  emailStellantis: string | null;
  altenId: string | null;
  languages: string[];
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordWithCodeRequest {
  email: string;
  code: string;
  newPassword: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  idStellantis: string | null;
  emailStellantis: string | null;
  altenId: string | null;
  languages: string[];
  isActive: boolean;
}

export interface LoginResponse extends AuthTokens {
  user: JwtClaims;
}

export interface DemoAccount {
  label: string;
  email: string;
  password: string;
  role: UserRole;
}
