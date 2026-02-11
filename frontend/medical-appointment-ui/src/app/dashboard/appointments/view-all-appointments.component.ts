import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule, HttpParams, HttpResponse } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TranslateModule } from '@ngx-translate/core';
import { API_BASE_URL } from '../../core/api.config';
import { toDateKey, toIsoDate } from '../../core/date-time/date-time.utils';

interface AppointmentRow {
  appointmentId: string;
  status: string;
  appointmentDateTime: string;
  patientName: string;
  doctorName: string;
  clinicId: string;
  clinicName: string;
  postponeRequestStatus: string;
  proposedDateTime: string | null;
  postponeReason: string | null;
  postponeRequestedAtUtc: string | null;
  cancellationReason: string | null;
}

interface ClinicRow {
  clinicId: string;
  name: string;
}

@Component({
  selector: 'app-view-all-appointments',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    TranslateModule
  ],
  templateUrl: './view-all-appointments.component.html',
  styleUrls: ['./view-all-appointments.component.scss']
})
export class ViewAllAppointmentsComponent implements OnInit {
  readonly skeletonRows = Array.from({ length: 5 });
  appointments: AppointmentRow[] = [];
  clinics: ClinicRow[] = [];
  selectedClinicId = 'all';
  selectedStatus = 'all';
  dateFrom = '';
  dateTo = '';
  dateFromInput: Date | null = null;
  dateToInput: Date | null = null;
  readonly statusFilterOptions = [
    { value: 'all', label: 'common.allStatuses' },
    { value: 'Scheduled', label: 'statuses.onSchedule' },
    { value: 'Cancelled', label: 'statuses.cancelled' },
    { value: 'Completed', label: 'statuses.completed' },
    { value: 'NoShow', label: 'statuses.noShow' }
  ];
  loading = false;
  exporting = false;
  errorMessage = '';
  successMessage = '';
  private readonly endpoint = `${API_BASE_URL}/api/Appointment/all`;
  private readonly exportEndpoint = `${API_BASE_URL}/api/Appointment/all/export`;
  private readonly clinicsEndpoint = `${API_BASE_URL}/api/Clinic`;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loading = true;

    this.http.get<ClinicRow[]>(this.clinicsEndpoint).subscribe({
      next: clinics => {
        this.clinics = clinics;
      },
      error: () => {
        this.errorMessage = 'appointmentsAdmin.errors.loadClinics';
      }
    });

    this.loadAppointments();
  }

  get selectedClinicLabel(): string {
    if (this.selectedClinicId === 'all') {
      return 'common.allClinics';
    }

    return this.clinics.find(clinic => clinic.clinicId === this.selectedClinicId)?.name || 'common.clinic';
  }

  get todayCount(): number {
    const todayKey = toDateKey(new Date());
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      toDateKey(new Date(appointment.appointmentDateTime)) === todayKey).length;
  }

  get upcomingCount(): number {
    const now = Date.now();
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      new Date(appointment.appointmentDateTime).getTime() >= now).length;
  }

  get pendingCount(): number {
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      this.normalizeStatus(appointment.postponeRequestStatus) === 'pending').length;
  }

  onClinicChange(clinicId: string): void {
    this.selectedClinicId = clinicId;
    this.loadAppointments();
  }

  onStatusChange(status: string): void {
    this.selectedStatus = status;
    this.loadAppointments();
  }

  applyDateFilters(): void {
    this.dateFrom = toIsoDate(this.dateFromInput) ?? '';
    this.dateTo = toIsoDate(this.dateToInput) ?? '';
    this.loadAppointments();
  }

  clearFilters(): void {
    this.selectedClinicId = 'all';
    this.selectedStatus = 'all';
    this.dateFrom = '';
    this.dateTo = '';
    this.dateFromInput = null;
    this.dateToInput = null;
    this.loadAppointments();
  }

  exportAppointmentsCsv(): void {
    this.exporting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.get(this.exportEndpoint, {
      params: this.buildFilterParams(),
      observe: 'response',
      responseType: 'blob'
    }).subscribe({
      next: response => {
        this.exporting = false;
        const fileName = this.readFileNameFromResponse(response)
          || `appointments-${new Date().toISOString().replace(/[:.]/g, '-')}.csv`;

        this.downloadCsv(response.body, fileName);
        this.successMessage = 'appointmentsAdmin.messages.exported';
      },
      error: () => {
        this.exporting = false;
        this.errorMessage = 'appointmentsAdmin.errors.exportFailed';
      }
    });
  }

  getStatusLabel(appointment: AppointmentRow): string {
    const lifecycleStatus = this.normalizeLifecycleStatus(appointment.status);
    if (lifecycleStatus === 'cancelled') return 'statuses.cancelled';
    if (lifecycleStatus === 'completed') return 'statuses.completed';
    if (lifecycleStatus === 'noshow') return 'statuses.noShow';

    const normalized = this.normalizeStatus(appointment.postponeRequestStatus);
    if (normalized === 'pending') return 'statuses.postponePending';
    if (normalized === 'counterproposed') return 'statuses.counterProposed';
    if (normalized === 'approved') return 'statuses.postponeApproved';
    if (normalized === 'rejected') return 'statuses.postponeRejected';
    return 'statuses.onSchedule';
  }

  getStatusClass(appointment: AppointmentRow): string {
    const lifecycleStatus = this.normalizeLifecycleStatus(appointment.status);
    if (lifecycleStatus === 'cancelled') return 'status-badge status-badge--rejected';
    if (lifecycleStatus === 'completed') return 'status-badge status-badge--approved';
    if (lifecycleStatus === 'noshow') return 'status-badge status-badge--counter';

    const normalized = this.normalizeStatus(appointment.postponeRequestStatus);
    if (normalized === 'pending') return 'status-badge status-badge--pending';
    if (normalized === 'counterproposed') return 'status-badge status-badge--counter';
    if (normalized === 'approved') return 'status-badge status-badge--approved';
    if (normalized === 'rejected') return 'status-badge status-badge--rejected';
    return 'status-badge status-badge--default';
  }

  trackAppointment(_: number, appointment: AppointmentRow): string {
    return appointment.appointmentId;
  }

  private loadAppointments(): void {
    this.loading = true;
    this.errorMessage = '';

    this.http.get<AppointmentRow[]>(this.endpoint, {
      params: this.buildFilterParams()
    }).subscribe({
      next: data => {
        this.appointments = data
          .slice()
          .sort((a, b) => a.appointmentDateTime.localeCompare(b.appointmentDateTime));
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'appointmentsAdmin.errors.loadAppointments';
        this.loading = false;
      }
    });
  }

  private normalizeStatus(status: string): string {
    return (status || '').trim().toLowerCase();
  }

  private normalizeLifecycleStatus(status: string): string {
    return (status || '').trim().toLowerCase();
  }

  private readFileNameFromResponse(response: HttpResponse<Blob>): string | null {
    const contentDisposition = response.headers.get('content-disposition') || '';
    const utfMatch = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
    if (utfMatch?.[1]) {
      return decodeURIComponent(utfMatch[1]);
    }

    const fileNameMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
    return fileNameMatch?.[1] || null;
  }

  private buildFilterParams(): HttpParams {
    let params = new HttpParams();

    if (this.selectedClinicId !== 'all') {
      params = params.set('clinicId', this.selectedClinicId);
    }

    if (this.selectedStatus !== 'all') {
      params = params.set('status', this.selectedStatus);
    }

    if (this.dateFrom) {
      params = params.set('dateFrom', this.dateFrom);
    }

    if (this.dateTo) {
      params = params.set('dateTo', this.dateTo);
    }

    return params;
  }

  private downloadCsv(blob: Blob | null, fileName: string): void {
    if (!blob) {
      return;
    }

    const objectUrl = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.click();
    window.URL.revokeObjectURL(objectUrl);
  }
}
