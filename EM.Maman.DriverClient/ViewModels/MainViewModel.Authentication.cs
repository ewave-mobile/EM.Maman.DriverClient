using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.Enums; // Added for UpdateType
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DbTask = EM.Maman.Models.LocalDbModels.Task;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        #region Authentication Methods

        private async Task<bool> IsFingerLocationAsync(int positionValue)
        {
            try
            {
                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var finger = (await unitOfWork.Fingers.FindAsync(f => f.Position == positionValue)).FirstOrDefault();
                    return finger != null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if position {PositionValue} is a finger location.", positionValue);
                return false;
            }
        }

        private async System.Threading.Tasks.Task LoadPalletsForFingerAuthenticationAsync(int fingerPositionValue)
        {
            IsFingerAuthenticationViewActive = false;
            var itemsToAdd = new List<PalletAuthenticationItem>();

            try
            {
                // Variables to capture outside the using block
                long capturedFingerId = 0;
                Finger capturedFinger = null;
                
                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var finger = (await unitOfWork.Fingers.FindAsync(f => f.Position == fingerPositionValue)).FirstOrDefault();
                    if (finger == null)
                    {
                        _logger.LogWarning("Could not find Finger entity for position value {PositionValue}.", fingerPositionValue);
                        _dispatcherService.Invoke(() =>
                        {
                            PalletsToAuthenticate.Clear();
                            PalletsReadyForStorage.Clear();
                        });
                        return;
                    }

                    // Capture these values for use outside the using block
                    capturedFingerId = finger.Id;
                    capturedFinger = finger;

                    var dbTasks = await unitOfWork.Tasks.FindAsync(
                        predicate: t => t.FingerLocationId == capturedFingerId && (t.IsExecuted == false || t.IsExecuted == null),
                        include: q => q.Include(t => t.CellEndLocation)
                    );

                    foreach (var task in dbTasks)
                    {
                        Pallet pallet = null;
                        
                            // Try to parse task.PalletId as integer to match Pallet.Id
                            
                            if (task.PalletId != null)
                            {
                                // Search by Pallet.Id instead of UldCode
                                pallet = (await unitOfWork.Pallets.FindAsync(p => p.Id == task.PalletId)).FirstOrDefault();
                                if (pallet == null)
                                {
                                    _logger.LogWarning("Could not find Pallet with Id {PalletId} for Task {TaskId}", task.PalletId, task.Id);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Could not parse PalletId {PalletId} to integer for Task {TaskId}", task.PalletId, task.Id);
                            }
                        

                        if (pallet != null)
                        {
                            var taskDetails = TaskDetails.FromDbModel(task, pallet, finger, null, task.CellEndLocation);
                            itemsToAdd.Add(new PalletAuthenticationItem(pallet, taskDetails));
                        }
                        else
                        {
                            _logger.LogWarning("Task {TaskId} from finger {FingerId} is missing Pallet information.", task.Id, capturedFingerId);
                        }
                    }
                }
                
                _dispatcherService.Invoke(() =>
                {
                    PalletsToAuthenticate.Clear();
                    foreach (var item in itemsToAdd)
                    {
                        item.AuthenticateCommand = ShowAuthenticationDialogCommand;
                        PalletsToAuthenticate.Add(item);
                    }
                    // Check if the finger for which these pallets were loaded (fingerPositionValue)
                    // is still considered the current active finger by the MainViewModel.
                    // _currentFingerPositionValue reflects the MainViewModel's latest understanding
                    // of the trolley's finger location from OnPositionChanged.
                    if (_currentFingerPositionValue == fingerPositionValue)
                    {
                        IsFingerAuthenticationViewActive = true;
                        _logger.LogInformation("Loaded {Count} pallets for authentication at finger {FingerId} (Pos: {FingerPos}). View Active.",
                            PalletsToAuthenticate.Count, capturedFingerId, fingerPositionValue);
                    }
                    else
                    {
                        // Trolley has moved on from this finger, or this finger was never fully "activated"
                        // in MainViewModel's state. Do not activate the view.
                        // The OnPositionChanged logic for the new location should handle visibility.
                        _logger.LogInformation("Finger auth view for finger {FingerId} (Pos: {OriginalFingerPos}) not activated because current active finger is now {CurrentActiveFingerPos}.",
                            capturedFingerId, fingerPositionValue, _currentFingerPositionValue);
                        // Optionally, ensure it's false if there's any doubt, though OnPositionChanged should handle it.
                        // IsFingerAuthenticationViewActive = false;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pallets for finger authentication.");
                _dispatcherService.Invoke(() =>
                {
                    PalletsToAuthenticate.Clear();
                    PalletsReadyForStorage.Clear();
                    OnPropertyChanged(nameof(HasPalletsReadyForStorage));
                });
                MessageBox.Show($"Error loading pallets for authentication: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowAuthenticationDialog(object parameter)
        {
            if (parameter is not PalletAuthenticationItem item) return;

            _logger.LogInformation("Showing authentication dialog for Pallet ULD: {UldCode}",
                item.PalletDetails?.UldCode ?? "N/A");

            var dialogVM = new AuthenticationDialogViewModel(item);
            var dialog = new AuthenticationDialog
            {
                DataContext = dialogVM,
                Owner = Application.Current.MainWindow
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _logger.LogInformation("Authentication dialog confirmed for Pallet ULD: {UldCode}.",
                    item.PalletDetails?.UldCode ?? "N/A");
                ConfirmAuthentication(dialogVM, item);
            }
            else
            {
                _logger.LogInformation("Authentication dialog cancelled for Pallet ULD: {UldCode}",
                    item.PalletDetails?.UldCode ?? "N/A");
            }
        }

        private async System.Threading.Tasks.Task ConfirmAuthentication(AuthenticationDialogViewModel dialogVM, PalletAuthenticationItem itemToAuth)
        {
            if (itemToAuth?.PalletDetails == null)
            {
                _logger.LogError("ConfirmAuthentication called with invalid item.");
                return;
            }

            bool isAuthenticated = false;
            string expectedIdentifierValue = "";
            string identifierTypeForMessage = "Pallet ID";

            if (itemToAuth.PalletDetails.UpdateType == UpdateType.Import)
            {
                expectedIdentifierValue = itemToAuth.PalletDetails.ImportUnit?.Trim();
                identifierTypeForMessage = "Pallet ID (Import Unit)";
                isAuthenticated = string.Equals(dialogVM.EnteredUldCode?.Trim(), expectedIdentifierValue, StringComparison.OrdinalIgnoreCase);
            }
            else if (itemToAuth.PalletDetails.UpdateType == UpdateType.Export)
            {
                expectedIdentifierValue = itemToAuth.PalletDetails.ExportAwbNumber?.Trim();
                identifierTypeForMessage = "AWB Number";
                isAuthenticated = string.Equals(dialogVM.EnteredUldCode?.Trim(), expectedIdentifierValue, StringComparison.OrdinalIgnoreCase);
            }
            // else: Current logic assumes UpdateType will be one of these for authenticated items.
            // If PalletDetails.UpdateType is null, isAuthenticated will remain false.

            if (isAuthenticated)
            {
                _logger.LogInformation("Authentication successful for Pallet ULD: {UldCode} using {IdentifierType}: {EnteredCode}",
                    itemToAuth.PalletDetails.UldCode, identifierTypeForMessage, dialogVM.EnteredUldCode);
                MessageBox.Show($"Pallet {itemToAuth.PalletDetails.DisplayName ?? itemToAuth.PalletDetails.UldCode} authenticated successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                _dispatcherService.Invoke(() => 
                {
                    PalletsToAuthenticate.Remove(itemToAuth);
                    // If no more pallets to authenticate at this finger,
                    // and we are still at this finger, the authentication specific view can be turned off.
                    // The ShouldShowTasksPanel will still be true if new storage/retrieval tasks are created.
                    if (!PalletsToAuthenticate.Any() && _currentFingerPositionValue.HasValue)
                    {
                        IsFingerAuthenticationViewActive = false;
                        _logger.LogInformation("Last pallet authenticated at current finger. Setting IsFingerAuthenticationViewActive to false.");
                    }
                });

                var finger = itemToAuth.OriginalTask?.SourceFinger;
                var pallet = itemToAuth.PalletDetails;
                if (finger != null && pallet != null && TrolleyVM != null)
                {
                    bool loaded = false;
                    if (finger.Side == 0) // Assuming 0 is Left
                    {
                        if (!TrolleyVM.LeftCell.IsOccupied)
                        {
                            await TrolleyOperationsVM.AddPalletToTrolleyLeftCellAsync(pallet);
                            _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Left Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                        else if (!TrolleyVM.RightCell.IsOccupied)
                        {
                            await TrolleyOperationsVM.AddPalletToTrolleyRightCellAsync(pallet);
                            _logger.LogInformation("Left Trolley Cell occupied. Loaded authenticated pallet {UldCode} onto Right Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                    }
                    else // Assuming non-0 is Right
                    {
                        if (!TrolleyVM.RightCell.IsOccupied)
                        {
                            await TrolleyOperationsVM.AddPalletToTrolleyRightCellAsync(pallet);
                            _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Right Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                        else if (!TrolleyVM.LeftCell.IsOccupied)
                        {
                            await TrolleyOperationsVM.AddPalletToTrolleyLeftCellAsync(pallet);
                            _logger.LogInformation("Right Trolley Cell occupied. Loaded authenticated pallet {UldCode} onto Left Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                    }

                    if (!loaded)
                    {
                        _logger.LogWarning("Could not load authenticated pallet {UldCode} onto trolley - both cells occupied.",
                            pallet.UldCode);
                        MessageBox.Show($"Cannot load pallet {pallet.UldCode}. Trolley is full.",
                            "Trolley Full", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Decide how to proceed based on the authentication context / task type
                if (itemToAuth.AuthContextMode == AuthenticationContextMode.Storage || 
                    (itemToAuth.OriginalTask != null && itemToAuth.OriginalTask.TaskType == Models.Enums.TaskType.Storage))
                {
                    await CreateStorageTaskFromAuthenticationAsync(itemToAuth);
                }
                else if (itemToAuth.AuthContextMode == AuthenticationContextMode.Retrieval ||
                         (itemToAuth.OriginalTask != null && itemToAuth.OriginalTask.TaskType == Models.Enums.TaskType.Retrieval))
                {
                    // This method will be created in MainViewModel.TaskOperations.cs
                    await ProcessAuthenticatedRetrievalAsync(itemToAuth); 
                }
                else
                {
                    // Fallback or error if context is unclear, though PalletAuthenticationItem constructor now defaults mode.
                    _logger.LogWarning("Authenticated item {PalletUld} has unclear context/task type. Cannot proceed with task creation/processing.", 
                        itemToAuth.PalletDetails?.UldCode ?? "N/A");
                    MessageBox.Show("Authentication context is unclear. Cannot proceed.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                _logger.LogWarning("Authentication failed for Pallet ULD: {UldCode}. Entered: '{EnteredCode}', Expected {IdentifierType}: '{ExpectedValue}'",
                    itemToAuth.PalletDetails.UldCode, dialogVM.EnteredUldCode, identifierTypeForMessage, expectedIdentifierValue);
                MessageBox.Show($"Authentication failed. Entered ID '{dialogVM.EnteredUldCode}' does not match the expected {identifierTypeForMessage}.",
                    "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteAuthenticatePalletAtCell(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem retrievalItem || retrievalItem.RetrievalTask == null || retrievalItem.PalletDetails == null)
            {
                _logger.LogWarning("ExecuteAuthenticatePalletAtCell called with invalid parameter or missing details.");
                MessageBox.Show("Cannot initiate authentication: Task or pallet details are missing.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _logger.LogInformation("Initiating authentication for pallet {PalletUld} at source cell for retrieval task ID {TaskId}.",
                retrievalItem.PalletDetails.UldCode, retrievalItem.RetrievalTask.Id);

            // Create a PalletAuthenticationItem specifically for this cell-based retrieval authentication
            var authItem = new PalletAuthenticationItem(
                retrievalItem.PalletDetails,
                retrievalItem.RetrievalTask,
                AuthenticationContextMode.Retrieval // Explicitly set mode
            );

            // Call the existing dialog showing logic
            ExecuteShowAuthenticationDialog(authItem);
        }

        private bool CanExecuteAuthenticatePalletAtCell(object parameter)
        {
            if (parameter is not PalletRetrievalTaskItem retrievalItem || retrievalItem.RetrievalTask == null || retrievalItem.PalletDetails == null)
            {
                return false;
            }
            // Only allow if the task is a retrieval task and is currently at its source cell (not yet in transit to destination)
            // and the trolley is physically at the source cell.
            bool isRetrievalTask = retrievalItem.RetrievalTask.TaskType == Models.Enums.TaskType.Retrieval;
            bool isAtSourceCellPhase = retrievalItem.RetrievalTask.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.retrieval || 
                                     retrievalItem.RetrievalTask.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.pending; // Or whatever status indicates it's at source cell awaiting auth

            // This needs a robust check that the trolley is actually at retrievalItem.RetrievalTask.SourceCell
            // For now, we assume if this command is available on UI, the trolley is at the correct cell.
            // A more precise check:
            // bool trolleyAtSourceCell = _currentCellLevel == retrievalItem.RetrievalTask.SourceCell?.Level &&
            //                            _currentCellPosition == retrievalItem.RetrievalTask.SourceCell?.Position &&
            //                            !_currentFingerPositionValue.HasValue; // Ensure not at a finger

            return isRetrievalTask && isAtSourceCellPhase; // && trolleyAtSourceCell; (add when trolley position is reliably checked)
        }

        #endregion
    }
}
