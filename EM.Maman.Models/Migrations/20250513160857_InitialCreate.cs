using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EM.Maman.Models.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WasDisplayed = table.Column<bool>(type: "bit", nullable: true),
                    ConfirmedToApi = table.Column<bool>(type: "bit", nullable: true),
                    ConfirmedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DownloadDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC07C13F34DE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CellTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CellType__3214EC07AACF101F", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitializedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "('0001-01-01T00:00:00.0000000')"),
                    InitializedByEmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkstationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveTrolleyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Configur__3214EC079B1E2953", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fingers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Position = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Side = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC07767BB490", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Levels__3214EC0796D8C15E", x => x.Id);
                    table.UniqueConstraint("AK_Levels_Number", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "PalletInCell",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PalletId = table.Column<long>(type: "bigint", nullable: true),
                    CellId = table.Column<long>(type: "bigint", nullable: true),
                    StorageDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PalletIn__3214EC0738C95D89", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PalletMovementLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PalletId = table.Column<long>(type: "bigint", nullable: true),
                    FromLocationId = table.Column<long>(type: "bigint", nullable: true),
                    FromLocationTypeId = table.Column<long>(type: "bigint", nullable: true),
                    ToLocationId = table.Column<long>(type: "bigint", nullable: true),
                    ToLocationTypeId = table.Column<long>(type: "bigint", nullable: true),
                    createDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PalletMo__3214EC07502484D2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UldCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AwbCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RefrigerationType = table.Column<int>(type: "int", nullable: true),
                    HeightLevel = table.Column<int>(type: "int", nullable: true),
                    IsCheckedOut = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    CheckedOutDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LocationId = table.Column<long>(type: "bigint", nullable: true),
                    IsSecure = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    CargoType = table.Column<int>(type: "int", nullable: true),
                    UpdateType = table.Column<int>(type: "int", nullable: true),
                    StorageType = table.Column<int>(type: "int", nullable: true),
                    HeightType = table.Column<int>(type: "int", nullable: true),
                    CargoHeight = table.Column<int>(type: "int", nullable: true),
                    ReportType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ImportManifest = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImportUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImportAppearance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportSwbPrefix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportAwbNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportAwbAppearance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportAwbStorage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UldNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UldAirline = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Pallets__3214EC075B4002B5", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingOperation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Parameters = table.Column<string>(name: "Parameters ", type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PendingO__3214EC0756C73109", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefrigerationTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC07DD6A0605", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC07B2FB596A", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<long>(type: "bigint", nullable: true),
                    PalletId = table.Column<long>(type: "bigint", nullable: true),
                    DoneLocationId = table.Column<long>(type: "bigint", nullable: true),
                    ExecutedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsExecuted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TaskDeta__3214EC07CEBCA418", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReportedLocationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TaskLogs__3214EC0722E0A04E", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TaskType__3214EC077981986E", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TraceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Request = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestBody = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResponseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TraceLog__3214EC074AE5E671", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrolleyLoadingLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrolleyId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Side = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC07785E6850", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrolleyMovementLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromLocationId = table.Column<long>(type: "bigint", nullable: true),
                    PlannedToLocationId = table.Column<long>(type: "bigint", nullable: true),
                    ReportedToLocationId = table.Column<long>(type: "bigint", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CompletedReasonId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TrolleyM__3214EC077DBF64AA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trolleys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC0737E90462", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__3214EC0781E6720C", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__3214EC0763E2D4F3", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cells",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true),
                    IsSecuredStorage = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    HeightLevel = table.Column<int>(type: "int", nullable: true, defaultValueSql: "((0))"),
                    Order = table.Column<int>(type: "int", nullable: true),
                    RefrigerationTypeId = table.Column<int>(type: "int", nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((1))"),
                    Side = table.Column<int>(type: "int", nullable: true),
                    IsBlockedReasonId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime", nullable: true),
                    Depth = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cells__3214EC074D1016A6", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cells_Levels",
                        column: x => x.HeightLevel,
                        principalTable: "Levels",
                        principalColumn: "Number");
                });

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

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DownloadDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsExecuted = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    ExecutedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsUploaded = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    UploadDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    TaskTypeId = table.Column<long>(type: "bigint", nullable: true),
                    CellEndLocationId = table.Column<long>(type: "bigint", nullable: true),
                    CurrentTrolleyLocationId = table.Column<long>(type: "bigint", nullable: true),
                    FingerLocationId = table.Column<long>(type: "bigint", nullable: true),
                    PalletId = table.Column<int>(type: "int", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    ActiveTaskStatus = table.Column<int>(type: "int", nullable: true, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tasks__3214EC0749F74CCB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Cells_CellEndLocationId",
                        column: x => x.CellEndLocationId,
                        principalTable: "Cells",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Fingers_FingerLocationId",
                        column: x => x.FingerLocationId,
                        principalTable: "Fingers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cells_HeightLevel",
                table: "Cells",
                column: "HeightLevel");

            migrationBuilder.CreateIndex(
                name: "UQ_Levels_Number",
                table: "Levels",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CellEndLocationId",
                table: "Tasks",
                column: "CellEndLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_FingerLocationId",
                table: "Tasks",
                column: "FingerLocationId");

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
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "CellTypes");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "PalletInCell");

            migrationBuilder.DropTable(
                name: "PalletMovementLogs");

            migrationBuilder.DropTable(
                name: "PendingOperation");

            migrationBuilder.DropTable(
                name: "RefrigerationTypes");

            migrationBuilder.DropTable(
                name: "StorageTypes");

            migrationBuilder.DropTable(
                name: "TaskDetails");

            migrationBuilder.DropTable(
                name: "TaskLogs");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "TaskTypes");

            migrationBuilder.DropTable(
                name: "TraceLogs");

            migrationBuilder.DropTable(
                name: "TrolleyCells");

            migrationBuilder.DropTable(
                name: "TrolleyLoadingLocations");

            migrationBuilder.DropTable(
                name: "TrolleyMovementLogs");

            migrationBuilder.DropTable(
                name: "UploadTasks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Cells");

            migrationBuilder.DropTable(
                name: "Fingers");

            migrationBuilder.DropTable(
                name: "Pallets");

            migrationBuilder.DropTable(
                name: "Trolleys");

            migrationBuilder.DropTable(
                name: "Levels");
        }
    }
}
