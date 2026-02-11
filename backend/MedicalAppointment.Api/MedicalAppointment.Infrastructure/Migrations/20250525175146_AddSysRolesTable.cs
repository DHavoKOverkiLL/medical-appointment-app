using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSysRolesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "Users",
                newName: "SysRoleId");

            migrationBuilder.CreateTable(
                name: "SysRoles",
                columns: table => new
                {
                    SysRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoles", x => x.SysRoleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SysRoleId",
                table: "Users",
                column: "SysRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SysRoles_SysRoleId",
                table: "Users",
                column: "SysRoleId",
                principalTable: "SysRoles",
                principalColumn: "SysRoleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_SysRoles_SysRoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "SysRoles");

            migrationBuilder.DropIndex(
                name: "IX_Users_SysRoleId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "SysRoleId",
                table: "Users",
                newName: "RoleId");
        }
    }
}
