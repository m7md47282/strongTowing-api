using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInsuranceAccountTaxServiceCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTaxExemptByDefault",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "ServiceChargePercent",
                table: "InsuranceAccounts");

            migrationBuilder.DropColumn(
                name: "TaxPercent",
                table: "InsuranceAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTaxExemptByDefault",
                table: "InsuranceAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
        }
    }
}
