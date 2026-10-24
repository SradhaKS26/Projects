using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceManagement.Infrastructure.Persistence;

#nullable disable

namespace ServiceManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011120000_AddRequestBroadcast")]
    public partial class AddRequestBroadcast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'utc'");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Status_ExpiresAt",
                table: "ServiceRequests",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_Status_ExpiresAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ServiceRequests");
        }
    }
}
