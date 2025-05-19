using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificTaskLocationFieldsToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RetrievalDestinationCellId",
                table: "Tasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RetrievalDestinationFingerId",
                table: "Tasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RetrievalSourceCellId",
                table: "Tasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorageDestinationCellId",
                table: "Tasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorageSourceFingerId",
                table: "Tasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RetrievalDestinationCellId",
                table: "Tasks",
                column: "RetrievalDestinationCellId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RetrievalDestinationFingerId",
                table: "Tasks",
                column: "RetrievalDestinationFingerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RetrievalSourceCellId",
                table: "Tasks",
                column: "RetrievalSourceCellId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StorageDestinationCellId",
                table: "Tasks",
                column: "StorageDestinationCellId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StorageSourceFingerId",
                table: "Tasks",
                column: "StorageSourceFingerId");

            // Data migration SQL
            migrationBuilder.Sql(@"
                -- For existing Storage tasks (TaskTypeId = 2)
                UPDATE Tasks
                SET StorageSourceFingerId = FingerLocationId,
                    StorageDestinationCellId = CellEndLocationId
                WHERE TaskTypeId = 2;
            ");

            migrationBuilder.Sql(@"
                -- For existing Retrieval tasks (TaskTypeId = 1)
                UPDATE Tasks
                SET RetrievalSourceCellId = CellEndLocationId, 
                    RetrievalDestinationFingerId = FingerLocationId
                WHERE TaskTypeId = 1;
            ");
            // RetrievalDestinationCellId will be null for existing API retrieval tasks by default.

            migrationBuilder.AddForeignKey(
                name: "FK_Task_Cell_RetrievalDestination",
                table: "Tasks",
                column: "RetrievalDestinationCellId",
                principalTable: "Cells",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_Cell_RetrievalSource",
                table: "Tasks",
                column: "RetrievalSourceCellId",
                principalTable: "Cells",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_Cell_StorageDestination",
                table: "Tasks",
                column: "StorageDestinationCellId",
                principalTable: "Cells",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_Finger_RetrievalDestination",
                table: "Tasks",
                column: "RetrievalDestinationFingerId",
                principalTable: "Fingers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Task_Finger_StorageSource",
                table: "Tasks",
                column: "StorageSourceFingerId",
                principalTable: "Fingers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Task_Cell_RetrievalDestination",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_Cell_RetrievalSource",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_Cell_StorageDestination",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_Finger_RetrievalDestination",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Task_Finger_StorageSource",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_RetrievalDestinationCellId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_RetrievalDestinationFingerId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_RetrievalSourceCellId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_StorageDestinationCellId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_StorageSourceFingerId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RetrievalDestinationCellId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RetrievalDestinationFingerId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RetrievalSourceCellId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StorageDestinationCellId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StorageSourceFingerId",
                table: "Tasks");
        }
    }
}
