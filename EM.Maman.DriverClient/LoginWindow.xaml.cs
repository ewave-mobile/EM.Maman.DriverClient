﻿﻿using EM.Maman.Models.Interfaces;
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
using EM.Maman.DriverClient.ViewModels; // Added for LoginViewModel
// using System.Diagnostics; // Removed Stopwatch using

namespace EM.Maman.DriverClient
{
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly LoginViewModel _loginViewModel; // Added
        private readonly ILogger<LoginWindow> _logger;

        // This EmployeeId property is primarily for the View's direct use during initialization.
        // The LoginViewModel will have its own EmployeeCode property for binding.
        private string _employeeId;
        public string EmployeeId
        {
            get => _employeeId;
            set
            {
                // If DataContext is LoginViewModel, this setter might not be hit directly from XAML.
                // We'll ensure _loginViewModel.EmployeeCode is updated.
                if (SetProperty(ref _employeeId, value))
                {
                    if (_loginViewModel != null) _loginViewModel.EmployeeCode = value; // Sync with ViewModel
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

        public LoginWindow(IServiceProvider serviceProvider, IUnitOfWorkFactory unitOfWorkFactory, LoginViewModel loginViewModel, ILogger<LoginWindow> logger)
        {
            _serviceProvider = serviceProvider;
            _unitOfWorkFactory = unitOfWorkFactory;
            _loginViewModel = loginViewModel ?? throw new ArgumentNullException(nameof(loginViewModel));
            _logger = logger;

            InitializeComponent(); 

            IsFirstInitializationMode = App.IsFirstInitialization;
            
            _loginViewModel.LoginSuccessful += LoginViewModel_LoginSuccessful;
            // Set DataContext to the ViewModel for MVVM bindings
            DataContext = _loginViewModel; 

            // Initialize properties on LoginWindow that are still used by its own logic or specific XAML bindings
            // (e.g., if EmployeeId TextBox was bound to LoginWindow.EmployeeId and needs to sync with LoginViewModel.EmployeeCode)
            // If EmployeeId TextBox binds directly to _loginViewModel.EmployeeCode, this line might not be needed for EmployeeId.
            // However, IsFirstInitializationMode is a property of LoginWindow.
            // We need to ensure EmployeeId from LoginWindow (if still used by init logic) is synced or init logic uses ViewModel's EmployeeCode.
            if (_loginViewModel.EmployeeCode != null)
            {
                this.EmployeeId = _loginViewModel.EmployeeCode;
            }


            SelectWorkstationCommand = new RelayCommand(ExecuteSelectWorkstation); // This command is on LoginWindow
            InitializeCommand = new RelayCommand(ExecuteInitialize, CanExecuteInitialize); // This command is on LoginWindow
        }

        private async void LoginViewModel_LoginSuccessful(object sender, LoginSuccessEventArgs e) // Make async
        {
            _logger.LogInformation("Login successful event received. UserID: {UserId}. Navigating to MainWindow.", e.LocalUserId);
            
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>(); 
            var mainViewModel = mainWindow.DataContext as MainViewModel;

            if (mainViewModel != null)
            {
                await mainViewModel.SetCurrentUserAsync(e.LocalUserId); 
            }
            else
            {
                _logger.LogError("Could not obtain MainViewModel instance to set current user.");
                MessageBox.Show("Critical error: Could not initialize user session. Please restart.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.Show();
                this.Close();
            });
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

        // LoginButton_Click is no longer needed if the standard login button uses Command binding to LoginViewModel.LoginCommand
        /*
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Standard login attempt for Employee ID: {EmployeeId}", _loginViewModel.EmployeeCode);
            if (_loginViewModel != null)
            {
                // EmployeeCode should already be set on _loginViewModel via XAML binding
                // _loginViewModel.EmployeeCode = this.EmployeeId; // Ensure ViewModel has the code
                await _loginViewModel.LoginAsync();
            }
            // Navigation is handled by LoginViewModel_LoginSuccessful event
        }
        */

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
        private async void Button_Click(object sender, RoutedEventArgs e) // This is for the INITIALIZE button
        {
            _logger.LogInformation("Initialize button clicked (Button_Click).");

            // EmployeeId for initialization will now come from _loginViewModel.EmployeeCode due to XAML binding
            // Or, if EmployeeId TextBox was bound to LoginWindow.EmployeeId, ensure it's up-to-date.
            // For simplicity, assume EmployeeId TextBox is bound to _loginViewModel.EmployeeCode.
            string currentEmployeeId = _loginViewModel.EmployeeCode;


            if (string.IsNullOrEmpty(SelectedWorkstationType))
            {
                _logger.LogWarning("No workstation selected for initialization.");
                MessageBox.Show("Please select a workstation type.", "Workstation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(currentEmployeeId))
            {
                _logger.LogWarning("No employee ID entered for initialization.");
                MessageBox.Show("Please enter your Employee ID.", "Employee ID Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Update the LoginWindow's EmployeeId property if it's still used by InitializeWorkstationAsync directly
            this.EmployeeId = currentEmployeeId;

            _logger.LogInformation("Starting initialization with Workstation: {WorkstationType}, Employee ID: {EmployeeId}", SelectedWorkstationType, currentEmployeeId);

            try
            {
                // Disable button to prevent multiple clicks?
                var button = sender as Button;
                if (button != null) button.IsEnabled = false;

                MessageBox.Show($"Starting initialization for {SelectedWorkstationType}.\nThis may take a moment...", "Initialization Started", MessageBoxButton.OK, MessageBoxImage.Information);

                bool initSuccess = await InitializeWorkstationAsync(SelectedWorkstationType); // Modified to return bool

                if (initSuccess)
                {
                    _logger.LogInformation("Initialization successful. Proceeding to login via ViewModel.");
                    // Now, perform login using LoginViewModel
                    // EmployeeCode should already be set on _loginViewModel via binding
                    // _loginViewModel.EmployeeCode = currentEmployeeId; // Already set by binding

                    int craneId = MapWorkstationTypeToCraneId(SelectedWorkstationType);
                    if (craneId == 0)
                    {
                        _logger.LogError($"Could not map workstation type '{SelectedWorkstationType}' to a valid CraneId. Aborting login.");
                        MessageBox.Show($"Configuration error: Unknown workstation type '{SelectedWorkstationType}'. Cannot proceed with login.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (button != null) button.IsEnabled = true;
                        return;
                    }
                    _loginViewModel.SelectedCraneId = craneId;
                    
                    await _loginViewModel.LoginAsync();
                    // Navigation is handled by _loginViewModel.LoginSuccessful event
                }
                else
                {
                    _logger.LogError("Initialization failed. Login step will be skipped.");
                    // MessageBox is shown in InitializeWorkstationAsync for failure
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Button_Click (Initialize) handler.");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var button = sender as Button;
                if (button != null) button.IsEnabled = true;
            }
        }

        private int MapWorkstationTypeToCraneId(string workstationType)
        {
            if (_loginViewModel?.CraneOptions == null) return 0;
            // Normalize names if necessary, or use a more robust mapping
            // Current LoginViewModel.CraneOptions: "Ludige", "LPV", "מצבר קטן", "מצבר גדול"
            // Current LoginWindow Tags: "לודיגה", "מצבר גדול", "מצבר קטן", "LPV"
            switch (workstationType)
            {
                case "לודיגה": // Assuming this matches "Ludige"
                    return _loginViewModel.CraneOptions.FirstOrDefault(c => c.Name.Equals("Ludige", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
                case "LPV":
                    return _loginViewModel.CraneOptions.FirstOrDefault(c => c.Name.Equals("LPV", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
                case "מצבר קטן":
                    return _loginViewModel.CraneOptions.FirstOrDefault(c => c.Name.Equals("מצבר קטן", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
                case "מצבר גדול":
                    return _loginViewModel.CraneOptions.FirstOrDefault(c => c.Name.Equals("מצבר גדול", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
                default:
                    _logger.LogWarning($"Unknown workstation type for CraneId mapping: {workstationType}");
                    return 0;
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

        private async void ExecuteInitialize(object parameter) // This is for the InitializeCommand on LoginWindow
        {
            _logger.LogInformation("Initialize command executed (ExecuteInitialize).");
            
            string currentEmployeeId = _loginViewModel.EmployeeCode; // Get from ViewModel
            this.EmployeeId = currentEmployeeId; // Sync to local property if still used by CanExecuteInitialize or InitializeWorkstationAsync

            if (!CanExecuteInitialize(null)) // CanExecuteInitialize uses LoginWindow's EmployeeId and SelectedWorkstationType
            {
                _logger.LogWarning("Initialize command executed but conditions not met (Workstation: {SelectedWorkstation}, EmployeeId: {EmployeeId})", SelectedWorkstationType, currentEmployeeId);
                MessageBox.Show("Please select a workstation and enter your Employee ID.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool initSuccess = await InitializeWorkstationAsync(SelectedWorkstationType);
            if (initSuccess)
            {
                _logger.LogInformation("Initialization successful via command. Proceeding to login via ViewModel.");
                int craneId = MapWorkstationTypeToCraneId(SelectedWorkstationType);
                 if (craneId == 0)
                {
                    _logger.LogError($"Could not map workstation type '{SelectedWorkstationType}' to a valid CraneId. Aborting login.");
                    MessageBox.Show($"Configuration error: Unknown workstation type '{SelectedWorkstationType}'. Cannot proceed with login.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _loginViewModel.SelectedCraneId = craneId;
                // EmployeeCode should already be on _loginViewModel
                await _loginViewModel.LoginAsync();
                // Navigation handled by LoginSuccessful event
            }
        }

        // Returns true if successful, false otherwise.
        private async Task<bool> InitializeWorkstationAsync(string workstationType)
        {
            // EmployeeId for logging within this method will use this.EmployeeId, which should be set by the caller (Button_Click or ExecuteInitialize)
            // from _loginViewModel.EmployeeCode.
            _logger.LogInformation("Starting first-time initialization for Workstation: {WorkstationType}, Employee ID: {EmployeeId}", workstationType, this.EmployeeId);

            try
            {
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    _logger.LogInformation("Clearing existing data before seeding...");
                    bool cleared = await ClearAllDataAsync(unitOfWork);
                    if (!cleared)
                    {
                        _logger.LogError("Database clearing failed. Aborting initialization.");
                        MessageBox.Show("Failed to clear existing database data. Initialization aborted.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false; 
                    }

                    _logger.LogInformation("Gathering predefined data for {WorkstationType}...", workstationType);
                    List<Level> predefinedLevels = GetPredefinedLevels();
                    List<Cell> predefinedCells = GetPredefinedCells();
                    List<Pallet> predefinedPallets = GetPredefinedPallets();
                    List<Finger> predefinedFingers = GetPredefinedFingers();

                    await AddAllDataAsync(unitOfWork, predefinedLevels, predefinedCells, predefinedPallets, predefinedFingers);
                    
                    var savedCells = (await unitOfWork.Cells.GetAllAsync()).ToList();
                    var savedPallets = (await unitOfWork.Pallets.GetAllAsync()).ToList();
                    var savedFingers = (await unitOfWork.Fingers.GetAllAsync()).ToList();

                    List<DbTask> predefinedTasks = GetPredefinedTasks(savedCells, savedPallets, savedFingers);
                    List<PalletInCell> predefinedPalletInCells = GetPredefinedPalletInCells(savedCells, savedPallets);

                    await AddTasksAndAssociationsAsync(unitOfWork, predefinedTasks, predefinedPalletInCells);

                    _logger.LogInformation("Creating Main Trolley for workstation: {WorkstationType}", SelectedWorkstationType);
                    var newTrolley = new Trolley
                    {
                        DisplayName = SelectedWorkstationType,
                        Position = 1,
                    };
                    await unitOfWork.Trolleys.AddAsync(newTrolley);
                    await unitOfWork.CompleteAsync(); 

                    _logger.LogInformation("Created new Trolley (ID: {TrolleyId}, DisplayName: {DisplayName}) for workstation.", newTrolley.Id, newTrolley.DisplayName);
                    
                    // Pass this.EmployeeId (which was set from ViewModel's EmployeeCode)
                    await AddConfigurationAsync(unitOfWork, (int)newTrolley.Id, this.EmployeeId); 
                } 

                _logger.LogInformation("Initialization completed successfully.");
                // REMOVED: NavigateToMainWindow(); // Navigation is now handled after LoginViewModel.LoginAsync succeeds
                return true; // Indicate success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during first-time initialization for Workstation: {WorkstationType}", workstationType);
                MessageBox.Show($"Failed to initialize workstation '{workstationType}'.\nPlease check logs.\nError: {ex.Message}",
                                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false; // Indicate failure
            }
        }

        private async System.Threading.Tasks.Task<bool> ClearAllDataAsync(IUnitOfWork unitOfWork)
        {
            try
            {
                _logger.LogInformation("Attempting to clear all data using IUnitOfWork.ClearAllDataAsync()...");
                await unitOfWork.ClearAllDataAsync();
                _logger.LogInformation("Successfully cleared all data via IUnitOfWork.ClearAllDataAsync().");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data using IUnitOfWork.ClearAllDataAsync()");
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

        private async System.Threading.Tasks.Task AddConfigurationAsync(IUnitOfWork unitOfWork, int activeTrolleyId, string initializedByEmployeeId)
        {
            try
            {
                if (!await unitOfWork.Configurations.AnyAsync())
                {
                    var newConfiguration = new Configuration
                    {
                        WorkstationType = SelectedWorkstationType, // From LoginWindow property
                        InitializedByEmployeeId = initializedByEmployeeId, // Passed in
                        InitializedAt = DateTime.UtcNow,
                        ActiveTrolleyId = activeTrolleyId,
                        // CraneId will be set by LoginViewModel after successful login
                    };
                    await unitOfWork.Configurations.AddAsync(newConfiguration);
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Configuration added successfully with WorkstationType: {WorkstationType}, EmployeeId: {EmployeeId}, ActiveTrolleyId: {ActiveTrolleyId}",
                        SelectedWorkstationType, initializedByEmployeeId, activeTrolleyId);
                }
                else
                {
                    _logger.LogWarning("Configuration already exists, skipping add. This might indicate an issue if first-time setup is re-run without proper DB clearing for Configurations table.");
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
                    c.Level == info.Cell.Level &&
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
                    Description = $"Move {palletForRetrieval1.DisplayName} from Cell {sourceCellForRetrieval1.DisplayName} (Pos:{sourceCellForRetrieval1.Position}, Lvl:{sourceCellForRetrieval1.Level}) to Finger {destFingerForRetrieval1.DisplayName}",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Retrieval,
                    CellEndLocationId = sourceCellForRetrieval1.Id, // Source for retrieval
                    FingerLocationId = destFingerForRetrieval1.Id,  // Destination for retrieval
                    PalletId = palletForRetrieval1.Id,  // Link by UldCode or AwbCode
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
                    Description = $"Move {palletForRetrieval2.DisplayName} from Cell {sourceCellForRetrieval2.DisplayName} (Pos:{sourceCellForRetrieval2.Position}, Lvl:{sourceCellForRetrieval2.Level}) to Finger {destFingerForRetrieval2.DisplayName}",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Retrieval,
                    CellEndLocationId = sourceCellForRetrieval2.Id,
                    FingerLocationId = destFingerForRetrieval2.Id,
                    PalletId = palletForRetrieval2.Id,
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
                    Description = $"Move {palletForStorage1.DisplayName} from Finger {sourceFingerForStorage1.DisplayName} to Cell {destCellForStorage1.DisplayName} (Pos:{destCellForStorage1.Position}, Lvl:{destCellForStorage1.Level})",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Storage,
                    FingerLocationId = sourceFingerForStorage1.Id,  // Source for storage
                    CellEndLocationId = destCellForStorage1.Id,    // Destination for storage
                    PalletId = palletForStorage1.Id,
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
                    Description = $"Move {palletForStorage2.DisplayName} from Finger {sourceFingerForStorage2.DisplayName} to Cell {destCellForStorage2.DisplayName} (Pos:{destCellForStorage2.Position}, Lvl:{destCellForStorage2.Level})",
                    DownloadDate = DateTime.UtcNow,
                    TaskTypeId = (int)EM.Maman.Models.Enums.TaskType.Storage,
                    FingerLocationId = sourceFingerForStorage2.Id,
                    CellEndLocationId = destCellForStorage2.Id,
                    PalletId = palletForStorage2.Id,
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
