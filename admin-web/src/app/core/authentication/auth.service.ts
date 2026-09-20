import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, tap, throwError, shareReplay, finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  AuthResponse,
  LoginRequest,
  UserDto,
} from '../../shared/models/auth.models';

const ACCESS_TOKEN_KEY = 'sm_access_token';
const REFRESH_TOKEN_KEY = 'sm_refresh_token';
const USER_KEY = 'sm_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly accessTokenSignal = signal<string | null>(this.readStoredToken());
  private readonly userSignal = signal<UserDto | null>(this.readStoredUser());
  private refreshInFlight$: Observable<AuthResponse> | null = null;

  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => {
    const token = this.accessTokenSignal();
    const user = this.userSignal();
    return !!token && !!user && !this.isAccessTokenExpired(token);
  });

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<ApiResponse<AuthResponse> | AuthResponse>(
        `${environment.apiBaseUrl}/auth/login`,
        request,
      )
      .pipe(
        map((response) => this.unwrapAuthResponse(response)),
        tap((data) => this.persistSession(data)),
      );
  }

  refreshSession(): Observable<AuthResponse> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }

    this.refreshInFlight$ = this.http
      .post<ApiResponse<AuthResponse> | AuthResponse>(
        `${environment.apiBaseUrl}/auth/refresh`,
        { refreshToken },
      )
      .pipe(
        map((response) => this.unwrapAuthResponse(response)),
        tap((data) => this.persistSession(data)),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
        shareReplay(1),
      );

    return this.refreshInFlight$;
  }

  logout(): void {
    try {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    } catch {
      // ignore storage failures on logout
    }

    this.accessTokenSignal.set(null);
    this.userSignal.set(null);
    void this.router.navigateByUrl('/login', { replaceUrl: true });
  }

  getAccessToken(): string | null {
    return this.accessTokenSignal();
  }

  getRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      return null;
    }
  }

  hasPermission(permission: string): boolean {
    return this.userSignal()?.permissions?.includes(permission) ?? false;
  }

  hasRole(role: string): boolean {
    return this.userSignal()?.roles?.includes(role) ?? false;
  }

  isProviderOnly(): boolean {
    return (
      this.hasRole('ServiceProvider') &&
      !this.hasPermission('ManageProviders') &&
      !this.hasPermission('ManageUsers') &&
      !this.hasPermission('ManageServices')
    );
  }

  homePath(): string {
    return this.isProviderOnly() ? '/my-application' : '/dashboard';
  }

  isAccessTokenExpired(token: string | null = this.accessTokenSignal()): boolean {
    if (!token) {
      return true;
    }

    const payload = this.decodeJwtPayload(token);
    if (!payload || typeof payload.exp !== 'number') {
      return true;
    }

    const nowSeconds = Math.floor(Date.now() / 1000);
    return payload.exp <= nowSeconds;
  }

  private persistSession(data: AuthResponse): void {
    try {
      localStorage.setItem(ACCESS_TOKEN_KEY, data.accessToken);
      localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
      localStorage.setItem(USER_KEY, JSON.stringify(data.user));
    } catch {
      // Continue with in-memory session if browser storage is unavailable.
    }

    this.accessTokenSignal.set(data.accessToken);
    this.userSignal.set(data.user);
  }

  private unwrapAuthResponse(
    response: ApiResponse<AuthResponse> | AuthResponse | Record<string, unknown>,
  ): AuthResponse {
    const root = response as Record<string, unknown>;
    const success = (root['success'] ?? root['Success']) as boolean | undefined;
    const wrapped = (root['data'] ?? root['Data']) as Record<string, unknown> | undefined;
    const payload = (wrapped ?? root) as Record<string, unknown>;

    if (success === false) {
      throw new Error(String(root['message'] ?? root['Message'] ?? 'Login failed.'));
    }

    const accessToken = String(payload['accessToken'] ?? payload['AccessToken'] ?? '');
    const refreshToken = String(payload['refreshToken'] ?? payload['RefreshToken'] ?? '');
    const accessTokenExpiresAt = String(
      payload['accessTokenExpiresAt'] ?? payload['AccessTokenExpiresAt'] ?? '',
    );
    const rawUser = (payload['user'] ?? payload['User']) as Record<string, unknown> | undefined;

    if (!accessToken || !rawUser) {
      throw new Error(String(root['message'] ?? root['Message'] ?? 'Login failed.'));
    }

    return {
      accessToken,
      refreshToken,
      accessTokenExpiresAt,
      user: this.normalizeUser(rawUser),
    };
  }

  private normalizeUser(raw: Record<string, unknown>): UserDto {
    return {
      id: String(raw['id'] ?? raw['Id'] ?? ''),
      firstName: String(raw['firstName'] ?? raw['FirstName'] ?? ''),
      lastName: String(raw['lastName'] ?? raw['LastName'] ?? ''),
      email: String(raw['email'] ?? raw['Email'] ?? ''),
      phoneNumber: (raw['phoneNumber'] ?? raw['PhoneNumber'] ?? null) as string | null,
      profileImageUrl: (raw['profileImageUrl'] ?? raw['ProfileImageUrl'] ?? null) as string | null,
      isActive: Boolean(raw['isActive'] ?? raw['IsActive'] ?? true),
      roles: (raw['roles'] ?? raw['Roles'] ?? []) as string[],
      permissions: (raw['permissions'] ?? raw['Permissions'] ?? []) as string[],
    };
  }

  private decodeJwtPayload(token: string): { exp?: number } | null {
    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    try {
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const json = atob(padded);
      return JSON.parse(json) as { exp?: number };
    } catch {
      return null;
    }
  }

  private readStoredToken(): string | null {
    try {
      return localStorage.getItem(ACCESS_TOKEN_KEY);
    } catch {
      return null;
    }
  }

  private readStoredUser(): UserDto | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      if (!raw || raw === 'undefined' || raw === 'null') {
        return null;
      }

      return this.normalizeUser(JSON.parse(raw) as Record<string, unknown>);
    } catch {
      return null;
    }
  }
}
