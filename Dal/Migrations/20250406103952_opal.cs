using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class opal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZipOffer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfferDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisitCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZipOffer", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ZipOffer",
                columns: new[] { "Id", "AreaName", "OfferDescription", "VisitCount", "ZipCode" },
                values: new object[,]
                {
                    { 1, "Leesburg", "🚗 10% off on towing services in Leesburg", 0, "20176" },
                    { 2, "Fairfax", "🚨 Free emergency towing up to 5 miles in Fairfax", 0, "22030" },
                    { 3, "Virginia Beach", "🛠️ 15% off vehicle recovery services in Virginia Beach", 0, "23464" },
                    { 4, "Danville", "🚛 Buy 1 tow, get 50% off your next in Danville", 0, "24541" },
                    { 5, "Chesapeake", "🔧 20% discount on long-distance towing in Chesapeake", 0, "23320" },
                    { 6, "Ashburn", "🎁 Free battery jumpstart with every tow in Ashburn", 0, "20147" },
                    { 7, "Charlottesville", "🆘 First-time customers get 25% off towing in Charlottesville", 0, "22903" },
                    { 8, "Salem", "⛓️ Free lockout service with towing in Salem", 0, "24153" },
                    { 9, "Hampton", "📦 Free tire change with towing in Hampton", 0, "23666" },
                    { 10, "Richmond", "🔥 Special offer: 30% off towing this week in Richmond", 0, "23223" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZipOffer");
        }
    }
}
