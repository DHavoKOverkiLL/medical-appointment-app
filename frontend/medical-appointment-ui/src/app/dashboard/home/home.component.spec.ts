import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService } from '../../auth/auth.service';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { HomeComponent } from './home.component';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  const authMock = jasmine.createSpyObj<AuthService>('AuthService', ['getUserRole', 'getUserRoleNormalized', 'logout']);

  beforeEach(async () => {
    authMock.getUserRole.and.returnValue('Admin');
    authMock.getUserRoleNormalized.and.returnValue('admin');

    await TestBed.configureTestingModule({
      imports: [
        HomeComponent,
        RouterTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [{ provide: AuthService, useValue: authMock }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
