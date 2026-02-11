import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpClientModule, HttpErrorResponse } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { API_BASE_URL } from '../../core/api.config';
import { toIsoDate } from '../../core/date-time/date-time.utils';

interface RegisterPayload {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  personalIdentifier: string;
  address: string;
  phoneNumber?: string | null;
  birthDate: string;
  clinicId: string;
}

interface ClinicOption {
  clinicId: string;
  name: string;
  code: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    HttpClientModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    TranslateModule
  ],
  templateUrl: './register.component.html',
  styles: [':host { display: block; }']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  clinics: ClinicOption[] = [];
  loadingClinics = true;
  isSubmitting = false;
  errorMessage = '';
  private readonly clinicsEndpoint = `${API_BASE_URL}/api/Clinic/public`;
  private readonly registerEndpoint = `${API_BASE_URL}/api/User/register`;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private translate: TranslateService
  ) {
    this.registerForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      personalIdentifier: ['', Validators.required],
      address: [''],
      phoneNumber: [''],
      birthDate: [null, Validators.required],
      clinicId: [{ value: '', disabled: true }, Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadClinics();
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;
    this.errorMessage = '';
    this.isSubmitting = true;

    const value = this.registerForm.value;
    const birthDate = toIsoDate(value.birthDate);
    if (!birthDate) {
      this.registerForm.controls['birthDate'].setErrors({ invalidDate: true });
      this.isSubmitting = false;
      return;
    }

    const payload: RegisterPayload = {
      username: value.username,
      email: value.email,
      password: value.password,
      firstName: value.firstName,
      lastName: value.lastName,
      personalIdentifier: value.personalIdentifier,
      address: value.address || '',
      phoneNumber: value.phoneNumber || null,
      birthDate,
      clinicId: value.clinicId
    };

    this.http.post(this.registerEndpoint, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/login']);
      },
      error: err => {
        this.errorMessage = this.getRegisterErrorMessage(err);
        this.isSubmitting = false;
      }
    });
  }

  private loadClinics(): void {
    this.loadingClinics = true;
    const clinicControl = this.registerForm.get('clinicId');
    clinicControl?.disable({ emitEvent: false });

    this.http.get<ClinicOption[]>(this.clinicsEndpoint).subscribe({
      next: clinics => {
        this.clinics = clinics;

        if (clinics.length === 1) {
          this.registerForm.patchValue({ clinicId: clinics[0].clinicId });
        }

        this.loadingClinics = false;
        clinicControl?.enable({ emitEvent: false });
      },
      error: () => {
        this.errorMessage = this.translate.instant('auth.register.errors.loadClinics');
        this.loadingClinics = false;
        clinicControl?.enable({ emitEvent: false });
      }
    });
  }

  private getRegisterErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return this.translate.instant('errors.apiUnavailable');
    }

    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }

    const validationErrors = error.error?.errors as Record<string, string[]> | undefined;
    if (validationErrors) {
      const firstError = Object.values(validationErrors).flat()[0];
      if (firstError) return firstError;
    }

    if (error.error?.message) return error.error.message;
    if (error.error?.title) return error.error.title;

    if (error.status === 409) {
      return this.translate.instant('auth.register.errors.conflict');
    }

    return this.translate.instant('auth.register.errors.failed');
  }
}
