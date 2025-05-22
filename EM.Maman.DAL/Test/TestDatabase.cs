using System;
using System.Collections.Generic;
using System.Linq;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.Enums;

namespace EM.Maman.DAL.Test
{
    public static class TestDatabase
    {
        // Event that notifies subscribers when test data changes
        public static event EventHandler TestDataChanged;
        private static void OnTestDataChanged()
        {
            TestDataChanged?.Invoke(null, EventArgs.Empty);
        }
        // Mimic a Pallets table
        public static List<Pallet> Pallets { get; set; } = new List<Pallet>
        {
            // --- Import ULD Pallets ---
            new Pallet {
                DisplayName = "PLT-A001-IMP-ULD", UldType = "AKE", UldCode = "AKE1234AX", UldNumber = "1234", UldAirline = "AX", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ,
                ImportManifest = "MANIFEST001", ImportUnit = "UNIT01", ImportAppearance = "Good",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddDays(-1), RefrigerationType = null, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.MED, CargoHeight = 150
            },
            new Pallet {
                DisplayName = "PLT-C003-IMP-ULD", UldType = "RKN", UldCode = "RKN9012CX", UldNumber = "9012", UldAirline = "CX", IsSecure = true,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ,
                ImportManifest = "MANIFEST003", ImportUnit = "UNIT03", ImportAppearance = "Scratched",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-2), RefrigerationType = 2, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.HIGH, CargoHeight = 220
            },
            new Pallet { // New Import ULD
                DisplayName = "PLT-I009-IMP-ULD", UldType = "AMF", UldCode = "AMF5555IX", UldNumber = "5555", UldAirline = "IX", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ,
                ImportManifest = "MANIFEST009", ImportUnit = "UNIT09", ImportAppearance = "New Container",
                ReceivedDate = DateTime.UtcNow.AddHours(-10), LastModifiedDate = DateTime.UtcNow.AddHours(-5), RefrigerationType = 1, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.FRZ, HeightType = HeightType.HIGH, CargoHeight = 280
            },

            // --- Export ULD Pallets ---
            new Pallet {
                DisplayName = "PLT-B002-EXP-ULD", UldType = "PAG", UldCode = "PAG5678BX", UldNumber = "5678", UldAirline = "BX", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ,
                ExportSwbPrefix = "SWB002", ExportAwbNumber = "AWB5678", ExportAwbAppearance = "Excellent", ExportAwbStorage = "Temp Control", ExportBarcode = "BARCODE002",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-2), RefrigerationType = 3, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.MED, CargoHeight = 155
            },
            new Pallet {
                DisplayName = "PLT-D004-EXP-ULD", UldType = "PAJ", UldCode = "PAJ3456DX", UldNumber = "3456", UldAirline = "DX", IsSecure = true,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ,
                ExportSwbPrefix = "SWB004", ExportAwbNumber = "AWB3456", ExportAwbAppearance = "Fair", ExportAwbStorage = "General", ExportBarcode = "BARCODE004",
                ReceivedDate = DateTime.UtcNow.AddDays(-5), LastModifiedDate = DateTime.UtcNow.AddDays(-1), RefrigerationType = null, HeightLevel = 4,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 230
            },
            new Pallet { // New Export ULD
                DisplayName = "PLT-J010-EXP-ULD", UldType = "PMC", UldCode = "PMC7777JX", UldNumber = "7777", UldAirline = "JX", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ,
                ExportSwbPrefix = "SWB010", ExportAwbNumber = "AWB7777", ExportAwbAppearance = "Like New", ExportAwbStorage = "Ambient", ExportBarcode = "BARCODE010",
                ReceivedDate = DateTime.UtcNow.AddHours(-20), LastModifiedDate = DateTime.UtcNow.AddHours(-3), RefrigerationType = null, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 290
            },

            // --- Import AWB Pallets ---
            new Pallet {
                DisplayName = "PLT-E005-IMP-AWB", AwbCode = "AWB7890E", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ,
                ImportManifest = "MANIFEST005", ImportUnit = "UNIT05", ImportAppearance = "New",
                ReceivedDate = DateTime.UtcNow.AddDays(-4), LastModifiedDate = DateTime.UtcNow.AddDays(-3), RefrigerationType = null, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.LOW, CargoHeight = 90
            },
            new Pallet {
                DisplayName = "PLT-G007-IMP-AWB", AwbCode = "AWB5678G", IsSecure = true,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ,
                ImportManifest = "MANIFEST007", ImportUnit = "UNIT07", ImportAppearance = "Sealed",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-12), RefrigerationType = 2, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.MED, CargoHeight = 140
            },
            new Pallet { // New Import AWB
                DisplayName = "PLT-K011-IMP-AWB", AwbCode = "AWB1122K", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ,
                ImportManifest = "MANIFEST011", ImportUnit = "UNIT11", ImportAppearance = "Boxed",
                ReceivedDate = DateTime.UtcNow.AddHours(-8), LastModifiedDate = DateTime.UtcNow.AddHours(-1), RefrigerationType = null, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 210
            },

            // --- Export AWB Pallets ---
            new Pallet {
                DisplayName = "PLT-F006-EXP-AWB", AwbCode = "AWB1234F", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ,
                ExportSwbPrefix = "SWB006", ExportAwbNumber = "AWB1234F", ExportAwbAppearance = "Used", ExportAwbStorage = "Ambient", ExportBarcode = "BARCODE006",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-6), RefrigerationType = null, HeightLevel = 4,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.LOW, CargoHeight = 85
            },
            new Pallet {
                DisplayName = "PLT-H008-EXP-AWB", AwbCode = "AWB9012H", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ,
                ExportSwbPrefix = "SWB008", ExportAwbNumber = "AWB9012H", ExportAwbAppearance = "Good", ExportAwbStorage = "General", ExportBarcode = "BARCODE008",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-1), RefrigerationType = 1, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.MED, CargoHeight = 150
            },
            new Pallet { // New Export AWB
                DisplayName = "PLT-L012-EXP-AWB", AwbCode = "AWB3344L", IsSecure = true,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ,
                ExportSwbPrefix = "SWB012", ExportAwbNumber = "AWB3344L", ExportAwbAppearance = "Securely Wrapped", ExportAwbStorage = "Valuable", ExportBarcode = "BARCODE012",
                ReceivedDate = DateTime.UtcNow.AddHours(-15), LastModifiedDate = DateTime.UtcNow.AddHours(-4), RefrigerationType = null, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 225 // Assuming 'Secure' storage type is handled by IsSecure flag, using REG for general storage.
            },
            // --- Add 18 More Pallets to reach 30 total ---
            new Pallet {
                DisplayName = "PLT-M013-IMP-ULD", UldType = "AKE", UldCode = "AKE2233MM", UldNumber = "2233", UldAirline = "MM", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ImportManifest = "MANIFEST013", ImportUnit = "UNIT13", ImportAppearance = "Good",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow, RefrigerationType = null, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.MED, CargoHeight = 160
            },
            new Pallet {
                DisplayName = "PLT-N014-EXP-AWB", AwbCode = "AWB4455N", IsSecure = true,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB014", ExportAwbNumber = "AWB4455N", ExportAwbAppearance = "Sealed", ExportAwbStorage = "Secure", ExportBarcode = "BARCODE014",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-5), RefrigerationType = 1, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.LOW, CargoHeight = 100
            },
            new Pallet {
                DisplayName = "PLT-O015-IMP-AWB", AwbCode = "AWB6677O", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ImportManifest = "MANIFEST015", ImportUnit = "UNIT15", ImportAppearance = "Boxed",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-1), RefrigerationType = null, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 200
            },
            new Pallet {
                DisplayName = "PLT-P016-EXP-ULD", UldType = "PMC", UldCode = "PMC8899PP", UldNumber = "8899", UldAirline = "PP", IsSecure = true,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB016", ExportAwbNumber = "AWB8899", ExportAwbAppearance = "New", ExportAwbStorage = "Temp Control", ExportBarcode = "BARCODE016",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-3), RefrigerationType = 3, HeightLevel = 4,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.FRZ, HeightType = HeightType.MED, CargoHeight = 170
            },
            new Pallet {
                DisplayName = "PLT-Q017-IMP-ULD", UldType = "RKN", UldCode = "RKN1122QQ", UldNumber = "1122", UldAirline = "QQ", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ImportManifest = "MANIFEST017", ImportUnit = "UNIT17", ImportAppearance = "Used",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-10), RefrigerationType = 2, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.HIGH, CargoHeight = 240
            },
            new Pallet {
                DisplayName = "PLT-R018-EXP-AWB", AwbCode = "AWB3344R", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB018", ExportAwbNumber = "AWB3344R", ExportAwbAppearance = "Fair", ExportAwbStorage = "General", ExportBarcode = "BARCODE018",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-2), RefrigerationType = null, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.LOW, CargoHeight = 95
            },
            new Pallet {
                DisplayName = "PLT-S019-IMP-AWB", AwbCode = "AWB5566S", IsSecure = true,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ImportManifest = "MANIFEST019", ImportUnit = "UNIT19", ImportAppearance = "Wrapped",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-8), RefrigerationType = 1, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.MED, CargoHeight = 145
            },
            new Pallet {
                DisplayName = "PLT-T020-EXP-ULD", UldType = "AMF", UldCode = "AMF7788TT", UldNumber = "7788", UldAirline = "TT", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB020", ExportAwbNumber = "AWB7788", ExportAwbAppearance = "Good", ExportAwbStorage = "Ambient", ExportBarcode = "BARCODE020",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-12), RefrigerationType = null, HeightLevel = 4,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 215
            },
            new Pallet {
                DisplayName = "PLT-U021-IMP-ULD", UldType = "PAG", UldCode = "PAG9900UU", UldNumber = "9900", UldAirline = "UU", IsSecure = true,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ImportManifest = "MANIFEST021", ImportUnit = "UNIT21", ImportAppearance = "Scratched",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-1), RefrigerationType = 3, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.FRZ, HeightType = HeightType.MED, CargoHeight = 165
            },
            new Pallet {
                DisplayName = "PLT-V022-EXP-AWB", AwbCode = "AWB1122V", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB022", ExportAwbNumber = "AWB1122V", ExportAwbAppearance = "Excellent", ExportAwbStorage = "Valuable", ExportBarcode = "BARCODE022",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-6), RefrigerationType = null, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.LOW, CargoHeight = 105
            },
            new Pallet {
                DisplayName = "PLT-W023-IMP-AWB", AwbCode = "AWB3344W", IsSecure = true,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ImportManifest = "MANIFEST023", ImportUnit = "UNIT23", ImportAppearance = "New",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-14), RefrigerationType = 2, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.HIGH, CargoHeight = 190
            },
            new Pallet {
                DisplayName = "PLT-X024-EXP-ULD", UldType = "PAJ", UldCode = "PAJ5566XX", UldNumber = "5566", UldAirline = "XX", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB024", ExportAwbNumber = "AWB5566", ExportAwbAppearance = "Used", ExportAwbStorage = "General", ExportBarcode = "BARCODE024",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-2), RefrigerationType = null, HeightLevel = 4,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.MED, CargoHeight = 175
            },
            new Pallet {
                DisplayName = "PLT-Y025-IMP-ULD", UldType = "AKE", UldCode = "AKE7788YY", UldNumber = "7788", UldAirline = "YY", IsSecure = true,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ImportManifest = "MANIFEST025", ImportUnit = "UNIT25", ImportAppearance = "Good",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-4), RefrigerationType = 1, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.HIGH, CargoHeight = 250
            },
            new Pallet {
                DisplayName = "PLT-Z026-EXP-AWB", AwbCode = "AWB9900Z", IsSecure = false,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB026", ExportAwbNumber = "AWB9900Z", ExportAwbAppearance = "Sealed", ExportAwbStorage = "Secure", ExportBarcode = "BARCODE026",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-9), RefrigerationType = null, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.LOW, CargoHeight = 110
            },
            new Pallet {
                DisplayName = "PLT-AA027-IMP-AWB", AwbCode = "AWB1212A", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ImportManifest = "MANIFEST027", ImportUnit = "UNIT27", ImportAppearance = "Boxed",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-1), RefrigerationType = 3, HeightLevel = 3,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.FRZ, HeightType = HeightType.MED, CargoHeight = 155
            },
            new Pallet {
                DisplayName = "PLT-BB028-EXP-ULD", UldType = "PMC", UldCode = "PMC3434BB", UldNumber = "3434", UldAirline = "BB", IsSecure = true,
                UpdateType = UpdateType.Export, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB028", ExportAwbNumber = "AWB3434", ExportAwbAppearance = "New", ExportAwbStorage = "Temp Control", ExportBarcode = "BARCODE028",
                ReceivedDate = DateTime.UtcNow.AddDays(-1), LastModifiedDate = DateTime.UtcNow.AddHours(-7), RefrigerationType = null, HeightLevel = 4,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.REG, HeightType = HeightType.HIGH, CargoHeight = 205
            },
            new Pallet {
                DisplayName = "PLT-CC029-IMP-ULD", UldType = "RKN", UldCode = "RKN5656CC", UldNumber = "5656", UldAirline = "CC", IsSecure = false,
                UpdateType = UpdateType.Import, CargoType = CargoType.ULD, ReportType = ReportType.REQ, ImportManifest = "MANIFEST029", ImportUnit = "UNIT29", ImportAppearance = "Used",
                ReceivedDate = DateTime.UtcNow.AddDays(-2), LastModifiedDate = DateTime.UtcNow.AddHours(-11), RefrigerationType = 2, HeightLevel = 1,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.MED, CargoHeight = 180
            },
            new Pallet {
                DisplayName = "PLT-DD030-EXP-AWB", AwbCode = "AWB7878D", IsSecure = true,
                UpdateType = UpdateType.Export, CargoType = CargoType.AWB, ReportType = ReportType.REQ, ExportSwbPrefix = "SWB030", ExportAwbNumber = "AWB7878D", ExportAwbAppearance = "Fair", ExportAwbStorage = "General", ExportBarcode = "BARCODE030",
                ReceivedDate = DateTime.UtcNow.AddDays(-3), LastModifiedDate = DateTime.UtcNow.AddDays(-2), RefrigerationType = 1, HeightLevel = 2,
                IsCheckedOut = false, CheckedOutDate = null, StorageType = StorageTypeEnum.PHARMA, HeightType = HeightType.LOW, CargoHeight = 115
            }
        };

        // Mimic a Fingers table
        public static List<Finger> Fingers { get; set; } = new List<Finger>
        {
            new Finger { Id = 1, Side = 0, Position = 102, Description = "Finger L1-02", DisplayName = "L02", DisplayColor = "Grey" },
            new Finger { Id = 3, Side = 0, Position = 105, Description = "Finger L1-05", DisplayName = "L05", DisplayColor = "Grey" },
            new Finger { Id = 5, Side = 0, Position = 112, Description = "Finger L1-12", DisplayName = "L12", DisplayColor = "Grey" },
            new Finger { Id = 7, Side = 0, Position = 118, Description = "Finger L1-18", DisplayName = "L18", DisplayColor = "Grey" },
            new Finger { Id = 2, Side = 1, Position = 103, Description = "Finger R1-03", DisplayName = "R03", DisplayColor = "Grey" },
            new Finger { Id = 4, Side = 1, Position = 108, Description = "Finger R1-08", DisplayName = "R08", DisplayColor = "Grey" },
            new Finger { Id = 6, Side = 1, Position = 115, Description = "Finger R1-15", DisplayName = "R15", DisplayColor = "Grey" },
            new Finger { Id = 8, Side = 1, Position = 120, Description = "Finger R1-20", DisplayName = "R20", DisplayColor = "Grey" }
        };

        // Mimic Levels (4 levels)
        public static List<Level> Levels { get; set; } = new List<Level>
        {
            new Level { Id = 1, Number = 1, DisplayName = "קומה 0" },
            new Level { Id = 2, Number = 2, DisplayName = "קומה 1" },
            new Level { Id = 3, Number = 3, DisplayName = "קומה 2" },
            new Level { Id = 4, Number = 4, DisplayName = "קומה 3" }
        };

        // Mimic Cells. For each level we create 23 positions, each with 4 cells.
        public static List<Cell> Cells { get; set; } = CreateCells();

        // Make this public so it can be called from LoginWindow initialization
        public static List<Cell> CreateCells()
        {
            var cells = new List<Cell>();

            // Level 1 cells (Level = 1)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 1000, Position = i, Level = 1, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1100, Position = i, Level = 1, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1200, Position = i, Level = 1, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1300, Position = i, Level = 1, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 2 cells (HeightLevel = 2)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 2000, Position = i, Level = 2, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2100, Position = i, Level = 2, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2200, Position = i, Level = 2, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2300, Position = i, Level = 2, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 3 cells (Level = 3)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 3000, Position = i, Level = 3, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3100, Position = i, Level = 3, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3200, Position = i, Level = 3, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3300, Position = i, Level = 3, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 4 cells (Level = 4)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 4000, Position = i, Level = 4, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4100, Position = i, Level = 4, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4200, Position = i, Level = 4, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4300, Position = i, Level = 4, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            return cells;
        }

        // Mimic cell–pallet associations.
        public static List<CellWithPalletInfo> CellWithPalletInfos { get; private set; } = InitializeCellPalletInfos();

        private static List<CellWithPalletInfo> InitializeCellPalletInfos()
        {
            var infos = new List<CellWithPalletInfo>();
            var random = new Random();

            // Get all cell IDs as int. Assuming Cell.Id is compatible with int.
            var allCellIds = Cells.Select(c => (int)c.Id).ToList(); // Cast to int
            var usedCellIds = new HashSet<int>();

            // Assign the first 8 pallets to specific, known cells (as before, for consistency if needed)
            // Keys in initialAssignments are cell IDs (int)
            var initialAssignments = new Dictionary<int, int>
            {
                { 1000, 0 }, { 1200, 1 }, { 2003, 2 }, { 2200, 3 },
                { 3008, 4 }, { 3200, 5 }, { 4000, 6 }, { 4200, 7 }
            };

            foreach (var assignment in initialAssignments)
            {
                // Ensure comparison is int to int
                var cell = Cells.FirstOrDefault(c => (int)c.Id == assignment.Key);
                var pallet = Pallets[assignment.Value];
                if (cell != null && pallet != null)
                {
                    infos.Add(new CellWithPalletInfo { Cell = cell, Pallet = pallet });
                    usedCellIds.Add((int)cell.Id); // Store as int
                }
            }

            // Get remaining available cell IDs (all lists/collections are of int)
            var availableCellIds = allCellIds.Except(usedCellIds).ToList();

            // Shuffle the available cell IDs for random assignment
            availableCellIds = availableCellIds.OrderBy(x => random.Next()).ToList();

            // Assign the remaining pallets (up to 30 total)
            int palletsAssignedCount = infos.Count;
            for (int i = palletsAssignedCount; i < Pallets.Count && i < 30; i++)
            {
                if (availableCellIds.Count == 0) break; // No more available cells

                var cellIdToUse = availableCellIds[0]; // This is an int
                availableCellIds.RemoveAt(0);

                // Ensure comparison is int to int
                var cell = Cells.FirstOrDefault(c => (int)c.Id == cellIdToUse);
                var pallet = Pallets[i];

                if (cell != null && pallet != null)
                {
                    infos.Add(new CellWithPalletInfo { Cell = cell, Pallet = pallet });
                }
                else
                {
                    // Log or handle if a cell/pallet isn't found, though this shouldn't happen with current setup
                }
            }
            return infos;
        }


        // Methods to mimic add/remove operations
        public static void AddPallet(Pallet pallet)
        {
            // ID is now database-generated, so we don't set it here.
            // For testing purposes, if you need a temporary ID before DB save,
            // you might assign one, but it will be overwritten by the DB.
            // For this TestDatabase, we'll assume EF Core handles ID assignment on save.
            Pallets.Add(pallet);
            OnTestDataChanged();
        }

        public static bool RemovePallet(int palletId)
        {
            var pallet = Pallets.FirstOrDefault(p => p.Id == palletId);
            if (pallet != null)
            {
                Pallets.Remove(pallet);
                OnTestDataChanged();
                return true;
            }
            return false;
        }
        //add pallet on a cell
        // Add a pallet to a specific cell (by cellId)
        public static void AddPalletToCell(int cellId, Pallet pallet)
        {
            // Find the cell by id in the list of cells
            var cell = Cells.FirstOrDefault(c => c.Id == cellId);
            if (cell != null)
            {
                // Create a new association and add it to the list
                CellWithPalletInfos.Add(new CellWithPalletInfo { Cell = cell, Pallet = pallet });
                OnTestDataChanged();
            }
        }
    }
}
