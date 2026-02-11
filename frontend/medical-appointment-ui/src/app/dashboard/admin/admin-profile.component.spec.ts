import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { AdminProfileComponent } from './admin-profile.component';
import { DashboardApiService } from '../dashboard-api.service';
import { UserSummary } from '../dashboard.models';

describe('AdminProfileComponent', () => {
  let component: AdminProfileComponent;
  let fixture: ComponentFixture<AdminProfileComponent>;
  let dashboardApi: jasmine.SpyObj<DashboardApiService>;

  const initialProfile: UserSummary = {
    userId: 'admin-1',
    username: 'admin.user',
    email: 'admin@example.com',
    role: 'Admin',
    firstName: 'Ad',
    lastName: 'Min',
    personalIdentifier: '1234567890123',
    address: 'HQ Address',
    phoneNumber: '0744444444',
    birthDate: '1985-04-20',
    clinicId: 'clinic-1',
    clinicName: 'Clinic One'
  };

  beforeEach(async () => {
    dashboardApi = jasmine.createSpyObj<DashboardApiService>('DashboardApiService', [
      'getMyProfile',
      'updateMyProfile'
    ]);
    dashboardApi.getMyProfile.and.returnValue(of(initialProfile));
    dashboardApi.updateMyProfile.and.returnValue(of({
      ...initialProfile,
      birthDate: '2026-04-09'
    }));

    await TestBed.configureTestingModule({
      imports: [
        AdminProfileComponent,
        RouterTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: DashboardApiService, useValue: dashboardApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should serialize birthDate as yyyy-mm-dd on save', () => {
    component.profileForm.patchValue({
      firstName: 'Ad',
      lastName: 'Min',
      birthDate: new Date(2026, 3, 9),
      address: 'HQ Address 2',
      phoneNumber: '0755555555'
    });

    component.saveProfile();

    expect(dashboardApi.updateMyProfile).toHaveBeenCalled();
    const payload = dashboardApi.updateMyProfile.calls.mostRecent().args[0];
    expect(payload.birthDate).toBe('2026-04-09');
  });
});

