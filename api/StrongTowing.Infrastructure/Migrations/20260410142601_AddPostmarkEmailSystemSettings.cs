using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostmarkEmailSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailClientDriverAssigned",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientFraudUnderReview",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientJobCancelled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientJobCompleted",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientJobCreated",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientPaymentFailed",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientPaymentLinkCreated",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientPaymentSucceeded",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientStatusLoaded",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientStatusOnRoute",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailClientStatusOnScene",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailDriverJobAssigned",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailDriverJobCompleted",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailDriverPayrollPaid",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PostmarkDefaultFromEmail",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostmarkMessageStream",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostmarkServerToken",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailClientDriverAssigned",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientFraudUnderReview",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientJobCancelled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientJobCompleted",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientJobCreated",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientPaymentFailed",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientPaymentLinkCreated",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientPaymentSucceeded",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientStatusLoaded",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientStatusOnRoute",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailClientStatusOnScene",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailDriverJobAssigned",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailDriverJobCompleted",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailDriverPayrollPaid",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PostmarkDefaultFromEmail",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PostmarkMessageStream",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PostmarkServerToken",
                table: "SystemSettings");
        }
    }
}
