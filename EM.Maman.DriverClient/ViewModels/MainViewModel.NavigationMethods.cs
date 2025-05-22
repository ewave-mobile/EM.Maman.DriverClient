using EM.Maman.Models.Enums;
using EM.Maman.Models.LocalDbModels; // Added for Finger
using Microsoft.Extensions.Logging; // Added for ILogger extensions
using System;                       // Added for Exception
using System.Windows;               // Added for MessageBox

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing navigation methods for MainViewModel
    public partial class MainViewModel
    {
        /// <summary>
        /// Switch to the warehouse view
        /// </summary>
        private void ShowWarehouseView()
        {
            IsWarehouseViewActive = true;
            (_showWarehouseViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Switch to the tasks view
        /// </summary>
        private void ShowTasksView()
        {
            IsWarehouseViewActive = false;
            (_showWarehouseViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        /// <summary>
        /// Switch to the map view within the tasks view
        /// </summary>
        private void ShowMapView()
        {
            IsMapViewActive = true;
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        /// <summary>
        /// Switch to the tasks list view within the tasks view
        /// </summary>
        private void ShowTasksListView()
        {
            IsMapViewActive = false;
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Removed CanNavigateToFinger and NavigateToFinger methods

        private void CheckForArrivalAtDestination(int positionValue)
        {
            // Check for storage tasks
            if (HasPalletsReadyForStorage)
            {
                // Find storage tasks in transit that match this destination
                foreach (var item in PalletsReadyForStorage.ToList())
                {
                    if (item.StorageTask.ActiveTaskStatus != ActiveTaskStatus.transit) continue;

                    var destinationCell = item.StorageTask.DestinationCell;
                    if (destinationCell == null) continue;

                    // Calculate the expected position value for the destination
                    int? level = destinationCell.Level;
                    int? position = destinationCell.Position;

                    if (!level.HasValue || !position.HasValue) continue;

                    int expectedPositionValue = (level.Value * 100) + position.Value;

                    // If we've arrived at the destination position
                    if (positionValue == expectedPositionValue)
                    {
                        _logger.LogInformation("Arrived at destination for Task ID: {TaskId}, updating status to 'storage'.",
                            item.StorageTask.Id);

                        // Update the task status to storage (arrived at destination, ready to unload)
                        item.StorageTask.ActiveTaskStatus = ActiveTaskStatus.storing;
                        OnPropertyChanged(nameof(PalletsReadyForStorage));

                        // Show a notification to the user
                        _dispatcherService.Invoke(() =>
                        {
                            MessageBox.Show($"Arrived at destination cell {destinationCell.DisplayName} for pallet {item.PalletDetails.UldCode}. " +
                                           "Ready to unload the pallet.",
                                           "Arrived at Destination",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Information);
                        });
                    }
                }
            }

            // Check for retrieval tasks
            if (HasPalletsForRetrieval)
            {
                // Find retrieval tasks in transit that match this source
                foreach (var item in PalletsForRetrieval.ToList())
                {
                    if (item.RetrievalTask.ActiveTaskStatus != ActiveTaskStatus.transit) continue;

                    var sourceCell = item.RetrievalTask.SourceCell;
                    if (sourceCell == null) continue;

                    // Calculate the expected position value for the source
                    int? level = sourceCell.Level;
                    int? position = sourceCell.Position;

                    if (!level.HasValue || !position.HasValue) continue;

                    int expectedPositionValue = (level.Value * 100) + position.Value;

                    // If we've arrived at the source position
                    if (positionValue == expectedPositionValue)
                    {
                        _logger.LogInformation("Arrived at source for Task ID: {TaskId}, updating status to 'retrieval'.",
                            item.RetrievalTask.Id);

                        // Update the task status to retrieval (arrived at source, ready for authentication/load)
                        item.RetrievalTask.ActiveTaskStatus = ActiveTaskStatus.retrieval;
                        OnPropertyChanged(nameof(PalletsForRetrieval)); // Notify UI about status change

                        _logger.LogInformation("Arrived at source cell {SourceCellName} for Task ID {TaskId}. Preparing for cell authentication.",
                                             sourceCell.DisplayName, item.RetrievalTask.Id);

                        CurrentCellDisplayName = sourceCell.DisplayName; // Update current cell display name

                        // Create PalletAuthenticationItem for the UI
                        var authItemForCell = new PalletAuthenticationItem(
                            item.PalletDetails,
                            item.RetrievalTask,
                            AuthenticationContextMode.Retrieval // Explicitly set mode
                        );
                        // Wire up the command that PalletAuthenticationItemControl will use
                        authItemForCell.AuthenticateCommand = this.ShowAuthenticationDialogCommand;

                        ActiveCellAuthenticationItem = authItemForCell;
                        IsCellAuthenticationViewActive = true; // Activate the new UI section

                        // Ensure finger auth is not active if we just arrived at a cell
                        if (IsFingerAuthenticationViewActive)
                        {
                            IsFingerAuthenticationViewActive = false;
                        }
                        if (PalletsToAuthenticate.Any())
                        {
                            _dispatcherService.Invoke(() => PalletsToAuthenticate.Clear());
                        }
                        
                        // Notify that task panel visibility might change due to IsCellAuthenticationViewActive
                        OnPropertyChanged(nameof(ShouldShowTasksPanel));
                        OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
                    }
                }
            }
        }
    }
}
