using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceManagement.Infrastructure.Persistence;

#nullable disable

namespace ServiceManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261025160000_AddDualRoleAndDocumentScopes")]
    public partial class AddDualRoleAndDocumentScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentRequirementScopes",
                columns: table => new
                {
                    DocumentRequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRequirementScopes", x => new { x.DocumentRequirementId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_DocumentRequirementScopes_CategoryDocumentRequirements_DocumentRequirementId",
                        column: x => x.DocumentRequirementId,
                        principalTable: "CategoryDocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentRequirementScopes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirementScopes_ServiceId",
                table: "DocumentRequirementScopes",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DocumentRequirementScopes");
        }
    }
}
