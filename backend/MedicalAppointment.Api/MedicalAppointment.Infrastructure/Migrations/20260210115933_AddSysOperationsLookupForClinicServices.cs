using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSysOperationsLookupForClinicServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SysOperationId",
                table: "ClinicServices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SysOperations",
                columns: table => new
                {
                    SysOperationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysOperations", x => x.SysOperationId);
                });

            migrationBuilder.Sql(
                @"
SET IDENTITY_INSERT [SysOperations] ON;

IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 1)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (1, 'Annual Wellness Visit', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 2)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (2, 'Primary Care Follow-up', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 3)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (3, 'Chronic Care Management', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 4)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (4, 'Preventive Screening', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 5)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (5, 'Vaccination', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 6)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (6, 'Urgent Care Visit', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 7)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (7, 'Telehealth Consultation', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 8)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (8, 'Lab Testing', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 9)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (9, 'Minor Procedure', 1);
IF NOT EXISTS (SELECT 1 FROM [SysOperations] WHERE [SysOperationId] = 10)
    INSERT INTO [SysOperations] ([SysOperationId], [Name], [IsActive]) VALUES (10, 'Behavioral Health Consultation', 1);

SET IDENTITY_INSERT [SysOperations] OFF;

INSERT INTO [SysOperations] ([Name], [IsActive])
SELECT DISTINCT source.[OperationName], 1
FROM
(
    SELECT LTRIM(RTRIM(COALESCE(NULLIF([Name], ''), NULLIF([ServiceCode], '')))) AS [OperationName]
    FROM [ClinicServices]
) AS source
WHERE source.[OperationName] IS NOT NULL
  AND source.[OperationName] <> ''
  AND NOT EXISTS
  (
      SELECT 1
      FROM [SysOperations] existing
      WHERE UPPER(LTRIM(RTRIM(existing.[Name]))) = UPPER(source.[OperationName])
  );

DECLARE @CurrentMaxSysOperationId int = (SELECT ISNULL(MAX([SysOperationId]), 0) FROM [SysOperations]);
DECLARE @ReseedSql nvarchar(200) =
    N'DBCC CHECKIDENT (''[SysOperations]'', RESEED, ' + CAST(@CurrentMaxSysOperationId AS nvarchar(20)) + N');';
EXEC (@ReseedSql);

UPDATE clinicService
SET [SysOperationId] = operationLookup.[SysOperationId]
FROM [ClinicServices] clinicService
INNER JOIN [SysOperations] operationLookup
    ON UPPER(LTRIM(RTRIM(operationLookup.[Name]))) =
       UPPER(LTRIM(RTRIM(COALESCE(NULLIF(clinicService.[Name], ''), NULLIF(clinicService.[ServiceCode], '')))))
WHERE clinicService.[SysOperationId] IS NULL;

UPDATE [ClinicServices]
SET [SysOperationId] = 1
WHERE [SysOperationId] IS NULL;

;WITH groupedServices AS
(
    SELECT
        [ClinicId],
        [SysOperationId],
        MIN([ClinicServiceId]) AS [KeepClinicServiceId],
        MAX(CASE WHEN [IsTelehealthAvailable] = 1 THEN 1 ELSE 0 END) AS [TelehealthFlag]
    FROM [ClinicServices]
    GROUP BY [ClinicId], [SysOperationId]
)
UPDATE keeper
SET [IsTelehealthAvailable] = CAST(groupedServices.[TelehealthFlag] AS bit)
FROM [ClinicServices] keeper
INNER JOIN groupedServices
    ON keeper.[ClinicServiceId] = groupedServices.[KeepClinicServiceId];

;WITH duplicateRows AS
(
    SELECT
        [ClinicServiceId],
        ROW_NUMBER() OVER (PARTITION BY [ClinicId], [SysOperationId] ORDER BY [ClinicServiceId]) AS [RowNumber]
    FROM [ClinicServices]
)
DELETE FROM [ClinicServices]
WHERE [ClinicServiceId] IN
(
    SELECT [ClinicServiceId]
    FROM duplicateRows
    WHERE [RowNumber] > 1
);
");

            migrationBuilder.DropIndex(
                name: "IX_ClinicServices_ClinicId_ServiceCode",
                table: "ClinicServices");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ClinicServices");

            migrationBuilder.DropColumn(
                name: "ServiceCode",
                table: "ClinicServices");

            migrationBuilder.AlterColumn<int>(
                name: "SysOperationId",
                table: "ClinicServices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicServices_ClinicId_SysOperationId",
                table: "ClinicServices",
                columns: new[] { "ClinicId", "SysOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicServices_SysOperationId",
                table: "ClinicServices",
                column: "SysOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SysOperations_Name",
                table: "SysOperations",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicServices_SysOperations_SysOperationId",
                table: "ClinicServices",
                column: "SysOperationId",
                principalTable: "SysOperations",
                principalColumn: "SysOperationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicServices_SysOperations_SysOperationId",
                table: "ClinicServices");

            migrationBuilder.DropIndex(
                name: "IX_ClinicServices_ClinicId_SysOperationId",
                table: "ClinicServices");

            migrationBuilder.DropIndex(
                name: "IX_ClinicServices_SysOperationId",
                table: "ClinicServices");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ClinicServices",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceCode",
                table: "ClinicServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                @"
UPDATE clinicService
SET
    [Name] = COALESCE(operationLookup.[Name], 'Unknown Service'),
    [ServiceCode] = LEFT(
        UPPER(
            REPLACE(
                COALESCE(operationLookup.[Name], CONCAT('OP_', CAST(clinicService.[SysOperationId] AS nvarchar(20)))),
                ' ',
                '_'
            )
        ),
        64
    )
FROM [ClinicServices] clinicService
LEFT JOIN [SysOperations] operationLookup
    ON clinicService.[SysOperationId] = operationLookup.[SysOperationId];
");

            migrationBuilder.DropColumn(
                name: "SysOperationId",
                table: "ClinicServices");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicServices_ClinicId_ServiceCode",
                table: "ClinicServices",
                columns: new[] { "ClinicId", "ServiceCode" },
                unique: true);

            migrationBuilder.DropTable(
                name: "SysOperations");
        }
    }
}
