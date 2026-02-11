import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
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

interface DoctorOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-book-appointment',
  standalone: true,
  imports: [
    CommonModule,
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

    this.appointmentForm.get('doctorId')?.valueChanges.subscribe(() => {
      this.reloadAvailableSlots();
    });

    this.appointmentForm.get('date')?.valueChanges.subscribe(() => {
      this.reloadAvailableSlots();
    });
  }

  loadDoctors(): void {
    this.loadingDoctors = true;
    this.http.get<DoctorOption[]>(`${this.apiBaseUrl}/User/doctors`).subscribe({
      next: doctors => {
        this.doctors = doctors;
        this.loadingDoctors = false;
      },
      error: () => {
        this.errorMessage = 'bookAppointment.errors.loadDoctors';
        this.loadingDoctors = false;
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

    const date = this.appointmentForm.value.date as Date;
    const time = this.appointmentForm.value.time as string;
    if (!this.availableSlots.includes(time)) {
      this.errorMessage = 'bookAppointment.errors.timeUnavailable';
      this.isSubmitting = false;
      return;
    }

    const payload = {
      doctorId: this.appointmentForm.value.doctorId,
      appointmentDateTime: this.combineDateAndTime(date, time).toISOString(),
    };

    this.http.post(`${this.apiBaseUrl}/Appointment`, payload).subscribe({
      next: () => {
        this.successMessage = 'bookAppointment.messages.booked';
        this.isSubmitting = false;
        this.router.navigate(['/dashboard/patient/calendar']);
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'bookAppointment.errors.bookFailed';
        this.isSubmitting = false;
      }
    });
  }

  private reloadAvailableSlots(): void {
    this.appointmentForm.patchValue({ time: '' }, { emitEvent: false });
    this.availableSlots = [];
    this.slotTimezone = '';

    const doctorId = (this.appointmentForm?.value?.doctorId as string) || '';
    const date = this.appointmentForm?.value?.date as Date | null;
    if (!doctorId || !date) {
      return;
    }

    this.loadingSlots = true;
    this.errorMessage = '';

    this.dashboardApi.getAvailableSlots(doctorId, date).subscribe({
      next: response => {
        this.availableSlots = response.slots.map(slot => slot.localTime);
        this.slotTimezone = response.timezone;
        this.loadingSlots = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'bookAppointment.errors.loadSlots';
        this.loadingSlots = false;
      }
    });
  }

  private combineDateAndTime(date: Date, time: string): Date {
    const [hours, minutes] = time.split(':').map(v => Number(v));
    return new Date(
      date.getFullYear(),
      date.getMonth(),
      date.getDate(),
      hours,
      minutes,
      0,
      0
    );
  }
}
