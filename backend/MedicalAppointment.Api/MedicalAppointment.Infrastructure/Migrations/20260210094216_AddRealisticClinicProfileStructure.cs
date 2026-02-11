using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRealisticClinicProfileStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Clinics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Clinics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AvgNewPatientWaitDays",
                table: "Clinics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingMethods",
                table: "Clinics",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Clinics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CliaNumber",
                table: "Clinics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClinicType",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Clinics",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "US");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Clinics",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Ein",
                table: "Clinics",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "Clinics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FoundedOn",
                table: "Clinics",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HipaaNoticeVersion",
                table: "Clinics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastSecurityRiskAssessmentOn",
                table: "Clinics",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Clinics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MainEmail",
                table: "Clinics",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MainPhone",
                table: "Clinics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NpiOrganization",
                table: "Clinics",
                type: "nvarchar(20)",
                maxLength: 20,
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
                name: "PatientPortalUrl",
                table: "Clinics",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Clinics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SameDayAvailable",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "EHR");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Clinics",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StateLicenseFacility",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxonomyCode",
                table: "Clinics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "Clinics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "America/Chicago");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Clinics",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Clinics",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ClinicAccreditations",
                columns: table => new
                {
                    ClinicAccreditationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EffectiveOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAccreditations", x => x.ClinicAccreditationId);
                    table.ForeignKey(
                        name: "FK_ClinicAccreditations_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInsurancePlans",
                columns: table => new
                {
                    ClinicInsurancePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsInNetwork = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInsurancePlans", x => x.ClinicInsurancePlanId);
                    table.ForeignKey(
                        name: "FK_ClinicInsurancePlans_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicOperatingHours",
                columns: table => new
                {
                    ClinicOperatingHourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicOperatingHours", x => x.ClinicOperatingHourId);
                    table.CheckConstraint("CK_ClinicOperatingHours_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.ForeignKey(
                        name: "FK_ClinicOperatingHours_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicServices",
                columns: table => new
                {
                    ClinicServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsTelehealthAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicServices", x => x.ClinicServiceId);
                    table.ForeignKey(
                        name: "FK_ClinicServices_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAccreditations_ClinicId",
                table: "ClinicAccreditations",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInsurancePlans_ClinicId_PayerName_PlanName",
                table: "ClinicInsurancePlans",
                columns: new[] { "ClinicId", "PayerName", "PlanName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOperatingHours_ClinicId_DayOfWeek",
                table: "ClinicOperatingHours",
                columns: new[] { "ClinicId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicServices_ClinicId_ServiceCode",
                table: "ClinicServices",
                columns: new[] { "ClinicId", "ServiceCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicAccreditations");

            migrationBuilder.DropTable(
                name: "ClinicInsurancePlans");

            migrationBuilder.DropTable(
                name: "ClinicOperatingHours");

            migrationBuilder.DropTable(
                name: "ClinicServices");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "AvgNewPatientWaitDays",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "BookingMethods",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "CliaNumber",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "ClinicType",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Ein",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "FoundedOn",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "HipaaNoticeVersion",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "LastSecurityRiskAssessmentOn",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "MainEmail",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "MainPhone",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "NpiOrganization",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "PatientPortalUrl",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "SameDayAvailable",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "StateLicenseFacility",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "TaxonomyCode",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Clinics");
        }
    }
}
