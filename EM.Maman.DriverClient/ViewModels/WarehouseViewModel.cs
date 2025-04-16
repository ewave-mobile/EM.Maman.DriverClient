using EM.Maman.Models.DisplayModels;
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

        /// <summary>
        /// Initializes a new instance of the WarehouseViewModel class
        /// </summary>
        public WarehouseViewModel()
        {
            Rows = new ObservableCollection<CompositeRow>();
            Levels = new ObservableCollection<CompositeLevel>();
            
            // Initialize with test data
            LoadTestData();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads test data for the warehouse
        /// </summary>
        private void LoadTestData()
        {
            // This would be replaced with actual data loading from a repository
            // For now, just create some test levels and rows
            
            // Create levels
            for (int i = 1; i <= 4; i++)
            {
                var level = new CompositeLevel
                {
                    Level = new Level { Id = i, Number = i, DisplayName = $"Level {i}" },
                    Rows = new ObservableCollection<CompositeRow>(),
                    IsCurrentLevel = i == 1,
                    HasTrolley = i == 1 || i == 2,
                    HasSecondTrolley = i == 0
                };
                
                // Create rows for this level
                for (int j = 0; j < 23; j++)
                {
                    var row = new CompositeRow
                    {
                        Position = j,
                        // Add cells and other properties as needed
                    };
                    
                    level.Rows.Add(row);
                }
                
                Levels.Add(level);
            }
            
            // Set initial values
            CurrentLevelNumber = 1;
            SelectedLevelNumber = 1;
            
            // Update rows for the selected level
            UpdateRowsForSelectedLevel();
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
