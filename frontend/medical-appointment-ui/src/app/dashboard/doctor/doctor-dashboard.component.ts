import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { DoctorDashboardResponse } from '../dashboard.models';

@Component({
  selector: 'app-doctor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, TranslateModule],
  templateUrl: './doctor-dashboard.component.html',
  styleUrls: ['./doctor-dashboard.component.scss']
})
export class DoctorDashboardComponent implements OnInit {
  readonly skeletonCards = Array.from({ length: 5 });
  summary: DoctorDashboardResponse | null = null;
  loading = true;
  errorMessage = '';

  constructor(private dashboardApi: DashboardApiService) {}

  ngOnInit(): void {
    this.dashboardApi.getDoctorDashboard().subscribe({
      next: response => {
        this.summary = response;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'doctorDashboard.errors.loadDashboard';
        this.loading = false;
      }
    });
  }

  get throughput(): string {
    if (!this.summary || this.summary.upcomingAppointments === 0) {
      return 'doctorDashboard.throughput.stable';
    }

    if (this.summary.appointmentsToday >= 8) {
      return 'doctorDashboard.throughput.high';
    }

    if (this.summary.appointmentsToday >= 4) {
      return 'doctorDashboard.throughput.moderate';
    }

    return 'doctorDashboard.throughput.low';
  }
}
