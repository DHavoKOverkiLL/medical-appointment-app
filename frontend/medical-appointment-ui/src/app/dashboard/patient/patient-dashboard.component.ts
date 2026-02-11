import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { PatientAppointment, PatientDashboardResponse } from '../dashboard.models';
import { combineDateAndTime, toDateKey, toTimeInputValue } from '../../core/date-time/date-time.utils';

interface CalendarCell {
  date: Date;
  inCurrentMonth: boolean;
  isToday: boolean;
  appointments: PatientAppointment[];
}

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    TranslateModule
  ],
  templateUrl: './patient-dashboard.component.html',
  styleUrls: ['./patient-dashboard.component.scss']
})
export class PatientDashboardComponent implements OnInit {
  readonly metricSkeletons = Array.from({ length: 4 });

  summary: PatientDashboardResponse | null = null;
  appointments: PatientAppointment[] = [];
  calendarCells: CalendarCell[] = [];
  selectedDayAppointments: PatientAppointment[] = [];

  loading = true;
  errorMessage = '';
  successMessage = '';

  currentMonth = this.getMonthStart(new Date());
  selectedDate = this.getDateOnly(new Date());
  readonly weekDays = [
    'common.daysShort.mon',
    'common.daysShort.tue',
    'common.daysShort.wed',
    'common.daysShort.thu',
    'common.daysShort.fri',
    'common.daysShort.sat',
    'common.daysShort.sun'
  ];
  readonly minDate = new Date();

  activePostponeAppointmentId: string | null = null;
  activePostponeDoctorId: string | null = null;
  activePostponeSlotTimezone = '';
  availablePostponeSlots: string[] = [];
  loadingPostponeSlots = false;
  postponeForm: FormGroup;
  postponeSubmitting = false;

  constructor(
    private dashboardApi: DashboardApiService,
    private fb: FormBuilder
  ) {
    this.postponeForm = this.fb.group({
      date: [null, Validators.required],
      time: ['', Validators.required],
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });

    this.postponeForm.get('date')?.valueChanges.subscribe(() => {
      if (this.activePostponeAppointmentId) {
        this.reloadPostponeSlots();
      }
    });
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  changeMonth(offset: number): void {
    this.currentMonth = this.getMonthStart(new Date(
      this.currentMonth.getFullYear(),
      this.currentMonth.getMonth() + offset,
      1
    ));
    this.buildCalendar();
  }

  selectDate(date: Date): void {
    this.selectedDate = this.getDateOnly(date);
    this.selectedDayAppointments = this.getAppointmentsForDate(this.selectedDate);
  }

  openPostponeRequest(appointment: PatientAppointment): void {
    this.activePostponeAppointmentId = appointment.appointmentId;
    this.activePostponeDoctorId = appointment.doctorId;
    this.successMessage = '';
    this.errorMessage = '';

    const source = new Date(appointment.proposedDateTime || appointment.appointmentDateTime);
    source.setDate(source.getDate() + 1);

    this.postponeForm.patchValue({
      date: source,
      time: toTimeInputValue(source),
      reason: appointment.postponeReason || ''
    });

    this.reloadPostponeSlots();
  }

  cancelPostponeRequest(): void {
    this.activePostponeAppointmentId = null;
    this.activePostponeDoctorId = null;
    this.activePostponeSlotTimezone = '';
    this.availablePostponeSlots = [];
    this.loadingPostponeSlots = false;
    this.postponeForm.reset();
    this.postponeSubmitting = false;
  }

  submitPostponeRequest(): void {
    if (this.postponeForm.invalid || !this.activePostponeAppointmentId) {
      return;
    }

    const dateValue = this.postponeForm.value.date as Date;
    const timeValue = this.postponeForm.value.time as string;
    const reason = this.postponeForm.value.reason as string;
    if (!this.availablePostponeSlots.includes(timeValue)) {
      this.errorMessage = 'patientDashboard.errors.timeUnavailable';
      return;
    }

    const proposedDateTime = combineDateAndTime(dateValue, timeValue).toISOString();

    this.postponeSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.requestPostponeAppointment(this.activePostponeAppointmentId, {
      proposedDateTime,
      reason
    }).subscribe({
      next: () => {
        this.successMessage = 'patientDashboard.messages.postponeSent';
        this.postponeSubmitting = false;
        this.cancelPostponeRequest();
        this.reloadAppointmentsOnly();
      },
      error: err => {
        this.postponeSubmitting = false;
        this.errorMessage = err?.error?.message || err?.error || 'patientDashboard.errors.postponeFailed';
      }
    });
  }

  private reloadPostponeSlots(): void {
    this.postponeForm.patchValue({ time: '' }, { emitEvent: false });
    this.availablePostponeSlots = [];
    this.activePostponeSlotTimezone = '';

    const doctorId = this.activePostponeDoctorId;
    const date = this.postponeForm.value.date as Date | null;
    if (!doctorId || !date) {
      return;
    }

    this.loadingPostponeSlots = true;
    this.dashboardApi.getAvailableSlots(doctorId, date).subscribe({
      next: response => {
        this.availablePostponeSlots = response.slots.map(slot => slot.localTime);
        this.activePostponeSlotTimezone = response.timezone;
        this.loadingPostponeSlots = false;
      },
      error: err => {
        this.loadingPostponeSlots = false;
        this.errorMessage = err?.error?.message || err?.error || 'patientDashboard.errors.loadSlots';
      }
    });
  }

  getStatusLabel(status: string, appointmentStatus: string): string {
    const lifecycleStatus = this.normalizeLifecycleStatus(appointmentStatus);
    if (lifecycleStatus === 'cancelled') return 'statuses.cancelled';
    if (lifecycleStatus === 'completed') return 'statuses.completed';
    if (lifecycleStatus === 'noshow') return 'statuses.noShow';

    const normalized = this.normalizeStatus(status);
    if (normalized === 'approved') return 'statuses.approved';
    if (normalized === 'pending') return 'statuses.pendingDoctorReview';
    if (normalized === 'counterproposed') return 'statuses.counterProposedByDoctor';
    if (normalized === 'rejected') return 'statuses.rejected';
    return 'statuses.onSchedule';
  }

  isPastAppointment(appointment: PatientAppointment): boolean {
    return new Date(appointment.appointmentDateTime).getTime() <= Date.now();
  }

  canRequestPostpone(appointment: PatientAppointment): boolean {
    if (this.normalizeLifecycleStatus(appointment.status) !== 'scheduled') {
      return false;
    }

    if (this.isPastAppointment(appointment)) {
      return false;
    }

    return this.normalizeStatus(appointment.postponeRequestStatus) !== 'counterproposed';
  }

  isCounterProposed(appointment: PatientAppointment): boolean {
    if (this.normalizeLifecycleStatus(appointment.status) !== 'scheduled') {
      return false;
    }

    return this.normalizeStatus(appointment.postponeRequestStatus) === 'counterproposed';
  }

  isRejectedStatus(status: string): boolean {
    return this.normalizeStatus(status) === 'rejected';
  }

  trackCalendar(_: number, cell: CalendarCell): string {
    return `${cell.date.getFullYear()}-${cell.date.getMonth()}-${cell.date.getDate()}`;
  }

  trackAppointment(_: number, appointment: PatientAppointment): string {
    return appointment.appointmentId;
  }

  private loadDashboard(): void {
    this.loading = true;
    this.errorMessage = '';

    forkJoin({
      summary: this.dashboardApi.getPatientDashboard(),
      appointments: this.dashboardApi.getPatientAppointments()
    }).subscribe({
      next: result => {
        this.summary = result.summary;
        this.appointments = result.appointments;
        this.buildCalendar();
        this.selectDate(this.selectedDate);
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'patientDashboard.errors.loadDashboard';
        this.loading = false;
      }
    });
  }

  private reloadAppointmentsOnly(): void {
    this.dashboardApi.getPatientAppointments().subscribe({
      next: appointments => {
        this.appointments = appointments;
        this.buildCalendar();
        this.selectDate(this.selectedDate);
      },
      error: () => {
        this.errorMessage = 'patientDashboard.errors.refreshAppointments';
      }
    });
  }

  private buildCalendar(): void {
    const monthStart = this.getMonthStart(this.currentMonth);
    const gridStart = this.getCalendarGridStart(monthStart);
    const today = this.getDateOnly(new Date());
    const appointmentsByDate = this.groupAppointmentsByDate(this.appointments);
    const cells: CalendarCell[] = [];

    for (let i = 0; i < 42; i++) {
      const date = new Date(gridStart);
      date.setDate(gridStart.getDate() + i);
      const key = toDateKey(date);

      cells.push({
        date,
        inCurrentMonth: date.getMonth() === monthStart.getMonth(),
        isToday: toDateKey(date) === toDateKey(today),
        appointments: appointmentsByDate.get(key) || []
      });
    }

    this.calendarCells = cells;
  }

  private groupAppointmentsByDate(items: PatientAppointment[]): Map<string, PatientAppointment[]> {
    const map = new Map<string, PatientAppointment[]>();

    for (const appointment of items) {
      const date = new Date(appointment.appointmentDateTime);
      const key = toDateKey(date);
      const list = map.get(key) || [];
      list.push(appointment);
      map.set(key, list);
    }

    return map;
  }

  private getAppointmentsForDate(date: Date): PatientAppointment[] {
    const targetKey = toDateKey(date);
    return this.appointments
      .filter(appointment => toDateKey(new Date(appointment.appointmentDateTime)) === targetKey)
      .sort((a, b) => a.appointmentDateTime.localeCompare(b.appointmentDateTime));
  }

  private getMonthStart(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  private getCalendarGridStart(monthStart: Date): Date {
    const mondayBasedDay = (monthStart.getDay() + 6) % 7;
    const start = new Date(monthStart);
    start.setDate(monthStart.getDate() - mondayBasedDay);
    return start;
  }

  private getDateOnly(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }

  private normalizeStatus(status: string): string {
    return (status || '').trim().toLowerCase();
  }

  private normalizeLifecycleStatus(status: string): string {
    return (status || '').trim().toLowerCase();
  }
}
