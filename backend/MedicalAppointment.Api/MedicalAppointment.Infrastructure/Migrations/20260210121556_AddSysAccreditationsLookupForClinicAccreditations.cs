using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSysAccreditationsLookupForClinicAccreditations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SysAccreditationId",
                table: "ClinicAccreditations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SysAccreditations",
                columns: table => new
                {
                    SysAccreditationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysAccreditations", x => x.SysAccreditationId);
                });

            migrationBuilder.Sql(
                @"
SET IDENTITY_INSERT [SysAccreditations] ON;

IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 1)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (1, 'Joint Commission Ambulatory Care', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 2)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (2, 'NCQA Patient-Centered Medical Home', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 3)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (3, 'AAAHC Ambulatory Health Care', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 4)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (4, 'URAC Telehealth Accreditation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 5)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (5, 'CAP Laboratory Accreditation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 6)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (6, 'CARF Health Program Accreditation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 7)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (7, 'ACR Imaging Accreditation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 8)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (8, 'COLA Laboratory Accreditation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 9)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (9, 'ACHC Ambulatory Care Accreditation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysAccreditations] WHERE [SysAccreditationId] = 10)
    INSERT INTO [SysAccreditations] ([SysAccreditationId], [Name], [IsActive]) VALUES (10, 'ISO 15189 Medical Laboratory', 1);

SET IDENTITY_INSERT [SysAccreditations] OFF;

INSERT INTO [SysAccreditations] ([Name], [IsActive])
SELECT DISTINCT source.[AccreditationName], 1
FROM
(
    SELECT LTRIM(RTRIM(NULLIF([Name], ''))) AS [AccreditationName]
    FROM [ClinicAccreditations]
) AS source
WHERE source.[AccreditationName] IS NOT NULL
  AND source.[AccreditationName] <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM [SysAccreditations] existing
      WHERE UPPER(LTRIM(RTRIM(existing.[Name]))) = UPPER(source.[AccreditationName])
  );

DECLARE @CurrentMaxSysAccreditationId int = (SELECT ISNULL(MAX([SysAccreditationId]), 0) FROM [SysAccreditations]);
DECLARE @ReseedSql nvarchar(200) =
    N'DBCC CHECKIDENT (''[SysAccreditations]'', RESEED, ' + CAST(@CurrentMaxSysAccreditationId AS nvarchar(20)) + N');';
EXEC (@ReseedSql);

UPDATE clinicAccreditation
SET [SysAccreditationId] = sysAccreditation.[SysAccreditationId]
FROM [ClinicAccreditations] clinicAccreditation
INNER JOIN [SysAccreditations] sysAccreditation
    ON UPPER(LTRIM(RTRIM(sysAccreditation.[Name]))) =
       UPPER(LTRIM(RTRIM(COALESCE(NULLIF(clinicAccreditation.[Name], ''), ''))))
WHERE clinicAccreditation.[SysAccreditationId] IS NULL;

UPDATE [ClinicAccreditations]
SET [SysAccreditationId] = 1
WHERE [SysAccreditationId] IS NULL;

;WITH groupedAccreditations AS
(
    SELECT
        [ClinicId],
        [SysAccreditationId],
        MIN([ClinicAccreditationId]) AS [KeepClinicAccreditationId],
        MIN([EffectiveOn]) AS [MinEffectiveOn],
        MAX([ExpiresOn]) AS [MaxExpiresOn],
        MAX(CASE WHEN [IsActive] = 1 THEN 1 ELSE 0 END) AS [ActiveFlag]
    FROM [ClinicAccreditations]
    GROUP BY [ClinicId], [SysAccreditationId]
)
UPDATE keeper
SET
    [EffectiveOn] = groupedAccreditations.[MinEffectiveOn],
    [ExpiresOn] = groupedAccreditations.[MaxExpiresOn],
    [IsActive] = CAST(groupedAccreditations.[ActiveFlag] AS bit)
FROM [ClinicAccreditations] keeper
INNER JOIN groupedAccreditations
    ON keeper.[ClinicAccreditationId] = groupedAccreditations.[KeepClinicAccreditationId];

;WITH duplicateRows AS
(
    SELECT
        [ClinicAccreditationId],
        ROW_NUMBER() OVER (PARTITION BY [ClinicId], [SysAccreditationId] ORDER BY [ClinicAccreditationId]) AS [RowNumber]
    FROM [ClinicAccreditations]
)
DELETE FROM [ClinicAccreditations]
WHERE [ClinicAccreditationId] IN
(
    SELECT [ClinicAccreditationId]
    FROM duplicateRows
    WHERE [RowNumber] > 1
);
");

            migrationBuilder.DropIndex(
                name: "IX_ClinicAccreditations_ClinicId",
                table: "ClinicAccreditations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ClinicAccreditations");

            migrationBuilder.AlterColumn<int>(
                name: "SysAccreditationId",
                table: "ClinicAccreditations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAccreditations_ClinicId_SysAccreditationId",
                table: "ClinicAccreditations",
                columns: new[] { "ClinicId", "SysAccreditationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAccreditations_SysAccreditationId",
                table: "ClinicAccreditations",
                column: "SysAccreditationId");

            migrationBuilder.CreateIndex(
                name: "IX_SysAccreditations_Name",
                table: "SysAccreditations",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicAccreditations_SysAccreditations_SysAccreditationId",
                table: "ClinicAccreditations",
                column: "SysAccreditationId",
                principalTable: "SysAccreditations",
                principalColumn: "SysAccreditationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicAccreditations_SysAccreditations_SysAccreditationId",
                table: "ClinicAccreditations");

            migrationBuilder.DropIndex(
                name: "IX_ClinicAccreditations_ClinicId_SysAccreditationId",
                table: "ClinicAccreditations");

            migrationBuilder.DropIndex(
                name: "IX_ClinicAccreditations_SysAccreditationId",
                table: "ClinicAccreditations");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ClinicAccreditations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                @"
UPDATE clinicAccreditation
SET [Name] = COALESCE(sysAccreditation.[Name], 'Unknown Accreditation')
FROM [ClinicAccreditations] clinicAccreditation
LEFT JOIN [SysAccreditations] sysAccreditation
    ON clinicAccreditation.[SysAccreditationId] = sysAccreditation.[SysAccreditationId];
");

            migrationBuilder.DropColumn(
                name: "SysAccreditationId",
                table: "ClinicAccreditations");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAccreditations_ClinicId",
                table: "ClinicAccreditations",
                column: "ClinicId");

            migrationBuilder.DropTable(
                name: "SysAccreditations");
        }
    }
}
