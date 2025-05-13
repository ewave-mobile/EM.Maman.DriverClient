using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoTypeId",
                table: "Pallets");

            migrationBuilder.AddColumn<int>(
                name: "CargoHeight",
                table: "Pallets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CargoType",
                table: "Pallets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeightType",
                table: "Pallets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportType",
                table: "Pallets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "StorageType",
                table: "Pallets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdateType",
                table: "Pallets",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoHeight",
                table: "Pallets");

            migrationBuilder.DropColumn(
                name: "CargoType",
                table: "Pallets");

            migrationBuilder.DropColumn(
                name: "HeightType",
                table: "Pallets");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "Pallets");

            migrationBuilder.DropColumn(
                name: "StorageType",
                table: "Pallets");

            migrationBuilder.DropColumn(
                name: "UpdateType",
                table: "Pallets");

            migrationBuilder.AddColumn<long>(
                name: "CargoTypeId",
                table: "Pallets",
                type: "bigint",
                nullable: true);
        }
    }
}
