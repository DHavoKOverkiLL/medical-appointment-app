import { Component, OnInit } from '@angular/core';

import { RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DashboardApiService } from '../dashboard-api.service';
import { ClinicSummary, RoleSummary, UserSummary } from '../dashboard.models';
import {
  CreateUserDialogPayload,
  EditUserDialogPayload,
  UserEditorDialogComponent,
  UserEditorDialogResult
} from './user-editor-dialog.component';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [
    RouterModule,
    MatFormFieldModule,
    MatButtonModule,
    MatSelectModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    TranslateModule
],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss']
})
export class UserManagementComponent implements OnInit {
  readonly skeletonRows = Array.from({ length: 5 });

  users: UserSummary[] = [];
  roles: RoleSummary[] = [];
  clinics: ClinicSummary[] = [];
  pendingRoleByUserId: Record<string, string> = {};
  savingRoleUserId: string | null = null;

  selectedClinicFilter = 'all';

  loading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly dashboardApi: DashboardApiService,
    private readonly dialog: MatDialog,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  get displayedUsers(): UserSummary[] {
    if (this.selectedClinicFilter === 'all') {
      return this.users;
    }

    return this.users.filter(u => u.clinicId === this.selectedClinicFilter);
  }

  get totalUsers(): number {
    return this.users.length;
  }

  get doctorsCount(): number {
    return this.countByRole('Doctor');
  }

  get patientsCount(): number {
    return this.countByRole('Patient');
  }

  get adminsCount(): number {
    return this.countByRole('Admin');
  }

  get selectedClinicLabel(): string {
    if (this.selectedClinicFilter === 'all') {
      return 'common.allClinics';
    }

    return this.clinics.find(clinic => clinic.clinicId === this.selectedClinicFilter)?.name || 'common.clinic';
  }

  getRoleBadgeClass(role: string): string {
    const normalized = (role || '').trim().toLowerCase();
    if (normalized === 'admin') return 'role-badge role-badge--admin';
    if (normalized === 'doctor') return 'role-badge role-badge--doctor';
    if (normalized === 'patient') return 'role-badge role-badge--patient';
    return 'role-badge role-badge--default';
  }

  openCreateUserDialog(): void {
    if (this.clinics.length === 0 || this.roles.length === 0) {
      this.errorMessage = 'userManagement.errors.dataRequiredForCreate';
      return;
    }

    const dialogRef = this.dialog.open<UserEditorDialogComponent, { mode: 'create'; roles: RoleSummary[]; clinics: ClinicSummary[] }, UserEditorDialogResult>(
      UserEditorDialogComponent,
      {
        width: '920px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        panelClass: 'modern-admin-dialog',
        backdropClass: 'modern-admin-backdrop',
        enterAnimationDuration: '210ms',
        exitAnimationDuration: '160ms',
        data: {
          mode: 'create',
          roles: this.roles,
          clinics: this.clinics
        }
      }
    );

    dialogRef.afterClosed().subscribe(result => {
      if (!result || result.mode !== 'create') {
        return;
      }

      this.createUser(result.payload as CreateUserDialogPayload);
    });
  }

  openEditUserDialog(user: UserSummary): void {
    if (this.clinics.length === 0 || this.roles.length === 0) {
      this.errorMessage = 'userManagement.errors.dataRequiredForEdit';
      return;
    }

    const dialogRef = this.dialog.open<
      UserEditorDialogComponent,
      { mode: 'edit'; roles: RoleSummary[]; clinics: ClinicSummary[]; user: UserSummary },
      UserEditorDialogResult
    >(UserEditorDialogComponent, {
      width: '920px',
      maxWidth: '95vw',
      maxHeight: '92vh',
      panelClass: 'modern-admin-dialog',
      backdropClass: 'modern-admin-backdrop',
      enterAnimationDuration: '210ms',
      exitAnimationDuration: '160ms',
      data: {
        mode: 'edit',
        roles: this.roles,
        clinics: this.clinics,
        user
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result || result.mode !== 'edit') {
        return;
      }

      this.updateUser(user.userId, result.payload as EditUserDialogPayload);
    });
  }

  onRoleSelectionChange(userId: string, roleName: string): void {
    this.pendingRoleByUserId[userId] = roleName;
  }

  trackUser(_: number, user: UserSummary): string {
    return user.userId;
  }

  isSaveRoleDisabled(user: UserSummary): boolean {
    const targetRole = this.pendingRoleByUserId[user.userId] || user.role;
    return this.loading || targetRole === user.role;
  }

  saveRole(user: UserSummary): void {
    const targetRole = this.pendingRoleByUserId[user.userId] || user.role;
    if (targetRole === user.role) {
      return;
    }

    this.loading = true;
    this.savingRoleUserId = user.userId;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.updateUserRole(user.userId, targetRole)
      .pipe(finalize(() => {
        this.loading = false;
        this.savingRoleUserId = null;
      }))
      .subscribe({
        next: () => {
          this.users = this.users.map(u => (u.userId === user.userId ? { ...u, role: targetRole } : u));
          this.successMessage = this.translate.instant('userManagement.messages.roleUpdated', { email: user.email });
        },
        error: err => {
          this.errorMessage = err?.error?.message || err?.error || 'userManagement.errors.roleUpdateFailed';
        }
      });
  }

  private createUser(payload: CreateUserDialogPayload): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.createUser(payload).subscribe({
      next: created => {
        this.users = [created, ...this.users];
        this.pendingRoleByUserId[created.userId] = created.role;
        this.successMessage = 'userManagement.messages.userCreated';
        this.loading = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'userManagement.errors.createFailed';
        this.loading = false;
      }
    });
  }

  private updateUser(userId: string, payload: EditUserDialogPayload): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.dashboardApi.updateUser(userId, payload).subscribe({
      next: updated => {
        this.users = this.users.map(u => (u.userId === updated.userId ? { ...u, ...updated } : u));
        this.successMessage = 'userManagement.messages.userUpdated';
        this.loading = false;
      },
      error: err => {
        this.errorMessage = err?.error?.message || err?.error || 'userManagement.errors.updateFailed';
        this.loading = false;
      }
    });
  }

  private loadData(): void {
    this.loading = true;
    this.errorMessage = '';

    forkJoin({
      users: this.dashboardApi.getUsers(),
      systemInfo: this.dashboardApi.getSystemInfo(),
      clinics: this.dashboardApi.getClinics()
    }).subscribe({
      next: result => {
        this.users = result.users;
        this.roles = result.systemInfo.roles ?? [];
        this.clinics = result.clinics;
        this.pendingRoleByUserId = result.users.reduce<Record<string, string>>((acc, user) => {
          acc[user.userId] = user.role;
          return acc;
        }, {});
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'userManagement.errors.loadDataFailed';
        this.loading = false;
      }
    });
  }

  private countByRole(roleName: string): number {
    return this.users.filter(user => user.role.trim().toLowerCase() === roleName.toLowerCase()).length;
  }
}
