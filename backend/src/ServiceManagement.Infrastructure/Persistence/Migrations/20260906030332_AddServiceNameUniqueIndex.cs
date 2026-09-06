using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_CategoryId",
                table: "Services");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId_Name",
                table: "Services",
                columns: new[] { "CategoryId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_CategoryId_Name",
                table: "Services");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId",
                table: "Services",
                column: "CategoryId");
        }
    }
}
