import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { HttpClient, HttpClientModule } from '@angular/common/http';
import { RouterModule, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TranslateModule } from '@ngx-translate/core';
import { API_BASE_URL } from '../../../core/api.config';
import { DashboardApiService } from '../../dashboard-api.service';
import { combineDateAndTime } from '../../../core/date-time/date-time.utils';

interface DoctorOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-book-appointment',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    HttpClientModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatDatepickerModule,
    MatNativeDateModule,
    TranslateModule
],
  templateUrl: './book-appointment.component.html',
  styleUrls: ['./book-appointment.component.scss'],
})
export class BookAppointmentComponent implements OnInit {
  readonly skeletonRows = Array.from({ length: 3 });
  appointmentForm: any;
  doctors: DoctorOption[] = [];
  loadingDoctors = false;
  loadingSlots = false;
  isSubmitting = false;
  availableSlots: string[] = [];
  slotTimezone = '';
  readonly minDate = new Date();
  errorMessage = '';
  successMessage = '';
  private readonly apiBaseUrl = `${API_BASE_URL}/api`;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private dashboardApi: DashboardApiService
  ) {}

  ngOnInit(): void {
    this.loadDoctors();

    this.appointmentForm = this.fb.group({
      doctorId: ['', Validators.required],
      date: [null, Validators.required],
      time: ['', Validators.required],
    });

    this.syncDoctorControlState();
    this.syncTimeControlState();

    this.appointmentForm.get('doctorId')?.valueChanges.subscribe(() => {
      this.reloadAvailableSlots();
    });

    this.appointmentForm.get('date')?.valueChanges.subscribe(() => {
      this.reloadAvailableSlots();
    });
  }

  loadDoctors(): void {
    this.loadingDoctors = true;
    this.syncDoctorControlState();

    this.http.get<DoctorOption[]>(`${this.apiBaseUrl}/User/doctors`).subscribe({
      next: doctors => {
        this.doctors = doctors;
        this.loadingDoctors = false;
        this.syncDoctorControlState();
      },
      error: () => {
        this.errorMessage = 'bookAppointment.errors.loadDoctors';
        this.loadingDoctors = false;
        this.syncDoctorControlState();
      }
    });
  }

  onSubmit(): void {
    if (this.appointmentForm.invalid) {
      this.errorMessage = 'bookAppointment.errors.fillAllFields';
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.isSubmitting = true;
    this.syncDoctorControlState();

    const date = this.appointmentForm.value.date as Date;
    const time = this.appointmentForm.value.time as string;
    if (!this.availableSlots.includes(time)) {
      this.errorMessage = 'bookAppointment.errors.timeUnavailable';
      this.isSubmitting = false;
      return;
    }

    const payload = {
      doctorId: this.appointmentForm.value.doctorId,
      appointmentDateTime: combineDateAndTime(date, time).toISOString(),
    };

    this.http.post(`${this.apiBaseUrl}/Appointment`, payload).subscribe({
      next: () => {
        this.successMessage = 'bookAppointment.messages.booked';
        this.isSubmitting = false;
        this.syncDoctorControlState();
        this.router.navigate(['/dashboard/patient/calendar']);
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'bookAppointment.errors.bookFailed';
        this.isSubmitting = false;
        this.syncDoctorControlState();
      }
    });
  }

  private reloadAvailableSlots(): void {
    this.appointmentForm.patchValue({ time: '' }, { emitEvent: false });
    this.availableSlots = [];
    this.slotTimezone = '';
    this.syncTimeControlState();

    const doctorId = (this.appointmentForm?.value?.doctorId as string) || '';
    const date = this.appointmentForm?.value?.date as Date | null;
    if (!doctorId || !date) {
      return;
    }

    this.loadingSlots = true;
    this.errorMessage = '';
    this.syncTimeControlState();

    this.dashboardApi.getAvailableSlots(doctorId, date).subscribe({
      next: response => {
        this.availableSlots = response.slots.map(slot => slot.localTime);
        this.slotTimezone = response.timezone;
        this.loadingSlots = false;
        this.syncTimeControlState();
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'bookAppointment.errors.loadSlots';
        this.loadingSlots = false;
        this.syncTimeControlState();
      }
    });
  }

  private syncDoctorControlState(): void {
    this.setControlDisabled('doctorId', this.loadingDoctors || this.isSubmitting);
  }

  private syncTimeControlState(): void {
    this.setControlDisabled('time', this.loadingSlots || this.availableSlots.length === 0);
  }

  private setControlDisabled(controlName: 'doctorId' | 'time', disabled: boolean): void {
    const control = this.appointmentForm?.get(controlName);
    if (!control) {
      return;
    }

    if (disabled && control.enabled) {
      control.disable({ emitEvent: false });
      return;
    }

    if (!disabled && control.disabled) {
      control.enable({ emitEvent: false });
    }
  }

}
