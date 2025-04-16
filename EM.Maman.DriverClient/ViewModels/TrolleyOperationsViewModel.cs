using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class TrolleyOperationsViewModel : INotifyPropertyChanged
    {
        private TrolleyViewModel _trolleyVM;
        private Trolley _currentTrolley;

        public TrolleyViewModel TrolleyVM
        {
            get => _trolleyVM;
            set
            {
                if (_trolleyVM != value)
                {
                    _trolleyVM = value;
                    OnPropertyChanged(nameof(TrolleyVM));
                }
            }
        }

        public Trolley CurrentTrolley
        {
            get => _currentTrolley;
            set
            {
                if (_currentTrolley != value)
                {
                    _currentTrolley = value;
                    OnPropertyChanged(nameof(CurrentTrolley));
                }
            }
        }

        // Test Commands for Trolley Cells
        private RelayCommand _testLoadLeftCellCommand;
        private RelayCommand _testLoadRightCellCommand;
        private RelayCommand _testUnloadLeftCellCommand;
        private RelayCommand _testUnloadRightCellCommand;

        public ICommand TestLoadLeftCellCommand => _testLoadLeftCellCommand ??= new RelayCommand(_ => TestLoadLeftCell(), _ => true);
        public ICommand TestLoadRightCellCommand => _testLoadRightCellCommand ??= new RelayCommand(_ => TestLoadRightCell(), _ => true);
        public ICommand TestUnloadLeftCellCommand => _testUnloadLeftCellCommand ??= new RelayCommand(_ => TestUnloadLeftCell(), _ => CanUnloadLeftCell());
        public ICommand TestUnloadRightCellCommand => _testUnloadRightCellCommand ??= new RelayCommand(_ => TestUnloadRightCell(), _ => CanUnloadRightCell());

        public TrolleyOperationsViewModel(TrolleyViewModel trolleyVM, Trolley currentTrolley)
        {
            TrolleyVM = trolleyVM;
            CurrentTrolley = currentTrolley;
        }

        // Method to add a pallet to the trolley's left cell
        public void AddPalletToTrolleyLeftCell(Pallet pallet)
        {
            if (TrolleyVM != null)
            {
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
            }
        }

        // Method to add a pallet to the trolley's right cell
        public void AddPalletToTrolleyRightCell(Pallet pallet)
        {
            if (TrolleyVM != null)
            {
                TrolleyVM.LoadPalletIntoRightCell(pallet);
            }
        }

        // Method to remove a pallet from the trolley's left cell
        public Pallet RemovePalletFromTrolleyLeftCell()
        {
            if (TrolleyVM != null)
            {
                return TrolleyVM.RemovePalletFromLeftCell();
            }
            return null;
        }

        // Method to remove a pallet from the trolley's right cell
        public Pallet RemovePalletFromTrolleyRightCell()
        {
            if (TrolleyVM != null)
            {
                return TrolleyVM.RemovePalletFromRightCell();
            }
            return null;
        }

        // Helper method to get the current composite row based on trolley position
        private CompositeRow GetCurrentRow()
        {
            if (TrolleyVM?.Rows == null || CurrentTrolley == null)
                return null;

            return TrolleyVM.Rows.FirstOrDefault(row => row.Position == CurrentTrolley.Position);
        }

        // Helper method to update a warehouse cell's pallet
        private void UpdateWarehouseCellPallet(int position, bool isLeftSide, int cellIndex, Pallet pallet)
        {
            // Find the correct row
            var row = TrolleyVM.Rows.FirstOrDefault(r => r.Position == position);
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

            // Notify the UI that the row has changed (needed since we're modifying a property of a property)
            var rowIndex = TrolleyVM.Rows.IndexOf(row);
            if (rowIndex >= 0)
            {
                TrolleyVM.Rows.RemoveAt(rowIndex);
                TrolleyVM.Rows.Insert(rowIndex, row);
            }
        }

        // Modified TestLoadLeftCell method with improved visual updates and level handling
        private void TestLoadLeftCell()
        {
            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            // Check if we're at the appropriate level for the warehouse cells
            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                MessageBox.Show($"Trolley is at level {TrolleyVM.CurrentLevelNumber} but viewing level {TrolleyVM.SelectedLevelNumber}. " +
                               "Please ensure you're viewing the correct level.");
            }

            // Special handling for lowest level (level 1) - load from finger instead of cell
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            
            if (isLowestLevel && currentRow.LeftFinger != null)
            {
                // On lowest level, check if finger has pallets
                if (currentRow.LeftFingerPalletCount > 0)
                {
                    // Check if trolley cell already has a pallet
                    if (TrolleyVM.LeftCell.IsOccupied)
                    {
                        // If right cell is empty, move left cell pallet to right cell first
                        if (!TrolleyVM.RightCell.IsOccupied)
                        {
                            // Move pallet from left to right cell
                            Pallet leftPallet = TrolleyVM.LeftCell.Pallet;
                            TrolleyVM.RemovePalletFromLeftCell();
                            TrolleyVM.LoadPalletIntoRightCell(leftPallet);
                            MessageBox.Show($"Moved pallet {leftPallet.DisplayName} from left to right trolley cell");
                        }
                        else
                        {
                            MessageBox.Show("Both trolley cells are occupied. Please unload one first.");
                            return;
                        }
                    }
                    
                    // Create a new pallet from the finger
                    var random = new Random();
                    int palletId = random.Next(1000, 10000);
                    var pallet = new Pallet
                    {
                        Id = palletId,
                        DisplayName = $"F-{palletId}",
                        UldType = "AKE",
                        UldCode = $"AKE{palletId}LF"
                    };
                    
                    // Load the pallet into the trolley cell
                    TrolleyVM.LoadPalletIntoLeftCell(pallet);
                    
                    // Decrease finger pallet count
                    currentRow.LeftFingerPalletCount--;
                    
                    MessageBox.Show($"Loaded pallet {pallet.DisplayName} from left finger. Remaining pallets: {currentRow.LeftFingerPalletCount}");
                    return;
                }
                else
                {
                    MessageBox.Show("No pallets available on the left finger");
                    return;
                }
            }

            // Regular cell handling for non-lowest levels
            // Check if there's a pallet in the left outer cell
            bool hasOuterPallet = currentRow.LeftCell1Pallet != null;

            if (hasOuterPallet)
            {
                // Get pallet info from the warehouse cell
                Pallet warehousePallet = currentRow.LeftCell1Pallet;
                string palletDisplay = warehousePallet.DisplayName;

                // Check if trolley cell already has a pallet
                if (TrolleyVM.LeftCell.IsOccupied)
                {
                    // If right cell is empty, move left cell pallet to right cell first
                    if (!TrolleyVM.RightCell.IsOccupied)
                    {
                        // Move pallet from left to right cell
                        Pallet leftPallet = TrolleyVM.LeftCell.Pallet;
                        TrolleyVM.RemovePalletFromLeftCell();
                        TrolleyVM.LoadPalletIntoRightCell(leftPallet);
                        MessageBox.Show($"Moved pallet {leftPallet.DisplayName} from left to right trolley cell");
                        
                        // Now load the warehouse pallet into the left cell
                        TrolleyVM.LoadPalletIntoLeftCell(warehousePallet);
                        
                        // Remove the pallet from the warehouse cell
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, 1, null);
                        
                        MessageBox.Show($"Loaded pallet {palletDisplay} from left outer cell to trolley");
                    }
                    else
                    {
                        // Create a reference to the current trolley pallet before replacing it
                        Pallet trolleyPallet = TrolleyVM.LeftCell.Pallet;
                        string trolleyPalletDisplay = trolleyPallet.DisplayName;

                        MessageBox.Show($"Swapping: Pallet {trolleyPalletDisplay} moved to left outer cell and pallet {palletDisplay} moved to trolley");

                        // Swap the pallets (visually update both the trolley and warehouse cells)
                        var tempPallet = warehousePallet;

                        // Update warehouse cell with trolley's pallet
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, 1, trolleyPallet);

                        // Update trolley cell with warehouse pallet
                        TrolleyVM.LoadPalletIntoLeftCell(tempPallet);
                    }
                }
                else
                {
                    MessageBox.Show($"Loading: Pallet {palletDisplay} from left outer cell to trolley");

                    // Load the pallet into the trolley cell
                    TrolleyVM.LoadPalletIntoLeftCell(warehousePallet);

                    // Remove the pallet from the warehouse cell
                    UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, 1, null);
                }
            }
            else
            {
                MessageBox.Show("No pallet in the left outer cell to load");

                // Check inner cells as alternatives
                Pallet innerPallet = null;
                int cellIndex = 0;
                
                if (currentRow.LeftCell2Pallet != null)
                {
                    innerPallet = currentRow.LeftCell2Pallet;
                    cellIndex = 2;
                }
                else if (currentRow.LeftCell3Pallet != null)
                {
                    innerPallet = currentRow.LeftCell3Pallet;
                    cellIndex = 3;
                }
                else if (currentRow.LeftCell4Pallet != null)
                {
                    innerPallet = currentRow.LeftCell4Pallet;
                    cellIndex = 4;
                }
                
                if (innerPallet != null)
                {
                    var result = MessageBox.Show(
                        $"Would you like to use the pallet from cell {cellIndex} instead?",
                        "Use Inner Cell",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (TrolleyVM.LeftCell.IsOccupied)
                        {
                            // If right cell is empty, move left cell pallet to right cell first
                            if (!TrolleyVM.RightCell.IsOccupied)
                            {
                                // Move pallet from left to right cell
                                Pallet leftPallet = TrolleyVM.LeftCell.Pallet;
                                TrolleyVM.RemovePalletFromLeftCell();
                                TrolleyVM.LoadPalletIntoRightCell(leftPallet);
                                MessageBox.Show($"Moved pallet {leftPallet.DisplayName} from left to right trolley cell");
                                
                                // Now load the inner pallet into the left cell
                                TrolleyVM.LoadPalletIntoLeftCell(innerPallet);
                                
                                // Remove the pallet from the warehouse cell
                                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, cellIndex, null);
                                
                                MessageBox.Show($"Loaded pallet {innerPallet.DisplayName} from left cell {cellIndex} to trolley");
                            }
                            else
                            {
                                // Swap with trolley pallet
                                Pallet trolleyPallet = TrolleyVM.LeftCell.Pallet;

                                // Update warehouse cell with trolley's pallet
                                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, cellIndex, trolleyPallet);
                                
                                TrolleyVM.LoadPalletIntoLeftCell(innerPallet);

                                MessageBox.Show($"Swapped with inner cell {cellIndex} pallet: {innerPallet.DisplayName}");
                            }
                        }
                        else
                        {
                            // Just load from inner cell
                            TrolleyVM.LoadPalletIntoLeftCell(innerPallet);
                            
                            // Remove the pallet from the warehouse cell
                            UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, cellIndex, null);

                            MessageBox.Show($"Loaded from inner cell {cellIndex}: {innerPallet.DisplayName}");
                        }
                        return;
                    }
                }
            }
        }

        // Modified TestLoadRightCell method with improved visual updates and level handling
        private void TestLoadRightCell()
        {
            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            // Check if we're at the appropriate level for the warehouse cells
            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                MessageBox.Show($"Trolley is at level {TrolleyVM.CurrentLevelNumber} but viewing level {TrolleyVM.SelectedLevelNumber}. " +
                               "Please ensure you're viewing the correct level.");
            }

            // Special handling for lowest level (level 1) - load from finger instead of cell
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            
            if (isLowestLevel && currentRow.RightFinger != null)
            {
                // On lowest level, check if finger has pallets
                if (currentRow.RightFingerPalletCount > 0)
                {
                    // Check if trolley cell already has a pallet
                    if (TrolleyVM.RightCell.IsOccupied)
                    {
                        // If left cell is empty, move right cell pallet to left cell first
                        if (!TrolleyVM.LeftCell.IsOccupied)
                        {
                            // Move pallet from right to left cell
                            Pallet rightPallet = TrolleyVM.RightCell.Pallet;
                            TrolleyVM.RemovePalletFromRightCell();
                            TrolleyVM.LoadPalletIntoLeftCell(rightPallet);
                            MessageBox.Show($"Moved pallet {rightPallet.DisplayName} from right to left trolley cell");
                        }
                        else
                        {
                            MessageBox.Show("Both trolley cells are occupied. Please unload one first.");
                            return;
                        }
                    }
                    
                    // Create a new pallet from the finger
                    var random = new Random();
                    int palletId = random.Next(1000, 10000);
                    var pallet = new Pallet
                    {
                        Id = palletId,
                        DisplayName = $"F-{palletId}",
                        UldType = "PAG",
                        UldCode = $"PAG{palletId}RF"
                    };
                    
                    // Load the pallet into the trolley cell
                    TrolleyVM.LoadPalletIntoRightCell(pallet);
                    
                    // Decrease finger pallet count
                    currentRow.RightFingerPalletCount--;
                    
                    MessageBox.Show($"Loaded pallet {pallet.DisplayName} from right finger. Remaining pallets: {currentRow.RightFingerPalletCount}");
                    return;
                }
                else
                {
                    MessageBox.Show("No pallets available on the right finger");
                    return;
                }
            }

            // Regular cell handling for non-lowest levels
            // Check if there's a pallet in the right outer cell
            bool hasOuterPallet = currentRow.RightCell1Pallet != null;

            if (hasOuterPallet)
            {
                // Get pallet info from the warehouse cell
                Pallet warehousePallet = currentRow.RightCell1Pallet;
                string palletDisplay = warehousePallet.DisplayName;

                // Check if trolley cell already has a pallet
                if (TrolleyVM.RightCell.IsOccupied)
                {
                    // If left cell is empty, move right cell pallet to left cell first
                    if (!TrolleyVM.LeftCell.IsOccupied)
                    {
                        // Move pallet from right to left cell
                        Pallet rightPallet = TrolleyVM.RightCell.Pallet;
                        TrolleyVM.RemovePalletFromRightCell();
                        TrolleyVM.LoadPalletIntoLeftCell(rightPallet);
                        MessageBox.Show($"Moved pallet {rightPallet.DisplayName} from right to left trolley cell");
                        
                        // Now load the warehouse pallet into the right cell
                        TrolleyVM.LoadPalletIntoRightCell(warehousePallet);
                        
                        // Remove the pallet from the warehouse cell
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, 1, null);
                        
                        MessageBox.Show($"Loaded pallet {palletDisplay} from right outer cell to trolley");
                    }
                    else
                    {
                        // Create a reference to the current trolley pallet before replacing it
                        Pallet trolleyPallet = TrolleyVM.RightCell.Pallet;
                        string trolleyPalletDisplay = trolleyPallet.DisplayName;

                        MessageBox.Show($"Swapping: Pallet {trolleyPalletDisplay} moved to right outer cell and pallet {palletDisplay} moved to trolley");

                        // Swap the pallets (visually update both the trolley and warehouse cells)
                        var tempPallet = warehousePallet;

                        // Update warehouse cell with trolley's pallet
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, 1, trolleyPallet);

                        // Update trolley cell with warehouse pallet
                        TrolleyVM.LoadPalletIntoRightCell(tempPallet);
                    }
                }
                else
                {
                    MessageBox.Show($"Loading: Pallet {palletDisplay} from right outer cell to trolley");

                    // Load the pallet into the trolley cell
                    TrolleyVM.LoadPalletIntoRightCell(warehousePallet);

                    // Remove the pallet from the warehouse cell
                    UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, 1, null);
                }
            }
            else
            {
                MessageBox.Show("No pallet in the right outer cell to load");

                // Check inner cells as alternatives
                Pallet innerPallet = null;
                int cellIndex = 0;
                
                if (currentRow.RightCell2Pallet != null)
                {
                    innerPallet = currentRow.RightCell2Pallet;
                    cellIndex = 2;
                }
                else if (currentRow.RightCell3Pallet != null)
                {
                    innerPallet = currentRow.RightCell3Pallet;
                    cellIndex = 3;
                }
                else if (currentRow.RightCell4Pallet != null)
                {
                    innerPallet = currentRow.RightCell4Pallet;
                    cellIndex = 4;
                }
                
                if (innerPallet != null)
                {
                    var result = MessageBox.Show(
                        $"Would you like to use the pallet from cell {cellIndex} instead?",
                        "Use Inner Cell",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (TrolleyVM.RightCell.IsOccupied)
                        {
                            // If left cell is empty, move right cell pallet to left cell first
                            if (!TrolleyVM.LeftCell.IsOccupied)
                            {
                                // Move pallet from right to left cell
                                Pallet rightPallet = TrolleyVM.RightCell.Pallet;
                                TrolleyVM.RemovePalletFromRightCell();
                                TrolleyVM.LoadPalletIntoLeftCell(rightPallet);
                                MessageBox.Show($"Moved pallet {rightPallet.DisplayName} from right to left trolley cell");
                                
                                // Now load the inner pallet into the right cell
                                TrolleyVM.LoadPalletIntoRightCell(innerPallet);
                                
                                // Remove the pallet from the warehouse cell
                                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, cellIndex, null);
                                
                                MessageBox.Show($"Loaded pallet {innerPallet.DisplayName} from right cell {cellIndex} to trolley");
                            }
                            else
                            {
                                // Swap with trolley pallet
                                Pallet trolleyPallet = TrolleyVM.RightCell.Pallet;

                                // Update warehouse cell with trolley's pallet
                                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, cellIndex, trolleyPallet);
                                
                                TrolleyVM.LoadPalletIntoRightCell(innerPallet);

                                MessageBox.Show($"Swapped with inner cell {cellIndex} pallet: {innerPallet.DisplayName}");
                            }
                        }
                        else
                        {
                            // Just load from inner cell
                            TrolleyVM.LoadPalletIntoRightCell(innerPallet);
                            
                            // Remove the pallet from the warehouse cell
                            UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, cellIndex, null);

                            MessageBox.Show($"Loaded from inner cell {cellIndex}: {innerPallet.DisplayName}");
                        }
                        return;
                    }
                }
            }
        }

        // Method to check if left cell can be unloaded
        private bool CanUnloadLeftCell() => TrolleyVM?.LeftCell?.IsOccupied == true;
        
        // Method to check if right cell can be unloaded
        private bool CanUnloadRightCell() => TrolleyVM?.RightCell?.IsOccupied == true;
        
        // Method to unload the left cell - calls implementation in UnloadMethods.cs
        private void TestUnloadLeftCell() => UnloadLeftCellImplementation();
        
        // Method to unload the right cell - calls implementation in UnloadMethods.cs
        private void TestUnloadRightCell() => UnloadRightCellImplementation();
    }
}
