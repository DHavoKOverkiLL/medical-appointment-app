using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorAvailabilityManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityBreaks",
                columns: table => new
                {
                    DoctorAvailabilityBreakId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityBreaks", x => x.DoctorAvailabilityBreakId);
                    table.CheckConstraint("CK_DoctorAvailabilityBreaks_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.CheckConstraint("CK_DoctorAvailabilityBreaks_TimeRange", "[StartTime] < [EndTime]");
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityBreaks_Users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityOverrides",
                columns: table => new
                {
                    DoctorAvailabilityOverrideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityOverrides", x => x.DoctorAvailabilityOverrideId);
                    table.CheckConstraint("CK_DoctorAvailabilityOverrides_TimeRange", "[StartTime] IS NULL OR [EndTime] IS NULL OR [StartTime] < [EndTime]");
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityOverrides_Users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityWindows",
                columns: table => new
                {
                    DoctorAvailabilityWindowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityWindows", x => x.DoctorAvailabilityWindowId);
                    table.CheckConstraint("CK_DoctorAvailabilityWindows_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    table.CheckConstraint("CK_DoctorAvailabilityWindows_TimeRange", "[StartTime] < [EndTime]");
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityWindows_Users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityBreaks_DoctorId",
                table: "DoctorAvailabilityBreaks",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityBreaks_DoctorId_DayOfWeek_StartTime_EndTime",
                table: "DoctorAvailabilityBreaks",
                columns: new[] { "DoctorId", "DayOfWeek", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityOverrides_DoctorId",
                table: "DoctorAvailabilityOverrides",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityOverrides_DoctorId_Date_IsAvailable_StartTime_EndTime",
                table: "DoctorAvailabilityOverrides",
                columns: new[] { "DoctorId", "Date", "IsAvailable", "StartTime", "EndTime" },
                unique: true,
                filter: "[StartTime] IS NOT NULL AND [EndTime] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityWindows_DoctorId",
                table: "DoctorAvailabilityWindows",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityWindows_DoctorId_DayOfWeek_StartTime_EndTime",
                table: "DoctorAvailabilityWindows",
                columns: new[] { "DoctorId", "DayOfWeek", "StartTime", "EndTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorAvailabilityBreaks");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityOverrides");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityWindows");
        }
    }
}
