
import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslateModule } from '@ngx-translate/core';
import { ClinicSummary, RoleSummary, UserSummary } from '../dashboard.models';
import { parseIsoDate, toIsoDate } from '../../core/date-time/date-time.utils';

export interface CreateUserDialogPayload {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  personalIdentifier: string;
  address: string;
  phoneNumber?: string | null;
  birthDate: string;
  roleName: string;
  clinicId: string;
}

export interface EditUserDialogPayload {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  personalIdentifier: string;
  address: string;
  phoneNumber?: string | null;
  birthDate: string;
  clinicId: string;
}

export interface UserEditorDialogData {
  mode: 'create' | 'edit';
  roles: RoleSummary[];
  clinics: ClinicSummary[];
  user?: UserSummary;
}

export interface UserEditorDialogResult {
  mode: 'create' | 'edit';
  payload: CreateUserDialogPayload | EditUserDialogPayload;
}

@Component({
  selector: 'app-user-editor-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    TranslateModule
],
  templateUrl: './user-editor-dialog.component.html',
  styleUrls: ['./user-editor-dialog.component.scss']
})
export class UserEditorDialogComponent {
  readonly form: FormGroup;
  readonly isEditMode: boolean;
  activeTabIndex = 0;

  constructor(
    private readonly fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<UserEditorDialogComponent, UserEditorDialogResult>,
    @Inject(MAT_DIALOG_DATA) readonly data: UserEditorDialogData
  ) {
    this.isEditMode = data.mode === 'edit';
    this.form = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      roleName: ['', Validators.required],
      clinicId: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      personalIdentifier: ['', Validators.required],
      birthDate: [null, Validators.required],
      address: [''],
      phoneNumber: ['']
    });

    if (this.isEditMode && data.user) {
      this.form.patchValue({
        username: data.user.username,
        email: data.user.email,
        roleName: data.user.role,
        clinicId: data.user.clinicId,
        firstName: data.user.firstName,
        lastName: data.user.lastName,
        personalIdentifier: data.user.personalIdentifier,
        birthDate: parseIsoDate(data.user.birthDate),
        address: data.user.address,
        phoneNumber: data.user.phoneNumber ?? ''
      });

      this.form.get('password')?.clearValidators();
      this.form.get('password')?.setValue('');
      this.form.get('password')?.updateValueAndValidity();
    } else {
      this.form.patchValue({
        roleName: data.roles[0]?.name ?? '',
        clinicId: data.clinics[0]?.clinicId ?? ''
      });
    }
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'userEditor.dialogTitleEdit' : 'userEditor.dialogTitleCreate';
  }

  get submitLabel(): string {
    return this.isEditMode ? 'actions.saveChanges' : 'actions.createUser';
  }

  get selectedClinicName(): string {
    const clinicId = this.form.get('clinicId')?.value as string | null;
    return this.data.clinics.find(clinic => clinic.clinicId === clinicId)?.name || 'userEditor.noClinicSelected';
  }

  close(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const birthDate = toIsoDate(raw.birthDate);
    if (!birthDate)
    {
      this.form.get('birthDate')?.setErrors({ invalidDate: true });
      return;
    }

    if (this.isEditMode) {
      const payload: EditUserDialogPayload = {
        username: raw.username,
        email: raw.email,
        firstName: raw.firstName,
        lastName: raw.lastName,
        personalIdentifier: raw.personalIdentifier,
        address: raw.address || '',
        phoneNumber: raw.phoneNumber || null,
        birthDate,
        clinicId: raw.clinicId
      };

      this.dialogRef.close({ mode: 'edit', payload });
      return;
    }

    const payload: CreateUserDialogPayload = {
      username: raw.username,
      email: raw.email,
      password: raw.password,
      firstName: raw.firstName,
      lastName: raw.lastName,
      personalIdentifier: raw.personalIdentifier,
      address: raw.address || '',
      phoneNumber: raw.phoneNumber || null,
      birthDate,
      roleName: raw.roleName,
      clinicId: raw.clinicId
    };

    this.dialogRef.close({ mode: 'create', payload });
  }
}
