using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StrongTowing.Infrastructure.Data;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260401090000_AddPricingProfilesAndPolicies")]
    public partial class AddPricingProfilesAndPolicies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HookupFee",
                table: "InsuranceAccounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxExemptByDefault",
                table: "InsuranceAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RateAB",
                table: "InsuranceAccounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RateBC",
                table: "InsuranceAccounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RateCA",
                table: "InsuranceAccounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargePercent",
                table: "InsuranceAccounts",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercent",
                table: "InsuranceAccounts",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AllowManualTotalOverride",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPricingHookupFee",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 75m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPricingServiceChargePercent",
                table: "SystemSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPricingTaxPercent",
                table: "SystemSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountPercent",
                table: "SystemSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 100m);

            migrationBuilder.AddColumn<bool>(
                name: "ManualOverrideRequiresReason",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricingMismatchTolerance",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "PricingRoundingMode",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "AwayFromZero");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HookupFee",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "IsTaxExemptByDefault",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "RateAB",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "RateBC",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "RateCA",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "ServiceChargePercent",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "TaxPercent",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "AllowManualTotalOverride",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultPricingHookupFee",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultPricingServiceChargePercent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultPricingTaxPercent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MaxDiscountPercent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ManualOverrideRequiresReason",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PricingMismatchTolerance",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PricingRoundingMode",
                table: "SystemSettings");
        }
    }
}
