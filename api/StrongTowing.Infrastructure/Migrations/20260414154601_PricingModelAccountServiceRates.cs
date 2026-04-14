using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PricingModelAccountServiceRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricingFreeMiles",
                table: "SystemSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMile",
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
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Legacy: LoadedPrice was $/mile on loaded leg; DeadHeadPrice was $/mile on return — only BC is billed now.
            migrationBuilder.Sql(
                """
                UPDATE ServicePricingProfiles
                SET PricePerMile = LoadedPrice,
                    BasePrice = 0,
                    HookFeeEnabled = 0,
                    HookFeeAmount = 0
                """);

            migrationBuilder.DropColumn(
                name: "LoadedPrice",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "DeadHeadPrice",
                table: "ServicePricingProfiles");

            migrationBuilder.AddColumn<bool>(
                name: "CommissionVisibleToDriver",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "InsuranceAccountServiceRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceAccountId = table.Column<int>(type: "int", nullable: false),
                    ServicePricingProfileId = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerMile = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HookFeeEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HookFeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceAccountServiceRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceAccountServiceRates_InsuranceAccounts_InsuranceAccountId",
                        column: x => x.InsuranceAccountId,
                        principalTable: "InsuranceAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsuranceAccountServiceRates_ServicePricingProfiles_ServicePricingProfileId",
                        column: x => x.ServicePricingProfileId,
                        principalTable: "ServicePricingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceAccountServiceRates_InsuranceAccountId_ServicePricingProfileId",
                table: "InsuranceAccountServiceRates",
                columns: new[] { "InsuranceAccountId", "ServicePricingProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceAccountServiceRates_ServicePricingProfileId",
                table: "InsuranceAccountServiceRates",
                column: "ServicePricingProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceAccountServiceRates");

            migrationBuilder.DropColumn(
                name: "PricingFreeMiles",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CommissionVisibleToDriver",
                table: "Jobs");

            migrationBuilder.AddColumn<decimal>(
                name: "LoadedPrice",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeadHeadPrice",
                table: "ServicePricingProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE ServicePricingProfiles
                SET LoadedPrice = PricePerMile,
                    DeadHeadPrice = 0
                """);

            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "PricePerMile",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "HookFeeEnabled",
                table: "ServicePricingProfiles");

            migrationBuilder.DropColumn(
                name: "HookFeeAmount",
                table: "ServicePricingProfiles");
        }
    }
}
