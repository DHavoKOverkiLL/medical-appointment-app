import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { NotificationStateService } from '../../core/notifications/notification-state.service';

interface NavItem {
  labelKey: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit, OnDestroy {
  private readonly patientNavItems: NavItem[] = [
    { labelKey: 'nav.dashboard', route: '/dashboard/patient', exact: true },
    { labelKey: 'nav.calendar', route: '/dashboard/patient/calendar', exact: true },
    { labelKey: 'nav.book', route: '/dashboard/appointments/book', exact: true },
    { labelKey: 'nav.notifications', route: '/dashboard/notifications', exact: true },
    { labelKey: 'nav.profile', route: '/dashboard/patient/profile', exact: true },
    { labelKey: 'nav.settings', route: '/dashboard/patient/settings', exact: true }
  ];

  private readonly doctorNavItems: NavItem[] = [
    { labelKey: 'nav.dashboard', route: '/dashboard/doctor', exact: true },
    { labelKey: 'nav.availability', route: '/dashboard/doctor/availability', exact: true },
    { labelKey: 'nav.consults', route: '/dashboard/doctor-appointments', exact: true },
    { labelKey: 'nav.notifications', route: '/dashboard/notifications', exact: true },
    { labelKey: 'nav.profile', route: '/dashboard/doctor/profile', exact: true },
    { labelKey: 'nav.settings', route: '/dashboard/doctor/settings', exact: true }
  ];

  private readonly adminNavItems: NavItem[] = [
    { labelKey: 'nav.dashboard', route: '/dashboard/admin', exact: true },
    { labelKey: 'nav.availability', route: '/dashboard/admin/availability', exact: true },
    { labelKey: 'nav.notifications', route: '/dashboard/notifications', exact: true },
    { labelKey: 'nav.profile', route: '/dashboard/admin/profile', exact: true },
    { labelKey: 'nav.settings', route: '/dashboard/admin/settings', exact: true },
    { labelKey: 'nav.users', route: '/dashboard/admin/users', exact: true },
    { labelKey: 'nav.clinics', route: '/dashboard/admin/clinics', exact: true },
    { labelKey: 'nav.appointments', route: '/dashboard/appointments/all', exact: true }
  ];

  private readonly defaultNavItems: NavItem[] = [{ labelKey: 'nav.dashboard', route: '/dashboard', exact: true }];
  unreadCount = 0;
  badgePulse = false;
  private unreadCountSubscription: Subscription | null = null;
  private previousUnreadCount = 0;
  private pulseTimerId: ReturnType<typeof setTimeout> | null = null;

  constructor(
    public auth: AuthService,
    private router: Router,
    private notificationState: NotificationStateService
  ) {}

  ngOnInit(): void {
    if (!this.isLoggedIn) {
      return;
    }

    this.unreadCountSubscription = this.notificationState.unreadCount$.subscribe(count => {
      this.unreadCount = count;
      if (count > this.previousUnreadCount) {
        this.triggerBadgePulse();
      }

      this.previousUnreadCount = count;
    });
    this.notificationState.startPolling();
  }

  ngOnDestroy(): void {
    this.unreadCountSubscription?.unsubscribe();
    this.unreadCountSubscription = null;
    if (this.pulseTimerId) {
      clearTimeout(this.pulseTimerId);
      this.pulseTimerId = null;
    }
    this.notificationState.stopPolling();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  get isLoggedIn(): boolean {
    return this.auth.isLoggedIn();
  }

  get userEmail(): string | null {
    return this.auth.getUserEmail();
  }

  get role(): string | null {
    const role = this.auth.getUserRole();
    return role ? role.trim() : null;
  }

  get normalizedRole(): string | null {
    return this.auth.getUserRoleNormalized();
  }

  get roleLabelKey(): string {
    const normalizedRole = this.normalizedRole;

    if (normalizedRole === 'admin') return 'roles.admin';
    if (normalizedRole === 'doctor') return 'roles.doctor';
    if (normalizedRole === 'patient') return 'roles.patient';

    return 'roles.user';
  }

  get clinicName(): string | null {
    return this.auth.getClinicName();
  }

  get dashboardRoute(): string {
    const role = this.normalizedRole;

    if (role === 'admin') return '/dashboard/admin';
    if (role === 'doctor') return '/dashboard/doctor';
    if (role === 'patient') return '/dashboard/patient';

    return '/dashboard';
  }

  get navItems(): NavItem[] {
    const role = this.normalizedRole;

    if (role === 'patient') {
      return this.patientNavItems;
    }

    if (role === 'doctor') {
      return this.doctorNavItems;
    }

    if (role === 'admin') {
      return this.adminNavItems;
    }

    return this.defaultNavItems;
  }

  trackByRoute(_: number, item: NavItem): string {
    return item.route;
  }

  isNotificationsItem(item: NavItem): boolean {
    return item.route === '/dashboard/notifications';
  }

  get unreadCountLabel(): string {
    return this.unreadCount > 99 ? '99+' : String(this.unreadCount);
  }

  private triggerBadgePulse(): void {
    if (this.unreadCount <= 0) {
      return;
    }

    this.badgePulse = false;
    if (this.pulseTimerId) {
      clearTimeout(this.pulseTimerId);
      this.pulseTimerId = null;
    }

    setTimeout(() => {
      this.badgePulse = true;
      this.pulseTimerId = setTimeout(() => {
        this.badgePulse = false;
        this.pulseTimerId = null;
      }, 1200);
    }, 0);
  }
}
