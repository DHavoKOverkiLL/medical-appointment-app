import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../auth/auth.service';
import { NotificationStateService } from '../../core/notifications/notification-state.service';

import { HeaderComponent } from './header.component';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  const authMock = jasmine.createSpyObj<AuthService>('AuthService', [
    'logout',
    'isLoggedIn',
    'getUserEmail',
    'getUserRole',
    'getUserRoleNormalized',
    'getClinicName'
  ]);
  const notificationStateMock = jasmine.createSpyObj<NotificationStateService>(
    'NotificationStateService',
    ['startPolling', 'stopPolling', 'setUnreadCount', 'refreshUnreadCount'],
    { unreadCount$: of(3) }
  );

  beforeEach(async () => {
    authMock.isLoggedIn.and.returnValue(true);
    authMock.getUserEmail.and.returnValue('admin@medio.local');
    authMock.getUserRole.and.returnValue('Admin');
    authMock.getUserRoleNormalized.and.returnValue('admin');
    authMock.getClinicName.and.returnValue('Default Clinic');

    await TestBed.configureTestingModule({
      imports: [HeaderComponent, RouterTestingModule, TranslateModule.forRoot()],
      providers: [
        { provide: AuthService, useValue: authMock },
        { provide: NotificationStateService, useValue: notificationStateMock }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(notificationStateMock.startPolling).toHaveBeenCalled();
    expect(component.unreadCountLabel).toBe('3');
  });

  it('navigates to login on logout', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    component.logout();

    expect(authMock.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('stops polling on destroy', () => {
    fixture.destroy();
    expect(notificationStateMock.stopPolling).toHaveBeenCalled();
  });
});
