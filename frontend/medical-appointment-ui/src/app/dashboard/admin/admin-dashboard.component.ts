import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { AdminDashboardResponse, ClinicLoadResponse } from '../dashboard.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, TranslateModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  readonly metricSkeletons = Array.from({ length: 7 });
  readonly clinicSkeletons = Array.from({ length: 4 });
  summary: AdminDashboardResponse | null = null;
  loading = true;
  errorMessage = '';

  constructor(private dashboardApi: DashboardApiService) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  trackClinic(_: number, item: ClinicLoadResponse): string {
    return item.clinicId;
  }

  private loadDashboard(): void {
    this.loading = true;
    this.errorMessage = '';

    this.dashboardApi.getAdminDashboard().subscribe({
      next: summary => {
        this.summary = summary;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'adminDashboard.errors.loadFailed';
      }
    });
  }
}
