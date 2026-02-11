import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { PatientAppointment, PatientDashboardResponse } from '../dashboard.models';
import { toDateKey } from '../../core/date-time/date-time.utils';
import {
  PostponeRequestDialogComponent,
  PostponeRequestDialogData,
  PostponeRequestDialogResult
} from './postpone-request-dialog.component';

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
    MatCardModule,
    MatButtonModule,
    MatDialogModule,
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

  constructor(
    private readonly dashboardApi: DashboardApiService,
    private readonly dialog: MatDialog
  ) {}

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
    this.errorMessage = '';
    this.successMessage = '';

    const dialogRef = this.dialog.open<
      PostponeRequestDialogComponent,
      PostponeRequestDialogData,
      PostponeRequestDialogResult
    >(PostponeRequestDialogComponent, {
      width: 'min(680px, 96vw)',
      panelClass: 'modern-admin-dialog',
      backdropClass: 'modern-admin-backdrop',
      disableClose: true,
      data: { appointment }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.sent) {
        this.successMessage = 'patientDashboard.messages.postponeSent';
        this.reloadAppointmentsOnly();
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
