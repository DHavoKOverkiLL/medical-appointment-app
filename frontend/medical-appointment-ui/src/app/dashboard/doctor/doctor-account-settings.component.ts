import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';

@Component({
  selector: 'app-doctor-account-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './doctor-account-settings.component.html',
  styleUrls: ['./doctor-account-settings.component.scss']
})
export class DoctorAccountSettingsComponent implements OnInit {
  readonly settingsSkeletons = Array.from({ length: 3 });
  settingsForm: FormGroup;
  loading = true;
  saving = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private dashboardApi: DashboardApiService,
    private translate: TranslateService
  ) {
    this.settingsForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      currentPassword: ['', [Validators.required, Validators.minLength(8)]],
      newPassword: ['', [Validators.minLength(8)]],
      confirmNewPassword: ['']
    }, { validators: this.newPasswordsMatchValidator() });
  }

  ngOnInit(): void {
    this.loadAccount();
  }

  get dashboardRoute(): string {
    return '/dashboard/doctor';
  }

  get profileRoute(): string {
    return '/dashboard/doctor/profile';
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) {
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.settingsForm.value;
    const payload = {
      username: formValue.username as string,
      email: formValue.email as string,
      currentPassword: formValue.currentPassword as string,
      newPassword: (formValue.newPassword as string)?.trim() || undefined
    };

    this.dashboardApi.updateMyAccountSettings(payload).subscribe({
      next: response => {
        this.successMessage = response.message || this.translate.instant('doctorAccountSettings.messages.updated');
        this.settingsForm.patchValue({
          currentPassword: '',
          newPassword: '',
          confirmNewPassword: ''
        });
        this.saving = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'doctorAccountSettings.errors.updateFailed';
        this.saving = false;
      }
    });
  }

  private loadAccount(): void {
    this.loading = true;
    this.errorMessage = '';

    this.dashboardApi.getMyProfile().subscribe({
      next: me => {
        this.settingsForm.patchValue({
          username: me.username,
          email: me.email
        });
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'doctorAccountSettings.errors.loadFailed';
        this.loading = false;
      }
    });
  }

  private newPasswordsMatchValidator(): ValidatorFn {
    return (control: AbstractControl) => {
      const newPassword = control.get('newPassword')?.value as string;
      const confirmNewPassword = control.get('confirmNewPassword')?.value as string;

      if (!newPassword && !confirmNewPassword) {
        return null;
      }

      return newPassword === confirmNewPassword ? null : { passwordsMismatch: true };
    };
  }
}
