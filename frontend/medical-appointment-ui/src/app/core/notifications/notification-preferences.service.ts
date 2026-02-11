import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface NotificationPreferences {
  soundEnabled: boolean;
  vibrationEnabled: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationPreferencesService {
  private readonly soundStorageKey = 'notifications_sound_enabled';
  private readonly vibrationStorageKey = 'notifications_vibration_enabled';
  private readonly preferencesSubject = new BehaviorSubject<NotificationPreferences>(this.loadPreferences());

  readonly preferences$ = this.preferencesSubject.asObservable();

  get currentPreferences(): NotificationPreferences {
    return this.preferencesSubject.value;
  }

  setSoundEnabled(enabled: boolean): void {
    this.persistBoolean(this.soundStorageKey, enabled);
    this.preferencesSubject.next({
      ...this.preferencesSubject.value,
      soundEnabled: enabled
    });
  }

  setVibrationEnabled(enabled: boolean): void {
    this.persistBoolean(this.vibrationStorageKey, enabled);
    this.preferencesSubject.next({
      ...this.preferencesSubject.value,
      vibrationEnabled: enabled
    });
  }

  private loadPreferences(): NotificationPreferences {
    return {
      soundEnabled: this.readBoolean(this.soundStorageKey, false),
      vibrationEnabled: this.readBoolean(this.vibrationStorageKey, false)
    };
  }

  private readBoolean(key: string, fallback: boolean): boolean {
    if (typeof localStorage === 'undefined') {
      return fallback;
    }

    const rawValue = localStorage.getItem(key);
    if (rawValue === 'true') {
      return true;
    }

    if (rawValue === 'false') {
      return false;
    }

    return fallback;
  }

  private persistBoolean(key: string, value: boolean): void {
    if (typeof localStorage === 'undefined') {
      return;
    }

    localStorage.setItem(key, String(value));
  }
}
