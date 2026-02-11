using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSysClinicLookupsForClinicProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SysClinicTypeId",
                table: "Clinics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SysOwnershipTypeId",
                table: "Clinics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SysSourceSystemId",
                table: "Clinics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SysClinicTypes",
                columns: table => new
                {
                    SysClinicTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysClinicTypes", x => x.SysClinicTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SysOwnershipTypes",
                columns: table => new
                {
                    SysOwnershipTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysOwnershipTypes", x => x.SysOwnershipTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SysSourceSystems",
                columns: table => new
                {
                    SysSourceSystemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysSourceSystems", x => x.SysSourceSystemId);
                });

            migrationBuilder.Sql(
                @"
SET IDENTITY_INSERT [SysClinicTypes] ON;
IF NOT EXISTS (SELECT 1 FROM [SysClinicTypes] WHERE [SysClinicTypeId] = 1)
    INSERT INTO [SysClinicTypes] ([SysClinicTypeId], [Name], [IsActive]) VALUES (1, 'Primary Care', 1);
IF NOT EXISTS (SELECT 1 FROM [SysClinicTypes] WHERE [SysClinicTypeId] = 2)
    INSERT INTO [SysClinicTypes] ([SysClinicTypeId], [Name], [IsActive]) VALUES (2, 'Specialty Care', 1);
IF NOT EXISTS (SELECT 1 FROM [SysClinicTypes] WHERE [SysClinicTypeId] = 3)
    INSERT INTO [SysClinicTypes] ([SysClinicTypeId], [Name], [IsActive]) VALUES (3, 'Urgent Care', 1);
IF NOT EXISTS (SELECT 1 FROM [SysClinicTypes] WHERE [SysClinicTypeId] = 4)
    INSERT INTO [SysClinicTypes] ([SysClinicTypeId], [Name], [IsActive]) VALUES (4, 'Multi-specialty Clinic', 1);
IF NOT EXISTS (SELECT 1 FROM [SysClinicTypes] WHERE [SysClinicTypeId] = 5)
    INSERT INTO [SysClinicTypes] ([SysClinicTypeId], [Name], [IsActive]) VALUES (5, 'Community Health Center', 1);
SET IDENTITY_INSERT [SysClinicTypes] OFF;

SET IDENTITY_INSERT [SysOwnershipTypes] ON;
IF NOT EXISTS (SELECT 1 FROM [SysOwnershipTypes] WHERE [SysOwnershipTypeId] = 1)
    INSERT INTO [SysOwnershipTypes] ([SysOwnershipTypeId], [Name], [IsActive]) VALUES (1, 'Physician Owned', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOwnershipTypes] WHERE [SysOwnershipTypeId] = 2)
    INSERT INTO [SysOwnershipTypes] ([SysOwnershipTypeId], [Name], [IsActive]) VALUES (2, 'Hospital Owned', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOwnershipTypes] WHERE [SysOwnershipTypeId] = 3)
    INSERT INTO [SysOwnershipTypes] ([SysOwnershipTypeId], [Name], [IsActive]) VALUES (3, 'Health System', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOwnershipTypes] WHERE [SysOwnershipTypeId] = 4)
    INSERT INTO [SysOwnershipTypes] ([SysOwnershipTypeId], [Name], [IsActive]) VALUES (4, 'Academic', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOwnershipTypes] WHERE [SysOwnershipTypeId] = 5)
    INSERT INTO [SysOwnershipTypes] ([SysOwnershipTypeId], [Name], [IsActive]) VALUES (5, 'Government', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOwnershipTypes] WHERE [SysOwnershipTypeId] = 6)
    INSERT INTO [SysOwnershipTypes] ([SysOwnershipTypeId], [Name], [IsActive]) VALUES (6, 'Nonprofit', 1);
SET IDENTITY_INSERT [SysOwnershipTypes] OFF;

SET IDENTITY_INSERT [SysSourceSystems] ON;
IF NOT EXISTS (SELECT 1 FROM [SysSourceSystems] WHERE [SysSourceSystemId] = 1)
    INSERT INTO [SysSourceSystems] ([SysSourceSystemId], [Name], [IsActive]) VALUES (1, 'EHR', 1);
IF NOT EXISTS (SELECT 1 FROM [SysSourceSystems] WHERE [SysSourceSystemId] = 2)
    INSERT INTO [SysSourceSystems] ([SysSourceSystemId], [Name], [IsActive]) VALUES (2, 'EMR', 1);
IF NOT EXISTS (SELECT 1 FROM [SysSourceSystems] WHERE [SysSourceSystemId] = 3)
    INSERT INTO [SysSourceSystems] ([SysSourceSystemId], [Name], [IsActive]) VALUES (3, 'Custom Platform', 1);
IF NOT EXISTS (SELECT 1 FROM [SysSourceSystems] WHERE [SysSourceSystemId] = 4)
    INSERT INTO [SysSourceSystems] ([SysSourceSystemId], [Name], [IsActive]) VALUES (4, 'Legacy PMS', 1);
SET IDENTITY_INSERT [SysSourceSystems] OFF;

INSERT INTO [SysClinicTypes] ([Name], [IsActive])
SELECT DISTINCT source.[ClinicTypeName], 1
FROM
(
    SELECT LTRIM(RTRIM(NULLIF([ClinicType], ''))) AS [ClinicTypeName]
    FROM [Clinics]
) AS source
WHERE source.[ClinicTypeName] IS NOT NULL
  AND source.[ClinicTypeName] <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM [SysClinicTypes] existing
      WHERE UPPER(LTRIM(RTRIM(existing.[Name]))) = UPPER(source.[ClinicTypeName])
  );

INSERT INTO [SysOwnershipTypes] ([Name], [IsActive])
SELECT DISTINCT source.[OwnershipTypeName], 1
FROM
(
    SELECT LTRIM(RTRIM(NULLIF([OwnershipType], ''))) AS [OwnershipTypeName]
    FROM [Clinics]
) AS source
WHERE source.[OwnershipTypeName] IS NOT NULL
  AND source.[OwnershipTypeName] <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM [SysOwnershipTypes] existing
      WHERE UPPER(LTRIM(RTRIM(existing.[Name]))) = UPPER(source.[OwnershipTypeName])
  );

INSERT INTO [SysSourceSystems] ([Name], [IsActive])
SELECT DISTINCT source.[SourceSystemName], 1
FROM
(
    SELECT LTRIM(RTRIM(NULLIF([SourceSystem], ''))) AS [SourceSystemName]
    FROM [Clinics]
) AS source
WHERE source.[SourceSystemName] IS NOT NULL
  AND source.[SourceSystemName] <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM [SysSourceSystems] existing
      WHERE UPPER(LTRIM(RTRIM(existing.[Name]))) = UPPER(source.[SourceSystemName])
  );

DECLARE @CurrentMaxSysClinicTypeId int = (SELECT ISNULL(MAX([SysClinicTypeId]), 0) FROM [SysClinicTypes]);
DECLARE @ReseedSysClinicTypesSql nvarchar(200) =
    N'DBCC CHECKIDENT (''[SysClinicTypes]'', RESEED, ' + CAST(@CurrentMaxSysClinicTypeId AS nvarchar(20)) + N');';
EXEC (@ReseedSysClinicTypesSql);

DECLARE @CurrentMaxSysOwnershipTypeId int = (SELECT ISNULL(MAX([SysOwnershipTypeId]), 0) FROM [SysOwnershipTypes]);
DECLARE @ReseedSysOwnershipTypesSql nvarchar(200) =
    N'DBCC CHECKIDENT (''[SysOwnershipTypes]'', RESEED, ' + CAST(@CurrentMaxSysOwnershipTypeId AS nvarchar(20)) + N');';
EXEC (@ReseedSysOwnershipTypesSql);

DECLARE @CurrentMaxSysSourceSystemId int = (SELECT ISNULL(MAX([SysSourceSystemId]), 0) FROM [SysSourceSystems]);
DECLARE @ReseedSysSourceSystemsSql nvarchar(200) =
    N'DBCC CHECKIDENT (''[SysSourceSystems]'', RESEED, ' + CAST(@CurrentMaxSysSourceSystemId AS nvarchar(20)) + N');';
EXEC (@ReseedSysSourceSystemsSql);

UPDATE clinic
SET [SysClinicTypeId] = CASE
    WHEN clinic.[ClinicType] IS NULL OR LTRIM(RTRIM(clinic.[ClinicType])) = '' THEN 1
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[ClinicType])), '-', '_'), ' ', '_')) = 'PRIMARY_CARE' THEN 1
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[ClinicType])), '-', '_'), ' ', '_')) = 'SPECIALTY_CARE' THEN 2
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[ClinicType])), '-', '_'), ' ', '_')) = 'URGENT_CARE' THEN 3
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[ClinicType])), '-', '_'), ' ', '_')) IN ('MULTI_SPECIALTY', 'MULTISPECIALTY', 'MULTI_SPECIALTY_CLINIC') THEN 4
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[ClinicType])), '-', '_'), ' ', '_')) IN ('COMMUNITY_HEALTH', 'COMMUNITY_HEALTH_CENTER') THEN 5
    ELSE lookup.[SysClinicTypeId]
END
FROM [Clinics] clinic
LEFT JOIN [SysClinicTypes] lookup
    ON UPPER(LTRIM(RTRIM(lookup.[Name]))) = UPPER(LTRIM(RTRIM(clinic.[ClinicType])))
WHERE clinic.[SysClinicTypeId] IS NULL;

UPDATE clinic
SET [SysOwnershipTypeId] = CASE
    WHEN clinic.[OwnershipType] IS NULL OR LTRIM(RTRIM(clinic.[OwnershipType])) = '' THEN 1
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[OwnershipType])), '-', '_'), ' ', '_')) = 'PHYSICIAN_OWNED' THEN 1
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[OwnershipType])), '-', '_'), ' ', '_')) = 'HOSPITAL_OWNED' THEN 2
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[OwnershipType])), '-', '_'), ' ', '_')) = 'HEALTH_SYSTEM' THEN 3
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[OwnershipType])), '-', '_'), ' ', '_')) = 'ACADEMIC' THEN 4
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[OwnershipType])), '-', '_'), ' ', '_')) = 'GOVERNMENT' THEN 5
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[OwnershipType])), '-', '_'), ' ', '_')) = 'NONPROFIT' THEN 6
    ELSE lookup.[SysOwnershipTypeId]
END
FROM [Clinics] clinic
LEFT JOIN [SysOwnershipTypes] lookup
    ON UPPER(LTRIM(RTRIM(lookup.[Name]))) = UPPER(LTRIM(RTRIM(clinic.[OwnershipType])))
WHERE clinic.[SysOwnershipTypeId] IS NULL;

UPDATE clinic
SET [SysSourceSystemId] = CASE
    WHEN clinic.[SourceSystem] IS NULL OR LTRIM(RTRIM(clinic.[SourceSystem])) = '' THEN 1
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[SourceSystem])), '-', '_'), ' ', '_')) = 'EHR' THEN 1
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[SourceSystem])), '-', '_'), ' ', '_')) = 'EMR' THEN 2
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[SourceSystem])), '-', '_'), ' ', '_')) = 'CUSTOM_PLATFORM' THEN 3
    WHEN UPPER(REPLACE(REPLACE(LTRIM(RTRIM(clinic.[SourceSystem])), '-', '_'), ' ', '_')) IN ('LEGACY_PMS', 'PMS') THEN 4
    ELSE lookup.[SysSourceSystemId]
END
FROM [Clinics] clinic
LEFT JOIN [SysSourceSystems] lookup
    ON UPPER(LTRIM(RTRIM(lookup.[Name]))) = UPPER(LTRIM(RTRIM(clinic.[SourceSystem])))
WHERE clinic.[SysSourceSystemId] IS NULL;

UPDATE [Clinics] SET [SysClinicTypeId] = 1 WHERE [SysClinicTypeId] IS NULL;
UPDATE [Clinics] SET [SysOwnershipTypeId] = 1 WHERE [SysOwnershipTypeId] IS NULL;
UPDATE [Clinics] SET [SysSourceSystemId] = 1 WHERE [SysSourceSystemId] IS NULL;
");

            migrationBuilder.AlterColumn<int>(
                name: "SysClinicTypeId",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SysOwnershipTypeId",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SysSourceSystemId",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_SysClinicTypeId",
                table: "Clinics",
                column: "SysClinicTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_SysOwnershipTypeId",
                table: "Clinics",
                column: "SysOwnershipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_SysSourceSystemId",
                table: "Clinics",
                column: "SysSourceSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SysClinicTypes_Name",
                table: "SysClinicTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysOwnershipTypes_Name",
                table: "SysOwnershipTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysSourceSystems_Name",
                table: "SysSourceSystems",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Clinics_SysClinicTypes_SysClinicTypeId",
                table: "Clinics",
                column: "SysClinicTypeId",
                principalTable: "SysClinicTypes",
                principalColumn: "SysClinicTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clinics_SysOwnershipTypes_SysOwnershipTypeId",
                table: "Clinics",
                column: "SysOwnershipTypeId",
                principalTable: "SysOwnershipTypes",
                principalColumn: "SysOwnershipTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clinics_SysSourceSystems_SysSourceSystemId",
                table: "Clinics",
                column: "SysSourceSystemId",
                principalTable: "SysSourceSystems",
                principalColumn: "SysSourceSystemId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "ClinicType",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "Clinics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClinicType",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipType",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "EHR");

            migrationBuilder.Sql(
                @"
UPDATE clinic
SET
    [ClinicType] = COALESCE(clinicType.[Name], ''),
    [OwnershipType] = COALESCE(ownershipType.[Name], ''),
    [SourceSystem] = COALESCE(sourceSystem.[Name], 'EHR')
FROM [Clinics] clinic
LEFT JOIN [SysClinicTypes] clinicType
    ON clinic.[SysClinicTypeId] = clinicType.[SysClinicTypeId]
LEFT JOIN [SysOwnershipTypes] ownershipType
    ON clinic.[SysOwnershipTypeId] = ownershipType.[SysOwnershipTypeId]
LEFT JOIN [SysSourceSystems] sourceSystem
    ON clinic.[SysSourceSystemId] = sourceSystem.[SysSourceSystemId];
");

            migrationBuilder.DropForeignKey(
                name: "FK_Clinics_SysClinicTypes_SysClinicTypeId",
                table: "Clinics");

            migrationBuilder.DropForeignKey(
                name: "FK_Clinics_SysOwnershipTypes_SysOwnershipTypeId",
                table: "Clinics");

            migrationBuilder.DropForeignKey(
                name: "FK_Clinics_SysSourceSystems_SysSourceSystemId",
                table: "Clinics");

            migrationBuilder.DropTable(
                name: "SysClinicTypes");

            migrationBuilder.DropTable(
                name: "SysOwnershipTypes");

            migrationBuilder.DropTable(
                name: "SysSourceSystems");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_SysClinicTypeId",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_SysOwnershipTypeId",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_SysSourceSystemId",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "SysClinicTypeId",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "SysOwnershipTypeId",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "SysSourceSystemId",
                table: "Clinics");
        }
    }
}
