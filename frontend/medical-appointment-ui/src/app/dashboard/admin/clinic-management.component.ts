import { Component, OnInit } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import {
  ClinicSummary,
  ClinicUpsertPayload,
  SysAccreditationSummary,
  SysClinicTypeSummary,
  SysOperationSummary,
  SysOwnershipTypeSummary,
  SysSourceSystemSummary
} from '../dashboard.models';
import { ClinicEditorDialogComponent, ClinicEditorDialogData, ClinicEditorDialogResult } from './clinic-editor-dialog.component';

@Component({
  selector: 'app-clinic-management',
  standalone: true,
  imports: [RouterModule, MatCardModule, MatButtonModule, MatDialogModule, TranslateModule],
  templateUrl: './clinic-management.component.html',
  styleUrls: ['./clinic-management.component.scss']
})
export class ClinicManagementComponent implements OnInit {
  readonly skeletonCards = Array.from({ length: 4 });

  clinics: ClinicSummary[] = [];
  sysOperations: SysOperationSummary[] = [];
  sysAccreditations: SysAccreditationSummary[] = [];
  sysClinicTypes: SysClinicTypeSummary[] = [];
  sysOwnershipTypes: SysOwnershipTypeSummary[] = [];
  sysSourceSystems: SysSourceSystemSummary[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly dashboardApi: DashboardApiService,
    private readonly dialog: MatDialog,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadClinics();
    this.loadSystemInfo();
  }

  get totalClinics(): number {
    return this.clinics.length;
  }

  get activeClinics(): number {
    return this.clinics.filter(clinic => clinic.isActive).length;
  }

  get inactiveClinics(): number {
    return this.clinics.filter(clinic => !clinic.isActive).length;
  }

  get mappedUsersCount(): number {
    return this.clinics.reduce((sum, clinic) => sum + clinic.usersCount, 0);
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-chip status-chip--active' : 'status-chip status-chip--inactive';
  }

  trackClinic(_: number, clinic: ClinicSummary): string {
    return clinic.clinicId;
  }

  getClinicTypeDisplayName(clinic: ClinicSummary): string {
    const key = `sys.clinicTypes.${clinic.sysClinicTypeId}`;
    const translated = this.translate.instant(key);
    if (translated && translated !== key) {
      return translated;
    }

    return clinic.clinicType;
  }

  openCreateClinicDialog(): void {
    if (!this.ensureSysLookupsReady()) {
      return;
    }

    const dialogRef = this.dialog.open<ClinicEditorDialogComponent, ClinicEditorDialogData, ClinicEditorDialogResult>(
      ClinicEditorDialogComponent,
      {
        width: '1040px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        panelClass: 'modern-admin-dialog',
        backdropClass: 'modern-admin-backdrop',
        enterAnimationDuration: '210ms',
        exitAnimationDuration: '160ms',
        data: {
          mode: 'create',
          sysOperations: this.sysOperations,
          sysAccreditations: this.sysAccreditations,
          sysClinicTypes: this.sysClinicTypes,
          sysOwnershipTypes: this.sysOwnershipTypes,
          sysSourceSystems: this.sysSourceSystems
        }
      }
    );

    dialogRef.afterClosed().subscribe(result => {
      if (!result || result.mode !== 'create') {
        return;
      }

      this.createClinic(result.payload as ClinicUpsertPayload);
    });
  }

  openEditClinicDialog(clinic: ClinicSummary): void {
    if (!this.ensureSysLookupsReady()) {
      return;
    }

    const dialogRef = this.dialog.open<
      ClinicEditorDialogComponent,
      ClinicEditorDialogData,
      ClinicEditorDialogResult
    >(ClinicEditorDialogComponent, {
      width: '1040px',
      maxWidth: '95vw',
      maxHeight: '92vh',
      panelClass: 'modern-admin-dialog',
      backdropClass: 'modern-admin-backdrop',
      enterAnimationDuration: '210ms',
      exitAnimationDuration: '160ms',
      data: {
        mode: 'edit',
        clinic,
        sysOperations: this.sysOperations,
        sysAccreditations: this.sysAccreditations,
        sysClinicTypes: this.sysClinicTypes,
        sysOwnershipTypes: this.sysOwnershipTypes,
        sysSourceSystems: this.sysSourceSystems
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result || result.mode !== 'edit') {
        return;
      }

      this.updateClinic(clinic.clinicId, result.payload as ClinicUpsertPayload & { isActive: boolean });
    });
  }

  private createClinic(payload: ClinicUpsertPayload): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.createClinic(payload).subscribe({
      next: created => {
        this.clinics = [...this.clinics, created].sort((a, b) => a.name.localeCompare(b.name));
        this.successMessage = 'clinicManagement.messages.created';
        this.loading = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'clinicManagement.errors.createFailed';
        this.loading = false;
      }
    });
  }

  private updateClinic(clinicId: string, payload: ClinicUpsertPayload & { isActive: boolean }): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.updateClinic(clinicId, payload).subscribe({
      next: updated => {
        this.clinics = this.clinics.map(c => (c.clinicId === updated.clinicId ? updated : c));
        this.successMessage = 'clinicManagement.messages.updated';
        this.loading = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'clinicManagement.errors.updateFailed';
        this.loading = false;
      }
    });
  }

  private loadClinics(): void {
    this.loading = true;
    this.errorMessage = '';

    this.dashboardApi.getClinics().subscribe({
      next: clinics => {
        this.clinics = clinics.sort((a, b) => a.name.localeCompare(b.name));
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'clinicManagement.errors.loadClinics';
        this.loading = false;
      }
    });
  }

  private loadSystemInfo(): void {
    this.dashboardApi.getSystemInfo().subscribe({
      next: systemInfo => {
        this.sysOperations = [...(systemInfo.operations ?? [])].sort((a, b) => a.name.localeCompare(b.name));
        this.sysAccreditations = [...(systemInfo.accreditations ?? [])].sort((a, b) => a.name.localeCompare(b.name));
        this.sysClinicTypes = [...(systemInfo.clinicTypes ?? [])].sort((a, b) => a.name.localeCompare(b.name));
        this.sysOwnershipTypes = [...(systemInfo.ownershipTypes ?? [])].sort((a, b) => a.name.localeCompare(b.name));
        this.sysSourceSystems = [...(systemInfo.sourceSystems ?? [])].sort((a, b) => a.name.localeCompare(b.name));
      },
      error: () => {
        this.sysOperations = [];
        this.sysAccreditations = [];
        this.sysClinicTypes = [];
        this.sysOwnershipTypes = [];
        this.sysSourceSystems = [];
        this.errorMessage = 'clinicManagement.errors.loadSystemInfo';
      }
    });
  }

  private ensureSysLookupsReady(): boolean {
    if (this.sysOperations.length === 0) {
      this.errorMessage = 'clinicManagement.errors.operationsUnavailable';
      return false;
    }

    if (this.sysAccreditations.length === 0) {
      this.errorMessage = 'clinicManagement.errors.accreditationsUnavailable';
      return false;
    }

    if (this.sysClinicTypes.length === 0) {
      this.errorMessage = 'clinicManagement.errors.clinicTypesUnavailable';
      return false;
    }

    if (this.sysOwnershipTypes.length === 0) {
      this.errorMessage = 'clinicManagement.errors.ownershipTypesUnavailable';
      return false;
    }

    if (this.sysSourceSystems.length === 0) {
      this.errorMessage = 'clinicManagement.errors.sourceSystemsUnavailable';
      return false;
    }

    return true;
  }
}
