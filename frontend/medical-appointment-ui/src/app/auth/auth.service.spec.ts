import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../core/api.config';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    sessionStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('stores session data when login succeeds', () => {
    service.login({ email: 'test@example.com', password: 'StrongPass1!' }).subscribe();
    const req = httpMock.expectOne(`${API_BASE_URL}/api/User/login`);

    expect(req.request.withCredentials).toBeTrue();
    req.flush({
      token: '',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'test@example.com',
      role: 'Patient',
      clinicId: '22222222-2222-2222-2222-222222222222',
      clinicName: 'Main clinic'
    });

    expect(service.isLoggedIn()).toBeTrue();
    expect(service.getUserRole()).toBe('Patient');
    expect(service.getUserEmail()).toBe('test@example.com');
  });

  it('returns false and clears session for expired auth state', () => {
    sessionStorage.setItem('auth_session', JSON.stringify({
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'test@example.com',
      role: 'Patient',
      clinicId: '22222222-2222-2222-2222-222222222222',
      clinicName: 'Main clinic',
      expiresAtUtc: '2000-01-01T00:00:00Z'
    }));

    expect(service.isLoggedIn()).toBeFalse();
    expect(sessionStorage.getItem('auth_session')).toBeNull();
  });

  it('returns false for malformed session payload', () => {
    sessionStorage.setItem('auth_session', 'invalid-json');

    expect(service.isLoggedIn()).toBeFalse();
  });

  it('clears local session and calls backend logout endpoint', () => {
    sessionStorage.setItem('auth_session', JSON.stringify({
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'test@example.com',
      role: 'Patient',
      clinicId: '22222222-2222-2222-2222-222222222222',
      clinicName: 'Main clinic',
      expiresAtUtc: '2099-01-01T00:00:00Z'
    }));

    service.logout();

    expect(sessionStorage.getItem('auth_session')).toBeNull();
    const req = httpMock.expectOne(`${API_BASE_URL}/api/User/logout`);
    expect(req.request.withCredentials).toBeTrue();
    req.flush({});
  });
});
