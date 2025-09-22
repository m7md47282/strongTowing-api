using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class ssssssssssssssss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IsOpen247",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedTerritory",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedServices",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WillProvideTowingInNVorTX",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZipCodeList",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpen247",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "RequestedTerritory",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "SelectedServices",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "WillProvideTowingInNVorTX",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "ZipCodeList",
                table: "Provider");
        }
    }
}
