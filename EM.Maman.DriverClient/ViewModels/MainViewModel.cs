﻿using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using EM.Maman.Models.LocalDbModels;
using System.Collections.ObjectModel;
using EM.Maman.Models.DisplayModels;
using EM.Maman.DAL;
using EM.Maman.Services;
using Microsoft.Extensions.Logging;
using EM.Maman.Models.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // Added for IServiceProvider
using System.Linq; // Added for FirstOrDefault
using EM.Maman.Models.Dtos; // Added for TaskItemDto
using System.Threading.Tasks; // Added for Task
using EM.Maman.DriverClient.Views; // Added for SelectHndDestinationDialog
using System.Windows; // Added for MessageBox
using EM.Maman.Models.Enums; // Added for ActiveTaskStatus

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Main view model that coordinates between different parts of the application
    /// </summary>
    public partial class MainViewModel
    {
        #region Fields

        private readonly ILogger<MainViewModel> _logger;
        private readonly ILoggerFactory _loggerFactory; // Added
        private readonly IOpcService _opcService;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        public readonly IDispatcherService _dispatcherService;
        private readonly IServiceProvider _serviceProvider; // Added
        private readonly ICurrentUserContext _currentUserContext; // Added
        private readonly IMamanHttpService _mamanHttpService; // Added

        // Navigation tracking fields
        private System.Threading.Tasks.TaskCompletionSource<bool> _activeNavigationTcs;
        private short? _activeNavigationTargetPosition;
        private string _activeNavigationTargetStatus = "Idle"; // Assuming "Idle" means stopped/completed

        // private User _currentUser; // Removed, state now in _currentUserContext
        private bool _isLogoutDrawerOpen; // Added

        private bool _isWarehouseViewActive = true;
        private bool _isMapViewActive = false;
        private bool _isFingerAuthenticationViewActive = false;
        private int? _currentFingerPositionValue = null;
        private int _currentCellLevel; // Added to store current cell level
        private int _currentCellPosition; // Added to store current cell position
        private string _currentFingerDisplayName;
        private string _currentCellDisplayName; // Added for cell auth UI
        private ObservableCollection<Finger> _availableFingers = new ObservableCollection<Finger>();
        private Trolley _currentTrolley;

        private PalletAuthenticationItem _activeCellAuthenticationItem;
        public PalletAuthenticationItem ActiveCellAuthenticationItem
        {
            get => _activeCellAuthenticationItem;
            set => SetProperty(ref _activeCellAuthenticationItem, value);
        }

        private bool _isCellAuthenticationViewActive;
        public bool IsCellAuthenticationViewActive
        {
            get => _isCellAuthenticationViewActive;
            set
            {
                if (SetProperty(ref _isCellAuthenticationViewActive, value))
                {
                    OnPropertyChanged(nameof(ShouldShowTasksPanel)); 
                    OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
                }
            }
        }

        #endregion

        #region Properties

        public bool IsSimulationMode { get; private set; }
        public string ConnectionStatus { get; private set; } = "Disconnected";

        public User CurrentUser => _currentUserContext.CurrentUser; // Get from context

        public string CurrentUserInitial => _currentUserContext.CurrentUser?.FirstName?.FirstOrDefault().ToString().ToUpper() ?? "?";

        public bool IsLogoutDrawerOpen
        {
            get => _isLogoutDrawerOpen;
            set => SetProperty(ref _isLogoutDrawerOpen, value);
        }

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

        public bool IsFingerAuthenticationViewActive
        {
            get => _isFingerAuthenticationViewActive;
            set
            {
                if (SetProperty(ref _isFingerAuthenticationViewActive, value)) // Assuming SetProperty calls OnPropertyChanged
                {
                    // IsFingerAuthenticationViewActive directly affects ShouldShowTasksPanel
                    OnPropertyChanged(nameof(ShouldShowTasksPanel));
                    // ShouldShowDefaultPhoto depends on ShouldShowTasksPanel
                    OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
                    // IsDefaultTaskViewActive also depends on ShouldShowTasksPanel
                    OnPropertyChanged(nameof(IsDefaultTaskViewActive)); 
                }
            }
        }

        public bool IsDefaultTaskViewActive => !ShouldShowTasksPanel;

        /// <summary>
        /// Determines whether to show the tasks panel (gray background) in the current task view.
        /// Shows the panel when: 1) At a finger location, 2) Has active storage tasks, 3) Has active retrieval tasks, 4) At a cell with retrieval task
        /// </summary>
        public bool ShouldShowTasksPanel
        {
            get
            {
                // Show tasks panel when:
                // 1. At a finger location
                // 2. Has active storage tasks
                // 3. Has active retrieval tasks
                // 4. At a cell with retrieval task
                // 5. Cell authentication view is active
                return IsFingerAuthenticationViewActive ||
                       IsCellAuthenticationViewActive || // Added
               HasPalletsReadyForStorage || 
               HasPalletsForRetrieval || 
               IsAtCellWithRetrievalTask ||
               HasPalletsReadyForDelivery; // Added this condition
    }
}

        /// <summary>
        /// Determines whether to show the default photo in the current task view.
        /// Shows the photo when: 1) No active tasks, 2) Not at a finger location, 3) Not at a cell with retrieval task
        /// </summary>
        public bool ShouldShowDefaultPhoto
        {
            get
            {
                // Show default photo when not showing tasks panel
                return !ShouldShowTasksPanel;
            }
        }

        /// <summary>
        /// Determines if the current position corresponds to a cell with a retrieval task.
        /// This will be implemented in the future for retrieval tasks.
        /// </summary>
        private bool IsAtCellWithRetrievalTask
        {
            get
            {
                // If we are at a finger location, specific finger logic takes precedence.
                // A finger location is not considered a "cell with retrieval task" for this property's purpose.
                if (_currentFingerPositionValue.HasValue)
                {
                    return false;
                }

                // Check if any active retrieval task's source cell matches the current cell location
                foreach (var taskItem in PalletsForRetrieval)
                {
                    if (taskItem.RetrievalTask?.SourceCell != null &&
                        taskItem.RetrievalTask.SourceCell.Level == _currentCellLevel &&
                        taskItem.RetrievalTask.SourceCell.Position == _currentCellPosition)
                    {
                        // Found an active retrieval task for the current cell
                        return true;
                    }
                }
                return false; // No active retrieval task for the current cell
            }
        }

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
        
        public string CurrentCellDisplayName
        {
            get => _currentCellDisplayName;
            set => SetProperty(ref _currentCellDisplayName, value);
        }

        public ObservableCollection<Finger> AvailableFingers
        {
            get => _availableFingers;
            private set
            {
                if (_availableFingers != value)
                {
                    _availableFingers = value;
                    OnPropertyChanged(nameof(AvailableFingers));
                }
            }
        }

        public ObservableCollection<TaskItemDto> ServerTasks { get; } = new ObservableCollection<TaskItemDto>();
        public System.Windows.Input.ICommand RefreshServerTasksCommand { get; private set; }
        private bool _isLoadingServerTasks;
        public bool IsLoadingServerTasks
        {
            get => _isLoadingServerTasks;
            set => SetProperty(ref _isLoadingServerTasks, value);
        }

        #endregion

        #region View Models

        public OpcViewModel OpcVM { get; }
        public TrolleyViewModel TrolleyVM { get; }
        public TrolleyOperationsViewModel TrolleyOperationsVM { get; }
        public WarehouseViewModel WarehouseVM { get; }
        public TaskViewModel TaskVM { get; }
        public FilterViewModel FilterVM { get; } // Added for the filter control

        #endregion

        #region Collections

        public ObservableCollection<PalletAuthenticationItem> PalletsToAuthenticate { get; } = new ObservableCollection<PalletAuthenticationItem>();
        public ObservableCollection<PalletStorageTaskItem> PalletsReadyForStorage { get; } = new ObservableCollection<PalletStorageTaskItem>();
        public ObservableCollection<PalletRetrievalTaskItem> PalletsForRetrieval { get; } = new ObservableCollection<PalletRetrievalTaskItem>();
        // PalletsReadyForDelivery is already defined in MainViewModel.TaskOperations.cs (partial class)
        public bool HasPalletsReadyForStorage => PalletsReadyForStorage.Any();
        public bool HasPalletsForRetrieval => PalletsForRetrieval.Any();
        public bool HasPalletsReadyForDelivery => PalletsReadyForDelivery.Any();


        // Commands for retrieval delivery
        public System.Windows.Input.ICommand GoToRetrievalDestinationCommand { get; private set; }
        public System.Windows.Input.ICommand UnloadAtDestinationCommand { get; private set; }
        public System.Windows.Input.ICommand AuthenticatePalletAtCellCommand { get; private set; }


        /// <summary>
        /// Notifies that the storage items collection has changed,
        /// prompting updates for dependent UI properties.
        /// </summary>
        public void NotifyStorageItemsChanged()
        {
            OnPropertyChanged(nameof(HasPalletsReadyForStorage));
            OnPropertyChanged(nameof(ShouldShowTasksPanel));
            OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
            OnPropertyChanged(nameof(IsDefaultTaskViewActive));
        }

        /// <summary>
        /// Notifies that the retrieval items collection has changed,
        /// prompting updates for dependent UI properties.
        /// </summary>
        public void NotifyRetrievalItemsChanged()
        {
            OnPropertyChanged(nameof(HasPalletsForRetrieval));
            OnPropertyChanged(nameof(ShouldShowTasksPanel));
            OnPropertyChanged(nameof(ShouldShowDefaultPhoto));
            OnPropertyChanged(nameof(IsDefaultTaskViewActive));
        }

        #endregion

        #region Constructor

        public MainViewModel(
            IOpcService opcService,
            IUnitOfWorkFactory unitOfWorkFactory,
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            ILoggerFactory loggerFactory,
            TrolleyViewModel trolleyViewModel,
            IConfiguration configuration,
            IServiceProvider serviceProvider, // Added IServiceProvider
            ICurrentUserContext currentUserContext, // Added ICurrentUserContext
            IMamanHttpService mamanHttpService) // Added IMamanHttpService
        {
            _logger = loggerFactory.CreateLogger<MainViewModel>();
            _opcService = opcService;
            _unitOfWorkFactory = unitOfWorkFactory;
            _dispatcherService = dispatcherService;
            this._loggerFactory = loggerFactory; // Store ILoggerFactory
            _serviceProvider = serviceProvider; // Store IServiceProvider
            _currentUserContext = currentUserContext ?? throw new System.ArgumentNullException(nameof(currentUserContext)); // Store ICurrentUserContext
            _mamanHttpService = mamanHttpService ?? throw new System.ArgumentNullException(nameof(mamanHttpService)); // Store IMamanHttpService
            IsSimulationMode = configuration.GetValue<bool>("AppSettings:UseSimulationMode");

            _logger.LogInformation("MainViewModel constructor START");

            // CurrentTrolley will be loaded from DB during InitializeApplicationAsync.
            // For now, TrolleyOperationsVM needs a non-null Trolley. We'll use a temporary one,
            // which will be replaced by the DB-loaded instance in InitializeApplicationAsync.
            var tempTrolleyForOpsVm = new Trolley { Id = 1, DisplayName = "Temp Initializing Trolley", Position = 1 };


            // Initialize view models
            TrolleyVM = trolleyViewModel;
            
            // Create a UnitOfWork instance for WarehouseViewModel initialization
            using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
            {
                WarehouseVM = new WarehouseViewModel(unitOfWork);
            }

            // Subscribe to connection changes
            opcService.ConnectivityChanged += (s, isConnected) =>
            {
                ConnectionStatus = isConnected ? "Connected" : "Disconnected";
                OnPropertyChanged(nameof(ConnectionStatus));
            };

            // Create the OPC view model
            OpcVM = new OpcViewModel(
                opcService,
                dispatcherService,
                loggerFactory.CreateLogger<OpcViewModel>());

            // Subscribe to position changes from OPC
            OpcVM.PositionChanged += OnPositionChanged;
            OpcVM.CurrentStatusChanged += OnTrolleyStatusChanged; // Subscribe to new status event

            // Initialize TrolleyOperationsViewModel with a temporary trolley instance.
            // This will be updated with the DB-loaded trolley in InitializeApplicationAsync.
            TrolleyOperationsVM = new TrolleyOperationsViewModel(
                TrolleyVM,
                tempTrolleyForOpsVm,
                _unitOfWorkFactory, // Pass the factory
                loggerFactory.CreateLogger<TrolleyOperationsViewModel>(), // Create and pass the logger
                _opcService // Pass the IOpcService instance
            );
            TrolleyOperationsVM.SetMainViewModel(this);

            // Initialize TaskViewModel with all required dependencies
            TaskVM = new TaskViewModel(
                _unitOfWorkFactory.CreateUnitOfWork(), // Create a new UnitOfWork instance
                connectionManager,
                opcService,
                _dispatcherService,
                loggerFactory.CreateLogger<TaskViewModel>(),
                this
            );

            // Initialize FilterViewModel
            FilterVM = new FilterViewModel(); // Assuming default constructor is sufficient for now

            InitializeCommands();
            RefreshServerTasksCommand = new RelayCommand(async _ => await LoadTasksFromServerAsync(), _ => !IsLoadingServerTasks);

            _logger.LogInformation("MainViewModel constructor END.");
        }

        #endregion

        public async System.Threading.Tasks.Task ShowSelectCellDialogForRetrievalItemAsync(PalletRetrievalTaskItem item)
        {
            if (item == null || item.RetrievalTask == null)
            {
                _logger.LogWarning("ShowSelectCellDialogForRetrievalItemAsync called with null item or item.RetrievalTask.");
                return;
            }

            try
            {
                var dialog = _serviceProvider.GetRequiredService<SelectHndDestinationDialog>(); // Keep this for the dialog view

                // Manually create the ViewModel, passing the required Pallet and Cell
                var loadedPallet = item.PalletDetails;
                var sourceCell = item.RetrievalTask?.SourceCell; // Null check for safety

                // Ensure we have the pallet before proceeding
                if (loadedPallet == null)
                {
                    _logger.LogError("PalletDetails is null for Task ID: {TaskId}. Cannot show SelectHndDestinationDialog.", item.RetrievalTask?.Id);
                    MessageBox.Show("Cannot change destination: Pallet details are missing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // sourceCell can be null if it's a new task or not yet defined; the SelectHndDestinationViewModel's constructor
                // and LoadDestinationsAsync method appear to handle a null sourceCell gracefully.

                var dialogViewModel = new SelectHndDestinationViewModel(
                    _unitOfWorkFactory, // Already available in MainViewModel
                    _loggerFactory.CreateLogger<SelectHndDestinationViewModel>(), // Create logger using existing _loggerFactory
                    loadedPallet,
                    sourceCell 
                );
                
                dialog.DataContext = dialogViewModel;

                _logger.LogInformation("Showing SelectHndDestinationDialog for Task ID: {TaskId}", item.RetrievalTask.Id);
                if (dialog.ShowDialog() == true)
                {
                    if (dialogViewModel.SelectedCell != null)
                    {
                        _logger.LogInformation("Cell selected from dialog: {CellDisplayName} for Task ID: {TaskId}", dialogViewModel.SelectedCell.DisplayName, item.RetrievalTask.Id);
                        // Update the task item
                        item.RetrievalTask.DestinationCell = dialogViewModel.SelectedCell;
                        item.RetrievalTask.DestinationFinger = null; // Clear finger destination
                        item.SelectedFinger = null; // Clear selected finger in the item's ViewModel, this will trigger its OnPropertyChanged for DestinationDisplay

                        // Save the updated task
                        bool saved = await TaskVM.SaveTaskToDatabase(item.RetrievalTask); // Saves the new destination
                        if (saved)
                        {
                            _logger.LogInformation("Successfully updated and saved destination for Task ID: {TaskId} to Cell: {CellId}", item.RetrievalTask.Id, dialogViewModel.SelectedCell.Id);
                            
                            // Set status to transit BEFORE reloading, so DB has it.
                            item.RetrievalTask.ActiveTaskStatus = ActiveTaskStatus.transit;
                            bool statusSaved = await TaskVM.SaveTaskToDatabase(item.RetrievalTask); // Save again with new status

                            if (!statusSaved) {
                                _logger.LogError("Failed to save updated ActiveTaskStatus to transit for Task ID: {TaskId}", item.RetrievalTask.Id);
                                // Proceed with caution, status might be inconsistent
                            }

                            await TaskVM.LoadTasksAsync(); // Reload all tasks

                            var updatedTaskDetailsFromLoad = TaskVM.Tasks.FirstOrDefault(t => t.Id == item.RetrievalTask.Id);
                            if (updatedTaskDetailsFromLoad != null)
                            {
                                item.RetrievalTask = updatedTaskDetailsFromLoad; // Ensure item in collection uses the reloaded TaskDetails
                                // Now item.RetrievalTask.ActiveTaskStatus should be 'transit' from the DB.
                                _logger.LogInformation("Refreshed RetrievalTask for item in UI for Task ID: {TaskId}. Current ActiveStatus: {Status}", item.RetrievalTask.Id, item.RetrievalTask.ActiveTaskStatus);
                            }
                            else
                            {
                                _logger.LogWarning("Could not find reloaded TaskDetails for Task ID: {TaskId} after LoadTasksAsync. UI might not reflect latest destination.", item.RetrievalTask.Id);
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to save updated destination for Task ID: {TaskId}", item.RetrievalTask.Id);
                            MessageBox.Show("Failed to save the new destination. Please try again.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            // Potentially revert changes in item.RetrievalTask if save fails and it's critical
                        }
                    }
                    else
                    {
                        _logger.LogInformation("SelectHndDestinationDialog closed with OK but no cell selected for Task ID: {TaskId}", item.RetrievalTask.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("SelectHndDestinationDialog cancelled for Task ID: {TaskId}", item.RetrievalTask.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShowSelectCellDialogForRetrievalItemAsync for Task ID: {TaskId}", item.RetrievalTask?.Id);
                MessageBox.Show($"An error occurred while trying to change the destination: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public async System.Threading.Tasks.Task SetCurrentUserAsync(int userId)
        {
            await _currentUserContext.SetCurrentUserAsync(userId);
            // Notify that CurrentUser and dependent properties have changed
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(CurrentUserInitial));

            if (_currentUserContext.CurrentUser == null && userId != 0)
            {
                _logger.LogError("Failed to set current user via CurrentUserContext for UserID {UserId}", userId);
            }
            else if (userId != 0)
            {
                _logger.LogInformation("Current user set in MainViewModel via CurrentUserContext: {EmployeeCode}", _currentUserContext.CurrentUser.EmployeeCode);
            }
        }

        public async System.Threading.Tasks.Task LoadTasksFromServerAsync()
        {
            if (IsLoadingServerTasks) return;
            IsLoadingServerTasks = true;
            ((RelayCommand)RefreshServerTasksCommand).RaiseCanExecuteChanged();
            _logger.LogInformation("Attempting to load tasks from server...");

            try
            {
                var apiResponse = await _mamanHttpService.GetTasksAsync();
                if (apiResponse != null && apiResponse.IsCompletedSuccessfully && apiResponse.Result != null)
                {
                    _dispatcherService.Invoke(() =>
                    {
                        ServerTasks.Clear();
                        foreach (var taskDto in apiResponse.Result)
                        {
                            ServerTasks.Add(taskDto);
                        }
                        _logger.LogInformation("Successfully loaded {TaskCount} tasks from server.", ServerTasks.Count);
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to load tasks from server or empty response. Status: {Status}, Error: {Error}", apiResponse?.Status, apiResponse?.Exception?.ToString() ?? "N/A");
                    // Optionally show a message to the user
                    // MessageBox.Show("Failed to load tasks from server.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tasks from server.");
                // Optionally show a message to the user
                // MessageBox.Show($"An error occurred while loading tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoadingServerTasks = false;
                ((RelayCommand)RefreshServerTasksCommand).RaiseCanExecuteChanged();
            }
        }

        private void ExecuteLogout()
        {
            _logger.LogInformation("LogoutCommand executed. Attempting to close MainWindow and show LoginWindow.");
            _currentUserContext.ClearCurrentUser(); // Clear user on logout
            try
            {
                var windowToClose = System.Windows.Application.Current.MainWindow as MainWindow; // Try to cast
                _logger.LogInformation($"Current Application.MainWindow is of type: {System.Windows.Application.Current.MainWindow?.GetType().FullName}");


                if (windowToClose != null)
                {
                    _logger.LogInformation($"Identified MainWindow (Title: '{windowToClose.Title}', HashCode: {windowToClose.GetHashCode()}) to close.");
                }
                else
                {
                    _logger.LogWarning("Application.Current.MainWindow was not of type MainWindow at the start of ExecuteLogout. This is unexpected.");
                    windowToClose = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (windowToClose != null) {
                        _logger.LogInformation($"Found MainWindow (Title: '{windowToClose.Title}', HashCode: {windowToClose.GetHashCode()}) via Application.Windows collection.");
                    } else {
                        _logger.LogWarning("Could not find any open MainWindow in Application.Windows collection.");
                    }
                }

                if (_serviceProvider == null)
                {
                    _logger.LogCritical("ServiceProvider is null in MainViewModel. Cannot perform logout.");
                    System.Windows.MessageBox.Show("Critical error during logout: Service provider not available.", "Logout Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
                _logger.LogInformation($"Showing new LoginWindow (HashCode: {loginWindow.GetHashCode()}).");
                loginWindow.Show(); 

                if (windowToClose != null)
                {
                    _logger.LogInformation($"Attempting to close identified MainWindow (HashCode: {windowToClose.GetHashCode()}).");
                    windowToClose.Close();
                    _logger.LogInformation($"Call to Close() on MainWindow (HashCode: {windowToClose.GetHashCode()}) completed.");
                }
                else
                {
                    _logger.LogWarning("No MainWindow instance was identified to close. Logout may leave old window open.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during logout process.");
                System.Windows.MessageBox.Show($"An error occurred during logout: {ex.Message}", "Logout Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                // System.Windows.Application.Current.Shutdown(); // Consider if shutdown is desired on logout error
            }
        }
    }
}
