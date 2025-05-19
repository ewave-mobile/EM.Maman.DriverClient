using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;
using System.Windows;
using EM.Maman.Models.Enums;
using Microsoft.Extensions.Logging;
using EM.Maman.Common.Enums; // Added for OpcInOutState
using EM.Maman.Common.Constants; // Added for OpcNodes
using System; // Added for Exception
using System.Linq; // Added for FirstOrDefault

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
            OpcInOutState opcUnloadState = OpcInOutState.Idle;

            if (unloadFromLeftTrolleyCell)
            {
                if (!TrolleyVM.LeftCell.IsOccupied)
                {
                    _logger.LogWarning("Attempted to unload from empty left trolley cell.");
                    MessageBox.Show("No pallet in left trolley cell to unload.");
                    return;
                }
                palletToUnload = TrolleyVM.LeftCell.Pallet; // Get pallet before attempting OPC
                opcUnloadState = OpcInOutState.OutFromLeft;
            }
            else // Unload from right trolley cell
            {
                if (!TrolleyVM.RightCell.IsOccupied)
                {
                     _logger.LogWarning("Attempted to unload from empty right trolley cell.");
                    MessageBox.Show("No pallet in right trolley cell to unload.");
                    return;
                }
                palletToUnload = TrolleyVM.RightCell.Pallet; // Get pallet before attempting OPC
                opcUnloadState = OpcInOutState.OutFromRight;
            }

            if (palletToUnload == null)
            {
                _logger.LogError($"Pallet to unload is null or not found on the specified trolley side (From Left: {unloadFromLeftTrolleyCell}).");
                MessageBox.Show("Pallet not found on the specified trolley side for unloading.");
                return;
            }
            palletIdToUpdateTask = palletToUnload.Id;

            // OPC Command to initiate physical unload
            try
            {
                _logger.LogInformation("Writing OPC command to unload: {State} ({Value}) to Node: {NodeId}", opcUnloadState, (short)opcUnloadState, OpcNodes.InOutRegister);
                await _opcService.WriteRegisterAsync(OpcNodes.InOutRegister, (short)opcUnloadState);
                // TODO: Add delay or check for OPC confirmation if available/needed
                // For now, we assume the command is sent and proceed with logical unload.
                
                // Perform logical unload from trolley AFTER successful OPC command dispatch
                if (unloadFromLeftTrolleyCell)
                {
                    await RemovePalletFromTrolleyLeftCellAsync(); 
                }
                else
                {
                    await RemovePalletFromTrolleyRightCellAsync(); 
                }
                _logger.LogInformation($"Pallet {palletToUnload.DisplayName} logically removed from trolley (From Left: {unloadFromLeftTrolleyCell}). Preparing to unload to warehouse (Target Left: {targetIsLeftWarehouseCell}).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OPC unload command or logical removal for {State}.", opcUnloadState);
                MessageBox.Show($"Error during physical unload or internal update: {ex.Message}", "Unload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Consider if pallet should be re-added to trolley VM if OPC succeeded but logical removal failed.
                return; // Stop if OPC command or initial logical removal fails
            }

            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                _logger.LogError("Trolley is not at a valid position for unload.");
                MessageBox.Show("Trolley is not at a valid position.");
                // Potentially re-add palletToUnload to trolley if critical? Or handle error more gracefully.
                 // Re-add pallet to trolley if subsequent logic fails before DB update
                if (opcUnloadState == OpcInOutState.OutFromLeft) await AddPalletToTrolleyLeftCellAsync(palletToUnload);
                else if (opcUnloadState == OpcInOutState.OutFromRight) await AddPalletToTrolleyRightCellAsync(palletToUnload);
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
                if (opcUnloadState == OpcInOutState.OutFromLeft) await AddPalletToTrolleyLeftCellAsync(palletToUnload);
                else if (opcUnloadState == OpcInOutState.OutFromRight) await AddPalletToTrolleyRightCellAsync(palletToUnload);
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
                }
            }

            if (fingerUnloadPerformed)
            {
                if (palletIdToUpdateTask.HasValue) UpdateTaskStatus(palletIdToUpdateTask);
                 // Set OPC back to Idle after finger unload operation.
                try
                {
                    await _opcService.WriteRegisterAsync(OpcNodes.InOutRegister, (short)OpcInOutState.Idle);
                    _logger.LogInformation("OPC InOutRegister set back to Idle after finger unload operation.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting OPC InOutRegister back to Idle after finger unload.");
                }
                return;
            }

            // Regular Warehouse Cell Unload
            _logger.LogInformation($"Attempting to unload pallet {palletToUnload.DisplayName} to warehouse cell (Target Left: {targetIsLeftWarehouseCell}), invoking shift deeper logic.");
            await ShiftPalletsDeeperInWarehouseCellAsync(palletToUnload, currentRow, targetIsLeftWarehouseCell);
            
            UpdateWarehouseCellPallet(currentRow.Position, targetIsLeftWarehouseCell, 1, palletToUnload); 
            MessageBox.Show($"Pallet {palletToUnload.DisplayName} logically unloaded to {(targetIsLeftWarehouseCell ? "left" : "right")} warehouse cell (Order 0). Deeper pallets shifted if necessary.");

            if (palletIdToUpdateTask.HasValue)
            {
                UpdateTaskStatus(palletIdToUpdateTask); 
            }

            // Set OPC back to Idle after logical operations are complete
            try
            {
                await _opcService.WriteRegisterAsync(OpcNodes.InOutRegister, (short)OpcInOutState.Idle);
                _logger.LogInformation("OPC InOutRegister set back to Idle after warehouse cell unload operation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting OPC InOutRegister back to Idle after warehouse cell unload.");
            }
        }

        public async System.Threading.Tasks.Task UnloadPalletFromLeftCellAsync()
        {
            _logger.LogInformation("UnloadPalletFromLeftCellAsync called (public).");
            if (TrolleyVM.LeftCell.IsOccupied)
            {
                await UnloadPalletToWarehouseAsync(true, true); 
            }
            else if (TrolleyVM.RightCell.IsOccupied) 
            {
                _logger.LogInformation("Left trolley cell empty, attempting to unload from Right cell to LEFT warehouse side (if that's the intent). This might need review based on operational flow.");
                 MessageBox.Show("Left trolley cell is empty. If a pallet is on the right and needs unloading, use the appropriate unload command for the right side or ensure the task specifies the correct unload operation.", "Unload Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _logger.LogWarning("Left trolley cell is empty. Nothing to unload.");
                MessageBox.Show("Left trolley cell is empty. Nothing to unload.");
            }
        }

        public async System.Threading.Tasks.Task UnloadPalletFromRightCellAsync()
        {
            _logger.LogInformation("UnloadPalletFromRightCellAsync called (public).");
            if (TrolleyVM.RightCell.IsOccupied)
            {
                await UnloadPalletToWarehouseAsync(false, false);
            }
            else if (TrolleyVM.LeftCell.IsOccupied)
            {
                 _logger.LogInformation("Right trolley cell empty, attempting to unload from Left cell to RIGHT warehouse side (if that's the intent). This might need review.");
                 MessageBox.Show("Right trolley cell is empty. If a pallet is on the left and needs unloading, use the appropriate unload command for the left side or ensure the task specifies the correct unload operation.", "Unload Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _logger.LogWarning("Right trolley cell is empty. Nothing to unload.");
                MessageBox.Show("Right trolley cell is empty. Nothing to unload.");
            }
        }

        private async void UpdateTaskStatus(int? palletId) 
        {
            if (_mainViewModel == null || palletId == null ) return;

            var taskItem = _mainViewModel.PalletsReadyForStorage.FirstOrDefault(
                item => item.PalletDetails?.Id == palletId);
            
            // Also check PalletsReadyForDelivery for retrieval tasks
            PalletRetrievalTaskItem retrievalTaskItem = null;
            if (taskItem == null)
            {
                retrievalTaskItem = _mainViewModel.PalletsReadyForDelivery.FirstOrDefault(
                    item => item.PalletDetails?.Id == palletId);
            }

            if (taskItem != null && taskItem.StorageTask != null) // It's a storage task
            {
                taskItem.StorageTask.ActiveTaskStatus = ActiveTaskStatus.finished;
                taskItem.StorageTask.Status = Models.Enums.TaskStatus.Completed; 
                taskItem.StorageTask.ExecutedDateTime = DateTime.Now; 
                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.PalletsReadyForStorage));

                if (_mainViewModel.TaskVM != null)
                {
                    await _mainViewModel.TaskVM.SaveTaskToDatabase(taskItem.StorageTask);
                }
                MessageBox.Show($"Storage task for pallet {palletId} has been completed.",
                               "Task Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                var timer = new System.Timers.Timer(5000); 
                timer.Elapsed += (sender, e) =>
                {
                    timer.Stop();
                    _mainViewModel._dispatcherService.Invoke(() =>
                    {
                        _mainViewModel.PalletsReadyForStorage.Remove(taskItem);
                        _mainViewModel.NotifyStorageItemsChanged();
                    });
                };
                timer.AutoReset = false; 
                timer.Start();
            }
            else if (retrievalTaskItem != null && retrievalTaskItem.RetrievalTask != null) // It's a retrieval task
            {
                // Note: The status update to Completed/finished for retrieval tasks
                // is already handled in MainViewModel.ExecuteUnloadAtDestination.
                // This UpdateTaskStatus method was originally for storage.
                // If specific post-unload actions for retrieval are needed here, they can be added.
                // For now, the main completion logic for retrieval is in ExecuteUnloadAtDestination.
                 _logger.LogInformation("Retrieval task for pallet {PalletId} already marked completed by ExecuteUnloadAtDestination.", palletId);
            }
        }
    }
}
