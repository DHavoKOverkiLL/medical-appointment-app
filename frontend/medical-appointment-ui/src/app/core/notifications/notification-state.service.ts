import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Subscription, of, timer } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { API_BASE_URL } from '../api.config';
import { NotificationPreferencesService } from './notification-preferences.service';

interface UnreadCountResponse {
  unreadCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationStateService implements OnDestroy {
  private readonly unreadCountEndpoint = `${API_BASE_URL}/api/Notification/unread-count`;
  private readonly unreadCountSubject = new BehaviorSubject<number>(0);
  private pollingSubscription: Subscription | null = null;
  private hasUnreadBaseline = false;

  readonly unreadCount$ = this.unreadCountSubject.asObservable();

  constructor(
    private http: HttpClient,
    private preferences: NotificationPreferencesService
  ) {}

  ngOnDestroy(): void {
    this.stopPolling();
  }

  startPolling(intervalMs = 30000): void {
    if (this.pollingSubscription) {
      return;
    }

    this.pollingSubscription = timer(0, intervalMs).subscribe(() => {
      this.refreshUnreadCount();
    });
  }

  stopPolling(): void {
    if (!this.pollingSubscription) {
      return;
    }

    this.pollingSubscription.unsubscribe();
    this.pollingSubscription = null;
  }

  refreshUnreadCount(): void {
    const previousCount = this.unreadCountSubject.value;
    this.http.get<UnreadCountResponse>(this.unreadCountEndpoint).pipe(
      map(response => this.normalizeCount(response.unreadCount)),
      catchError(() => of(this.unreadCountSubject.value))
    ).subscribe(unreadCount => {
      this.unreadCountSubject.next(unreadCount);
      if (this.hasUnreadBaseline && unreadCount > previousCount) {
        this.playAlertCue();
      }

      this.hasUnreadBaseline = true;
    });
  }

  setUnreadCount(count: number): void {
    this.unreadCountSubject.next(this.normalizeCount(count));
    this.hasUnreadBaseline = true;
  }

  playPreviewCue(): void {
    this.playAlertCue();
  }

  private normalizeCount(count: number): number {
    if (!Number.isFinite(count)) {
      return 0;
    }

    return Math.max(0, Math.floor(count));
  }

  private playAlertCue(): void {
    if (typeof document !== 'undefined' && document.visibilityState === 'hidden') {
      return;
    }

    const preferences = this.preferences.currentPreferences;
    if (preferences.soundEnabled) {
      this.playSoundCue();
    }

    if (preferences.vibrationEnabled) {
      this.playVibrationCue();
    }
  }

  private playSoundCue(): void {
    if (typeof window === 'undefined') {
      return;
    }

    const audioContextFactory = (window as any).AudioContext || (window as any).webkitAudioContext;
    if (!audioContextFactory) {
      return;
    }

    try {
      const context: AudioContext = new audioContextFactory();
      const now = context.currentTime;
      const gain = context.createGain();
      gain.connect(context.destination);
      gain.gain.setValueAtTime(0.0001, now);
      gain.gain.exponentialRampToValueAtTime(0.09, now + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.4);

      const firstTone = context.createOscillator();
      firstTone.type = 'sine';
      firstTone.frequency.setValueAtTime(880, now);
      firstTone.connect(gain);
      firstTone.start(now);
      firstTone.stop(now + 0.16);

      const secondTone = context.createOscillator();
      secondTone.type = 'sine';
      secondTone.frequency.setValueAtTime(660, now + 0.18);
      secondTone.connect(gain);
      secondTone.start(now + 0.18);
      secondTone.stop(now + 0.36);

      void context.resume().catch(() => undefined);
      setTimeout(() => {
        void context.close().catch(() => undefined);
      }, 500);
    } catch {
      // Ignore browser/device playback restrictions.
    }
  }

  private playVibrationCue(): void {
    if (typeof navigator === 'undefined' || typeof navigator.vibrate !== 'function') {
      return;
    }

    navigator.vibrate([110, 70, 110]);
  }
}
