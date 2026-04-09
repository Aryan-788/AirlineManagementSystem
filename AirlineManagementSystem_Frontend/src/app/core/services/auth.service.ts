import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse, UserProfile } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/identity`;

  private readonly _currentUser = signal<AuthResponse | null>(this.loadUser());
  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => !!this._currentUser());
  readonly userRole = computed(() => this._currentUser()?.role ?? '');
  readonly userId = computed(() => this._currentUser()?.userId ?? 0);
  readonly userName = computed(() => this._currentUser()?.name ?? '');
  readonly userEmail = computed(() => this._currentUser()?.email ?? '');

  constructor(private http: HttpClient, private router: Router) {}

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, data).pipe(
      tap(res => this.saveUser(res))
    );
  }

  register(data: RegisterRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, data);
  }

  verifyRegistration(data: { email: string, token: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register-verify`, data).pipe(
      tap(res => this.saveUser(res))
    );
  }

  resendVerification(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/resend-verification`, { email });
  }

  oauthLogin(data: { email: string, name: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/oauth-login`, data).pipe(
      tap(res => this.saveUser(res))
    );
  }

  getUserProfile(userId: number): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/user/${userId}`);
  }

  updateProfile(userId: number, data: import('../models/api.models').UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.apiUrl}/user/${userId}/profile`, data);
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(data: { email: string; token: string; newPassword: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, data);
  }

  logout(): void {
    localStorage.removeItem('sky_user');
    this._currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this._currentUser()?.token ?? null;
  }

  private saveUser(user: AuthResponse): void {
    localStorage.setItem('sky_user', JSON.stringify(user));
    this._currentUser.set(user);
  }

  private loadUser(): AuthResponse | null {
    try {
      const data = localStorage.getItem('sky_user');
      return data ? JSON.parse(data) : null;
    } catch {
      return null;
    }
  }

  getDefaultRoute(): string {
    switch (this.userRole()) {
      case 'Admin': return '/admin/dashboard';
      case 'Passenger': return '/passenger/dashboard';
      case 'Dealer': return '/dealer/dashboard';
      case 'GroundStaff': return '/ground-staff/baggage';
      default: return '/login';
    }
  }
}
