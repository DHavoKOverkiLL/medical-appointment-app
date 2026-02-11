import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['isLoggedIn']);

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: authService }
      ],
    });

    guard = TestBed.inject(AuthGuard);
    router = TestBed.inject(Router);
  });

  it('allows access when authenticated', () => {
    authService.isLoggedIn.and.returnValue(true);
    const state = { url: '/dashboard' } as any;

    const result = guard.canActivate({} as any, state);

    expect(result).toBeTrue();
  });

  it('redirects to login with returnUrl when not authenticated', () => {
    authService.isLoggedIn.and.returnValue(false);
    const state = { url: '/dashboard/appointments/book' } as any;

    const result = guard.canActivate({} as any, state) as UrlTree;
    const serialized = router.serializeUrl(result);

    expect(serialized).toContain('/login');
    expect(serialized).toContain('returnUrl=%2Fdashboard%2Fappointments%2Fbook');
  });
});

