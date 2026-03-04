using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeTestLiveKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeLivePublicKey",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeLiveSecretKey",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeLiveWebhookSecret",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeMode",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeTestPublicKey",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeTestSecretKey",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeTestWebhookSecret",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeLivePublicKey",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "StripeLiveSecretKey",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "StripeLiveWebhookSecret",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "StripeMode",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "StripeTestPublicKey",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "StripeTestSecretKey",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "StripeTestWebhookSecret",
                table: "SystemSettings");
        }
    }
}
