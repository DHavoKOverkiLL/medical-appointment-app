import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader, TranslateService } from '@ngx-translate/core';

import { VerifyEmailComponent } from './verify-email.component';
import { AuthService } from '../auth.service';

describe('VerifyEmailComponent', () => {
  let component: VerifyEmailComponent;
  let fixture: ComponentFixture<VerifyEmailComponent>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['verifyEmail', 'resendVerificationCode']);
    authService.verifyEmail.and.returnValue(of({ emailVerified: true, message: 'Verified' }));
    authService.resendVerificationCode.and.returnValue(of({ verificationEmailSent: true, message: 'Resent' }));

    await TestBed.configureTestingModule({
      imports: [
        VerifyEmailComponent,
        RouterTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (key: string) => (key === 'email' ? 'test@example.com' : null)
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(VerifyEmailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show countdown and redirect to login after 5 seconds when verification succeeds', fakeAsync(() => {
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    const translateService = TestBed.inject(TranslateService);
    spyOn(translateService, 'instant').and.callFake((key: string | string[], params?: object) => {
      if (typeof key !== 'string') {
        return key.join(',');
      }

      if (key === 'auth.verify.messages.redirectingToLogin') {
        const seconds = (params as { seconds?: number } | undefined)?.seconds;
        return `Redirecting to sign in in ${seconds} seconds.`;
      }

      return key;
    });

    authService.verifyEmail.and.returnValue(of({ emailVerified: true, message: 'Email verified successfully.' }));

    component.verifyForm.patchValue({
      email: 'verified.user@example.com',
      code: '123456'
    });

    component.onSubmit();

    expect(component.successMessage).toBe('Email verified successfully.');
    expect(component.infoMessage).toBe('Redirecting to sign in in 5 seconds.');
    expect(navigateSpy).not.toHaveBeenCalled();

    tick(1000);
    expect(component.infoMessage).toBe('Redirecting to sign in in 4 seconds.');

    tick(1000);
    expect(component.infoMessage).toBe('Redirecting to sign in in 3 seconds.');

    tick(2000);
    expect(component.infoMessage).toBe('Redirecting to sign in in 1 seconds.');
    expect(navigateSpy).not.toHaveBeenCalled();

    tick(1000);
    expect(navigateSpy).toHaveBeenCalledWith(['/login'], {
      queryParams: { email: 'verified.user@example.com' }
    });
  }));
});
