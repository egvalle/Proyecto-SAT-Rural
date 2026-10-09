import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, throwError } from 'rxjs';

import { AdminUser } from '../models/user';
import { environment } from '../../../environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  fullName: string;
  roleId: number;
}

export interface LoginResponse {
  token?: string;
  accessToken?: string;
  user?: {
    roleId?: number;
    roleDescription?: string;
    role?: string;
  };
  [key: string]: unknown;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly loginUrl = `${environment.apiBaseUrl}/api/auth/login`;
  private readonly tokenKey = 'sat-rural-auth-token';
  private readonly roleKey = 'sat-rural-user-role';
  private readonly roleIdKey = 'sat-rural-user-role-id';
  private readonly authenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());

  readonly isAuthenticated$ = this.authenticatedSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  login(credentials: LoginRequest, rememberSession = true): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(this.loginUrl, credentials).pipe(
      tap(response => {
        const token = response.token ?? response.accessToken;

        if (token) {
          const storage = rememberSession ? localStorage : sessionStorage;
          storage.setItem(this.tokenKey, token);
          const role = response.user?.roleDescription ?? response.user?.role;

          if (role) {
            storage.setItem(this.roleKey, role);
          }
        
          if (response.user?.roleId !== undefined) {
            storage.setItem(this.roleIdKey, String(response.user.roleId));
          }
          this.authenticatedSubject.next(true);
        }
        
      })
    );
  }

  register(request: RegisterRequest): Observable<unknown> {
    return this.http.post(
      `${environment.apiBaseUrl}/api/auth/register`,
      request
    );
  }

  getAdminUsers(roleId?: number): Observable<AdminUser[]> {
    let params = new HttpParams();
    if (roleId !== undefined) {
      params = params.set('roleId', roleId);
    }

    return this.http.get<AdminUser[]>(
      `${environment.apiBaseUrl}/api/auth/admin/users`,
      { params }
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    sessionStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.roleIdKey);
    sessionStorage.removeItem(this.roleIdKey);
    this.authenticatedSubject.next(false);
  }

  getRole(): string | null {
    return localStorage.getItem(this.roleKey) ?? sessionStorage.getItem(this.roleKey);
  }

  getRoleId(): number | null {
    const value = localStorage.getItem(this.roleIdKey) ?? sessionStorage.getItem(this.roleIdKey);
    if (value === null) {
      return null;
    }

    const roleId = Number(value);
    return Number.isInteger(roleId) ? roleId : null;
  }

  canAccessRoute(url: string): boolean {
    if (this.getRoleId() !== 2) {
      return true;
    }

    const path = url.split(/[?#]/, 1)[0];
    return ['/dashboard', '/sensors', '/alerts'].includes(path);
  }

  canWrite(): boolean {
    return this.getRoleId() !== 3;
  }

  rejectWrite<T>(): Observable<T> {
    return throwError(() => new Error('No posee permisos para guardar información.'));
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey) ?? sessionStorage.getItem(this.tokenKey);
  }

  private hasToken(): boolean {
    return Boolean(localStorage.getItem(this.tokenKey) ?? sessionStorage.getItem(this.tokenKey));
  }
}