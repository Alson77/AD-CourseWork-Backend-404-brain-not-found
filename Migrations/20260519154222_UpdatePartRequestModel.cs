using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehiclePartsBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRequests_Customers_CustomerId",
                table: "PartRequests");

            migrationBuilder.RenameColumn(
                name: "VehicleDetails",
                table: "PartRequests",
                newName: "VehicleModel");

            migrationBuilder.RenameColumn(
                name: "RequestDate",
                table: "PartRequests",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "PartRequests",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRequests_CustomerId",
                table: "PartRequests",
                newName: "IX_PartRequests_UserId");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "PartRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "PartRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PartRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PartRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "PartRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "PartRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserRole",
                table: "PartRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRequests_Users_UserId",
                table: "PartRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRequests_Users_UserId",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "UserRole",
                table: "PartRequests");

            migrationBuilder.RenameColumn(
                name: "VehicleModel",
                table: "PartRequests",
                newName: "VehicleDetails");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "PartRequests",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PartRequests",
                newName: "RequestDate");

            migrationBuilder.RenameIndex(
                name: "IX_PartRequests_UserId",
                table: "PartRequests",
                newName: "IX_PartRequests_CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRequests_Customers_CustomerId",
                table: "PartRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
