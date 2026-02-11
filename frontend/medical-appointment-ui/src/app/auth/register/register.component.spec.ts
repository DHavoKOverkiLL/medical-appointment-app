import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        RegisterComponent,
        RouterTestingModule,
        HttpClientTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    spyOn<any>(component, 'loadClinics').and.stub();
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should serialize birthDate as yyyy-mm-dd when submitting', () => {
    const postSpy = spyOn((component as any).http, 'post').and.returnValue(of({
      requiresEmailVerification: true,
      verificationEmailSent: true,
      email: 'john@example.com'
    }));
    const navigateSpy = spyOn((component as any).router, 'navigate').and.returnValue(Promise.resolve(true));
    component.registerForm.controls['clinicId'].enable({ emitEvent: false });

    component.registerForm.patchValue({
      username: 'johnpatient',
      email: 'john@example.com',
      password: 'P@ssword1',
      firstName: 'John',
      lastName: 'Patient',
      personalIdentifier: '1234567890123',
      address: 'Some Street',
      phoneNumber: '0700000000',
      birthDate: new Date(2026, 1, 11),
      clinicId: 'clinic-1'
    });

    component.onSubmit();

    expect(postSpy).toHaveBeenCalled();
    const payload = postSpy.calls.mostRecent().args[1] as { birthDate: string };
    expect(payload.birthDate).toBe('2026-02-11');
    expect(navigateSpy).toHaveBeenCalledWith(['/verify-email'], jasmine.any(Object));
  });
});
