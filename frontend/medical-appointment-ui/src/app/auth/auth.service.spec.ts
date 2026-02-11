import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;

  const validToken =
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
    'eyJuYW1laWQiOiIxMjMiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJyb2xlIjoiUGF0aWVudCIsImV4cCI6NDA3MDkwODgwMCwiaWF0IjoxNzAwMDAwMDAwfQ.' +
    'signature';

  const expiredToken =
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
    'eyJuYW1laWQiOiIxMjMiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJyb2xlIjoiUGF0aWVudCIsImV4cCI6MTAwMCwiaWF0IjoxMDAwfQ.' +
    'signature';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(AuthService);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('returns true for non-expired token', () => {
    localStorage.setItem('token', validToken);

    expect(service.isLoggedIn()).toBeTrue();
    expect(service.getUserRole()).toBe('Patient');
  });

  it('returns false and clears storage for expired token', () => {
    localStorage.setItem('token', expiredToken);

    expect(service.isLoggedIn()).toBeFalse();
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('returns false for malformed token', () => {
    localStorage.setItem('token', 'invalid-token');

    expect(service.isLoggedIn()).toBeFalse();
    expect(localStorage.getItem('token')).toBeNull();
  });
});

