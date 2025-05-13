using EM.Maman.Models.CustomModels;
using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace EM.Maman.DriverClient.ViewModels
{
    public class TrolleyViewModel : INotifyPropertyChanged
    {
        private const int MaxPositionsPerLevel = 24;
        private ObservableCollection<CompositeRow> _rows;
        private ObservableCollection<CompositeLevel> _levels;
        private ObservableCollection<LevelTab> _levelTabs;
        private int _highestActiveRow;
        private int _currentLevelNumber;
        private int _selectedLevelNumber; // Level being viewed
        private int _currentPosition;
        private bool _autoSyncToTrolleyLevel = true;
        private RelayCommand _selectLevelCommand;

        // New properties for trolley cells
        private Models.DisplayModels.TrolleyCell _leftCell;
        private Models.DisplayModels.TrolleyCell _rightCell;
        private string _trolleyName = "Main Trolley";
        
        #region Properties

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

        public ObservableCollection<LevelTab> LevelTabs
        {
            get => _levelTabs;
            set
            {
                if (_levelTabs != value)
                {
                    _levelTabs = value;
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
                    OnPropertyChanged();

                    // If auto-sync is enabled, also update the selected level
                    if (_autoSyncToTrolleyLevel)
                    {
                        SelectedLevelNumber = value;
                    }

                    UpdateCurrentLevel();
                }
            }
        }

        public int SelectedLevelNumber
        {
            get => _selectedLevelNumber;
            set
            {
                if (_selectedLevelNumber != value)
                {
                    _selectedLevelNumber = value;
                    OnPropertyChanged();
                    UpdateLevelTabs();
                    UpdateRowsForSelectedLevel();
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

        public bool AutoSyncToTrolleyLevel
        {
            get => _autoSyncToTrolleyLevel;
            set
            {
                if (_autoSyncToTrolleyLevel != value)
                {
                    _autoSyncToTrolleyLevel = value;
                    OnPropertyChanged();

                    // If enabling auto-sync, immediately sync to current trolley level
                    if (value)
                    {
                        SelectedLevelNumber = CurrentLevelNumber;
                    }
                }
            }
        }

        // New properties for trolley cells
        public Models.DisplayModels.TrolleyCell LeftCell
        {
            get => _leftCell;
            set
            {
                if (_leftCell != value)
                {
                    _leftCell = value;
                    OnPropertyChanged();
                }
            }
        }

        public Models.DisplayModels.TrolleyCell RightCell
        {
            get => _rightCell;
            set
            {
                if (_rightCell != value)
                {
                    _rightCell = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TrolleyName
        {
            get => _trolleyName;
            set
            {
                if (_trolleyName != value)
                {
                    _trolleyName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SelectLevelCommand => _selectLevelCommand ??= new RelayCommand(SelectLevel);

        #endregion

        private readonly IUnitOfWork _unitOfWork; // Keep for backward compatibility
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;

        public TrolleyViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            
            // Get the UnitOfWorkFactory from the App's ServiceProvider
            _unitOfWorkFactory = (App.Current as App)?.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            if (_unitOfWorkFactory == null)
            {
                throw new InvalidOperationException("Could not resolve IUnitOfWorkFactory from ServiceProvider");
            }
            
            Rows = new ObservableCollection<CompositeRow>();
            Levels = new ObservableCollection<CompositeLevel>();
            LevelTabs = new ObservableCollection<LevelTab>();

            // Initialize trolley cells
            LeftCell = new Models.DisplayModels.TrolleyCell { CellPosition = "Left" };
            RightCell = new Models.DisplayModels.TrolleyCell { CellPosition = "Right" };

            // Data loading will be triggered externally via InitializeAsync
            // LoadDataFromDatabaseAsync().ConfigureAwait(false); // REMOVED FROM CONSTRUCTOR

            // Add some test pallets to the trolley cells
            LoadTrolleyPallets();

            // Initialize tabs
            InitializeLevelTabs(); // Keep this if it doesn't involve DB access
        }

        /// <summary>
        /// Asynchronously loads data required by the ViewModel.
        /// Should be called after the ViewModel is constructed.
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync() // Fully qualify Task
        {
            await LoadDataFromDatabaseAsync();
            
            // Load trolley cell data
            await LoadTrolleyCellsFromDatabaseAsync();
            
            // Any other async init needed for this VM
            // return System.Threading.Tasks.Task.CompletedTask; // Explicitly return completed task - removed as await handles it implicitly
        }

        /// <summary>
        /// Loads trolley cells and their pallets from the database
        /// </summary>
        private async System.Threading.Tasks.Task LoadTrolleyCellsFromDatabaseAsync()
        {
            try
            {
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var trolleyCells = await unitOfWork.TrolleyCells.GetByTrolleyIdAsync(EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyId);
                    
                    foreach (var cell in trolleyCells)
                    {
                        if (cell.PalletId.HasValue && cell.Pallet != null)
                        {
                            if (cell.Position == EM.Maman.Common.Constants.TrolleyConstants.LeftCellPosition)
                            {
                                LoadPalletIntoLeftCell(cell.Pallet);
                            }
                            else if (cell.Position == EM.Maman.Common.Constants.TrolleyConstants.RightCellPosition)
                            {
                                LoadPalletIntoRightCell(cell.Pallet);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                System.Diagnostics.Debug.WriteLine($"Error loading trolley cells from database: {ex.Message}");
            }
        }


        private void LoadTrolleyPallets()
        {
            // Create some test pallets for the trolley cells (for demonstration)
            var leftPallet = new Pallet
            {
                Id = 101,
                DisplayName = "TRL-L001",
                UldType = "AKE",
                UldCode = "AKE2345LT",
                IsSecure = true
        };

        // Initialize both cells empty
        LeftCell.Pallet = null; // Initialize empty
        RightCell.Pallet = null;
    }

        // Method to load a pallet into the left cell
        public void LoadPalletIntoLeftCell(Pallet pallet)
        {
            LeftCell.Pallet = pallet;
        }

        // Method to load a pallet into the right cell
        public void LoadPalletIntoRightCell(Pallet pallet)
        {
            RightCell.Pallet = pallet;
        }

        // Method to remove pallet from the left cell
        public Pallet RemovePalletFromLeftCell()
        {
            var pallet = LeftCell.Pallet;
            LeftCell.Pallet = null;
            return pallet;
        }

        // Method to remove pallet from the right cell
        public Pallet RemovePalletFromRightCell()
        {
            var pallet = RightCell.Pallet;
            RightCell.Pallet = null;
            return pallet;
        }

        // Rest of your existing methods...

        //private void LoadTestData()
        //{
        //    var levels = LoadLevelsFromDb();
        //    var allCells = LoadCellsFromDb();
        //    var allFingers = LoadFingersFromDb();
        //    var cellsWithPallets = LoadCellsWithPalletsFromDb();

        //    // Create a dictionary for quick lookup of pallet info by cell ID
        //    var palletsByCellId = cellsWithPallets.ToDictionary(cwp => cwp.Cell.Id, cwp => cwp.Pallet);

        //    // Organize data by levels
        //    foreach (var level in levels)
        //    {
        //        var compositeLevel = new CompositeLevel
        //        {
        //            Level = level,
        //            Rows = new ObservableCollection<CompositeRow>(),
        //            IsCurrentLevel = false,
        //            HasTrolley = level.Number == 1 || level.Number == 2, // Example values
        //            HasSecondTrolley = level.Number == 0 // Example value
        //        };

        //        // Filter cells for this level
        //        var levelCells = allCells.Where(c => c.HeightLevel == level.Number).ToList();

        //        // Create rows for this level
        //        for (int pos = 0; pos < MaxPositionsPerLevel; pos++)
        //        {
        //            // For fingers, we include them in every level at the same position
        //            // This will make them visible on all levels but with different visual appearance
        //            var leftFinger = allFingers.FirstOrDefault(f => f.Side == 0 && f.Position % 100 == pos);
        //            var rightFinger = allFingers.FirstOrDefault(f => f.Side == 1 && f.Position % 100 == pos);

        //            var leftOuter = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 1 && c.Position == pos);
        //            var leftInner = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 0 && c.Position == pos);
        //            var rightOuter = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 1 && c.Position == pos);
        //            var rightInner = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 0 && c.Position == pos);

        //            // Only create a row if there's something to show
        //            if (leftOuter != null || leftInner != null || leftFinger != null ||
        //                rightOuter != null || rightInner != null || rightFinger != null)
        //            {
        //                var row = new CompositeRow
        //                {
        //                    Position = pos,
        //                    LeftFinger = leftFinger,
        //                    LeftOuterCell = leftOuter,
        //                    LeftInnerCell = leftInner,
        //                    RightOuterCell = rightOuter,
        //                    RightInnerCell = rightInner,
        //                    RightFinger = rightFinger,
        //                    // Store pallet information
        //                    LeftOuterPallet = leftOuter != null && palletsByCellId.ContainsKey(leftOuter.Id) ? palletsByCellId[leftOuter.Id] : null,
        //                    LeftInnerPallet = leftInner != null && palletsByCellId.ContainsKey(leftInner.Id) ? palletsByCellId[leftInner.Id] : null,
        //                    RightOuterPallet = rightOuter != null && palletsByCellId.ContainsKey(rightOuter.Id) ? palletsByCellId[rightOuter.Id] : null,
        //                    RightInnerPallet = rightInner != null && palletsByCellId.ContainsKey(rightInner.Id) ? palletsByCellId[rightInner.Id] : null
        //                };

        //                compositeLevel.Rows.Add(row);
        //            }
        //        }

        //        if (compositeLevel.Rows.Any())
        //        {
        //            Levels.Add(compositeLevel);
        //        }
        //    }

        //    // Set initial current level and position
        //    if (Levels.Any())
        //    {
        //        CurrentLevelNumber = Levels[0].Level.Number;
        //        SelectedLevelNumber = CurrentLevelNumber; // Initially the same
        //        CurrentPosition = 0;
        //    }

        //    // For backward compatibility, populate Rows with the first level's rows
        //    if (Levels.Any() && Levels[0].Rows.Any())
        //    {
        //        Rows = Levels[0].Rows;
        //        HighestActiveRow = Rows.Max(r => r.Position);
        //    }
        //}
        private async System.Threading.Tasks.Task LoadDataFromDatabaseAsync()
        {
            try
            {
                // Clear existing data
                Levels.Clear();
                Rows.Clear();

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Get data from repositories
                    var levels = await unitOfWork.Levels.GetAllAsync();
                    var allCells = await unitOfWork.Cells.GetAllAsync();
                    var cellsWithPallets = await unitOfWork.Cells.GetCellsWithPalletsAsync();
                    
                    // Get fingers - assuming there's a repository for them
                    // If there's no repository, we might need to handle this differently
                    var allFingers = await unitOfWork.Fingers.GetAllAsync();

                    // Create a dictionary for quick lookup of pallet info by cell ID
                    var palletsByCellId = cellsWithPallets
                        .Where(cwp => cwp.Pallet != null)
                        .ToDictionary(cwp => cwp.Cell.Id, cwp => cwp.Pallet);

                    // Organize data by levels
                    foreach (var level in levels)
                    {
                    var compositeLevel = new CompositeLevel
                    {
                        Level = level,
                        Rows = new ObservableCollection<CompositeRow>(),
                        IsCurrentLevel = false,
                        HasTrolley = level.Number == 1 || level.Number == 2, // Example values
                        HasSecondTrolley = level.Number == 0 // Example value
                    };

                    // Filter cells for this level
                    var levelCells = allCells.Where(c => c.HeightLevel == level.Number).ToList();

                    // Create rows for this level
                    for (int pos = 0; pos < 23; pos++)
                    {
                        // For fingers, we include them in every level at the same position
                        var leftFinger = allFingers.FirstOrDefault(f => f.Side == 0 && f.Position % 100 == pos);
                        var rightFinger = allFingers.FirstOrDefault(f => f.Side == 1 && f.Position % 100 == pos);

                        var leftOuter = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 1 && c.Position == pos);
                        var leftInner = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 0 && c.Position == pos);
                        var rightOuter = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 1 && c.Position == pos);
                        var rightInner = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 0 && c.Position == pos);

                        // Only create a row if there's something to show
                        if (leftOuter != null || leftInner != null || leftFinger != null ||
                            rightOuter != null || rightInner != null || rightFinger != null)
                        {
                            var row = new CompositeRow
                            {
                                Position = pos,
                                LeftFinger = leftFinger,
                                LeftOuterCell = leftOuter,
                                LeftInnerCell = leftInner,
                                RightOuterCell = rightOuter,
                                RightInnerCell = rightInner,
                                RightFinger = rightFinger,
                                
                                // Store pallet information
                                LeftOuterPallet = leftOuter != null && palletsByCellId.ContainsKey(leftOuter.Id) ? palletsByCellId[leftOuter.Id] : null,
                                LeftInnerPallet = leftInner != null && palletsByCellId.ContainsKey(leftInner.Id) ? palletsByCellId[leftInner.Id] : null,
                                RightOuterPallet = rightOuter != null && palletsByCellId.ContainsKey(rightOuter.Id) ? palletsByCellId[rightOuter.Id] : null,
                                RightInnerPallet = rightInner != null && palletsByCellId.ContainsKey(rightInner.Id) ? palletsByCellId[rightInner.Id] : null,
                                
                                // Initialize additional cells (initially null)
                                LeftCell3 = null,
                                LeftCell4 = null,
                                RightCell3 = null,
                                RightCell4 = null,
                                
                                // Initialize finger pallet counts (for level 1 only)
                                LeftFingerPalletCount = level.Number == 1 && leftFinger != null ? 0 : 0,
                                RightFingerPalletCount = level.Number == 1 && rightFinger != null ? 0 : 0
                            };

                            compositeLevel.Rows.Add(row);
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
                        SelectedLevelNumber = CurrentLevelNumber; // Initially the same
                        CurrentPosition = 0;
                    }

                    // For backward compatibility, populate Rows with the first level's rows
                    if (Levels.Any() && Levels[0].Rows.Any())
                    {
                        Rows = Levels[0].Rows;
                        HighestActiveRow = Rows.Max(r => r.Position);
                    }
                    
                    // Initialize level tabs
                    InitializeLevelTabs();
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                System.Diagnostics.Debug.WriteLine($"Error loading data from database: {ex.Message}");
                
                // Fallback to empty collections
                Levels.Clear();
                Rows.Clear();
                LevelTabs.Clear();
            }
        }

        private void InitializeLevelTabs()
        {
            LevelTabs.Clear();

            // Add tabs for each level (0-3)
            foreach (var level in Levels.OrderBy(l => l.Level.Number))
            {
                var tab = new LevelTab(level.Level, level.Level.Number == SelectedLevelNumber)
                {
                    DisplayName = level.Level.DisplayName
                };
                LevelTabs.Add(tab);
            }
        }

        private void UpdateCurrentLevel()
        {
            // Update the IsCurrentLevel property for all levels
            foreach (var level in Levels)
            {
                level.IsCurrentLevel = level.Level.Number == CurrentLevelNumber;
            }
        }

        private void UpdateLevelTabs()
        {
            foreach (var tab in LevelTabs)
            {
                tab.IsSelected = tab.Level.Number == SelectedLevelNumber;
            }
        }

        private void UpdateRowsForSelectedLevel()
        {
            // Find the level that matches the selected level number
            var selectedLevel = Levels.FirstOrDefault(l => l.Level.Number == SelectedLevelNumber);
            if (selectedLevel != null)
            {
                // Update Rows collection
                Rows = selectedLevel.Rows;
                HighestActiveRow = Rows.Any() ? Rows.Max(r => r.Position) : 0;
            }
        }
        public async System.Threading.Tasks.Task RefreshDataAsync()
        {
            Levels.Clear();
            Rows.Clear();
            LevelTabs.Clear();
            await LoadDataFromDatabaseAsync();
        }

        public void UpdateTrolleyPosition(int levelNumber, int position)
        {
            CurrentLevelNumber = levelNumber;
            CurrentPosition = position;

            // If auto-sync is enabled, automatically switch to showing the trolley's level
            if (AutoSyncToTrolleyLevel)
            {
                SelectedLevelNumber = levelNumber;
            }
        }

        private void SelectLevel(object parameter)
        {
            if (parameter is int levelNumber)
            {
                SelectedLevelNumber = levelNumber;

                // Temporarily disable auto-sync when user explicitly selects a level
                //disable it for a ten second period
              
                if (levelNumber != CurrentLevelNumber)
                {
                    AutoSyncToTrolleyLevel = false;
                }
                System.Threading.Tasks.Task.Delay(10000).ContinueWith(t =>
                {
                    AutoSyncToTrolleyLevel = true;
                });
            }
        }

        #region Test Data Loaders
        // Existing test data loaders remain unchanged
        private IEnumerable<Level> LoadLevelsFromDb()
        {
            // Implementation remains the same
            return new List<Level>
            {
                new Level { Id = 1, Number = 1, DisplayName = "Level 0" },
                new Level { Id = 2, Number = 2, DisplayName = "Level 1" },
                new Level { Id = 3, Number = 3, DisplayName = "Level 2" },
                new Level { Id = 4, Number = 4, DisplayName = "Level 3" }
            };
        }

        private IEnumerable<Cell> LoadCellsFromDb()
        {
            // Implementation remains the same
            var cells = new List<Cell>();

            // Level 0 cells
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 1000, Position = i, HeightLevel = 1, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1100, Position = i, HeightLevel = 1, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1200, Position = i, HeightLevel = 1, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 1300, Position = i, HeightLevel = 1, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 1 cells
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 2000, Position = i, HeightLevel = 2, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2100, Position = i, HeightLevel = 2, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2200, Position = i, HeightLevel = 2, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 2300, Position = i, HeightLevel = 2, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 2 cells
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 3000, Position = i, HeightLevel = 3, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3100, Position = i, HeightLevel = 3, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3200, Position = i, HeightLevel = 3, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 3300, Position = i, HeightLevel = 3, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            // Level 3 cells
            for (int i = 0; i < 23; i++)
            {
                cells.Add(new Cell { Id = i + 4000, Position = i, HeightLevel = 4, Side = 1, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4100, Position = i, HeightLevel = 4, Side = 1, Order = 1, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4200, Position = i, HeightLevel = 4, Side = 2, Order = 0, DisplayName = $"{i + 1}" });
                cells.Add(new Cell { Id = i + 4300, Position = i, HeightLevel = 4, Side = 2, Order = 1, DisplayName = $"{i + 1}" });
            }

            return cells;
        }

        private IEnumerable<Finger> LoadFingersFromDb()
        {
            // Implementation remains the same
            return new List<Finger>
            {
                // Position numbering format: Level*100 + RowPosition
                // For example, 102 means Level 1, Row 2
                
                // Left side fingers (Side = 0)
                new Finger{ Id = 1, Side = 0, Position = 102, Description = "Finger L1-02", DisplayName = "L02", DisplayColor = "Grey" },
                new Finger{ Id = 3, Side = 0, Position = 105, Description = "Finger L1-05", DisplayName = "L05", DisplayColor = "Grey" },
                new Finger{ Id = 5, Side = 0, Position = 112, Description = "Finger L1-12", DisplayName = "L12", DisplayColor = "Grey" },
                new Finger{ Id = 7, Side = 0, Position = 118, Description = "Finger L1-18", DisplayName = "L18", DisplayColor = "Grey" },
                
                // Right side fingers (Side = 1)
                new Finger{ Id = 2, Side = 1, Position = 103, Description = "Finger R1-03", DisplayName = "R03", DisplayColor = "Grey" },
                new Finger{ Id = 4, Side = 1, Position = 108, Description = "Finger R1-08", DisplayName = "R08", DisplayColor = "Grey" },
                new Finger{ Id = 6, Side = 1, Position = 115, Description = "Finger R1-15", DisplayName = "R15", DisplayColor = "Grey" },
                new Finger{ Id = 8, Side = 1, Position = 120, Description = "Finger R1-20", DisplayName = "R20", DisplayColor = "Grey" }
            };
        }

        private IEnumerable<CellWithPalletInfo> LoadCellsWithPalletsFromDb()
        {
            // Implementation remains the same
            // Create some test pallets
            var testPallets = new List<Pallet>
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

            // Create cell associations - these IDs should match some of the cells we're creating elsewhere
            var cellWithPalletInfoList = new List<CellWithPalletInfo>
            {
                // Level 1 pallets
                new CellWithPalletInfo { Cell = new Cell { Id = 1000, Position = 0, HeightLevel = 1, Side = 1, Order = 0 }, Pallet = testPallets[0] },
                new CellWithPalletInfo { Cell = new Cell { Id = 1200, Position = 0, HeightLevel = 1, Side = 2, Order = 0 }, Pallet = testPallets[1] },
                new CellWithPalletInfo { Cell = new Cell { Id = 1005, Position = 5, HeightLevel = 1, Side = 1, Order = 0 }, Pallet = testPallets[2] },
                
                // Level 2 pallets
                new CellWithPalletInfo { Cell = new Cell { Id = 2003, Position = 3, HeightLevel = 2, Side = 1, Order = 0 }, Pallet = testPallets[3] },
                new CellWithPalletInfo { Cell = new Cell { Id = 2103, Position = 3, HeightLevel = 2, Side = 1, Order = 1 }, Pallet = testPallets[4] },
                new CellWithPalletInfo { Cell = new Cell { Id = 2210, Position = 10, HeightLevel = 2, Side = 2, Order = 0 }, Pallet = testPallets[5] },
                
                // Level 3 pallets
                new CellWithPalletInfo { Cell = new Cell { Id = 3008, Position = 8, HeightLevel = 3, Side = 1, Order = 0 }, Pallet = testPallets[6] },
                new CellWithPalletInfo { Cell = new Cell { Id = 3215, Position = 15, HeightLevel = 3, Side = 2, Order = 0 }, Pallet = testPallets[7] }
            };

            return cellWithPalletInfoList;
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
