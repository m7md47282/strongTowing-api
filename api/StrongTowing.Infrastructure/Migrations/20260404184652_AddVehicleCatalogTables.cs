using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleCatalogMakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhtsaMakeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogMakes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCatalogSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastSyncStartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncCompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MakesCount = table.Column<int>(type: "int", nullable: false),
                    ModelsCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogSyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCatalogModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MakeId = table.Column<int>(type: "int", nullable: false),
                    NhtsaModelId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleCatalogModels_VehicleCatalogMakes_MakeId",
                        column: x => x.MakeId,
                        principalTable: "VehicleCatalogMakes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogMakes_NhtsaMakeId",
                table: "VehicleCatalogMakes",
                column: "NhtsaMakeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogModels_MakeId_NhtsaModelId",
                table: "VehicleCatalogModels",
                columns: new[] { "MakeId", "NhtsaModelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleCatalogModels");

            migrationBuilder.DropTable(
                name: "VehicleCatalogSyncStates");

            migrationBuilder.DropTable(
                name: "VehicleCatalogMakes");
        }
    }
}
