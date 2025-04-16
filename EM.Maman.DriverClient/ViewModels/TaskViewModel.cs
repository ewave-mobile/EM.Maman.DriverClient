using EM.Maman.Models.CustomModels;
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
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace EM.Maman.DriverClient.ViewModels
{
    public class TaskViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConnectionManager _connectionManager;
        private readonly IOpcService _opcService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<TaskViewModel> _logger;

        private ObservableCollection<TaskDetails> _tasks;
        private ObservableCollection<TaskDetails> _pendingTasks;
        private ObservableCollection<TaskDetails> _completedTasks;
        private ObservableCollection<TaskDetails> _storageTasks;
        private ObservableCollection<TaskDetails> _retrievalTasks;
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

        public ObservableCollection<TaskDetails> Tasks
        {
            get => _tasks;
            set
            {
                if (_tasks != value)
                {
                    _tasks = value;
                    OnPropertyChanged();
                    UpdateFilteredLists();
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
                    OnPropertyChanged(nameof(StorageTasksCount));
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

        public int StorageTasksCount => StorageTasks?.Count ?? 0;
        public int RetrievalTasksCount => RetrievalTasks?.Count ?? 0;

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
        public ICommand StartTaskCommand => _startTaskCommand ??= new RelayCommand(param => StartTask(param), param => true);
        public ICommand CompleteTaskCommand => _completeTaskCommand ??= new RelayCommand(_ => CompleteTask(), _ => CanCompleteTask());
        public ICommand NavigateToSourceCommand => _navigateToSourceCommand ??= new RelayCommand(_ => NavigateToSource(), _ => CanNavigateToSource());
        public ICommand NavigateToDestinationCommand => _navigateToDestinationCommand ??= new RelayCommand(_ => NavigateToDestination(), _ => CanNavigateToDestination());
        public ICommand CancelTaskCommand => _cancelTaskCommand ??= new RelayCommand(_ => CancelTask(), _ => CanCancelTask());
        public ICommand RefreshTasksCommand => _refreshTasksCommand ??= new RelayCommand(_ => RefreshTasks(), _ => !IsLoading);
        public ICommand NextNavigationCommand => _nextNavigationCommand ??= new RelayCommand(_ => ExecuteNextNavigation(), _ => true);
        public ICommand SelectStorageTabCommand => _selectStorageTabCommand ??= new RelayCommand(_ => IsStorageTabSelected = true);
        public ICommand SelectRetrievalTabCommand => _selectRetrievalTabCommand ??= new RelayCommand(_ => IsStorageTabSelected = false);

        public TaskViewModel(
            IUnitOfWork unitOfWork,
            IConnectionManager connectionManager,
            IOpcService opcService,
            IDispatcherService dispatcherService,
            ILogger<TaskViewModel> logger)
        {
            _unitOfWork = unitOfWork;
            _connectionManager = connectionManager;
            _opcService = opcService;
            _dispatcherService = dispatcherService;
            _logger = logger;

            Tasks = new ObservableCollection<TaskDetails>();
            PendingTasks = new ObservableCollection<TaskDetails>();
            CompletedTasks = new ObservableCollection<TaskDetails>();
            StorageTasks = new ObservableCollection<TaskDetails>();
            RetrievalTasks = new ObservableCollection<TaskDetails>();

            // Subscribe to PLC register changes for position feedback
            _opcService.RegisterChanged += OpcService_RegisterChanged;

            // Initialize data
            LoadTasksAsync();
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
                                    targetLevel = ActiveTask.DestinationCell.HeightLevel ?? 0;
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

                var dbTasks = await _unitOfWork.Tasks.GetAllAsync();
                var tasks = new ObservableCollection<TaskDetails>();

                foreach (var task in dbTasks)
                {
                    // Load related entities for the task
                    Pallet pallet = null;
                    if (!string.IsNullOrEmpty(task.PalletId) && int.TryParse(task.PalletId, out int palletId))
                    {
                        pallet = await _unitOfWork.Pallets.GetByIdAsync(palletId);
                    }

                    Finger sourceFinger = null;
                    if (task.FingerLocationId.HasValue)
                    {
                        sourceFinger = await _unitOfWork.Fingers.GetByIdAsync(task.FingerLocationId.Value);
                    }

                    Cell destinationCell = null;
                    if (task.CellEndLocationId.HasValue)
                    {
                        destinationCell = await _unitOfWork.Cells.GetByIdAsync(task.CellEndLocationId.Value);
                    }

                    // Create the task details
                    var taskDetails = TaskDetails.FromDbModel(task, pallet, sourceFinger, null, destinationCell);
                    tasks.Add(taskDetails);
                }

                // Update the tasks collection
                Tasks = tasks;
                StatusMessage = $"Loaded {tasks.Count} tasks.";
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

        private void UpdateFilteredLists()
        {
            PendingTasks = new ObservableCollection<TaskDetails>(
                Tasks.Where(t => t.Status == Models.Enums.TaskStatus.Created || t.Status == Models.Enums.TaskStatus.InProgress)
                     .OrderByDescending(t => t.IsPriority)
                     .ThenBy(t => t.CreatedDateTime));

            CompletedTasks = new ObservableCollection<TaskDetails>(
                Tasks.Where(t => t.Status == Models.Enums.TaskStatus.Completed ||
                              t.Status == Models.Enums.TaskStatus.Failed ||
                              t.Status == Models.Enums.TaskStatus.Cancelled)
                     .OrderByDescending(t => t.ExecutedDateTime));

            // Update the storage and retrieval task lists
            StorageTasks = new ObservableCollection<TaskDetails>(
                PendingTasks.Where(t => t.IsImportTask)
                     .OrderByDescending(t => t.IsPriority)
                     .ThenBy(t => t.CreatedDateTime));

            RetrievalTasks = new ObservableCollection<TaskDetails>(
                PendingTasks.Where(t => !t.IsImportTask)
                     .OrderByDescending(t => t.IsPriority)
                     .ThenBy(t => t.CreatedDateTime));
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
            await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.PositionRequest", targetValue);
            await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.control", 1);
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
                targetValue = (ActiveTask.DestinationCell.HeightLevel ?? 0) * 100 + (ActiveTask.DestinationCell.Position ?? 0);
            }
            else if (ActiveTask.IsExportTask && ActiveTask.DestinationFinger != null)
            {
                targetValue = ActiveTask.DestinationFingerPosition ?? 0;
            }
            await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.PositionRequest", (short)targetValue);
            await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.control", (short)1);
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
        private void CreateStorageTask()
        {
            // Show the task creation dialog for import
            var dialog = new ImportTaskDialog();
            var viewModel = new ImportTaskViewModel(_unitOfWork);
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true && viewModel.TaskDetails != null)
            {
                // Add the new task to the collection
                Tasks.Add(viewModel.TaskDetails);
                SaveTaskToDatabase(viewModel.TaskDetails);
                UpdateFilteredLists();
                StatusMessage = "Storage task created.";
            }
        }

        private void CreateRetrievalTask()
        {
            // Show the task creation dialog for export
            var dialog = new ExportTaskDialog();
            var viewModel = new ExportTaskViewModel(_unitOfWork);
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true && viewModel.TaskDetails != null)
            {
                // Add the new task to the collection
                Tasks.Add(viewModel.TaskDetails);
                SaveTaskToDatabase(viewModel.TaskDetails);
                UpdateFilteredLists();
                StatusMessage = "Retrieval task created.";
            }
        }

        private void CreateManualTask()
        {
            // Show the manual task creation dialog
            var dialog = new ManualTaskDialog();

            if (dialog.ShowDialog() == true && dialog.TaskDetails != null)
            {
                // Add the new task to the collection
                Tasks.Add(dialog.TaskDetails);
                SaveTaskToDatabase(dialog.TaskDetails);
                UpdateFilteredLists();
                StatusMessage = dialog.TaskDetails.IsImportTask ? "Storage task created." : "Retrieval task created.";
            }
        }

        private async void SaveTaskToDatabase(TaskDetails taskDetails)
        {
            try
            {
                var dbTask = taskDetails.ToDbModel();

                if (dbTask.Id == 0)
                {
                    await _unitOfWork.Tasks.AddAsync(dbTask);
                }
                else
                {
                    _unitOfWork.Tasks.Update(dbTask);
                }

                await _unitOfWork.CompleteAsync();

                // Update the ID in the task details if it was a new task
                if (taskDetails.Id == 0)
                {
                    taskDetails.Id = dbTask.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving task to database");
                MessageBox.Show("Error saving task to database: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTask(object parameter)
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
            ActiveTask.Status = Models.Enums.TaskStatus.InProgress;
            SaveTaskToDatabase(ActiveTask);

            // Initialize the task workflow (set up steps, etc.)
            InitializeTaskWorkflow();
        }

        private void InitializeTaskWorkflow()
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
                source += (ActiveTask.SourceCell.HeightLevel ?? 1) * 100;
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
                await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.PositionRequest", (short)source);

                // Set the control register to start movement
                await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.control", (short)1);
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
                    positionValue = (ActiveTask.DestinationCell.HeightLevel ?? 0) * 100 +
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
                await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.PositionRequest", (short)positionValue);

                // Set the control register to start movement
                await _opcService.WriteRegisterAsync("ns=2;s=s7.s7 300.maman.control", (short)1);
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

        private void CompleteTask()
        {
            if (ActiveTask == null)
                return;

            try
            {
                ActiveTask.Status = Models.Enums.TaskStatus.Completed;
                ActiveTask.ExecutedDateTime = DateTime.Now;
                SaveTaskToDatabase(ActiveTask);

                StatusMessage = "Task completed successfully.";

                // Reset active task
                ActiveTask = null;
                CurrentStep = 0;
                TotalSteps = 0;

                // Refresh the task lists
                UpdateFilteredLists();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task");
                StatusMessage = "Error completing task.";
                MessageBox.Show("Error completing task: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelTask()
        {
            if (ActiveTask == null)
                return;

            if (MessageBox.Show("Are you sure you want to cancel this task?", "Cancel Task",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ActiveTask.Status = Models.Enums.TaskStatus.Cancelled;
                SaveTaskToDatabase(ActiveTask);
                StatusMessage = "Task cancelled.";

                // Reset active task
                ActiveTask = null;
                CurrentStep = 0;
                TotalSteps = 0;

                // Refresh the task lists
                UpdateFilteredLists();
            }
        }

        private void RefreshTasks()
        {
            LoadTasksAsync();
        }

        // Command availability logic
        private bool CanStartTask() => true;
        private bool CanCompleteTask() => true;
        private bool CanNavigateToSource() => true;
        private bool CanNavigateToDestination() => true;
        private bool CanCancelTask() => ActiveTask != null && !IsNavigating;

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
