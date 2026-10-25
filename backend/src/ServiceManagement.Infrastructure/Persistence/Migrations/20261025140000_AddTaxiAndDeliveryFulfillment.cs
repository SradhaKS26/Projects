using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceManagement.Infrastructure.Persistence;

#nullable disable

namespace ServiceManagement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261025140000_AddTaxiAndDeliveryFulfillment")]
    public partial class AddTaxiAndDeliveryFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FulfillmentKind",
                table: "ServiceCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_FulfillmentKind",
                table: "ServiceCategories",
                column: "FulfillmentKind");

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationAddressId",
                table: "ServiceRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCount",
                table: "ServiceRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParcelDescription",
                table: "ServiceRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "ServiceRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "ServiceRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_DestinationAddressId",
                table: "ServiceRequests",
                column: "DestinationAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_CustomerAddresses_DestinationAddressId",
                table: "ServiceRequests",
                column: "DestinationAddressId",
                principalTable: "CustomerAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_CustomerAddresses_DestinationAddressId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_DestinationAddressId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCategories_FulfillmentKind",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(name: "DestinationAddressId", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "PassengerCount", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "ParcelDescription", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "RecipientName", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "RecipientPhone", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "FulfillmentKind", table: "ServiceCategories");
        }
    }
}
