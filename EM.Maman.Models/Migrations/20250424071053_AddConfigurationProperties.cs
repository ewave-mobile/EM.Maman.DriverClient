using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InitializedAt",
                table: "Configurations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InitializedByEmployeeId",
                table: "Configurations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkstationType",
                table: "Configurations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CellEndLocationId",
                table: "Tasks",
                column: "CellEndLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_FingerLocationId",
                table: "Tasks",
                column: "FingerLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Cells_CellEndLocationId",
                table: "Tasks",
                column: "CellEndLocationId",
                principalTable: "Cells",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Fingers_FingerLocationId",
                table: "Tasks",
                column: "FingerLocationId",
                principalTable: "Fingers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Cells_CellEndLocationId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Fingers_FingerLocationId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_CellEndLocationId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_FingerLocationId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "InitializedAt",
                table: "Configurations");

            migrationBuilder.DropColumn(
                name: "InitializedByEmployeeId",
                table: "Configurations");

            migrationBuilder.DropColumn(
                name: "WorkstationType",
                table: "Configurations");
        }
    }
}
