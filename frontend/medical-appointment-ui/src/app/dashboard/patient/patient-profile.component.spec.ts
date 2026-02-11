import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { PatientProfileComponent } from './patient-profile.component';
import { DashboardApiService } from '../dashboard-api.service';
import { UserSummary } from '../dashboard.models';

describe('PatientProfileComponent', () => {
  let component: PatientProfileComponent;
  let fixture: ComponentFixture<PatientProfileComponent>;
  let dashboardApi: jasmine.SpyObj<DashboardApiService>;

  const initialProfile: UserSummary = {
    userId: 'user-1',
    username: 'patient.user',
    email: 'patient@example.com',
    role: 'Patient',
    firstName: 'Pat',
    lastName: 'Ient',
    personalIdentifier: '1234567890123',
    address: 'Old Address',
    phoneNumber: '0700000000',
    birthDate: '2000-01-01',
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
      birthDate: '2026-02-15'
    }));

    await TestBed.configureTestingModule({
      imports: [
        PatientProfileComponent,
        RouterTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: DashboardApiService, useValue: dashboardApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PatientProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should serialize birthDate as yyyy-mm-dd on save', () => {
    component.profileForm.patchValue({
      firstName: 'Pat',
      lastName: 'Ient',
      birthDate: new Date(2026, 1, 15),
      address: 'New Address',
      phoneNumber: '0711111111'
    });

    component.saveProfile();

    expect(dashboardApi.updateMyProfile).toHaveBeenCalled();
    const payload = dashboardApi.updateMyProfile.calls.mostRecent().args[0];
    expect(payload.birthDate).toBe('2026-02-15');
  });
});

