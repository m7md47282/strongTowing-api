using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFraudPreauthCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CancelFeeAfterArrivalPercent",
                table: "SystemSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CancelFeeAfterDispatchPercent",
                table: "SystemSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CancelFeeBeforeDispatchPercent",
                table: "SystemSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DuplicateRequestWindowMinutes",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FraudReviewScoreThreshold",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PreAuthorizationEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PreAuthorizationMaxAmount",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PreAuthorizationMinAmount",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationExpiresAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AuthorizedAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationFeeAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationFeePolicySnapshot",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptureStatus",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CapturedAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FraudReasons",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FraudReviewedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FraudReviewedBy",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FraudScore",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FraudStatus",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCancellationFeePayment",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreAuthorization",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationFeeAmount",
                table: "Jobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatus_FraudStatus",
                table: "Payments",
                columns: new[] { "PaymentStatus", "FraudStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatus_FraudStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancelFeeAfterArrivalPercent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CancelFeeAfterDispatchPercent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CancelFeeBeforeDispatchPercent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DuplicateRequestWindowMinutes",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "FraudReviewScoreThreshold",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PreAuthorizationEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PreAuthorizationMaxAmount",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PreAuthorizationMinAmount",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AuthorizationExpiresAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AuthorizedAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancellationFeeAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancellationFeePolicySnapshot",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CaptureStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CapturedAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CapturedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FraudReasons",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FraudReviewedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FraudReviewedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FraudScore",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FraudStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsCancellationFeePayment",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsPreAuthorization",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancellationFeeAmount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Jobs");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
