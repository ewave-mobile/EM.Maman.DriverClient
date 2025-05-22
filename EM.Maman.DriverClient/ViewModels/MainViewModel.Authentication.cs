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
                        predicate: t => t.StorageSourceFingerId == capturedFingerId &&
                                        t.TaskTypeId == (int)Models.Enums.TaskType.Storage &&
                                        (t.IsExecuted == false || t.IsExecuted == null),
                        include: q => q.Include(t => t.StorageDestinationCell) // Include the correct destination cell for storage
                                        // Pallet is fetched separately below, so no direct include here unless DB structure changes
                    );

                    foreach (var task in dbTasks)
                    {
                        Pallet pallet = null;
                        if (task.PalletId != null)
                        {
                            pallet = (await unitOfWork.Pallets.FindAsync(p => p.Id == task.PalletId)).FirstOrDefault();
                            if (pallet == null)
                            {
                                _logger.LogWarning("Could not find Pallet with Id {PalletId} for Storage Task {TaskId}", task.PalletId, task.Id);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Storage Task {TaskId} has null PalletId.", task.Id);
                        }
                        
                        if (pallet != null)
                        {
                            // Use the simpler FromDbModel that relies on included navigation properties
                            // StorageSourceFinger is implicitly 'finger' (capturedFinger) due to the predicate
                            // StorageDestinationCell is included above
                            var taskDetails = TaskDetails.FromDbModel(task); // This will use task.StorageSourceFinger and task.StorageDestinationCell
                            if (taskDetails != null)
                            {
                                taskDetails.Pallet = pallet; // Assign the separately fetched pallet
                                // Ensure SourceFinger is explicitly set if FromDbModel(task) doesn't set it from context
                                // (though it should if StorageSourceFingerId is used in predicate and StorageSourceFinger is included or implicitly linked)
                                if (taskDetails.SourceFinger == null && capturedFinger != null)
                                {
                                     taskDetails.SourceFinger = capturedFinger; // Ensure source finger is the current finger
                                }
                                itemsToAdd.Add(new PalletAuthenticationItem(pallet, taskDetails)); // Default AuthContextMode is Storage
                            }
                            else
                            {
                                _logger.LogWarning("TaskDetails.FromDbModel(task) returned null for Storage Task ID {TaskId}", task.Id);
                            }
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
                DataContext = dialogVM
                // Owner will be set below after checks
            };

            var owner = Application.Current.MainWindow;

            _logger.LogInformation("Attempting to show AuthenticationDialog. Current Application.MainWindow is '{OwnerType}', IsNull: {IsNull}, IsLoaded: {IsLoaded}, IsVisible: {IsVisible}, IsActive: {IsActive}. Dialog instance hash: {DialogHash}",
                owner?.GetType().FullName ?? "null",
                owner == null,
                owner?.IsLoaded,
                owner?.IsVisible,
                owner?.IsActive,
                dialog.GetHashCode());

            if (owner != null && owner != dialog && owner.IsLoaded) // owner.IsVisible might also be a good check
            {
                dialog.Owner = owner;
                _logger.LogInformation("Set Owner of AuthenticationDialog to '{OwnerType}'.", owner.GetType().FullName);
            }
            else if (owner == dialog)
            {
                _logger.LogWarning("Attempted to set dialog Owner to itself. Owner will not be set for dialog hash: {DialogHash}.", dialog.GetHashCode());
            }
            else
            {
                _logger.LogWarning("Application.Current.MainWindow ('{OwnerType}') is not suitable to be an owner (IsNull: {IsNull}, IsLoaded: {IsLoaded}, IsVisible: {IsVisible}, IsActive: {IsActive}). Dialog hash: {DialogHash}. Dialog will be shown without an owner.",
                    owner?.GetType().FullName ?? "null",
                    owner == null,
                    owner?.IsLoaded,
                    owner?.IsVisible,
                    owner?.IsActive,
                    dialog.GetHashCode());
                // Fallback: try to find any active, visible window that is not the dialog itself
                var fallbackOwner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible && w != dialog);
                if (fallbackOwner != null)
                {
                    dialog.Owner = fallbackOwner;
                    _logger.LogInformation("Set Owner of AuthenticationDialog to fallback owner '{OwnerType}'.", fallbackOwner.GetType().FullName);
                }
                else
                {
                    _logger.LogWarning("No suitable fallback owner found for AuthenticationDialog.");
                }
            }

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
                    PalletsToAuthenticate.Remove(itemToAuth); // Remove from finger auth list if present
                    if (PalletsToAuthenticate.Contains(itemToAuth)) 
                    {
                        if (!PalletsToAuthenticate.Any() && _currentFingerPositionValue.HasValue)
                        {
                            IsFingerAuthenticationViewActive = false;
                            _logger.LogInformation("Last pallet authenticated at current finger. Setting IsFingerAuthenticationViewActive to false.");
                        }
                    }
                    
                    if (ActiveCellAuthenticationItem == itemToAuth) // If it was the cell auth item
                    {
                        ActiveCellAuthenticationItem = null;
                        IsCellAuthenticationViewActive = false;
                        _logger.LogInformation("Cell authentication item processed. Setting IsCellAuthenticationViewActive to false.");
                    }
                });

                bool loaded = false; // Initialize loaded flag
                var pallet = itemToAuth.PalletDetails;

                if (pallet != null && TrolleyVM != null)
                {
                    // Attempt to load pallet, regardless of finger or cell context, if it's a retrieval task or storage task needing loading
                    // For Storage (from finger): itemToAuth.OriginalTask.SourceFinger will be non-null
                    // For Retrieval (from cell): itemToAuth.OriginalTask.SourceCell will be non-null
                    
                    var originalTask = itemToAuth.OriginalTask;

                    if (originalTask != null && (originalTask.TaskType == Models.Enums.TaskType.Storage || originalTask.TaskType == Models.Enums.TaskType.Retrieval))
                    {
                        if (originalTask.TaskType == Models.Enums.TaskType.Retrieval)
                        {
                            // For Retrieval, prioritize Right Cell
                            if (!TrolleyVM.RightCell.IsOccupied)
                            {
                                await TrolleyOperationsVM.AddPalletToTrolleyRightCellAsync(pallet);
                                _logger.LogInformation("Loaded authenticated retrieval pallet {UldCode} onto Right Trolley Cell.", pallet.UldCode);
                                loaded = true;
                            }
                            else if (!TrolleyVM.LeftCell.IsOccupied)
                            {
                                await TrolleyOperationsVM.AddPalletToTrolleyLeftCellAsync(pallet);
                                _logger.LogInformation("Right Trolley Cell occupied. Loaded authenticated retrieval pallet {UldCode} onto Left Trolley Cell.", pallet.UldCode);
                                loaded = true;
                            }
                        }
                        else // For Storage (or other types if generalized further)
                        {
                            // Default: try left cell, then right cell.
                            if (!TrolleyVM.LeftCell.IsOccupied)
                            {
                                await TrolleyOperationsVM.AddPalletToTrolleyLeftCellAsync(pallet);
                                _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Left Trolley Cell.", pallet.UldCode);
                                loaded = true;
                            }
                            else if (!TrolleyVM.RightCell.IsOccupied)
                            {
                                await TrolleyOperationsVM.AddPalletToTrolleyRightCellAsync(pallet);
                                _logger.LogInformation("Left Trolley Cell occupied. Loaded authenticated pallet {UldCode} onto Right Trolley Cell.", pallet.UldCode);
                                loaded = true;
                            }
                        }

                        if (!loaded)
                        {
                            _logger.LogWarning("Could not load authenticated pallet {UldCode} onto trolley - both cells occupied.", pallet.UldCode);
                            MessageBox.Show($"Cannot load pallet {pallet.UldCode}. Trolley is full.", "Trolley Full", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        // If pallet was loaded successfully, update task status and DB records
                        if (loaded)
                        {
                            if (originalTask.TaskType == Models.Enums.TaskType.Retrieval)
                            {
                                // Remove from PalletInCell if retrieved from a cell
                                if (originalTask.SourceCell != null)
                                {
                                    using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                                    {
                                        var palletInSourceCell = await uow.PalletInCells.GetByPalletAndCellAsync(pallet.Id, originalTask.SourceCell.Id);
                                        if (palletInSourceCell != null)
                                        {
                                            uow.PalletInCells.Remove(palletInSourceCell);
                                            await uow.CompleteAsync();
                                            _logger.LogInformation("Pallet {PalletId} removed from source cell {SourceCellId} record after loading to trolley.", pallet.Id, originalTask.SourceCell.Id);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Could not find PalletInCell record for Pallet {PalletId} in SourceCell {SourceCellId} to remove.", pallet.Id, originalTask.SourceCell.Id);
                                        }
                                    }
                                }

                                originalTask.Status = Models.Enums.TaskStatus.InProgress;
                                originalTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.transit; // To destination
                                await TaskVM.SaveTaskToDatabase(originalTask);
                                _logger.LogInformation("Retrieval Task {TaskId} status updated to InProgress/Transit after pallet load.", originalTask.Id);
                                TaskVM.InitializeTaskWorkflow(); // Initialize workflow for delivery steps
                            }
                            else if (originalTask.TaskType == Models.Enums.TaskType.Storage)
                            {
                                _logger.LogInformation("Storage pallet {UldCode} loaded for Task {TaskId}.", pallet.UldCode, originalTask.Id);
                            }
                        }
                    }
                }


                // Decide how to proceed based on the authentication context / task type
                if (itemToAuth.AuthContextMode == AuthenticationContextMode.Storage || 
                    (itemToAuth.OriginalTask != null && itemToAuth.OriginalTask.TaskType == Models.Enums.TaskType.Storage))
                {
                    if (loaded) // Ensure pallet is loaded before creating storage task
                    {
                        await this.CreateStorageTaskFromAuthenticationAsync(itemToAuth);
                    }
                    else
                    {
                        _logger.LogWarning("Storage authentication for Task {TaskId} successful, but pallet was not loaded. Cannot create storage task.", 
                            itemToAuth.OriginalTask?.Id.ToString() ?? "N/A");
                        // Potentially re-activate finger auth view if it was cleared prematurely
                        if (itemToAuth.OriginalTask?.SourceFinger != null && !IsFingerAuthenticationViewActive)
                        {
                            // IsFingerAuthenticationViewActive = true; // This might be complex to re-trigger correctly
                        }
                    }
                }
                else if (itemToAuth.AuthContextMode == AuthenticationContextMode.Retrieval ||
                         (itemToAuth.OriginalTask != null && itemToAuth.OriginalTask.TaskType == Models.Enums.TaskType.Retrieval))
                {
                    if (loaded) // Pallet must be loaded to proceed with retrieval delivery
                    {
                        await this.ProcessAuthenticatedRetrievalAsync(itemToAuth);
                    }
                    else
                    {
                        _logger.LogWarning("Retrieval authentication for Task {TaskId} successful, but pallet was not loaded. Cannot proceed to delivery.", 
                            itemToAuth.OriginalTask?.Id.ToString() ?? "N/A");
                        // If ActiveCellAuthenticationItem was itemToAuth and was cleared, we might need to re-set it or handle this state.
                        // For now, IsCellAuthenticationViewActive might have been set to false.
                        // Consider re-showing auth if pallet loading failed.
                        if (ActiveCellAuthenticationItem == null && IsCellAuthenticationViewActive == false && itemToAuth == itemToAuth) // Re-check if it was the cell item
                        {
                           // ActiveCellAuthenticationItem = itemToAuth; // This would require itemToAuth to persist
                           // IsCellAuthenticationViewActive = true;
                           // _logger.LogInformation("Re-activating cell authentication view for Task {TaskId} due to loading failure.", itemToAuth.OriginalTask.Id);
                        }
                    }
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
            bool trolleyAtSourceCell = false;
            if (retrievalItem.RetrievalTask.SourceCell != null && 
                retrievalItem.RetrievalTask.SourceCell.Level.HasValue && 
                retrievalItem.RetrievalTask.SourceCell.Position.HasValue)
            {
                // _currentCellLevel and _currentCellPosition are updated by OnPositionChanged
                trolleyAtSourceCell = _currentCellLevel == retrievalItem.RetrievalTask.SourceCell.Level.Value &&
                                      _currentCellPosition == retrievalItem.RetrievalTask.SourceCell.Position.Value &&
                                      !_currentFingerPositionValue.HasValue; // Ensure not at a finger
            }
            
            return isRetrievalTask && isAtSourceCellPhase && trolleyAtSourceCell;
        }

        #endregion
    }
}
