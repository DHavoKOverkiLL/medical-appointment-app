export interface AdminDashboardResponse {
  clinics: number;
  users: number;
  doctors: number;
  patients: number;
  admins: number;
  appointmentsToday: number;
  upcomingAppointments: number;
  clinicLoad: ClinicLoadResponse[];
}

export interface ClinicLoadResponse {
  clinicId: string;
  clinicName: string;
  users: number;
  appointments: number;
}

export interface SysOperationSummary {
  sysOperationId: number;
  name: string;
}

export interface SysAccreditationSummary {
  sysAccreditationId: number;
  name: string;
}

export interface SysClinicTypeSummary {
  sysClinicTypeId: number;
  name: string;
}

export interface SysOwnershipTypeSummary {
  sysOwnershipTypeId: number;
  name: string;
}

export interface SysSourceSystemSummary {
  sysSourceSystemId: number;
  name: string;
}

export interface DoctorDashboardResponse {
  clinicName: string;
  appointmentsToday: number;
  upcomingAppointments: number;
  uniquePatientsUpcoming: number;
  nextAppointmentUtc: string | null;
}

export interface PatientDashboardResponse {
  clinicName: string;
  upcomingAppointments: number;
  nextAppointmentUtc: string | null;
  doctorsInClinic: number;
}

export interface AvailableAppointmentSlot {
  slotUtc: string;
  localTime: string;
}

export interface AvailableAppointmentSlotsResponse {
  doctorId: string;
  clinicId: string;
  date: string;
  timezone: string;
  slotDurationMinutes: number;
  slots: AvailableAppointmentSlot[];
}

export interface DoctorWeeklyTimeRange {
  dayOfWeek: number;
  start: string;
  end: string;
}

export interface DoctorDateOverride {
  date: string;
  start: string | null;
  end: string | null;
  isAvailable: boolean;
  reason: string;
}

export interface DoctorAvailabilityResponse {
  doctorId: string;
  clinicId: string;
  timezone: string;
  weeklyAvailability: DoctorWeeklyTimeRange[];
  weeklyBreaks: DoctorWeeklyTimeRange[];
  overrides: DoctorDateOverride[];
}

export interface PatientAppointment {
  appointmentId: string;
  doctorId: string;
  status: string;
  appointmentDateTime: string;
  doctorName: string;
  clinicName: string;
  postponeRequestStatus: string;
  proposedDateTime: string | null;
  postponeReason: string | null;
  postponeRequestedAtUtc: string | null;
  doctorResponseNote: string | null;
  doctorRespondedAtUtc: string | null;
  patientRespondedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancelledByUserId: string | null;
  cancellationReason: string | null;
}

export interface UserSummary {
  userId: string;
  username: string;
  email: string;
  role: string;
  firstName: string;
  lastName: string;
  personalIdentifier: string;
  address: string;
  phoneNumber?: string | null;
  birthDate: string;
  clinicId: string;
  clinicName: string;
}

export interface RoleSummary {
  sysRoleId: string;
  name: string;
  description: string | null;
}

export interface SystemInfoResponse {
  roles: RoleSummary[];
  operations: SysOperationSummary[];
  accreditations: SysAccreditationSummary[];
  clinicTypes: SysClinicTypeSummary[];
  ownershipTypes: SysOwnershipTypeSummary[];
  sourceSystems: SysSourceSystemSummary[];
}

export interface ClinicSummary {
  clinicId: string;
  name: string;
  code: string;
  legalName: string;
  sysClinicTypeId: number;
  clinicType: string;
  sysOwnershipTypeId: number;
  ownershipType: string;
  foundedOn: string | null;
  npiOrganization: string;
  ein: string;
  taxonomyCode: string;
  stateLicenseFacility: string;
  cliaNumber: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  timezone: string;
  mainPhone: string;
  fax: string;
  mainEmail: string;
  websiteUrl: string;
  patientPortalUrl: string;
  bookingMethods: string[];
  avgNewPatientWaitDays: number | null;
  sameDayAvailable: boolean;
  hipaaNoticeVersion: string;
  lastSecurityRiskAssessmentOn: string | null;
  sysSourceSystemId: number;
  sourceSystem: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  operatingHours: ClinicOperatingHour[];
  services: ClinicService[];
  insurancePlans: ClinicInsurancePlan[];
  accreditations: ClinicAccreditation[];
  isActive: boolean;
  usersCount: number;
}

export interface ClinicUpsertPayload {
  name: string;
  code: string;
  legalName: string;
  sysClinicTypeId: number;
  sysOwnershipTypeId: number;
  foundedOn: string | null;
  npiOrganization: string;
  ein: string;
  taxonomyCode: string;
  stateLicenseFacility: string;
  cliaNumber: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  timezone: string;
  mainPhone: string;
  fax: string;
  mainEmail: string;
  websiteUrl: string;
  patientPortalUrl: string;
  bookingMethods: string[];
  avgNewPatientWaitDays: number | null;
  sameDayAvailable: boolean;
  hipaaNoticeVersion: string;
  lastSecurityRiskAssessmentOn: string | null;
  sysSourceSystemId: number;
  operatingHours: ClinicOperatingHour[];
  services: ClinicService[];
  insurancePlans: ClinicInsurancePlan[];
  accreditations: ClinicAccreditation[];
}

export interface ClinicOperatingHour {
  dayOfWeek: number;
  open: string | null;
  close: string | null;
  isClosed: boolean;
}

export interface ClinicService {
  sysOperationId: number;
  name: string;
  isTelehealthAvailable: boolean;
}

export interface ClinicInsurancePlan {
  payerName: string;
  planName: string;
  isInNetwork: boolean;
}

export interface ClinicAccreditation {
  sysAccreditationId: number;
  name: string;
  effectiveOn: string | null;
  expiresOn: string | null;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  address: string;
  phoneNumber?: string | null;
  birthDate: string;
}

export interface UpdateAccountSettingsRequest {
  username: string;
  email: string;
  currentPassword: string;
  newPassword?: string;
}
