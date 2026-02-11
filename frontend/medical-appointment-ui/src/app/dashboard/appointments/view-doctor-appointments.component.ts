import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { API_BASE_URL } from '../../core/api.config';
import { DashboardApiService } from '../dashboard-api.service';
import { combineDateAndTime, toTimeInputValue } from '../../core/date-time/date-time.utils';

interface DoctorAppointmentRow {
  appointmentId: string;
  doctorId: string;
  status: string;
  appointmentDateTime: string;
  patientName: string;
  postponeRequestStatus: string;
  proposedDateTime: string | null;
  postponeReason: string | null;
  postponeRequestedAtUtc: string | null;
  doctorResponseNote: string | null;
  doctorRespondedAtUtc: string | null;
  patientRespondedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancelledByUserId: string | null;
  cancellationReason: string | null;
}

@Component({
  selector: 'app-view-doctor-appointments',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    RouterModule,
    TranslateModule
  ],
  templateUrl: './view-doctor-appointments.component.html',
  styleUrls: ['./view-doctor-appointments.component.scss']
})
export class ViewDoctorAppointmentsComponent implements OnInit {
  readonly skeletonRows = Array.from({ length: 4 });
  appointments: DoctorAppointmentRow[] = [];
  loading = true;
  actionSubmitting = false;
  actionSubmittingAppointmentId: string | null = null;
  actionSubmittingMode: 'approve' | 'reject' | 'counter' | 'cancel' | 'complete' | 'noshow' | null = null;
  errorMessage = '';
  successMessage = '';
  activeCounterAppointmentId: string | null = null;
  activeRejectAppointmentId: string | null = null;
  availableCounterSlots: string[] = [];
  counterSlotsTimezone = '';
  loadingCounterSlots = false;
  readonly minDate = new Date();
  counterForm: FormGroup;
  rejectForm: FormGroup;

  private readonly endpoint = `${API_BASE_URL}/api/Appointment/doctor`;
  private readonly responseEndpointBase = `${API_BASE_URL}/api/Appointment`;

  constructor(
    private http: HttpClient,
    private fb: FormBuilder,
    private dashboardApi: DashboardApiService
  ) {
    this.counterForm = this.fb.group({
      date: ['', Validators.required],
      time: ['', Validators.required],
      note: ['', [Validators.maxLength(500)]]
    });

    this.rejectForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });

    this.counterForm.get('date')?.valueChanges.subscribe(() => {
      if (this.activeCounterAppointmentId) {
        this.reloadCounterSlots();
      }
    });
  }

  ngOnInit(): void {
    this.loadAppointments();
  }

  get pendingRequests(): DoctorAppointmentRow[] {
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      this.normalizeStatus(appointment.postponeRequestStatus) === 'pending');
  }

  get counterProposalsAwaitingPatient(): DoctorAppointmentRow[] {
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      this.normalizeStatus(appointment.postponeRequestStatus) === 'counterproposed');
  }

  get upcomingConsults(): DoctorAppointmentRow[] {
    const now = Date.now();
    return this.appointments.filter(appointment =>
      this.normalizeLifecycleStatus(appointment.status) === 'scheduled' &&
      new Date(appointment.appointmentDateTime).getTime() >= now);
  }

  get completedConsults(): DoctorAppointmentRow[] {
    return this.appointments.filter(appointment => {
      const status = this.normalizeLifecycleStatus(appointment.status);
      return status === 'completed' || status === 'noshow';
    });
  }

  getStatusClass(appointment: DoctorAppointmentRow): string {
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

  getStatusLabel(appointment: DoctorAppointmentRow): string {
    const lifecycleStatus = this.normalizeLifecycleStatus(appointment.status);
    if (lifecycleStatus === 'cancelled') return 'statuses.cancelled';
    if (lifecycleStatus === 'completed') return 'statuses.completed';
    if (lifecycleStatus === 'noshow') return 'statuses.noShow';

    const normalized = this.normalizeStatus(appointment.postponeRequestStatus);
    if (normalized === 'pending') return 'statuses.requestPending';
    if (normalized === 'counterproposed') return 'statuses.counterProposalSent';
    if (normalized === 'approved') return 'statuses.requestApproved';
    if (normalized === 'rejected') return 'statuses.requestRejected';
    return 'statuses.onSchedule';
  }

  get counterTargetAppointment(): DoctorAppointmentRow | null {
    if (!this.activeCounterAppointmentId) {
      return null;
    }

    return this.findAppointmentById(this.activeCounterAppointmentId);
  }

  get rejectTargetAppointment(): DoctorAppointmentRow | null {
    if (!this.activeRejectAppointmentId) {
      return null;
    }

    return this.findAppointmentById(this.activeRejectAppointmentId);
  }

  openCounterProposal(appointment: DoctorAppointmentRow): void {
    this.activeCounterAppointmentId = appointment.appointmentId;
    this.cancelRejectRequest();
    this.errorMessage = '';
    this.successMessage = '';

    const source = new Date(appointment.proposedDateTime || appointment.appointmentDateTime);
    source.setDate(source.getDate() + 1);

    this.counterForm.patchValue({
      date: source,
      time: toTimeInputValue(source),
      note: appointment.doctorResponseNote || ''
    });

    this.reloadCounterSlots();
  }

  cancelCounterProposal(): void {
    this.activeCounterAppointmentId = null;
    this.availableCounterSlots = [];
    this.counterSlotsTimezone = '';
    this.loadingCounterSlots = false;
    this.counterForm.reset();
  }

  openRejectRequest(appointment: DoctorAppointmentRow): void {
    this.activeRejectAppointmentId = appointment.appointmentId;
    this.cancelCounterProposal();
    this.errorMessage = '';
    this.successMessage = '';
    this.rejectForm.patchValue({
      reason: appointment.doctorResponseNote || ''
    });
  }

  cancelRejectRequest(): void {
    this.activeRejectAppointmentId = null;
    this.rejectForm.reset();
  }

  submitCounterProposal(): void {
    if (this.counterForm.invalid || !this.activeCounterAppointmentId) {
      return;
    }

    const dateValue = this.counterForm.value.date as Date;
    const timeValue = this.counterForm.value.time as string;
    if (!this.availableCounterSlots.includes(timeValue)) {
      this.errorMessage = 'appointmentsDoctor.errors.timeUnavailable';
      return;
    }

    const note = (this.counterForm.value.note as string || '').trim();
    const counterProposedDateTime = combineDateAndTime(dateValue, timeValue).toISOString();

    this.actionSubmitting = true;
    this.actionSubmittingAppointmentId = this.activeCounterAppointmentId;
    this.actionSubmittingMode = 'counter';
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.responseEndpointBase}/${this.activeCounterAppointmentId}/postpone-response`, {
      decision: 'CounterPropose',
      counterProposedDateTime,
      note
    }).subscribe({
      next: () => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.successMessage = 'appointmentsDoctor.messages.counterSent';
        this.cancelCounterProposal();
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsDoctor.errors.counterSendFailed';
      }
    });
  }

  private reloadCounterSlots(): void {
    this.counterForm.patchValue({ time: '' }, { emitEvent: false });
    this.availableCounterSlots = [];
    this.counterSlotsTimezone = '';

    const appointment = this.counterTargetAppointment;
    const date = this.counterForm.value.date as Date | null;
    if (!appointment || !date) {
      return;
    }

    this.loadingCounterSlots = true;
    this.dashboardApi.getAvailableSlots(appointment.doctorId, date).subscribe({
      next: response => {
        this.availableCounterSlots = response.slots.map(slot => slot.localTime);
        this.counterSlotsTimezone = response.timezone;
        this.loadingCounterSlots = false;
      },
      error: err => {
        this.loadingCounterSlots = false;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsDoctor.errors.loadSlots';
      }
    });
  }

  respondToPendingRequest(appointmentId: string): void {
    this.actionSubmitting = true;
    this.actionSubmittingAppointmentId = appointmentId;
    this.actionSubmittingMode = 'approve';
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.responseEndpointBase}/${appointmentId}/postpone-response`, { decision: 'Approve' }).subscribe({
      next: () => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.successMessage = 'appointmentsDoctor.messages.requestApproved';
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsDoctor.errors.approveFailed';
      }
    });
  }

  submitRejectRequest(): void {
    if (this.rejectForm.invalid || !this.activeRejectAppointmentId) {
      return;
    }

    const reason = (this.rejectForm.value.reason as string).trim();
    this.actionSubmitting = true;
    this.actionSubmittingAppointmentId = this.activeRejectAppointmentId;
    this.actionSubmittingMode = 'reject';
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.responseEndpointBase}/${this.activeRejectAppointmentId}/postpone-response`, {
      decision: 'Reject',
      note: reason
    }).subscribe({
      next: () => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.successMessage = 'appointmentsDoctor.messages.requestRejected';
        this.cancelRejectRequest();
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsDoctor.errors.rejectFailed';
      }
    });
  }

  canCancelAppointment(appointment: DoctorAppointmentRow): boolean {
    return this.normalizeLifecycleStatus(appointment.status) === 'scheduled'
      && new Date(appointment.appointmentDateTime).getTime() >= Date.now();
  }

  cancelAppointment(appointment: DoctorAppointmentRow): void {
    if (!this.canCancelAppointment(appointment)) {
      return;
    }

    this.actionSubmitting = true;
    this.actionSubmittingAppointmentId = appointment.appointmentId;
    this.actionSubmittingMode = 'cancel';
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.responseEndpointBase}/${appointment.appointmentId}/cancel`, {
      reason: 'Cancelled by doctor due to clinical schedule update.'
    }).subscribe({
      next: () => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.successMessage = 'appointmentsDoctor.messages.appointmentCancelled';
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsDoctor.errors.cancelFailed';
      }
    });
  }

  canMarkAttendance(appointment: DoctorAppointmentRow): boolean {
    return this.normalizeLifecycleStatus(appointment.status) === 'scheduled'
      && new Date(appointment.appointmentDateTime).getTime() <= Date.now();
  }

  markAttendance(appointment: DoctorAppointmentRow, status: 'Completed' | 'NoShow'): void {
    if (!this.canMarkAttendance(appointment)) {
      return;
    }

    this.actionSubmitting = true;
    this.actionSubmittingAppointmentId = appointment.appointmentId;
    this.actionSubmittingMode = status === 'Completed' ? 'complete' : 'noshow';
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.responseEndpointBase}/${appointment.appointmentId}/attendance`, {
      status
    }).subscribe({
      next: () => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.successMessage = status === 'Completed'
          ? 'appointmentsDoctor.messages.markedCompleted'
          : 'appointmentsDoctor.messages.markedNoShow';
        this.loadAppointments();
      },
      error: err => {
        this.actionSubmitting = false;
        this.actionSubmittingAppointmentId = null;
        this.actionSubmittingMode = null;
        this.errorMessage = err?.error?.message || err?.error || 'appointmentsDoctor.errors.markAttendanceFailed';
      }
    });
  }

  trackAppointment(_: number, appointment: DoctorAppointmentRow): string {
    return appointment.appointmentId;
  }

  private loadAppointments(): void {
    this.loading = true;
    this.errorMessage = '';

    this.http
      .get<DoctorAppointmentRow[]>(this.endpoint)
      .subscribe({
        next: data => {
          this.appointments = data
            .slice()
            .sort((a, b) => a.appointmentDateTime.localeCompare(b.appointmentDateTime));
          this.loading = false;
        },
        error: () => {
          this.errorMessage = 'appointmentsDoctor.errors.loadConsults';
          this.loading = false;
        },
      });
  }

  private normalizeStatus(status: string): string {
    return (status || '').trim().toLowerCase();
  }

  private normalizeLifecycleStatus(status: string): string {
    return (status || '').trim().toLowerCase();
  }

  private findAppointmentById(appointmentId: string): DoctorAppointmentRow | null {
    return this.appointments.find(appointment => appointment.appointmentId === appointmentId) ?? null;
  }
}
