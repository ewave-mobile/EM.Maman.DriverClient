using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTaskPalletIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set non-numeric PalletId values to NULL before attempting to alter the column type
            migrationBuilder.Sql("UPDATE Tasks SET PalletId = NULL WHERE ISNUMERIC(PalletId) = 0;");

            migrationBuilder.AlterColumn<int>(
                name: "PalletId",
                table: "Tasks",
                type: "int",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PalletId",
                table: "Tasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
