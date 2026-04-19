using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrucksAndTruckTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy TruckId was free-text; replace with FK to Trucks (data not migrated).
            migrationBuilder.DropColumn(
                name: "TruckId",
                table: "Jobs");

            migrationBuilder.CreateTable(
                name: "TruckTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TruckTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trucks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TruckTypeId = table.Column<int>(type: "int", nullable: false),
                    UnitLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Vin = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    Make = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOutOfService = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trucks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trucks_TruckTypes_TruckTypeId",
                        column: x => x.TruckTypeId,
                        principalTable: "TruckTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "TruckId",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_TruckId",
                table: "Jobs",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_TruckTypeId_UnitLabel",
                table: "Trucks",
                columns: new[] { "TruckTypeId", "UnitLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TruckTypes_Name",
                table: "TruckTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Trucks_TruckId",
                table: "Jobs",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Trucks_TruckId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "Trucks");

            migrationBuilder.DropTable(
                name: "TruckTypes");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_TruckId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TruckId",
                table: "Jobs");

            migrationBuilder.AddColumn<string>(
                name: "TruckId",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
