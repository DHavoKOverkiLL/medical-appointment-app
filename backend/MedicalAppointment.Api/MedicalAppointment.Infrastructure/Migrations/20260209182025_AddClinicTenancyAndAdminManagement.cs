using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicTenancyAndAdminManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultClinicId = new Guid("3f3d1a5c-f8f1-4c29-a2b2-6a1f5adf3fd3");

            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.ClinicId);
                });

            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [Clinics] WHERE [ClinicId] = '{defaultClinicId}')
                BEGIN
                    INSERT INTO [Clinics] ([ClinicId], [Name], [Code], [IsActive])
                    VALUES ('{defaultClinicId}', 'Default Clinic', 'DEFAULT', 1);
                END
            """);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultClinicId);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultClinicId);

            migrationBuilder.Sql($"""
                UPDATE [Users]
                SET [ClinicId] = '{defaultClinicId}'
                WHERE [ClinicId] = '00000000-0000-0000-0000-000000000000';
            """);

            migrationBuilder.Sql($"""
                UPDATE A
                SET A.[ClinicId] = U.[ClinicId]
                FROM [Appointments] AS A
                INNER JOIN [Users] AS U ON A.[PatientId] = U.[UserId];

                UPDATE [Appointments]
                SET [ClinicId] = '{defaultClinicId}'
                WHERE [ClinicId] = '00000000-0000-0000-0000-000000000000';
            """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClinicId",
                table: "Users",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId",
                table: "Appointments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Code",
                table: "Clinics",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Name",
                table: "Clinics",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "ClinicId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Clinics_ClinicId",
                table: "Users",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "ClinicId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Clinics_ClinicId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Users_ClinicId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Appointments");
        }
    }
}
