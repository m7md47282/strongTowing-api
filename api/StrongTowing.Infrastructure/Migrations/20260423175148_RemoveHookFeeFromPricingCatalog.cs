using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHookFeeFromPricingCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ServicePricingProfiles
                SET BasePrice = BasePrice + HookFeeAmount
                WHERE HookFeeEnabled = 1;

                UPDATE InsuranceAccountServiceRates
                SET BasePrice = BasePrice + HookFeeAmount
                WHERE HookFeeEnabled = 1;
                """);

            migrationBuilder.DropColumn(
                name: "HookFeeAmount",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "HookFeeEnabled",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "HookFeeAmount",
                table: "InsuranceAccountServiceRates");

            migrationBuilder.DropColumn(
                name: "HookFeeEnabled",
                table: "InsuranceAccountServiceRates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HookFeeAmount",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HookFeeEnabled",
                table: "ServicePricingProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HookFeeAmount",
                table: "InsuranceAccountServiceRates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HookFeeEnabled",
                table: "InsuranceAccountServiceRates",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
