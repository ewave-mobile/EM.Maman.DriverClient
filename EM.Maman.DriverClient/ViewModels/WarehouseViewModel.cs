using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// View model for warehouse operations
    /// </summary>
    public class WarehouseViewModel : INotifyPropertyChanged
    {
        #region Fields and Properties

        private ObservableCollection<CompositeRow> _rows;
        private ObservableCollection<CompositeLevel> _levels;
        private int _selectedLevelNumber;
        private int _currentLevelNumber;
        private RelayCommand _selectLevelCommand;

        /// <summary>
        /// Gets or sets the collection of rows in the warehouse
        /// </summary>
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

        /// <summary>
        /// Gets or sets the collection of levels in the warehouse
        /// </summary>
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

        /// <summary>
        /// Gets or sets the selected level number
        /// </summary>
        public int SelectedLevelNumber
        {
            get => _selectedLevelNumber;
            set
            {
                if (_selectedLevelNumber != value)
                {
                    _selectedLevelNumber = value;
                    OnPropertyChanged();
                    UpdateRowsForSelectedLevel();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current level number
        /// </summary>
        public int CurrentLevelNumber
        {
            get => _currentLevelNumber;
            set
            {
                if (_currentLevelNumber != value)
                {
                    _currentLevelNumber = value;
                    OnPropertyChanged();
                    UpdateCurrentLevel();
                }
            }
        }

        /// <summary>
        /// Command to select a level
        /// </summary>
        public ICommand SelectLevelCommand => _selectLevelCommand ??= new RelayCommand(SelectLevel);

        #endregion

        #region Constructor

        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the WarehouseViewModel class
        /// </summary>
        public WarehouseViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            
            Rows = new ObservableCollection<CompositeRow>();
            Levels = new ObservableCollection<CompositeLevel>();

            // Data loading will be triggered externally via InitializeAsync
            // LoadDataFromDatabaseAsync().ConfigureAwait(false); // REMOVED FROM CONSTRUCTOR
        }

        /// <summary>
        /// Asynchronously loads data required by the ViewModel.
        /// Should be called after the ViewModel is constructed.
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync() // Fully qualify Task
        {
            await LoadDataFromDatabaseAsync();
            // Any other async init needed for this VM
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads data from the database for the warehouse
        /// </summary>
        private async System.Threading.Tasks.Task LoadDataFromDatabaseAsync()
        {
            try
            {
                // Clear existing data
                Levels.Clear();
                Rows.Clear();

                // Get data from repositories
                var levels = await _unitOfWork.Levels.GetAllAsync();
                var allCells = await _unitOfWork.Cells.GetAllAsync();
                var cellsWithPallets = await _unitOfWork.Cells.GetCellsWithPalletsAsync();
                
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
                        IsCurrentLevel = level.Number == 1, // Default to level 1 as current
                        HasTrolley = level.Number == 1 || level.Number == 2, // Example values
                        HasSecondTrolley = level.Number == 0 // Example value
                    };

                    // Filter cells for this level
                    var levelCells = allCells.Where(c => c.HeightLevel == level.Number).ToList();

                    // Create rows for this level
                    for (int pos = 0; pos < 23; pos++)
                    {
                        // Get cells for this position
                        var leftOuter = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 1 && c.Position == pos);
                        var leftInner = levelCells.FirstOrDefault(c => c.Side == 2 && c.Order == 0 && c.Position == pos);
                        var rightOuter = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 1 && c.Position == pos);
                        var rightInner = levelCells.FirstOrDefault(c => c.Side == 1 && c.Order == 0 && c.Position == pos);

                        // Create a row even if there are no cells, to ensure consistent row count
                        var row = new CompositeRow
                        {
                            Position = pos,
                            LeftOuterCell = leftOuter,
                            LeftInnerCell = leftInner,
                            RightOuterCell = rightOuter,
                            RightInnerCell = rightInner,
                            
                            // Store pallet information
                            LeftOuterPallet = leftOuter != null && palletsByCellId.ContainsKey(leftOuter.Id) ? palletsByCellId[leftOuter.Id] : null,
                            LeftInnerPallet = leftInner != null && palletsByCellId.ContainsKey(leftInner.Id) ? palletsByCellId[leftInner.Id] : null,
                            RightOuterPallet = rightOuter != null && palletsByCellId.ContainsKey(rightOuter.Id) ? palletsByCellId[rightOuter.Id] : null,
                            RightInnerPallet = rightInner != null && palletsByCellId.ContainsKey(rightInner.Id) ? palletsByCellId[rightInner.Id] : null,
                            
                            // Initialize additional cells (initially null)
                            LeftCell3 = null,
                            LeftCell4 = null,
                            RightCell3 = null,
                            RightCell4 = null
                        };
                        
                        compositeLevel.Rows.Add(row);
                    }
                    
                    Levels.Add(compositeLevel);
                }
                
                // Set initial values
                CurrentLevelNumber = 1;
                SelectedLevelNumber = 1;
                
                // Update rows for the selected level
                UpdateRowsForSelectedLevel();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                System.Diagnostics.Debug.WriteLine($"Error loading data from database: {ex.Message}");
                
                // Fallback to empty collections
                Levels.Clear();
                Rows.Clear();
            }
        }

        /// <summary>
        /// Updates the current level
        /// </summary>
        private void UpdateCurrentLevel()
        {
            foreach (var level in Levels)
            {
                level.IsCurrentLevel = level.Level.Number == CurrentLevelNumber;
            }
        }

        /// <summary>
        /// Updates the rows for the selected level
        /// </summary>
        private void UpdateRowsForSelectedLevel()
        {
            var selectedLevel = Levels.FirstOrDefault(l => l.Level.Number == SelectedLevelNumber);
            if (selectedLevel != null)
            {
                Rows = selectedLevel.Rows;
            }
        }

        /// <summary>
        /// Selects a level
        /// </summary>
        private void SelectLevel(object parameter)
        {
            if (parameter is int levelNumber)
            {
                SelectedLevelNumber = levelNumber;
            }
        }

        /// <summary>
        /// Updates a cell's pallet
        /// </summary>
        public void UpdateCellPallet(int position, bool isLeftSide, int cellIndex, Pallet pallet)
        {
            // Find the correct row
            var row = Rows.FirstOrDefault(r => r.Position == position);
            if (row == null) return;

            // Update the appropriate cell pallet property
            if (isLeftSide)
            {
                switch (cellIndex)
                {
                    case 1:
                        row.LeftCell1Pallet = pallet;
                        break;
                    case 2:
                        row.LeftCell2Pallet = pallet;
                        break;
                    case 3:
                        row.LeftCell3Pallet = pallet;
                        break;
                    case 4:
                        row.LeftCell4Pallet = pallet;
                        break;
                }
            }
            else // Right side
            {
                switch (cellIndex)
                {
                    case 1:
                        row.RightCell1Pallet = pallet;
                        break;
                    case 2:
                        row.RightCell2Pallet = pallet;
                        break;
                    case 3:
                        row.RightCell3Pallet = pallet;
                        break;
                    case 4:
                        row.RightCell4Pallet = pallet;
                        break;
                }
            }

            // Notify the UI that the row has changed
            var rowIndex = Rows.IndexOf(row);
            if (rowIndex >= 0)
            {
                Rows.RemoveAt(rowIndex);
                Rows.Insert(rowIndex, row);
            }
        }

        /// <summary>
        /// Gets a cell's pallet
        /// </summary>
        public Pallet GetCellPallet(int position, bool isLeftSide, int cellIndex)
        {
            // Find the correct row
            var row = Rows.FirstOrDefault(r => r.Position == position);
            if (row == null) return null;

            // Get the appropriate cell pallet
            if (isLeftSide)
            {
                switch (cellIndex)
                {
                    case 1:
                        return row.LeftCell1Pallet;
                    case 2:
                        return row.LeftCell2Pallet;
                    case 3:
                        return row.LeftCell3Pallet;
                    case 4:
                        return row.LeftCell4Pallet;
                    default:
                        return null;
                }
            }
            else // Right side
            {
                switch (cellIndex)
                {
                    case 1:
                        return row.RightCell1Pallet;
                    case 2:
                        return row.RightCell2Pallet;
                    case 3:
                        return row.RightCell3Pallet;
                    case 4:
                        return row.RightCell4Pallet;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Gets the current row based on a position
        /// </summary>
        public CompositeRow GetRow(int position)
        {
            return Rows.FirstOrDefault(r => r.Position == position);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
