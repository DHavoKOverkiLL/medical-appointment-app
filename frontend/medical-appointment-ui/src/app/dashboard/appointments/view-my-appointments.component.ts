import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { API_BASE_URL } from '../../core/api.config';
import { PatientAppointment } from '../dashboard.models';

@Component({
  selector: 'app-view-my-appointments',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    RouterModule,
    TranslateModule
  ],
  templateUrl: './view-my-appointments.component.html',
  styleUrls: ['./view-my-appointments.component.scss']
})
export class ViewMyAppointmentsComponent implements OnInit {
  readonly skeletonRows = Array.from({ length: 4 });
  appointments: PatientAppointment[] = [];
  loading = true;
  actionSubmittingAppointmentId: string | null = null;
  actionDecision: 'Accept' | 'Reject' | 'Cancel' | null = null;
  errorMessage = '';
  successMessage = '';
  private readonly endpoint = `${API_BASE_URL}/api/Appointment/patient`;
  private readonly counterResponseEndpointBase = `${API_BASE_URL}/api/Appointment`;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadAppointments();
  }

  get upcomingCount(): number {
    const now = Date.now();
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      new Date(appointment.appointmentDateTime).getTime() >= now).length;
  }

  get pendingPostponeCount(): number {
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      this.normalizeStatus(appointment.postponeRequestStatus) === 'pending').length;
  }

  get counterProposalCount(): number {
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      this.normalizeStatus(appointment.postponeRequestStatus) === 'counterproposed').length;
  }

  get completedCount(): number {
    const now = Date.now();
    return this.appointments.filter(appointment => new Date(appointment.appointmentDateTime).getTime() < now).length;
  }

  getStatusLabel(appointment: PatientAppointment): string {
    const lifecycleStatus = this.normalizeLifecycleStatus(appointment.status);
    if (lifecycleStatus === 'cancelled') return 'statuses.cancelled';
    if (lifecycleStatus === 'completed') return 'statuses.completed';
    if (lifecycleStatus === 'noshow') return 'statuses.noShow';

    const normalized = this.normalizeStatus(appointment.postponeRequestStatus);
    if (normalized === 'approved') return 'statuses.postponeApproved';
    if (normalized === 'pending') return 'statuses.postponePending';
    if (normalized === 'counterproposed') return 'statuses.doctorCounterProposed';
    if (normalized === 'rejected') return 'statuses.postponeRejected';
    return 'statuses.onSchedule';
  }

  getStatusClass(appointment: PatientAppointment): string {
    const lifecycleStatus = this.normalizeLifecycleStatus(appointment.status);
    if (lifecycleStatus === 'cancelled') return 'status-badge status-badge--rejected';
    if (lifecycleStatus === 'completed') return 'status-badge status-badge--approved';
    if (lifecycleStatus === 'noshow') return 'status-badge status-badge--counter';

    const normalized = this.normalizeStatus(appointment.postponeRequestStatus);
    if (normalized === 'approved') return 'status-badge status-badge--approved';
    if (normalized === 'pending') return 'status-badge status-badge--pending';
    if (normalized === 'counterproposed') return 'status-badge status-badge--counter';
    if (normalized === 'rejected') return 'status-badge status-badge--rejected';
    return 'status-badge status-badge--default';
  }

  isRejectedStatus(status: string): boolean {
    return this.normalizeStatus(status) === 'rejected';
  }

  canRespondToCounterProposal(appointment: PatientAppointment): boolean {
    if (this.normalizeLifecycleStatus(appointment.status) !== 'scheduled') {
      return false;
    }

    return this.normalizeStatus(appointment.postponeRequestStatus) === 'counterproposed'
      && !!appointment.proposedDateTime
      && !this.isPast(appointment);
  }

  canCancelAppointment(appointment: PatientAppointment): boolean {
    return this.normalizeLifecycleStatus(appointment.status) === 'scheduled' && !this.isPast(appointment);
  }

  cancelAppointment(appointment: PatientAppointment): void {
    if (!this.canCancelAppointment(appointment)) {
      return;
    }

    this.actionSubmittingAppointmentId = appointment.appointmentId;
    this.actionDecision = 'Cancel';
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.counterResponseEndpointBase}/${appointment.appointmentId}/cancel`, {
      reason: 'Cancelled by patient through self-service portal.'
    }).subscribe({
      next: () => {
        this.actionSubmittingAppointmentId = null;
        this.actionDecision = null;
        this.successMessage = 'appointmentsMy.messages.cancelled';
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmittingAppointmentId = null;
        this.actionDecision = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsMy.errors.cancelFailed';
      }
    });
  }

  respondToCounterProposal(appointment: PatientAppointment, decision: 'Accept' | 'Reject'): void {
    this.actionSubmittingAppointmentId = appointment.appointmentId;
    this.actionDecision = decision;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.counterResponseEndpointBase}/${appointment.appointmentId}/postpone-counter-response`, {
      decision
    }).subscribe({
      next: () => {
        this.actionSubmittingAppointmentId = null;
        this.actionDecision = null;
        this.successMessage = decision === 'Accept'
          ? 'appointmentsMy.messages.counterAccepted'
          : 'appointmentsMy.messages.counterRejected';
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmittingAppointmentId = null;
        this.actionDecision = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsMy.errors.counterResponseFailed';
      }
    });
  }

  isPast(appointment: PatientAppointment): boolean {
    return new Date(appointment.appointmentDateTime).getTime() < Date.now();
  }

  trackAppointment(_: number, appointment: PatientAppointment): string {
    return appointment.appointmentId;
  }

  private loadAppointments(): void {
    this.loading = true;
    this.errorMessage = '';

    this.http.get<PatientAppointment[]>(this.endpoint).subscribe({
      next: data => {
        this.appointments = data
          .slice()
          .sort((a, b) => a.appointmentDateTime.localeCompare(b.appointmentDateTime));
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'appointmentsMy.errors.loadAppointments';
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
}
