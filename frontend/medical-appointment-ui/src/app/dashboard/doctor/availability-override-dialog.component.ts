import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { TranslateModule } from '@ngx-translate/core';
import { parseIsoDate, parseTime, toIsoDate, toTimeString } from '../../core/date-time/date-time.utils';

export interface AvailabilityOverrideDialogData {
  mode: 'create' | 'edit';
  date: string;
  isAvailable: boolean;
  start: string;
  end: string;
  reason: string;
}

export interface AvailabilityOverrideDialogResult {
  date: string;
  isAvailable: boolean;
  start: string;
  end: string;
  reason: string;
}

@Component({
  selector: 'app-availability-override-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTimepickerModule,
    MatButtonModule,
    TranslateModule
  ],
  templateUrl: './availability-override-dialog.component.html',
  styleUrls: ['./availability-override-dialog.component.scss']
})
export class AvailabilityOverrideDialogComponent {
  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<AvailabilityOverrideDialogComponent, AvailabilityOverrideDialogResult>,
    @Inject(MAT_DIALOG_DATA) readonly data: AvailabilityOverrideDialogData
  ) {
    this.form = this.fb.group({
      date: [parseIsoDate(data.date), Validators.required],
      isAvailable: [data.isAvailable, Validators.required],
      start: [parseTime(data.start)],
      end: [parseTime(data.end)],
      reason: [data.reason || '', [Validators.maxLength(250)]]
    });

    this.form.controls['isAvailable'].valueChanges.subscribe(() => {
      this.syncTimeValidators();
    });

    this.syncTimeValidators();
  }

  get titleKey(): string {
    if (this.data.mode === 'create') {
      return 'availability.actions.addOverride';
    }

    return 'availability.dialogs.editOverride';
  }

  get submitLabelKey(): string {
    return this.data.mode === 'create'
      ? 'availability.dialogs.createEntry'
      : 'availability.dialogs.saveEntry';
  }

  close(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const date = toIsoDate(value.date);
    if (!date) {
      this.form.controls['date'].setErrors({ invalidDate: true });
      return;
    }

    const startTime = toTimeString(value.start);
    const endTime = toTimeString(value.end);
    if (value.start && !startTime) {
      this.form.controls['start'].setErrors({ invalidTime: true });
      return;
    }

    if (value.end && !endTime) {
      this.form.controls['end'].setErrors({ invalidTime: true });
      return;
    }

    this.dialogRef.close({
      date,
      isAvailable: value.isAvailable === true,
      start: startTime ?? '',
      end: endTime ?? '',
      reason: String(value.reason || '').trim()
    });
  }

  private syncTimeValidators(): void {
    const isAvailable = this.form.controls['isAvailable'].value === true;
    const startControl = this.form.controls['start'];
    const endControl = this.form.controls['end'];

    if (isAvailable) {
      startControl.addValidators(Validators.required);
      endControl.addValidators(Validators.required);
    } else {
      startControl.removeValidators(Validators.required);
      endControl.removeValidators(Validators.required);
    }

    startControl.updateValueAndValidity({ emitEvent: false });
    endControl.updateValueAndValidity({ emitEvent: false });
  }
}
