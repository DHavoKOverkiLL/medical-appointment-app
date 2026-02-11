using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentReminderDispatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentReminderDispatches",
                columns: table => new
                {
                    AppointmentReminderDispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentReminderDispatches", x => x.AppointmentReminderDispatchId);
                    table.ForeignKey(
                        name: "FK_AppointmentReminderDispatches_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentReminderDispatches_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminderDispatches_AppointmentId",
                table: "AppointmentReminderDispatches",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminderDispatches_AppointmentId_RecipientUserId_ReminderType",
                table: "AppointmentReminderDispatches",
                columns: new[] { "AppointmentId", "RecipientUserId", "ReminderType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminderDispatches_RecipientUserId",
                table: "AppointmentReminderDispatches",
                column: "RecipientUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentReminderDispatches");
        }
    }
}
