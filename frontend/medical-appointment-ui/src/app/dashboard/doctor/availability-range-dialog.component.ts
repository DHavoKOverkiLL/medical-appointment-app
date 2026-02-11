import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { TranslateModule } from '@ngx-translate/core';
import { parseTime, toTimeString } from '../../core/date-time/date-time.utils';

export type AvailabilityRangeDialogContext = 'availability' | 'break';

export interface AvailabilityRangeDialogData {
  mode: 'create' | 'edit';
  context: AvailabilityRangeDialogContext;
  dayOfWeek: number;
  start: string;
  end: string;
  dayOptions: ReadonlyArray<{ value: number; labelKey: string }>;
}

export interface AvailabilityRangeDialogResult {
  dayOfWeek: number;
  start: string;
  end: string;
}

@Component({
  selector: 'app-availability-range-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatNativeDateModule,
    MatInputModule,
    MatSelectModule,
    MatTimepickerModule,
    MatButtonModule,
    TranslateModule
  ],
  templateUrl: './availability-range-dialog.component.html',
  styleUrls: ['./availability-range-dialog.component.scss']
})
export class AvailabilityRangeDialogComponent {
  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<AvailabilityRangeDialogComponent, AvailabilityRangeDialogResult>,
    @Inject(MAT_DIALOG_DATA) readonly data: AvailabilityRangeDialogData
  ) {
    this.form = this.fb.group({
      dayOfWeek: [data.dayOfWeek, [Validators.required, Validators.min(0), Validators.max(6)]],
      start: [parseTime(data.start), [Validators.required]],
      end: [parseTime(data.end), [Validators.required]]
    });
  }

  get titleKey(): string {
    if (this.data.mode === 'create') {
      return this.data.context === 'availability'
        ? 'availability.actions.addAvailabilityWindow'
        : 'availability.actions.addBreak';
    }

    return this.data.context === 'availability'
      ? 'availability.dialogs.editAvailabilityWindow'
      : 'availability.dialogs.editBreak';
  }

  get subtitleKey(): string {
    return this.data.context === 'availability'
      ? 'availability.hints.weeklyAvailability'
      : 'availability.hints.weeklyBreaks';
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
    const start = toTimeString(value.start);
    const end = toTimeString(value.end);
    if (!start || !end) {
      this.form.controls['start'].setErrors({ invalidTime: true });
      this.form.controls['end'].setErrors({ invalidTime: true });
      return;
    }

    this.dialogRef.close({
      dayOfWeek: Number(value.dayOfWeek),
      start,
      end
    });
  }
}
