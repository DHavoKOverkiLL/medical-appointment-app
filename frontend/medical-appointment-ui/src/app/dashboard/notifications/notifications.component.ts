import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription, interval } from 'rxjs';
import { API_BASE_URL } from '../../core/api.config';
import { NotificationStateService } from '../../core/notifications/notification-state.service';
import { NotificationPreferencesService } from '../../core/notifications/notification-preferences.service';

interface NotificationRow {
  userNotificationId: string;
  userId: string;
  appointmentId: string | null;
  actorUserId: string | null;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
}

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit, OnDestroy {
  notifications: NotificationRow[] = [];
  loading = true;
  markingAll = false;
  markingNotificationId: string | null = null;
  errorMessage = '';
  successMessage = '';
  soundAlertsEnabled = false;
  vibrationAlertsEnabled = false;
  private pollSubscription: Subscription | null = null;
  private preferencesSubscription: Subscription | null = null;

  private readonly endpoint = `${API_BASE_URL}/api/Notification`;

  constructor(
    private http: HttpClient,
    private notificationState: NotificationStateService,
    private notificationPreferences: NotificationPreferencesService
  ) {}

  ngOnInit(): void {
    this.preferencesSubscription = this.notificationPreferences.preferences$.subscribe(preferences => {
      this.soundAlertsEnabled = preferences.soundEnabled;
      this.vibrationAlertsEnabled = preferences.vibrationEnabled;
    });

    this.loadNotifications();
    this.pollSubscription = interval(30000).subscribe(() => {
      this.loadNotifications(false);
    });
  }

  ngOnDestroy(): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = null;
    this.preferencesSubscription?.unsubscribe();
    this.preferencesSubscription = null;
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  markNotificationAsRead(notification: NotificationRow): void {
    if (notification.isRead || this.markingNotificationId || this.markingAll) {
      return;
    }

    this.markingNotificationId = notification.userNotificationId;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.endpoint}/${notification.userNotificationId}/read`, {}).subscribe({
      next: () => {
        notification.isRead = true;
        notification.readAtUtc = new Date().toISOString();
        this.notificationState.setUnreadCount(this.unreadCount);
        this.markingNotificationId = null;
      },
      error: () => {
        this.markingNotificationId = null;
        this.errorMessage = 'notifications.errors.markReadFailed';
      }
    });
  }

  markAllAsRead(): void {
    if (this.unreadCount === 0 || this.markingAll || this.markingNotificationId) {
      return;
    }

    this.markingAll = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post<{ markedCount: number }>(`${this.endpoint}/read-all`, {}).subscribe({
      next: result => {
        const readTimestamp = new Date().toISOString();
        this.notifications = this.notifications.map(notification => ({
          ...notification,
          isRead: true,
          readAtUtc: notification.readAtUtc || readTimestamp
        }));
        this.markingAll = false;
        this.successMessage = result.markedCount > 0
          ? 'notifications.messages.markAllSuccess'
          : 'notifications.messages.alreadyRead';
        this.notificationState.setUnreadCount(0);
      },
      error: () => {
        this.markingAll = false;
        this.errorMessage = 'notifications.errors.markAllFailed';
      }
    });
  }

  trackNotification(_: number, notification: NotificationRow): string {
    return notification.userNotificationId;
  }

  onSoundAlertsChange(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    this.notificationPreferences.setSoundEnabled(!!target?.checked);
  }

  onVibrationAlertsChange(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    this.notificationPreferences.setVibrationEnabled(!!target?.checked);
  }

  testAlertCue(): void {
    this.notificationState.playPreviewCue();
    this.successMessage = 'notifications.messages.testAlertPlayed';
    this.errorMessage = '';
  }

  private loadNotifications(showLoading = true): void {
    if (showLoading) {
      this.loading = true;
      this.errorMessage = '';
      this.successMessage = '';
    }

    this.http.get<NotificationRow[]>(this.endpoint, {
      params: { take: '100' }
    }).subscribe({
      next: notifications => {
        this.notifications = notifications;
        this.notificationState.setUnreadCount(this.unreadCount);
        if (showLoading) {
          this.loading = false;
        }
      },
      error: () => {
        if (showLoading) {
          this.errorMessage = 'notifications.errors.loadFailed';
          this.loading = false;
        }
      }
    });
  }
}
