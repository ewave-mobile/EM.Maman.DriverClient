using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        public async Task InitializeApplicationAsync()
        {
            _logger.LogInformation("Starting MainViewModel asynchronous initialization...");
            var initSw = Stopwatch.StartNew();

            try
            {
                ConnectionStatus = "Connecting";
                OnPropertyChanged(nameof(ConnectionStatus));

                // Ensure trolley exists in database
                await EnsureTrolleyExistsAsync();

                // Update existing tasks to match the new TaskType values
                await UpdateExistingTaskTypesAsync();

                // Initialize OPC VM first (as it might provide data needed by others)
                _logger.LogInformation("Initializing OpcVM...");
                await OpcVM.InitializeAsync();
                _logger.LogInformation("OpcVM initialized.");

                // Initialize other ViewModels sequentially to avoid DbContext concurrency issues
                _logger.LogInformation("Initializing TrolleyVM...");
                await TrolleyVM.InitializeAsync();
                _logger.LogInformation("TrolleyVM initialized.");

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

        /// <summary>
        /// Ensures that the trolley exists in the database, creating it if necessary.
        /// </summary>
        private async Task EnsureTrolleyExistsAsync()
        {
            try
            {
                _logger.LogInformation("Ensuring trolley exists in database...");
                
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Check if trolley with default ID exists
                    var existingTrolley = await unitOfWork.Trolleys.GetByIdAsync(EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyId);
                    
                    if (existingTrolley == null)
                    {
                        // Create the trolley if it doesn't exist
                        _logger.LogInformation("Trolley not found in database. Creating new trolley...");
                        
                        var newTrolley = new Models.LocalDbModels.Trolley 
                        { 
                            Id = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyId, 
                            DisplayName = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyName, 
                            Description = "Primary trolley for warehouse operations",
                            Position = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyPosition,
                            Capacity = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyCapacity
                        };
                        
                        await unitOfWork.Trolleys.AddAsync(newTrolley);
                        
                        // Create the left and right cells
                        var leftCell = new Models.LocalDbModels.TrolleyCell
                        {
                            TrolleyId = newTrolley.Id,
                            Position = EM.Maman.Common.Constants.TrolleyConstants.LeftCellPosition
                        };
                        
                        var rightCell = new Models.LocalDbModels.TrolleyCell
                        {
                            TrolleyId = newTrolley.Id,
                            Position = EM.Maman.Common.Constants.TrolleyConstants.RightCellPosition
                        };
                        
                        await unitOfWork.TrolleyCells.AddAsync(leftCell);
                        await unitOfWork.TrolleyCells.AddAsync(rightCell);
                        
                        await unitOfWork.CompleteAsync();
                        
                        _logger.LogInformation("Trolley created successfully with ID: {TrolleyId}", newTrolley.Id);
                        
                        // Update the current trolley reference
                        CurrentTrolley = newTrolley;
                    }
                    else
                    {
                        // Use the existing trolley
                        _logger.LogInformation("Found existing trolley with ID: {TrolleyId}", existingTrolley.Id);
                        CurrentTrolley = existingTrolley;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring trolley exists in database");
                // Create a default trolley in memory even if database operation fails
                CurrentTrolley = new Models.LocalDbModels.Trolley 
                { 
                    Id = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyId, 
                    DisplayName = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyName, 
                    Position = EM.Maman.Common.Constants.TrolleyConstants.DefaultTrolleyPosition 
                };
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
    }
}
