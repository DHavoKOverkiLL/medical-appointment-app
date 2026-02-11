import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import {
  AvailabilityRangeDialogComponent,
  AvailabilityRangeDialogData
} from './availability-range-dialog.component';

describe('AvailabilityRangeDialogComponent', () => {
  let component: AvailabilityRangeDialogComponent;
  let fixture: ComponentFixture<AvailabilityRangeDialogComponent>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<AvailabilityRangeDialogComponent>>;

  beforeEach(async () => {
    dialogRef = jasmine.createSpyObj<MatDialogRef<AvailabilityRangeDialogComponent>>('MatDialogRef', ['close']);

    const dialogData: AvailabilityRangeDialogData = {
      mode: 'create',
      context: 'availability',
      dayOfWeek: 1,
      start: '09:00',
      end: '17:00',
      dayOptions: [
        { value: 1, labelKey: 'common.days.monday' }
      ]
    };

    await TestBed.configureTestingModule({
      imports: [
        AvailabilityRangeDialogComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: dialogRef }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AvailabilityRangeDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should serialize selected times as HH:mm on submit', () => {
    const start = new Date(2026, 1, 11, 9, 5, 0, 0);
    const end = new Date(2026, 1, 11, 10, 45, 0, 0);

    component.form.patchValue({ start, end });
    component.submit();

    expect(dialogRef.close).toHaveBeenCalledWith({
      dayOfWeek: 1,
      start: '09:05',
      end: '10:45'
    });
  });
});

