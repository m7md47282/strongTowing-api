using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class YU96 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "HRs");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "Dispatchers");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "Dispatchers");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "HRs",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "Dispatchers",
                newName: "FullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "HRs",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Dispatchers",
                newName: "SecurityStamp");

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "HRs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "HRs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "HRs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "HRs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "HRs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "HRs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "HRs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "HRs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "HRs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "HRs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "Dispatchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Dispatchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Dispatchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "Dispatchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "Dispatchers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Dispatchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "Dispatchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Dispatchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "Dispatchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Dispatchers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
