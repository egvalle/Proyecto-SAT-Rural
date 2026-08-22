import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token?: string;
  accessToken?: string;
  user?: {
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
          if (response.user?.role) {
            storage.setItem(this.roleKey, response.user.role);
          }
          this.authenticatedSubject.next(true);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    sessionStorage.removeItem(this.roleKey);
    this.authenticatedSubject.next(false);
  }

  getRole(): string | null {
    return localStorage.getItem(this.roleKey) ?? sessionStorage.getItem(this.roleKey);
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