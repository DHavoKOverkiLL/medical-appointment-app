import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { PatientAppointment } from '../dashboard.models';
import { combineDateAndTime, toTimeInputValue } from '../../core/date-time/date-time.utils';

export interface PostponeRequestDialogData {
  appointment: PatientAppointment;
}

export interface PostponeRequestDialogResult {
  sent: boolean;
}

@Component({
  selector: 'app-postpone-request-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './postpone-request-dialog.component.html',
  styleUrls: ['./postpone-request-dialog.component.scss']
})
export class PostponeRequestDialogComponent {
  readonly minDate = new Date();
  readonly form: FormGroup;

  loadingSlots = false;
  submitting = false;
  availableSlots: string[] = [];
  slotTimezone = '';
  errorMessage = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly dashboardApi: DashboardApiService,
    private readonly dialogRef: MatDialogRef<PostponeRequestDialogComponent, PostponeRequestDialogResult>,
    @Inject(MAT_DIALOG_DATA) readonly data: PostponeRequestDialogData
  ) {
    const source = new Date(data.appointment.proposedDateTime || data.appointment.appointmentDateTime);
    source.setDate(source.getDate() + 1);

    this.form = this.fb.group({
      date: [source, Validators.required],
      time: ['', Validators.required],
      reason: [data.appointment.postponeReason || '', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });

    this.form.get('date')?.valueChanges.subscribe(() => {
      this.reloadSlots();
    });

    this.reloadSlots();
  }

  close(): void {
    if (this.submitting) {
      return;
    }

    this.dialogRef.close();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const date = this.normalizeDate(this.form.value.date);
    const time = String(this.form.value.time || '').trim();
    const reason = String(this.form.value.reason || '').trim();

    if (!date) {
      this.errorMessage = 'patientDashboard.errors.timeUnavailable';
      return;
    }

    if (!this.availableSlots.includes(time)) {
      this.errorMessage = 'patientDashboard.errors.timeUnavailable';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';

    this.dashboardApi.requestPostponeAppointment(this.data.appointment.appointmentId, {
      proposedDateTime: combineDateAndTime(date, time).toISOString(),
      reason
    }).subscribe({
      next: () => {
        this.submitting = false;
        this.dialogRef.close({ sent: true });
      },
      error: err => {
        this.submitting = false;
        this.errorMessage = err?.error?.message || err?.error || 'patientDashboard.errors.postponeFailed';
      }
    });
  }

  private reloadSlots(): void {
    this.form.patchValue({ time: '' }, { emitEvent: false });
    this.availableSlots = [];
    this.slotTimezone = '';

    const date = this.normalizeDate(this.form.value.date);
    if (!date) {
      return;
    }

    this.loadingSlots = true;
    this.errorMessage = '';

    this.dashboardApi.getAvailableSlots(this.data.appointment.doctorId, date).subscribe({
      next: response => {
        this.availableSlots = response.slots.map(slot => slot.localTime);
        this.slotTimezone = response.timezone;
        this.loadingSlots = false;
      },
      error: err => {
        this.loadingSlots = false;
        this.errorMessage = err?.error?.message || err?.error || 'patientDashboard.errors.loadSlots';
      }
    });
  }

  private normalizeDate(value: unknown): Date | null {
    if (value instanceof Date && !Number.isNaN(value.getTime())) {
      return value;
    }

    if (typeof value === 'string' || typeof value === 'number') {
      const parsed = new Date(value);
      return Number.isNaN(parsed.getTime()) ? null : parsed;
    }

    return null;
  }
}
