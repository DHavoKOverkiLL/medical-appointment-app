import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { API_BASE_URL } from '../../core/api.config';
import { AuthService } from '../../auth/auth.service';

interface AppointmentAuditEventRow {
  appointmentAuditEventId: string;
  appointmentId: string;
  eventType: string;
  details: string;
  actorUserId: string | null;
  actorRole: string;
  actorName: string | null;
  occurredAtUtc: string;
}

@Component({
  selector: 'app-appointment-audit',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './appointment-audit.component.html',
  styleUrls: ['./appointment-audit.component.scss']
})
export class AppointmentAuditComponent implements OnInit {
  appointmentId = '';
  auditEvents: AppointmentAuditEventRow[] = [];
  loading = true;
  errorMessage = '';

  private readonly endpointBase = `${API_BASE_URL}/api/Appointment`;

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.appointmentId = this.route.snapshot.paramMap.get('appointmentId') || '';
    if (!this.appointmentId) {
      this.loading = false;
      this.errorMessage = 'appointmentAudit.errors.invalidAppointment';
      return;
    }

    this.loadAudit();
  }

  get backRoute(): string {
    const role = this.auth.getUserRoleNormalized();
    if (role === 'admin') return '/dashboard/appointments/all';
    if (role === 'doctor') return '/dashboard/doctor-appointments';
    if (role === 'patient') return '/dashboard/appointments/my';
    return '/dashboard';
  }

  eventTypeLabel(eventType: string): string {
    const normalized = (eventType || '').trim().toLowerCase();
    if (normalized === 'created') return 'appointmentAudit.eventTypes.created';
    if (normalized === 'postponerequested') return 'appointmentAudit.eventTypes.postponeRequested';
    if (normalized === 'postponeapprovedbydoctor') return 'appointmentAudit.eventTypes.postponeApproved';
    if (normalized === 'postponerejectedbydoctor') return 'appointmentAudit.eventTypes.postponeRejected';
    if (normalized === 'postponecounterproposedbydoctor') return 'appointmentAudit.eventTypes.counterProposed';
    if (normalized === 'postponecounteracceptedbypatient') return 'appointmentAudit.eventTypes.counterAccepted';
    if (normalized === 'postponecounterrejectedbypatient') return 'appointmentAudit.eventTypes.counterRejected';
    if (normalized === 'cancelled') return 'appointmentAudit.eventTypes.cancelled';
    if (normalized === 'attendancemarkedcompleted') return 'appointmentAudit.eventTypes.attendanceCompleted';
    if (normalized === 'attendancemarkednoshow') return 'appointmentAudit.eventTypes.attendanceNoShow';
    return eventType;
  }

  trackAuditEvent(_: number, event: AppointmentAuditEventRow): string {
    return event.appointmentAuditEventId;
  }

  private loadAudit(): void {
    this.loading = true;
    this.errorMessage = '';

    this.http.get<AppointmentAuditEventRow[]>(`${this.endpointBase}/${this.appointmentId}/audit`).subscribe({
      next: events => {
        this.auditEvents = events;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'appointmentAudit.errors.loadFailed';
        this.loading = false;
      }
    });
  }
}
