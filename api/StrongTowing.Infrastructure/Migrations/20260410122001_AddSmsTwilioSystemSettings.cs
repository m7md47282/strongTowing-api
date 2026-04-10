using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsTwilioSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SmsClientDriverAssigned",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientFraudUnderReview",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientJobCancelled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientJobCompleted",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientJobCreated",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientPaymentFailed",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientPaymentLinkCreated",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientPaymentSucceeded",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientStatusLoaded",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientStatusOnRoute",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsClientStatusOnScene",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsDriverJobAssigned",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsDriverJobCompleted",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsDriverPayrollPaid",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SmsTwilioAccountSid",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsTwilioAuthToken",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsTwilioFromNumber",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsTwilioMessagingServiceSid",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsClientDriverAssigned",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientFraudUnderReview",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientJobCancelled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientJobCompleted",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientJobCreated",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientPaymentFailed",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientPaymentLinkCreated",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientPaymentSucceeded",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientStatusLoaded",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientStatusOnRoute",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsClientStatusOnScene",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsDriverJobAssigned",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsDriverJobCompleted",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsDriverPayrollPaid",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsTwilioAccountSid",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsTwilioAuthToken",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsTwilioFromNumber",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmsTwilioMessagingServiceSid",
                table: "SystemSettings");
        }
    }
}
