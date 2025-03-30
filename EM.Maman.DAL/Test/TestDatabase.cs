using System;
using System.Collections.Generic;
using System.Linq;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;

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
            new Pallet { Id = 1, DisplayName = "PLT-A001", UldType = "AKE", UldCode = "AKE1234AX", IsSecure = true },
            new Pallet { Id = 2, DisplayName = "PLT-B002", UldType = "PAG", UldCode = "PAG5678BX", IsSecure = false },
            new Pallet { Id = 3, DisplayName = "PLT-C003", UldType = "AKE", UldCode = "AKE9012CX", IsSecure = false },
            new Pallet { Id = 4, DisplayName = "PLT-D004", UldType = "PAJ", UldCode = "PAJ3456DX", IsSecure = true },
            new Pallet { Id = 5, DisplayName = "PLT-E005", UldType = "AKE", UldCode = "AKE7890EX", IsSecure = false },
            new Pallet { Id = 6, DisplayName = "PLT-F006", UldType = "PAG", UldCode = "PAG1234FX", IsSecure = false },
            new Pallet { Id = 7, DisplayName = "PLT-G007", UldType = "PMC", UldCode = "PMC5678GX", IsSecure = true },
            new Pallet { Id = 8, DisplayName = "PLT-H008", UldType = "AKE", UldCode = "AKE9012HX", IsSecure = false }
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
            new Level { Id = 1, Number = 1, DisplayName = "Level 0" },
            new Level { Id = 2, Number = 2, DisplayName = "Level 1" },
            new Level { Id = 3, Number = 3, DisplayName = "Level 2" },
            new Level { Id = 4, Number = 4, DisplayName = "Level 3" }
        };

        // Mimic Cells. For each level we create 23 positions, each with 4 cells.
        public static List<Cell> Cells { get; set; } = CreateCells();

        private static List<Cell> CreateCells()
        {
            var cells = new List<Cell>();

            // Level 1 cells (HeightLevel = 1)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 1000, Position = i, HeightLevel = 1, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1100, Position = i, HeightLevel = 1, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1200, Position = i, HeightLevel = 1, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1300, Position = i, HeightLevel = 1, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 2 cells (HeightLevel = 2)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 2000, Position = i, HeightLevel = 2, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2100, Position = i, HeightLevel = 2, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2200, Position = i, HeightLevel = 2, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2300, Position = i, HeightLevel = 2, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 3 cells (HeightLevel = 3)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 3000, Position = i, HeightLevel = 3, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3100, Position = i, HeightLevel = 3, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3200, Position = i, HeightLevel = 3, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3300, Position = i, HeightLevel = 3, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 4 cells (HeightLevel = 4)
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 4000, Position = i, HeightLevel = 4, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4100, Position = i, HeightLevel = 4, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4200, Position = i, HeightLevel = 4, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4300, Position = i, HeightLevel = 4, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            return cells;
        }

        // Mimic cell–pallet associations. We’ll associate one cell from each level with a pallet.
        public static List<CellWithPalletInfo> CellWithPalletInfos { get; set; } = new List<CellWithPalletInfo>
        {
            // Level 1: use cell with Id = 1000 and 1200
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 1000), Pallet = Pallets[0] },
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 1200), Pallet = Pallets[1] },
            // Level 2: use cell with Id = 2003 and 2200
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 2003), Pallet = Pallets[2] },
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 2200), Pallet = Pallets[3] },
            // Level 3: use cell with Id = 3008 and 3200
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 3008), Pallet = Pallets[4] },
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 3200), Pallet = Pallets[5] },
            // Level 4: use cell with Id = 4000 and 4200
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 4000), Pallet = Pallets[6] },
            new CellWithPalletInfo { Cell = Cells.First(c => c.Id == 4200), Pallet = Pallets[7] }
        };

        // Methods to mimic add/remove operations
        public static void AddPallet(Pallet pallet)
        {
            pallet.Id = Pallets.Any() ? Pallets.Max(p => p.Id) + 1 : 1;
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
