using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehiclePartsBackend.Migrations
{
    /// <inheritdoc />
    public partial class MileageToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Mileage",
                table: "CustomerVehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Mileage",
                table: "CustomerVehicles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
