﻿﻿﻿﻿﻿﻿using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using EM.Maman.DriverClient.Services;
using System.Windows;
using EM.Maman.Models.DisplayModels;
using EM.Maman.DAL;
using EM.Maman.Services;
using Microsoft.Extensions.Logging;
using EM.Maman.Models.Interfaces;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics; // Keep for InitializeApplicationAsync logging
using EM.Maman.Models.CustomModels; // Added for TaskDetails
using Microsoft.EntityFrameworkCore; // Added for Include
using EM.Maman.Models.Enums; // Added for TaskStatus, TaskType
using EM.Maman.DriverClient; // Added for AuthenticationDialog (future)
using DbTask = EM.Maman.Models.LocalDbModels.Task; // Alias for DB Task model
using TaskStatusEnum = EM.Maman.Models.Enums.TaskStatus; // Alias for Enum TaskStatus
using TaskTypeEnum = EM.Maman.Models.Enums.TaskType;
using Microsoft.Extensions.DependencyInjection; // Alias for Enum TaskType

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Main view model that coordinates between different parts of the application
    /// </summary>
    public partial class MainViewModel // Assuming partial class for INotifyPropertyChanged implementation
    {
        #region Fields and Properties

        private readonly ILogger<MainViewModel> _logger;
        private readonly IOpcService _opcService; // Re-added
        private readonly IUnitOfWork _unitOfWork; // Added
        public readonly IDispatcherService _dispatcherService; // Added

        private bool _isWarehouseViewActive = true;
        private bool _isMapViewActive = false;
        private bool _isFingerAuthenticationViewActive = false; // Added
        private int? _currentFingerPositionValue = null; // Added - Store the raw OPC value for the current finger
        private string _currentFingerDisplayName; // Display name of finger im currently on
        private RelayCommand _showWarehouseViewCommand;
        private RelayCommand _showTasksViewCommand;
        private RelayCommand _showMapViewCommand;
        private RelayCommand _showTasksListViewCommand;
        private RelayCommand _showAuthenticationDialogCommand; // Changed to non-generic
        private RelayCommand _goToStorageLocationCommand; // Changed to non-generic
        private RelayCommand _changeDestinationCommand; // Changed to non-generic
        private RelayCommand _openCreateTaskDialogCommand; // Kept non-generic

        private Trolley _currentTrolley;


        /// <summary>
        /// Gets or sets whether the warehouse view is currently active
        /// </summary>
        public bool IsWarehouseViewActive
        {
            get => _isWarehouseViewActive;
            set
            {
                if (_isWarehouseViewActive != value)
                {
                    _isWarehouseViewActive = value;
                    OnPropertyChanged(nameof(IsWarehouseViewActive));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the special finger authentication view is active.
        /// </summary>
        public bool IsFingerAuthenticationViewActive
        {
            get => _isFingerAuthenticationViewActive;
            set
            {
                if (_isFingerAuthenticationViewActive != value)
                {
                    _isFingerAuthenticationViewActive = value;
                    OnPropertyChanged(nameof(IsFingerAuthenticationViewActive));
                    // When this view becomes active/inactive, the main task view should update visibility
                    OnPropertyChanged(nameof(IsDefaultTaskViewActive));
                }
            }
        }
        public void NotifyStorageItemsChanged()
        {
            OnPropertyChanged(nameof(HasPalletsReadyForStorage));
        }
        /// <summary>
        /// Helper property to control visibility of the default task view (opposite of finger auth view)
        /// </summary>
        public bool IsDefaultTaskViewActive => !IsFingerAuthenticationViewActive;


        /// <summary>
        /// Gets or sets whether the map view is currently active (vs tasks list view)
        /// </summary>
        public bool IsMapViewActive
        {
            get => _isMapViewActive;
            set
            {
                if (_isMapViewActive != value)
                {
                    _isMapViewActive = value;
                    OnPropertyChanged(nameof(IsMapViewActive));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current trolley
        /// </summary>
        public Trolley CurrentTrolley
        {
            get => _currentTrolley;
            set
            {
                if (_currentTrolley != value)
                {
                    _currentTrolley = value;
                    OnPropertyChanged(nameof(CurrentTrolley));
                }
            }
        }
        public string CurrentFingerDisplayName
        {
            get => _currentFingerDisplayName;
            set
            {
                if (_currentFingerDisplayName != value)
                {
                    _currentFingerDisplayName = value;
                    OnPropertyChanged(nameof(CurrentFingerDisplayName));
                }
            }
        }

        /// <summary>
        /// Command to show the warehouse view
        /// </summary>
        public ICommand ShowWarehouseViewCommand => _showWarehouseViewCommand ??= new RelayCommand(_ => ShowWarehouseView(), _ => !IsWarehouseViewActive);

        /// <summary>
        /// Command to show the tasks view
        /// </summary>
        public ICommand ShowTasksViewCommand => _showTasksViewCommand ??= new RelayCommand(_ => ShowTasksView(), _ => IsWarehouseViewActive);

        /// <summary>
        /// Command to show the map view within the tasks view
        /// </summary>
        public ICommand ShowMapViewCommand => _showMapViewCommand ??= new RelayCommand(_ => ShowMapView(), _ => !IsMapViewActive && !IsWarehouseViewActive);

        /// <summary>
        /// Command to show the tasks list view within the tasks view
        /// </summary>
        public ICommand ShowTasksListViewCommand => _showTasksListViewCommand ??= new RelayCommand(_ => ShowTasksListView(), _ => IsMapViewActive && !IsWarehouseViewActive);

        /// <summary>
        /// Command to show the authentication dialog for a specific pallet.
        /// </summary>
        public ICommand ShowAuthenticationDialogCommand => _showAuthenticationDialogCommand ??= new RelayCommand(ExecuteShowAuthenticationDialog); // Changed

        /// <summary>
        /// Command to navigate the trolley to the storage location for an authenticated pallet.
        /// </summary>
        public ICommand GoToStorageLocationCommand => _goToStorageLocationCommand ??= new RelayCommand(ExecuteGoToStorageLocation, CanExecuteGoToStorageLocation); // Changed

        /// <summary>
        /// Command to change the destination of a storage task (future implementation).
        /// </summary>
        public ICommand ChangeDestinationCommand => _changeDestinationCommand ??= new RelayCommand(ExecuteChangeDestination, CanExecuteChangeDestination); // Changed

        /// <summary>
        /// Command to open the manual task creation dialog.
        /// </summary>
        public ICommand OpenCreateTaskDialogCommand => _openCreateTaskDialogCommand ??= new RelayCommand(ExecuteOpenCreateTaskDialog);


        /// <summary>

        /// <summary>
        /// View model for OPC operations
        /// </summary>
        public OpcViewModel OpcVM { get; }

        /// <summary>
        /// View model for trolley state and operations
        /// </summary>
        public TrolleyViewModel TrolleyVM { get; }

        /// <summary>
        /// View model for trolley operations
        /// </summary>
        public TrolleyOperationsViewModel TrolleyOperationsVM { get; }

        /// <summary>
        /// View model for warehouse operations
        /// </summary>
        public WarehouseViewModel WarehouseVM { get; }

        /// <summary>
        /// View model for task management
        /// </summary>
        public TaskViewModel TaskVM { get; }

        /// <summary>
        /// Collection of pallets at the current finger waiting for authentication.
        /// </summary>
        public ObservableCollection<PalletAuthenticationItem> PalletsToAuthenticate { get; } = new ObservableCollection<PalletAuthenticationItem>();

        /// <summary>
        /// Collection of authenticated pallets ready for their storage task.
        /// </summary>
        public ObservableCollection<PalletStorageTaskItem> PalletsReadyForStorage { get; } = new ObservableCollection<PalletStorageTaskItem>();

        /// <summary>
        /// Gets a value indicating whether there are any pallets ready for storage.
        /// Used to control UI visibility.
        /// </summary>
        public bool HasPalletsReadyForStorage => PalletsReadyForStorage.Any();


        #endregion

        #region Commands

        public ICommand MoveTrolleyUpCommand { get; }
        public ICommand MoveTrolleyDownCommand { get; }
        public ICommand TestLoadLeftCellCommand { get; }
        public ICommand TestLoadRightCellCommand { get; }
        public ICommand TestUnloadLeftCellCommand { get; }
        public ICommand TestUnloadRightCellCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class
        /// </summary>
        public MainViewModel(
            IOpcService opcService,
            IUnitOfWork unitOfWork,
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            ILoggerFactory loggerFactory,
            TrolleyViewModel trolleyViewModel)
        {
             _logger = loggerFactory.CreateLogger<MainViewModel>();
             _opcService = opcService; // Store injected service
             _unitOfWork = unitOfWork; // Store injected UnitOfWork
             _dispatcherService = dispatcherService; // Store injected DispatcherService

            _logger.LogInformation("MainViewModel constructor START");

            // Initialize trolley
            CurrentTrolley = new Trolley { Id = 1, DisplayName = "Main Trolley", Position = 1 };

            // Initialize view models
            TrolleyVM = trolleyViewModel; // Use injected instance
            WarehouseVM = new WarehouseViewModel(unitOfWork); // Creates new instance

            // Create the OPC view model
            OpcVM = new OpcViewModel(
                opcService,
                dispatcherService,
                loggerFactory.CreateLogger<OpcViewModel>());

            // Subscribe to position changes from OPC
            OpcVM.PositionChanged += OnPositionChanged;

            // Initialize TrolleyOperationsViewModel
            TrolleyOperationsVM = new TrolleyOperationsViewModel(TrolleyVM, CurrentTrolley);
            TrolleyOperationsVM.SetMainViewModel(this); // Pass the MainViewModel instance to TrolleyOperationsVM

            // Initialize TaskViewModel with all required dependencies
            // Pass the MainViewModel logger factory, TaskViewModel will create its own logger
            TaskVM = new TaskViewModel(
                _unitOfWork, // Pass stored UoW
                connectionManager,
                opcService, // Pass opcService directly to TaskVM constructor
                _dispatcherService, // Pass stored dispatcher
                loggerFactory.CreateLogger<TaskViewModel>(), // Create specific logger instance
                this // Pass the MainViewModel instance
            );

            // Initialize commands
            MoveTrolleyUpCommand = new RelayCommand(_ => TrolleyMethods_MoveTrolleyUp(), _ => CurrentTrolley?.Position > 0);
            MoveTrolleyDownCommand = new RelayCommand(_ => TrolleyMethods_MoveTrolleyDown(), _ => true);

            // Initialize test commands
            TestLoadLeftCellCommand = TrolleyOperationsVM.TestLoadLeftCellCommand;
            TestLoadRightCellCommand = TrolleyOperationsVM.TestLoadRightCellCommand;
            TestUnloadLeftCellCommand = TrolleyOperationsVM.TestUnloadLeftCellCommand;
            TestUnloadRightCellCommand = TrolleyOperationsVM.TestUnloadRightCellCommand;

            // DO NOT start async initialization in constructor anymore
            // OpcVM.InitializeAsync();
            // Child VMs InitializeAsync will be called by InitializeApplicationAsync

             _logger.LogInformation("MainViewModel constructor END.");
        }

        /// <summary>
        /// Asynchronously initializes the MainViewModel and its dependencies.
        /// Should be called after the MainWindow is loaded.
        /// </summary>
        public async System.Threading.Tasks.Task InitializeApplicationAsync() // Fully qualify Task
        {
            _logger.LogInformation("Starting MainViewModel asynchronous initialization...");
            var initSw = Stopwatch.StartNew();

            try
            {
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


                // Any other main initialization steps...
                _logger.LogInformation("MainViewModel asynchronous initialization completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MainViewModel asynchronous initialization");
                // Handle or surface the error appropriately
                 MessageBox.Show($"An error occurred during application initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                initSw.Stop();
                _logger.LogInformation($"MainViewModel InitializeApplicationAsync Time: {initSw.ElapsedMilliseconds} ms");
            }
        }


        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles position changes from the OPC service.
        /// Also checks if the new position is a finger location to trigger authentication view.
        /// </summary>
        private async void OnPositionChanged(object sender, int positionValue) // Made async
        {
            CheckForArrivalAtDestination(positionValue);
            // Extract level and position
            int level = positionValue / 100; // Assuming level is encoded in hundreds
            int position = positionValue % 100;

            // Update the trolley's position
            if (CurrentTrolley != null)
            {
                CurrentTrolley.Position = position;
                OnPropertyChanged(nameof(CurrentTrolley));
            }

            // Update the TrolleyViewModel with level info
            if (TrolleyVM != null)
            {
                TrolleyVM.UpdateTrolleyPosition(level, position);
            }

            // Update the WarehouseViewModel with level info
            if (WarehouseVM != null)
            {
                WarehouseVM.CurrentLevelNumber = level;
            }

            // --- Finger Authentication Logic ---
            bool isFinger = await IsFingerLocationAsync(positionValue); // Check if it's a finger

            if (isFinger)
            {
                // Only reload if it's a *different* finger than the one we are currently showing
                if (_currentFingerPositionValue != positionValue)
                {
                    _logger.LogInformation("Arrived at finger location (PositionValue: {PositionValue}). Loading pallets for authentication.", positionValue);
                    _currentFingerPositionValue = positionValue; // Store the current finger position value
                    await LoadPalletsForFingerAuthenticationAsync(positionValue);
                }
                var finger = (await _unitOfWork.Fingers.FindAsync(f => f.Position == positionValue)).FirstOrDefault();
                if (finger!= null)
                {
                    CurrentFingerDisplayName = finger.DisplayName;
                }
                // Else: Already at this finger, do nothing, keep current view state.
            }
            else
            {
                // Not at a finger location
                if (_currentFingerPositionValue != null) // Check if we were previously at a finger
                {
                    _logger.LogInformation("Left finger location (Previous PositionValue: {PositionValue}). Hiding authentication view.", _currentFingerPositionValue);
                    // Reset finger state only if we were previously at one
                    _currentFingerPositionValue = null;
                    IsFingerAuthenticationViewActive = false;
                    _dispatcherService.Invoke(() => // Ensure UI updates happen on the UI thread
                    {
                        PalletsToAuthenticate.Clear(); // Only clear the authentication list
                        // PalletsReadyForStorage.Clear(); // DO NOT CLEAR STORAGE LIST HERE
                        // OnPropertyChanged(nameof(HasPalletsReadyForStorage)); // No need to notify if storage list didn't change
                    });
                }
                // Else: Wasn't at a finger before, still not at one, do nothing.
            }
            // --- End Finger Authentication Logic ---
        }


        /// <summary>
        /// Checks if the given OPC position value corresponds to a known finger location.
        /// </summary>
        /// <param name="positionValue">The raw position value from OPC.</param>
        /// <returns>True if it's a finger location, false otherwise.</returns>
        private async System.Threading.Tasks.Task<bool> IsFingerLocationAsync(int positionValue) // Fully qualify Task
        {
            // TODO: Implement actual logic to determine if positionValue is a finger.
            // This might involve:
            // 1. Querying the database for a Finger with a matching Position property.
            // 2. Checking against a predefined list or range of position values.
            // 3. Checking the 'level' part of positionValue (e.g., level 0 might be fingers).

            // Placeholder implementation: Assume position 0 is the only finger for now.
            int position = positionValue % 100;
            // int level = positionValue / 100;
            // Example: Check if a finger exists in DB with this position value
            try
            {
                 var finger = (await _unitOfWork.Fingers.FindAsync(f => f.Position == positionValue)).FirstOrDefault();
                 return finger != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if position {PositionValue} is a finger location.", positionValue);
                return false; // Safer to assume not a finger if DB query fails
            }

            // return position == 0; // Simple placeholder
        }

        /// <summary>
        /// Loads pallets waiting for authentication at the specified finger location.
        /// </summary>
        /// <param name="fingerPositionValue">The raw OPC position value of the finger.</param>
        private async System.Threading.Tasks.Task LoadPalletsForFingerAuthenticationAsync(int fingerPositionValue)
        {
            IsFingerAuthenticationViewActive = false; // Hide initially while loading
            var itemsToAdd = new List<PalletAuthenticationItem>();

            try
            {
                // Find the Finger ID corresponding to the position value
                var finger = (await _unitOfWork.Fingers.FindAsync(f => f.Position == fingerPositionValue)).FirstOrDefault();
                if (finger == null)
                {
                    _logger.LogWarning("Could not find Finger entity for position value {PositionValue}.", fingerPositionValue);
                    _dispatcherService.Invoke(() => // Clear lists on UI thread
                    {
                        PalletsToAuthenticate.Clear();
                        PalletsReadyForStorage.Clear(); // Also clear storage list
                    });
                    return;
                }
                long fingerId = finger.Id;

                // Find tasks originating from this finger that are not yet executed
                var dbTasks = await _unitOfWork.Tasks.FindAsync(
                    predicate: t => t.FingerLocationId == fingerId && (t.IsExecuted == false || t.IsExecuted == null), // Use IsExecuted flag
                    include: q => q.Include(t => t.CellEndLocation) // Include potential destination
                                   // Cannot include FingerLocation directly if FingerLocationId is the FK
                );

                foreach (var task in dbTasks)
                {
                    // Fetch Pallet separately using PalletId (UldCode)
                    Pallet pallet = null;
                    if (!string.IsNullOrEmpty(task.PalletId))
                    {
                        pallet = (await _unitOfWork.Pallets.FindAsync(p => p.UldCode == task.PalletId)).FirstOrDefault();
                    }

                    if (pallet != null)
                    {
                        // Create TaskDetails, passing the separately loaded pallet and the known finger
                        var taskDetails = TaskDetails.FromDbModel(task, pallet, finger, null, task.CellEndLocation);
                        itemsToAdd.Add(new PalletAuthenticationItem(pallet, taskDetails));
                    }
                    else
                    {
                        _logger.LogWarning("Task {TaskId} from finger {FingerId} is missing Pallet information or Pallet could not be found for PalletId '{PalletId}'.", task.Id, fingerId, task.PalletId);
                    }
                }

                 _dispatcherService.Invoke(() => // Update collections on UI thread
                 {
                     PalletsToAuthenticate.Clear(); // Only clear the authentication list
                     // PalletsReadyForStorage.Clear(); // DO NOT CLEAR STORAGE LIST HERE
                     // OnPropertyChanged(nameof(HasPalletsReadyForStorage)); // No need to notify if storage list didn't change here
                     foreach (var item in itemsToAdd)
                     {
                         // Assign the command here, pointing to the MainViewModel's command execution method
                         item.AuthenticateCommand = ShowAuthenticationDialogCommand;
                         PalletsToAuthenticate.Add(item);
                     }
                     IsFingerAuthenticationViewActive = PalletsToAuthenticate.Any(); // Show view only if there are items
                     _logger.LogInformation("Loaded {Count} pallets for authentication at finger {FingerId}.", PalletsToAuthenticate.Count, fingerId);
                 });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pallets for finger authentication (PositionValue: {PositionValue}).", fingerPositionValue);
                 _dispatcherService.Invoke(() => // Clear lists on UI thread
                 {
                     PalletsToAuthenticate.Clear();
                     PalletsReadyForStorage.Clear();
                     OnPropertyChanged(nameof(HasPalletsReadyForStorage)); // Notify UI
                 });
                MessageBox.Show($"Error loading pallets for authentication: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #endregion

        #region Finger Authentication Command Methods

        /// <summary>
        /// Shows the authentication dialog for the selected pallet item.
        /// </summary>
        private void ExecuteShowAuthenticationDialog(object parameter) // Changed parameter type
        {
            if (parameter is not PalletAuthenticationItem item) return; // Cast and check

            _logger.LogInformation("Showing authentication dialog for Pallet ULD: {UldCode}", item.PalletDetails?.UldCode ?? "N/A");

            var dialogVM = new AuthenticationDialogViewModel(item);
            var dialog = new AuthenticationDialog // Assuming AuthenticationDialog.xaml exists
            {
                DataContext = dialogVM,
                Owner = Application.Current.MainWindow // Set owner for proper dialog behavior
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _logger.LogInformation("Authentication dialog confirmed for Pallet ULD: {UldCode}. Entered Code: {EnteredCode}",
                    item.PalletDetails?.UldCode ?? "N/A", dialogVM.EnteredUldCode);
                // Pass the ViewModel containing the entered code and the original item
                ConfirmAuthentication(dialogVM, item); // Pass original item too
            }
            else
            {
                 _logger.LogInformation("Authentication dialog cancelled for Pallet ULD: {UldCode}", item.PalletDetails?.UldCode ?? "N/A");
            }
        }

        /// <summary>
        /// Confirms the authentication based on the dialog result.
        /// </summary>
        private async void ConfirmAuthentication(AuthenticationDialogViewModel dialogVM, PalletAuthenticationItem itemToAuth) // Added item parameter
        {
            // var itemToAuth = dialogVM.ItemToAuthenticate; // Already passed in
            if (itemToAuth?.PalletDetails == null)
            {
                _logger.LogError("ConfirmAuthentication called with invalid item.");
                return;
            }

            // Verify entered code against Pallet's UldCode (case-insensitive comparison)
            if (string.Equals(dialogVM.EnteredUldCode?.Trim(), itemToAuth.PalletDetails.ImportUnit?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Authentication successful for Pallet ULD: {UldCode}", itemToAuth.PalletDetails.UldCode);
                MessageBox.Show($"Pallet {itemToAuth.PalletDetails.UldCode} authenticated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Remove from authentication list (use dispatcher)
                _dispatcherService.Invoke(() => PalletsToAuthenticate.Remove(itemToAuth));

                // --- Load Pallet onto Trolley (Requirement #1) ---
                var finger = itemToAuth.OriginalTask?.SourceFinger;
                var pallet = itemToAuth.PalletDetails;
                if (finger != null && pallet != null && TrolleyVM != null)
                {
                    bool loaded = false;
                    // Finger Side: 0 = Left, 1 = Right (Based on TrolleyViewModel test data)
                    if (finger.Side == 0) // Load to Left Cell if possible
                    {
                        if (!TrolleyVM.LeftCell.IsOccupied)
                        {
                            TrolleyVM.LoadPalletIntoLeftCell(pallet);
                            _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Left Trolley Cell.", pallet.UldCode);
                            loaded = true;
                        }
                        else if (!TrolleyVM.RightCell.IsOccupied) // Try Right Cell as fallback
                        {
                            TrolleyVM.LoadPalletIntoRightCell(pallet);
                            _logger.LogInformation("Left Trolley Cell occupied. Loaded authenticated pallet {UldCode} onto Right Trolley Cell instead.", pallet.UldCode);
                            loaded = true;
                        }
                    }
                    else // finger.Side == 1 (or default), Load to Right Cell if possible
                    {
                        if (!TrolleyVM.RightCell.IsOccupied)
                        {
                            TrolleyVM.LoadPalletIntoRightCell(pallet);
                            _logger.LogInformation("Loaded authenticated pallet {UldCode} onto Right Trolley Cell.", pallet.UldCode);
                            loaded = true;
                        }
                        else if (!TrolleyVM.LeftCell.IsOccupied) // Try Left Cell as fallback
                        {
                            TrolleyVM.LoadPalletIntoLeftCell(pallet);
                            _logger.LogInformation("Right Trolley Cell occupied. Loaded authenticated pallet {UldCode} onto Left Trolley Cell instead.", pallet.UldCode);
                            loaded = true;
                        }
                    }

                    if (!loaded)
                    {
                        _logger.LogWarning("Could not load authenticated pallet {UldCode} onto trolley - both cells occupied.", pallet.UldCode);
                        MessageBox.Show($"Cannot load pallet {pallet.UldCode}. Trolley is full.", "Trolley Full", MessageBoxButton.OK, MessageBoxImage.Warning);
                        // Decide if task creation should proceed if trolley is full? For now, let it proceed.
                    }
                }
                else
                {
                    _logger.LogWarning("Could not load authenticated pallet onto trolley - missing Finger or Pallet details.");
                }
                // --- End Load Pallet onto Trolley ---


                // Create the new storage task
                await CreateStorageTaskFromAuthenticationAsync(itemToAuth);
            }
            else
            {
                 _logger.LogWarning("Authentication failed for Pallet ULD: {UldCode}. Expected: {ExpectedCode}, Entered: {EnteredCode}",
                     itemToAuth.PalletDetails.UldCode, itemToAuth.PalletDetails.UldCode, dialogVM.EnteredUldCode);
                MessageBox.Show($"Authentication failed. Entered ID '{dialogVM.EnteredUldCode}' does not match.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// Creates a new storage task after successful authentication.
        /// </summary>
        private async System.Threading.Tasks.Task CreateStorageTaskFromAuthenticationAsync(PalletAuthenticationItem authenticatedItem)
        {
            if (authenticatedItem?.PalletDetails == null || authenticatedItem.OriginalTask == null)
            {
                _logger.LogError("Cannot create storage task: Invalid authenticated item data.");
                return;
            }

            try
            {
                _logger.LogInformation("Creating storage task for authenticated Pallet ULD: {UldCode}", authenticatedItem.PalletDetails.UldCode);

                // 1. Determine Destination Cell ID from the OriginalTask's DestinationCell object
                long? destinationCellId = authenticatedItem.OriginalTask.DestinationCell?.Id;

                if (!destinationCellId.HasValue)
                {
                    _logger.LogInformation("Original task for Pallet ULD {UldCode} has no destination. Attempting automatic assignment.", authenticatedItem.PalletDetails.UldCode);
                    // TODO: Implement automatic destination assignment logic
                    // destinationCellId = await _destinationAssignmentService.GetNextAvailableCellIdAsync(authenticatedItem.PalletDetails.Type); // Example
                    MessageBox.Show("Automatic destination assignment not yet implemented. Cannot create storage task.", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // For now, fail if no destination is provided by the original task
                     return; // Remove this return when auto-assignment is ready
                }

                // 2. Get Destination Cell Details (needed for TaskDetails)
                Cell destinationCell = null;
                if (destinationCellId.HasValue)
                {
                    destinationCell = await _unitOfWork.Cells.GetByIdAsync(destinationCellId.Value);
                    if (destinationCell == null)
                    {
                         _logger.LogWarning("Could not find destination cell with ID {CellId} for Pallet ULD {UldCode}.", destinationCellId.Value, authenticatedItem.PalletDetails.UldCode);
                         // Decide how to handle: fail task creation or proceed without full destination info?
                         // For now, let's proceed but log the warning.
                    }
                }


                // 3. Create New Task (DB Model) - Use Alias DbTask
                var newTask = new DbTask
                {
                    // Id will be generated by DB
                    PalletId = authenticatedItem.PalletDetails.UldCode, // Link by UldCode (Correct mapping)
                    TaskTypeId = await GetTaskTypeIdAsync(1), // Get ID for Storage type (assuming Code 1 = Storage)
                    FingerLocationId = authenticatedItem.OriginalTask.SourceFinger?.Id, // Source is the finger we are at
                    CellEndLocationId = destinationCellId, // Determined destination
                    DownloadDate = DateTime.Now, // Or use server time if available
                    IsExecuted = false, // Initial state
                    // Priority = authenticatedItem.OriginalTask.IsPriority, // No Priority in DB model
                    // UserId = authenticatedItem.OriginalTask.UserId, // No UserId in DB model
                    // Copy other relevant fields if they exist in DbTask model (e.g., Name, Description, Code)
                    Name = $"Store {authenticatedItem.PalletDetails.UldCode}", // Example name
                    Description = $"Store pallet {authenticatedItem.PalletDetails.UldCode} from finger {authenticatedItem.OriginalTask.SourceFinger?.DisplayName ?? "N/A"} to cell {destinationCell?.DisplayName ?? "N/A"}",
                    Code = authenticatedItem.OriginalTask.Code // Copy original code? Or generate new?
                };

                // 4. Save New Task to DB
                await _unitOfWork.Tasks.AddAsync(newTask);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully saved new storage task (ID: {TaskId}) for Pallet ULD: {UldCode} to database.", newTask.Id, authenticatedItem.PalletDetails.UldCode);


                // 5. Create PalletStorageTaskItem for UI
                // Need the source finger details for TaskDetails.FromDbModel
                Finger sourceFinger = null;
                if (newTask.FingerLocationId.HasValue)
                {
                    sourceFinger = await _unitOfWork.Fingers.GetByIdAsync(newTask.FingerLocationId.Value);
                }

                var storageTaskDetails = TaskDetails.FromDbModel(newTask, authenticatedItem.PalletDetails, sourceFinger, null, destinationCell);
                
                var storageItem = new PalletStorageTaskItem(authenticatedItem.PalletDetails, storageTaskDetails)
                {
                    // Assign commands
                    GoToStorageCommand = this.GoToStorageLocationCommand,
                    ChangeDestinationCommand = this.ChangeDestinationCommand
                };

                // 6. Add to UI Collection (use dispatcher)
                _dispatcherService.Invoke(() =>
                {
                    PalletsReadyForStorage.Add(storageItem);
                    OnPropertyChanged(nameof(HasPalletsReadyForStorage)); // Notify UI
                });
                 _logger.LogInformation("Added PalletStorageTaskItem for Task ID {TaskId} to UI.", newTask.Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating storage task for authenticated Pallet ULD: {UldCode}", authenticatedItem.PalletDetails?.UldCode ?? "N/A");
                MessageBox.Show($"Error creating storage task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Navigates the trolley to the storage location specified by the task item.
        /// </summary>
        private async void ExecuteGoToStorageLocation(object parameter) // Changed parameter type
        {
            if (parameter is not PalletStorageTaskItem item || item.StorageTask?.DestinationCell == null) // Cast and check
            {
                _logger.LogWarning("Cannot navigate to storage: Invalid parameter or destination cell information missing for Task ID {TaskId}.", (parameter as PalletStorageTaskItem)?.StorageTask?.Id ?? 0);
                MessageBox.Show("Cannot navigate: Destination cell information is missing.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var destinationCell = item.StorageTask.DestinationCell;
            int? level = destinationCell.HeightLevel;
            int? position = destinationCell.Position;

            if (!level.HasValue || !position.HasValue)
            {
                 _logger.LogWarning("Cannot navigate to storage: Destination cell (ID: {CellId}) has incomplete level/position.", destinationCell.Id);
                 MessageBox.Show("Cannot navigate: Destination cell has incomplete level/position information.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }

            // Calculate OPC position value (assuming Level * 100 + Position)
            short targetPositionValue = (short)((level.Value * 100) + position.Value);
            short commandCode = 1; // Assuming 1 is 'Go' command - VERIFY THIS

            string positionSpNodeId = "ns=2;s=s7.s7 300.maman.PositionRequest"; // Use actual Node IDs
            string commandNodeId = "ns=2;s=s7.s7 300.maman.Control";     // Use actual Node IDs

            try
            {
                _logger.LogInformation("Executing GoToStorageLocation for Task ID: {TaskId}, Pallet ULD: {UldCode}, Destination Cell: {CellName} (L:{Level}, P:{Position}), Target OPC Value: {TargetValue}",
                    item.StorageTask.Id, item.PalletDetails?.UldCode ?? "N/A", destinationCell.DisplayName, level.Value, position.Value, targetPositionValue);

                // TODO: Set IsNavigating state? Update StatusMessage?
                // IsNavigating = true; // Assuming IsNavigating property exists or will be added
                // StatusMessage = $"Navigating to cell {destinationCell.DisplayName}..."; // Assuming StatusMessage property exists

                    // Use the stored _opcService instance
                    await _opcService.WriteRegisterAsync(positionSpNodeId, targetPositionValue);
                    await _opcService.WriteRegisterAsync(commandNodeId, commandCode);

                    _logger.LogInformation("Navigation command sent for Task ID {TaskId}.", item.StorageTask.Id);
                item.StorageTask.ActiveTaskStatus = ActiveTaskStatus.transit; // Update task status
                OnPropertyChanged(nameof(PalletsReadyForStorage)); // Notify UI of changes

                // Optionally remove the item from PalletsReadyForStorage after sending command? Or wait for confirmation?
                // _dispatcherService.Invoke(() => PalletsReadyForStorage.Remove(item));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GoToStorageLocation for Task ID {TaskId}.", item.StorageTask.Id);
                MessageBox.Show($"Error navigating to storage location: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Reset navigation state if applicable
                // IsNavigating = false;
                // StatusMessage = "Navigation error.";
            }
        }

        private bool CanExecuteGoToStorageLocation(object parameter) // Changed parameter type
        {
            if (parameter is not PalletStorageTaskItem item) return false; // Cast and check
            // Can navigate if the item and its destination cell are valid
            return item.StorageTask?.DestinationCell != null &&
                   item.StorageTask.DestinationCell.HeightLevel.HasValue &&
                   item.StorageTask.DestinationCell.Position.HasValue;
                   // && !IsNavigating; // Add check if IsNavigating property exists
        }

        /// <summary>
        /// Placeholder for changing the destination (future implementation).
        /// </summary>
        private void ExecuteChangeDestination(object parameter) // Changed parameter type
        {
             if (parameter is not PalletStorageTaskItem item) return; // Cast and check
             _logger.LogInformation("ExecuteChangeDestination called for Task ID: {TaskId}", item.StorageTask?.Id ?? 0);
             MessageBox.Show("Changing destination is not yet implemented.", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
             // TODO: Implement logic to show a dialog/view to select a new destination cell
             // Update item.StorageTask.DestinationCellId and item.StorageTask.DestinationCell
             // Save the change to the database (_unitOfWork.Tasks.Update, _unitOfWork.CompleteAsync)
        }

        private bool CanExecuteChangeDestination(object parameter) // Changed parameter type
        {
            if (parameter is not PalletStorageTaskItem item) return false; // Cast and check
            // Enable if item is valid (add more conditions if needed, e.g., task status)
            // return item != null; // Basic check
            return false; // Disabled for now
        }

        /// <summary>
        /// Opens the manual task creation dialog.
        /// Adds newly created tasks for the current finger to the auth list.
        /// </summary>
        private async void ExecuteOpenCreateTaskDialog(object parameter)
        {
            _logger.LogInformation("ExecuteOpenCreateTaskDialog called.");

            try
            {
                // Get the dialog directly from DI container - no need to manually get the view model
                var dialog = (App.Current as App)?.ServiceProvider.GetRequiredService<ManualTaskDialog>();

                if (dialog == null)
                {
                    _logger.LogError("Failed to resolve ManualTaskDialog from service provider");
                    return;
                }

                // No need to set the DataContext - it's already set by DI in the constructor

                if (dialog.ShowDialog() == true) // Check dialog result first
                {
                    // Get the view model from the dialog's DataContext
                    if (dialog.DataContext is ManualTaskViewModel manualTaskVM)
                    {
                        // Determine which VM holds the details based on selection
                        TaskDetails newTaskDetails = manualTaskVM.IsImportSelected ?
                            manualTaskVM.ImportVM?.TaskDetails : manualTaskVM.ExportVM?.TaskDetails;

                        if (newTaskDetails == null)
                        {
                            _logger.LogWarning("Manual task dialog closed with OK, but TaskDetails were null.");
                            return;
                        }

                        // Save the task first
                        bool saved = await TaskVM.SaveTaskToDatabase(newTaskDetails);

                        if (saved)
                        {
                            _logger.LogInformation("Manual task (ID: {TaskId}) created and saved.", newTaskDetails.Id);
                            // Add to the main TaskVM list
                            TaskVM.Tasks.Add(newTaskDetails);

                            // The rest of your existing code for handling the created task
                            if (newTaskDetails.IsImportTask &&
                                newTaskDetails.SourceFinger?.Id != null &&
                                _currentFingerPositionValue.HasValue)
                            {
                                if (newTaskDetails.SourceFinger?.Position == _currentFingerPositionValue)
                                {
                                    _logger.LogInformation("Newly created manual task (ID: {TaskId}) is for the current finger. Adding to authentication list.", newTaskDetails.Id);

                                    string palletUldCode = newTaskDetails.Pallet?.UldCode;
                                    if (newTaskDetails.Pallet == null && !string.IsNullOrEmpty(palletUldCode))
                                    {
                                        newTaskDetails.Pallet = (await _unitOfWork.Pallets.FindAsync(p => p.UldCode == palletUldCode)).FirstOrDefault();
                                    }

                                    if (newTaskDetails.Pallet != null)
                                    {
                                        var authItem = new PalletAuthenticationItem(newTaskDetails.Pallet, newTaskDetails)
                                        {
                                            AuthenticateCommand = this.ShowAuthenticationDialogCommand
                                        };
                                        _dispatcherService.Invoke(() => PalletsToAuthenticate.Add(authItem));
                                        IsFingerAuthenticationViewActive = true;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Could not find Pallet details for newly created manual task (ID: {TaskId}, Pallet ULD: {PalletUldCode}). Cannot add to auth list.", newTaskDetails.Id, palletUldCode ?? "N/A");
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to save manually created task.");
                        }
                    }
                    else
                    {
                        _logger.LogError("Dialog's DataContext is not of type ManualTaskViewModel");
                    }
                }
                else
                {
                    _logger.LogInformation("Manual task creation cancelled or failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteOpenCreateTaskDialog");
                MessageBox.Show($"Error creating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #endregion

        #region Trolley Methods

        /// <summary>
        /// Moves the trolley up
        /// </summary>
        private void TrolleyMethods_MoveTrolleyUp()
        {
            if (CurrentTrolley.Position > 0)
            {
                CurrentTrolley.Position--;
                OnPropertyChanged(nameof(CurrentTrolley));

                // Update commands can execute state
                (MoveTrolleyUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveTrolleyDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Moves the trolley down
        /// </summary>
        private void TrolleyMethods_MoveTrolleyDown()
        {
            if (CurrentTrolley.Position < 23)
            {
                CurrentTrolley.Position++;
                OnPropertyChanged(nameof(CurrentTrolley));

                // Update commands can execute state
                (MoveTrolleyUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveTrolleyDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Method to execute when a pick operation is initiated from the UI
        /// </summary>
        public void PickPallet(Cell sourceCell, Pallet pallet)
        {
            // Check if the trolley has an available cell
            if (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.RightCell.IsOccupied)
            {
                // No available cell, show message to user
                MessageBox.Show("Trolley has no available cells. Please unload a cell first.");
                return;
            }

            // Determine which cell to use (left first if available)
            if (!TrolleyVM.LeftCell.IsOccupied)
            {
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
            }
            else
            {
                TrolleyVM.LoadPalletIntoRightCell(pallet);
            }

            // In a real application, you would remove the pallet from the source cell
            // through the repository and update the database
        }

        /// <summary>
        /// Method to execute when a put operation is initiated from the UI
        /// </summary>
        public void PutPallet(Cell destinationCell, string cellSide)
        {
            Pallet pallet = null;

            // Determine which trolley cell to unload from
            if (cellSide == "Left" && TrolleyVM.LeftCell.IsOccupied)
            {
                pallet = TrolleyVM.RemovePalletFromLeftCell();
            }
            else if (cellSide == "Right" && TrolleyVM.RightCell.IsOccupied)
            {
                pallet = TrolleyVM.RemovePalletFromRightCell();
            }

            if (pallet == null)
            {
                // No pallet found in the selected cell
                MessageBox.Show("No pallet in the selected trolley cell.");
                return;
            }

            // In a real application, you would update the destination cell through the repository
            // and update the database
        }

        #endregion

        // INotifyPropertyChanged implementation is likely in a partial class (e.g., MainViewModel.PropertyChanged.cs)
        // public event PropertyChangedEventHandler PropertyChanged; // Already removed
        // protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) // Already removed
        // {
        //     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // }

        #region Helper Methods

        /// <summary>
        /// Gets the database ID for a given TaskType Code.
        /// </summary>
        /// <param name="taskTypeCode">The Code (e.g., 1 for Storage, 2 for Retrieval) of the TaskType.</param>
        /// <returns>The database ID (long) or null if not found.</returns>
        private async Task<long?> GetTaskTypeIdAsync(int taskTypeCode)
        {
            try
            {
                // Query the database for the TaskType entity with the matching code
                var taskTypeEntity = (await _unitOfWork.TaskTypes.FindAsync(tt => tt.Code == taskTypeCode)).FirstOrDefault(); // Assuming TaskTypes repository exists

                if (taskTypeEntity != null)
                {
                    return taskTypeEntity.Id;
                }
                else
                {
                    _logger.LogWarning("Could not find TaskType in database with Code {TaskTypeCode}.", taskTypeCode);
                    return null; // Return null if not found
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving TaskType ID for Code {TaskTypeCode}.", taskTypeCode);
                return null; // Return null on error
            }
        }

        #endregion
    }
}
