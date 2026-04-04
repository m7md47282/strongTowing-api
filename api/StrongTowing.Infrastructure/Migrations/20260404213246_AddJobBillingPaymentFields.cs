using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobBillingPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingPaymentMode",
                table: "Jobs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<decimal>(
                name: "ClientCoveredAmount",
                table: "Jobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClientPortionPaid",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DriverCashCollectedAmount",
                table: "Jobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceCoveredAmount",
                table: "Jobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InsurancePortionBilled",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PayrollDeductionAmount",
                table: "Jobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PayrollDeductionRecorded",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingPaymentMode",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ClientCoveredAmount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ClientPortionPaid",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DriverCashCollectedAmount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "InsuranceCoveredAmount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "InsurancePortionBilled",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PayrollDeductionAmount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PayrollDeductionRecorded",
                table: "Jobs");
        }
    }
}
