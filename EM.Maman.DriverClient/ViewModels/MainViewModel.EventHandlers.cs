using System;
using System.Linq;
using System.Windows;
using EM.Maman.Models.Enums;
using Microsoft.Extensions.Logging;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        private async void OnPositionChanged(object sender, int positionValue)
        {
            CheckForArrivalAtDestination(positionValue);

            int level = positionValue / 100;
            int position = positionValue % 100;

            // Update current cell level and position for MainViewModel
            _currentCellLevel = level;
            _currentCellPosition = position;

            if (CurrentTrolley != null)
            {
                CurrentTrolley.Position = position;
                OnPropertyChanged(nameof(CurrentTrolley));
            }

            if (TrolleyVM != null)
            {
                TrolleyVM.UpdateTrolleyPosition(level, position);
                // Ensure selected level always matches current physical level after a position change
                if (TrolleyVM.SelectedLevelNumber != level)
                {
                    _logger.LogInformation($"Physical trolley level ({level}) and selected display level ({TrolleyVM.SelectedLevelNumber}) differ on position change. Syncing display level.");
                    TrolleyVM.SelectedLevelNumber = level;
                }
            }

            if (WarehouseVM != null)
            {
                WarehouseVM.CurrentLevelNumber = level;
            }

            bool isFinger = await IsFingerLocationAsync(positionValue);

            if (isFinger)
            {
                if (_currentFingerPositionValue != positionValue)
                {
                    _logger.LogInformation("Arrived at finger location (PositionValue: {PositionValue}).", positionValue);
                    _currentFingerPositionValue = positionValue; // Set this before loading pallets
                    CurrentCellDisplayName = string.Empty; // Clear cell display name when at a finger
                    IsCellAuthenticationViewActive = false; // Hide cell auth when at a finger
                    ActiveCellAuthenticationItem = null;    // Clear cell auth item

                    // Create a new UnitOfWork instance for this operation
                    using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                    {
                        var finger = (await unitOfWork.Fingers.FindAsync(f => f.Position == positionValue)).FirstOrDefault();
                        if (finger != null)
                        {
                            CurrentFingerDisplayName = finger.DisplayName;
                        }
                    }
                    await LoadPalletsForFingerAuthenticationAsync(positionValue); // Load pallets after updating display names
                }
                // No 'else if' needed here, if it's a finger, it's handled above.
                // If _currentFingerPositionValue was already positionValue, it means we are still at the same finger.
            }
            else // Not a finger location (i.e., at a cell or travelling)
            {
                if (_currentFingerPositionValue != null) // Just left a finger
                {
                    _logger.LogInformation("Left finger location (Was: {_currentFingerPositionValue}, Now: {positionValue}). Clearing finger context.", _currentFingerPositionValue, positionValue);
                    _currentFingerPositionValue = null;
                    CurrentFingerDisplayName = string.Empty;
                    IsFingerAuthenticationViewActive = false;
                    _dispatcherService.Invoke(() =>
                    {
                        PalletsToAuthenticate.Clear();
                    });
                }

                CurrentCellDisplayName = $"L{_currentCellLevel}-P{_currentCellPosition}"; // Update current cell display name

                // Check if we arrived at a cell that is a source for an active retrieval task awaiting authentication
                // This task would be in PalletsForRetrieval or could be the TaskVM.ActiveTask if just started
                var taskToAuthAtCell = PalletsForRetrieval
                    .FirstOrDefault(prItem => prItem.RetrievalTask?.SourceCell != null &&
                                             prItem.RetrievalTask.SourceCell.Level == _currentCellLevel &&
                                             prItem.RetrievalTask.SourceCell.Position == _currentCellPosition &&
                                             prItem.RetrievalTask.ActiveTaskStatus == ActiveTaskStatus.retrieval);
                
                // Also consider TaskVM.ActiveTask if it's not in PalletsForRetrieval yet
                if (taskToAuthAtCell == null && TaskVM?.ActiveTask?.TaskType == Models.Enums.TaskType.Retrieval &&
                    TaskVM.ActiveTask.SourceCell != null &&
                    TaskVM.ActiveTask.SourceCell.Level == _currentCellLevel &&
                    TaskVM.ActiveTask.SourceCell.Position == _currentCellPosition &&
                    TaskVM.ActiveTask.ActiveTaskStatus == ActiveTaskStatus.retrieval)
                {
                    // Create a temporary PalletRetrievalTaskItem for consistency if needed, or directly use TaskVM.ActiveTask.Pallet
                    if (TaskVM.ActiveTask.Pallet != null)
                    {
                        var selectCellCmd = new RelayCommand(async (item) =>
                        {
                            if (item is PalletRetrievalTaskItem taskItemFromCmd)
                            {
                                await ShowSelectCellDialogForRetrievalItemAsync(taskItemFromCmd);
                            }
                        });

                        // Use a local var to avoid modifying PalletsForRetrieval here
                        var tempRetrievalItem = new PalletRetrievalTaskItem(
                            TaskVM.ActiveTask.Pallet, 
                            TaskVM.ActiveTask,
                            this.AvailableFingers,
                            this.GoToRetrievalDestinationCommand, // Or specific command for this context
                            this.ChangeSourceCommand,             // Or specific command for this context
                            this.UnloadAtDestinationCommand,      // Or specific command for this context
                            selectCellCmd
                            );
                        taskToAuthAtCell = tempRetrievalItem; 
                         _logger.LogInformation("Considering TaskVM.ActiveTask {TaskId} for cell authentication.", TaskVM.ActiveTask.Id);
                    }
                }


                if (taskToAuthAtCell != null && taskToAuthAtCell.PalletDetails != null)
                {
                    // Only set up new auth item if it's different from the current one or if current is null
                    if (ActiveCellAuthenticationItem == null || ActiveCellAuthenticationItem.OriginalTask?.Id != taskToAuthAtCell.RetrievalTask.Id)
                    {
                        _logger.LogInformation("Arrived at cell L{Level}-P{Position} for retrieval task {TaskId} which is awaiting authentication. Setting up auth view.",
                                               _currentCellLevel, _currentCellPosition, taskToAuthAtCell.RetrievalTask.Id);
                        ActiveCellAuthenticationItem = new PalletAuthenticationItem(
                            taskToAuthAtCell.PalletDetails,
                            taskToAuthAtCell.RetrievalTask,
                            AuthenticationContextMode.Retrieval);
                        IsCellAuthenticationViewActive = true;
                    }
                }
                else if (ActiveCellAuthenticationItem != null) // If there's an active cell auth item
                {
                    // And we are no longer at its cell OR the task is no longer in 'retrieval' state
                    bool stillAtAuthCell = ActiveCellAuthenticationItem.OriginalTask?.SourceCell?.Level == _currentCellLevel &&
                                           ActiveCellAuthenticationItem.OriginalTask?.SourceCell?.Position == _currentCellPosition;
                    bool taskStillAwaitingAuth = ActiveCellAuthenticationItem.OriginalTask?.ActiveTaskStatus == ActiveTaskStatus.retrieval;

                    if (!stillAtAuthCell || !taskStillAwaitingAuth)
                    {
                        _logger.LogInformation("Moved away from cell L{Level}-P{Position} for which auth item {TaskId} was active, or task status changed. Clearing cell auth view. StillAtAuthCell: {StillAtAuthCell}, TaskStillAwaitingAuth: {TaskStillAwaitingAuth}",
                                               ActiveCellAuthenticationItem.OriginalTask?.SourceCell?.Level, ActiveCellAuthenticationItem.OriginalTask?.SourceCell?.Position, ActiveCellAuthenticationItem.OriginalTask?.Id, stillAtAuthCell, taskStillAwaitingAuth);
                        ActiveCellAuthenticationItem = null;
                        IsCellAuthenticationViewActive = false;
                    }
                }
            }
            
            OnPropertyChanged(nameof(CurrentFingerDisplayName)); // Ensure this is raised after changes
            OnPropertyChanged(nameof(CurrentCellDisplayName));   // Ensure this is raised after changes
            OnPropertyChanged(nameof(IsFingerAuthenticationViewActive));
            OnPropertyChanged(nameof(IsCellAuthenticationViewActive));
            // Notify that task panel visibility properties might have changed
            OnPropertyChanged(nameof(ShouldShowTasksPanel));
            OnPropertyChanged(nameof(ShouldShowDefaultPhoto));

            // Check for active navigation completion
            if (_activeNavigationTcs != null && !_activeNavigationTcs.Task.IsCompleted &&
                positionValue == _activeNavigationTargetPosition &&
                this.OpcVM.CurrentStatus == _activeNavigationTargetStatus) // Check current status from OpcVM
            {
                _logger.LogInformation($"Navigation target L{level}-P{position} reached on position change, status is '{this.OpcVM.CurrentStatus}'. Completing navigation task.");
                _activeNavigationTcs.TrySetResult(true);
            }
        }

        private void OnTrolleyStatusChanged(object sender, string newStatus)
        {
            if (_activeNavigationTcs != null && !_activeNavigationTcs.Task.IsCompleted &&
                this.OpcVM.PositionPV == _activeNavigationTargetPosition &&
                newStatus == _activeNavigationTargetStatus)
            {
                _logger.LogInformation($"Navigation target L{this.OpcVM.PvLevel}-P{this.OpcVM.PvLocation} reached and status is '{newStatus}'. Completing navigation task.");
                _activeNavigationTcs.TrySetResult(true);
            }
        }
    }
}
