using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class qooqo888888888888888 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessTypes",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyBusinessDuration",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HasElectricVehicleExperience",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsesDigitalDispatchSoftware",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessTypes",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "CompanyBusinessDuration",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "HasElectricVehicleExperience",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "UsesDigitalDispatchSoftware",
                table: "Provider");
        }
    }
}
