import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import {
  ClinicAccreditation,
  ClinicInsurancePlan,
  ClinicOperatingHour,
  ClinicService,
  ClinicSummary,
  ClinicUpsertPayload,
  SysAccreditationSummary,
  SysClinicTypeSummary,
  SysOperationSummary,
  SysOwnershipTypeSummary,
  SysSourceSystemSummary
} from '../dashboard.models';

export interface ClinicEditorDialogData {
  mode: 'create' | 'edit';
  clinic?: ClinicSummary;
  sysOperations: SysOperationSummary[];
  sysAccreditations: SysAccreditationSummary[];
  sysClinicTypes: SysClinicTypeSummary[];
  sysOwnershipTypes: SysOwnershipTypeSummary[];
  sysSourceSystems: SysSourceSystemSummary[];
}

export type ClinicDialogPayload = ClinicUpsertPayload | (ClinicUpsertPayload & { isActive: boolean });

export interface ClinicEditorDialogResult {
  mode: 'create' | 'edit';
  payload: ClinicDialogPayload;
}

interface DayOption {
  value: number;
  labelKey: string;
}

@Component({
  selector: 'app-clinic-editor-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
    MatButtonModule,
    TranslateModule
  ],
  templateUrl: './clinic-editor-dialog.component.html',
  styleUrls: ['./clinic-editor-dialog.component.scss']
})
export class ClinicEditorDialogComponent {
  readonly form: FormGroup;
  readonly operatingHourForm: FormGroup;
  readonly serviceForm: FormGroup;
  readonly insurancePlanForm: FormGroup;
  readonly accreditationForm: FormGroup;
  readonly sysOperations: SysOperationSummary[];
  readonly sysAccreditations: SysAccreditationSummary[];
  readonly sysClinicTypes: SysClinicTypeSummary[];
  readonly sysOwnershipTypes: SysOwnershipTypeSummary[];
  readonly sysSourceSystems: SysSourceSystemSummary[];
  readonly isEditMode: boolean;
  readonly dayOptions: DayOption[] = [
    { value: 0, labelKey: 'common.days.sunday' },
    { value: 1, labelKey: 'common.days.monday' },
    { value: 2, labelKey: 'common.days.tuesday' },
    { value: 3, labelKey: 'common.days.wednesday' },
    { value: 4, labelKey: 'common.days.thursday' },
    { value: 5, labelKey: 'common.days.friday' },
    { value: 6, labelKey: 'common.days.saturday' }
  ];

  activeTabIndex = 0;
  errorMessage = '';

  operatingHoursDraft: ClinicOperatingHour[] = [];
  servicesDraft: ClinicService[] = [];
  insurancePlansDraft: ClinicInsurancePlan[] = [];
  accreditationsDraft: ClinicAccreditation[] = [];

  showOperatingHourEditor = false;
  showServiceEditor = false;
  showInsurancePlanEditor = false;
  showAccreditationEditor = false;

  editingOperatingHourDay: number | null = null;
  editingServiceOperationId: number | null = null;
  editingInsurancePlanKey: string | null = null;
  editingAccreditationId: number | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly translate: TranslateService,
    private readonly dialogRef: MatDialogRef<ClinicEditorDialogComponent, ClinicEditorDialogResult>,
    @Inject(MAT_DIALOG_DATA) readonly data: ClinicEditorDialogData
  ) {
    this.isEditMode = data.mode === 'edit';
    this.sysOperations = [...(data.sysOperations || [])].sort((a, b) => a.name.localeCompare(b.name));
    this.sysAccreditations = [...(data.sysAccreditations || [])].sort((a, b) => a.name.localeCompare(b.name));
    this.sysClinicTypes = [...(data.sysClinicTypes || [])].sort((a, b) => a.name.localeCompare(b.name));
    this.sysOwnershipTypes = [...(data.sysOwnershipTypes || [])].sort((a, b) => a.name.localeCompare(b.name));
    this.sysSourceSystems = [...(data.sysSourceSystems || [])].sort((a, b) => a.name.localeCompare(b.name));
    this.form = this.fb.group({
      name: ['', Validators.required],
      code: ['', Validators.required],
      legalName: [''],
      sysClinicTypeId: [this.sysClinicTypes[0]?.sysClinicTypeId ?? null, [Validators.required, Validators.min(1)]],
      sysOwnershipTypeId: [this.sysOwnershipTypes[0]?.sysOwnershipTypeId ?? null, [Validators.required, Validators.min(1)]],
      foundedOn: [null],
      npiOrganization: [''],
      ein: [''],
      taxonomyCode: [''],
      stateLicenseFacility: [''],
      cliaNumber: [''],
      addressLine1: [''],
      addressLine2: [''],
      city: [''],
      state: [''],
      postalCode: [''],
      countryCode: ['US', Validators.maxLength(2)],
      timezone: ['America/Chicago'],
      mainPhone: [''],
      fax: [''],
      mainEmail: ['', Validators.email],
      websiteUrl: [''],
      patientPortalUrl: [''],
      bookingMethodsInput: [''],
      avgNewPatientWaitDays: [null, [Validators.min(0), Validators.max(365)]],
      sameDayAvailable: [false],
      hipaaNoticeVersion: [''],
      lastSecurityRiskAssessmentOn: [null],
      sysSourceSystemId: [this.sysSourceSystems[0]?.sysSourceSystemId ?? null, [Validators.required, Validators.min(1)]],
      isActive: [true]
    });

    this.operatingHourForm = this.fb.group({
      dayOfWeek: [1, [Validators.required, Validators.min(0), Validators.max(6)]],
      open: ['08:00'],
      close: ['17:00'],
      isClosed: [false]
    });

    this.serviceForm = this.fb.group({
      sysOperationId: [null, [Validators.required, Validators.min(1)]],
      isTelehealthAvailable: [false]
    });

    this.insurancePlanForm = this.fb.group({
      payerName: ['', Validators.required],
      planName: ['', Validators.required],
      isInNetwork: [true]
    });

    this.accreditationForm = this.fb.group({
      sysAccreditationId: [null, [Validators.required, Validators.min(1)]],
      effectiveOn: [null],
      expiresOn: [null]
    });

    if (this.isEditMode && data.clinic) {
      this.form.patchValue({
        name: data.clinic.name,
        code: data.clinic.code,
        legalName: data.clinic.legalName,
        sysClinicTypeId: data.clinic.sysClinicTypeId,
        sysOwnershipTypeId: data.clinic.sysOwnershipTypeId,
        foundedOn: this.parseIsoDate(data.clinic.foundedOn),
        npiOrganization: data.clinic.npiOrganization,
        ein: data.clinic.ein,
        taxonomyCode: data.clinic.taxonomyCode,
        stateLicenseFacility: data.clinic.stateLicenseFacility,
        cliaNumber: data.clinic.cliaNumber,
        addressLine1: data.clinic.addressLine1,
        addressLine2: data.clinic.addressLine2,
        city: data.clinic.city,
        state: data.clinic.state,
        postalCode: data.clinic.postalCode,
        countryCode: data.clinic.countryCode || 'US',
        timezone: data.clinic.timezone || 'America/Chicago',
        mainPhone: data.clinic.mainPhone,
        fax: data.clinic.fax,
        mainEmail: data.clinic.mainEmail,
        websiteUrl: data.clinic.websiteUrl,
        patientPortalUrl: data.clinic.patientPortalUrl,
        bookingMethodsInput: data.clinic.bookingMethods.join(', '),
        avgNewPatientWaitDays: data.clinic.avgNewPatientWaitDays,
        sameDayAvailable: data.clinic.sameDayAvailable,
        hipaaNoticeVersion: data.clinic.hipaaNoticeVersion,
        lastSecurityRiskAssessmentOn: this.parseIsoDate(data.clinic.lastSecurityRiskAssessmentOn),
        sysSourceSystemId: data.clinic.sysSourceSystemId,
        isActive: data.clinic.isActive
      });

      this.operatingHoursDraft = [...data.clinic.operatingHours]
        .sort((a, b) => a.dayOfWeek - b.dayOfWeek);

      this.servicesDraft = [...data.clinic.services]
        .map(service => ({
          ...service,
          name: this.getOperationName(service.sysOperationId, service.name)
        }))
        .sort((a, b) => a.name.localeCompare(b.name));

      this.insurancePlansDraft = [...data.clinic.insurancePlans]
        .sort((a, b) => this.getInsurancePlanKey(a).localeCompare(this.getInsurancePlanKey(b)));

      this.accreditationsDraft = [...data.clinic.accreditations]
        .map(accreditation => ({
          ...accreditation,
          name: this.getAccreditationTypeName(accreditation.sysAccreditationId, accreditation.name)
        }))
        .sort((a, b) => a.name.localeCompare(b.name));
    }
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'clinicEditor.dialogTitleEdit' : 'clinicEditor.dialogTitleCreate';
  }

  get submitLabel(): string {
    return this.isEditMode ? 'actions.saveClinic' : 'actions.createClinic';
  }

  get operatingHourEditorTitle(): string {
    if (this.editingOperatingHourDay === null) {
      return this.translate.instant('clinicEditor.editorTitles.addOperatingHour');
    }

    const dayLabel = this.translate.instant(this.getDayLabel(this.editingOperatingHourDay));
    return this.translate.instant('clinicEditor.editorTitles.editOperatingHourDay', { day: dayLabel });
  }

  get serviceEditorTitle(): string {
    return this.editingServiceOperationId === null
      ? this.translate.instant('clinicEditor.editorTitles.addService')
      : this.translate.instant('clinicEditor.editorTitles.editService');
  }

  get insurancePlanEditorTitle(): string {
    return this.editingInsurancePlanKey === null
      ? this.translate.instant('clinicEditor.editorTitles.addInsurancePlan')
      : this.translate.instant('clinicEditor.editorTitles.editInsurancePlan');
  }

  get accreditationEditorTitle(): string {
    return this.editingAccreditationId === null
      ? this.translate.instant('clinicEditor.editorTitles.addAccreditation')
      : this.translate.instant('clinicEditor.editorTitles.editAccreditation');
  }

  close(): void {
    this.dialogRef.close();
  }

  submit(): void {
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.buildPayloadFromForm(this.form);
    if (!payload) {
      return;
    }

    if (this.isEditMode) {
      this.dialogRef.close({
        mode: 'edit',
        payload: {
          ...payload,
          isActive: !!this.form.value.isActive
        }
      });
      return;
    }

    this.dialogRef.close({
      mode: 'create',
      payload
    });
  }

  private closeAllEditors(): void {
    this.showOperatingHourEditor = false;
    this.showServiceEditor = false;
    this.showInsurancePlanEditor = false;
    this.showAccreditationEditor = false;
  }

  startAddOperatingHour(): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showOperatingHourEditor = true;
    this.editingOperatingHourDay = null;
    this.operatingHourForm.reset({
      dayOfWeek: 1,
      open: '08:00',
      close: '17:00',
      isClosed: false
    });
  }

  startEditOperatingHour(hour: ClinicOperatingHour): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showOperatingHourEditor = true;
    this.editingOperatingHourDay = hour.dayOfWeek;
    this.operatingHourForm.reset({
      dayOfWeek: hour.dayOfWeek,
      open: hour.open || '08:00',
      close: hour.close || '17:00',
      isClosed: hour.isClosed
    });
  }

  cancelOperatingHourEdit(): void {
    this.showOperatingHourEditor = false;
    this.editingOperatingHourDay = null;
    this.operatingHourForm.reset({
      dayOfWeek: 1,
      open: '08:00',
      close: '17:00',
      isClosed: false
    });
  }

  saveOperatingHour(): void {
    this.errorMessage = '';
    if (this.operatingHourForm.invalid) {
      this.operatingHourForm.markAllAsTouched();
      return;
    }

    const raw = this.operatingHourForm.getRawValue();
    const dayOfWeek = Number(raw.dayOfWeek);
    const isClosed = !!raw.isClosed;
    const open = String(raw.open || '').trim();
    const close = String(raw.close || '').trim();

    if (!isClosed) {
      if (!this.isValidTime(open) || !this.isValidTime(close)) {
        this.errorMessage = 'clinicEditor.errors.operatingHourFormat';
        return;
      }

      if (open >= close) {
        this.errorMessage = 'clinicEditor.errors.operatingHourOrder';
        return;
      }
    }

    const duplicate = this.operatingHoursDraft.find(hour =>
      hour.dayOfWeek === dayOfWeek && hour.dayOfWeek !== this.editingOperatingHourDay);

    if (duplicate) {
      this.errorMessage = this.translate.instant('clinicEditor.errors.operatingHourDuplicate', {
        day: this.translate.instant(this.getDayLabel(dayOfWeek))
      });
      return;
    }

    const item: ClinicOperatingHour = {
      dayOfWeek,
      open: isClosed ? null : open,
      close: isClosed ? null : close,
      isClosed
    };

    if (this.editingOperatingHourDay === null) {
      this.operatingHoursDraft = [...this.operatingHoursDraft, item]
        .sort((a, b) => a.dayOfWeek - b.dayOfWeek);
    } else {
      this.operatingHoursDraft = this.operatingHoursDraft
        .map(hour => (hour.dayOfWeek === this.editingOperatingHourDay ? item : hour))
        .sort((a, b) => a.dayOfWeek - b.dayOfWeek);
    }

    this.cancelOperatingHourEdit();
  }

  removeOperatingHour(dayOfWeek: number): void {
    this.errorMessage = '';
    this.operatingHoursDraft = this.operatingHoursDraft.filter(hour => hour.dayOfWeek !== dayOfWeek);
    if (this.editingOperatingHourDay === dayOfWeek) {
      this.cancelOperatingHourEdit();
    }
  }

  startAddService(): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showServiceEditor = true;
    this.editingServiceOperationId = null;
    this.serviceForm.reset({
      sysOperationId: this.sysOperations[0]?.sysOperationId ?? null,
      isTelehealthAvailable: false
    });
  }

  startEditService(service: ClinicService): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showServiceEditor = true;
    this.editingServiceOperationId = service.sysOperationId;
    this.serviceForm.reset({
      sysOperationId: service.sysOperationId,
      isTelehealthAvailable: service.isTelehealthAvailable
    });
  }

  cancelServiceEdit(): void {
    this.showServiceEditor = false;
    this.editingServiceOperationId = null;
    this.serviceForm.reset({
      sysOperationId: this.sysOperations[0]?.sysOperationId ?? null,
      isTelehealthAvailable: false
    });
  }

  saveService(): void {
    this.errorMessage = '';
    if (this.serviceForm.invalid) {
      this.serviceForm.markAllAsTouched();
      return;
    }

    const raw = this.serviceForm.getRawValue();
    const sysOperationId = Number(raw.sysOperationId);
    const selectedOperation = this.sysOperations.find(operation => operation.sysOperationId === sysOperationId);

    if (!Number.isInteger(sysOperationId) || sysOperationId <= 0 || !selectedOperation) {
      this.errorMessage = 'clinicEditor.errors.invalidServiceOperation';
      return;
    }

    const duplicate = this.servicesDraft.find(service =>
      service.sysOperationId === sysOperationId &&
      service.sysOperationId !== this.editingServiceOperationId);

    if (duplicate) {
      this.errorMessage = this.translate.instant('clinicEditor.errors.serviceDuplicate', {
        service: this.getOperationDisplayName(selectedOperation.sysOperationId, selectedOperation.name)
      });
      return;
    }

    const item: ClinicService = {
      sysOperationId,
      name: selectedOperation.name,
      isTelehealthAvailable: !!raw.isTelehealthAvailable
    };

    if (this.editingServiceOperationId === null) {
      this.servicesDraft = [...this.servicesDraft, item]
        .sort((a, b) => a.name.localeCompare(b.name));
    } else {
      this.servicesDraft = this.servicesDraft
        .map(service => (service.sysOperationId === this.editingServiceOperationId ? item : service))
        .sort((a, b) => a.name.localeCompare(b.name));
    }

    this.cancelServiceEdit();
  }

  removeService(sysOperationId: number): void {
    this.errorMessage = '';
    this.servicesDraft = this.servicesDraft.filter(service => service.sysOperationId !== sysOperationId);
    if (this.editingServiceOperationId === sysOperationId) {
      this.cancelServiceEdit();
    }
  }

  startAddInsurancePlan(): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showInsurancePlanEditor = true;
    this.editingInsurancePlanKey = null;
    this.insurancePlanForm.reset({
      payerName: '',
      planName: '',
      isInNetwork: true
    });
  }

  startEditInsurancePlan(plan: ClinicInsurancePlan): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showInsurancePlanEditor = true;
    this.editingInsurancePlanKey = this.getInsurancePlanKey(plan);
    this.insurancePlanForm.reset({
      payerName: plan.payerName,
      planName: plan.planName,
      isInNetwork: plan.isInNetwork
    });
  }

  cancelInsurancePlanEdit(): void {
    this.showInsurancePlanEditor = false;
    this.editingInsurancePlanKey = null;
    this.insurancePlanForm.reset({
      payerName: '',
      planName: '',
      isInNetwork: true
    });
  }

  saveInsurancePlan(): void {
    this.errorMessage = '';
    if (this.insurancePlanForm.invalid) {
      this.insurancePlanForm.markAllAsTouched();
      return;
    }

    const raw = this.insurancePlanForm.getRawValue();
    const payerName = this.normalizeText(raw.payerName);
    const planName = this.normalizeText(raw.planName);
    if (!payerName || !planName) {
      this.errorMessage = 'clinicEditor.errors.insuranceRequired';
      return;
    }

    const item: ClinicInsurancePlan = {
      payerName,
      planName,
      isInNetwork: !!raw.isInNetwork
    };

    const nextKey = this.getInsurancePlanKey(item);
    const duplicate = this.insurancePlansDraft.find(plan =>
      this.getInsurancePlanKey(plan) === nextKey && this.getInsurancePlanKey(plan) !== this.editingInsurancePlanKey);

    if (duplicate) {
      this.errorMessage = this.translate.instant('clinicEditor.errors.insuranceDuplicate', {
        payer: payerName,
        plan: planName
      });
      return;
    }

    if (this.editingInsurancePlanKey === null) {
      this.insurancePlansDraft = [...this.insurancePlansDraft, item]
        .sort((a, b) => this.getInsurancePlanKey(a).localeCompare(this.getInsurancePlanKey(b)));
    } else {
      this.insurancePlansDraft = this.insurancePlansDraft
        .map(plan => (this.getInsurancePlanKey(plan) === this.editingInsurancePlanKey ? item : plan))
        .sort((a, b) => this.getInsurancePlanKey(a).localeCompare(this.getInsurancePlanKey(b)));
    }

    this.cancelInsurancePlanEdit();
  }

  removeInsurancePlan(plan: ClinicInsurancePlan): void {
    this.errorMessage = '';
    const key = this.getInsurancePlanKey(plan);
    this.insurancePlansDraft = this.insurancePlansDraft
      .filter(current => this.getInsurancePlanKey(current) !== key);

    if (this.editingInsurancePlanKey === key) {
      this.cancelInsurancePlanEdit();
    }
  }

  startAddAccreditation(): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showAccreditationEditor = true;
    this.editingAccreditationId = null;
    this.accreditationForm.reset({
      sysAccreditationId: this.sysAccreditations[0]?.sysAccreditationId ?? null,
      effectiveOn: null,
      expiresOn: null
    });
  }

  startEditAccreditation(accreditation: ClinicAccreditation): void {
    this.errorMessage = '';
    this.closeAllEditors();
    this.showAccreditationEditor = true;
    this.editingAccreditationId = accreditation.sysAccreditationId;
    this.accreditationForm.reset({
      sysAccreditationId: accreditation.sysAccreditationId,
      effectiveOn: this.parseIsoDate(accreditation.effectiveOn),
      expiresOn: this.parseIsoDate(accreditation.expiresOn)
    });
  }

  cancelAccreditationEdit(): void {
    this.showAccreditationEditor = false;
    this.editingAccreditationId = null;
    this.accreditationForm.reset({
      sysAccreditationId: this.sysAccreditations[0]?.sysAccreditationId ?? null,
      effectiveOn: null,
      expiresOn: null
    });
  }

  saveAccreditation(): void {
    this.errorMessage = '';
    if (this.accreditationForm.invalid) {
      this.accreditationForm.markAllAsTouched();
      return;
    }

    const raw = this.accreditationForm.getRawValue();
    const sysAccreditationId = Number(raw.sysAccreditationId);
    const selectedAccreditationType = this.sysAccreditations
      .find(accreditation => accreditation.sysAccreditationId === sysAccreditationId);
    if (!Number.isInteger(sysAccreditationId) || sysAccreditationId <= 0 || !selectedAccreditationType) {
      this.errorMessage = 'clinicEditor.errors.invalidAccreditation';
      return;
    }

    const effectiveOn = this.toIsoDate(raw.effectiveOn);
    const expiresOn = this.toIsoDate(raw.expiresOn);
    if (effectiveOn && expiresOn && effectiveOn > expiresOn) {
      this.errorMessage = this.translate.instant('clinicEditor.errors.accreditationDateOrder', {
        accreditation: this.getAccreditationDisplayName(
          selectedAccreditationType.sysAccreditationId,
          selectedAccreditationType.name
        )
      });
      return;
    }

    const duplicate = this.accreditationsDraft.find(accreditation =>
      accreditation.sysAccreditationId === sysAccreditationId &&
      accreditation.sysAccreditationId !== this.editingAccreditationId);

    if (duplicate) {
      this.errorMessage = this.translate.instant('clinicEditor.errors.accreditationDuplicate', {
        accreditation: this.getAccreditationDisplayName(
          selectedAccreditationType.sysAccreditationId,
          selectedAccreditationType.name
        )
      });
      return;
    }

    const item: ClinicAccreditation = {
      sysAccreditationId,
      name: selectedAccreditationType.name,
      effectiveOn,
      expiresOn
    };

    if (this.editingAccreditationId === null) {
      this.accreditationsDraft = [...this.accreditationsDraft, item]
        .sort((a, b) => a.name.localeCompare(b.name));
    } else {
      this.accreditationsDraft = this.accreditationsDraft
        .map(accreditation => (accreditation.sysAccreditationId === this.editingAccreditationId ? item : accreditation))
        .sort((a, b) => a.name.localeCompare(b.name));
    }

    this.cancelAccreditationEdit();
  }

  removeAccreditation(sysAccreditationId: number): void {
    this.errorMessage = '';
    this.accreditationsDraft = this.accreditationsDraft
      .filter(accreditation => accreditation.sysAccreditationId !== sysAccreditationId);
    if (this.editingAccreditationId === sysAccreditationId) {
      this.cancelAccreditationEdit();
    }
  }

  getDayLabel(dayOfWeek: number): string {
    return this.dayOptions.find(day => day.value === dayOfWeek)?.labelKey ?? 'common.dayUnknown';
  }

  getClinicTypeName(sysClinicTypeId: number, fallbackName = ''): string {
    return this.sysClinicTypes.find(clinicType => clinicType.sysClinicTypeId === sysClinicTypeId)?.name ?? fallbackName;
  }

  getClinicTypeDisplayName(sysClinicTypeId: number, fallbackName = ''): string {
    return this.translateLookupById(`sys.clinicTypes.${sysClinicTypeId}`, this.getClinicTypeName(sysClinicTypeId, fallbackName));
  }

  getOwnershipTypeName(sysOwnershipTypeId: number, fallbackName = ''): string {
    return this.sysOwnershipTypes
      .find(ownershipType => ownershipType.sysOwnershipTypeId === sysOwnershipTypeId)?.name ?? fallbackName;
  }

  getOwnershipTypeDisplayName(sysOwnershipTypeId: number, fallbackName = ''): string {
    return this.translateLookupById(
      `sys.ownershipTypes.${sysOwnershipTypeId}`,
      this.getOwnershipTypeName(sysOwnershipTypeId, fallbackName)
    );
  }

  getSourceSystemName(sysSourceSystemId: number, fallbackName = ''): string {
    return this.sysSourceSystems.find(sourceSystem => sourceSystem.sysSourceSystemId === sysSourceSystemId)?.name ?? fallbackName;
  }

  getSourceSystemDisplayName(sysSourceSystemId: number, fallbackName = ''): string {
    return this.translateLookupById(
      `sys.sourceSystems.${sysSourceSystemId}`,
      this.getSourceSystemName(sysSourceSystemId, fallbackName)
    );
  }

  getOperationName(sysOperationId: number, fallbackName = ''): string {
    return this.sysOperations.find(operation => operation.sysOperationId === sysOperationId)?.name ?? fallbackName;
  }

  getOperationDisplayName(sysOperationId: number, fallbackName = ''): string {
    return this.translateLookupById(`sys.operations.${sysOperationId}`, this.getOperationName(sysOperationId, fallbackName));
  }

  getAccreditationTypeName(sysAccreditationId: number, fallbackName = ''): string {
    return this.sysAccreditations
      .find(accreditation => accreditation.sysAccreditationId === sysAccreditationId)?.name ?? fallbackName;
  }

  getAccreditationDisplayName(sysAccreditationId: number, fallbackName = ''): string {
    return this.translateLookupById(
      `sys.accreditations.${sysAccreditationId}`,
      this.getAccreditationTypeName(sysAccreditationId, fallbackName)
    );
  }

  getInsurancePlanNetworkLabel(plan: ClinicInsurancePlan): string {
    return plan.isInNetwork ? 'statuses.inNetwork' : 'statuses.outOfNetwork';
  }

  getAccreditationWindowText(accreditation: ClinicAccreditation): string {
    if (accreditation.effectiveOn && accreditation.expiresOn) {
      return `${accreditation.effectiveOn} - ${accreditation.expiresOn}`;
    }

    if (accreditation.effectiveOn) {
      return this.translate.instant('clinicEditor.accreditationEffectiveOnly', {
        date: accreditation.effectiveOn
      });
    }

    if (accreditation.expiresOn) {
      return this.translate.instant('clinicEditor.accreditationExpiresOnly', {
        date: accreditation.expiresOn
      });
    }

    return this.translate.instant('clinicEditor.noValidityWindow');
  }

  private buildPayloadFromForm(form: FormGroup): ClinicUpsertPayload | null {
    const raw = form.getRawValue();

    return {
      name: this.normalizeText(raw.name),
      code: this.normalizeText(raw.code),
      legalName: this.normalizeText(raw.legalName),
      sysClinicTypeId: this.normalizeLookupId(raw.sysClinicTypeId),
      sysOwnershipTypeId: this.normalizeLookupId(raw.sysOwnershipTypeId),
      foundedOn: this.normalizeDate(raw.foundedOn),
      npiOrganization: this.normalizeText(raw.npiOrganization),
      ein: this.normalizeText(raw.ein),
      taxonomyCode: this.normalizeText(raw.taxonomyCode),
      stateLicenseFacility: this.normalizeText(raw.stateLicenseFacility),
      cliaNumber: this.normalizeText(raw.cliaNumber),
      addressLine1: this.normalizeText(raw.addressLine1),
      addressLine2: this.normalizeText(raw.addressLine2),
      city: this.normalizeText(raw.city),
      state: this.normalizeText(raw.state),
      postalCode: this.normalizeText(raw.postalCode),
      countryCode: this.normalizeCountryCode(raw.countryCode),
      timezone: this.normalizeText(raw.timezone) || 'America/Chicago',
      mainPhone: this.normalizeText(raw.mainPhone),
      fax: this.normalizeText(raw.fax),
      mainEmail: this.normalizeText(raw.mainEmail).toLowerCase(),
      websiteUrl: this.normalizeText(raw.websiteUrl),
      patientPortalUrl: this.normalizeText(raw.patientPortalUrl),
      bookingMethods: this.parseBookingMethods(raw.bookingMethodsInput || ''),
      avgNewPatientWaitDays: this.normalizeInteger(raw.avgNewPatientWaitDays),
      sameDayAvailable: !!raw.sameDayAvailable,
      hipaaNoticeVersion: this.normalizeText(raw.hipaaNoticeVersion),
      lastSecurityRiskAssessmentOn: this.normalizeDate(raw.lastSecurityRiskAssessmentOn),
      sysSourceSystemId: this.normalizeLookupId(raw.sysSourceSystemId),
      operatingHours: [...this.operatingHoursDraft]
        .sort((a, b) => a.dayOfWeek - b.dayOfWeek),
      services: [...this.servicesDraft]
        .sort((a, b) => a.name.localeCompare(b.name)),
      insurancePlans: [...this.insurancePlansDraft]
        .sort((a, b) => this.getInsurancePlanKey(a).localeCompare(this.getInsurancePlanKey(b))),
      accreditations: [...this.accreditationsDraft]
        .sort((a, b) => a.name.localeCompare(b.name))
    };
  }

  private parseBookingMethods(input: string): string[] {
    return input
      .split(',')
      .map(method => method.trim().toLowerCase())
      .filter(Boolean)
      .filter((value, index, all) => all.indexOf(value) === index);
  }

  private normalizeText(value: unknown): string {
    return String(value || '').trim();
  }

  private normalizeDate(value: unknown): string | null {
    return this.toIsoDate(value);
  }

  private normalizeCountryCode(value: unknown): string {
    const normalized = this.normalizeText(value).toUpperCase();
    return normalized.length === 2 ? normalized : 'US';
  }

  private normalizeInteger(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const parsed = Number(value);
    if (!Number.isFinite(parsed)) {
      return null;
    }

    return Math.trunc(parsed);
  }

  private normalizeLookupId(value: unknown): number {
    const parsed = Number(value);
    if (!Number.isInteger(parsed) || parsed <= 0) {
      return 1;
    }

    return parsed;
  }

  private getInsurancePlanKey(plan: ClinicInsurancePlan): string {
    return `${plan.payerName.trim().toLowerCase()}|${plan.planName.trim().toLowerCase()}`;
  }

  private translateLookupById(key: string, fallbackName: string): string {
    const translated = this.translate.instant(key);
    if (translated && translated !== key) {
      return translated;
    }

    return fallbackName;
  }

  private isValidTime(value: string): boolean {
    return /^([01]\d|2[0-3]):[0-5]\d$/.test(value);
  }

  private parseIsoDate(value: string | null | undefined): Date | null {
    if (!value) {
      return null;
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return null;
    }

    return parsed;
  }

  private toIsoDate(value: unknown): string | null {
    if (!value) {
      return null;
    }

    if (value instanceof Date) {
      if (Number.isNaN(value.getTime())) {
        return null;
      }

      const year = value.getFullYear();
      const month = String(value.getMonth() + 1).padStart(2, '0');
      const day = String(value.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    }

    const raw = this.normalizeText(value);
    if (!raw) {
      return null;
    }

    if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
      return raw;
    }

    const parsed = new Date(raw);
    if (Number.isNaN(parsed.getTime())) {
      return null;
    }

    const year = parsed.getFullYear();
    const month = String(parsed.getMonth() + 1).padStart(2, '0');
    const day = String(parsed.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
