using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePricingSegmentRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeadheadPricePerMile",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EnroutePricePerMile",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoadedPricePerMile",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeadheadPricePerMile",
                table: "InsuranceAccountServiceRates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EnroutePricePerMile",
                table: "InsuranceAccountServiceRates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoadedPricePerMile",
                table: "InsuranceAccountServiceRates",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadheadPricePerMile",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "EnroutePricePerMile",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "LoadedPricePerMile",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "DeadheadPricePerMile",
                table: "InsuranceAccountServiceRates");

            migrationBuilder.DropColumn(
                name: "EnroutePricePerMile",
                table: "InsuranceAccountServiceRates");

            migrationBuilder.DropColumn(
                name: "LoadedPricePerMile",
                table: "InsuranceAccountServiceRates");
        }
    }
}
