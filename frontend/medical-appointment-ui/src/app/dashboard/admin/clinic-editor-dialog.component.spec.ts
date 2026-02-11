import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';

import {
  ClinicEditorDialogComponent,
  ClinicEditorDialogData,
  ClinicEditorDialogResult
} from './clinic-editor-dialog.component';

describe('ClinicEditorDialogComponent', () => {
  let component: ClinicEditorDialogComponent;
  let fixture: ComponentFixture<ClinicEditorDialogComponent>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<ClinicEditorDialogComponent, ClinicEditorDialogResult>>;

  beforeEach(async () => {
    dialogRef = jasmine.createSpyObj<MatDialogRef<ClinicEditorDialogComponent, ClinicEditorDialogResult>>(
      'MatDialogRef',
      ['close']
    );

    const dialogData: ClinicEditorDialogData = {
      mode: 'create',
      sysOperations: [{ sysOperationId: 1, name: 'Operation' }],
      sysAccreditations: [{ sysAccreditationId: 1, name: 'Accreditation' }],
      sysClinicTypes: [{ sysClinicTypeId: 1, name: 'Clinic Type' }],
      sysOwnershipTypes: [{ sysOwnershipTypeId: 1, name: 'Ownership Type' }],
      sysSourceSystems: [{ sysSourceSystemId: 1, name: 'Source System' }]
    };

    await TestBed.configureTestingModule({
      imports: [
        ClinicEditorDialogComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: dialogRef }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClinicEditorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should serialize operating-hour time values as HH:mm', () => {
    component.startAddOperatingHour();
    component.operatingHourForm.patchValue({
      dayOfWeek: 2,
      isClosed: false,
      open: new Date(2026, 1, 11, 8, 15, 0, 0),
      close: new Date(2026, 1, 11, 17, 45, 0, 0)
    });

    component.saveOperatingHour();

    expect(component.operatingHoursDraft.length).toBe(1);
    expect(component.operatingHoursDraft[0].open).toBe('08:15');
    expect(component.operatingHoursDraft[0].close).toBe('17:45');
  });

  it('should serialize date values as yyyy-mm-dd on submit', () => {
    component.form.patchValue({
      name: 'Clinic Name',
      code: 'CLN-1',
      foundedOn: new Date(2020, 0, 2),
      lastSecurityRiskAssessmentOn: new Date(2021, 5, 3)
    });

    component.submit();

    expect(dialogRef.close).toHaveBeenCalled();
    const result = dialogRef.close.calls.mostRecent().args[0] as ClinicEditorDialogResult;

    expect(result.mode).toBe('create');
    expect(result.payload.foundedOn).toBe('2020-01-02');
    expect(result.payload.lastSecurityRiskAssessmentOn).toBe('2021-06-03');
  });
});

