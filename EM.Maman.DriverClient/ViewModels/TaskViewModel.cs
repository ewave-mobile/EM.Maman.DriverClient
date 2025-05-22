﻿﻿﻿﻿using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks; // Keep this for Task
using System.Windows.Input;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel; // Required for INotifyPropertyChanged
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using EM.Maman.Common.Constants; // Required for CallerMemberName

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Helper class to hold finger information and its pallet count for the storage view.
    /// </summary>
    public class FingerStorageInfo : INotifyPropertyChanged
    {
        private Finger _finger;
        private int _palletCount;
        private List<object> _palletPlaceholders; // For the grey squares

        public Finger Finger
        {
            get => _finger;
            set { _finger = value; OnPropertyChanged(); }
        }

        public int PalletCount
        {
            get => _palletCount;
            set
            {
                if (_palletCount != value)
                {
                    _palletCount = value;
                    OnPropertyChanged();
                    // Update placeholders when count changes
                    PalletPlaceholders = Enumerable.Repeat(new object(), _palletCount).ToList();
                }
            }
        }

        // This collection will be bound to the ItemsControl for the grey squares
        public List<object> PalletPlaceholders
        {
            get => _palletPlaceholders;
            private set { _palletPlaceholders = value; OnPropertyChanged(); }
        }

        public FingerStorageInfo(Finger finger, int palletCount)
        {
            Finger = finger;
            // Initialize placeholders with empty list if count is 0
            _palletPlaceholders = new List<object>();
            PalletCount = palletCount; // Set PalletCount last to trigger placeholder generation
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TaskViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly IConnectionManager _connectionManager;
        private readonly IOpcService _opcService;
        public readonly IDispatcherService _dispatcherService; // Keep this for local use if needed
        private readonly ILogger<TaskViewModel> _logger;
        public readonly MainViewModel _mainVM; // Added reference to MainViewModel

        private ObservableCollection<TaskDetails> _tasks;
        private ObservableCollection<TaskDetails> _pendingTasks;
        private ObservableCollection<TaskDetails> _completedTasks;
        private ObservableCollection<TaskDetails> _storageTasks; // Keep for now, might be used elsewhere
        private ObservableCollection<TaskDetails> _retrievalTasks;
        private ObservableCollection<FingerStorageInfo> _availableStorageFingers; // Added for the new view
        private ObservableCollection<int> _recentlyCompletedTaskIds = new ObservableCollection<int>(); // For delayed removal from pending list
        private TaskDetails _selectedTask;
        private TaskDetails _activeTask;
        private bool _isTaskActive = false ;
        private bool _isNavigating;
        private bool _isLoading;
        private bool _isStorageTabSelected = true;
        private string _statusMessage;
        private int _currentStep;
        private int _totalSteps;
        private RelayCommand _createImportTaskCommand;
        private RelayCommand _createExportTaskCommand;
        private RelayCommand _createManualTaskCommand;
        private RelayCommand _startTaskCommand;
        private RelayCommand _completeTaskCommand;
        private RelayCommand _navigateToSourceCommand;
        private RelayCommand _navigateToDestinationCommand;
        private RelayCommand _cancelTaskCommand;
        private RelayCommand _refreshTasksCommand;
        private RelayCommand _nextNavigationCommand;
        private RelayCommand _selectStorageTabCommand;
        private RelayCommand _selectRetrievalTabCommand;
        private RelayCommand _goToFingerCommand; // Added command


        public ObservableCollection<TaskDetails> Tasks
        {
            get => _tasks;
            set
            {
                if (_tasks != value)
                {
                    _tasks = value;
                    OnPropertyChanged();
                    _ = UpdateFilteredListsAsync(); // Fire and forget as property setter cannot be async
                }
            }
        }

        public ObservableCollection<TaskDetails> PendingTasks
        {
            get => _pendingTasks;
            set
            {
                if (_pendingTasks != value)
                {
                    _pendingTasks = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TaskDetails> CompletedTasks
        {
            get => _completedTasks;
            set
            {
                if (_completedTasks != value)
                {
                    _completedTasks = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TaskDetails> StorageTasks
        {
            get => _storageTasks;
            set
            {
                if (_storageTasks != value)
                {
                    _storageTasks = value;
                    OnPropertyChanged();
                    // Note: StorageTasksCount is now repurposed for AvailableStorageFingers count
                }
            }
        }

        public ObservableCollection<TaskDetails> RetrievalTasks
        {
            get => _retrievalTasks;
            set
            {
                if (_retrievalTasks != value)
                {
                    _retrievalTasks = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RetrievalTasksCount));
                }
            }
        }

        /// <summary>
        /// Collection of fingers available for storage, with their pallet counts.
        /// </summary>
        public ObservableCollection<FingerStorageInfo> AvailableStorageFingers
        {
            get => _availableStorageFingers;
            set
            {
                if (_availableStorageFingers != value)
                {
                    _availableStorageFingers = value;
                    OnPropertyChanged();
                    // Update the count property when the collection changes
                    OnPropertyChanged(nameof(AvailableStorageFingerCount)); 
                }
            }
        }

        // Counts available fingers for the storage header
        public int AvailableStorageFingerCount => AvailableStorageFingers?.Count ?? 0;
        // Counts actual pending storage task items
        public int PendingStorageItemCount => StorageTasks?.Count ?? 0;
        public int RetrievalTasksCount => RetrievalTasks?.Count ?? 0; // Keep retrieval count as is

        public bool IsStorageTabSelected
        {
            get => _isStorageTabSelected;
            set
            {
                if (_isStorageTabSelected != value)
                {
                    _isStorageTabSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public TaskDetails SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (_selectedTask != value)
                {
                    _selectedTask = value;
                    OnPropertyChanged();
                    UpdateCommandAvailability();
                }
            }
        }

        public TaskDetails ActiveTask
        {
            get => _activeTask;
            set
            {
                if (_activeTask != value)
                {
                    _activeTask = value;
                    IsTaskActive = _activeTask != null;
                    OnPropertyChanged();
                    UpdateCommandAvailability();
                }
            }
        }

        public bool IsTaskActive
        {
            get => _isTaskActive;
            set
            {
                if (_isTaskActive != value)
                {
                    _isTaskActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsNavigating
        {
            get => _isNavigating;
            set
            {
                if (_isNavigating != value)
                {
                    _isNavigating = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public int TotalSteps
        {
            get => _totalSteps;
            set
            {
                if (_totalSteps != value)
                {
                    _totalSteps = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public double ProgressPercentage => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;

        // Commands
        public ICommand CreateImportTaskCommand => _createImportTaskCommand ??= new RelayCommand(_ => CreateStorageTask(), _ => true);
        public ICommand CreateExportTaskCommand => _createExportTaskCommand ??= new RelayCommand(_ => CreateRetrievalTask(), _ => true);
        public ICommand CreateManualTaskCommand => _createManualTaskCommand ??= new RelayCommand(_ => CreateManualTask(), _ => true);
        public ICommand StartTaskCommand => _startTaskCommand ??= new RelayCommand(param => StartTask(param), CanStartTaskFromGeneralList);
        public ICommand CompleteTaskCommand => _completeTaskCommand ??= new RelayCommand(_ => CompleteTask(), _ => CanCompleteTask());
        public ICommand NavigateToSourceCommand => _navigateToSourceCommand ??= new RelayCommand(_ => NavigateToSource(), _ => CanNavigateToSource());
        public ICommand NavigateToDestinationCommand => _navigateToDestinationCommand ??= new RelayCommand(_ => NavigateToDestination(), _ => CanNavigateToDestination());
        public ICommand CancelTaskCommand => _cancelTaskCommand ??= new RelayCommand(_ => CancelTask(), _ => CanCancelTask());
        public ICommand RefreshTasksCommand => _refreshTasksCommand ??= new RelayCommand(_ => RefreshTasks(), _ => !IsLoading);
        public ICommand NextNavigationCommand => _nextNavigationCommand ??= new RelayCommand(_ => ExecuteNextNavigation(), _ => true);
        public ICommand SelectStorageTabCommand => _selectStorageTabCommand ??= new RelayCommand(_ => IsStorageTabSelected = true);
        public ICommand SelectRetrievalTabCommand => _selectRetrievalTabCommand ??= new RelayCommand(_ => IsStorageTabSelected = false);
        public ICommand GoToFingerCommand => _goToFingerCommand ??= new RelayCommand(NavigateToFinger, CanNavigateToFinger); // Added command


        public TaskViewModel(
            IUnitOfWork unitOfWork, // Keep parameter for backward compatibility
            IConnectionManager connectionManager,
            IOpcService opcService,
            IDispatcherService dispatcherService, // Keep parameter
            ILogger<TaskViewModel> logger,
            MainViewModel mainViewModel) // Added parameter
        {
            // Get the UnitOfWorkFactory from the App's ServiceProvider
            _unitOfWorkFactory = (App.Current as App)?.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            if (_unitOfWorkFactory == null)
            {
                throw new InvalidOperationException("Could not resolve IUnitOfWorkFactory from ServiceProvider");
            }
            
            _connectionManager = connectionManager;
            _opcService = opcService;
            _dispatcherService = dispatcherService; // Assign injected dispatcher
            _logger = logger;
            _mainVM = mainViewModel; // Store reference to MainViewModel

            Tasks = new ObservableCollection<TaskDetails>();
            PendingTasks = new ObservableCollection<TaskDetails>();
            CompletedTasks = new ObservableCollection<TaskDetails>();
            StorageTasks = new ObservableCollection<TaskDetails>(); // Keep original task lists for now
            RetrievalTasks = new ObservableCollection<TaskDetails>();
            AvailableStorageFingers = new ObservableCollection<FingerStorageInfo>(); // Initialize new collection

            // Subscribe to PLC register changes for position feedback
            _opcService.RegisterChanged += OpcService_RegisterChanged;

            // Data loading will be triggered externally via InitializeAsync
            // LoadTasksAsync(); // REMOVED FROM CONSTRUCTOR
        }

        /// <summary>
        /// Asynchronously loads data required by the ViewModel.
        /// Should be called after the ViewModel is constructed.
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync() // Fully qualify Task
        {
            await LoadTasksAsync(); // This will trigger UpdateFilteredListsAsync, which now calls LoadAvailableStorageFingersAsync
            // Any other async init needed for this VM
        }

        public void RefreshTaskStatus(TaskDetails updatedTaskDetails)
        {
            if (updatedTaskDetails == null) return;

            var taskInCollection = Tasks.FirstOrDefault(t => t.Id == updatedTaskDetails.Id);
            if (taskInCollection != null)
            {
                // Update properties from the provided details
                // This ensures that the instance in the Tasks collection reflects the latest state
                taskInCollection.Status = updatedTaskDetails.Status;
                taskInCollection.ActiveTaskStatus = updatedTaskDetails.ActiveTaskStatus;
                taskInCollection.ExecutedDateTime = updatedTaskDetails.ExecutedDateTime;
                // Update any other relevant properties if needed

                _logger.LogInformation("TaskViewModel: Refreshed status for Task ID {TaskId} to {Status}, ActiveStatus {ActiveStatus}", 
                    updatedTaskDetails.Id, updatedTaskDetails.Status, updatedTaskDetails.ActiveTaskStatus);

                if (updatedTaskDetails.Status == Models.Enums.TaskStatus.Completed &&
                    !_recentlyCompletedTaskIds.Contains(updatedTaskDetails.Id) &&
                    (updatedTaskDetails.TaskType == Models.Enums.TaskType.Retrieval || updatedTaskDetails.TaskType == Models.Enums.TaskType.Storage)) // Apply delay for both types as per "like storage item"
                {
                    _logger.LogInformation("Task ID {TaskId} is completed. Adding to recently completed for 5s display.", updatedTaskDetails.Id);
                    _recentlyCompletedTaskIds.Add(updatedTaskDetails.Id);
                    _ = UpdateFilteredListsAsync(); // Show with green bar

                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(5000);
                        _dispatcherService.Invoke(() =>
                        {
                            if (_recentlyCompletedTaskIds.Contains(updatedTaskDetails.Id))
                            {
                                _recentlyCompletedTaskIds.Remove(updatedTaskDetails.Id);
                                _logger.LogInformation("5s elapsed for Task ID {TaskId}. Removing from recently completed and re-filtering.", updatedTaskDetails.Id);
                                _ = UpdateFilteredListsAsync(); // Re-filter to move it out of pending
                            }
                        });
                    });
                }
                else if (!_recentlyCompletedTaskIds.Contains(updatedTaskDetails.Id))
                {
                    // If not recently completed or already processed by timer, update lists normally
                    _ = UpdateFilteredListsAsync();
                }
                // If it IS in _recentlyCompletedTaskIds, UpdateFilteredListsAsync() was already called when it was added.
                // It will be called again when removed by the timer.
            }
            else
            {
                _logger.LogWarning("TaskViewModel: Attempted to refresh status for Task ID {TaskId}, but it was not found in the main Tasks collection.", updatedTaskDetails.Id);
                // Optionally, if the task is new and completed elsewhere, consider adding it directly to CompletedTasks
                // or reloading all tasks if this scenario implies a missing task.
                // For now, we assume it should have been in Tasks if it was being actively worked on.
            }
        }

        private void OpcService_RegisterChanged(object sender, Models.PlcModels.RegisterChangedEventArgs e)
        {
            // Handle PLC feedback during navigation
            if (e.RegisterName.Contains("Position_PV") && IsNavigating)
            {
                _dispatcherService.Invoke(() =>
                {
                    if (int.TryParse(e.NewValue.ToString(), out var position))
                    {
                        // Check if we've reached our target position
                        if (ActiveTask != null)
                        {
                            if (IsNavigatingToSource && ActiveTask.SourceFingerPosition.HasValue)
                            {
                                int targetPos = ActiveTask.SourceFingerPosition.Value % 100; // Get position without level
                                int targetLevel = ActiveTask.SourceFingerPosition.Value / 100; // Get level
                                int currentPos = position % 100;
                                int currentLevel = position / 100;

                                if (currentPos == targetPos && currentLevel == targetLevel)
                                {
                                    IsNavigating = false;
                                    StatusMessage = "Reached source position. Ready for next step.";
                                    CurrentStep++;
                                }
                            }
                            else if (IsNavigatingToDestination &&
                                    ((ActiveTask.IsImportTask && ActiveTask.DestinationCell != null) ||
                                    (ActiveTask.IsExportTask && ActiveTask.DestinationFingerPosition.HasValue)))
                            {
                                int targetPos;
                                int targetLevel;

                                if (ActiveTask.IsImportTask && ActiveTask.DestinationCell != null)
                                {
                                    targetPos = ActiveTask.DestinationCell.Position ?? 0;
                                    targetLevel = ActiveTask.DestinationCell.Level ?? 0;
                                }
                                else
                                {
                                    targetPos = ActiveTask.DestinationFingerPosition.Value % 100;
                                    targetLevel = ActiveTask.DestinationFingerPosition.Value / 100;
                                }

                                int currentPos = position % 100;
                                int currentLevel = position / 100;

                                if (currentPos == targetPos && currentLevel == targetLevel)
                                {
                                    IsNavigating = false;
                                    StatusMessage = "Reached destination position. Ready for next step.";
                                    CurrentStep++;
                                }
                            }
                        }
                    }
                });
            }
        }

        // Properties to track navigation state
        private bool IsNavigatingToSource => IsNavigating && ActiveTask?.NeedsSourceNavigation == true && CurrentStep == 1;
        private bool IsNavigatingToDestination => IsNavigating && ActiveTask?.NeedsDestinationNavigation == true &&
                                              ((ActiveTask.IsImportTask && CurrentStep == 3) ||
                                              (ActiveTask.IsExportTask && CurrentStep == 1));

        public async System.Threading.Tasks.Task LoadTasksAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading tasks...";

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Fetch tasks with related entities included using the new overload
                    var dbTasks = await unitOfWork.Tasks.FindAsync(
                        // Use Status field if available, otherwise fall back to IsExecuted
                        predicate: t => t.Status == null 
                            ? (t.IsExecuted == false || t.IsExecuted == null) 
                            : (t.Status == (int)Models.Enums.TaskStatus.Created || t.Status == (int)Models.Enums.TaskStatus.InProgress),
                        include: query => query
                            // Include new specific navigation properties
                            .Include(t => t.RetrievalSourceCell)
                            .Include(t => t.RetrievalDestinationFinger)
                            .Include(t => t.RetrievalDestinationCell) // For HND
                            .Include(t => t.StorageSourceFinger)
                            .Include(t => t.StorageDestinationCell),
                        orderBy: query => query.OrderByDescending(t => t.DownloadDate) // Example ordering
                    );

                    var tasks = new ObservableCollection<TaskDetails>();
                    foreach (var task in dbTasks)
                    {
                        // Fetch Pallet separately based on the new int? PalletId
                        Pallet pallet = null;
                        if (task.PalletId.HasValue)
                        {
                            var foundPallets = await unitOfWork.Pallets.FindAsync(p => p.Id == task.PalletId.Value);
                            pallet = foundPallets.FirstOrDefault();
                            if (pallet == null)
                            {
                                _logger.LogWarning("Could not find Pallet with Id {PalletId} for Task {TaskId}", task.PalletId.Value, task.Id);
                            }
                        }

                        // Create TaskDetails using the simpler FromDbModel, then assign pallet
                        // This relies on TaskDetails.FromDbModel(dbTask) to use the included navigation properties
                        var taskDetails = TaskDetails.FromDbModel(task);
                        if (taskDetails != null)
                        {
                            taskDetails.Pallet = pallet;
                        }
                        else
                        {
                            _logger.LogWarning("TaskDetails.FromDbModel(task) returned null for Task ID {TaskId}", task.Id);
                            continue; // Skip this task if basic details can't be formed
                        }

                        // Check if this is an ongoing task (InProgress status)
                        if (taskDetails.Status == Models.Enums.TaskStatus.InProgress)
                        {
                            // Set as active task on startup if it's in progress
                            _logger.LogInformation("Found ongoing task (ID: {TaskId}) on startup, setting as active.", taskDetails.Id);
                            ActiveTask = taskDetails;
                            IsTaskActive = true;
                        }

                        tasks.Add(taskDetails);
                    }

                    // Update the tasks collection
                    Tasks = tasks; // This triggers UpdateFilteredLists

                    // Add ongoing tasks to the MainViewModel's PalletsReadyForStorage collection
                    if (_mainVM != null)
                    {
                        await LoadOngoingTasksToMainViewModel();
                    }

                    StatusMessage = $"Loaded {Tasks.Count} tasks.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tasks");
                StatusMessage = "Error loading tasks.";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Loads ongoing tasks (with InProgress status) to the MainViewModel's collections
        /// </summary>
        private async System.Threading.Tasks.Task LoadOngoingTasksToMainViewModel()
        {
            try
            {
                // Find tasks with InProgress status and ActiveTaskStatus set to transit or storing
                var ongoingTasks = Tasks.Where(t =>
                    t.Status == Models.Enums.TaskStatus.InProgress &&
                    (t.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.transit ||
                     t.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.storing)).ToList();

                if (ongoingTasks.Any())
                {
                    _logger.LogInformation("Found {Count} ongoing tasks to process.", ongoingTasks.Count);

                    // Create a new UnitOfWork instance for this operation
                    using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                    {
                        foreach (var task in ongoingTasks)
                        {
                            // Get the pallet for this task
                            Pallet pallet = null;
                            if (task.Pallet != null) // This refers to TaskDetails.Pallet
                            {
                                pallet = task.Pallet;
                            }
                            // If task.Pallet is null, we need to fetch it based on task.Id (which is TaskDetails.Id)
                            // and its associated PalletId from the database if not already done.
                            // However, the ongoingTasks are derived from 'Tasks' collection which should have Pallet populated by LoadTasksAsync.
                            // If task.PalletId (from DB) was used in FromDbModel to populate TaskDetails.Pallet, this should be fine.

                            // Let's re-verify pallet fetching for ongoing tasks if task.Pallet is null
                            if (pallet == null && task.Id > 0) // task.Id here is TaskDetails.Id
                            {
                                // Find the original DB task to get its PalletId
                                var originalDbTask = await unitOfWork.Tasks.GetByIdAsync(task.Id);
                                if (originalDbTask != null && originalDbTask.PalletId.HasValue)
                                {
                                    var foundPallets = await unitOfWork.Pallets.FindAsync(p => p.Id == originalDbTask.PalletId.Value);
                                    pallet = foundPallets.FirstOrDefault();
                                    if (pallet == null)
                                    {
                                        _logger.LogWarning("Could not find Pallet with Id {PalletId} for ongoing Task {TaskId}", originalDbTask.PalletId.Value, task.Id);
                                    }
                                    else
                                    {
                                        // Optionally update the task.Pallet in the ongoingTasks collection
                                        task.Pallet = pallet;
                                    }
                                }
                            }


                            if (pallet != null)
                            {
                                if (task.TaskType == Models.Enums.TaskType.Storage)
                                {
                                    // Create a PalletStorageTaskItem and add it to the storage collection
                                    var storageItem = new PalletStorageTaskItem(pallet, task)
                                    {
                                        GoToStorageCommand = _mainVM.GoToStorageLocationCommand,
                                        ChangeDestinationCommand = _mainVM.ChangeDestinationCommand
                                    };

                                    _dispatcherService.Invoke(() =>
                                    {
                                        // Check if this task is already in the collection to avoid duplicates
                                        if (!_mainVM.PalletsReadyForStorage.Any(p => p.StorageTask?.Id == task.Id))
                                        {
                                            _mainVM.PalletsReadyForStorage.Add(storageItem);
                                            _mainVM.NotifyStorageItemsChanged();
                                        }
                                    });
                                }
                                else // TaskType.Retrieval
                                {
                                    // Create a PalletRetrievalTaskItem and add it to the retrieval collection
                                    // Ensure _mainVM and its properties are not null
                                    if (_mainVM == null)
                                    {
                                        _logger.LogError("MainViewModel is null in LoadOngoingTasksToMainViewModel. Cannot create PalletRetrievalTaskItem.");
                                        continue; // Skip this item
                                    }

                                    var selectCellCmd = new RelayCommand(async (item) =>
                                    {
                                        if (item is PalletRetrievalTaskItem taskItem)
                                        {
                                            await _mainVM.ShowSelectCellDialogForRetrievalItemAsync(taskItem);
                                        }
                                    });

                                    var retrievalItem = new PalletRetrievalTaskItem(
                                        pallet,
                                        task,
                                        _mainVM.AvailableFingers, // Pass the list of available fingers
                                        _mainVM.GoToRetrievalDestinationCommand, // Command to go to destination
                                        _mainVM.ChangeSourceCommand, // Command to change source (if applicable)
                                        _mainVM.UnloadAtDestinationCommand, // Command to unload
                                        selectCellCmd // Command to select cell
                                    );

                                    _dispatcherService.Invoke(() =>
                                    {
                                        // Check if this task is already in the collection to avoid duplicates
                                        if (!_mainVM.PalletsForRetrieval.Any(p => p.RetrievalTask?.Id == task.Id))
                                        {
                                            _mainVM.PalletsForRetrieval.Add(retrievalItem);
                                            _mainVM.NotifyRetrievalItemsChanged();
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No ongoing tasks found with appropriate status.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ongoing tasks to MainViewModel");
            }
        }

        internal async System.Threading.Tasks.Task UpdateFilteredListsAsync()
        {
            // Tasks that are genuinely pending (Created or InProgress) OR are recently completed (green bar phase)
            PendingTasks = new ObservableCollection<TaskDetails>(
                Tasks.Where(t =>
                        (_recentlyCompletedTaskIds.Contains(t.Id) && t.Status == Models.Enums.TaskStatus.Completed) || // Recently completed, show with green bar
                        (t.Status == Models.Enums.TaskStatus.Created || t.Status == Models.Enums.TaskStatus.InProgress) && !_recentlyCompletedTaskIds.Contains(t.Id) // Genuinely pending
                     )
                     .OrderByDescending(t => t.IsPriority)
                     .ThenBy(t => t.CreatedDateTime));

            // Tasks that are truly completed and not in the 5s green bar phase
            CompletedTasks = new ObservableCollection<TaskDetails>(
                Tasks.Where(t => (t.Status == Models.Enums.TaskStatus.Completed && !_recentlyCompletedTaskIds.Contains(t.Id)) ||
                              t.Status == Models.Enums.TaskStatus.Failed ||
                              t.Status == Models.Enums.TaskStatus.Cancelled)
                     .OrderByDescending(t => t.ExecutedDateTime));

            // Update the storage and retrieval task lists based on the (potentially augmented) PendingTasks
            StorageTasks = new ObservableCollection<TaskDetails>(
                PendingTasks.Where(t => t.IsImportTask) // IsImportTask is true for Storage TaskType
                     .OrderByDescending(t => t.IsPriority)
                     .ThenBy(t => t.CreatedDateTime));

            RetrievalTasks = new ObservableCollection<TaskDetails>(
                PendingTasks.Where(t => t.TaskType == Models.Enums.TaskType.Retrieval) // Explicitly check TaskType for retrieval
                     .OrderByDescending(t => t.IsPriority)
                     .ThenBy(t => t.CreatedDateTime));
            
            OnPropertyChanged(nameof(RetrievalTasksCount));
            OnPropertyChanged(nameof(AvailableStorageFingerCount)); 
            OnPropertyChanged(nameof(PendingStorageItemCount));

            // Add this call here:
            // This ensures that whenever the filtered task lists are updated,
            // the finger storage information (which depends on PendingTasks) is also refreshed.
            await LoadAvailableStorageFingersAsync(this.PendingTasks); 
        }

        private async void ExecuteNextNavigation()
        {
            if (ActiveTask == null)
            {
                MessageBox.Show("No active task.");
                return;
            }

            // For an Import task:
            //    Step 1: Navigate to the source finger.
            //    Step 2: Navigate to the destination cell.
            // For an Export task:
            //    Step 1: Navigate to the destination cell.
            //    Step 2: Navigate to the source finger.

            if (ActiveTask.IsImportTask)
            {
                if (CurrentStep == 1)
                {
                    // Navigate to source finger
                    await NavigateToFingerAsync();
                    CurrentStep++; // Move to the next step
                }
                else if (CurrentStep == 2)
                {
                    // Navigate to destination cell
                    await NavigateToDestinationCellAsync();
                    // You might then mark the task as completed or reset the step.
                    // For example: CompleteTask(); or CurrentStep = 0;
                }
            }
            else // Export task
            {
                if (CurrentStep == 1)
                {
                    // Navigate to destination cell first
                    await NavigateToDestinationCellAsync();
                    CurrentStep++;
                }
                else if (CurrentStep == 2)
                {
                    // Then navigate to source finger
                    await NavigateToFingerAsync();
                    // Again, complete or reset as needed.
                }
            }
        }

        private async System.Threading.Tasks.Task NavigateToFingerAsync()
        {
            // For demonstration, assume we determine the target value from ActiveTask.SourceFingerPosition
            int targetValue = ActiveTask.SourceFingerPosition ?? 0;
            await _opcService.WriteRegisterAsync(OpcNodes.PositionRequest, targetValue);
            await _opcService.WriteRegisterAsync(OpcNodes.Control, 1);
            // Optionally simulate a delay or wait for an OPC register update callback.
            await System.Threading.Tasks.Task.Delay(1000);
            MessageBox.Show($"Navigated to finger: {ActiveTask.SourceFinger?.DisplayName}");
        }

        private async System.Threading.Tasks.Task NavigateToDestinationCellAsync()
        {
            // For demonstration, determine the target value based on a cell's data
            int targetValue = 0;
            if (ActiveTask.IsImportTask && ActiveTask.DestinationCell != null)
            {
                targetValue = (ActiveTask.DestinationCell.Level ?? 0) * 100 + (ActiveTask.DestinationCell.Position ?? 0);
            }
            else if (ActiveTask.IsExportTask && ActiveTask.DestinationFinger != null)
            {
                targetValue = ActiveTask.DestinationFingerPosition ?? 0;
            }
            await _opcService.WriteRegisterAsync(OpcNodes.PositionRequest, (short)targetValue);
            await _opcService.WriteRegisterAsync(OpcNodes.Control, (short)1);
            await System.Threading.Tasks.Task.Delay(1000);
            MessageBox.Show("Navigated to destination cell.");
        }

        private void UpdateCommandAvailability()
        {
            (_startTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_completeTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_navigateToSourceCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_navigateToDestinationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_cancelTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_createImportTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_createExportTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_createManualTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Command execution logic
        private async void CreateStorageTask() // Make async
        {
            // Show the task creation dialog for import
            var dialog = new ImportTaskDialog();
            var viewModel = new ImportTaskViewModel(_unitOfWorkFactory.CreateUnitOfWork());
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true && viewModel.TaskDetails != null)
            {
                // Save first, then add to collection if successful
                bool saved = await SaveTaskToDatabase(viewModel.TaskDetails); // Make async
                if (saved)
                {
                    Tasks.Add(viewModel.TaskDetails); // Add after saving (ID might be updated)
                    await System.Threading.Tasks.Task.Delay(150); // Added delay
                    await UpdateFilteredListsAsync(); // Ensure PendingTasks is updated before finger counts are recalculated
                    StatusMessage = "Storage task created.";
                }
            }
        }

        private async void CreateRetrievalTask() // Make async
        {
            // Show the task creation dialog for export
            var dialog = new ExportTaskDialog();
            var viewModel = new ExportTaskViewModel(_unitOfWorkFactory.CreateUnitOfWork());
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true && viewModel.TaskDetails != null)
            {
                // Save first, then add to collection if successful
                bool saved = await SaveTaskToDatabase(viewModel.TaskDetails); // Make async
                if (saved)
                {
                    Tasks.Add(viewModel.TaskDetails); // Add after saving (ID might be updated)
                    await System.Threading.Tasks.Task.Delay(150); // Added delay
                    await UpdateFilteredListsAsync(); // Ensure PendingTasks is updated before finger counts are recalculated
                    StatusMessage = "Retrieval task created.";
                }
            }
        }

        private async void CreateManualTask() // Make async
        {
            // Show the manual task creation dialog
            var dialog = (App.Current as App)?.ServiceProvider.GetRequiredService<ManualTaskDialog>();

            if (dialog.ShowDialog() == true && dialog.TaskDetails != null)
            {
                // Save first, then add to collection if successful
                bool saved = await SaveTaskToDatabase(dialog.TaskDetails); // Make async
                if (saved)
                {
                    Tasks.Add(dialog.TaskDetails); // Add after saving (ID might be updated)
                    await System.Threading.Tasks.Task.Delay(150); // Added delay
                    await UpdateFilteredListsAsync(); // Ensure PendingTasks is updated before finger counts are recalculated
                    StatusMessage = dialog.TaskDetails.IsImportTask ? "Storage task created." : "Retrieval task created.";
                }
            }
        }

        // Return bool indicating success - Changed to internal
        internal async Task<bool> SaveTaskToDatabase(TaskDetails taskDetails)
        {
            try
            {
                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // For new import tasks, create the pallet first if it doesn't exist
                    if (taskDetails.IsImportTask && taskDetails.Pallet != null && taskDetails.Pallet.Id == 0)
                    {
                        _logger.LogInformation("Creating new pallet for import task: {PalletDisplayName}", taskDetails.Pallet.DisplayName);
                        var newPallet = new Pallet
                        {
                            DisplayName = taskDetails.Pallet.DisplayName,
                            Description = taskDetails.Pallet.Description,
                            UldCode = taskDetails.Pallet.UldCode, // Assuming UldCode is set from dialog
                            AwbCode = taskDetails.Pallet.AwbCode,
                            ReceivedDate = DateTime.Now, // Set current date or from dialog
                            LastModifiedDate = DateTime.Now,
                            CargoType = taskDetails.Pallet.CargoType,
                            HeightType = taskDetails.Pallet.HeightType,
                            StorageType = taskDetails.Pallet.StorageType,
                            ReportType = taskDetails.Pallet.ReportType,
                            RefrigerationType = taskDetails.Pallet.RefrigerationType,
                            IsCheckedOut = taskDetails.Pallet.IsCheckedOut,
                            CheckedOutDate = taskDetails.Pallet.CheckedOutDate,
                            LocationId = taskDetails.Pallet.LocationId,
                            IsSecure = taskDetails.Pallet.IsSecure,

                            UpdateType = taskDetails.Pallet.UpdateType,
       
                            CargoHeight = taskDetails.Pallet.CargoHeight,

                            ImportManifest = taskDetails.Pallet.ImportManifest,
                            ImportUnit = taskDetails.Pallet.ImportUnit,
                            ImportAppearance = taskDetails.Pallet.ImportAppearance,
                            ExportSwbPrefix = taskDetails.Pallet.ExportSwbPrefix,
                            ExportAwbNumber = taskDetails.Pallet.ExportAwbNumber,
                            ExportAwbAppearance = taskDetails.Pallet.ExportAwbAppearance,
                            ExportAwbStorage = taskDetails.Pallet.ExportAwbStorage,
                            ExportBarcode = taskDetails.Pallet.ExportBarcode,
                            UldType = taskDetails.Pallet.UldType,
                            UldNumber = taskDetails.Pallet.UldNumber,
                            UldAirline = taskDetails.Pallet.UldAirline
                        };
                        await unitOfWork.Pallets.AddAsync(newPallet);
                        await unitOfWork.CompleteAsync(); // Save pallet to get its ID
                        taskDetails.Pallet.Id = newPallet.Id; // Update TaskDetails with the new Pallet ID
                        _logger.LogInformation("New pallet created with ID: {PalletId}", newPallet.Id);
                    }

                    var dbTask = taskDetails.ToDbModel();

                    // Truncate the Name field if it exceeds 255 characters
                    if (dbTask.Name != null && dbTask.Name.Length > 255)
                    {
                        dbTask.Name = dbTask.Name.Substring(0, 255);
                        _logger.LogWarning("Task name truncated to 255 characters: {OriginalName}", taskDetails.Name);
                        
                        // Also update the display model to keep them in sync
                        taskDetails.Name = dbTask.Name;
                    }

                    if (dbTask.Id == 0)
                    {
                        await unitOfWork.Tasks.AddAsync(dbTask);
                    }
                    else
                    {
                        unitOfWork.Tasks.Update(dbTask);
                    }

                    await unitOfWork.CompleteAsync();

                    // Update the ID in the task details if it was a new task
                    if (taskDetails.Id == 0)
                    {
                        taskDetails.Id = dbTask.Id; // Update the display model with the new ID
                    }
                    return true; // Indicate success
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving task to database");
                MessageBox.Show("Error saving task to database: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false; // Indicate failure
            }
        }

        private async void StartTask(object parameter) // Make async
        {
            // If a task is passed as parameter, set it as the SelectedTask.
            if (parameter is TaskDetails task)
            {
                SelectedTask = task;
            }

            // Now, if SelectedTask is null or cannot be started, do nothing (or show a message).
            if (SelectedTask == null || !SelectedTask.CanStart)
            {
                MessageBox.Show("Cannot start this task.");
                return;
            }

            // Make the task active and update its status.
            ActiveTask = SelectedTask;
            ActiveTask.Status = Models.Enums.TaskStatus.InProgress; // Update local object status

            // Save the status change to the database
            bool saved = await SaveTaskToDatabase(ActiveTask);

            if (saved)
            {
                await UpdateFilteredListsAsync(); // Update lists to reflect InProgress status

                if (ActiveTask.TaskType == Models.Enums.TaskType.Retrieval)
                {
                    // For retrieval tasks, hand off to MainViewModel to navigate and then authenticate
                    _logger.LogInformation("Retrieval task {TaskId} started. Handing off to MainViewModel for navigation and authentication.", ActiveTask.Id);
                    await _mainVM.BeginRetrievalTaskAtSourceCellAsync(ActiveTask);
                    // InitializeTaskWorkflow will be called by MainViewModel after successful authentication for retrieval tasks
                }
                else // For Storage tasks or other types
                {
                    InitializeTaskWorkflow(); // Original workflow initialization
                }
            }
            else
            {
                // Revert status if save failed? Or handle error appropriately
                _logger.LogError("Failed to save status for starting task {TaskId}. Reverting.", SelectedTask.Id);
                ActiveTask.Status = Models.Enums.TaskStatus.Created; // Example revert
                // ActiveTask = null; // Do not nullify ActiveTask here, allow UI to reflect failed start
                _ = UpdateFilteredListsAsync(); // Re-filter if status reverted
                MessageBox.Show("Failed to save task start status. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Removed duplicate InitializeTaskWorkflow();
        }

        // Made public to be callable from MainViewModel after authentication
        public void InitializeTaskWorkflow()
        {
            if (ActiveTask == null)
                return;

            CurrentStep = 0;

            if (ActiveTask.IsImportTask)
            {
                // Import task workflow
                TotalSteps = ActiveTask.SourceFinger != null ? 5 : 3;
                StatusMessage = "Starting import task...";

                // Set initial step
                CurrentStep = 1;
                StatusMessage = ActiveTask.SourceFinger != null ?
                    "Step 1: Navigate to source finger." :
                    "Step 1: Ready to scan pallet.";
            }
            else
            {
                // Export task workflow
                TotalSteps = 3;
                StatusMessage = "Starting export task...";

                // Set initial step
                CurrentStep = 1;
                StatusMessage = "Step 1: Navigate to pallet location.";
            }

            // Initialize task steps
            InitializeTaskSteps();
        }

        private async void NavigateToSource()
        {
            int source = 0;
            if (ActiveTask.IsImportTask)
            {
                source = ActiveTask.SourceFingerPosition ?? 0;
            }
            else
            {
                source = ActiveTask.SourceCell.Position ?? 0;
                source += (ActiveTask.SourceCell.Level ?? 1) * 100;
            }

            try
            {
                IsNavigating = true;
                try
                {
                    if (ActiveTask.IsImportTask)
                        StatusMessage = $"Navigating to finger {ActiveTask.SourceFinger.DisplayName}...";
                    else
                        StatusMessage = $"Navigating to cell {ActiveTask.SourceCell.DisplayName}...";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error navigating to source");
                    StatusMessage = "Error navigating to source.";
                    MessageBox.Show("Error navigating to source: " + ex.Message, "Navigation Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Calculate the position register value (level * 100 + position)
                int positionValue = ActiveTask.SourceFingerPosition ?? 0;

                // Set the position request register
                await _opcService.WriteRegisterAsync(OpcNodes.PositionRequest, (short)source);

                // Set the control register to start movement
                await _opcService.WriteRegisterAsync(OpcNodes.Control, (short)1);
            }
            catch (Exception ex)
            {
                IsNavigating = false;
                _logger.LogError(ex, "Error navigating to source");
                StatusMessage = "Error navigating to source.";
                MessageBox.Show("Error navigating to source: " + ex.Message, "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NavigateToDestination()
        {
            try
            {
                IsNavigating = true;

                int positionValue;
                if (ActiveTask.IsImportTask && ActiveTask.DestinationCell != null)
                {
                    StatusMessage = $"Navigating to storage cell {ActiveTask.DestinationCell.DisplayName}...";
                    positionValue = (ActiveTask.DestinationCell.Level ?? 0) * 100 +
                                  (ActiveTask.DestinationCell.Position ?? 0);
                }
                else if (ActiveTask.IsExportTask && ActiveTask.DestinationFinger != null)
                {
                    StatusMessage = $"Navigating to finger {ActiveTask.DestinationFinger.DisplayName}...";
                    positionValue = ActiveTask.DestinationFingerPosition ?? 0;
                }
                else
                {
                    StatusMessage = "No valid destination found.";
                    IsNavigating = false;
                    return;
                }

                // Set the position request register
                await _opcService.WriteRegisterAsync(OpcNodes.PositionRequest, (short)positionValue);

                // Set the control register to start movement
                await _opcService.WriteRegisterAsync(OpcNodes.Control, (short)1);
            }
            catch (Exception ex)
            {
                IsNavigating = false;
                _logger.LogError(ex, "Error navigating to destination");
                StatusMessage = "Error navigating to destination.";
                MessageBox.Show("Error navigating to destination: " + ex.Message, "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompleteTask() // Make async
        {
            if (ActiveTask == null)
                return;

            try
            {
                ActiveTask.ActiveTaskStatus = Models.Enums.ActiveTaskStatus.finished; // Set active status first
                ActiveTask.Status = Models.Enums.TaskStatus.Completed; // Then the overall status
                ActiveTask.ExecutedDateTime = DateTime.Now; // And the execution time

                // Save the status change to the database
                bool saved = await SaveTaskToDatabase(ActiveTask);

                if (!saved)
                {
                    // Revert status or handle error
                    ActiveTask.Status = Models.Enums.TaskStatus.InProgress; // Example revert
                    return; // Stop processing if save failed
                }

                // --- Handle Task Completion ---
                if (_mainVM != null)
                {
                    if (ActiveTask.IsImportTask)
                    {
                        // Find the corresponding item in the MainViewModel's storage list
                        var storageItem = _mainVM.PalletsReadyForStorage
                                                 .FirstOrDefault(item => item.StorageTask?.Id == ActiveTask.Id);

                        if (storageItem != null)
                        {
                            // Use dispatcher to remove from the collection on the UI thread
                            _mainVM._dispatcherService.Invoke(() =>
                            {
                                _mainVM.PalletsReadyForStorage.Remove(storageItem);
                                _mainVM.NotifyStorageItemsChanged(); // Notify MainVM property change
                            });
                            _logger.LogInformation("Removed completed storage task item (ID: {TaskId}) from PalletsReadyForStorage.", ActiveTask.Id);

                            // Show success message
                            MessageBox.Show("Storage task completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            _logger.LogWarning("Could not find corresponding PalletStorageTaskItem for completed Task ID {TaskId} in MainViewModel.", ActiveTask.Id);
                        }
                    }
                    else // Retrieval task
                    {
                        // Find the corresponding item in the MainViewModel's retrieval list
                        var retrievalItem = _mainVM.PalletsForRetrieval
                                                  .FirstOrDefault(item => item.RetrievalTask?.Id == ActiveTask.Id);

                        if (retrievalItem != null)
                        {
                            // Use dispatcher to remove from the collection on the UI thread
                            _mainVM._dispatcherService.Invoke(() =>
                            {
                                _mainVM.PalletsForRetrieval.Remove(retrievalItem);
                                _mainVM.NotifyRetrievalItemsChanged(); // Notify MainVM property change
                            });
                            _logger.LogInformation("Removed completed retrieval task item (ID: {TaskId}) from PalletsForRetrieval.", ActiveTask.Id);

                            // Show success message
                            MessageBox.Show("Retrieval task completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            _logger.LogWarning("Could not find corresponding PalletRetrievalTaskItem for completed Task ID {TaskId} in MainViewModel.", ActiveTask.Id);
                        }
                    }
                }
                else
                {
                    // If _mainVM is null, just set the status message
                    StatusMessage = "Task completed successfully.";
                }
                // --- End Handle Task Completion ---


                // Reset active task
                var completedTask = ActiveTask; // Keep reference for logging/UI update if needed
                ActiveTask = null;
                CurrentStep = 0;
                TotalSteps = 0;

                // Refresh the task lists
                await UpdateFilteredListsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task");
                StatusMessage = "Error completing task.";
                MessageBox.Show("Error completing task: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelTask() // Make async
        {
            if (ActiveTask == null)
                return;

            if (MessageBox.Show("Are you sure you want to cancel this task?", "Cancel Task",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ActiveTask.Status = Models.Enums.TaskStatus.Cancelled; // Update local object status

                // Save the status change to the database
                bool saved = await SaveTaskToDatabase(ActiveTask);

                if (!saved)
                {
                     // Handle error if save failed
                     ActiveTask.Status = Models.Enums.TaskStatus.InProgress; // Example revert
                     return;
                }

                StatusMessage = "Task cancelled.";

                // Reset active task
                ActiveTask = null;
                CurrentStep = 0;
                TotalSteps = 0;

                // Refresh the task lists
                await UpdateFilteredListsAsync();
            }
        }

        private async void RefreshTasks() // Make async
        {
            await LoadTasksAsync(); // This will trigger UpdateFilteredListsAsync, which now calls LoadAvailableStorageFingersAsync
        }

        /// <summary>
        /// Loads the list of available storage fingers and their pallet counts.
        /// </summary>
        public async System.Threading.Tasks.Task LoadAvailableStorageFingersAsync(ObservableCollection<TaskDetails> currentPendingTasks) // Fully qualify Task
        {
            if (currentPendingTasks == null)
            {
                _logger.LogWarning("LoadAvailableStorageFingersAsync called with null currentPendingTasks.");
                // Optionally clear AvailableStorageFingers or return
                _dispatcherService.Invoke(() => {
                    if (AvailableStorageFingers != null) AvailableStorageFingers.Clear();
                });
                return;
            }
            try
            {
                _logger.LogInformation("Loading available storage fingers based on current pending tasks count: {PendingCount}", currentPendingTasks.Count);
                var storageFingers = new ObservableCollection<FingerStorageInfo>();

                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Get all fingers
                    var allFingers = await unitOfWork.Fingers.GetAllAsync(); // Assuming GetAllAsync exists

                    foreach (var finger in allFingers) // Load all fingers
                    {
                        int palletCount = 0;
                        try
                        {
                            // Count pending storage tasks where the SourceFingerPosition matches the finger's Position
                            palletCount = currentPendingTasks.Count(task => task.IsImportTask && task.SourceFingerPosition.HasValue && task.SourceFingerPosition.Value == finger.Position);
                            if (finger.DisplayName == "L05") // Example logging for L05
                            {
                                _logger.LogInformation("Finger L05: Position {Position}. Calculated PalletCount: {PalletCount} from {PendingCount} pending tasks.", finger.Position, palletCount, currentPendingTasks.Count);
                                foreach(var task in currentPendingTasks.Where(t => t.SourceFingerPosition.HasValue && t.SourceFingerPosition.Value == finger.Position))
                                {
                                    _logger.LogInformation("L05 Relevant Task: ID {TaskId}, IsImportTask: {IsImport}, SourceFingerPos: {SFP}", task.Id, task.IsImportTask, task.SourceFingerPosition);
                                }
                                foreach(var task in currentPendingTasks.Where(t => !(t.SourceFingerPosition.HasValue && t.SourceFingerPosition.Value == finger.Position) && t.IsImportTask))
                                {
                                     _logger.LogInformation("L05 Non-Matching Import Task: ID {TaskId}, IsImportTask: {IsImport}, SourceFingerPos: {SFP}", task.Id, task.IsImportTask, task.SourceFingerPosition);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error counting tasks for finger {FingerId}", finger.Id);
                            // Continue with count 0 if error occurs for one finger
                        }

                        storageFingers.Add(new FingerStorageInfo(finger, palletCount));
                    }
                }

                // Use dispatcher if updating from a non-UI thread, otherwise direct assignment is fine
                 _dispatcherService.Invoke(() => {
                    AvailableStorageFingers = storageFingers;
                 });

                _logger.LogInformation("Loaded {Count} storage fingers.", AvailableStorageFingers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available storage fingers");
                StatusMessage = "Error loading finger storage info.";
                // Optionally clear the list or show an error indicator
                 _dispatcherService.Invoke(() => {
                    if (AvailableStorageFingers != null) // Check if null before clearing
                    {
                       AvailableStorageFingers.Clear();
                    }
                 });
            }
        }


        // Command availability logic
        private bool CanStartTaskFromGeneralList(object parameter)
        {
            if (parameter is not TaskDetails taskToStart) return false;

            // Define startable/resumable states
            bool isGenerallyStartable = taskToStart.Status == Models.Enums.TaskStatus.Created ||
                                        taskToStart.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.pending ||
                                        taskToStart.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.New;

            bool isRetrievalResumable = taskToStart.TaskType == Models.Enums.TaskType.Retrieval &&
                                        (taskToStart.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.navigating_to_source ||
                                         taskToStart.ActiveTaskStatus == Models.Enums.ActiveTaskStatus.retrieval); // 'retrieval' means at cell for auth

            bool isStartableState = isGenerallyStartable || isRetrievalResumable;

            if (!isStartableState)
            {
                _logger.LogTrace("CanStartTaskFromGeneralList: Task {TaskId} is not in a startable/resumable state (Status: {Status}, ActiveStatus: {ActiveStatus})", 
                    taskToStart.Id, taskToStart.Status, taskToStart.ActiveTaskStatus);
                return false;
            }

            if (_mainVM != null)
            {
                // Prevent starting if pallet is already loaded and ready for delivery (meaning past source cell pickup)
                bool palletIsLoadedForDelivery = _mainVM.PalletsReadyForDelivery.Any(p => p.RetrievalTask?.Id == taskToStart.Id);
                if (palletIsLoadedForDelivery)
                {
                    _logger.LogTrace("CanStartTaskFromGeneralList: Task {TaskId} cannot be started, pallet is already in PalletsReadyForDelivery.", taskToStart.Id);
                    return false;
                }

                // If it's a retrieval task that is resumable (navigating_to_source or retrieval)
                // it's okay if it's in PalletsForRetrieval or is the ActiveCellAuthenticationItem, as re-pressing "Go" should re-initiate.
                // However, if it's a storage task, being in PalletsReadyForStorage means it's past the initial "start" from the general list.
                if (taskToStart.TaskType == Models.Enums.TaskType.Storage)
                {
                    bool isActiveStorageInMainVM = _mainVM.PalletsReadyForStorage.Any(p => p.StorageTask?.Id == taskToStart.Id);
                    if (isActiveStorageInMainVM)
                    {
                         _logger.LogTrace("CanStartTaskFromGeneralList: Storage Task {TaskId} is already in PalletsReadyForStorage.", taskToStart.Id);
                        return false;
                    }
                }
                
                // Consider if trolley is busy with another critical operation from MainViewModel
                // For now, we assume MainViewModel's BeginRetrievalTaskAtSourceCellAsync can handle re-entrant calls if trolley is busy or target is same.
                // A more robust check could be: if (_mainVM._activeNavigationTcs != null && _mainVM._activeNavigationTargetPosition != targetForThisTask) return false;
            }
            
            _logger.LogTrace("CanStartTaskFromGeneralList: Task {TaskId} can be started.", taskToStart.Id);
            return true;
        }
        private bool CanCompleteTask() => true;
        private bool CanNavigateToSource() => true;
        private bool CanNavigateToDestination() => true;
        private bool CanCancelTask() => ActiveTask != null && !IsNavigating;

        /// <summary>
        /// Determines if the NavigateToFinger command can execute.
        /// </summary>
        private bool CanNavigateToFinger(object parameter)
        {
            // Can execute if the parameter is a valid FingerStorageInfo object with a valid Finger
            return parameter is FingerStorageInfo info && info.Finger != null && info.Finger.Position.HasValue;
        }

        /// <summary>
        /// Executes the logic to navigate the trolley to the specified finger.
        /// </summary>
        private async void NavigateToFinger(object parameter)
        {
            if (parameter is FingerStorageInfo fingerInfo && fingerInfo.Finger != null && fingerInfo.Finger.Position.HasValue)
            {
                var targetFinger = fingerInfo.Finger;
                _logger.LogInformation($"NavigateToFinger command executed for Finger: {targetFinger.DisplayName} (ID: {targetFinger.Id}, Position: {targetFinger.Position})");

                try
                {
                    // Assuming Finger.Position holds the combined Level * 100 + Location
                    short targetPosition = (short)targetFinger.Position.Value;
                    short commandCode = 1; // Assuming 1 is the 'Go' command code - VERIFY THIS

                    string positionSpNodeId = OpcNodes.PositionRequest; // Use actual Node IDs
                    string commandNodeId = OpcNodes.Control;     // Use actual Node IDs

                    _logger.LogInformation($"Writing Position_SP ({positionSpNodeId}): {targetPosition}");
                    await _opcService.WriteRegisterAsync(positionSpNodeId, targetPosition);

                    _logger.LogInformation($"Writing Command ({commandNodeId}): {commandCode}");
                    await _opcService.WriteRegisterAsync(commandNodeId, commandCode);

                    _logger.LogInformation($"Navigation command sent for Finger {targetFinger.DisplayName}.");
                    StatusMessage = $"Navigation command sent to finger {targetFinger.DisplayName}.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing NavigateToFinger for Finger ID {targetFinger.Id}");
                    StatusMessage = $"Error navigating to finger: {ex.Message}";
                    MessageBox.Show(StatusMessage, "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _logger.LogWarning("NavigateToFinger command executed with invalid parameter.");
                StatusMessage = "Cannot navigate: Invalid finger information.";
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<TaskStepModel> _taskSteps;

        public ObservableCollection<TaskStepModel> TaskSteps
        {
            get => _taskSteps;
            set
            {
                if (_taskSteps != value)
                {
                    _taskSteps = value;
                    OnPropertyChanged();
                }
            }
        }

        // Simplified InitializeTaskSteps method
        private void InitializeTaskSteps()
        {
            TaskSteps = new ObservableCollection<TaskStepModel>();
            // The implementation details will be added in a future update
        }
    }
}
