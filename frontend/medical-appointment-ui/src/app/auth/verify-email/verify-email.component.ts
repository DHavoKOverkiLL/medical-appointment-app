import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    RouterModule,
    TranslateModule
  ],
  templateUrl: './verify-email.component.html',
  styles: [':host { display: block; }']
})
export class VerifyEmailComponent implements OnInit, OnDestroy {
  verifyForm: FormGroup;
  isSubmitting = false;
  isResending = false;
  infoMessage = '';
  successMessage = '';
  errorMessage = '';
  private readonly loginRedirectDelayMs = 5000;
  private readonly loginRedirectDelaySeconds = this.loginRedirectDelayMs / 1000;
  private loginRedirectTimer: ReturnType<typeof setTimeout> | null = null;
  private loginRedirectCountdownTimer: ReturnType<typeof setInterval> | null = null;
  private loginRedirectCountdownSeconds = 0;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private translate: TranslateService
  ) {
    this.verifyForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      code: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(16)]]
    });
  }

  ngOnInit(): void {
    const queryMap = this.route.snapshot.queryParamMap;
    const emailFromQuery = queryMap.get('email')?.trim();
    if (emailFromQuery) {
      this.verifyForm.patchValue({ email: emailFromQuery.toLowerCase() });
    }

    const source = queryMap.get('source');
    const sent = queryMap.get('sent');
    const nextAllowedAtUtc = queryMap.get('nextAllowedAtUtc');

    if (source === 'register') {
      this.infoMessage =
        sent === '1'
          ? this.translate.instant('auth.verify.messages.registerCodeSent')
          : this.translate.instant('auth.verify.messages.registerCheckSupport');
    } else if (source === 'login') {
      this.infoMessage =
        sent === '1'
          ? this.translate.instant('auth.verify.messages.loginCodeSent')
          : this.translate.instant('auth.verify.messages.loginCodePending');
    }

    if (nextAllowedAtUtc) {
      this.infoMessage = `${this.infoMessage} ${this.translate.instant('auth.verify.messages.nextRetryAt', {
        dateTime: this.formatUtc(nextAllowedAtUtc)
      })}`.trim();
    }
  }

  ngOnDestroy(): void {
    this.clearLoginRedirectTimer();
  }

  onSubmit(): void {
    if (this.verifyForm.invalid) {
      this.verifyForm.markAllAsTouched();
      return;
    }

    this.clearMessages();
    this.isSubmitting = true;

    const email = (this.verifyForm.get('email')?.value ?? '').trim().toLowerCase();
    const code = (this.verifyForm.get('code')?.value ?? '').trim();

    this.auth.verifyEmail({ email, code }).subscribe({
      next: response => {
        this.isSubmitting = false;
        if (response.emailVerified) {
          this.successMessage = response.message || this.translate.instant('auth.verify.messages.verified');
          this.scheduleLoginRedirect(email);
          return;
        }

        this.errorMessage = response.message || this.translate.instant('auth.verify.errors.invalidOrExpired');
      },
      error: error => {
        this.isSubmitting = false;
        this.errorMessage = this.getVerifyErrorMessage(error);
      }
    });
  }

  onResend(): void {
    const emailControl = this.verifyForm.get('email');
    if (!emailControl || emailControl.invalid) {
      emailControl?.markAsTouched();
      this.errorMessage = this.translate.instant('auth.verify.errors.emailRequiredForResend');
      return;
    }

    this.clearMessages();
    this.isResending = true;

    const email = String(emailControl.value ?? '').trim().toLowerCase();
    this.auth.resendVerificationCode({ email }).subscribe({
      next: response => {
        this.isResending = false;
        this.infoMessage = response.message || this.translate.instant('auth.verify.messages.codeResent');
      },
      error: error => {
        this.isResending = false;
        this.errorMessage = this.getResendErrorMessage(error);
      }
    });
  }

  private clearMessages(): void {
    this.clearLoginRedirectTimer();
    this.infoMessage = '';
    this.successMessage = '';
    this.errorMessage = '';
  }

  private scheduleLoginRedirect(email: string): void {
    this.clearLoginRedirectTimer();

    this.loginRedirectCountdownSeconds = this.loginRedirectDelaySeconds;
    this.setRedirectInfoMessage(this.loginRedirectCountdownSeconds);

    this.loginRedirectCountdownTimer = setInterval(() => {
      if (this.loginRedirectCountdownSeconds <= 1) {
        this.clearLoginRedirectCountdownTimer();
        return;
      }

      this.loginRedirectCountdownSeconds -= 1;
      this.setRedirectInfoMessage(this.loginRedirectCountdownSeconds);
    }, 1000);

    this.loginRedirectTimer = setTimeout(() => {
      void this.router.navigate(['/login'], {
        queryParams: { email }
      });
    }, this.loginRedirectDelayMs);
  }

  private clearLoginRedirectTimer(): void {
    if (this.loginRedirectTimer) {
      clearTimeout(this.loginRedirectTimer);
      this.loginRedirectTimer = null;
    }

    this.clearLoginRedirectCountdownTimer();
  }

  private clearLoginRedirectCountdownTimer(): void {
    if (this.loginRedirectCountdownTimer) {
      clearInterval(this.loginRedirectCountdownTimer);
      this.loginRedirectCountdownTimer = null;
    }

    this.loginRedirectCountdownSeconds = 0;
  }

  private setRedirectInfoMessage(seconds: number): void {
    this.infoMessage = this.translate.instant('auth.verify.messages.redirectingToLogin', {
      seconds
    });
  }

  private getVerifyErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return this.translate.instant('errors.apiUnavailable');
    }

    if (error.status === 400) {
      return this.translate.instant('auth.verify.errors.invalidOrExpired');
    }

    if (error.status === 503) {
      return this.translate.instant('auth.verify.errors.featureUnavailable');
    }

    return this.extractServerMessage(error) || this.translate.instant('auth.verify.errors.failed');
  }

  private getResendErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return this.translate.instant('errors.apiUnavailable');
    }

    if (error.status === 429) {
      const nextAllowed = error.error?.nextAllowedAtUtc as string | undefined;
      if (nextAllowed) {
        return this.translate.instant('auth.verify.messages.nextRetryAt', {
          dateTime: this.formatUtc(nextAllowed)
        });
      }

      return this.extractServerMessage(error) || this.translate.instant('auth.verify.errors.resendThrottled');
    }

    if (error.status === 503) {
      return this.translate.instant('auth.verify.errors.featureUnavailable');
    }

    return this.extractServerMessage(error) || this.translate.instant('auth.verify.errors.resendFailed');
  }

  private extractServerMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }

    const message = error.error?.message || error.error?.title;
    if (typeof message === 'string' && message.trim()) {
      return message;
    }

    return '';
  }

  private formatUtc(utcValue: string): string {
    const date = new Date(utcValue);
    if (Number.isNaN(date.getTime())) {
      return utcValue;
    }

    return date.toLocaleString();
  }
}
