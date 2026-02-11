import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { tap } from 'rxjs/operators';
import { LoginRequest, LoginResponse, DecodedToken } from '../models/auth.models';
import { API_BASE_URL } from '../core/api.config';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${API_BASE_URL}/api/User`;
  private readonly tokenStorageKey = 'token';
  private readonly roleClaimKeys = [
    'role',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'
  ];
  private readonly emailClaimKeys = [
    'email',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
  ];
  private readonly userIdClaimKeys = [
    'nameid',
    'sub',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  ];
  private readonly clinicIdClaimKeys = ['clinic_id', 'clinicId'];
  private readonly clinicNameClaimKeys = ['clinic_name', 'clinicName'];

  constructor(private http: HttpClient) {}

  login(credentials: LoginRequest) {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        localStorage.setItem(this.tokenStorageKey, response.token);
      })
    );
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;

    const decoded = this.decodeToken(token);
    if (!decoded) {
      this.logout();
      return false;
    }

    const exp = this.readNumericClaim(decoded.exp);
    if (!exp) {
      this.logout();
      return false;
    }

    const isExpired = exp * 1000 <= Date.now();
    if (isExpired) {
      this.logout();
      return false;
    }

    return true;
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
  }

  getUserRole(): string | null {
    return this.readStringClaim(this.roleClaimKeys);
  }

  getUserRoleNormalized(): string | null {
    const role = this.getUserRole();
    if (!role) {
      return null;
    }

    return role.trim().toLowerCase();
  }

  getUserEmail(): string | null {
    return this.readStringClaim(this.emailClaimKeys);
  }

  getUserId(): string | null {
    return this.readStringClaim(this.userIdClaimKeys);
  }

  getClinicId(): string | null {
    return this.readStringClaim(this.clinicIdClaimKeys);
  }

  getClinicName(): string | null {
    return this.readStringClaim(this.clinicNameClaimKeys);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  private getDecodedToken(): DecodedToken | null {
    const token = this.getToken();
    if (!token) return null;

    return this.decodeToken(token);
  }

  private decodeToken(token: string): DecodedToken | null {
    try {
      return jwtDecode<DecodedToken>(token);
    } catch {
      return null;
    }
  }

  private readStringClaim(claimKeys: string[]): string | null {
    const decoded = this.getDecodedToken();
    if (!decoded) return null;

    for (const key of claimKeys) {
      const value = decoded[key];
      if (typeof value === 'string' && value.trim()) {
        return value;
      }

      if (Array.isArray(value) && value.length > 0 && typeof value[0] === 'string') {
        return value[0];
      }
    }

    return null;
  }

  private readNumericClaim(value: unknown): number | null {
    if (typeof value === 'number') {
      return Number.isFinite(value) ? value : null;
    }

    if (typeof value === 'string') {
      const parsed = Number(value);
      return Number.isFinite(parsed) ? parsed : null;
    }

    return null;
  }
}
