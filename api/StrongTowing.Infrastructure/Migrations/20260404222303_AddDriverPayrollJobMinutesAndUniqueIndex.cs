using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPayrollJobMinutesAndUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverPayrolls_DriverId",
                table: "DriverPayrolls");

            migrationBuilder.AddColumn<int>(
                name: "TotalJobMinutes",
                table: "DriverPayrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayrolls_DriverId_PayPeriodStart_PayPeriodEnd",
                table: "DriverPayrolls",
                columns: new[] { "DriverId", "PayPeriodStart", "PayPeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverPayrolls_DriverId_PayPeriodStart_PayPeriodEnd",
                table: "DriverPayrolls");

            migrationBuilder.DropColumn(
                name: "TotalJobMinutes",
                table: "DriverPayrolls");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayrolls_DriverId",
                table: "DriverPayrolls",
                column: "DriverId");
        }
    }
}
