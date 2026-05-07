using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobsPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_DriverId",
                table: "Jobs");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DriverId_Status",
                table: "Jobs",
                columns: new[] { "DriverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_CreatedAt",
                table: "Jobs",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_DriverId_Status",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status_CreatedAt",
                table: "Jobs");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DriverId",
                table: "Jobs",
                column: "DriverId");
        }
    }
}
