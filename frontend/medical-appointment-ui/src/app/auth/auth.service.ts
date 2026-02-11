import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { tap } from 'rxjs/operators';
import { LoginRequest, LoginResponse } from '../models/auth.models';
import { API_BASE_URL } from '../core/api.config';

interface AuthSession {
  userId: string;
  email: string;
  role: string;
  clinicId: string;
  clinicName: string;
  expiresAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${API_BASE_URL}/api/User`;
  private readonly sessionStorageKey = 'auth_session';

  constructor(private http: HttpClient) {}

  login(credentials: LoginRequest) {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, credentials, { withCredentials: true })
      .pipe(
      tap(response => {
        this.saveSession({
          userId: response.userId,
          email: response.email || credentials.email.trim().toLowerCase(),
          role: response.role,
          clinicId: response.clinicId,
          clinicName: response.clinicName,
          expiresAtUtc: response.expiresAtUtc
        });
      })
    );
  }

  isLoggedIn(): boolean {
    const session = this.getSession();
    if (!session) {
      return false;
    }

    const expiresAt = Date.parse(session.expiresAtUtc);
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      this.clearSession();
      return false;
    }

    return true;
  }

  logout(): void {
    this.clearSession();
    this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true }).subscribe({
      error: () => undefined
    });
  }

  getUserRole(): string | null {
    return this.getSession()?.role ?? null;
  }

  getUserRoleNormalized(): string | null {
    const role = this.getUserRole();
    if (!role) {
      return null;
    }

    return role.trim().toLowerCase();
  }

  getUserEmail(): string | null {
    return this.getSession()?.email ?? null;
  }

  getUserId(): string | null {
    return this.getSession()?.userId ?? null;
  }

  getClinicId(): string | null {
    return this.getSession()?.clinicId ?? null;
  }

  getClinicName(): string | null {
    return this.getSession()?.clinicName ?? null;
  }

  getToken(): string | null {
    return null;
  }

  private getSession(): AuthSession | null {
    if (typeof sessionStorage === 'undefined') {
      return null;
    }

    const rawValue = sessionStorage.getItem(this.sessionStorageKey);
    if (!rawValue) {
      return null;
    }
    try {
      const parsed = JSON.parse(rawValue) as Partial<AuthSession>;
      if (
        typeof parsed.userId !== 'string' ||
        typeof parsed.email !== 'string' ||
        typeof parsed.role !== 'string' ||
        typeof parsed.clinicId !== 'string' ||
        typeof parsed.clinicName !== 'string' ||
        typeof parsed.expiresAtUtc !== 'string'
      ) {
        return null;
      }

      return {
        userId: parsed.userId,
        email: parsed.email,
        role: parsed.role,
        clinicId: parsed.clinicId,
        clinicName: parsed.clinicName,
        expiresAtUtc: parsed.expiresAtUtc
      };
    } catch {
      return null;
    }
  }

  private saveSession(session: AuthSession): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }

    sessionStorage.setItem(this.sessionStorageKey, JSON.stringify(session));
  }

  private clearSession(): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }

    sessionStorage.removeItem(this.sessionStorageKey);
  }
}
