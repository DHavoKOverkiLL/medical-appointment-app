import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { API_BASE_URL } from '../core/api.config';
import { AuthService } from './auth.service';
import { TokenInterceptor } from './token.interceptor';

describe('TokenInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['logout', 'getToken']);
    authService.getToken.and.returnValue(null);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        {
          provide: HTTP_INTERCEPTORS,
          useClass: TokenInterceptor,
          multi: true
        }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('enables credentials on API requests', () => {
    http.get(`${API_BASE_URL}/api/User/secure`).subscribe();
    const req = httpMock.expectOne(`${API_BASE_URL}/api/User/secure`);

    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('adds bearer token on API requests when session token is available', () => {
    authService.getToken.and.returnValue('test-jwt-token');

    http.get(`${API_BASE_URL}/api/User/secure`).subscribe();
    const req = httpMock.expectOne(`${API_BASE_URL}/api/User/secure`);

    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-jwt-token');
    req.flush({});
  });

  it('does not force credentials on non-API requests', () => {
    http.get('https://example.com/ping').subscribe();
    const req = httpMock.expectOne('https://example.com/ping');

    expect(req.request.withCredentials).toBeFalse();
    req.flush({});
  });

  it('logs out and redirects on 401 for protected API endpoints', () => {
    http.get(`${API_BASE_URL}/api/Appointment/patient`).subscribe({
      error: () => undefined
    });
    const req = httpMock.expectOne(`${API_BASE_URL}/api/Appointment/patient`);
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('does not redirect on login 401', () => {
    http.post(`${API_BASE_URL}/api/User/login`, { email: 'bad@example.com', password: 'wrongpass' }).subscribe({
      error: () => undefined
    });
    const req = httpMock.expectOne(`${API_BASE_URL}/api/User/login`);
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
