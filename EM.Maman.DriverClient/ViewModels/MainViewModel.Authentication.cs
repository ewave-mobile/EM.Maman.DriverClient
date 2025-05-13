using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.DisplayModels;
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
                        if (!string.IsNullOrEmpty(task.PalletId))
                        {
                            // Try to parse task.PalletId as integer to match Pallet.Id
                            int palletId;
                            if (int.TryParse(task.PalletId, out palletId))
                            {
                                // Search by Pallet.Id instead of UldCode
                                pallet = (await unitOfWork.Pallets.FindAsync(p => p.Id == palletId)).FirstOrDefault();
                                if (pallet == null)
                                {
                                    _logger.LogWarning("Could not find Pallet with Id {PalletId} for Task {TaskId}", palletId, task.Id);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Could not parse PalletId {PalletId} to integer for Task {TaskId}", task.PalletId, task.Id);
                            }
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
                    IsFingerAuthenticationViewActive = PalletsToAuthenticate.Any();
                    _logger.LogInformation("Loaded {Count} pallets for authentication at finger {FingerId}.",
                        PalletsToAuthenticate.Count, capturedFingerId);
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

        private async void ConfirmAuthentication(AuthenticationDialogViewModel dialogVM, PalletAuthenticationItem itemToAuth)
        {
            if (itemToAuth?.PalletDetails == null)
            {
                _logger.LogError("ConfirmAuthentication called with invalid item.");
                return;
            }

            if (string.Equals(dialogVM.EnteredUldCode?.Trim(), itemToAuth.PalletDetails.ImportUnit?.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Authentication successful for Pallet ULD: {UldCode}",
                    itemToAuth.PalletDetails.UldCode);
                MessageBox.Show($"Pallet {itemToAuth.PalletDetails.UldCode} authenticated successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                _dispatcherService.Invoke(() => PalletsToAuthenticate.Remove(itemToAuth));

                var finger = itemToAuth.OriginalTask?.SourceFinger;
                var pallet = itemToAuth.PalletDetails;
                if (finger != null && pallet != null && TrolleyVM != null)
                {
                    bool loaded = false;
                    if (finger.Side == 0)
                    {
                        if (!TrolleyVM.LeftCell.IsOccupied)
                        {
                            TrolleyVM.LoadPalletIntoLeftCell(pallet);
                            _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Left Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                        else if (!TrolleyVM.RightCell.IsOccupied)
                        {
                            TrolleyVM.LoadPalletIntoRightCell(pallet);
                            _logger.LogInformation("Left Trolley Cell occupied. Loaded authenticated pallet {UldCode} onto Right Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                    }
                    else
                    {
                        if (!TrolleyVM.RightCell.IsOccupied)
                        {
                            TrolleyVM.LoadPalletIntoRightCell(pallet);
                            _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Right Trolley Cell.",
                                pallet.UldCode);
                            loaded = true;
                        }
                        else if (!TrolleyVM.LeftCell.IsOccupied)
                        {
                            TrolleyVM.LoadPalletIntoLeftCell(pallet);
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

                await CreateStorageTaskFromAuthenticationAsync(itemToAuth);
            }
            else
            {
                _logger.LogWarning("Authentication failed for Pallet ULD: {UldCode}.",
                    itemToAuth.PalletDetails.UldCode);
                MessageBox.Show($"Authentication failed. Entered ID '{dialogVM.EnteredUldCode}' does not match.",
                    "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}
