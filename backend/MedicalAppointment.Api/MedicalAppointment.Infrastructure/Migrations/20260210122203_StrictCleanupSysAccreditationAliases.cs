using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrictCleanupSysAccreditationAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
DECLARE @CanonicalJointCommissionId int =
(
    SELECT TOP (1) [SysAccreditationId]
    FROM [SysAccreditations]
    WHERE [Name] = 'Joint Commission Ambulatory Care'
);

DECLARE @CanonicalNcqaId int =
(
    SELECT TOP (1) [SysAccreditationId]
    FROM [SysAccreditations]
    WHERE [Name] = 'NCQA Patient-Centered Medical Home'
);

DECLARE @LegacyJointCommissionId int =
(
    SELECT TOP (1) [SysAccreditationId]
    FROM [SysAccreditations]
    WHERE [Name] = 'Joint Commission Ambulatory'
);

DECLARE @LegacyNcqaId int =
(
    SELECT TOP (1) [SysAccreditationId]
    FROM [SysAccreditations]
    WHERE [Name] = 'NCQA-PCMH'
);

DECLARE @Mappings TABLE
(
    [LegacyId] int NOT NULL,
    [CanonicalId] int NOT NULL
);

IF @LegacyJointCommissionId IS NOT NULL
   AND @CanonicalJointCommissionId IS NOT NULL
   AND @LegacyJointCommissionId <> @CanonicalJointCommissionId
BEGIN
    INSERT INTO @Mappings ([LegacyId], [CanonicalId])
    VALUES (@LegacyJointCommissionId, @CanonicalJointCommissionId);
END;

IF @LegacyNcqaId IS NOT NULL
   AND @CanonicalNcqaId IS NOT NULL
   AND @LegacyNcqaId <> @CanonicalNcqaId
BEGIN
    INSERT INTO @Mappings ([LegacyId], [CanonicalId])
    VALUES (@LegacyNcqaId, @CanonicalNcqaId);
END;

;WITH overlappingPairs AS
(
    SELECT
        legacy.[ClinicAccreditationId] AS [LegacyRowId],
        canonical.[ClinicAccreditationId] AS [CanonicalRowId]
    FROM @Mappings mapping
    INNER JOIN [ClinicAccreditations] legacy
        ON legacy.[SysAccreditationId] = mapping.[LegacyId]
    INNER JOIN [ClinicAccreditations] canonical
        ON canonical.[ClinicId] = legacy.[ClinicId]
       AND canonical.[SysAccreditationId] = mapping.[CanonicalId]
)
UPDATE canonical
SET
    [EffectiveOn] = CASE
        WHEN canonical.[EffectiveOn] IS NULL THEN legacy.[EffectiveOn]
        WHEN legacy.[EffectiveOn] IS NULL THEN canonical.[EffectiveOn]
        WHEN legacy.[EffectiveOn] < canonical.[EffectiveOn] THEN legacy.[EffectiveOn]
        ELSE canonical.[EffectiveOn]
    END,
    [ExpiresOn] = CASE
        WHEN canonical.[ExpiresOn] IS NULL THEN legacy.[ExpiresOn]
        WHEN legacy.[ExpiresOn] IS NULL THEN canonical.[ExpiresOn]
        WHEN legacy.[ExpiresOn] > canonical.[ExpiresOn] THEN legacy.[ExpiresOn]
        ELSE canonical.[ExpiresOn]
    END,
    [IsActive] = CASE
        WHEN canonical.[IsActive] = 1 OR legacy.[IsActive] = 1 THEN 1
        ELSE 0
    END
FROM overlappingPairs pair
INNER JOIN [ClinicAccreditations] canonical
    ON canonical.[ClinicAccreditationId] = pair.[CanonicalRowId]
INNER JOIN [ClinicAccreditations] legacy
    ON legacy.[ClinicAccreditationId] = pair.[LegacyRowId];

DELETE legacy
FROM [ClinicAccreditations] legacy
INNER JOIN @Mappings mapping
    ON legacy.[SysAccreditationId] = mapping.[LegacyId]
WHERE EXISTS
(
    SELECT 1
    FROM [ClinicAccreditations] canonical
    WHERE canonical.[ClinicId] = legacy.[ClinicId]
      AND canonical.[SysAccreditationId] = mapping.[CanonicalId]
);

UPDATE clinicAccreditation
SET [SysAccreditationId] = mapping.[CanonicalId]
FROM [ClinicAccreditations] clinicAccreditation
INNER JOIN @Mappings mapping
    ON clinicAccreditation.[SysAccreditationId] = mapping.[LegacyId];

;WITH ranked AS
(
    SELECT
        [ClinicAccreditationId],
        ROW_NUMBER() OVER
        (
            PARTITION BY [ClinicId], [SysAccreditationId]
            ORDER BY [ClinicAccreditationId]
        ) AS [RowNumber]
    FROM [ClinicAccreditations]
)
DELETE FROM [ClinicAccreditations]
WHERE [ClinicAccreditationId] IN
(
    SELECT [ClinicAccreditationId]
    FROM ranked
    WHERE [RowNumber] > 1
);

UPDATE legacy
SET [IsActive] = 0
FROM [SysAccreditations] legacy
INNER JOIN @Mappings mapping
    ON legacy.[SysAccreditationId] = mapping.[LegacyId];

DELETE legacy
FROM [SysAccreditations] legacy
INNER JOIN @Mappings mapping
    ON legacy.[SysAccreditationId] = mapping.[LegacyId]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [ClinicAccreditations] clinicAccreditation
    WHERE clinicAccreditation.[SysAccreditationId] = legacy.[SysAccreditationId]
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
