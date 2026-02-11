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
  userId: string;
  email: string;
  role: string;
  clinicId: string;
  clinicName: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  role: string;
  clinicId: string;
  clinicName: string;
  requiresEmailVerification: boolean;
  verificationEmailSent: boolean;
  verificationCodeExpiresAtUtc?: string | null;
}

export interface EmailVerificationRequiredResponse {
  requiresEmailVerification: boolean;
  email: string;
  verificationEmailSent: boolean;
  nextAllowedAtUtc?: string | null;
  message?: string;
}

export interface VerifyEmailRequest {
  email: string;
  code: string;
}

export interface VerifyEmailResponse {
  emailVerified: boolean;
  message: string;
}

export interface ResendEmailVerificationRequest {
  email: string;
}

export interface ResendEmailVerificationResponse {
  verificationEmailSent: boolean;
  nextAllowedAtUtc?: string | null;
  message: string;
}
