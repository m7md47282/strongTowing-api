using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VehicleCatalogSyncProgressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MakesProcessed",
                table: "VehicleCatalogSyncStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModelsAddedSoFar",
                table: "VehicleCatalogSyncStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalMakes",
                table: "VehicleCatalogSyncStates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MakesProcessed",
                table: "VehicleCatalogSyncStates");

            migrationBuilder.DropColumn(
                name: "ModelsAddedSoFar",
                table: "VehicleCatalogSyncStates");

            migrationBuilder.DropColumn(
                name: "TotalMakes",
                table: "VehicleCatalogSyncStates");
        }
    }
}
