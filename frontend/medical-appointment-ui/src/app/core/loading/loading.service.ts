import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private readonly showDelayMs = 150;
  private readonly minVisibleMs = 240;

  private activeRequestCount = 0;
  private visibleSince = 0;
  private showTimer: ReturnType<typeof setTimeout> | null = null;
  private hideTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly isVisibleSubject = new BehaviorSubject<boolean>(false);
  readonly isVisible$ = this.isVisibleSubject.asObservable();

  begin(): void {
    this.activeRequestCount += 1;

    if (this.activeRequestCount !== 1) {
      return;
    }

    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }

    if (this.isVisibleSubject.value) {
      return;
    }

    this.showTimer = setTimeout(() => {
      this.showTimer = null;

      if (this.activeRequestCount <= 0) {
        return;
      }

      this.visibleSince = Date.now();
      this.isVisibleSubject.next(true);
    }, this.showDelayMs);
  }

  end(): void {
    if (this.activeRequestCount === 0) {
      return;
    }

    this.activeRequestCount -= 1;

    if (this.activeRequestCount > 0) {
      return;
    }

    if (this.showTimer) {
      clearTimeout(this.showTimer);
      this.showTimer = null;
    }

    if (!this.isVisibleSubject.value) {
      return;
    }

    const elapsedMs = Date.now() - this.visibleSince;
    const hideDelayMs = Math.max(this.minVisibleMs - elapsedMs, 0);

    this.hideTimer = setTimeout(() => {
      this.hideTimer = null;

      if (this.activeRequestCount === 0) {
        this.isVisibleSubject.next(false);
      }
    }, hideDelayMs);
  }
}
