using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCellLevelForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cells_Levels",
                table: "Cells");

            migrationBuilder.DropIndex(
                name: "IX_Cells_HeightLevel",
                table: "Cells");

            migrationBuilder.DropColumn(
                name: "HeightLevel",
                table: "Cells");

            migrationBuilder.CreateIndex(
                name: "IX_Cells_Level",
                table: "Cells",
                column: "Level");

            migrationBuilder.AddForeignKey(
                name: "FK_Cells_Levels",
                table: "Cells",
                column: "Level",
                principalTable: "Levels",
                principalColumn: "Number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cells_Levels",
                table: "Cells");

            migrationBuilder.DropIndex(
                name: "IX_Cells_Level",
                table: "Cells");

            migrationBuilder.AddColumn<int>(
                name: "HeightLevel",
                table: "Cells",
                type: "int",
                nullable: true,
                defaultValueSql: "((0))");

            migrationBuilder.CreateIndex(
                name: "IX_Cells_HeightLevel",
                table: "Cells",
                column: "HeightLevel");

            migrationBuilder.AddForeignKey(
                name: "FK_Cells_Levels",
                table: "Cells",
                column: "HeightLevel",
                principalTable: "Levels",
                principalColumn: "Number");
        }
    }
}
