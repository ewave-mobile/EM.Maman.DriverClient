using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq; // Added for FirstOrDefault

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        private async Task<int?> GetActiveTrolleyIdFromConfigurationAsync()
        {
            _logger.LogInformation("Fetching active trolley ID from configuration...");
            try
            {
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Assuming there's only one configuration entry after initial setup
                    var config = (await unitOfWork.Configurations.GetAllAsync()).FirstOrDefault();
                    if (config != null && config.ActiveTrolleyId > 0) // Check if ActiveTrolleyId is valid
                    {
                        _logger.LogInformation("Found active trolley ID: {TrolleyId} for workstation: {WorkstationType}", config.ActiveTrolleyId, config.WorkstationType);
                        return config.ActiveTrolleyId;
                    }
                    _logger.LogWarning("No active trolley ID found in configuration or configuration missing. Workstation might not be initialized.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active trolley ID from configuration.");
                return null;
            }
        }

        private async Task<bool> LoadAndSetMainTrolleyAsync()
        {
            int? activeTrolleyId = await GetActiveTrolleyIdFromConfigurationAsync();

            if (!activeTrolleyId.HasValue)
            {
                _logger.LogCritical("Failed to retrieve active trolley ID from configuration. Application cannot determine which trolley to load.");
                MessageBox.Show($"Critical Error: Workstation not properly initialized or configuration is missing/corrupted. Active trolley ID could not be determined. Please run first-time setup from the Login screen.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            _logger.LogInformation("Attempting to load main trolley with ID {TrolleyId} from database based on configuration...", activeTrolleyId.Value);
            try
            {
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var dbTrolley = await unitOfWork.Trolleys.GetByIdAsync((long)activeTrolleyId.Value); // Cast to long

                    if (dbTrolley == null)
                    {
                        _logger.LogCritical("Main trolley with ID {TrolleyId} (from configuration) not found in the database. Workstation configuration may be corrupted or trolley was deleted.", activeTrolleyId.Value);
                        MessageBox.Show($"Critical Error: Configured main trolley (ID: {activeTrolleyId.Value}) not found in database. Application cannot initialize correctly. Please check configuration or re-initialize workstation.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    CurrentTrolley = dbTrolley; // Set the main ViewModel's CurrentTrolley
                    if (TrolleyOperationsVM != null)
                    {
                        TrolleyOperationsVM.CurrentTrolley = dbTrolley; // Update TrolleyOperationsVM's instance
                    }
                    _logger.LogInformation("Successfully loaded main trolley {TrolleyId} - {DisplayName}.", dbTrolley.Id, dbTrolley.DisplayName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to load main trolley with ID {TrolleyId} from database.", activeTrolleyId.Value);
                MessageBox.Show($"Critical Error: Could not load main trolley (ID: {activeTrolleyId.Value}) from database. {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task InitializeApplicationAsync()
        {
            _logger.LogInformation("Starting MainViewModel asynchronous initialization...");
            var initSw = Stopwatch.StartNew();

            try
            {
                ConnectionStatus = "Connecting";
                OnPropertyChanged(nameof(ConnectionStatus));

                // Step 1: Load and set the main trolley for this workstation based on configuration.
                bool trolleyLoaded = await LoadAndSetMainTrolleyAsync(); 
                if (!trolleyLoaded)
                {
                    ConnectionStatus = "Failed - Trolley/Config";
                    OnPropertyChanged(nameof(ConnectionStatus));
                    _logger.LogError("Halting initialization as main trolley could not be loaded.");
                    return; // Halt further initialization if trolley isn't loaded
                }

                // Initialize OPC VM first (as it might provide data needed by others)
                _logger.LogInformation("Initializing OpcVM...");
                await OpcVM.InitializeAsync();
                _logger.LogInformation("OpcVM initialized.");

                // Initialize other ViewModels sequentially to avoid DbContext concurrency issues
                _logger.LogInformation("Initializing TrolleyVM...");
                await TrolleyVM.InitializeAsync();
                _logger.LogInformation("TrolleyVM initialized.");

                await LoadPersistedTrolleyStateAsync(); // Load persisted trolley state (relies on CurrentTrolley being set)

                // After TrolleyVM is initialized and its state loaded, refresh TrolleyOperationsVM command states
                if (TrolleyOperationsVM != null)
                {
                    _logger.LogInformation("Requesting TrolleyOperationsVM to refresh unload command states post TrolleyVM load.");
                    TrolleyOperationsVM.RefreshCommandStates();
                }
                else
                {
                    _logger.LogWarning("TrolleyOperationsVM is null after TrolleyVM initialization. Cannot refresh command states.");
                }

                _logger.LogInformation("Initializing WarehouseVM...");
                await WarehouseVM.InitializeAsync();
                _logger.LogInformation("WarehouseVM initialized.");

                _logger.LogInformation("Initializing TaskVM...");
                await TaskVM.InitializeAsync();
                _logger.LogInformation("TaskVM initialized.");

                // Load available fingers for TasksView
                await LoadAvailableFingersAsync();
                _logger.LogInformation("Available fingers loaded.");

                _logger.LogInformation("MainViewModel asynchronous initialization completed.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Failed";
                OnPropertyChanged(nameof(ConnectionStatus));
                _logger.LogError(ex, "Error during MainViewModel asynchronous initialization");
                MessageBox.Show($"An error occurred during application initialization: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                initSw.Stop();
                _logger.LogInformation($"MainViewModel InitializeApplicationAsync Time: {initSw.ElapsedMilliseconds} ms");
            }
        }

        /// <summary>
        /// Updates existing tasks in the database to match the new TaskType values:
        /// - Import (1) -> Storage (2)
        /// - Export (2) -> Retrieval (1)
        /// </summary>
        private async Task UpdateExistingTaskTypesAsync()
        {
            try
            {
                _logger.LogInformation("Updating existing task types...");
                
                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var tasks = await unitOfWork.Tasks.GetAllAsync();
                    int updatedCount = 0;
                    
                    foreach (var task in tasks)
                    {
                        // Convert Import (1) to Storage (2) and Export (2) to Retrieval (1)
                        if (task.TaskTypeId == 1) // Import
                        {
                            task.TaskTypeId = 2; // Storage
                            updatedCount++;
                        }
                        else if (task.TaskTypeId == 2) // Export
                        {
                            task.TaskTypeId = 1; // Retrieval
                            updatedCount++;
                        }
                        
                        unitOfWork.Tasks.Update(task);
                    }
                    
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Updated {Count} tasks to match new TaskType values.", updatedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating existing task types");
                MessageBox.Show($"Error updating task types: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAvailableFingersAsync()
        {
            try
            {
                _logger.LogInformation("Loading available fingers...");

                _dispatcherService.Invoke(() => AvailableFingers.Clear());

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var fingers = await unitOfWork.Fingers.GetAllAsync();

                    _dispatcherService.Invoke(() =>
                    {
                        foreach (var finger in fingers)
                        {
                            AvailableFingers.Add(finger);
                        }
                    });
                }

                _logger.LogInformation("Loaded {Count} fingers.", AvailableFingers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available fingers");
                MessageBox.Show($"Error loading fingers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPersistedTrolleyStateAsync()
        {
            _logger.LogInformation("Loading persisted trolley state...");
            try
            {
                if (CurrentTrolley == null)
                {
                    _logger.LogWarning("CurrentTrolley is null, cannot load persisted state.");
                    return;
                }

                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var trolleyCells = await unitOfWork.TrolleyCells.FindAsync(
                        tc => tc.TrolleyId == CurrentTrolley.Id && tc.PalletId != null,
                        include: q => q.Include(tc => tc.Pallet)
                    );

                    foreach (var cell in trolleyCells)
                    {
                        if (cell.Pallet != null)
                        {
                            if (cell.Position == EM.Maman.Common.Constants.TrolleyConstants.LeftCellPosition)
                            {
                                TrolleyVM.LoadPalletIntoLeftCell(cell.Pallet);
                                _logger.LogInformation("Loaded pallet {PalletId} into Left Trolley Cell from persisted state.", cell.Pallet.Id);
                            }
                            else if (cell.Position == EM.Maman.Common.Constants.TrolleyConstants.RightCellPosition)
                            {
                                TrolleyVM.LoadPalletIntoRightCell(cell.Pallet);
                                _logger.LogInformation("Loaded pallet {PalletId} into Right Trolley Cell from persisted state.", cell.Pallet.Id);
                            }
                        }
                    }
                }
                _logger.LogInformation("Finished loading persisted trolley state.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading persisted trolley state.");
                MessageBox.Show($"Error loading persisted trolley data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
