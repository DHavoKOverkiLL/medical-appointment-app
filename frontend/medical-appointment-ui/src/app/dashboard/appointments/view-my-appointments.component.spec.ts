import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { ViewMyAppointmentsComponent } from './view-my-appointments.component';

describe('ViewMyAppointmentsComponent', () => {
  let component: ViewMyAppointmentsComponent;
  let fixture: ComponentFixture<ViewMyAppointmentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ViewMyAppointmentsComponent,
        RouterTestingModule,
        HttpClientTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ViewMyAppointmentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
