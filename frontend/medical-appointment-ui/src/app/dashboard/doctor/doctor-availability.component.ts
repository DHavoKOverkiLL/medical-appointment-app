import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../auth/auth.service';
import { DashboardApiService } from '../dashboard-api.service';
import {
  DoctorAvailabilityResponse,
  DoctorDateOverride,
  DoctorWeeklyTimeRange,
  UserSummary
} from '../dashboard.models';

type AvailabilityRangeForm = FormGroup<{
  dayOfWeek: any;
  start: any;
  end: any;
}>;

type AvailabilityOverrideForm = FormGroup<{
  date: any;
  isAvailable: any;
  start: any;
  end: any;
  reason: any;
}>;

type DoctorAvailabilityUpsertPayload = {
  weeklyAvailability: DoctorWeeklyTimeRange[];
  weeklyBreaks: DoctorWeeklyTimeRange[];
  overrides: DoctorDateOverride[];
};

@Component({
  selector: 'app-doctor-availability',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './doctor-availability.component.html',
  styleUrls: ['./doctor-availability.component.scss']
})
export class DoctorAvailabilityComponent implements OnInit {
  readonly dayOptions = [
    { value: 0, labelKey: 'common.days.sunday' },
    { value: 1, labelKey: 'common.days.monday' },
    { value: 2, labelKey: 'common.days.tuesday' },
    { value: 3, labelKey: 'common.days.wednesday' },
    { value: 4, labelKey: 'common.days.thursday' },
    { value: 5, labelKey: 'common.days.friday' },
    { value: 6, labelKey: 'common.days.saturday' }
  ];

  isAdmin = false;
  doctors: UserSummary[] = [];
  selectedDoctorId = '';
  selectedClinicFilter = '';
  doctorSearchTerm = '';
  timezone = '';
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';

  form: FormGroup<{
    weeklyAvailability: FormArray<AvailabilityRangeForm>;
    weeklyBreaks: FormArray<AvailabilityRangeForm>;
    overrides: FormArray<AvailabilityOverrideForm>;
  }>;

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly dashboardApi: DashboardApiService
  ) {
    this.form = this.fb.group({
      weeklyAvailability: this.fb.array<AvailabilityRangeForm>([]),
      weeklyBreaks: this.fb.array<AvailabilityRangeForm>([]),
      overrides: this.fb.array<AvailabilityOverrideForm>([])
    });
  }

  ngOnInit(): void {
    this.isAdmin = this.auth.getUserRoleNormalized() === 'admin';
    if (this.isAdmin) {
      this.loadAdminDoctors();
      return;
    }

    this.loadAvailability();
  }

  get weeklyAvailability(): FormArray<AvailabilityRangeForm> {
    return this.form.controls.weeklyAvailability;
  }

  get weeklyBreaks(): FormArray<AvailabilityRangeForm> {
    return this.form.controls.weeklyBreaks;
  }

  get overrides(): FormArray<AvailabilityOverrideForm> {
    return this.form.controls.overrides;
  }

  get clinicFilters(): string[] {
    return Array.from(
      new Set(
        this.doctors
          .map(doctor => doctor.clinicName?.trim())
          .filter((clinicName): clinicName is string => !!clinicName)
      )
    ).sort((a, b) => a.localeCompare(b));
  }

  get filteredDoctors(): UserSummary[] {
    const normalizedClinic = this.selectedClinicFilter.trim().toLowerCase();
    const normalizedSearch = this.doctorSearchTerm.trim().toLowerCase();

    return this.doctors.filter(doctor => {
      const matchesClinic = !normalizedClinic ||
        doctor.clinicName.trim().toLowerCase() === normalizedClinic;

      if (!matchesClinic) {
        return false;
      }

      if (!normalizedSearch) {
        return true;
      }

      const fullName = `${doctor.firstName} ${doctor.lastName}`.trim().toLowerCase();
      return fullName.includes(normalizedSearch)
        || doctor.email.toLowerCase().includes(normalizedSearch)
        || doctor.username.toLowerCase().includes(normalizedSearch);
    });
  }

  onDoctorChange(doctorId: string): void {
    this.selectedDoctorId = doctorId;
    this.loadAvailability(doctorId);
  }

  onClinicFilterChange(clinicName: string): void {
    this.selectedClinicFilter = clinicName || '';
    this.ensureSelectedDoctorVisible();
  }

  onDoctorSearchChange(searchTerm: string): void {
    this.doctorSearchTerm = searchTerm || '';
    this.ensureSelectedDoctorVisible();
  }

  addWeeklyAvailability(): void {
    this.weeklyAvailability.push(this.createRangeGroup());
  }

  removeWeeklyAvailability(index: number): void {
    this.weeklyAvailability.removeAt(index);
  }

  addWeeklyBreak(): void {
    this.weeklyBreaks.push(this.createRangeGroup(1, '12:00', '13:00'));
  }

  removeWeeklyBreak(index: number): void {
    this.weeklyBreaks.removeAt(index);
  }

  addOverride(): void {
    this.overrides.push(this.createOverrideGroup(this.getTodayDateKey()));
  }

  removeOverride(index: number): void {
    this.overrides.removeAt(index);
  }

  save(): void {
    if (this.form.invalid) {
      this.errorMessage = 'availability.errors.invalidForm';
      return;
    }

    if (this.isAdmin && !this.selectedDoctorId) {
      this.errorMessage = this.filteredDoctors.length === 0
        ? 'availability.errors.noMatchingDoctors'
        : 'availability.errors.selectDoctor';
      return;
    }

    const payload = this.buildPayload();
    const validationError = this.validatePayload(payload);
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.upsertDoctorAvailability(payload, this.isAdmin ? this.selectedDoctorId : undefined).subscribe({
      next: response => {
        this.applyResponse(response);
        this.saving = false;
        this.successMessage = 'availability.messages.saved';
      },
      error: err => {
        this.saving = false;
        this.errorMessage = err?.error?.message || err?.error || 'availability.errors.saveFailed';
      }
    });
  }

  trackByIndex(index: number): number {
    return index;
  }

  trackDoctor(_: number, doctor: UserSummary): string {
    return doctor.userId;
  }

  private loadAdminDoctors(): void {
    this.loading = true;
    this.errorMessage = '';

    this.dashboardApi.getUsers().subscribe({
      next: users => {
        this.doctors = users
          .filter(user => user.role.trim().toLowerCase() === 'doctor')
          .sort((a, b) => `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`));

        if (this.filteredDoctors.length === 0) {
          this.loading = false;
          this.errorMessage = 'availability.errors.noDoctors';
          return;
        }

        this.selectedDoctorId = this.filteredDoctors[0].userId;
        this.loadAvailability(this.selectedDoctorId);
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'availability.errors.loadDoctorsFailed';
      }
    });
  }

  private loadAvailability(doctorId?: string): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.getDoctorAvailability(doctorId).subscribe({
      next: response => {
        this.applyResponse(response);
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.errorMessage = err?.error?.message || err?.error || 'availability.errors.loadFailed';
      }
    });
  }

  private applyResponse(response: DoctorAvailabilityResponse): void {
    this.timezone = response.timezone;

    this.weeklyAvailability.clear();
    for (const range of response.weeklyAvailability) {
      this.weeklyAvailability.push(this.createRangeGroup(range.dayOfWeek, range.start, range.end));
    }

    this.weeklyBreaks.clear();
    for (const range of response.weeklyBreaks) {
      this.weeklyBreaks.push(this.createRangeGroup(range.dayOfWeek, range.start, range.end));
    }

    this.overrides.clear();
    for (const override of response.overrides) {
      this.overrides.push(this.createOverrideGroup(
        override.date,
        override.isAvailable,
        override.start ?? '',
        override.end ?? '',
        override.reason ?? ''
      ));
    }
  }

  private createRangeGroup(dayOfWeek = 1, start = '09:00', end = '17:00'): AvailabilityRangeForm {
    return this.fb.group({
      dayOfWeek: [dayOfWeek, [Validators.required, Validators.min(0), Validators.max(6)]],
      start: [start, [Validators.required]],
      end: [end, [Validators.required]]
    });
  }

  private createOverrideGroup(
    date = '',
    isAvailable = false,
    start = '',
    end = '',
    reason = ''
  ): AvailabilityOverrideForm {
    return this.fb.group({
      date: [date, Validators.required],
      isAvailable: [isAvailable, Validators.required],
      start: [start],
      end: [end],
      reason: [reason]
    });
  }

  private normalizeOptionalTime(value: string | null | undefined): string | null {
    const trimmed = (value || '').trim();
    return trimmed ? trimmed : null;
  }

  private buildPayload(): DoctorAvailabilityUpsertPayload {
    return {
      weeklyAvailability: this.weeklyAvailability.controls.map(group => ({
        dayOfWeek: Number(group.value.dayOfWeek),
        start: String(group.value.start),
        end: String(group.value.end)
      })),
      weeklyBreaks: this.weeklyBreaks.controls.map(group => ({
        dayOfWeek: Number(group.value.dayOfWeek),
        start: String(group.value.start),
        end: String(group.value.end)
      })),
      overrides: this.overrides.controls.map(group => {
        const isAvailable = group.value.isAvailable === true || group.value.isAvailable === 'true';
        const start = this.normalizeOptionalTime(group.value.start as string | null | undefined);
        const end = this.normalizeOptionalTime(group.value.end as string | null | undefined);

        return {
          date: this.normalizeDateValue(group.value.date),
          isAvailable,
          start: isAvailable ? start : null,
          end: isAvailable ? end : null,
          reason: String(group.value.reason || '')
        };
      })
    };
  }

  private validatePayload(payload: DoctorAvailabilityUpsertPayload): string | null {
    if (payload.weeklyAvailability.some(range => !this.isOrderedRange(range.start, range.end))) {
      return 'availability.errors.invalidRangeOrder';
    }

    if (payload.weeklyBreaks.some(range => !this.isOrderedRange(range.start, range.end))) {
      return 'availability.errors.invalidRangeOrder';
    }

    if (this.hasOverlappingWeeklyRanges(payload.weeklyAvailability)) {
      return 'availability.errors.overlapInAvailability';
    }

    if (this.hasOverlappingWeeklyRanges(payload.weeklyBreaks)) {
      return 'availability.errors.overlapInBreaks';
    }

    if (
      payload.weeklyAvailability.length > 0 &&
      this.hasBreakOutsideAvailability(payload.weeklyAvailability, payload.weeklyBreaks)
    ) {
      return 'availability.errors.breakOutsideAvailability';
    }

    for (const override of payload.overrides) {
      const hasStart = !!override.start;
      const hasEnd = !!override.end;

      if (hasStart !== hasEnd) {
        return 'availability.errors.overridePartialTime';
      }

      if (override.isAvailable && (!hasStart || !hasEnd)) {
        return 'availability.errors.availableOverrideMissingTime';
      }

      if (hasStart && hasEnd && !this.isOrderedRange(override.start!, override.end!)) {
        return 'availability.errors.overrideRangeOrder';
      }
    }

    if (this.hasDuplicateOverrides(payload.overrides)) {
      return 'availability.errors.duplicateOverride';
    }

    if (this.hasOverrideOverlaps(payload.overrides, true)) {
      return 'availability.errors.overrideOverlapAvailable';
    }

    if (this.hasOverrideOverlaps(payload.overrides, false)) {
      return 'availability.errors.overrideOverlapUnavailable';
    }

    return null;
  }

  private hasOverlappingWeeklyRanges(ranges: DoctorWeeklyTimeRange[]): boolean {
    const byDay = new Map<number, Array<{ start: number; end: number }>>();

    for (const range of ranges) {
      const start = this.parseTimeToMinutes(range.start);
      const end = this.parseTimeToMinutes(range.end);
      if (start === null || end === null) {
        return true;
      }

      const dayRanges = byDay.get(range.dayOfWeek) ?? [];
      dayRanges.push({ start, end });
      byDay.set(range.dayOfWeek, dayRanges);
    }

    for (const dayRanges of byDay.values()) {
      if (this.hasIntervalOverlap(dayRanges)) {
        return true;
      }
    }

    return false;
  }

  private hasBreakOutsideAvailability(
    weeklyAvailability: DoctorWeeklyTimeRange[],
    weeklyBreaks: DoctorWeeklyTimeRange[]
  ): boolean {
    const availabilityByDay = new Map<number, Array<{ start: number; end: number }>>();
    for (const window of weeklyAvailability) {
      const start = this.parseTimeToMinutes(window.start);
      const end = this.parseTimeToMinutes(window.end);
      if (start === null || end === null) {
        return true;
      }

      const dayWindows = availabilityByDay.get(window.dayOfWeek) ?? [];
      dayWindows.push({ start, end });
      availabilityByDay.set(window.dayOfWeek, dayWindows);
    }

    for (const breakRange of weeklyBreaks) {
      const breakStart = this.parseTimeToMinutes(breakRange.start);
      const breakEnd = this.parseTimeToMinutes(breakRange.end);
      if (breakStart === null || breakEnd === null) {
        return true;
      }

      const dayWindows = availabilityByDay.get(breakRange.dayOfWeek) ?? [];
      const insideAvailability = dayWindows.some(window =>
        window.start <= breakStart &&
        breakEnd <= window.end);
      if (!insideAvailability) {
        return true;
      }
    }

    return false;
  }

  private hasDuplicateOverrides(overrides: DoctorDateOverride[]): boolean {
    const keySet = new Set<string>();
    for (const override of overrides) {
      const key = [
        override.date,
        override.isAvailable ? '1' : '0',
        override.start ?? '',
        override.end ?? ''
      ].join('|');

      if (keySet.has(key)) {
        return true;
      }

      keySet.add(key);
    }

    return false;
  }

  private hasOverrideOverlaps(overrides: DoctorDateOverride[], isAvailable: boolean): boolean {
    const byDate = new Map<string, Array<{ start: number; end: number }>>();

    for (const override of overrides) {
      if (override.isAvailable !== isAvailable) {
        continue;
      }

      let start = 0;
      let end = 24 * 60;

      if (override.start && override.end) {
        const parsedStart = this.parseTimeToMinutes(override.start);
        const parsedEnd = this.parseTimeToMinutes(override.end);
        if (parsedStart === null || parsedEnd === null) {
          return true;
        }

        start = parsedStart;
        end = parsedEnd;
      }

      const dateRanges = byDate.get(override.date) ?? [];
      dateRanges.push({ start, end });
      byDate.set(override.date, dateRanges);
    }

    for (const dateRanges of byDate.values()) {
      if (this.hasIntervalOverlap(dateRanges)) {
        return true;
      }
    }

    return false;
  }

  private hasIntervalOverlap(intervals: Array<{ start: number; end: number }>): boolean {
    const sorted = intervals
      .slice()
      .sort((a, b) => (a.start - b.start) || (a.end - b.end));

    for (let index = 1; index < sorted.length; index++) {
      if (sorted[index].start < sorted[index - 1].end) {
        return true;
      }
    }

    return false;
  }

  private isOrderedRange(start: string, end: string): boolean {
    const parsedStart = this.parseTimeToMinutes(start);
    const parsedEnd = this.parseTimeToMinutes(end);
    return parsedStart !== null && parsedEnd !== null && parsedStart < parsedEnd;
  }

  private parseTimeToMinutes(timeValue: string): number | null {
    const match = /^([01]?\d|2[0-3]):([0-5]\d)$/.exec((timeValue || '').trim());
    if (!match) {
      return null;
    }

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    return (hours * 60) + minutes;
  }

  private ensureSelectedDoctorVisible(): void {
    if (!this.isAdmin) {
      return;
    }

    const filtered = this.filteredDoctors;
    if (filtered.length === 0) {
      this.selectedDoctorId = '';
      this.timezone = '';
      this.clearAvailabilityForm();
      return;
    }

    if (!filtered.some(doctor => doctor.userId === this.selectedDoctorId)) {
      this.onDoctorChange(filtered[0].userId);
    }
  }

  private clearAvailabilityForm(): void {
    this.weeklyAvailability.clear();
    this.weeklyBreaks.clear();
    this.overrides.clear();
  }

  private normalizeDateValue(value: unknown): string {
    if (value instanceof Date) {
      const y = value.getFullYear();
      const m = String(value.getMonth() + 1).padStart(2, '0');
      const d = String(value.getDate()).padStart(2, '0');
      return `${y}-${m}-${d}`;
    }

    return String(value || '').trim();
  }

  private getTodayDateKey(): string {
    const now = new Date();
    const y = now.getFullYear();
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const d = String(now.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
