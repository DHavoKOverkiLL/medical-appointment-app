import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { UserSummary } from '../dashboard.models';
import { parseIsoDate, toIsoDate } from '../../core/date-time/date-time.utils';

@Component({
  selector: 'app-admin-profile',
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
    MatDatepickerModule,
    MatNativeDateModule,
    TranslateModule
  ],
  templateUrl: './admin-profile.component.html',
  styleUrls: ['./admin-profile.component.scss']
})
export class AdminProfileComponent implements OnInit {
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
      birthDate: [null, Validators.required],
      address: [''],
      phoneNumber: ['']
    });
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  get dashboardRoute(): string {
    return '/dashboard/admin';
  }

  get settingsRoute(): string {
    return '/dashboard/admin/settings';
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const value = this.profileForm.value;
    const birthDate = toIsoDate(value.birthDate);
    if (!birthDate) {
      this.profileForm.controls['birthDate'].setErrors({ invalidDate: true });
      this.saving = false;
      return;
    }

    this.dashboardApi.updateMyProfile({
      firstName: value.firstName,
      lastName: value.lastName,
      address: value.address || '',
      phoneNumber: value.phoneNumber || null,
      birthDate
    }).subscribe({
      next: updated => {
        this.me = updated;
        this.profileForm.patchValue({
          firstName: updated.firstName,
          lastName: updated.lastName,
          birthDate: parseIsoDate(updated.birthDate),
          address: updated.address || '',
          phoneNumber: updated.phoneNumber || ''
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
          birthDate: parseIsoDate(me.birthDate),
          address: me.address || '',
          phoneNumber: me.phoneNumber || ''
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
