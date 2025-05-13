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
                    int? level = destinationCell.HeightLevel;
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
                    int? level = sourceCell.HeightLevel;
                    int? position = sourceCell.Position;

                    if (!level.HasValue || !position.HasValue) continue;

                    int expectedPositionValue = (level.Value * 100) + position.Value;

                    // If we've arrived at the source position
                    if (positionValue == expectedPositionValue)
                    {
                        _logger.LogInformation("Arrived at source for Task ID: {TaskId}, updating status to 'retrieving'.",
                            item.RetrievalTask.Id);

                        // Update the task status to retrieving (arrived at source, ready to load)
                        item.RetrievalTask.ActiveTaskStatus = ActiveTaskStatus.storing; // Reusing 'storing' status for now
                        OnPropertyChanged(nameof(PalletsForRetrieval));

                        // Show a notification to the user
                        _dispatcherService.Invoke(() =>
                        {
                            MessageBox.Show($"Arrived at source cell {sourceCell.DisplayName} for pallet {item.PalletDetails.UldCode}. " +
                                           "Ready to load the pallet.",
                                           "Arrived at Source",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Information);
                        });
                    }
                }
            }
        }
    }
}
