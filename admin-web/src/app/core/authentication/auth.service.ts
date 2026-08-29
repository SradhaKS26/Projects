import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, tap } from 'rxjs';
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

  private readonly userSignal = signal<UserDto | null>(this.readStoredUser());
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.getAccessToken() && !!this.userSignal());

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<ApiResponse<AuthResponse>>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(
        map((response) => {
          const data = response?.data;
          if (!response?.success || !data?.accessToken || !data.user) {
            throw new Error(response?.message || 'Login failed.');
          }
          return data;
        }),
        tap((data) => this.persistSession(data)),
      );
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
    void this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  hasPermission(permission: string): boolean {
    return this.userSignal()?.permissions?.includes(permission) ?? false;
  }

  hasRole(role: string): boolean {
    return this.userSignal()?.roles?.includes(role) ?? false;
  }

  private persistSession(data: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, data.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(data.user));
    this.userSignal.set(data.user);
  }

  private readStoredUser(): UserDto | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw || raw === 'undefined' || raw === 'null') {
      return null;
    }

    try {
      return JSON.parse(raw) as UserDto;
    } catch {
      return null;
    }
  }
}
