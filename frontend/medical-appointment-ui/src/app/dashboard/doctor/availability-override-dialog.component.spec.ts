import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import {
  AvailabilityOverrideDialogComponent,
  AvailabilityOverrideDialogData
} from './availability-override-dialog.component';

describe('AvailabilityOverrideDialogComponent', () => {
  let component: AvailabilityOverrideDialogComponent;
  let fixture: ComponentFixture<AvailabilityOverrideDialogComponent>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<AvailabilityOverrideDialogComponent>>;

  beforeEach(async () => {
    dialogRef = jasmine.createSpyObj<MatDialogRef<AvailabilityOverrideDialogComponent>>('MatDialogRef', ['close']);

    const dialogData: AvailabilityOverrideDialogData = {
      mode: 'create',
      date: '2026-02-10',
      isAvailable: true,
      start: '09:00',
      end: '10:00',
      reason: 'Initial reason'
    };

    await TestBed.configureTestingModule({
      imports: [
        AvailabilityOverrideDialogComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: dialogRef }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AvailabilityOverrideDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should serialize date and time values on submit', () => {
    component.form.patchValue({
      date: new Date(2026, 1, 12),
      start: new Date(2026, 1, 12, 14, 30, 0, 0),
      end: new Date(2026, 1, 12, 15, 45, 0, 0),
      reason: 'Updated reason'
    });

    component.submit();

    expect(dialogRef.close).toHaveBeenCalledWith({
      date: '2026-02-12',
      isAvailable: true,
      start: '14:30',
      end: '15:45',
      reason: 'Updated reason'
    });
  });
});

