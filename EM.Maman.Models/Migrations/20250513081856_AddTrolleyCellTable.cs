using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddTrolleyCellTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrolleyCells",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrolleyId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PalletId = table.Column<int>(type: "int", nullable: true),
                    StorageDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TrolleyC__3214EC07XXXXXXXX", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrolleyCells_Pallets",
                        column: x => x.PalletId,
                        principalTable: "Pallets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrolleyCells_Trolleys",
                        column: x => x.TrolleyId,
                        principalTable: "Trolleys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrolleyCells_PalletId",
                table: "TrolleyCells",
                column: "PalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TrolleyCells_TrolleyId",
                table: "TrolleyCells",
                column: "TrolleyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrolleyCells");
        }
    }
}
