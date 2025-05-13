﻿using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using DbTask = EM.Maman.Models.LocalDbModels.Task; // Alias for model Task
using EM.Maman.DAL.Test;
using EM.Maman.DriverClient.ViewModels;
using System.Text;
// using System.Diagnostics; // Removed Stopwatch using

namespace EM.Maman.DriverClient
{
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;

        private readonly ILogger<LoginWindow> _logger;

        private string _employeeId;
        public string EmployeeId
        {
            get => _employeeId;
            set
            {
                if (SetProperty(ref _employeeId, value))
                {
                    InitializeCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isFirstInitializationMode;
        public bool IsFirstInitializationMode
        {
            get => _isFirstInitializationMode;
            private set
            {
                if (SetProperty(ref _isFirstInitializationMode, value))
                {
                    OnPropertyChanged(nameof(IsNotFirstInitializationMode));
                }
            }
        }

        public bool IsNotFirstInitializationMode => !IsFirstInitializationMode;

        private string _selectedWorkstationType;
        public string SelectedWorkstationType
        {
            get => _selectedWorkstationType;
            set
            {
                if (SetProperty(ref _selectedWorkstationType, value))
                {
                    InitializeCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand SelectWorkstationCommand { get; private set; }
        public RelayCommand InitializeCommand { get; private set; }

        private TextBox _employeeIdTextBox; // Keep reference if needed by Button_Click logic
        // private Button _loginButton; // No longer needed if CreateBasicUI is removed
        // private StackPanel _workstationPanel; // No longer needed
        // private RadioButton _lodigaRadioButton; // No longer needed
        // private RadioButton _bigBatteryRadioButton; // No longer needed
        // private RadioButton _smallBatteryRadioButton; // No longer needed
        // private RadioButton _lpvRadioButton; // No longer needed

        public LoginWindow(IServiceProvider serviceProvider, IUnitOfWorkFactory unitOfWorkFactory, ILogger<LoginWindow> logger)
        {
            _serviceProvider = serviceProvider;
            _unitOfWorkFactory = unitOfWorkFactory;
            _logger = logger;

            InitializeComponent(); // This might still cause compile error until user fixes build issue

            IsFirstInitializationMode = App.IsFirstInitialization;

            // CreateBasicUI(); // Ensure this remains commented out
            // _logger.LogInformation("Created basic UI programmatically"); // Comment out log if CreateBasicUI is removed

            SelectWorkstationCommand = new RelayCommand(ExecuteSelectWorkstation);
            InitializeCommand = new RelayCommand(ExecuteInitialize, CanExecuteInitialize);
            DataContext = this; // Set DataContext to the window itself
        }

        // CreateBasicUI method should be removed or fully commented out if not used
        /*
        private void CreateBasicUI()
        {
            // ... entire method content commented out ...
        }
        */

        // CreateWorkstationRadioButton method should be removed or fully commented out if not used
        /*
        private RadioButton CreateWorkstationRadioButton(string content, string tag)
        {
            // ... entire method content commented out ...
        }
        */

        public string Version => $"גרסה {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        // public string Greeting => "hi"; // Example property, remove if not needed

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Standard login attempt for Employee ID: {EmployeeId}", EmployeeId);
            // Add actual login logic if needed, or just navigate
            NavigateToMainWindow();
        }

        private void WorkstationRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                string workstationType = radioButton.Tag?.ToString();
                if (!string.IsNullOrEmpty(workstationType))
                {
                    SelectedWorkstationType = workstationType;
                    _logger.LogInformation("Workstation selected via direct event: {WorkstationType}", workstationType);
                }
            }
        }

        // This click handler might be redundant if InitializeCommand works,
        // but keep it for now if the command binding was problematic.
        // Ensure the Command binding is removed from the button in XAML if using this.
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
             _logger.LogInformation("Initialize button clicked via direct event (Button_Click).");

            // Check if we have all required information
            if (string.IsNullOrEmpty(SelectedWorkstationType))
            {
                _logger.LogWarning("No workstation selected. Prompting user to select one.");
                MessageBox.Show("Please select a workstation type.",
                    "Workstation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EmployeeId))
            {
                _logger.LogWarning("No employee ID entered. Prompting user to enter one.");
                 MessageBox.Show("Please enter your Employee ID.",
                    "Employee ID Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                 // Consider focusing the TextBox: this.FindName("EmployeeIdTextBox") as TextBox)?.Focus();
                return;
            }

            _logger.LogInformation("All conditions met. Starting initialization with Workstation: {WorkstationType}, Employee ID: {EmployeeId}",
                SelectedWorkstationType, EmployeeId);

            try
            {
                MessageBox.Show($"Starting initialization for {SelectedWorkstationType}.\nThis may take a moment...",
                    "Initialization Started", MessageBoxButton.OK, MessageBoxImage.Information);

                await InitializeWorkstationAsync(SelectedWorkstationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Button_Click handler during initialization");
                MessageBox.Show($"An error occurred during initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSelectWorkstation(object parameter)
        {
            if (parameter is string workstationType)
            {
                SelectedWorkstationType = workstationType;
                _logger.LogInformation("Workstation selected via command: {WorkstationType}", workstationType);
            }
        }

        private bool CanExecuteInitialize(object parameter)
        {
            bool canExecute = !string.IsNullOrEmpty(SelectedWorkstationType) && !string.IsNullOrWhiteSpace(EmployeeId);
            // _logger.LogTrace($"CanExecuteInitialize check: {canExecute} (Workstation: '{SelectedWorkstationType}', EmployeeId: '{EmployeeId}')"); // Use Trace for frequent logs
            return canExecute;
        }

        private async void ExecuteInitialize(object parameter)
        {
            _logger.LogInformation("Initialize command executed (ExecuteInitialize).");
            if (!CanExecuteInitialize(null))
            {
                 _logger.LogWarning("Initialize command executed but conditions not met (Workstation: {SelectedWorkstation}, EmployeeId: {EmployeeId})", SelectedWorkstationType, EmployeeId);
                 MessageBox.Show("Please select a workstation and enter your Employee ID.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }
            await InitializeWorkstationAsync(SelectedWorkstationType);
        }

        private async System.Threading.Tasks.Task InitializeWorkstationAsync(string workstationType)
        {
            // Database connection check has been removed as it was not correctly checking the connection
            // Any actual database connection errors will be caught by the try-catch blocks in the subsequent operations

            _logger.LogInformation("Starting first-time initialization for Workstation: {WorkstationType}, Employee ID: {EmployeeId}", workstationType, EmployeeId);

            // EmployeeId check already done in Button_Click/ExecuteInitialize

            try
            {
                // Create a new UnitOfWork for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    _logger.LogInformation("Clearing existing data before seeding...");
                    bool cleared = await ClearAllDataAsync(unitOfWork); // Use the return value

                    if (!cleared)
                    {
                        _logger.LogError("Database clearing failed. Aborting initialization.");
                        MessageBox.Show("Failed to clear existing database data. Initialization aborted.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // Stop if clearing failed
                    }

                    // Prepare predefined data
                    _logger.LogInformation("Gathering predefined data for {WorkstationType}...", workstationType);
                    List<Level> predefinedLevels = GetPredefinedLevels();
                    List<Cell> predefinedCells = GetPredefinedCells(); // These will have ID=0 initially
                    List<Pallet> predefinedPallets = GetPredefinedPallets(); // These will have ID=0 initially
                    List<Finger> predefinedFingers = GetPredefinedFingers(); // These will have ID=0 initially

                    // Add base data (Levels, Cells, Pallets, Fingers)
                    await AddAllDataAsync(unitOfWork, predefinedLevels, predefinedCells, predefinedPallets, predefinedFingers);
                    // IMPORTANT: After AddAllDataAsync completes and saves, the IDs of the objects
                    // in the predefinedCells, predefinedPallets, predefinedFingers lists *might* be updated
                    // by EF Core depending on the repository implementation. If not, we need to re-fetch them.
                    // For safety, let's re-fetch the necessary items by their unique properties before creating associations.

                    var savedCells = (await unitOfWork.Cells.GetAllAsync()).ToList();
                    var savedPallets = (await unitOfWork.Pallets.GetAllAsync()).ToList();
                    var savedFingers = (await unitOfWork.Fingers.GetAllAsync()).ToList();


                    // Create tasks and pallet-in-cell associations using the *saved* entities with correct IDs
                    List<DbTask> predefinedTasks = GetPredefinedTasks(savedCells, savedPallets, savedFingers);
                    List<PalletInCell> predefinedPalletInCells = GetPredefinedPalletInCells(savedCells, savedPallets);

                    // Add tasks and associations
                    await AddTasksAndAssociationsAsync(unitOfWork, predefinedTasks, predefinedPalletInCells);

                    // Finally, add the configuration
                    await AddConfigurationAsync(unitOfWork); // This uses SelectedWorkstationType and EmployeeId from the class properties
                } // UnitOfWork disposed here, along with its DbContext

                _logger.LogInformation("Initialization completed successfully.");
                NavigateToMainWindow(); // Navigate only after successful initialization
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during first-time initialization for Workstation: {WorkstationType}", workstationType);
                MessageBox.Show($"Failed to initialize workstation '{workstationType}'.\nPlease check the application logs for details.\n\nError: {ex.Message}",
                                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Returns true if successful, false otherwise.
        private async System.Threading.Tasks.Task<bool> ClearAllDataAsync(IUnitOfWork unitOfWork)
        {
            // Diagnostic variables to track progress
            var deletionResults = new Dictionary<string, (bool Success, string Message, int Count)>();

            try
            {
                _logger.LogInformation("Attempting to clear all data...");

                // STEP 1: Clear PalletInCell entities
                try
                {
                    var existingPalletInCells = await unitOfWork.PalletInCells.GetAllAsync(null, null, false); // Disable AsNoTracking
                    int count = existingPalletInCells.Count();

                    if (count > 0)
                    {
                        _logger.LogInformation($"Removing {count} PalletInCell records");
                        unitOfWork.PalletInCells.RemoveRange(existingPalletInCells);
                        int result = await unitOfWork.CompleteAsync();
                        _logger.LogInformation($"PalletInCell CompleteAsync result: {result}");
                        deletionResults["PalletInCell"] = (true, $"Removed {result} records", count);
                        _logger.LogInformation($"Successfully removed {result} PalletInCell records");
                    }
                    else
                    {
                        deletionResults["PalletInCell"] = (true, "No records to remove", 0);
                        _logger.LogInformation("No PalletInCell records to remove");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing PalletInCell records");
                    deletionResults["PalletInCell"] = (false, $"Error: {ex.Message}", 0);
                    // Continue anyway - we'll try to clear other tables
                }

                // STEP 2: Clear Tasks entities
                try
                {
                    var existingTasks = await unitOfWork.Tasks.GetAllAsync(null, null, false); // Disable AsNoTracking
                    int count = existingTasks.Count();

                    if (count > 0)
                    {
                        _logger.LogInformation($"Removing {count} Task records");
                        unitOfWork.Tasks.RemoveRange(existingTasks);
                        int result = await unitOfWork.CompleteAsync();
                        _logger.LogInformation($"Task CompleteAsync result: {result}");
                        deletionResults["Tasks"] = (true, $"Removed {result} records", count);
                        _logger.LogInformation($"Successfully removed {result} Task records");
                    }
                    else
                    {
                        deletionResults["Tasks"] = (true, "No records to remove", 0);
                        _logger.LogInformation("No Task records to remove");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing Task records");
                    deletionResults["Tasks"] = (false, $"Error: {ex.Message}", 0);
                    // Continue anyway
                }

                // STEP 3: Clear Fingers entities
                try
                {
                    var existingFingers = await unitOfWork.Fingers.GetAllAsync(null, null, false); // Disable AsNoTracking
                    int count = existingFingers.Count();

                    if (count > 0)
                    {
                        _logger.LogInformation($"Removing {count} Finger records");
                        unitOfWork.Fingers.RemoveRange(existingFingers);
                        int result = await unitOfWork.CompleteAsync();
                        _logger.LogInformation($"Finger CompleteAsync result: {result}");
                        deletionResults["Fingers"] = (true, $"Removed {result} records", count);
                        _logger.LogInformation($"Successfully removed {result} Finger records");
                    }
                    else
                    {
                        deletionResults["Fingers"] = (true, "No records to remove", 0);
                        _logger.LogInformation("No Finger records to remove");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing Finger records");
                    deletionResults["Fingers"] = (false, $"Error: {ex.Message}", 0);
                    // Continue anyway
                }

                // STEP 4: Clear Pallets entities
                try
                {
                    var existingPallets = await unitOfWork.Pallets.GetAllAsync(null, null, false); // Disable AsNoTracking
                    int count = existingPallets.Count();

                    if (count > 0)
                    {
                        _logger.LogInformation($"Removing {count} Pallet records");
                        unitOfWork.Pallets.RemoveRange(existingPallets);
                        int result = await unitOfWork.CompleteAsync();
                        _logger.LogInformation($"Pallet CompleteAsync result: {result}");
                        deletionResults["Pallets"] = (true, $"Removed {result} records", count);
                        _logger.LogInformation($"Successfully removed {result} Pallet records");
                    }
                    else
                    {
                        deletionResults["Pallets"] = (true, "No records to remove", 0);
                        _logger.LogInformation("No Pallet records to remove");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing Pallet records");
                    deletionResults["Pallets"] = (false, $"Error: {ex.Message}", 0);
                    // Continue anyway
                }

                // STEP 5: Clear Cells entities
                try
                {
                    var existingCells = await unitOfWork.Cells.GetAllAsync(null, null, false); // Disable AsNoTracking
                    int count = existingCells.Count();

                    if (count > 0)
                    {
                        _logger.LogInformation($"Removing {count} Cell records");
                        unitOfWork.Cells.RemoveRange(existingCells);
                        int result = await unitOfWork.CompleteAsync();
                        _logger.LogInformation($"Cell CompleteAsync result: {result}");
                        deletionResults["Cells"] = (true, $"Removed {result} records", count);
                        _logger.LogInformation($"Successfully removed {result} Cell records");
                    }
                    else
                    {
                        deletionResults["Cells"] = (true, "No records to remove", 0);
                        _logger.LogInformation("No Cell records to remove");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing Cell records");
                    deletionResults["Cells"] = (false, $"Error: {ex.Message}", 0);
                    // Continue anyway
                }

                // STEP 6: Clear Levels entities
                try
                {
                    var existingLevels = await unitOfWork.Levels.GetAllAsync(null, null, false); // Disable AsNoTracking
                    int count = existingLevels.Count();

                    if (count > 0)
                    {
                        _logger.LogInformation($"Removing {count} Level records");
                        unitOfWork.Levels.RemoveRange(existingLevels);
                        int result = await unitOfWork.CompleteAsync();
                        _logger.LogInformation($"Level CompleteAsync result: {result}");
                        deletionResults["Levels"] = (true, $"Removed {result} records", count);
                        _logger.LogInformation($"Successfully removed {result} Level records");
                    }
                    else
                    {
                        deletionResults["Levels"] = (true, "No records to remove", 0);
                        _logger.LogInformation("No Level records to remove");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing Level records");
                    deletionResults["Levels"] = (false, $"Error: {ex.Message}", 0);
                    // Continue anyway
                }

                // Log a summary of all deletion operations
                StringBuilder summary = new StringBuilder();
                summary.AppendLine("Database clearing summary:");
                foreach (var result in deletionResults)
                {
                    summary.AppendLine($"- {result.Key}: {(result.Value.Success ? "SUCCESS" : "FAILED")} - {result.Value.Message} - Found: {result.Value.Count}");
                }
                _logger.LogInformation(summary.ToString());

                // Determine overall success - if any critical operation failed, return false
                bool overallSuccess = deletionResults.All(r => r.Value.Success || r.Value.Count == 0);

                if (overallSuccess)
                {
                    _logger.LogInformation("All existing data cleared successfully (or no data to clear)");
                }
                else
                {
                    _logger.LogWarning("Some data clearing operations failed - see details above");
                }

                return overallSuccess;
            }
            catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error clearing existing data");
                    return false;
                }
        }

        private async System.Threading.Tasks.Task AddAllDataAsync(IUnitOfWork unitOfWork, List<Level> levels, List<Cell> cells, List<Pallet> pallets, List<Finger> fingers)
        {
            // Diagnostic variables to track progress
            var additionResults = new Dictionary<string, (bool Success, string Message, int Count, int SavedCount)>();

            try
            {
                _logger.LogInformation("Starting to add base data...");

                // STEP 1: Add Levels
                try
                {
                    if (levels != null && levels.Any())
                    {
                        int count = levels.Count;
                        _logger.LogInformation($"Adding {count} Level records");

                        await unitOfWork.Levels.AddRangeAsync(levels);
                        int result = await unitOfWork.CompleteAsync();

                        additionResults["Levels"] = (true, $"Added and saved {result} records", count, result);
                        _logger.LogInformation($"Successfully added {result} Level records");

                        // Diagnostics: verify that IDs were assigned
                        bool allLevelsHaveIds = levels.All(l => l.Id > 0);
                        _logger.LogInformation($"All levels have IDs assigned: {allLevelsHaveIds}");
                        if (!allLevelsHaveIds)
                        {
                            // Log the level IDs for debugging
                            _logger.LogWarning($"Level IDs after save: {string.Join(", ", levels.Select(l => l.Id))}");
                        }
                    }
                    else
                    {
                        additionResults["Levels"] = (true, "No records to add", 0, 0);
                        _logger.LogInformation("No Level records to add");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding Level records");
                    additionResults["Levels"] = (false, $"Error: {ex.Message}", levels?.Count ?? 0, 0);
                    throw; // Re-throw - this is a critical step
                }

                // STEP 2: Add Cells
                try
                {
                    if (cells != null && cells.Any())
                    {
                        int count = cells.Count;
                        _logger.LogInformation($"Adding {count} Cell records");

                        // Diagnostic check: Make sure cells have valid Level references
                        bool allCellsHaveValidLevelId = cells.All(c => c.Level > 0);
                        if (!allCellsHaveValidLevelId)
                        {
                            _logger.LogWarning("Some cells don't have valid Level IDs. This may cause foreign key constraint issues.");
                            _logger.LogWarning($"Cell LevelIDs: {string.Join(", ", cells.Select(c => c.Level))}");
                        }

                        await unitOfWork.Cells.AddRangeAsync(cells);
                        int result = await unitOfWork.CompleteAsync();

                        additionResults["Cells"] = (true, $"Added and saved {result} records", count, result);
                        _logger.LogInformation($"Successfully added {result} Cell records");

                        // Diagnostics: verify that IDs were assigned
                        bool allCellsHaveIds = cells.All(c => c.Id > 0);
                        _logger.LogInformation($"All cells have IDs assigned: {allCellsHaveIds}");
                        if (!allCellsHaveIds)
                        {
                            // Log the cell IDs for debugging
                            _logger.LogWarning($"Cell IDs after save: {string.Join(", ", cells.Select(c => c.Id))}");
                        }
                    }
                    else
                    {
                        additionResults["Cells"] = (true, "No records to add", 0, 0);
                        _logger.LogInformation("No Cell records to add");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding Cell records");
                    additionResults["Cells"] = (false, $"Error: {ex.Message}", cells?.Count ?? 0, 0);
                    throw; // Re-throw - this is a critical step
                }

                // STEP 3: Add Pallets
                try
                {
                    if (pallets != null && pallets.Any())
                    {
                        int count = pallets.Count;
                        _logger.LogInformation($"Adding {count} Pallet records");

                        await unitOfWork.Pallets.AddRangeAsync(pallets);
                        int result = await unitOfWork.CompleteAsync();

                        additionResults["Pallets"] = (true, $"Added and saved {result} records", count, result);
                        _logger.LogInformation($"Successfully added {result} Pallet records");

                        // Diagnostics: verify that IDs were assigned
                        bool allPalletsHaveIds = pallets.All(p => p.Id > 0);
                        _logger.LogInformation($"All pallets have IDs assigned: {allPalletsHaveIds}");
                        if (!allPalletsHaveIds)
                        {
                            // Log the pallet IDs for debugging
                            _logger.LogWarning($"Pallet IDs after save: {string.Join(", ", pallets.Select(p => p.Id))}");
                        }
                    }
                    else
                    {
                        additionResults["Pallets"] = (true, "No records to add", 0, 0);
                        _logger.LogInformation("No Pallet records to add");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding Pallet records");
                    additionResults["Pallets"] = (false, $"Error: {ex.Message}", pallets?.Count ?? 0, 0);
                    throw; // Re-throw - this is a critical step
                }

                // STEP 4: Add Fingers
                try
                {
                    if (fingers != null && fingers.Any())
                    {
                        int count = fingers.Count;
                        _logger.LogInformation($"Adding {count} Finger records");

                        await unitOfWork.Fingers.AddRangeAsync(fingers);
                        int result = await unitOfWork.CompleteAsync();

                        additionResults["Fingers"] = (true, $"Added and saved {result} records", count, result);
                        _logger.LogInformation($"Successfully added {result} Finger records");

                        // Diagnostics: verify that IDs were assigned
                        bool allFingersHaveIds = fingers.All(f => f.Id > 0);
                        _logger.LogInformation($"All fingers have IDs assigned: {allFingersHaveIds}");
                        if (!allFingersHaveIds)
                        {
                            // Log the finger IDs for debugging
                            _logger.LogWarning($"Finger IDs after save: {string.Join(", ", fingers.Select(f => f.Id))}");
                        }
                    }
                    else
                    {
                        additionResults["Fingers"] = (true, "No records to add", 0, 0);
                        _logger.LogInformation("No Finger records to add");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding Finger records");
                    additionResults["Fingers"] = (false, $"Error: {ex.Message}", fingers?.Count ?? 0, 0);
                    throw; // Re-throw - this is a critical step
                }

                // Log a summary of all addition operations
                StringBuilder summary = new StringBuilder();
                summary.AppendLine("Base data addition summary:");
                foreach (var result in additionResults)
                {
                    summary.AppendLine($"- {result.Key}: {(result.Value.Success ? "SUCCESS" : "FAILED")} - {result.Value.Message} - Attempted: {result.Value.Count}, Saved: {result.Value.SavedCount}");
                }
                _logger.LogInformation(summary.ToString());

                _logger.LogInformation("Base data added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding base data");
                throw; // Re-throw to be caught by InitializeWorkstationAsync
            }
        }

        private async System.Threading.Tasks.Task AddTasksAndAssociationsAsync(IUnitOfWork unitOfWork, List<DbTask> tasks, List<PalletInCell> palletInCells)
        {
            try
            {
                if (tasks.Any())
                {
                    await unitOfWork.Tasks.AddRangeAsync(tasks); // Use AddRangeAsync
                }

                if (palletInCells.Any())
                {
                    await unitOfWork.PalletInCells.AddRangeAsync(palletInCells); // Use AddRangeAsync
                }

                // Save all changes at once
               var res = await unitOfWork.CompleteAsync();
                _logger.LogInformation("Tasks and associations added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tasks and associations");
                 throw; // Re-throw to be caught by InitializeWorkstationAsync
            }
        }

        private async System.Threading.Tasks.Task AddConfigurationAsync(IUnitOfWork unitOfWork)
        {
            try
            {
                // Ensure configuration doesn't already exist (though Clear should handle this)
                if (!await unitOfWork.Configurations.AnyAsync())
                {
                    var newConfiguration = new Configuration
                    {
                        WorkstationType = SelectedWorkstationType,
                        InitializedByEmployeeId = EmployeeId,
                        InitializedAt = DateTime.UtcNow
                    };
                    await unitOfWork.Configurations.AddAsync(newConfiguration);
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Configuration added successfully with WorkstationType: {WorkstationType}, EmployeeId: {EmployeeId}",
                        SelectedWorkstationType, EmployeeId);
                }
                else
                {
                     _logger.LogWarning("Configuration already exists, skipping add.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding configuration");
                 throw; // Re-throw to be caught by InitializeWorkstationAsync
            }
        }

        // --- GetPredefined... methods remain largely the same, ---
        // --- but ensure they only create NEW objects without IDs ---
        // --- The actual saving and ID generation happens in AddAllDataAsync ---

        private List<Level> GetPredefinedLevels()
        {
            // Correct: Creates new objects without IDs
            return TestDatabase.Levels.Select(l => new Level
            {
                Number = l.Number,
                DisplayName = l.DisplayName
            }).ToList();
        }

        private List<Cell> GetPredefinedCells()
        {
             _logger.LogInformation("Creating predefined Cells objects (without IDs).");
            // Correct: Creates new objects using TestDatabase logic, ensures ID is 0
            var cells = TestDatabase.CreateCells();
            foreach (var cell in cells) { cell.Id = 0; }
            return cells;
        }

        private List<Finger> GetPredefinedFingers()
        {
             // Correct: Creates new objects without IDs
            return TestDatabase.Fingers.Select(f => new Finger
            {
                Side = f.Side,
                Position = f.Position,
                Description = f.Description,
                DisplayName = f.DisplayName,
                DisplayColor = f.DisplayColor
            }).ToList();
        }

        private List<Pallet> GetPredefinedPallets()
        {
             _logger.LogInformation("Creating predefined Pallets objects (without IDs).");
             // Correct: Creates new objects without IDs
            return TestDatabase.Pallets.Select(p => new Pallet
            {
                DisplayName = p.DisplayName,
                Description = p.Description, // Assuming Description might be set in TestDatabase
                UldCode = p.UldCode,
                AwbCode = p.AwbCode,
                ReceivedDate = p.ReceivedDate,
                LastModifiedDate = p.LastModifiedDate,
                RefrigerationType = p.RefrigerationType,
                HeightLevel = p.HeightLevel,
                IsCheckedOut = p.IsCheckedOut,
                CheckedOutDate = p.CheckedOutDate,
                LocationId = p.LocationId,
                IsSecure = p.IsSecure,
                CargoType = p.CargoType,
                UpdateType = p.UpdateType,
                StorageType = p.StorageType,
                HeightType = p.HeightType,
                CargoHeight = p.CargoHeight,
                ReportType = p.ReportType,
                ImportManifest = p.ImportManifest,
                ImportUnit = p.ImportUnit,
                ImportAppearance = p.ImportAppearance,
                ExportSwbPrefix = p.ExportSwbPrefix,
                ExportAwbNumber = p.ExportAwbNumber,
                ExportAwbAppearance = p.ExportAwbAppearance,
                ExportAwbStorage = p.ExportAwbStorage,
                ExportBarcode = p.ExportBarcode,
                UldType = p.UldType,
                UldNumber = p.UldNumber,
                UldAirline = p.UldAirline
            }).ToList();
        }

        // --- Methods to create associations need to use the SAVED entities ---
        // --- passed in as arguments, which now have IDs             ---

        private List<PalletInCell> GetPredefinedPalletInCells(List<Cell> savedCells, List<Pallet> savedPallets)
        {
            _logger.LogInformation("Creating PalletInCell associations using SAVED entities.");
            var associations = new List<PalletInCell>();
            var testInfos = TestDatabase.CellWithPalletInfos; // Get the template data

            foreach (var info in testInfos)
            {
                // Find the *saved* cell and pallet using unique properties
                var cellToAdd = savedCells.FirstOrDefault(c =>
                    c.DisplayName == info.Cell.DisplayName && // Assuming DisplayName is unique enough for lookup
                    c.Position == info.Cell.Position &&
                    c.HeightLevel == info.Cell.HeightLevel &&
                    c.Side == info.Cell.Side &&
                    c.Order == info.Cell.Order);

                var palletToAdd = savedPallets.FirstOrDefault(p =>
                    p.UldCode == info.Pallet.UldCode); // Assuming UldCode is unique

                if (cellToAdd != null && palletToAdd != null)
                {
                    associations.Add(new PalletInCell
                    {
                        CellId = cellToAdd.Id, // Use the ID from the saved entity
                        PalletId = palletToAdd.Id, // Use the ID from the saved entity
                        StorageDate = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("Could not find matching SAVED Cell (Name:{CellName}) or Pallet (Code:{PalletCode}) for PalletInCell association.",
                        info.Cell.DisplayName, info.Pallet.UldCode);
                }
            }
            return associations;
        }

         private List<DbTask> GetPredefinedTasks(List<Cell> savedCells, List<Pallet> savedPallets, List<Finger> savedFingers)
        {
            _logger.LogInformation("Creating sample Tasks using SAVED entities.");
            var tasks = new List<DbTask>();
            var testCells = TestDatabase.CreateCells(); // Get template cells for lookup info
            var testPallets = TestDatabase.Pallets; // Get template pallets for reference
            var testFingers = TestDatabase.Fingers; // Get template fingers for reference

            // Ensure we have enough saved entities to create diverse tasks
            if (savedPallets.Count < 4 || savedCells.Count < 4 || savedFingers.Count < 4)
            {
                _logger.LogWarning("Not enough saved entities to create all predefined tasks.");
                return tasks; // Return empty or partially filled list
            }

            // --- Retrieval Tasks (TaskType 1) ---
            // Task 1: Retrieve Pallet 0 (Import ULD) from Cell 0 to Finger 0
            var palletForRetrieval1 = savedPallets[0]; // PLT-A001-IMP-ULD
            var sourceCellForRetrieval1 = savedCells.FirstOrDefault(c => c.DisplayName == TestDatabase.Cells.First(tc => tc.Id == 1000).DisplayName && c.Position == TestDatabase.Cells.First(tc => tc.Id == 1000).Position); // Cell Id 1000
            var destFingerForRetrieval1 = savedFingers[0]; // Finger L02 (Id 1 in TestDatabase)

            if (palletForRetrieval1 != null && sourceCellForRetrieval1 != null && destFingerForRetrieval1 != null)
            {
                tasks.Add(new DbTask
                {
                    Name = $"Retrieve {palletForRetrieval1.DisplayName}",
                    Description = $"Move {palletForRetrieval1.DisplayName} from Cell {sourceCellForRetrieval1.DisplayName} (Pos:{sourceCellForRetrieval1.Position}, Lvl:{sourceCellForRetrieval1.HeightLevel}) to Finger {destFingerForRetrieval1.DisplayName}",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Retrieval,
                    CellEndLocationId = sourceCellForRetrieval1.Id, // Source for retrieval
                    FingerLocationId = destFingerForRetrieval1.Id,  // Destination for retrieval
                    PalletId = palletForRetrieval1.UldCode ?? palletForRetrieval1.AwbCode ?? palletForRetrieval1.DisplayName, // Link by UldCode or AwbCode
                    Status = (int?)EM.Maman.Models.Enums.TaskStatus.Created,
                    ActiveTaskStatus = (int?)EM.Maman.Models.Enums.ActiveTaskStatus.New // Changed to New and cast
                });
            }
            else
            {
                _logger.LogWarning($"Could not create Retrieval Task 1. Missing: Pallet={palletForRetrieval1 == null}, Cell={sourceCellForRetrieval1 == null}, Finger={destFingerForRetrieval1 == null}");
            }

            // Task 2: Retrieve Pallet 2 (Export ULD) from Cell 2 to Finger 1
            var palletForRetrieval2 = savedPallets[2]; // PLT-B002-EXP-ULD
            var sourceCellForRetrieval2 = savedCells.FirstOrDefault(c => c.DisplayName == TestDatabase.Cells.First(tc => tc.Id == 2003).DisplayName && c.Position == TestDatabase.Cells.First(tc => tc.Id == 2003).Position); // Cell Id 2003
            var destFingerForRetrieval2 = savedFingers[1]; // Finger R03 (Id 2 in TestDatabase)

            if (palletForRetrieval2 != null && sourceCellForRetrieval2 != null && destFingerForRetrieval2 != null)
            {
                tasks.Add(new DbTask
                {
                    Name = $"Retrieve {palletForRetrieval2.DisplayName}",
                    Description = $"Move {palletForRetrieval2.DisplayName} from Cell {sourceCellForRetrieval2.DisplayName} (Pos:{sourceCellForRetrieval2.Position}, Lvl:{sourceCellForRetrieval2.HeightLevel}) to Finger {destFingerForRetrieval2.DisplayName}",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Retrieval,
                    CellEndLocationId = sourceCellForRetrieval2.Id,
                    FingerLocationId = destFingerForRetrieval2.Id,
                    PalletId = palletForRetrieval2.UldCode ?? palletForRetrieval2.AwbCode ?? palletForRetrieval2.DisplayName,
                    Status = (int?)EM.Maman.Models.Enums.TaskStatus.Created,
                    ActiveTaskStatus = (int?)EM.Maman.Models.Enums.ActiveTaskStatus.New // Changed to New and cast
                });
            }
            else
            {
                _logger.LogWarning($"Could not create Retrieval Task 2. Missing: Pallet={palletForRetrieval2 == null}, Cell={sourceCellForRetrieval2 == null}, Finger={destFingerForRetrieval2 == null}");
            }

            // --- Storage Tasks (TaskType 2) ---
            // Task 3: Store Pallet 4 (Import AWB) from Finger 2 to Cell 4
            var palletForStorage1 = savedPallets[4]; // PLT-E005-IMP-AWB
            var sourceFingerForStorage1 = savedFingers[2]; // Finger L05 (Id 3 in TestDatabase)
            var destCellForStorage1 = savedCells.FirstOrDefault(c => c.DisplayName == TestDatabase.Cells.First(tc => tc.Id == 3008).DisplayName && c.Position == TestDatabase.Cells.First(tc => tc.Id == 3008).Position); // Cell Id 3008

            if (palletForStorage1 != null && sourceFingerForStorage1 != null && destCellForStorage1 != null)
            {
                tasks.Add(new DbTask
                {
                    Name = $"Store {palletForStorage1.DisplayName}",
                    Description = $"Move {palletForStorage1.DisplayName} from Finger {sourceFingerForStorage1.DisplayName} to Cell {destCellForStorage1.DisplayName} (Pos:{destCellForStorage1.Position}, Lvl:{destCellForStorage1.HeightLevel})",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Storage,
                    FingerLocationId = sourceFingerForStorage1.Id,  // Source for storage
                    CellEndLocationId = destCellForStorage1.Id,    // Destination for storage
                    PalletId = palletForStorage1.AwbCode ?? palletForStorage1.UldCode ?? palletForStorage1.DisplayName,
                    Status = (int?)EM.Maman.Models.Enums.TaskStatus.Created,
                    ActiveTaskStatus = (int?)EM.Maman.Models.Enums.ActiveTaskStatus.New // Changed to New and cast
                });
            }
            else
            {
                _logger.LogWarning($"Could not create Storage Task 1. Missing: Pallet={palletForStorage1 == null}, Finger={sourceFingerForStorage1 == null}, Cell={destCellForStorage1 == null}");
            }

            // Task 4: Store Pallet 6 (Export AWB) from Finger 3 to Cell 6
            var palletForStorage2 = savedPallets[6]; // PLT-F006-EXP-AWB
            var sourceFingerForStorage2 = savedFingers[3]; // Finger R08 (Id 4 in TestDatabase)
            var destCellForStorage2 = savedCells.FirstOrDefault(c => c.DisplayName == TestDatabase.Cells.First(tc => tc.Id == 4000).DisplayName && c.Position == TestDatabase.Cells.First(tc => tc.Id == 4000).Position); // Cell Id 4000

            if (palletForStorage2 != null && sourceFingerForStorage2 != null && destCellForStorage2 != null)
            {
                tasks.Add(new DbTask
                {
                    Name = $"Store {palletForStorage2.DisplayName}",
                    Description = $"Move {palletForStorage2.DisplayName} from Finger {sourceFingerForStorage2.DisplayName} to Cell {destCellForStorage2.DisplayName} (Pos:{destCellForStorage2.Position}, Lvl:{destCellForStorage2.HeightLevel})",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Storage,
                    FingerLocationId = sourceFingerForStorage2.Id,
                    CellEndLocationId = destCellForStorage2.Id,
                    PalletId = palletForStorage2.AwbCode ?? palletForStorage2.UldCode ?? palletForStorage2.DisplayName,
                    Status = (int?)EM.Maman.Models.Enums.TaskStatus.Created,
                    ActiveTaskStatus = (int?)EM.Maman.Models.Enums.ActiveTaskStatus.New // Changed to New and cast
                });
            }
            else
            {
                _logger.LogWarning($"Could not create Storage Task 2. Missing: Pallet={palletForStorage2 == null}, Finger={sourceFingerForStorage2 == null}, Cell={destCellForStorage2 == null}");
            }

            _logger.LogInformation($"Created {tasks.Count} predefined tasks.");
            return tasks;
        }


        // Create a template for rounded buttons
        /* // This seems unused now, can be removed if CreateBasicUI is removed
        private ControlTemplate CreateRoundedButtonTemplate()
        {
            // ... implementation ...
        }
        */

        private void NavigateToMainWindow()
        {
            try
            {
                _logger.LogInformation("Attempting to resolve and show MainWindow...");
#if DEBUG
                // Stopwatch code removed as it requires MainWindow.xaml.cs modification which fails
#endif
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                this.Close(); // Close LoginWindow after MainWindow opens
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Failed to navigate to MainWindow.");
                 // Show specific error related to MainWindow loading
                 string errorDetails = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                 MessageBox.Show($"Error loading main application window: {errorDetails}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
