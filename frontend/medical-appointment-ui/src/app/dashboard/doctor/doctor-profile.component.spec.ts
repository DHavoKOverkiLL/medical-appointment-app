import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { DoctorProfileComponent } from './doctor-profile.component';
import { DashboardApiService } from '../dashboard-api.service';
import { UserSummary } from '../dashboard.models';

describe('DoctorProfileComponent', () => {
  let component: DoctorProfileComponent;
  let fixture: ComponentFixture<DoctorProfileComponent>;
  let dashboardApi: jasmine.SpyObj<DashboardApiService>;

  const initialProfile: UserSummary = {
    userId: 'doctor-1',
    username: 'doctor.user',
    email: 'doctor@example.com',
    role: 'Doctor',
    firstName: 'Doc',
    lastName: 'Tor',
    personalIdentifier: '1234567890123',
    address: 'Clinic Street',
    phoneNumber: '0722222222',
    birthDate: '1990-06-10',
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
      birthDate: '2026-03-01'
    }));

    await TestBed.configureTestingModule({
      imports: [
        DoctorProfileComponent,
        RouterTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: DashboardApiService, useValue: dashboardApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DoctorProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should serialize birthDate as yyyy-mm-dd on save', () => {
    component.profileForm.patchValue({
      firstName: 'Doc',
      lastName: 'Tor',
      birthDate: new Date(2026, 2, 1),
      address: 'Clinic Street 2',
      phoneNumber: '0733333333'
    });

    component.saveProfile();

    expect(dashboardApi.updateMyProfile).toHaveBeenCalled();
    const payload = dashboardApi.updateMyProfile.calls.mostRecent().args[0];
    expect(payload.birthDate).toBe('2026-03-01');
  });
});

