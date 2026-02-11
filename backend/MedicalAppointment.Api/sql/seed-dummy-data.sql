SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ClinicId uniqueidentifier =
    (
        SELECT TOP (1) ClinicId
        FROM Clinics
        WHERE IsActive = 1
        ORDER BY Name
    );

    IF @ClinicId IS NULL
    BEGIN
        SET @ClinicId = NEWID();
        INSERT INTO Clinics (ClinicId, Name, Code, IsActive)
        VALUES (@ClinicId, 'Downtown Health Hub', 'DHH', 1);
    END;

    SET IDENTITY_INSERT SysClinicTypes ON;

    IF NOT EXISTS (SELECT 1 FROM SysClinicTypes WHERE SysClinicTypeId = 1)
        INSERT INTO SysClinicTypes (SysClinicTypeId, Name, IsActive) VALUES (1, 'Primary Care', 1);
    IF NOT EXISTS (SELECT 1 FROM SysClinicTypes WHERE SysClinicTypeId = 2)
        INSERT INTO SysClinicTypes (SysClinicTypeId, Name, IsActive) VALUES (2, 'Specialty Care', 1);
    IF NOT EXISTS (SELECT 1 FROM SysClinicTypes WHERE SysClinicTypeId = 3)
        INSERT INTO SysClinicTypes (SysClinicTypeId, Name, IsActive) VALUES (3, 'Urgent Care', 1);
    IF NOT EXISTS (SELECT 1 FROM SysClinicTypes WHERE SysClinicTypeId = 4)
        INSERT INTO SysClinicTypes (SysClinicTypeId, Name, IsActive) VALUES (4, 'Multi-specialty Clinic', 1);
    IF NOT EXISTS (SELECT 1 FROM SysClinicTypes WHERE SysClinicTypeId = 5)
        INSERT INTO SysClinicTypes (SysClinicTypeId, Name, IsActive) VALUES (5, 'Community Health Center', 1);

    SET IDENTITY_INSERT SysClinicTypes OFF;

    DECLARE @CurrentMaxSysClinicTypeId int = (SELECT ISNULL(MAX(SysClinicTypeId), 0) FROM SysClinicTypes);
    DECLARE @ReseedSysClinicTypesSql nvarchar(200) =
        N'DBCC CHECKIDENT (''SysClinicTypes'', RESEED, ' + CAST(@CurrentMaxSysClinicTypeId AS nvarchar(20)) + N')';
    EXEC (@ReseedSysClinicTypesSql);

    SET IDENTITY_INSERT SysOwnershipTypes ON;

    IF NOT EXISTS (SELECT 1 FROM SysOwnershipTypes WHERE SysOwnershipTypeId = 1)
        INSERT INTO SysOwnershipTypes (SysOwnershipTypeId, Name, IsActive) VALUES (1, 'Physician Owned', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOwnershipTypes WHERE SysOwnershipTypeId = 2)
        INSERT INTO SysOwnershipTypes (SysOwnershipTypeId, Name, IsActive) VALUES (2, 'Hospital Owned', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOwnershipTypes WHERE SysOwnershipTypeId = 3)
        INSERT INTO SysOwnershipTypes (SysOwnershipTypeId, Name, IsActive) VALUES (3, 'Health System', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOwnershipTypes WHERE SysOwnershipTypeId = 4)
        INSERT INTO SysOwnershipTypes (SysOwnershipTypeId, Name, IsActive) VALUES (4, 'Academic', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOwnershipTypes WHERE SysOwnershipTypeId = 5)
        INSERT INTO SysOwnershipTypes (SysOwnershipTypeId, Name, IsActive) VALUES (5, 'Government', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOwnershipTypes WHERE SysOwnershipTypeId = 6)
        INSERT INTO SysOwnershipTypes (SysOwnershipTypeId, Name, IsActive) VALUES (6, 'Nonprofit', 1);

    SET IDENTITY_INSERT SysOwnershipTypes OFF;

    DECLARE @CurrentMaxSysOwnershipTypeId int = (SELECT ISNULL(MAX(SysOwnershipTypeId), 0) FROM SysOwnershipTypes);
    DECLARE @ReseedSysOwnershipTypesSql nvarchar(200) =
        N'DBCC CHECKIDENT (''SysOwnershipTypes'', RESEED, ' + CAST(@CurrentMaxSysOwnershipTypeId AS nvarchar(20)) + N')';
    EXEC (@ReseedSysOwnershipTypesSql);

    SET IDENTITY_INSERT SysSourceSystems ON;

    IF NOT EXISTS (SELECT 1 FROM SysSourceSystems WHERE SysSourceSystemId = 1)
        INSERT INTO SysSourceSystems (SysSourceSystemId, Name, IsActive) VALUES (1, 'EHR', 1);
    IF NOT EXISTS (SELECT 1 FROM SysSourceSystems WHERE SysSourceSystemId = 2)
        INSERT INTO SysSourceSystems (SysSourceSystemId, Name, IsActive) VALUES (2, 'EMR', 1);
    IF NOT EXISTS (SELECT 1 FROM SysSourceSystems WHERE SysSourceSystemId = 3)
        INSERT INTO SysSourceSystems (SysSourceSystemId, Name, IsActive) VALUES (3, 'Custom Platform', 1);
    IF NOT EXISTS (SELECT 1 FROM SysSourceSystems WHERE SysSourceSystemId = 4)
        INSERT INTO SysSourceSystems (SysSourceSystemId, Name, IsActive) VALUES (4, 'Legacy PMS', 1);

    SET IDENTITY_INSERT SysSourceSystems OFF;

    DECLARE @CurrentMaxSysSourceSystemId int = (SELECT ISNULL(MAX(SysSourceSystemId), 0) FROM SysSourceSystems);
    DECLARE @ReseedSysSourceSystemsSql nvarchar(200) =
        N'DBCC CHECKIDENT (''SysSourceSystems'', RESEED, ' + CAST(@CurrentMaxSysSourceSystemId AS nvarchar(20)) + N')';
    EXEC (@ReseedSysSourceSystemsSql);

    DECLARE @DefaultClinicTypeId int =
    (
        SELECT TOP (1) SysClinicTypeId
        FROM SysClinicTypes
        WHERE Name = 'Primary Care'
        ORDER BY SysClinicTypeId
    );

    DECLARE @DefaultOwnershipTypeId int =
    (
        SELECT TOP (1) SysOwnershipTypeId
        FROM SysOwnershipTypes
        WHERE Name = 'Physician Owned'
        ORDER BY SysOwnershipTypeId
    );

    DECLARE @DefaultSourceSystemId int =
    (
        SELECT TOP (1) SysSourceSystemId
        FROM SysSourceSystems
        WHERE Name = 'EHR'
        ORDER BY SysSourceSystemId
    );

    UPDATE Clinics
    SET
        LegalName = CASE WHEN ISNULL(LTRIM(RTRIM(LegalName)), '') = '' THEN Name + ' PLLC' ELSE LegalName END,
        SysClinicTypeId = ISNULL(SysClinicTypeId, @DefaultClinicTypeId),
        SysOwnershipTypeId = ISNULL(SysOwnershipTypeId, @DefaultOwnershipTypeId),
        FoundedOn = ISNULL(FoundedOn, '2017-04-10'),
        NpiOrganization = CASE WHEN ISNULL(LTRIM(RTRIM(NpiOrganization)), '') = '' THEN '1234567890' ELSE NpiOrganization END,
        Ein = CASE WHEN ISNULL(LTRIM(RTRIM(Ein)), '') = '' THEN '12-3456789' ELSE Ein END,
        TaxonomyCode = CASE WHEN ISNULL(LTRIM(RTRIM(TaxonomyCode)), '') = '' THEN '207Q00000X' ELSE TaxonomyCode END,
        StateLicenseFacility = CASE WHEN ISNULL(LTRIM(RTRIM(StateLicenseFacility)), '') = '' THEN 'FAC-2024-11892' ELSE StateLicenseFacility END,
        CliaNumber = CASE WHEN ISNULL(LTRIM(RTRIM(CliaNumber)), '') = '' THEN '12D3456789' ELSE CliaNumber END,
        AddressLine1 = CASE WHEN ISNULL(LTRIM(RTRIM(AddressLine1)), '') = '' THEN '1450 Oak Street' ELSE AddressLine1 END,
        AddressLine2 = CASE WHEN ISNULL(LTRIM(RTRIM(AddressLine2)), '') = '' THEN 'Suite 220' ELSE AddressLine2 END,
        City = CASE WHEN ISNULL(LTRIM(RTRIM(City)), '') = '' THEN 'Austin' ELSE City END,
        [State] = CASE WHEN ISNULL(LTRIM(RTRIM([State])), '') = '' THEN 'TX' ELSE [State] END,
        PostalCode = CASE WHEN ISNULL(LTRIM(RTRIM(PostalCode)), '') = '' THEN '78701' ELSE PostalCode END,
        CountryCode = CASE WHEN ISNULL(LTRIM(RTRIM(CountryCode)), '') = '' THEN 'US' ELSE CountryCode END,
        Timezone = CASE WHEN ISNULL(LTRIM(RTRIM(Timezone)), '') = '' THEN 'America/Chicago' ELSE Timezone END,
        MainPhone = CASE WHEN ISNULL(LTRIM(RTRIM(MainPhone)), '') = '' THEN '+1-512-555-0142' ELSE MainPhone END,
        Fax = CASE WHEN ISNULL(LTRIM(RTRIM(Fax)), '') = '' THEN '+1-512-555-0199' ELSE Fax END,
        MainEmail = CASE WHEN ISNULL(LTRIM(RTRIM(MainEmail)), '') = '' THEN 'frontdesk@medio.local' ELSE MainEmail END,
        WebsiteUrl = CASE WHEN ISNULL(LTRIM(RTRIM(WebsiteUrl)), '') = '' THEN 'https://medio.local/clinic' ELSE WebsiteUrl END,
        PatientPortalUrl = CASE WHEN ISNULL(LTRIM(RTRIM(PatientPortalUrl)), '') = '' THEN 'https://portal.medio.local' ELSE PatientPortalUrl END,
        BookingMethods = CASE WHEN ISNULL(LTRIM(RTRIM(BookingMethods)), '') = '' THEN 'phone,portal' ELSE BookingMethods END,
        AvgNewPatientWaitDays = ISNULL(AvgNewPatientWaitDays, 12),
        SameDayAvailable = ISNULL(SameDayAvailable, 1),
        HipaaNoticeVersion = CASE WHEN ISNULL(LTRIM(RTRIM(HipaaNoticeVersion)), '') = '' THEN '2025.1' ELSE HipaaNoticeVersion END,
        LastSecurityRiskAssessmentOn = ISNULL(LastSecurityRiskAssessmentOn, '2025-11-08'),
        SysSourceSystemId = ISNULL(SysSourceSystemId, @DefaultSourceSystemId),
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE ClinicId = @ClinicId;

    IF NOT EXISTS (SELECT 1 FROM ClinicOperatingHours WHERE ClinicId = @ClinicId)
    BEGIN
        INSERT INTO ClinicOperatingHours (ClinicOperatingHourId, ClinicId, DayOfWeek, OpenTime, CloseTime, IsClosed)
        VALUES
            (NEWID(), @ClinicId, 1, '08:00', '17:00', 0),
            (NEWID(), @ClinicId, 2, '08:00', '17:00', 0),
            (NEWID(), @ClinicId, 3, '08:00', '17:00', 0),
            (NEWID(), @ClinicId, 4, '08:00', '17:00', 0),
            (NEWID(), @ClinicId, 5, '08:00', '15:00', 0),
            (NEWID(), @ClinicId, 0, NULL, NULL, 1),
            (NEWID(), @ClinicId, 6, NULL, NULL, 1);
    END;

    SET IDENTITY_INSERT SysOperations ON;

    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 1)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (1, 'Annual Wellness Visit', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 2)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (2, 'Primary Care Follow-up', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 3)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (3, 'Chronic Care Management', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 4)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (4, 'Preventive Screening', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 5)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (5, 'Vaccination', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 6)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (6, 'Urgent Care Visit', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 7)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (7, 'Telehealth Consultation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 8)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (8, 'Lab Testing', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 9)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (9, 'Minor Procedure', 1);
    IF NOT EXISTS (SELECT 1 FROM SysOperations WHERE SysOperationId = 10)
        INSERT INTO SysOperations (SysOperationId, Name, IsActive) VALUES (10, 'Behavioral Health Consultation', 1);

    SET IDENTITY_INSERT SysOperations OFF;

    DECLARE @CurrentMaxSysOperationId int = (SELECT ISNULL(MAX(SysOperationId), 0) FROM SysOperations);
    DECLARE @ReseedSysOperationsSql nvarchar(200) =
        N'DBCC CHECKIDENT (''SysOperations'', RESEED, ' + CAST(@CurrentMaxSysOperationId AS nvarchar(20)) + N')';
    EXEC (@ReseedSysOperationsSql);

    IF NOT EXISTS (SELECT 1 FROM ClinicServices WHERE ClinicId = @ClinicId)
    BEGIN
        INSERT INTO ClinicServices (ClinicServiceId, ClinicId, SysOperationId, IsTelehealthAvailable, IsActive)
        VALUES
            (NEWID(), @ClinicId, 1, 0, 1),
            (NEWID(), @ClinicId, 2, 1, 1),
            (NEWID(), @ClinicId, 3, 1, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM ClinicInsurancePlans WHERE ClinicId = @ClinicId)
    BEGIN
        INSERT INTO ClinicInsurancePlans (ClinicInsurancePlanId, ClinicId, PayerName, PlanName, IsInNetwork, IsActive)
        VALUES
            (NEWID(), @ClinicId, 'Aetna', 'PPO', 1, 1),
            (NEWID(), @ClinicId, 'Blue Cross Blue Shield', 'HMO', 1, 1),
            (NEWID(), @ClinicId, 'Cigna', 'Open Access Plus', 1, 1);
    END;

    SET IDENTITY_INSERT SysAccreditations ON;

    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 1)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (1, 'Joint Commission Ambulatory Care', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 2)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (2, 'NCQA Patient-Centered Medical Home', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 3)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (3, 'AAAHC Ambulatory Health Care', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 4)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (4, 'URAC Telehealth Accreditation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 5)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (5, 'CAP Laboratory Accreditation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 6)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (6, 'CARF Health Program Accreditation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 7)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (7, 'ACR Imaging Accreditation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 8)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (8, 'COLA Laboratory Accreditation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 9)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (9, 'ACHC Ambulatory Care Accreditation', 1);
    IF NOT EXISTS (SELECT 1 FROM SysAccreditations WHERE SysAccreditationId = 10)
        INSERT INTO SysAccreditations (SysAccreditationId, Name, IsActive) VALUES (10, 'ISO 15189 Medical Laboratory', 1);

    SET IDENTITY_INSERT SysAccreditations OFF;

    DECLARE @CurrentMaxSysAccreditationId int = (SELECT ISNULL(MAX(SysAccreditationId), 0) FROM SysAccreditations);
    DECLARE @ReseedSysAccreditationsSql nvarchar(200) =
        N'DBCC CHECKIDENT (''SysAccreditations'', RESEED, ' + CAST(@CurrentMaxSysAccreditationId AS nvarchar(20)) + N')';
    EXEC (@ReseedSysAccreditationsSql);

    IF NOT EXISTS (SELECT 1 FROM ClinicAccreditations WHERE ClinicId = @ClinicId)
    BEGIN
        INSERT INTO ClinicAccreditations (ClinicAccreditationId, ClinicId, SysAccreditationId, EffectiveOn, ExpiresOn, IsActive)
        VALUES
            (NEWID(), @ClinicId, 2, '2024-01-01', '2027-01-01', 1),
            (NEWID(), @ClinicId, 1, '2023-07-01', '2026-07-01', 1);
    END;

    DECLARE @CanonicalJointCommissionId int =
    (
        SELECT TOP (1) SysAccreditationId
        FROM SysAccreditations
        WHERE Name = 'Joint Commission Ambulatory Care'
    );

    DECLARE @CanonicalNcqaId int =
    (
        SELECT TOP (1) SysAccreditationId
        FROM SysAccreditations
        WHERE Name = 'NCQA Patient-Centered Medical Home'
    );

    DECLARE @LegacyJointCommissionId int =
    (
        SELECT TOP (1) SysAccreditationId
        FROM SysAccreditations
        WHERE Name = 'Joint Commission Ambulatory'
    );

    DECLARE @LegacyNcqaId int =
    (
        SELECT TOP (1) SysAccreditationId
        FROM SysAccreditations
        WHERE Name = 'NCQA-PCMH'
    );

    DECLARE @AccreditationCleanupMap TABLE
    (
        LegacyId int NOT NULL,
        CanonicalId int NOT NULL
    );

    IF @LegacyJointCommissionId IS NOT NULL AND @CanonicalJointCommissionId IS NOT NULL AND @LegacyJointCommissionId <> @CanonicalJointCommissionId
        INSERT INTO @AccreditationCleanupMap (LegacyId, CanonicalId) VALUES (@LegacyJointCommissionId, @CanonicalJointCommissionId);

    IF @LegacyNcqaId IS NOT NULL AND @CanonicalNcqaId IS NOT NULL AND @LegacyNcqaId <> @CanonicalNcqaId
        INSERT INTO @AccreditationCleanupMap (LegacyId, CanonicalId) VALUES (@LegacyNcqaId, @CanonicalNcqaId);

    ;WITH overlappingPairs AS
    (
        SELECT
            legacy.ClinicAccreditationId AS LegacyRowId,
            canonical.ClinicAccreditationId AS CanonicalRowId
        FROM @AccreditationCleanupMap mapping
        INNER JOIN ClinicAccreditations legacy
            ON legacy.SysAccreditationId = mapping.LegacyId
        INNER JOIN ClinicAccreditations canonical
            ON canonical.ClinicId = legacy.ClinicId
           AND canonical.SysAccreditationId = mapping.CanonicalId
    )
    UPDATE canonical
    SET
        EffectiveOn = CASE
            WHEN canonical.EffectiveOn IS NULL THEN legacy.EffectiveOn
            WHEN legacy.EffectiveOn IS NULL THEN canonical.EffectiveOn
            WHEN legacy.EffectiveOn < canonical.EffectiveOn THEN legacy.EffectiveOn
            ELSE canonical.EffectiveOn
        END,
        ExpiresOn = CASE
            WHEN canonical.ExpiresOn IS NULL THEN legacy.ExpiresOn
            WHEN legacy.ExpiresOn IS NULL THEN canonical.ExpiresOn
            WHEN legacy.ExpiresOn > canonical.ExpiresOn THEN legacy.ExpiresOn
            ELSE canonical.ExpiresOn
        END,
        IsActive = CASE WHEN canonical.IsActive = 1 OR legacy.IsActive = 1 THEN 1 ELSE 0 END
    FROM overlappingPairs pair
    INNER JOIN ClinicAccreditations canonical
        ON canonical.ClinicAccreditationId = pair.CanonicalRowId
    INNER JOIN ClinicAccreditations legacy
        ON legacy.ClinicAccreditationId = pair.LegacyRowId;

    DELETE legacy
    FROM ClinicAccreditations legacy
    INNER JOIN @AccreditationCleanupMap mapping
        ON legacy.SysAccreditationId = mapping.LegacyId
    WHERE EXISTS
    (
        SELECT 1
        FROM ClinicAccreditations canonical
        WHERE canonical.ClinicId = legacy.ClinicId
          AND canonical.SysAccreditationId = mapping.CanonicalId
    );

    UPDATE clinicAccreditation
    SET SysAccreditationId = mapping.CanonicalId
    FROM ClinicAccreditations clinicAccreditation
    INNER JOIN @AccreditationCleanupMap mapping
        ON clinicAccreditation.SysAccreditationId = mapping.LegacyId;

    ;WITH rankedAccreditations AS
    (
        SELECT
            ClinicAccreditationId,
            ROW_NUMBER() OVER
            (
                PARTITION BY ClinicId, SysAccreditationId
                ORDER BY ClinicAccreditationId
            ) AS RowNumber
        FROM ClinicAccreditations
    )
    DELETE FROM ClinicAccreditations
    WHERE ClinicAccreditationId IN
    (
        SELECT ClinicAccreditationId
        FROM rankedAccreditations
        WHERE RowNumber > 1
    );

    UPDATE legacy
    SET IsActive = 0
    FROM SysAccreditations legacy
    INNER JOIN @AccreditationCleanupMap mapping
        ON legacy.SysAccreditationId = mapping.LegacyId;

    DELETE legacy
    FROM SysAccreditations legacy
    INNER JOIN @AccreditationCleanupMap mapping
        ON legacy.SysAccreditationId = mapping.LegacyId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM ClinicAccreditations clinicAccreditation
        WHERE clinicAccreditation.SysAccreditationId = legacy.SysAccreditationId
    );

    DECLARE @PatientRoleId uniqueidentifier =
    (
        SELECT TOP (1) SysRoleId
        FROM SysRoles
        WHERE Name = 'Patient'
    );

    DECLARE @DoctorRoleId uniqueidentifier =
    (
        SELECT TOP (1) SysRoleId
        FROM SysRoles
        WHERE Name = 'Doctor'
    );

    DECLARE @AdminRoleId uniqueidentifier =
    (
        SELECT TOP (1) SysRoleId
        FROM SysRoles
        WHERE Name = 'Admin'
    );

    IF @PatientRoleId IS NULL
    BEGIN
        SET @PatientRoleId = NEWID();
        INSERT INTO SysRoles (SysRoleId, Name, Description, IsActive)
        VALUES (@PatientRoleId, 'Patient', 'Patient role', 1);
    END;

    IF @DoctorRoleId IS NULL
    BEGIN
        SET @DoctorRoleId = NEWID();
        INSERT INTO SysRoles (SysRoleId, Name, Description, IsActive)
        VALUES (@DoctorRoleId, 'Doctor', 'Doctor role', 1);
    END;

    IF @AdminRoleId IS NULL
    BEGIN
        SET @AdminRoleId = NEWID();
        INSERT INTO SysRoles (SysRoleId, Name, Description, IsActive)
        VALUES (@AdminRoleId, 'Admin', 'Administrator role', 1);
    END;

    DECLARE @ReferencePasswordHash nvarchar(max) =
    (
        SELECT TOP (1) PasswordHash
        FROM Users
        ORDER BY UserId
    );

    IF @ReferencePasswordHash IS NULL
    BEGIN
        -- Fallback hash (password unknown) if database was empty before seed.
        SET @ReferencePasswordHash = 'AQAAAAIAAYagAAAAEPub0m3vWm9Y2kMtC7m6S2mGF0J2H1W6rH8q6NQ33Dfxs2OVS6sM3rWk0jvX5A0eQw==';
    END;

    DECLARE @DoctorAlexPersonId uniqueidentifier =
    (
        SELECT TOP (1) PersonId
        FROM Persons
        WHERE PersonalIdentifier = '5000101001001'
    );

    IF @DoctorAlexPersonId IS NULL
    BEGIN
        SET @DoctorAlexPersonId = NEWID();
        INSERT INTO Persons (PersonId, FirstName, LastName, NormalizedName, PersonalIdentifier, Address, BirthDate)
        VALUES (@DoctorAlexPersonId, 'Alex', 'Munteanu', 'ALEXMUNTEANU', '5000101001001', '12 Clinic Avenue', '1985-01-01');
    END;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'doctor.alex@medio.local')
    BEGIN
        INSERT INTO Users (UserId, Username, Email, PasswordHash, SysRoleId, PersonId, ClinicId)
        VALUES (NEWID(), 'doctor.alex', 'doctor.alex@medio.local', @ReferencePasswordHash, @DoctorRoleId, @DoctorAlexPersonId, @ClinicId);
    END;

    DECLARE @DoctorMayaPersonId uniqueidentifier =
    (
        SELECT TOP (1) PersonId
        FROM Persons
        WHERE PersonalIdentifier = '5000202001002'
    );

    IF @DoctorMayaPersonId IS NULL
    BEGIN
        SET @DoctorMayaPersonId = NEWID();
        INSERT INTO Persons (PersonId, FirstName, LastName, NormalizedName, PersonalIdentifier, Address, BirthDate)
        VALUES (@DoctorMayaPersonId, 'Maya', 'Petrescu', 'MAYAPETRESCU', '5000202001002', '18 Clinic Avenue', '1987-02-02');
    END;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'doctor.maya@medio.local')
    BEGIN
        INSERT INTO Users (UserId, Username, Email, PasswordHash, SysRoleId, PersonId, ClinicId)
        VALUES (NEWID(), 'doctor.maya', 'doctor.maya@medio.local', @ReferencePasswordHash, @DoctorRoleId, @DoctorMayaPersonId, @ClinicId);
    END;

    DECLARE @AdminPersonId uniqueidentifier =
    (
        SELECT TOP (1) PersonId
        FROM Persons
        WHERE PersonalIdentifier = '5000303001003'
    );

    IF @AdminPersonId IS NULL
    BEGIN
        SET @AdminPersonId = NEWID();
        INSERT INTO Persons (PersonId, FirstName, LastName, NormalizedName, PersonalIdentifier, Address, BirthDate)
        VALUES (@AdminPersonId, 'Dana', 'Ionescu', 'DANAIONESCU', '5000303001003', '20 Operations Street', '1982-03-03');
    END;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin.clinic@medio.local')
    BEGIN
        INSERT INTO Users (UserId, Username, Email, PasswordHash, SysRoleId, PersonId, ClinicId)
        VALUES (NEWID(), 'admin.clinic', 'admin.clinic@medio.local', @ReferencePasswordHash, @AdminRoleId, @AdminPersonId, @ClinicId);
    END;

    DECLARE @PatientUserId uniqueidentifier =
    (
        SELECT TOP (1) u.UserId
        FROM Users u
        JOIN SysRoles r ON r.SysRoleId = u.SysRoleId
        WHERE r.Name = 'Patient' AND u.ClinicId = @ClinicId
        ORDER BY u.UserId
    );

    IF @PatientUserId IS NULL
    BEGIN
        DECLARE @PatientPersonId uniqueidentifier = NEWID();
        SET @PatientUserId = NEWID();

        INSERT INTO Persons (PersonId, FirstName, LastName, NormalizedName, PersonalIdentifier, Address, BirthDate)
        VALUES (@PatientPersonId, 'Sofia', 'Marin', 'SOFIAMARIN', '9000101001001', '45 River Road', '1995-04-04');

        INSERT INTO Users (UserId, Username, Email, PasswordHash, SysRoleId, PersonId, ClinicId)
        VALUES (@PatientUserId, 'patient.sofia', 'patient.sofia@medio.local', @ReferencePasswordHash, @PatientRoleId, @PatientPersonId, @ClinicId);
    END;

    DECLARE @DoctorAlexUserId uniqueidentifier = (SELECT TOP (1) UserId FROM Users WHERE Email = 'doctor.alex@medio.local');
    DECLARE @DoctorMayaUserId uniqueidentifier = (SELECT TOP (1) UserId FROM Users WHERE Email = 'doctor.maya@medio.local');

    DECLARE @NowUtc datetime2 = SYSUTCDATETIME();
    DECLARE @Appt1 datetime2 = DATEADD(MINUTE, 10 * 60, CAST(DATEADD(DAY, 1, CONVERT(date, @NowUtc)) AS datetime2));
    DECLARE @Appt2 datetime2 = DATEADD(MINUTE, 14 * 60 + 30, CAST(DATEADD(DAY, 3, CONVERT(date, @NowUtc)) AS datetime2));
    DECLARE @Appt3 datetime2 = DATEADD(MINUTE, 9 * 60 + 45, CAST(DATEADD(DAY, 7, CONVERT(date, @NowUtc)) AS datetime2));
    DECLARE @ApptReject datetime2 = DATEADD(MINUTE, 16 * 60 + 20, CAST(DATEADD(DAY, 9, CONVERT(date, @NowUtc)) AS datetime2));
    DECLARE @ApptRejectProposed datetime2;

    WHILE EXISTS
    (
        SELECT 1
        FROM Appointments
        WHERE (DoctorId = @DoctorAlexUserId OR PatientId = @PatientUserId)
          AND AppointmentDateTime = @ApptReject
    )
    BEGIN
        SET @ApptReject = DATEADD(MINUTE, 30, @ApptReject);
    END;

    SET @ApptRejectProposed = DATEADD(DAY, 1, @ApptReject);

    WHILE EXISTS
    (
        SELECT 1
        FROM Appointments
        WHERE (DoctorId = @DoctorAlexUserId OR PatientId = @PatientUserId)
          AND AppointmentDateTime = @ApptRejectProposed
    )
    BEGIN
        SET @ApptRejectProposed = DATEADD(MINUTE, 30, @ApptRejectProposed);
    END;

    IF @PatientUserId IS NOT NULL AND @DoctorAlexUserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Appointments WHERE PatientId = @PatientUserId AND DoctorId = @DoctorAlexUserId AND AppointmentDateTime = @Appt1)
    BEGIN
        INSERT INTO Appointments (AppointmentId, PatientId, DoctorId, AppointmentDateTime, ClinicId, PostponeRequestStatus, ProposedDateTime, PostponeReason, PostponeRequestedAtUtc)
        VALUES (NEWID(), @PatientUserId, @DoctorAlexUserId, @Appt1, @ClinicId, 'Pending', DATEADD(DAY, 1, @Appt1), 'Work travel overlap. Requesting next day at same hour.', @NowUtc);
    END;

    IF @PatientUserId IS NOT NULL AND @DoctorMayaUserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Appointments WHERE PatientId = @PatientUserId AND DoctorId = @DoctorMayaUserId AND AppointmentDateTime = @Appt2)
    BEGIN
        INSERT INTO Appointments (AppointmentId, PatientId, DoctorId, AppointmentDateTime, ClinicId, PostponeRequestStatus, ProposedDateTime, PostponeReason, PostponeRequestedAtUtc)
        VALUES (NEWID(), @PatientUserId, @DoctorMayaUserId, @Appt2, @ClinicId, 'Approved', DATEADD(DAY, 1, @Appt2), 'Family event conflict.', DATEADD(DAY, -1, @NowUtc));
    END;

    IF @PatientUserId IS NOT NULL AND @DoctorAlexUserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Appointments WHERE PatientId = @PatientUserId AND DoctorId = @DoctorAlexUserId AND AppointmentDateTime = @Appt3)
    BEGIN
        INSERT INTO Appointments (AppointmentId, PatientId, DoctorId, AppointmentDateTime, ClinicId, PostponeRequestStatus, ProposedDateTime, PostponeReason, PostponeRequestedAtUtc)
        VALUES (NEWID(), @PatientUserId, @DoctorAlexUserId, @Appt3, @ClinicId, 'None', NULL, NULL, NULL);
    END;

    IF @PatientUserId IS NOT NULL AND @DoctorAlexUserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Appointments WHERE PatientId = @PatientUserId AND DoctorId = @DoctorAlexUserId AND AppointmentDateTime = @ApptReject)
    BEGIN
        INSERT INTO Appointments (AppointmentId, PatientId, DoctorId, AppointmentDateTime, ClinicId, PostponeRequestStatus, ProposedDateTime, PostponeReason, PostponeRequestedAtUtc)
        VALUES (NEWID(), @PatientUserId, @DoctorAlexUserId, @ApptReject, @ClinicId, 'Pending', @ApptRejectProposed, 'Please reject this seeded request for testing.', @NowUtc);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

SELECT 'Seed completed' AS ResultMessage;
SELECT Name, COUNT(*) AS UsersPerRole
FROM Users u
JOIN SysRoles r ON r.SysRoleId = u.SysRoleId
GROUP BY Name
ORDER BY Name;
