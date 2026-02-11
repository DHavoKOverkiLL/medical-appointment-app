import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { BehaviorSubject } from 'rxjs';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';
import { AppComponent } from './app.component';
import { AuthService } from './auth/auth.service';
import { I18nService } from './core/i18n/i18n.service';
import { LoadingService } from './core/loading/loading.service';

describe('AppComponent', () => {
  const authServiceMock = jasmine.createSpyObj<AuthService>('AuthService', ['isLoggedIn', 'logout']);
  const i18nServiceMock = {
    currentLanguage: 'ro',
    setLanguage: jasmine.createSpy('setLanguage')
  };
  const loadingServiceMock = {
    isVisible$: new BehaviorSubject(false)
  };

  beforeEach(async () => {
    authServiceMock.isLoggedIn.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
        RouterTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: I18nService, useValue: i18nServiceMock },
        { provide: LoadingService, useValue: loadingServiceMock }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
