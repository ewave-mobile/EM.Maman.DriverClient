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

        public async System.Threading.Tasks.Task BeginRetrievalTaskAtSourceCellAsync(TaskDetails retrievalTaskDetails)
        {
            if (retrievalTaskDetails == null || retrievalTaskDetails.SourceCell == null)
            {
                _logger.LogWarning("BeginRetrievalTaskAtSourceCellAsync called with null task or source cell.");
                MessageBox.Show("Cannot begin retrieval: Task or source cell details are missing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            _logger.LogInformation("Beginning retrieval task {TaskId} at source cell {CellName}", retrievalTaskDetails.Id, retrievalTaskDetails.SourceCell.DisplayName);

            retrievalTaskDetails.ActiveTaskStatus = ActiveTaskStatus.navigating_to_source;
            await TaskVM.SaveTaskToDatabase(retrievalTaskDetails); 
            _logger.LogInformation("Task {TaskId} status updated to navigating_to_source.", retrievalTaskDetails.Id);

            try
            {
                await NavigateToCellAsync(retrievalTaskDetails.SourceCell);
                _logger.LogInformation("Navigation to source cell {CellName} for task {TaskId} deemed complete.", retrievalTaskDetails.SourceCell.DisplayName, retrievalTaskDetails.Id);
            }
            catch (Exception navEx)
            {
                _logger.LogError(navEx, "Navigation to source cell failed for Task {TaskId}.", retrievalTaskDetails.Id);
                retrievalTaskDetails.ActiveTaskStatus = ActiveTaskStatus.pending; 
                await TaskVM.SaveTaskToDatabase(retrievalTaskDetails);
                MessageBox.Show($"Navigation failed for task {retrievalTaskDetails.Name}: {navEx.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            _logger.LogInformation("Task {TaskId} successfully navigated to source cell {CellName}. Updating status to Retrieval for authentication.", retrievalTaskDetails.Id, retrievalTaskDetails.SourceCell.DisplayName);
            retrievalTaskDetails.ActiveTaskStatus = ActiveTaskStatus.retrieval; // Changed from transit
            await TaskVM.SaveTaskToDatabase(retrievalTaskDetails); 

            // CheckForArrivalAtDestination is implicitly called by OnPositionChanged when _currentCellLevel/_currentCellPosition are updated
            // However, explicitly calling it here ensures the logic runs immediately after navigation completes.
            // The OnPositionChanged handler will also update IsAtCellWithRetrievalTask which might be used by UI.
            CheckForArrivalAtDestination((short)((_currentCellLevel * 100) + _currentCellPosition)); // Ensure correct type
            _logger.LogInformation("Explicitly called CheckForArrivalAtDestination with L{Level} P{Position} for Task {TaskId}", _currentCellLevel, _currentCellPosition, retrievalTaskDetails.Id);

            // Setup for cell authentication
            if (retrievalTaskDetails.Pallet != null)
            {
                var authItemForCell = new PalletAuthenticationItem(
                    retrievalTaskDetails.Pallet,
                    retrievalTaskDetails,
                    AuthenticationContextMode.Retrieval
                );
                ActiveCellAuthenticationItem = authItemForCell;
                IsCellAuthenticationViewActive = true;
                _logger.LogInformation("Task {TaskId} at source cell {CellName}. Cell authentication view activated.", retrievalTaskDetails.Id, retrievalTaskDetails.SourceCell.DisplayName);
            }
            else
            {
                _logger.LogWarning("Task {TaskId} at source cell {CellName} but PalletDetails are null. Cannot set up cell authentication.", retrievalTaskDetails.Id, retrievalTaskDetails.SourceCell.DisplayName);
                // Optionally, set IsCellAuthenticationViewActive = false or handle error
            }
        }

        private async System.Threading.Tasks.Task NavigateToCellAsync(Cell targetCell)
        {
            if (targetCell == null)
            {
                _logger.LogWarning("NavigateToCellAsync called with null targetCell.");
                MessageBox.Show("Cannot navigate: Target cell is not specified.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            if (!targetCell.Level.HasValue || !targetCell.Position.HasValue)
            {
                 _logger.LogWarning("NavigateToCellAsync: Target cell {CellName} (ID: {CellId}) has null Level or Position.", targetCell.DisplayName, targetCell.Id);
                 MessageBox.Show($"Cannot navigate: Target cell '{targetCell.DisplayName}' has incomplete location details.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 return; 
            }

            _logger.LogInformation("Navigating to Cell: {CellName} (L{Level}-P{Position})", targetCell.DisplayName, targetCell.Level, targetCell.Position);
            
            short targetPositionValue = (short)((targetCell.Level.Value * 100) + targetCell.Position.Value);
            short commandCode = 1; 

            try
            {
                await _opcService.WriteRegisterAsync(OpcNodes.PositionRequest, targetPositionValue);
                await _opcService.WriteRegisterAsync(OpcNodes.Control, commandCode);
                _logger.LogInformation("OPC command sent to navigate to cell L{Level}-P{Position} (Value: {Value})", targetCell.Level, targetCell.Position, targetPositionValue);

                _activeNavigationTargetPosition = targetPositionValue;
                _activeNavigationTcs = new System.Threading.Tasks.TaskCompletionSource<bool>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
                
                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30))) 
                {
                    cts.Token.Register(() => _activeNavigationTcs?.TrySetCanceled(), useSynchronizationContext: false);

                    _logger.LogInformation("Waiting for arrival at L{Level}-P{Position} (Value: {Value}) and status '{Status}'...",
                                         targetCell.Level, targetCell.Position, targetPositionValue, _activeNavigationTargetStatus);
                    try
                    {
                        await _activeNavigationTcs.Task; 
                        _logger.LogInformation("Confirmed arrival at Cell: {CellName} (L{Level}-P{Position})", targetCell.DisplayName, targetCell.Level, targetCell.Position);
                    }
                    catch (System.Threading.Tasks.TaskCanceledException)
                    {
                        _logger.LogWarning("Navigation to L{Level}-P{Position} timed out.", targetCell.Level, targetCell.Position);
                        MessageBox.Show($"Navigation to {targetCell.DisplayName} timed out.", "Navigation Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                        throw; 
                    }
                    finally
                    {
                        _activeNavigationTcs = null;
                        _activeNavigationTargetPosition = null;
                    }
                }
                
                _currentCellLevel = targetCell.Level.Value;
                _currentCellPosition = targetCell.Position.Value;
                _currentFingerPositionValue = null; 
                OnPropertyChanged(nameof(CurrentFingerDisplayName)); 
                OnPropertyChanged(nameof(IsAtCellWithRetrievalTask)); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to cell L{Level}-P{Position}", targetCell.Level, targetCell.Position);
                MessageBox.Show($"Error navigating to cell: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

                _logger.LogInformation("Processing authenticated retrieval for Task ID: {TaskId}. Pallet is on trolley, task status should be InProgress/Transit.",
                    retrievalTaskDetails.Id);

                var selectCellCmd = new RelayCommand(async (item) =>
                {
                    if (item is PalletRetrievalTaskItem taskItem)
                    {
                        await ShowSelectCellDialogForRetrievalItemAsync(taskItem);
                    }
                });

                var retrievalDeliveryItem = new PalletRetrievalTaskItem(
                    authenticatedItem.PalletDetails,
                    retrievalTaskDetails,
                    this.AvailableFingers,
                    this.GoToRetrievalDestinationCommand,
                    this.ChangeSourceCommand, // Assuming ChangeSourceCommand is a valid command in MainViewModel
                    this.UnloadAtDestinationCommand,
                    selectCellCmd
                );
                
                _dispatcherService.Invoke(() =>
                {
                    var itemToRemove = PalletsForRetrieval.FirstOrDefault(p => p.RetrievalTask.Id == retrievalTaskDetails.Id);
                    if (itemToRemove != null)
                    {
                        PalletsForRetrieval.Remove(itemToRemove);
                        _logger.LogInformation("Removed PalletRetrievalTaskItem for Task ID {TaskId} from PalletsForRetrieval.", retrievalTaskDetails.Id);
                    }

                    PalletsReadyForDelivery.Add(retrievalDeliveryItem);
                    _logger.LogInformation("Added PalletRetrievalTaskItem for Task ID {TaskId} to PalletsReadyForDelivery.", retrievalTaskDetails.Id);
                    OnPropertyChanged(nameof(HasPalletsReadyForDelivery)); // Notify UI to update visibility
                    OnPropertyChanged(nameof(ShouldShowTasksPanel));      // Also update panel visibility
                    OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
                    OnPropertyChanged(nameof(IsDefaultTaskViewActive));
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

                var originalTask = authenticatedItem.OriginalTask;
                
                long? destinationCellId = originalTask.DestinationCell?.Id;

                if (!destinationCellId.HasValue)
                {
                    _logger.LogInformation("Task for Pallet ULD {UldCode} has no destination.",
                        authenticatedItem.PalletDetails.UldCode);
                    MessageBox.Show("Automatic destination assignment not yet implemented. Cannot proceed with task.",
                        "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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

                    originalTask.Status = Models.Enums.TaskStatus.InProgress;
                    originalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit;
                    
                    var dbTask = originalTask.ToDbModel();
                    
                    unitOfWork.Tasks.Update(dbTask);
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Successfully updated task status (ID: {TaskId}).", originalTask.Id);

                    Finger sourceFinger = null;
                    if (originalTask.SourceFinger?.Id != null)
                    {
                        sourceFinger = await unitOfWork.Fingers.GetByIdAsync(originalTask.SourceFinger.Id);
                    }
                }

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
            if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask == null || item.PalletDetails == null) return false;

            bool isCorrectStatus = item.RetrievalTask.ActiveTaskStatus == ActiveTaskStatus.pending ||
                                   item.RetrievalTask.ActiveTaskStatus == ActiveTaskStatus.navigating_to_source ||
                                   item.RetrievalTask.ActiveTaskStatus == ActiveTaskStatus.retrieval;

            bool hasValidSource = item.RetrievalTask.SourceCell != null &&
                                  item.RetrievalTask.SourceCell.Level.HasValue &&
                                  item.RetrievalTask.SourceCell.Position.HasValue;

            bool palletNotOnTrolley = true; 
            if (TrolleyVM != null && item.PalletDetails != null)
            {
                if ((TrolleyVM.LeftCell.IsOccupied && TrolleyVM.LeftCell.Pallet?.Id == item.PalletDetails.Id) ||
                    (TrolleyVM.RightCell.IsOccupied && TrolleyVM.RightCell.Pallet?.Id == item.PalletDetails.Id))
                {
                    palletNotOnTrolley = false; 
                }
            }
            
            // Trolley is not moving if there's no active navigation task completion source
            bool trolleyNotMoving = _activeNavigationTcs == null; 

            return isCorrectStatus && hasValidSource && palletNotOnTrolley && trolleyNotMoving;
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
                            await TaskVM.LoadAvailableStorageFingersAsync();

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
                targetPositionValue = (short)retrievalTask.DestinationFinger.Position.Value;
                isValidDestination = true;
                _logger.LogInformation("Retrieval destination is Finger: {FingerName} (Position: {Position})", 
                    retrievalTask.DestinationFinger.DisplayName, targetPositionValue);
            }
            else if (retrievalTask.DestinationCell != null && retrievalTask.DestinationCell.Level.HasValue && retrievalTask.DestinationCell.Position.HasValue)
            {
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

            short commandCode = 1; 
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
                OnPropertyChanged(nameof(PalletsReadyForDelivery)); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GoToRetrievalDestination for Task ID {TaskId}.", retrievalTask.Id);
                MessageBox.Show($"Error navigating to retrieval destination: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanExecuteGoToRetrievalDestination(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask == null || item.PalletDetails == null) return false;

            bool isCorrectStatus = item.RetrievalTask.ActiveTaskStatus == ActiveTaskStatus.transit ||
                                   item.RetrievalTask.ActiveTaskStatus == ActiveTaskStatus.arrived_at_destination; 

            bool hasValidDestination = (item.RetrievalTask.DestinationFinger != null && item.RetrievalTask.DestinationFinger.Position.HasValue) ||
                                       (item.RetrievalTask.DestinationCell != null && item.RetrievalTask.DestinationCell.Level.HasValue && item.RetrievalTask.DestinationCell.Position.HasValue);

            bool palletOnTrolley = false;
            if (TrolleyVM != null && item.PalletDetails != null)
            {
                if ((TrolleyVM.LeftCell.IsOccupied && TrolleyVM.LeftCell.Pallet?.Id == item.PalletDetails.Id) ||
                    (TrolleyVM.RightCell.IsOccupied && TrolleyVM.RightCell.Pallet?.Id == item.PalletDetails.Id))
                {
                    palletOnTrolley = true;
                }
            }
            
            bool trolleyNotMoving = true;

            return isCorrectStatus && hasValidDestination && palletOnTrolley && trolleyNotMoving;
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
            bool isDestinationCell = retrievalTask.DestinationCell != null && !isDestinationFinger; 

            _logger.LogInformation("Executing UnloadAtDestination for Task ID: {TaskId}, Pallet: {PalletUld}", retrievalTask.Id, palletToUnload.UldCode);

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
                bool unloadSuccessful = false;
                if (trolleyCellWithPallet == TrolleyVM.LeftCell)
                {
                    unloadSuccessful = await TrolleyOperationsVM.UnloadPalletFromLeftCellAsync();
                }
                else
                {
                    unloadSuccessful = await TrolleyOperationsVM.UnloadPalletFromRightCellAsync();
                }

                if (unloadSuccessful)
                {
                    _logger.LogInformation("Unload operation successful for pallet {PalletUld} from Task ID {TaskId}.", palletToUnload.UldCode, retrievalTask.Id);

                    retrievalTask.Status = Models.Enums.TaskStatus.Completed;
                    retrievalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.finished;
                    retrievalTask.ExecutedDateTime = DateTime.Now;

                    using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                    {
                        var dbTask = retrievalTask.ToDbModel();
                        unitOfWork.Tasks.Update(dbTask);

                        if (isDestinationCell && retrievalTask.DestinationCell != null)
                        {
                            var palletInCell = new PalletInCell
                            {
                                PalletId = palletToUnload.Id,
                                CellId = retrievalTask.DestinationCell.Id,
                                StorageDate = DateTime.Now,
                            };
                            await unitOfWork.PalletInCells.AddAsync(palletInCell);
                            _logger.LogInformation("Pallet {PalletUld} recorded in Cell {CellId} after retrieval task.", palletToUnload.Id, retrievalTask.DestinationCell.Id);
                        }
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

                    _logger.LogInformation("Retrieval Task ID {TaskId} successfully completed and database updated.", retrievalTask.Id);
                    TaskVM?.RefreshTaskStatus(retrievalTask);

                    _logger.LogInformation("Retrieval Task ID {TaskId} completed. It will be removed from active display in 5 seconds.", retrievalTask.Id);
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(5000);
                        _dispatcherService.Invoke(() =>
                        {
                            if (PalletsReadyForDelivery.Contains(item))
                            {
                                PalletsReadyForDelivery.Remove(item);
                                _logger.LogInformation("Delayed removal of completed Retrieval Task ID {TaskId} from PalletsReadyForDelivery.", retrievalTask.Id);
                                OnPropertyChanged(nameof(HasPalletsReadyForDelivery));
                                OnPropertyChanged(nameof(ShouldShowTasksPanel));
                                OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
                                OnPropertyChanged(nameof(IsDefaultTaskViewActive));
                            }
                        });
                    });
                }
                else
                {
                    _logger.LogError("Unload operation failed for Task ID {TaskId}, Pallet: {PalletUld}. Task status not set to Completed.", retrievalTask.Id, palletToUnload.UldCode);
                    // Optionally, set task status to Failed or revert to InProgress
                    // retrievalTask.Status = Models.Enums.TaskStatus.Failed;
                    // await TaskVM.SaveTaskToDatabase(retrievalTask); // Save updated status
                    // TaskVM?.RefreshTaskStatus(retrievalTask);
                    MessageBox.Show($"Unload operation failed for pallet {palletToUnload.UldCode}. Task not marked as completed.", "Unload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteUnloadAtDestination for Task ID {TaskId}.", retrievalTask.Id);
                MessageBox.Show($"An error occurred while finalizing unload for task {retrievalTask.Name}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Consider additional error handling, like setting task status to Failed.
            }
        }

        public bool CanExecuteUnloadAtDestination(object parameter)
        {
             if (parameter is not PalletRetrievalTaskItem item || item.RetrievalTask == null || item.PalletDetails == null) return false;
            bool palletOnTrolley = (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.LeftCell.Pallet?.Id == item.PalletDetails.Id) ||
                                   (TrolleyVM.RightCell.IsOccupied && TrolleyVM.RightCell.Pallet?.Id == item.PalletDetails.Id);

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
                    Status = Models.Enums.TaskStatus.InProgress, 
                    ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit, 
                    CreatedDateTime = DateTime.Now,
                    Pallet = loadedPallet,
                    SourceCell = sourceCell,
                    DestinationFinger = selectedFinger,
                    DestinationCell = selectedCell, 
                };
                
                if (_currentUserContext.CurrentUser != null) newTaskDetails.UserId = _currentUserContext.CurrentUser.Id;


                bool saved = await TaskVM.SaveTaskToDatabase(newTaskDetails);
                if (saved)
                {
                    _logger.LogInformation("HND Retrieval Task (ID: {TaskId}) created and saved for pallet {PalletId}.", newTaskDetails.Id, loadedPallet.Id);

                    var selectCellCmdForHnd = new RelayCommand(async (item) =>
                    {
                        if (item is PalletRetrievalTaskItem taskItem)
                        {
                            await ShowSelectCellDialogForRetrievalItemAsync(taskItem);
                        }
                    });
                    
                    var retrievalDeliveryItem = new PalletRetrievalTaskItem(
                        loadedPallet, 
                        newTaskDetails,
                        this.AvailableFingers,
                        this.GoToRetrievalDestinationCommand,
                        this.ChangeSourceCommand, // Assuming ChangeSourceCommand is a valid command in MainViewModel
                        this.UnloadAtDestinationCommand,
                        selectCellCmdForHnd
                        );

                    _dispatcherService.Invoke(() =>
                    {
                        PalletsReadyForDelivery.Add(retrievalDeliveryItem);
                        OnPropertyChanged(nameof(HasPalletsReadyForDelivery)); 
                        OnPropertyChanged(nameof(ShouldShowTasksPanel)); 
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
