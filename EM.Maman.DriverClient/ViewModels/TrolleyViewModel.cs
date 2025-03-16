using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EM.Maman.DriverClient.ViewModels
{
    public class TrolleyViewModel : INotifyPropertyChanged
    {
        private const int MaxPositionsPerLevel = 24;
        private ObservableCollection<CompositeRow> _rows;
        private ObservableCollection<CompositeLevel> _levels;
        private int _highestActiveRow;
        private int _currentLevelNumber;
        private int _currentPosition;

        public ObservableCollection<CompositeRow> Rows
        {
            get => _rows;
            set
            {
                if (_rows != value)
                {
                    _rows = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<CompositeLevel> Levels
        {
            get => _levels;
            set
            {
                if (_levels != value)
                {
                    _levels = value;
                    OnPropertyChanged();
                }
            }
        }

        public int HighestActiveRow
        {
            get => _highestActiveRow;
            set
            {
                if (_highestActiveRow != value)
                {
                    _highestActiveRow = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentLevelNumber
        {
            get => _currentLevelNumber;
            set
            {
                if (_currentLevelNumber != value)
                {
                    _currentLevelNumber = value;
                    UpdateCurrentLevel();
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public TrolleyViewModel()
        {
            Rows = new ObservableCollection<CompositeRow>();
            Levels = new ObservableCollection<CompositeLevel>();

            // Load test data
            LoadTestData();
        }

        private void LoadTestData()
        {
            // Load levels, cells, and fingers
            var levels = LoadLevelsFromDb();
            var allCells = LoadCellsFromDb();
            var allFingers = LoadFingersFromDb();

            // Organize data by levels
            foreach (var level in levels)
            {
                var compositeLevel = new CompositeLevel
                {
                    Level = level,
                    Rows = new ObservableCollection<CompositeRow>()
                };

                // Filter cells and fingers for this level
                var levelCells = allCells.Where(c => c.HeightLevel == level.Number).ToList();
                var levelFingers = allFingers.Where(f => f.Position / 100 == level.Number).ToList();

                // Create rows for this level
                for (int pos = 0; pos < MaxPositionsPerLevel; pos++)
                {
                    var leftOuter = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 1 && c.Position == pos);
                    var leftInner = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 0 && c.Position == pos);
                    var leftFinger = levelFingers.FirstOrDefault(f => f.Side == 0 && f.Position % 100 == pos);

                    var rightOuter = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 1 && c.Position == pos);
                    var rightInner = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 0 && c.Position == pos);
                    var rightFinger = levelFingers.FirstOrDefault(f => f.Side == 1 && f.Position % 100 == pos);

                    if (leftOuter != null || leftInner != null || leftFinger != null ||
                        rightOuter != null || rightInner != null || rightFinger != null)
                    {
                        compositeLevel.Rows.Add(new CompositeRow
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
                }

                if (compositeLevel.Rows.Any())
                {
                    Levels.Add(compositeLevel);
                }
            }

            // Set initial current level and position
            if (Levels.Any())
            {
                CurrentLevelNumber = Levels[0].Level.Number;
                CurrentPosition = 0;
            }

            // For backward compatibility, populate Rows with the first level's rows
            if (Levels.Any() && Levels[0].Rows.Any())
            {
                Rows = Levels[0].Rows;
                HighestActiveRow = Rows.Max(r => r.Position);
            }
        }

        private void UpdateCurrentLevel()
        {
            // Update the IsCurrentLevel property for all levels
            foreach (var level in Levels)
            {
                level.IsCurrentLevel = level.Level.Number == CurrentLevelNumber;
            }

            // Update Rows collection for backward compatibility
            var currentLevel = Levels.FirstOrDefault(l => l.Level.Number == CurrentLevelNumber);
            if (currentLevel != null)
            {
                Rows = currentLevel.Rows;
                HighestActiveRow = Rows.Any() ? Rows.Max(r => r.Position) : 0;
            }
        }

        public void UpdateTrolleyPosition(int levelNumber, int position)
        {
            CurrentLevelNumber = levelNumber;
            CurrentPosition = position;
        }

        #region Test Data Loaders

        private IEnumerable<Level> LoadLevelsFromDb()
        {
            // This would come from your database in a real implementation
            return new List<Level>
            {
                new Level { Id = 1, Number = 102, DisplayName = "102" },
                new Level { Id = 2, Number = 105, DisplayName = "105" },
                new Level { Id = 3, Number = 108, DisplayName = "108" },
                new Level { Id = 4, Number = 115, DisplayName = "115" },
                new Level { Id = 5, Number = 119, DisplayName = "119" },
                new Level { Id = 6, Number = 122, DisplayName = "122" }
            };
        }

        private IEnumerable<Cell> LoadCellsFromDb()
        {
            var cells = new List<Cell>();

            // Level 102 cells
            for (int i = 0; i < 10; i++)
            {
                cells.Add(new Cell { Id = i + 1000, Position = i, HeightLevel = 102, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1100, Position = i, HeightLevel = 102, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1200, Position = i, HeightLevel = 102, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1300, Position = i, HeightLevel = 102, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 105 cells
            cells.Add(new Cell { Id = 2001, Position = 5, HeightLevel = 105, Side = 1, Order = 0, DisplayName = "89" });
            cells.Add(new Cell { Id = 2002, Position = 5, HeightLevel = 105, Side = 1, Order = 1, DisplayName = "89" });
            cells.Add(new Cell { Id = 2003, Position = 6, HeightLevel = 105, Side = 1, Order = 0, DisplayName = "67" });
            cells.Add(new Cell { Id = 2004, Position = 6, HeightLevel = 105, Side = 1, Order = 1, DisplayName = "67" });
            cells.Add(new Cell { Id = 2005, Position = 7, HeightLevel = 105, Side = 1, Order = 0, DisplayName = "12.3" });
            cells.Add(new Cell { Id = 2006, Position = 7, HeightLevel = 105, Side = 1, Order = 1, DisplayName = "12.3" });

            cells.Add(new Cell { Id = 2007, Position = 5, HeightLevel = 105, Side = 2, Order = 0, DisplayName = "89" });
            cells.Add(new Cell { Id = 2008, Position = 5, HeightLevel = 105, Side = 2, Order = 1, DisplayName = "89" });
            cells.Add(new Cell { Id = 2009, Position = 6, HeightLevel = 105, Side = 2, Order = 0, DisplayName = "67" });
            cells.Add(new Cell { Id = 2010, Position = 6, HeightLevel = 105, Side = 2, Order = 1, DisplayName = "67" });
            cells.Add(new Cell { Id = 2011, Position = 7, HeightLevel = 105, Side = 2, Order = 0, DisplayName = "12.3" });
            cells.Add(new Cell { Id = 2012, Position = 7, HeightLevel = 105, Side = 2, Order = 1, DisplayName = "12.3" });

            // Level 108 cells
            cells.Add(new Cell { Id = 3001, Position = 8, HeightLevel = 108, Side = 1, Order = 0, DisplayName = "12" });
            cells.Add(new Cell { Id = 3002, Position = 8, HeightLevel = 108, Side = 1, Order = 1, DisplayName = "12" });
            cells.Add(new Cell { Id = 3003, Position = 9, HeightLevel = 108, Side = 1, Order = 0, DisplayName = "22" });
            cells.Add(new Cell { Id = 3004, Position = 9, HeightLevel = 108, Side = 1, Order = 1, DisplayName = "22" });
            cells.Add(new Cell { Id = 3005, Position = 8, HeightLevel = 108, Side = 2, Order = 0, DisplayName = "12" });
            cells.Add(new Cell { Id = 3006, Position = 8, HeightLevel = 108, Side = 2, Order = 1, DisplayName = "12" });
            cells.Add(new Cell { Id = 3007, Position = 9, HeightLevel = 108, Side = 2, Order = 0, DisplayName = "22" });
            cells.Add(new Cell { Id = 3008, Position = 9, HeightLevel = 108, Side = 2, Order = 1, DisplayName = "22" });

            // Level 119 cells
            cells.Add(new Cell { Id = 4001, Position = 21, HeightLevel = 119, Side = 1, Order = 0, DisplayName = "678/90" });
            cells.Add(new Cell { Id = 4002, Position = 21, HeightLevel = 119, Side = 1, Order = 1, DisplayName = "678/90" });
            cells.Add(new Cell { Id = 4003, Position = 22, HeightLevel = 119, Side = 1, Order = 0, DisplayName = "87" });
            cells.Add(new Cell { Id = 4004, Position = 22, HeightLevel = 119, Side = 1, Order = 1, DisplayName = "87" });
            cells.Add(new Cell { Id = 4005, Position = 21, HeightLevel = 119, Side = 2, Order = 0, DisplayName = "678/90" });
            cells.Add(new Cell { Id = 4006, Position = 21, HeightLevel = 119, Side = 2, Order = 1, DisplayName = "678/90" });
            cells.Add(new Cell { Id = 4007, Position = 22, HeightLevel = 119, Side = 2, Order = 0, DisplayName = "87" });
            cells.Add(new Cell { Id = 4008, Position = 22, HeightLevel = 119, Side = 2, Order = 1, DisplayName = "87" });

            return cells;
        }

        private IEnumerable<Finger> LoadFingersFromDb()
        {
            // Position is calculated as (level*100 + position)
            return new List<Finger>
            {
                new Finger{ Id = 1, Side = 0, Position = 10500, Description = "Finger 105-00", DisplayName = "2", DisplayColor = "Grey" },
                new Finger{ Id = 2, Side = 1, Position = 10810, Description = "Finger 108-10", DisplayName = "3", DisplayColor = "Grey" },
                new Finger{ Id = 3, Side = 0, Position = 11912, Description = "Finger 119-12", DisplayName = "4", DisplayColor = "Grey" },
                new Finger{ Id = 4, Side = 1, Position = 11700, Description = "Finger 117-00", DisplayName = "2", DisplayColor = "Grey" }
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}