import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AuthGuard } from '../auth/auth.guard';
import { RoleGuard } from '../auth/role.guard';
import { BookAppointmentComponent } from './appointments/book-appointment/book-appointment.component';
import { ViewDoctorAppointmentsComponent } from './appointments/view-doctor-appointments.component';
import { ViewMyAppointmentsComponent } from './appointments/view-my-appointments.component';
import { ViewAllAppointmentsComponent } from './appointments/view-all-appointments.component';
import { AppointmentAuditComponent } from './appointments/appointment-audit.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { DoctorDashboardComponent } from './doctor/doctor-dashboard.component';
import { DoctorAvailabilityComponent } from './doctor/doctor-availability.component';
import { PatientDashboardComponent } from './patient/patient-dashboard.component';
import { PatientProfileComponent } from './patient/patient-profile.component';
import { PatientAccountSettingsComponent } from './patient/patient-account-settings.component';
import { UserManagementComponent } from './admin/user-management.component';
import { ClinicManagementComponent } from './admin/clinic-management.component';
import { NotificationsComponent } from './notifications/notifications.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: HomeComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'admin',
    component: AdminDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'doctor',
    component: DoctorDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'doctor/availability',
    component: DoctorAvailabilityComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'patient',
    component: PatientDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'patient/calendar',
    component: PatientDashboardComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'patient/profile',
    component: PatientProfileComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'patient/settings',
    component: PatientAccountSettingsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'doctor/profile',
    component: PatientProfileComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'doctor/settings',
    component: PatientAccountSettingsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: 'admin/profile',
    component: PatientProfileComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/settings',
    component: PatientAccountSettingsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/users',
    component: UserManagementComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/clinics',
    component: ClinicManagementComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'admin/availability',
    component: DoctorAvailabilityComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'appointments/book',
    component: BookAppointmentComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'appointments/my',
    component: ViewMyAppointmentsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Patient'] }
  },
  {
    path: 'appointments/all',
    component: ViewAllAppointmentsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'appointments/:appointmentId/audit',
    component: AppointmentAuditComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Doctor', 'Patient'] }
  },
  {
    path: 'notifications',
    component: NotificationsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'Doctor', 'Patient'] }
  },
  {
    path: 'doctor-appointments',
    component: ViewDoctorAppointmentsComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Doctor'] }
  },
  {
    path: '**',
    redirectTo: ''
  }
];
