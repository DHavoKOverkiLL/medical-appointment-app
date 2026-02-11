import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import { BookAppointmentComponent } from './book-appointment.component';

describe('BookAppointmentComponent', () => {
  let component: BookAppointmentComponent;
  let fixture: ComponentFixture<BookAppointmentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        BookAppointmentComponent,
        HttpClientTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BookAppointmentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
