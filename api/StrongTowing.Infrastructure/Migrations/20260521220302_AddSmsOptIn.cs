using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SmsOptIn",
                table: "PendingDriverSignups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContactSmsOptIn",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsOptIn",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsOptInUpdatedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsOptIn",
                table: "PendingDriverSignups");

            migrationBuilder.DropColumn(
                name: "ContactSmsOptIn",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SmsOptIn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SmsOptInUpdatedAtUtc",
                table: "AspNetUsers");
        }
    }
}
