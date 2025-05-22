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
        // Returns true if unload was successful, false otherwise.
        private async System.Threading.Tasks.Task<bool> UnloadPalletToWarehouseAsync(bool unloadFromLeftTrolleyCell, bool targetIsLeftWarehouseCell)
        {
            Pallet palletToUnload;
            int? palletIdToUpdateTask = null; // This will be used by UpdateTaskStatus for storage tasks. Retrieval tasks are handled by MainViewModel.
            OpcInOutState opcUnloadState = OpcInOutState.Idle;

            if (unloadFromLeftTrolleyCell)
            {
                if (!TrolleyVM.LeftCell.IsOccupied)
                {
                    _logger.LogWarning("Attempted to unload from empty left trolley cell.");
                    MessageBox.Show("No pallet in left trolley cell to unload.");
                    return false;
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
                    return false;
                }
                palletToUnload = TrolleyVM.RightCell.Pallet; // Get pallet before attempting OPC
                opcUnloadState = OpcInOutState.OutFromRight;
            }

            if (palletToUnload == null)
            {
                _logger.LogError($"Pallet to unload is null or not found on the specified trolley side (From Left: {unloadFromLeftTrolleyCell}).");
                MessageBox.Show("Pallet not found on the specified trolley side for unloading.");
                return false;
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
                return false; // Stop if OPC command or initial logical removal fails
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
                return false;
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
                return false;
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
                return true; // Finger unload successful
            }

            // Regular Warehouse Cell Unload
            _logger.LogInformation($"Attempting to unload pallet {palletToUnload.DisplayName} to warehouse cell (Target Left: {targetIsLeftWarehouseCell}), invoking shift deeper logic.");
            bool shiftAndPlaceSuccess = await ShiftPalletsDeeperInWarehouseCellAsync(palletToUnload, currentRow, targetIsLeftWarehouseCell);
            
            if (shiftAndPlaceSuccess)
            {
                UpdateWarehouseCellPallet(currentRow.Position, targetIsLeftWarehouseCell, 1, palletToUnload); 
                MessageBox.Show($"Pallet {palletToUnload.DisplayName} successfully unloaded to {(targetIsLeftWarehouseCell ? "left" : "right")} warehouse cell (Order 0). Deeper pallets shifted if necessary.", "Unload Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                if (palletIdToUpdateTask.HasValue)
                {
                    UpdateTaskStatus(palletIdToUpdateTask); 
                }
            }
            else
            {
                _logger.LogError($"Failed to unload pallet {palletToUnload.DisplayName} to warehouse cell (Target Left: {targetIsLeftWarehouseCell}) because shifting or placement failed in DB.");
                MessageBox.Show($"Failed to unload pallet {palletToUnload.DisplayName} to the warehouse. The cell may be full or blocked. The pallet will be restored to the trolley.", "Unload To Warehouse Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Restore pallet to the trolley logically, as it was already removed
                if (opcUnloadState == OpcInOutState.OutFromLeft) // Indicates it was originally on the left
                {
                    await AddPalletToTrolleyLeftCellAsync(palletToUnload); 
                    _logger.LogInformation($"Restored pallet {palletToUnload.DisplayName} to left trolley cell due to warehouse unload failure.");
                }
                else if (opcUnloadState == OpcInOutState.OutFromRight) // Indicates it was originally on the right
                {
                    await AddPalletToTrolleyRightCellAsync(palletToUnload);
                    _logger.LogInformation($"Restored pallet {palletToUnload.DisplayName} to right trolley cell due to warehouse unload failure.");
                }
                // Note: Task status is NOT updated to completed if warehouse unload fails.
                // Set OPC back to Idle after logical operations are complete or warehouse unload attempt is finished
                try
                {
                    await _opcService.WriteRegisterAsync(OpcNodes.InOutRegister, (short)OpcInOutState.Idle);
                    _logger.LogInformation("OPC InOutRegister set back to Idle after warehouse cell unload operation (failure case).");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting OPC InOutRegister back to Idle after warehouse cell unload (failure case).");
                }
                return false; // Warehouse unload failed
            }

            // Set OPC back to Idle after logical operations are complete or warehouse unload attempt is finished
            try
            {
                await _opcService.WriteRegisterAsync(OpcNodes.InOutRegister, (short)OpcInOutState.Idle);
                _logger.LogInformation("OPC InOutRegister set back to Idle after warehouse cell unload operation (success case).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting OPC InOutRegister back to Idle after warehouse cell unload (success case).");
            }
            return true; // Warehouse unload successful
        }

        public async System.Threading.Tasks.Task<bool> UnloadPalletFromLeftCellAsync()
        {
            _logger.LogInformation("UnloadPalletFromLeftCellAsync called (public).");
            if (TrolleyVM.LeftCell.IsOccupied)
            {
                // Pallet is on the left trolley cell, unload to left warehouse.
                _logger.LogInformation("Left trolley cell has a pallet. Unloading from LEFT trolley cell to LEFT warehouse side.");
                return await UnloadPalletToWarehouseAsync(unloadFromLeftTrolleyCell: true, targetIsLeftWarehouseCell: true);
            }
            else if (TrolleyVM.RightCell.IsOccupied)
            {
                // Left trolley cell is empty, but right is occupied.
                // Unload the pallet from the RIGHT trolley cell to the LEFT warehouse side.
                _logger.LogInformation("Left trolley cell empty, right cell has a pallet. Unloading pallet from RIGHT trolley cell to LEFT warehouse side.");
                // Consider adding a user confirmation dialog here if this behavior needs to be optional or more explicit.
                // For now, proceeding directly as per the plan.
                return await UnloadPalletToWarehouseAsync(unloadFromLeftTrolleyCell: false, targetIsLeftWarehouseCell: true);
            }
            else
            {
                _logger.LogWarning("Both trolley cells are empty. Nothing to unload via left unload command.");
                MessageBox.Show("Both trolley cells are empty. Nothing to unload.", "Unload Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> UnloadPalletFromRightCellAsync()
        {
            _logger.LogInformation("UnloadPalletFromRightCellAsync called (public).");
            if (TrolleyVM.RightCell.IsOccupied)
            {
                // Pallet is on the right trolley cell, unload to right warehouse.
                _logger.LogInformation("Right trolley cell has a pallet. Unloading from RIGHT trolley cell to RIGHT warehouse side.");
                return await UnloadPalletToWarehouseAsync(unloadFromLeftTrolleyCell: false, targetIsLeftWarehouseCell: false);
            }
            else if (TrolleyVM.LeftCell.IsOccupied)
            {
                // Right trolley cell is empty, but left is occupied.
                // Unload the pallet from the LEFT trolley cell to the RIGHT warehouse side.
                _logger.LogInformation("Right trolley cell empty, left cell has a pallet. Unloading pallet from LEFT trolley cell to RIGHT warehouse side.");
                // Consider adding a user confirmation dialog here.
                return await UnloadPalletToWarehouseAsync(unloadFromLeftTrolleyCell: true, targetIsLeftWarehouseCell: false);
            }
            else
            {
                _logger.LogWarning("Both trolley cells are empty. Nothing to unload via right unload command.");
                MessageBox.Show("Both trolley cells are empty. Nothing to unload.", "Unload Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }

        // This method is called after a pallet unload to update the status of any associated task (storage or retrieval).
        private async void UpdateTaskStatus(int? palletId)
        {
            if (_mainViewModel == null || palletId == null) return;

            // Try to find a storage task associated with the pallet
            var storageTaskItem = _mainViewModel.PalletsReadyForStorage.FirstOrDefault(
                item => item.PalletDetails?.Id == palletId);

            if (storageTaskItem != null && storageTaskItem.StorageTask != null) // It's a storage task
            {
                storageTaskItem.StorageTask.ActiveTaskStatus = ActiveTaskStatus.finished;
                storageTaskItem.StorageTask.Status = Models.Enums.TaskStatus.Completed;
                storageTaskItem.StorageTask.ExecutedDateTime = DateTime.Now;
                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.PalletsReadyForStorage));

                if (_mainViewModel.TaskVM != null)
                {
                    await _mainViewModel.TaskVM.SaveTaskToDatabase(storageTaskItem.StorageTask);
                }
                _logger.LogInformation("Storage task for pallet {PalletId} has been completed via UpdateTaskStatus.", palletId);
                MessageBox.Show($"Storage task for pallet {palletId} has been completed.",
                               "Task Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                var timer = new System.Timers.Timer(5000);
                timer.Elapsed += (sender, e) =>
                {
                    timer.Stop();
                    _mainViewModel._dispatcherService.Invoke(() =>
                    {
                        _mainViewModel.PalletsReadyForStorage.Remove(storageTaskItem);
                        _mainViewModel.NotifyStorageItemsChanged();
                    });
                };
                timer.AutoReset = false;
                timer.Start();
            }
            else // If not a storage task, check if it's a retrieval task
            {
                var retrievalTaskItem = _mainViewModel.PalletsReadyForDelivery.FirstOrDefault(
                    item => item.PalletDetails?.Id == palletId && item.RetrievalTask != null);

                if (retrievalTaskItem != null) // It's a retrieval task
                {
                    _logger.LogInformation("Completing retrieval task for pallet {PalletId} via UpdateTaskStatus in TrolleyOperationsVM.", palletId);
                    retrievalTaskItem.RetrievalTask.ActiveTaskStatus = ActiveTaskStatus.finished;
                    retrievalTaskItem.RetrievalTask.Status = Models.Enums.TaskStatus.Completed;
                    retrievalTaskItem.RetrievalTask.ExecutedDateTime = DateTime.Now;
                    
                    if (_mainViewModel.TaskVM != null)
                    {
                        await _mainViewModel.TaskVM.SaveTaskToDatabase(retrievalTaskItem.RetrievalTask);
                        // Ensure MainViewModel's view of tasks is updated.
                        // RefreshTaskStatus might involve more than just UI, could reload or re-evaluate task lists.
                        _mainViewModel.TaskVM.RefreshTaskStatus(retrievalTaskItem.RetrievalTask); 
                    }
                    
                    MessageBox.Show($"Retrieval task for pallet {palletId} has been completed.",
                                   "Task Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                    var retrievalTimer = new System.Timers.Timer(5000);
                    retrievalTimer.Elapsed += (sender, e) =>
                    {
                        retrievalTimer.Stop();
                        _mainViewModel._dispatcherService.Invoke(() =>
                        {
                            if (_mainViewModel.PalletsReadyForDelivery.Contains(retrievalTaskItem))
                            {
                                _mainViewModel.PalletsReadyForDelivery.Remove(retrievalTaskItem);
                                _logger.LogInformation("Delayed removal of completed Retrieval Task ID {TaskId} from PalletsReadyForDelivery via UpdateTaskStatus.", retrievalTaskItem.RetrievalTask.Id);
                                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.HasPalletsReadyForDelivery));
                                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.ShouldShowTasksPanel));
                                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.ShouldShowDefaultPhoto));
                                _mainViewModel.OnPropertyChanged(nameof(_mainViewModel.IsDefaultTaskViewActive));
                            }
                        });
                    };
                    retrievalTimer.AutoReset = false;
                    retrievalTimer.Start();
                }
                else
                {
                    _logger.LogInformation("UpdateTaskStatus called for pallet {PalletId}, but no associated active storage or retrieval task found.", palletId);
                }
            }
        }
    }
}
