import { Routes } from '@angular/router';
import { AuthGuard } from '../auth/auth.guard';
import { RoleGuard } from '../auth/role.guard';

const loadHomeComponent = () =>
  import('./home/home.component').then(m => m.HomeComponent);
const loadAdminDashboardComponent = () =>
  import('./admin/admin-dashboard.component').then(m => m.AdminDashboardComponent);
const loadDoctorDashboardComponent = () =>
  import('./doctor/doctor-dashboard.component').then(m => m.DoctorDashboardComponent);
const loadDoctorAvailabilityComponent = () =>
  import('./doctor/doctor-availability.component').then(m => m.DoctorAvailabilityComponent);
const loadPatientDashboardComponent = () =>
  import('./patient/patient-dashboard.component').then(m => m.PatientDashboardComponent);
const loadPatientProfileComponent = () =>
  import('./patient/patient-profile.component').then(m => m.PatientProfileComponent);
const loadPatientAccountSettingsComponent = () =>
  import('./patient/patient-account-settings.component').then(m => m.PatientAccountSettingsComponent);
const loadDoctorProfileComponent = () =>
  import('./doctor/doctor-profile.component').then(m => m.DoctorProfileComponent);
const loadDoctorAccountSettingsComponent = () =>
  import('./doctor/doctor-account-settings.component').then(m => m.DoctorAccountSettingsComponent);
const loadAdminProfileComponent = () =>
  import('./admin/admin-profile.component').then(m => m.AdminProfileComponent);
const loadAdminAccountSettingsComponent = () =>
  import('./admin/admin-account-settings.component').then(m => m.AdminAccountSettingsComponent);
const loadUserManagementComponent = () =>
  import('./admin/user-management.component').then(m => m.UserManagementComponent);
const loadClinicManagementComponent = () =>
  import('./admin/clinic-management.component').then(m => m.ClinicManagementComponent);
const loadBookAppointmentComponent = () =>
  import('./appointments/book-appointment/book-appointment.component').then(m => m.BookAppointmentComponent);
const loadViewMyAppointmentsComponent = () =>
  import('./appointments/view-my-appointments.component').then(m => m.ViewMyAppointmentsComponent);
const loadViewAllAppointmentsComponent = () =>
  import('./appointments/view-all-appointments.component').then(m => m.ViewAllAppointmentsComponent);
const loadAppointmentAuditComponent = () =>
  import('./appointments/appointment-audit.component').then(m => m.AppointmentAuditComponent);
const loadNotificationsComponent = () =>
  import('./notifications/notifications.component').then(m => m.NotificationsComponent);
const loadViewDoctorAppointmentsComponent = () =>
  import('./appointments/view-doctor-appointments.component').then(m => m.ViewDoctorAppointmentsComponent);

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadHomeComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'admin',
    loadComponent: loadAdminDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'doctor',
    loadComponent: loadDoctorDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'doctor/availability',
    loadComponent: loadDoctorAvailabilityComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'patient',
    loadComponent: loadPatientDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'patient/calendar',
    loadComponent: loadPatientDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'patient/profile',
    loadComponent: loadPatientProfileComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'patient/settings',
    loadComponent: loadPatientAccountSettingsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'doctor/profile',
    loadComponent: loadDoctorProfileComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'doctor/settings',
    loadComponent: loadDoctorAccountSettingsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'admin/profile',
    loadComponent: loadAdminProfileComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/settings',
    loadComponent: loadAdminAccountSettingsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/users',
    loadComponent: loadUserManagementComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/clinics',
    loadComponent: loadClinicManagementComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/availability',
    loadComponent: loadDoctorAvailabilityComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'appointments/book',
    loadComponent: loadBookAppointmentComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'appointments/my',
    loadComponent: loadViewMyAppointmentsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'appointments/all',
    loadComponent: loadViewAllAppointmentsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'appointments/:appointmentId/audit',
    loadComponent: loadAppointmentAuditComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Doctor', 'Patient'] }
  },
  {
    path: 'notifications',
    loadComponent: loadNotificationsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Doctor', 'Patient'] }
  },
  {
    path: 'doctor-appointments',
    loadComponent: loadViewDoctorAppointmentsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: '**',
    redirectTo: ''
  }
];
