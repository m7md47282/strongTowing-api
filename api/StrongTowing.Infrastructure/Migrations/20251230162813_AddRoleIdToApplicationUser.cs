using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrongTowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleIdToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add RoleId column as nullable first
            migrationBuilder.AddColumn<string>(
                name: "RoleId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            // Create index
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RoleId",
                table: "AspNetUsers",
                column: "RoleId");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetRoles_RoleId",
                table: "AspNetUsers",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // For existing users, set their RoleId based on their roles in AspNetUserRoles
            // This SQL finds the first role for each user and sets RoleId
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.RoleId = (
                    SELECT TOP 1 r.Id 
                    FROM AspNetRoles r
                    INNER JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
                    WHERE ur.UserId = u.Id
                )
                FROM AspNetUsers u
                WHERE u.RoleId IS NULL
                AND EXISTS (SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id);
            ");

            // For users without any role, assign Driver role (create it if it doesn't exist)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Driver')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'Driver', 'DRIVER', NEWID());
                END
                
                UPDATE u
                SET u.RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Driver')
                FROM AspNetUsers u
                WHERE u.RoleId IS NULL;
            ");

            // Drop the index before altering the column
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RoleId",
                table: "AspNetUsers");

            // Now make RoleId required (non-nullable)
            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: false);

            // Recreate the index
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RoleId",
                table: "AspNetUsers",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetRoles_RoleId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RoleId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "AspNetUsers");
        }
    }
}
