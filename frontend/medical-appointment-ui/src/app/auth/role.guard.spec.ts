import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService } from './auth.service';
import { RoleGuard } from './role.guard';

describe('RoleGuard', () => {
  let guard: RoleGuard;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['isLoggedIn', 'getUserRole']);

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [
        RoleGuard,
        { provide: AuthService, useValue: authService }
      ]
    });

    guard = TestBed.inject(RoleGuard);
    router = TestBed.inject(Router);
  });

  it('allows access for matching role', () => {
    authService.isLoggedIn.and.returnValue(true);
    authService.getUserRole.and.returnValue('Doctor');

    const route = { data: { roles: ['Doctor'] } } as any;
    const result = guard.canActivate(route);

    expect(result).toBeTrue();
  });

  it('redirects to dashboard for mismatched role', () => {
    authService.isLoggedIn.and.returnValue(true);
    authService.getUserRole.and.returnValue('Patient');

    const route = { data: { roles: ['Admin'] } } as any;
    const result = guard.canActivate(route) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/dashboard');
  });
});

