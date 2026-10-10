using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ServiceProviderProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "ServiceProviderProfiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ServiceProviderProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "ServiceProviderProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAt",
                table: "ProviderServices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ProviderServices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProviderServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CategoryDocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryDocumentRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryDocumentRequirements_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentRequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderDocuments_CategoryDocumentRequirements_DocumentRequ~",
                        column: x => x.DocumentRequirementId,
                        principalTable: "CategoryDocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderDocuments_ServiceProviderProfiles_ProviderProfileId",
                        column: x => x.ProviderProfileId,
                        principalTable: "ServiceProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderProfiles_ApprovalStatus",
                table: "ServiceProviderProfiles",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderProfiles_ReviewedByUserId",
                table: "ServiceProviderProfiles",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDocumentRequirements_CategoryId_Name",
                table: "CategoryDocumentRequirements",
                columns: new[] { "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_DocumentRequirementId",
                table: "ProviderDocuments",
                column: "DocumentRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_ProviderProfileId_DocumentRequirementId",
                table: "ProviderDocuments",
                columns: new[] { "ProviderProfileId", "DocumentRequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_StorageKey",
                table: "ProviderDocuments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProviderProfiles_Users_ReviewedByUserId",
                table: "ServiceProviderProfiles",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceProviderProfiles_Users_ReviewedByUserId",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropTable(
                name: "ProviderDocuments");

            migrationBuilder.DropTable(
                name: "CategoryDocumentRequirements");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProviderProfiles_ApprovalStatus",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProviderProfiles_ReviewedByUserId",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "AppliedAt",
                table: "ProviderServices");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ProviderServices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProviderServices");
        }
    }
}
