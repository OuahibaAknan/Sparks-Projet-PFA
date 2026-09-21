import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { delay, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  JwtClaims,
  ResetPasswordWithCodeRequest,
  UpdateProfileRequest,
} from '../models/auth.model';
import { MOCK_USERS } from '../mocks/mock-users';
import { DEMO_ACCOUNTS } from '../mocks/mock-auth';

const ACCESS_TOKEN_KEY = 'sparks_access_token';
const REFRESH_TOKEN_KEY = 'sparks_refresh_token';
const USER_KEY = 'sparks_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly currentUser = signal<JwtClaims | null>(this.readStoredUser());

  constructor(private http: HttpClient) {}

  private readStoredUser(): JwtClaims | null {
    if (!environment.useMockData) {
      this.clearStaleMockSession();
    }

    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as JwtClaims) : null;
  }

  private clearStaleMockSession(): void {
    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (accessToken?.startsWith('mock.')) {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    }
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    if (environment.useMockData) {
      const account = DEMO_ACCOUNTS.find(
        (a) => a.email.toLowerCase() === payload.email.toLowerCase() && a.password === payload.password
      );
      if (!account) {
        return throwError(() => new Error('Invalid email or password.')).pipe(delay(environment.mockLatencyMs));
      }
      const user = MOCK_USERS.find((u) => u.email.toLowerCase() === account.email.toLowerCase())!;
      const claims: JwtClaims = {
        sub: user.id,
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        role: user.role,
        initials: user.initials,
        mustChangePassword: false,
        idStellantis: null,
        profilePhotoUrl: null,
        status: user.status,
        emailStellantis: null,
        altenId: null,
        languages: [],
      };
      const response: LoginResponse = {
        accessToken: `mock.${btoa(JSON.stringify(claims))}.token`,
        refreshToken: `mock-refresh-${user.id}`,
        expiresAt: Date.now() + 1000 * 60 * 60,
        user: claims,
      };
      return of(response).pipe(delay(environment.mockLatencyMs), tap((res) => this.persistSession(res)));
    }
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, payload)
      .pipe(tap((res) => this.persistSession(res)));
  }

  refresh(): Observable<LoginResponse> {
    const storedUser = this.currentUser();
    if (environment.useMockData && storedUser) {
      const response: LoginResponse = {
        accessToken: `mock.${btoa(JSON.stringify(storedUser))}.token`,
        refreshToken: localStorage.getItem(REFRESH_TOKEN_KEY) ?? '',
        expiresAt: Date.now() + 1000 * 60 * 60,
        user: storedUser,
      };
      return of(response).pipe(delay(200), tap((res) => this.persistSession(res)));
    }
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/refresh`, {
        refreshToken: localStorage.getItem(REFRESH_TOKEN_KEY),
      })
      .pipe(tap((res) => this.persistSession(res)));
  }

  forgotPassword(payload: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiBaseUrl}/auth/forgot-password`, payload);
  }

  resetPasswordWithCode(payload: ResetPasswordWithCodeRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiBaseUrl}/auth/reset-password`, payload);
  }

  changePassword(payload: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http
      .post<{ message: string }>(`${environment.apiBaseUrl}/auth/change-password`, payload)
      .pipe(
        tap(() => {
          const user = this.currentUser();
          if (user) {
            const updated: JwtClaims = { ...user, mustChangePassword: false };
            localStorage.setItem(USER_KEY, JSON.stringify(updated));
            this.currentUser.set(updated);
          }
        })
      );
  }

  updateProfile(payload: UpdateProfileRequest): Observable<JwtClaims> {
    return this.http
      .put<JwtClaims>(`${environment.apiBaseUrl}/profile`, payload)
      .pipe(tap((claims) => this.applyClaims(claims)));
  }

  uploadProfilePhoto(file: File): Observable<JwtClaims> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<JwtClaims>(`${environment.apiBaseUrl}/profile/photo`, formData)
      .pipe(tap((claims) => this.applyClaims(claims)));
  }

  private applyClaims(claims: JwtClaims): void {
    localStorage.setItem(USER_KEY, JSON.stringify(claims));
    this.currentUser.set(claims);
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.currentUser();
  }

  private persistSession(res: LoginResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this.currentUser.set(res.user);
  }
}
