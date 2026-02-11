import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { ViewAllAppointmentsComponent } from './view-all-appointments.component';

describe('ViewAllAppointmentsComponent', () => {
  let component: ViewAllAppointmentsComponent;
  let fixture: ComponentFixture<ViewAllAppointmentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ViewAllAppointmentsComponent,
        RouterTestingModule,
        HttpClientTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ViewAllAppointmentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
