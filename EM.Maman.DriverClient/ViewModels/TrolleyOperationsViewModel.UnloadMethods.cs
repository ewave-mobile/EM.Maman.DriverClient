using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;
using System.Windows;
using EM.Maman.Models.Enums;
using Microsoft.Extensions.Logging;

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing the unload methods for TrolleyOperationsViewModel
    public partial class TrolleyOperationsViewModel
    {
        // Generic unload implementation, can be called for left or right.
        // targetIsLeftWarehouseCell indicates if the pallet should go to the left warehouse side.
        private async System.Threading.Tasks.Task UnloadPalletToWarehouseAsync(bool unloadFromLeftTrolleyCell, bool targetIsLeftWarehouseCell)
        {
            Pallet palletToUnload;
            int? palletIdToUpdateTask = null;

            if (unloadFromLeftTrolleyCell)
            {
                if (!TrolleyVM.LeftCell.IsOccupied)
                {
                    _logger.LogWarning("Attempted to unload from empty left trolley cell.");
                    MessageBox.Show("No pallet in left trolley cell to unload.");
                    return;
                }
                palletToUnload = await RemovePalletFromTrolleyLeftCellAsync();
            }
            else // Unload from right trolley cell
            {
                if (!TrolleyVM.RightCell.IsOccupied)
                {
                     _logger.LogWarning("Attempted to unload from empty right trolley cell.");
                    MessageBox.Show("No pallet in right trolley cell to unload.");
                    return;
                }
                palletToUnload = await RemovePalletFromTrolleyRightCellAsync();
            }

            if (palletToUnload == null)
            {
                _logger.LogError($"Failed to retrieve pallet from trolley cell during unload (From Left: {unloadFromLeftTrolleyCell}).");
                MessageBox.Show("Failed to retrieve pallet from trolley cell during unload.");
                return;
            }
            palletIdToUpdateTask = palletToUnload.Id;
            _logger.LogInformation($"Pallet {palletToUnload.DisplayName} removed from trolley (From Left: {unloadFromLeftTrolleyCell}). Preparing to unload to warehouse (Target Left: {targetIsLeftWarehouseCell}).");


            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                _logger.LogError("Trolley is not at a valid position for unload.");
                MessageBox.Show("Trolley is not at a valid position.");
                // Potentially re-add palletToUnload to trolley if critical? Or handle error more gracefully.
                return;
            }

            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                _logger.LogWarning($"Operation cancelled: Trolley level mismatch. Current: {TrolleyVM.CurrentLevelNumber}, Selected: {TrolleyVM.SelectedLevelNumber}.");
                MessageBox.Show($"Operation cancelled: Trolley is at Level {TrolleyVM.CurrentLevelNumber} but you are viewing Level {TrolleyVM.SelectedLevelNumber}. " +
                                $"The view will now switch to Level {TrolleyVM.CurrentLevelNumber}. Please try the operation again if you wish.",
                                "Level Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                TrolleyVM.SelectedLevelNumber = TrolleyVM.CurrentLevelNumber; // Switch view to current level
                // Re-add pallet to trolley as the operation is cancelled before unload.
                if (unloadFromLeftTrolleyCell) await AddPalletToTrolleyLeftCellAsync(palletToUnload); else await AddPalletToTrolleyRightCellAsync(palletToUnload);
                return;
            }
            
            // Finger Unload (Lowest Level)
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            bool fingerUnloadPerformed = false;

            if (isLowestLevel)
            {
                bool fingerExistsAtLocation = (targetIsLeftWarehouseCell && currentRow.LeftFinger != null) || 
                                              (!targetIsLeftWarehouseCell && currentRow.RightFinger != null);

                if (fingerExistsAtLocation)
                {
                    if (targetIsLeftWarehouseCell && currentRow.LeftFinger != null)
                    {
                        currentRow.LeftFingerPalletCount++;
                        _logger.LogInformation($"Unloaded pallet {palletToUnload.DisplayName} to left finger. Count: {currentRow.LeftFingerPalletCount}.");
                        MessageBox.Show($"Unloaded pallet {palletToUnload.DisplayName} to left finger. Finger pallet count: {currentRow.LeftFingerPalletCount}");
                        fingerUnloadPerformed = true;
                    }
                    else if (!targetIsLeftWarehouseCell && currentRow.RightFinger != null)
                    {
                        currentRow.RightFingerPalletCount++;
                        _logger.LogInformation($"Unloaded pallet {palletToUnload.DisplayName} to right finger. Count: {currentRow.RightFingerPalletCount}.");
                        MessageBox.Show($"Unloaded pallet {palletToUnload.DisplayName} to right finger. Finger pallet count: {currentRow.RightFingerPalletCount}");
                        fingerUnloadPerformed = true;
                    }
                    // If finger exists on the side but not at this specific currentRow, it will fall through to cell unload.
                }
                // If no finger at the specific target location on the lowest level, proceed to cell unload.
            }

            if (fingerUnloadPerformed)
            {
                if (palletIdToUpdateTask.HasValue) UpdateTaskStatus(palletIdToUpdateTask);
                return;
            }

            // Regular Warehouse Cell Unload (Non-Lowest Levels OR Lowest Level without a finger at target position)
            // This is where the "push-in" logic happens.
            // The ShiftPalletsDeeperInWarehouseCellAsync will handle finding Order 0 cell,
            // shifting existing pallets, and adding the newPalletToStore to Order 0.
            _logger.LogInformation($"Attempting to unload pallet {palletToUnload.DisplayName} to warehouse cell (Target Left: {targetIsLeftWarehouseCell}), invoking shift deeper logic.");
            await ShiftPalletsDeeperInWarehouseCellAsync(palletToUnload, currentRow, targetIsLeftWarehouseCell);
            
            // Update UI for the cell at Order 0 (cellIndex 1)
            // A more comprehensive UI update would query all pallets in the physical cell location and update LeftCell1Pallet, LeftCell2Pallet etc.
            UpdateWarehouseCellPallet(currentRow.Position, targetIsLeftWarehouseCell, 1, palletToUnload); 
            MessageBox.Show($"Unloaded pallet {palletToUnload.DisplayName} to {(targetIsLeftWarehouseCell ? "left" : "right")} warehouse cell (Order 0). Deeper pallets shifted if necessary.");

            if (palletIdToUpdateTask.HasValue)
            {
                UpdateTaskStatus(palletIdToUpdateTask);
            }
        }


        // Implementation of unload left cell functionality
        private async System.Threading.Tasks.Task UnloadLeftCellImplementationAsync()
        {
            _logger.LogInformation("UnloadLeftCellImplementationAsync called.");
            if (TrolleyVM.LeftCell.IsOccupied)
            {
                await UnloadPalletToWarehouseAsync(true, true); // Unload from Left Trolley to Left Warehouse
            }
            else if (TrolleyVM.RightCell.IsOccupied) // Left is empty, but right has a pallet
            {
                _logger.LogInformation("Left trolley cell empty, right cell has pallet. Unloading right pallet to LEFT warehouse side.");
                MessageBox.Show("Left trolley cell is empty. Unloading pallet from right trolley cell to the left warehouse side.");
                await UnloadPalletToWarehouseAsync(false, true); // Unload from Right Trolley to Left Warehouse
            }
            else
            {
                _logger.LogWarning("Both trolley cells are empty. Nothing to unload.");
                MessageBox.Show("Both trolley cells are empty. Nothing to unload.");
            }
        }

        // Implementation of unload right cell functionality
        private async System.Threading.Tasks.Task UnloadRightCellImplementationAsync()
        {
            _logger.LogInformation("UnloadRightCellImplementationAsync called.");
            if (TrolleyVM.RightCell.IsOccupied)
            {
                await UnloadPalletToWarehouseAsync(false, false); // Unload from Right Trolley to Right Warehouse
            }
            else if (TrolleyVM.LeftCell.IsOccupied) // Right is empty, but left has a pallet
            {
                _logger.LogInformation("Right trolley cell empty, left cell has pallet. Unloading left pallet to RIGHT warehouse side.");
                MessageBox.Show("Right trolley cell is empty. Unloading pallet from left trolley cell to the right warehouse side.");
                await UnloadPalletToWarehouseAsync(true, false); // Unload from Left Trolley to Right Warehouse
            }
            else
            {
                _logger.LogWarning("Both trolley cells are empty. Nothing to unload.");
                MessageBox.Show("Both trolley cells are empty. Nothing to unload.");
            }
        }

        private async void UpdateTaskStatus(int? palletId) // Made async due to SaveTaskToDatabase call
        {
            if (_mainViewModel == null || palletId == null ) return;

            // Find the task for this pallet in the PalletsReadyForStorage collection
            var taskItem = _mainViewModel.PalletsReadyForStorage.FirstOrDefault(
                item => item.PalletDetails?.Id == palletId);

            if (taskItem != null)
            {
                // Update the task status to finished
                taskItem.StorageTask.ActiveTaskStatus = ActiveTaskStatus.finished;
                taskItem.StorageTask.Status = Models.Enums.TaskStatus.Completed; // Explicitly set to Completed
                taskItem.StorageTask.ExecutedDateTime = DateTime.Now; // Set execution time
                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.PalletsReadyForStorage));

                // Persist the ActiveTaskStatus change
                if (_mainViewModel.TaskVM != null && taskItem.StorageTask != null)
                {
                    // SaveTaskToDatabase will now save the .Status and .ExecutedDateTime as well
                    await _mainViewModel.TaskVM.SaveTaskToDatabase(taskItem.StorageTask);
                }

                // Show a message to confirm the task is complete
                MessageBox.Show($"Storage task for pallet {palletId} has been completed.",
                               "Task Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                // Set up a timer to remove the task after 5 seconds
                var timer = new System.Timers.Timer(5000); // 5 seconds
                timer.Elapsed += (sender, e) =>
                {
                    timer.Stop();
                    _mainViewModel._dispatcherService.Invoke(() =>
                    {
                        // Remove the task from UI collection
                        _mainViewModel.PalletsReadyForStorage.Remove(taskItem);
                        _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.HasPalletsReadyForStorage));
                    });
                };
                timer.AutoReset = false; // Only fire once
                timer.Start();
            }
        }
    }
}
