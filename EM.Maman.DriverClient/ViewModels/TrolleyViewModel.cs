using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.LocalDbModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EM.Maman.DriverClient.ViewModels
{
    public class TrolleyViewModel
    {
        public ObservableCollection<CompositeRow> Rows { get; set; }
        private const int MaxRows = 30;
        public int HighestActiveRow { get; set; }


        public TrolleyViewModel()
        {
            // Load your cells and fingers from your database/service.
            var allCells = LoadCellsFromDb();
            var allFingers = LoadFingersFromDb();

            Rows = new ObservableCollection<CompositeRow>();
            for (int pos = 0; pos < MaxRows; pos++)
            {
                var leftOuter = allCells.Where(c => c.Side == 2 && c.Order == 1).FirstOrDefault(c => c.Position == pos);
                var leftInner = allCells.Where(c => c.Side == 2 && c.Order == 0).FirstOrDefault(c => c.Position == pos);
                var leftFinger = allFingers.Where(f => f.Side == 0).FirstOrDefault(f => f.Position == pos);

                var rightOuter = allCells.Where(c => c.Side == 1 && c.Order == 1).FirstOrDefault(c => c.Position == pos);
                var rightInner = allCells.Where(c => c.Side == 1 && c.Order == 0).FirstOrDefault(c => c.Position == pos);
                var rightFinger = allFingers.Where(f => f.Side == 1).FirstOrDefault(f => f.Position == pos);

                Rows.Add(new CompositeRow
                {
                    Position = pos,
                    LeftFinger = leftFinger,
                    LeftOuterCell = leftOuter,
                    LeftInnerCell = leftInner,
                    RightOuterCell = rightOuter,
                    RightInnerCell = rightInner,
                    RightFinger = rightFinger
                });
            }
             HighestActiveRow = Rows.Where(r =>
                         r.LeftFinger != null ||
                         r.LeftOuterCell != null ||
                         r.LeftInnerCell != null ||
                         r.RightOuterCell != null ||
                         r.RightInnerCell != null ||
                         r.RightFinger != null)
                      .DefaultIfEmpty(new CompositeRow { Position = 0 })
                      .Max(r => r.Position);

        }

        private IEnumerable<Cell> LoadCellsFromDb()
        {
            var cells = new List<Cell>();
            for (int i = 0; i < 24; i++)
            {
                // Example stub – adjust as needed.
                cells.Add(new Cell { Id = i + 1, Position = i, Side = 1, Order = 0, DisplayName = $"Cell R{i} inner" });
                cells.Add(new Cell { Id = i + 1, Position = i, Side = 1, Order = 1, DisplayName = $"Cell R{i} outer" });
                cells.Add(new Cell { Id = i + 25, Position = i, Side = 2, Order = 0, DisplayName = $"Cell L{i} inner" });
                cells.Add(new Cell { Id = i + 25, Position = i, Side = 2, Order = 1, DisplayName = $"Cell L{i} outer" });
            }
            return cells;
        }

        private IEnumerable<Finger> LoadFingersFromDb()
        {
            return new List<Finger>
            {
                new Finger{ Id = 1, Side = 0, Position = 0, Description = "Finger 1", DisplayName = "100", DisplayColor = "Grey" },
                new Finger{ Id = 2, Side = 1, Position = 10, Description = "Finger 2", DisplayName = "110", DisplayColor = "Grey" },
                new Finger{ Id = 3, Side = 0, Position = 12, Description = "Finger 3", DisplayName = "112", DisplayColor = "Grey" },
                new Finger{ Id = 4, Side = 1, Position = 20, Description = "Finger 4", DisplayName = "120", DisplayColor = "Grey" },
            };
        }
    }
}
