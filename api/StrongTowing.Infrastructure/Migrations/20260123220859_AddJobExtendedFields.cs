using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriveType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseState",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingNotes",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallType",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyOverride",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhoneNumber",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationAddress",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Drivable",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveType",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ETA",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HaveKeys",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeBillingNotesOnReceipt",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceChargesJson",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyLocation",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseState",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Odometer",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledTime",
                table: "Jobs",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckId",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriveType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LicenseState",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Account",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "BillingNotes",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CallType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompanyOverride",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ContactPhoneNumber",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DestinationAddress",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Drivable",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DriveType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ETA",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "HaveKeys",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IncludeBillingNotesOnReceipt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "InvoiceChargesJson",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "KeyLocation",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LicenseState",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Odometer",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PickupLocation",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TruckId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Jobs");
        }
    }
}
