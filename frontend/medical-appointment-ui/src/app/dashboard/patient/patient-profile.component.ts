import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { UserSummary } from '../dashboard.models';

@Component({
  selector: 'app-patient-profile',
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
  templateUrl: './patient-profile.component.html',
  styleUrls: ['./patient-profile.component.scss']
})
export class PatientProfileComponent implements OnInit {
  readonly profileSkeletons = Array.from({ length: 3 });
  profileForm: FormGroup;
  me: UserSummary | null = null;
  loading = true;
  saving = false;
  errorMessage = '';
  successMessage = '';

  constructor(private fb: FormBuilder, private dashboardApi: DashboardApiService) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      birthDate: ['', Validators.required],
      address: ['']
    });
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  get roleLabelKey(): string {
    const role = (this.me?.role || '').trim().toLowerCase();
    if (role === 'doctor') return 'roles.doctor';
    if (role === 'admin') return 'roles.admin';
    if (role === 'patient') return 'roles.patient';
    return 'roles.user';
  }

  get dashboardRoute(): string {
    const role = (this.me?.role || '').trim().toLowerCase();

    if (role === 'doctor') return '/dashboard/doctor';
    if (role === 'admin') return '/dashboard/admin';
    return '/dashboard/patient';
  }

  get settingsRoute(): string {
    const role = (this.me?.role || '').trim().toLowerCase();

    if (role === 'doctor') return '/dashboard/doctor/settings';
    if (role === 'admin') return '/dashboard/admin/settings';
    return '/dashboard/patient/settings';
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.updateMyProfile(this.profileForm.value).subscribe({
      next: updated => {
        this.me = updated;
        this.profileForm.patchValue({
          firstName: updated.firstName,
          lastName: updated.lastName,
          birthDate: updated.birthDate ? updated.birthDate.slice(0, 10) : '',
          address: updated.address || ''
        });
        this.successMessage = 'profile.messages.updated';
        this.saving = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'profile.errors.updateFailed';
        this.saving = false;
      }
    });
  }

  private loadProfile(): void {
    this.loading = true;
    this.errorMessage = '';

    this.dashboardApi.getMyProfile().subscribe({
      next: me => {
        this.me = me;
        this.profileForm.patchValue({
          firstName: me.firstName,
          lastName: me.lastName,
          birthDate: me.birthDate ? me.birthDate.slice(0, 10) : '',
          address: me.address || ''
        });
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'profile.errors.loadFailed';
        this.loading = false;
      }
    });
  }
}
