using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class s : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackgroundFilesPaths",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ElectronicSignature",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsNotSubjectToBackupWithholding",
                table: "Provider",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfEmployees",
                table: "Provider",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "W9Address",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "W9FilePath",
                table: "Provider",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundFilesPaths",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "ElectronicSignature",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "IsNotSubjectToBackupWithholding",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "NumberOfEmployees",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "W9Address",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "W9FilePath",
                table: "Provider");
        }
    }
}
