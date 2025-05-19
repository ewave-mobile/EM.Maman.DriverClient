using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EM.Maman.Common.Constants;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.Enums;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel; // Added for ObservableCollection
using DbTask = EM.Maman.Models.LocalDbModels.Task;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        // New collection for pallets on trolley, authenticated for retrieval, ready for delivery to finger/cell
        public ObservableCollection<PalletRetrievalTaskItem> PalletsReadyForDelivery { get; } = new ObservableCollection<PalletRetrievalTaskItem>();

        #region Task Operations

        private async System.Threading.Tasks.Task ProcessAuthenticatedRetrievalAsync(PalletAuthenticationItem authenticatedItem)
        {
            if (authenticatedItem?.PalletDetails == null || authenticatedItem.OriginalTask == null)
            {
                _logger.LogError("Cannot process authenticated retrieval: Invalid authenticated item data.");
                return;
            }

            if (authenticatedItem.OriginalTask.TaskType != Models.Enums.TaskType.Retrieval)
            {
                _logger.LogWarning("ProcessAuthenticatedRetrievalAsync called for a non-retrieval task. TaskType: {TaskType}", authenticatedItem.OriginalTask.TaskType);
                return;
            }

            try
            {
                _logger.LogInformation("Processing authenticated retrieval for Pallet ULD: {UldCode}, Task ID: {TaskId}",
                    authenticatedItem.PalletDetails.UldCode, authenticatedItem.OriginalTask.Id);

                var retrievalTaskDetails = authenticatedItem.OriginalTask;

                // Update task status
                retrievalTaskDetails.Status = Models.Enums.TaskStatus.InProgress;
                retrievalTaskDetails.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit; // Now in transit to destination finger/cell

                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var dbTask = retrievalTaskDetails.ToDbModel();
                    unitOfWork.Tasks.Update(dbTask);
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Successfully updated retrieval task status (ID: {TaskId}) to InProgress/Transit.", retrievalTaskDetails.Id);
                }

                // Create a retrieval task item for the UI (pallets on trolley, ready for delivery)
                // This item will have commands to go to destination finger/cell and unload.
                var retrievalDeliveryItem = new PalletRetrievalTaskItem(authenticatedItem.PalletDetails, retrievalTaskDetails)
                {
                    GoToRetrievalCommand = this.GoToRetrievalDestinationCommand, // Renaming for clarity in PalletRetrievalTaskItem if it's reused
                    // Or, if PalletRetrievalTaskItem has specific GoToDestinationFinger/Cell commands:
                    // GoToDestinationFingerCommand = this.GoToRetrievalDestinationCommand, // if destination is finger
                    // GoToDestinationCellCommand = this.GoToRetrievalDestinationCommand, // if destination is cell
                    // UnloadCommand = this.UnloadAtDestinationCommand // Assuming a generic UnloadCommand on the item
                };
                // For PalletRetrievalTaskItem, it already has GoToRetrievalCommand.
                // We need to ensure it's clear this command now points to GoToRetrievalDestination.
                // Let's assume PalletRetrievalTaskItem will be used for items on the trolley ready for delivery.
                // It might be better to have distinct item view models or more explicit command properties on PalletRetrievalTaskItem.
                // For now, re-purposing GoToRetrievalCommand to mean "Go To Next Destination for this Retrieval Item"
                // and adding a new command for unload.
                // A cleaner approach might be to add specific command properties to PalletRetrievalTaskItem:
                // e.g., NavigateToDestinationCommand, PerformUnloadCommand.

                // Assigning the new commands:
                retrievalDeliveryItem.GoToRetrievalCommand = this.GoToRetrievalDestinationCommand; // This command on item will now navigate to its dest.
                // We need an Unload command on PalletRetrievalTaskItem. 
                retrievalDeliveryItem.UnloadCommand = this.UnloadAtDestinationCommand;

                _dispatcherService.Invoke(() =>
                {
                    // Remove from any list that showed it as "pending at source cell" if applicable
                    // For example, if PalletsForRetrieval held items before authentication at source cell:
                    var itemToRemove = PalletsForRetrieval.FirstOrDefault(p => p.RetrievalTask.Id == retrievalTaskDetails.Id);
                    if (itemToRemove != null)
                    {
                        PalletsForRetrieval.Remove(itemToRemove);
                        _logger.LogInformation("Removed PalletRetrievalTaskItem for Task ID {TaskId} from PalletsForRetrieval.", retrievalTaskDetails.Id);
                    }

                    PalletsReadyForDelivery.Add(retrievalDeliveryItem);
                    // Notify relevant UI elements if needed, e.g., OnPropertyChanged(nameof(HasPalletsReadyForDelivery));
                    _logger.LogInformation("Added PalletRetrievalTaskItem for Task ID {TaskId} to PalletsReadyForDelivery.", retrievalTaskDetails.Id);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing authenticated retrieval for Pallet ULD: {UldCode}, Task ID: {TaskId}",
                    authenticatedItem.PalletDetails?.UldCode ?? "N/A", authenticatedItem.OriginalTask?.Id.ToString() ?? "N/A");
                MessageBox.Show($"Error processing authenticated retrieval: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task CreateStorageTaskFromAuthenticationAsync(PalletAuthenticationItem authenticatedItem)
        {
            if (authenticatedItem?.PalletDetails == null || authenticatedItem.OriginalTask == null)
            {
                _logger.LogError("Cannot update task status: Invalid authenticated item data.");
                return;
            }

            try
            {
                _logger.LogInformation("Updating task status for authenticated Pallet ULD: {UldCode}",
                    authenticatedItem.PalletDetails.UldCode);

                // Get the original task
                var originalTask = authenticatedItem.OriginalTask;
                
                // Check if destination cell is available
                long? destinationCellId = originalTask.DestinationCell?.Id;

                if (!destinationCellId.HasValue)
                {
                    _logger.LogInformation("Task for Pallet ULD {UldCode} has no destination.",
                        authenticatedItem.PalletDetails.UldCode);
                    MessageBox.Show("Automatic destination assignment not yet implemented. Cannot proceed with task.",
                        "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    Cell destinationCell = null;
                    if (destinationCellId.HasValue)
                    {
                        destinationCell = await unitOfWork.Cells.GetByIdAsync(destinationCellId.Value);
                        if (destinationCell == null)
                        {
                            _logger.LogWarning("Could not find destination cell with ID {CellId}.", destinationCellId.Value);
                        }
                    }

                    // Update the original task status
                    originalTask.Status = Models.Enums.TaskStatus.InProgress;
                    originalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit;
                    
                    // Convert to DB model and update in database
                    var dbTask = originalTask.ToDbModel();
                    
                    // Update the task in the database
                    unitOfWork.Tasks.Update(dbTask);
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Successfully updated task status (ID: {TaskId}).", originalTask.Id);

                    // Get the source finger
                    Finger sourceFinger = null;
                    if (originalTask.SourceFinger?.Id != null)
                    {
                        sourceFinger = await unitOfWork.Fingers.GetByIdAsync(originalTask.SourceFinger.Id);
                    }
                }

                // Create a storage task item for the UI
                var storageTaskDetails = originalTask;

                var storageItem = new PalletStorageTaskItem(authenticatedItem.PalletDetails, storageTaskDetails)
                {
                    GoToStorageCommand = this.GoToStorageLocationCommand,
                    ChangeDestinationCommand = this.ChangeDestinationCommand
                };

                _dispatcherService.Invoke(() =>
                {
                    PalletsReadyForStorage.Add(storageItem);
                    NotifyStorageItemsChanged();
                });

                _logger.LogInformation("Added PalletStorageTaskItem for Task ID {TaskId} to UI.", originalTask.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating storage task for authenticated Pallet ULD: {UldCode}",
                    authenticatedItem.PalletDetails?.UldCode ?? "N/A");
                MessageBox.Show($"Error creating storage task: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void ExecuteGoToStorageLocation(object parameter)
        {
            if (parameter is not PalletStorageTaskItem item || item.StorageTask?.DestinationCell == null)
            {
                _logger.LogWarning("Cannot navigate to storage: Invalid parameter.");
                MessageBox.Show("Cannot navigate: Destination cell information is missing.",
                    "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var destinationCell = item.StorageTask.DestinationCell;
            int? level = destinationCell.Level;
            int? position = destinationCell.Position;

            if (!level.HasValue || !position.HasValue)
            {
                _logger.LogWarning("Cannot navigate: Destination cell has incomplete level/position.");
                MessageBox.Show("Cannot navigate: Destination cell has incomplete level/position information.",
                    "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            short targetPositionValue = (short)((level.Value * 100) + position.Value);
            short commandCode = 1;

            string positionSpNodeId = OpcNodes.PositionRequest;
            string commandNodeId = OpcNodes.Control;

            try
            {
                _logger.LogInformation("Executing GoToStorageLocation for Task ID: {TaskId}", item.StorageTask.Id);

                await _opcService.WriteRegisterAsync(positionSpNodeId, targetPositionValue);
                await _opcService.WriteRegisterAsync(commandNodeId, commandCode);

                _logger.LogInformation("Navigation command sent for Task ID {TaskId}.", item.StorageTask.Id);
                item.StorageTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit;
                OnPropertyChanged(nameof(PalletsReadyForStorage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GoToStorageLocation.");
                MessageBox.Show($"Error navigating to storage location: {ex.Message}",
                    "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanExecuteGoToStorageLocation(object parameter)
        {
            if (parameter is not PalletStorageTaskItem item) return false;
            return item.StorageTask?.DestinationCell != null &&
                   item.StorageTask.DestinationCell.Level.HasValue &&
                   item.StorageTask.DestinationCell.Position.HasValue;
        }

        public void ExecuteChangeDestination(object parameter)
        {
            if (parameter is not PalletStorageTaskItem item) return;
            _logger.LogInformation("ExecuteChangeDestination called for Task ID: {TaskId}", item.StorageTask?.Id ?? 0);
            MessageBox.Show("Changing destination is not yet implemented.", "Not Implemented",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool CanExecuteChangeDestination(object parameter)
        {
            return false; // Disabled for now
        }

        public async void ExecuteGoToRetrievalLocation(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask?.SourceCell == null)
            {
                _logger.LogWarning("Cannot navigate to retrieval: Invalid parameter.");
                MessageBox.Show("Cannot navigate: Source cell information is missing.",
                    "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sourceCell = item.RetrievalTask.SourceCell;
            int? level = sourceCell.Level;
            int? position = sourceCell.Position;

            if (!level.HasValue || !position.HasValue)
            {
                _logger.LogWarning("Cannot navigate: Source cell has incomplete level/position.");
                MessageBox.Show("Cannot navigate: Source cell has incomplete level/position information.",
                    "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            short targetPositionValue = (short)((level.Value * 100) + position.Value);
            short commandCode = 1;

            string positionSpNodeId = OpcNodes.PositionRequest;
            string commandNodeId = OpcNodes.Control;

            try
            {
                _logger.LogInformation("Executing GoToRetrievalLocation for Task ID: {TaskId}", item.RetrievalTask.Id);

                await _opcService.WriteRegisterAsync(positionSpNodeId, targetPositionValue);
                await _opcService.WriteRegisterAsync(commandNodeId, commandCode);

                _logger.LogInformation("Navigation command sent for Task ID {TaskId}.", item.RetrievalTask.Id);
                item.RetrievalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit;
                OnPropertyChanged(nameof(PalletsForRetrieval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GoToRetrievalLocation.");
                MessageBox.Show($"Error navigating to retrieval location: {ex.Message}",
                    "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanExecuteGoToRetrievalLocation(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item) return false;
            return item.RetrievalTask?.SourceCell != null &&
                   item.RetrievalTask.SourceCell.Level.HasValue &&
                   item.RetrievalTask.SourceCell.Position.HasValue;
        }

        public void ExecuteChangeSource(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item) return;
            _logger.LogInformation("ExecuteChangeSource called for Task ID: {TaskId}", item.RetrievalTask?.Id ?? 0);
            MessageBox.Show("Changing source is not yet implemented.", "Not Implemented",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool CanExecuteChangeSource(object parameter)
        {
            return false; // Disabled for now
        }

        private async void ExecuteOpenCreateTaskDialog(object parameter)
        {
            _logger.LogInformation("ExecuteOpenCreateTaskDialog called.");

            try
            {
                // Get the current finger ID
                int currentFingerId = 0;
                
                if (_currentFingerPositionValue.HasValue)
                {
                    using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                    {
                        var finger = (await unitOfWork.Fingers.FindAsync(f => f.Position == _currentFingerPositionValue.Value)).FirstOrDefault();
                        if (finger != null)
                        {
                            currentFingerId = (int)finger.Id;
                        }
                    }
                }
                
                if (currentFingerId <= 0)
                {
                    _logger.LogWarning("Cannot create task: No valid finger ID found at current position.");
                    MessageBox.Show("Cannot create task: No valid finger location detected.", 
                        "Invalid Location", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var dialog = new ManualInputDialog(_unitOfWorkFactory, currentFingerId);
                
                if (dialog == null)
                {
                    _logger.LogError("Failed to create ManualInputDialog");
                    return;
                }

                if (dialog.ShowDialog() == true)
                {
                    if (dialog.DataContext is ManualInputViewModel manualInputVM)
                    {
                        TaskDetails newTaskDetails = manualInputVM.TaskDetails;

                        if (newTaskDetails == null)
                        {
                            _logger.LogWarning("Manual input dialog closed with OK, but TaskDetails were null.");
                            return;
                        }

                        bool saved = await TaskVM.SaveTaskToDatabase(newTaskDetails);

                        if (saved)
                        {
                            _logger.LogInformation("Manual task (ID: {TaskId}) created and saved.", newTaskDetails.Id);
                            TaskVM.Tasks.Add(newTaskDetails);

                            if (newTaskDetails.IsImportTask &&
                                newTaskDetails.SourceFinger?.Id != null &&
                                _currentFingerPositionValue.HasValue)
                            {
                                if (newTaskDetails.SourceFinger?.Position == _currentFingerPositionValue)
                                {
                                    _logger.LogInformation("Adding new task to authentication list.");

                                    if (newTaskDetails.Pallet != null)
                                    {
                                        var authItem = new PalletAuthenticationItem(newTaskDetails.Pallet, newTaskDetails)
                                        {
                                            AuthenticateCommand = this.ShowAuthenticationDialogCommand
                                        };
                                        _dispatcherService.Invoke(() => PalletsToAuthenticate.Add(authItem));
                                        IsFingerAuthenticationViewActive = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteOpenCreateTaskDialog");
                MessageBox.Show($"Error creating task: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Retrieval Destination Navigation and Unload

        public async void ExecuteGoToRetrievalDestination(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item)
            {
                _logger.LogWarning("Cannot navigate to retrieval destination: Invalid parameter.");
                MessageBox.Show("Cannot navigate: Invalid task item.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var retrievalTask = item.RetrievalTask;
            if (retrievalTask == null)
            {
                _logger.LogWarning("Cannot navigate to retrieval destination: Task details are missing.");
                MessageBox.Show("Cannot navigate: Task details are missing.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            short targetPositionValue = 0;
            bool isValidDestination = false;

            if (retrievalTask.DestinationFinger != null && retrievalTask.DestinationFinger.Position.HasValue)
            {
                // Destination is a Finger
                targetPositionValue = (short)retrievalTask.DestinationFinger.Position.Value;
                isValidDestination = true;
                _logger.LogInformation("Retrieval destination is Finger: {FingerName} (Position: {Position})", 
                    retrievalTask.DestinationFinger.DisplayName, targetPositionValue);
            }
            else if (retrievalTask.DestinationCell != null && retrievalTask.DestinationCell.Level.HasValue && retrievalTask.DestinationCell.Position.HasValue)
            {
                // Destination is a Cell (for HND retrieval cell-to-cell)
                targetPositionValue = (short)((retrievalTask.DestinationCell.Level.Value * 100) + retrievalTask.DestinationCell.Position.Value);
                isValidDestination = true;
                 _logger.LogInformation("Retrieval destination is Cell: {CellName} (Level: {Level}, Position: {Position})", 
                    retrievalTask.DestinationCell.DisplayName, retrievalTask.DestinationCell.Level, retrievalTask.DestinationCell.Position);
            }

            if (!isValidDestination)
            {
                _logger.LogWarning("Cannot navigate: Retrieval task ID {TaskId} has no valid destination finger or cell position.", retrievalTask.Id);
                MessageBox.Show("Cannot navigate: Destination information is incomplete or missing.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            short commandCode = 1; // Assuming 1 is the standard "go to position" command
            string positionSpNodeId = OpcNodes.PositionRequest;
            string commandNodeId = OpcNodes.Control;

            try
            {
                _logger.LogInformation("Executing GoToRetrievalDestination for Task ID: {TaskId}, Target Position: {TargetPosition}", 
                    retrievalTask.Id, targetPositionValue);

                await _opcService.WriteRegisterAsync(positionSpNodeId, targetPositionValue);
                await _opcService.WriteRegisterAsync(commandNodeId, commandCode);

                _logger.LogInformation("Navigation command sent for retrieval Task ID {TaskId} to position {TargetPosition}.", 
                    retrievalTask.Id, targetPositionValue);
                retrievalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit;
                OnPropertyChanged(nameof(PalletsReadyForDelivery)); // Assuming this collection holds these items
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GoToRetrievalDestination for Task ID {TaskId}.", retrievalTask.Id);
                MessageBox.Show($"Error navigating to retrieval destination: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanExecuteGoToRetrievalDestination(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask == null) return false;
            
            bool hasFingerDest = item.RetrievalTask.DestinationFinger != null && item.RetrievalTask.DestinationFinger.Position.HasValue;
            bool hasCellDest = item.RetrievalTask.DestinationCell != null && 
                               item.RetrievalTask.DestinationCell.Level.HasValue && 
                               item.RetrievalTask.DestinationCell.Position.HasValue;
            
            return hasFingerDest || hasCellDest;
        }

        public async void ExecuteUnloadAtDestination(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask == null || item.PalletDetails == null)
            {
                _logger.LogWarning("Cannot unload at destination: Invalid parameter.");
                MessageBox.Show("Cannot unload: Invalid task or pallet item.", "Unload Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var retrievalTask = item.RetrievalTask;
            var palletToUnload = item.PalletDetails;
            bool isDestinationFinger = retrievalTask.DestinationFinger != null;
            bool isDestinationCell = retrievalTask.DestinationCell != null && !isDestinationFinger; // If finger is primary, cell is secondary for HND

            _logger.LogInformation("Executing UnloadAtDestination for Task ID: {TaskId}, Pallet: {PalletUld}", retrievalTask.Id, palletToUnload.UldCode);

            // Determine which side of the trolley the pallet is on
            EM.Maman.Models.DisplayModels.TrolleyCell trolleyCellWithPallet = null;
            if (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.LeftCell.Pallet?.Id == palletToUnload.Id)
            {
                trolleyCellWithPallet = TrolleyVM.LeftCell;
            }
            else if (TrolleyVM.RightCell.IsOccupied && TrolleyVM.RightCell.Pallet?.Id == palletToUnload.Id)
            {
                trolleyCellWithPallet = TrolleyVM.RightCell;
            }

            if (trolleyCellWithPallet == null)
            {
                 _logger.LogWarning("Pallet {PalletUld} for Task ID {TaskId} not found on trolley for unloading.", palletToUnload.UldCode, retrievalTask.Id);
                 MessageBox.Show($"Pallet {palletToUnload.UldCode} not found on trolley.", "Unload Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }
            
            try
            {
                // TODO: Adapt actual unload OPC commands / logic from TrolleyOperationsViewModel
                // For now, simulating unload and using existing TrolleyOperationsVM methods.
                // This will need to be replaced with actual OPC calls for unloading to finger/cell.
                if (trolleyCellWithPallet == TrolleyVM.LeftCell)
                {
                    await TrolleyOperationsVM.UnloadPalletFromLeftCellAsync(); // This method needs to exist and handle OPC
                }
                else
                {
                    await TrolleyOperationsVM.UnloadPalletFromRightCellAsync(); // This method needs to exist and handle OPC
                }
                _logger.LogInformation("Unload command simulated for pallet {PalletUld} from Task ID {TaskId}.", palletToUnload.UldCode, retrievalTask.Id);

                retrievalTask.Status = Models.Enums.TaskStatus.Completed;
                retrievalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.finished; // Or a specific "unloading" then "completed"
                retrievalTask.ExecutedDateTime = DateTime.Now;

                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var dbTask = retrievalTask.ToDbModel();
                    unitOfWork.Tasks.Update(dbTask);
                    
                    // If unloaded to a cell, update PalletInCell
                    if (isDestinationCell && retrievalTask.DestinationCell != null)
                    {
                        var palletInCell = new PalletInCell
                        {
                            PalletId = palletToUnload.Id,
                            CellId = retrievalTask.DestinationCell.Id,
                            StorageDate = DateTime.Now,
                            // Notes, etc.
                        };
                        await unitOfWork.PalletInCells.AddAsync(palletInCell);
                        _logger.LogInformation("Pallet {PalletUld} recorded in Cell {CellId} after retrieval task.", palletToUnload.Id, retrievalTask.DestinationCell.Id);
                    }
                    // If it was previously in a cell (RetrievalSourceCell), mark it as removed from there.
                    if (retrievalTask.SourceCell != null)
                    {
                        var existingPic = await unitOfWork.PalletInCells.GetByPalletAndCellAsync(palletToUnload.Id, retrievalTask.SourceCell.Id);
                        if (existingPic != null)
                        {
                            unitOfWork.PalletInCells.Remove(existingPic);
                             _logger.LogInformation("Pallet {PalletUld} removed from SourceCell {CellId} tracking.", palletToUnload.Id, retrievalTask.SourceCell.Id);
                        }
                    }

                    await unitOfWork.CompleteAsync();
                }

                _logger.LogInformation("Retrieval Task ID {TaskId} successfully completed and unloaded.", retrievalTask.Id);
                
                _dispatcherService.Invoke(() =>
                {
                    PalletsReadyForDelivery.Remove(item);
                    // Notify relevant UI if needed
                });

                // Start 5-second timer (as per requirement "five minutess toimer to remove it" - assuming 5 seconds for now)
                // This timer's action (e.g., clearing finger status) needs to be defined.
                System.Diagnostics.Debug.WriteLine($"Timer started for Task {retrievalTask.Id} completion (5s). Action TBD.");
                await System.Threading.Tasks.Task.Delay(5000); 
                System.Diagnostics.Debug.WriteLine($"Timer elapsed for Task {retrievalTask.Id}.");
                // TODO: Implement actual action after timer (e.g., clear finger display, etc.)

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing UnloadAtDestination for Task ID {TaskId}.", retrievalTask.Id);
                MessageBox.Show($"Error unloading pallet: {ex.Message}", "Unload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Potentially revert task status if unload failed critically
            }
        }

        public bool CanExecuteUnloadAtDestination(object parameter)
        {
             if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask == null || item.PalletDetails == null) return false;
            // Condition: Trolley should be at the destination (finger or cell)
            // This requires OPC feedback for current trolley position matching task's destination.
            // For now, let's assume if the task is in 'PalletsReadyForDelivery' and not 'transit', it might be at destination.
            // This needs robust checking with actual trolley position.
            // Also, the pallet must be on the trolley.
            bool palletOnTrolley = (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.LeftCell.Pallet?.Id == item.PalletDetails.Id) ||
                                   (TrolleyVM.RightCell.IsOccupied && TrolleyVM.RightCell.Pallet?.Id == item.PalletDetails.Id);

            // Simplified: if it's in the list and not in transit, and pallet is on trolley.
            // A more accurate check would involve comparing CurrentTrolleyPosition with task destination.
            return item.RetrievalTask.ActiveTaskStatus != Models.Enums.ActiveTaskStatus.transit && palletOnTrolley;
        }

        #endregion

        #region HND Retrieval Task Creation

        public async System.Threading.Tasks.Task InitiateHndRetrievalTaskCreation(Pallet loadedPallet, Cell sourceCell)
        {
            if (loadedPallet == null || sourceCell == null)
            {
                _logger.LogWarning("InitiateHndRetrievalTaskCreation called with null pallet or source cell.");
                MessageBox.Show("Cannot create HND retrieval task: Pallet or source cell information is missing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _logger.LogInformation("Initiating HND retrieval task creation for pallet {PalletId} from source cell {SourceCellId}", loadedPallet.Id, sourceCell.Id);

            var dialogVM = _serviceProvider.GetRequiredService<SelectHndDestinationViewModel>();
            // Re-initialize or pass parameters to existing dialogVM instance if it's not transient
            // For a transient dialog VM, new instance is fine. If it's scoped/singleton, need to reset its state.
            // Assuming SelectHndDestinationViewModel can be newed up or its state reset:
            // For this example, let's assume it's newed up or its constructor handles resetting.
            // The constructor of SelectHndDestinationViewModel takes (IUnitOfWorkFactory, ILogger, Pallet, Cell)
            // We need to get IUnitOfWorkFactory and ILogger from _serviceProvider or pass them from MainViewModel.
            
            var hndDialogVM = new SelectHndDestinationViewModel(_unitOfWorkFactory, _loggerFactory.CreateLogger<SelectHndDestinationViewModel>(), loadedPallet, sourceCell);
            var dialog = new SelectHndDestinationDialog
            {
                DataContext = hndDialogVM,
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                Finger selectedFinger = hndDialogVM.SelectedFinger;
                Cell selectedCell = hndDialogVM.SelectedCell;

                if (selectedFinger == null && selectedCell == null)
                {
                    _logger.LogWarning("HND destination selection was confirmed, but no destination was selected.");
                    MessageBox.Show("No destination selected for HND retrieval.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newTaskDetails = new TaskDetails
                {
                    Name = $"HND Retrieval - {loadedPallet.DisplayName}",
                    Description = $"Manual HND retrieval of pallet {loadedPallet.UldCode} from cell {sourceCell.DisplayName}",
                    TaskType = Models.Enums.TaskType.Retrieval,
                    Status = Models.Enums.TaskStatus.InProgress, // Already on trolley
                    ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit, // Ready to move to destination
                    CreatedDateTime = DateTime.Now,
                    Pallet = loadedPallet,
                    SourceCell = sourceCell,
                    DestinationFinger = selectedFinger,
                    DestinationCell = selectedCell, // This will be null if finger was selected, and vice-versa
                    // UserId = _currentUserContext.CurrentUser.Id // Assuming CurrentUser is available and has Id
                };
                
                // TODO: Ensure UserId is set if required by DB or TaskDetails logic.
                if (_currentUserContext.CurrentUser != null) newTaskDetails.UserId = _currentUserContext.CurrentUser.Id;


                bool saved = await TaskVM.SaveTaskToDatabase(newTaskDetails);
                if (saved)
                {
                    _logger.LogInformation("HND Retrieval Task (ID: {TaskId}) created and saved for pallet {PalletId}.", newTaskDetails.Id, loadedPallet.Id);
                    
                    // Add to PalletsReadyForDelivery
                    var retrievalDeliveryItem = new PalletRetrievalTaskItem(loadedPallet, newTaskDetails)
                    {
                        GoToRetrievalCommand = this.GoToRetrievalDestinationCommand,
                        UnloadCommand = this.UnloadAtDestinationCommand
                    };
                    _dispatcherService.Invoke(() =>
                    {
                        PalletsReadyForDelivery.Add(retrievalDeliveryItem);
                        OnPropertyChanged(nameof(HasPalletsReadyForDelivery)); // Notify UI
                        OnPropertyChanged(nameof(ShouldShowTasksPanel)); // Update dependent properties
                        OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
                        OnPropertyChanged(nameof(IsDefaultTaskViewActive));
                    });
                    MessageBox.Show($"HND Retrieval task created for pallet {loadedPallet.DisplayName}. Ready for delivery.", "HND Task Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _logger.LogError("Failed to save HND Retrieval Task for pallet {PalletId}.", loadedPallet.Id);
                    MessageBox.Show("Failed to save HND retrieval task.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _logger.LogInformation("HND retrieval destination selection cancelled for pallet {PalletId}.", loadedPallet.Id);
            }
        }

        #endregion
    }
}
