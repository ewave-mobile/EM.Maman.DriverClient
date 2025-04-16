using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;
using System.Windows;

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing the unload methods for TrolleyOperationsViewModel
    public partial class TrolleyOperationsViewModel
    {
        // Implementation of unload left cell functionality
        private void UnloadLeftCellImplementation()
        {
            // Check if trolley left cell has a pallet
            if (!TrolleyVM.LeftCell.IsOccupied)
            {
                MessageBox.Show("No pallet in left trolley cell to unload");
                return;
            }

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

            // Special handling for lowest level (level 1) - unload to finger instead of cell
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            
            if (isLowestLevel && currentRow.LeftFinger != null)
            {
                // Get the trolley pallet info before removing it
                Pallet palletFromTrolley = TrolleyVM.LeftCell.Pallet;
                string palletDisplayName = palletFromTrolley.DisplayName;
                
                // Remove the pallet from the trolley
                TrolleyVM.RemovePalletFromLeftCell();
                
                // Increase finger pallet count
                currentRow.LeftFingerPalletCount++;
                
                MessageBox.Show($"Unloaded pallet {palletDisplayName} to left finger. Finger pallet count: {currentRow.LeftFingerPalletCount}");
                return;
            }

            // Regular cell handling for non-lowest levels
            // Check if the warehouse outer cell has a pallet
            bool outerCellHasPallet = currentRow.LeftCell1Pallet != null;

            if (outerCellHasPallet)
            {
                // Get the warehouse pallet info
                Pallet warehousePallet = currentRow.LeftCell1Pallet;
                string warehousePalletDisplay = warehousePallet.DisplayName;

                // Get the trolley pallet info
                Pallet palletFromTrolley = TrolleyVM.LeftCell.Pallet;
                string trolleyPalletDisplay = palletFromTrolley.DisplayName;

                // In a real system, you'd swap the pallets
                MessageBox.Show($"Swapping: Pallet {trolleyPalletDisplay} moved to left outer cell and pallet {warehousePalletDisplay} moved to trolley");

                // Swap the pallets
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, 1, palletFromTrolley);
                TrolleyVM.LoadPalletIntoLeftCell(warehousePallet);
            }
            else
            {
                // Check inner cells if outer is empty
                bool innerCellHasPallet = false;
                int occupiedCellIndex = 0;
                Pallet innerPallet = null;
                
                if (currentRow.LeftCell2Pallet != null)
                {
                    innerCellHasPallet = true;
                    occupiedCellIndex = 2;
                    innerPallet = currentRow.LeftCell2Pallet;
                }
                else if (currentRow.LeftCell3Pallet != null)
                {
                    innerCellHasPallet = true;
                    occupiedCellIndex = 3;
                    innerPallet = currentRow.LeftCell3Pallet;
                }
                else if (currentRow.LeftCell4Pallet != null)
                {
                    innerCellHasPallet = true;
                    occupiedCellIndex = 4;
                    innerPallet = currentRow.LeftCell4Pallet;
                }

                if (innerCellHasPallet)
                {
                    // Offer to swap with inner cell pallet
                    var result = MessageBox.Show(
                        $"Outer cell is empty but cell {occupiedCellIndex} has a pallet. Would you like to swap with inner cell?",
                        "Swap with Inner Cell",
                        MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Get the trolley pallet info
                        Pallet palletFromTrolleyInner = TrolleyVM.LeftCell.Pallet;
                        string trolleyPalletDisplayInner = palletFromTrolleyInner.DisplayName;

                        // Swap with inner cell pallet
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, occupiedCellIndex, palletFromTrolleyInner);
                        TrolleyVM.LoadPalletIntoLeftCell(innerPallet);

                        MessageBox.Show($"Swapped with inner cell {occupiedCellIndex} pallet: {innerPallet.DisplayName}");
                        return;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return; // Cancel the operation
                    }
                    // If No, continue to outer cell placement
                }

                // Get the trolley pallet info before removing it
                Pallet palletFromTrolleyOuter = TrolleyVM.LeftCell.Pallet;
                string palletDisplayNameOuter = palletFromTrolleyOuter.DisplayName;

                // In a real system, you'd move the pallet to the outer cell
                MessageBox.Show($"Unloading: Pallet {palletDisplayNameOuter} moved to left outer cell");

                // Update the warehouse cell with the trolley pallet
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, 1, palletFromTrolleyOuter);

                // Remove the pallet from the trolley
                TrolleyVM.RemovePalletFromLeftCell();
            }
        }

        // Implementation of unload right cell functionality
        private void UnloadRightCellImplementation()
        {
            // Check if trolley right cell has a pallet
            if (!TrolleyVM.RightCell.IsOccupied)
            {
                MessageBox.Show("No pallet in right trolley cell to unload");
                return;
            }

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

            // Special handling for lowest level (level 1) - unload to finger instead of cell
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            
            if (isLowestLevel && currentRow.RightFinger != null)
            {
                // Increase finger pallet count
                currentRow.RightFingerPalletCount++;
                
                // Get the trolley pallet info before removing it
                Pallet palletFromTrolley = TrolleyVM.RightCell.Pallet;
                string palletDisplayName = palletFromTrolley.DisplayName;
                
                // Remove the pallet from the trolley
                TrolleyVM.RemovePalletFromRightCell();
                
                MessageBox.Show($"Unloaded pallet {palletDisplayName} to right finger. Finger pallet count: {currentRow.RightFingerPalletCount}");
                return;
            }

            // Regular cell handling for non-lowest levels
            // Check if the warehouse outer cell has a pallet
            bool outerCellHasPallet = currentRow.RightCell1Pallet != null;

            if (outerCellHasPallet)
            {
                // Get the warehouse pallet info
                Pallet warehousePallet = currentRow.RightCell1Pallet;
                string warehousePalletDisplay = warehousePallet.DisplayName;

                // Get the trolley pallet info
                Pallet palletFromTrolley = TrolleyVM.RightCell.Pallet;
                string trolleyPalletDisplay = palletFromTrolley.DisplayName;

                // In a real system, you'd swap the pallets
                MessageBox.Show($"Swapping: Pallet {trolleyPalletDisplay} moved to right outer cell and pallet {warehousePalletDisplay} moved to trolley");

                // Swap the pallets
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, 1, palletFromTrolley);
                TrolleyVM.LoadPalletIntoRightCell(warehousePallet);
            }
            else
            {
                // Check inner cells if outer is empty
                bool innerCellHasPallet = false;
                int occupiedCellIndex = 0;
                Pallet innerPallet = null;
                
                if (currentRow.RightCell2Pallet != null)
                {
                    innerCellHasPallet = true;
                    occupiedCellIndex = 2;
                    innerPallet = currentRow.RightCell2Pallet;
                }
                else if (currentRow.RightCell3Pallet != null)
                {
                    innerCellHasPallet = true;
                    occupiedCellIndex = 3;
                    innerPallet = currentRow.RightCell3Pallet;
                }
                else if (currentRow.RightCell4Pallet != null)
                {
                    innerCellHasPallet = true;
                    occupiedCellIndex = 4;
                    innerPallet = currentRow.RightCell4Pallet;
                }

                if (innerCellHasPallet)
                {
                    // Offer to swap with inner cell pallet
                    var result = MessageBox.Show(
                        $"Outer cell is empty but cell {occupiedCellIndex} has a pallet. Would you like to swap with inner cell?",
                        "Swap with Inner Cell",
                        MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Get the trolley pallet info
                        Pallet palletFromTrolleyInner = TrolleyVM.RightCell.Pallet;
                        string trolleyPalletDisplayInner = palletFromTrolleyInner.DisplayName;

                        // Swap with inner cell pallet
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, occupiedCellIndex, palletFromTrolleyInner);
                        TrolleyVM.LoadPalletIntoRightCell(innerPallet);

                        MessageBox.Show($"Swapped with inner cell {occupiedCellIndex} pallet: {innerPallet.DisplayName}");
                        return;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return; // Cancel the operation
                    }
                    // If No, continue to outer cell placement
                }

                // Get the trolley pallet info before removing it
                Pallet palletFromTrolleyOuter = TrolleyVM.RightCell.Pallet;
                string palletDisplayNameOuter = palletFromTrolleyOuter.DisplayName;

                // In a real system, you'd move the pallet to the outer cell
                MessageBox.Show($"Unloading: Pallet {palletDisplayNameOuter} moved to right outer cell");

                // Update the warehouse cell with the trolley pallet
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, 1, palletFromTrolleyOuter);

                // Remove the pallet from the trolley
                TrolleyVM.RemovePalletFromRightCell();
            }
        }
    }
}
