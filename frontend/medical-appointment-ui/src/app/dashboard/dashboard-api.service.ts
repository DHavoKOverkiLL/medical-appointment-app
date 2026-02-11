import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { API_BASE_URL } from '../core/api.config';
import {
  AdminDashboardResponse,
  DoctorDashboardResponse,
  PatientDashboardResponse,
  PatientAppointment,
  AvailableAppointmentSlotsResponse,
  DoctorAvailabilityResponse,
  DoctorWeeklyTimeRange,
  DoctorDateOverride,
  ClinicUpsertPayload,
  ClinicSummary,
  SystemInfoResponse,
  UserSummary,
  UpdateProfileRequest,
  UpdateAccountSettingsRequest
} from './dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardApiService {
  private readonly apiBase = `${API_BASE_URL}/api`;

  constructor(private http: HttpClient) {}

  getAdminDashboard() {
    return this.http.get<AdminDashboardResponse>(`${this.apiBase}/Dashboard/admin`);
  }

  getDoctorDashboard() {
    return this.http.get<DoctorDashboardResponse>(`${this.apiBase}/Dashboard/doctor`);
  }

  getPatientDashboard() {
    return this.http.get<PatientDashboardResponse>(`${this.apiBase}/Dashboard/patient`);
  }

  getPatientAppointments() {
    return this.http.get<PatientAppointment[]>(`${this.apiBase}/Appointment/patient`);
  }

  getAvailableSlots(doctorId: string, date: Date | string) {
    const dateParam = typeof date === 'string'
      ? date
      : this.toDateOnlyString(date);

    return this.http.get<AvailableAppointmentSlotsResponse>(`${this.apiBase}/Appointment/available-slots`, {
      params: {
        doctorId,
        date: dateParam
      }
    });
  }

  getDoctorAvailability(doctorId?: string) {
    const params = doctorId ? { doctorId } : undefined;
    return this.http.get<DoctorAvailabilityResponse>(`${this.apiBase}/Appointment/doctor-availability`, { params });
  }

  upsertDoctorAvailability(payload: {
    weeklyAvailability: DoctorWeeklyTimeRange[];
    weeklyBreaks: DoctorWeeklyTimeRange[];
    overrides: DoctorDateOverride[];
  }, doctorId?: string) {
    const params = doctorId ? { doctorId } : undefined;
    return this.http.put<DoctorAvailabilityResponse>(`${this.apiBase}/Appointment/doctor-availability`, payload, { params });
  }

  private toDateOnlyString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  requestPostponeAppointment(appointmentId: string, payload: { proposedDateTime: string; reason: string }) {
    return this.http.post(`${this.apiBase}/Appointment/${appointmentId}/postpone-request`, payload);
  }

  getUsers() {
    return this.http.get<UserSummary[]>(`${this.apiBase}/User`);
  }

  createUser(payload: {
    username: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    personalIdentifier: string;
    address: string;
    birthDate: string;
    roleName: string;
    clinicId: string;
  }) {
    return this.http.post<UserSummary>(`${this.apiBase}/User/admin`, payload);
  }

  updateUser(userId: string, payload: {
    username: string;
    email: string;
    firstName: string;
    lastName: string;
    personalIdentifier: string;
    address: string;
    birthDate: string;
    clinicId: string;
  }) {
    return this.http.put<UserSummary>(`${this.apiBase}/User/${userId}`, payload);
  }

  updateUserRole(userId: string, roleName: string) {
    return this.http.put(`${this.apiBase}/User/${userId}/role`, { roleName });
  }

  getSystemInfo() {
    return this.http.get<SystemInfoResponse>(`${this.apiBase}/SystemInfo`);
  }

  getClinics() {
    return this.http.get<ClinicSummary[]>(`${this.apiBase}/Clinic`);
  }

  createClinic(payload: ClinicUpsertPayload) {
    return this.http.post<ClinicSummary>(`${this.apiBase}/Clinic`, payload);
  }

  updateClinic(clinicId: string, payload: ClinicUpsertPayload & { isActive: boolean }) {
    return this.http.put<ClinicSummary>(`${this.apiBase}/Clinic/${clinicId}`, payload);
  }

  getMyProfile() {
    return this.http.get<UserSummary>(`${this.apiBase}/User/me`);
  }

  updateMyProfile(payload: UpdateProfileRequest) {
    return this.http.put<UserSummary>(`${this.apiBase}/User/me/profile`, payload);
  }

  updateMyAccountSettings(payload: UpdateAccountSettingsRequest) {
    return this.http.put<{ message: string; username: string; email: string }>(`${this.apiBase}/User/me/account`, payload);
  }
}
