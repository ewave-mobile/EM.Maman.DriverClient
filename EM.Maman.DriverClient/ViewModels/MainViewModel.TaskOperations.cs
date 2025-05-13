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
using DbTask = EM.Maman.Models.LocalDbModels.Task;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        #region Task Operations

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
                    OnPropertyChanged(nameof(HasPalletsReadyForStorage));
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
            int? level = destinationCell.HeightLevel;
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
                   item.StorageTask.DestinationCell.HeightLevel.HasValue &&
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
            int? level = sourceCell.HeightLevel;
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
                   item.RetrievalTask.SourceCell.HeightLevel.HasValue &&
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
                var dialog = (App.Current as App)?.ServiceProvider.GetRequiredService<ManualTaskDialog>();

                if (dialog == null)
                {
                    _logger.LogError("Failed to resolve ManualTaskDialog from service provider");
                    return;
                }

                if (dialog.ShowDialog() == true)
                {
                    if (dialog.DataContext is ManualTaskViewModel manualTaskVM)
                    {
                        TaskDetails newTaskDetails = manualTaskVM.IsImportSelected ?
                            manualTaskVM.ImportVM?.TaskDetails : manualTaskVM.ExportVM?.TaskDetails;

                        if (newTaskDetails == null)
                        {
                            _logger.LogWarning("Manual task dialog closed with OK, but TaskDetails were null.");
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

                                    string palletUldCode = newTaskDetails.Pallet?.UldCode;
                                    if (newTaskDetails.Pallet == null && !string.IsNullOrEmpty(palletUldCode))
                                    {
                                        // Create a new UnitOfWork instance for this operation
                                        using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                                        {
                                            newTaskDetails.Pallet = (await unitOfWork.Pallets
                                                .FindAsync(p => p.UldCode == palletUldCode)).FirstOrDefault();
                                        }
                                    }

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
    }
}
