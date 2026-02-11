export interface DecodedToken {
  [claim: string]: unknown;
  nameid?: string;
  email?: string;
  role?: string;
  clinic_id?: string;
  clinic_name?: string;
  exp?: number | string;
  iat?: number | string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  role: string;
  clinicId: string;
  clinicName: string;
}
