using EM.Maman.DAL.Test;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public class ImportTaskViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWork _unitOfWork; // Keep for backward compatibility
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private TaskDetails _taskDetails;
        private ObservableCollection<Finger> _fingers;
        private Finger _selectedSourceFinger;
        private bool _isBusy;
        private string _statusMessage;
        private RelayCommand _saveCommand;
        private RelayCommand _cancelCommand;

        public TaskDetails TaskDetails
        {
            get => _taskDetails;
            set
            {
                if (_taskDetails != value)
                {
                    _taskDetails = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Finger> Fingers
        {
            get => _fingers;
            set
            {
                if (_fingers != value)
                {
                    _fingers = value;
                    OnPropertyChanged();
                }
            }
        }

        public Finger SelectedSourceFinger
        {
            get => _selectedSourceFinger;
            set
            {
                if (_selectedSourceFinger != value)
                {
                    _selectedSourceFinger = value;
                    if (TaskDetails != null)
                    {
                        TaskDetails.SourceFinger = value; // Assign selected finger
                    }
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Re-evaluate CanSave
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        public ICommand SaveCommand => _saveCommand ??= new RelayCommand(_ => Save(), _ => CanSave());
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => Cancel(), _ => true);

        // Event for dialog result
        public event EventHandler<DialogResultEventArgs> RequestClose;

        public ImportTaskViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            
            // Get the UnitOfWorkFactory from the App's ServiceProvider
            _unitOfWorkFactory = (App.Current as App)?.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            if (_unitOfWorkFactory == null)
            {
                throw new InvalidOperationException("Could not resolve IUnitOfWorkFactory from ServiceProvider");
            }
            Fingers = new ObservableCollection<Finger>();

            // Initialize a new task details object
            TaskDetails = new TaskDetails
            {
                TaskType = Models.Enums.TaskType.Storage,
                Status = Models.Enums.TaskStatus.Created,
                CreatedDateTime = DateTime.Now,
                Code = GenerateTaskCode()
                // Pallet and SourceFinger will be set via binding/selection
            };

            // Load fingers
            //LoadFingersAsync();
        }
        public async System.Threading.Tasks.Task InitializeAsync() {
            await LoadFingersAsync();
        }
        private string GenerateTaskCode()
        {
            // Generate a unique code for the task - starting with IMP for import
            return $"IMP-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private async System.Threading.Tasks.Task LoadFingersAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading fingers...";
                
                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var fingersFromDb = await unitOfWork.Fingers.GetAllAsync(); // Fetch from DB
                    Fingers = new ObservableCollection<Finger>(fingersFromDb.OrderBy(f => f.Position));
                    StatusMessage = $"Loaded {Fingers.Count} fingers.";
                }
            }
            catch (Exception ex)
            {
                // Log the error appropriately
                StatusMessage = "Error loading fingers.";
                // Consider showing a message box or logging
                System.Windows.MessageBox.Show($"Error loading fingers: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Save()
        {
            // Validate essential details before closing
            if (TaskDetails == null || string.IsNullOrWhiteSpace(TaskDetails.Code))
            {
                StatusMessage = "Task code is required.";
                return;
            }
             if (string.IsNullOrWhiteSpace(PalletDisplayName) || string.IsNullOrWhiteSpace(UldType))
             {
                 StatusMessage = "Pallet Display Name and ULD Type are required.";
                 return;
             }
             if (SelectedSourceFinger == null) // Check the ViewModel's selected finger
             {
                 StatusMessage = "Source Finger must be selected.";
                 return;
             }

            // Create/Update Pallet object within TaskDetails
            // IMPORTANT: This pallet won't have a DB ID until saved by TaskViewModel.
            // The relationship needs to be handled correctly during the actual save.
             TaskDetails.Pallet = CreatePalletFromInput(); // Assign the newly created/edited pallet info

            // Assign the selected finger (already done via binding, but ensure it's set)
            TaskDetails.SourceFinger = SelectedSourceFinger;

            // Set a descriptive name
            TaskDetails.Name = $"Import {TaskDetails.Pallet?.DisplayName ?? "N/A"} from {TaskDetails.SourceFinger?.DisplayName ?? "N/A"}";

            // Close the dialog, indicating success. TaskViewModel will handle the actual DB save.
            RequestClose?.Invoke(this, new DialogResultEventArgs(true));
        }


        private Pallet CreatePalletFromInput()
        {
            // Create a new pallet using the data from UI fields
            // These fields would be bound to properties in this view model
            // IMPORTANT: This Pallet object does NOT have an ID yet.
            // The actual Pallet creation/linking needs to happen in TaskViewModel.SaveTaskToDatabase
            return new Pallet
            {
                Id = 0, // Explicitly set ID to 0 for a new pallet
                DisplayName = PalletDisplayName,
                UldType = UldType,
                UldCode = UldCode,
                UldNumber = UldNumber,
                UldAirline = UldAirline,
                Description = PalletDescription,
                ReceivedDate = DateTime.Now,
                IsSecure = IsSecurePallet // Assign IsSecure
            };
        }

        private void Cancel()
        {
            // Close the dialog with cancel result
            RequestClose?.Invoke(this, new DialogResultEventArgs(false));
        }

        private bool CanSave()
        {
            // Ensure a source finger is selected
            return !IsBusy && TaskDetails != null &&
                  SelectedSourceFinger != null && // Check ViewModel's property
                  !string.IsNullOrWhiteSpace(PalletDisplayName) &&
                  !string.IsNullOrWhiteSpace(UldType);
        }

        #region Pallet Properties
        // These properties would be bound to input fields in the dialog
        private string _palletDisplayName;
        private string _uldType;
        private string _uldCode;
        private string _uldNumber;
        private string _uldAirline;
        private string _palletDescription;
        private bool _isSecurePallet;

        public string PalletDisplayName
        {
            get => _palletDisplayName;
            set
            {
                if (_palletDisplayName != value)
                {
                    _palletDisplayName = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string UldType
        {
            get => _uldType;
            set
            {
                if (_uldType != value)
                {
                    _uldType = value;
                    OnPropertyChanged();
                    (_saveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string UldCode
        {
            get => _uldCode;
            set
            {
                if (_uldCode != value)
                {
                    _uldCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UldNumber
        {
            get => _uldNumber;
            set
            {
                if (_uldNumber != value)
                {
                    _uldNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UldAirline
        {
            get => _uldAirline;
            set
            {
                if (_uldAirline != value)
                {
                    _uldAirline = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PalletDescription
        {
            get => _palletDescription;
            set
            {
                if (_palletDescription != value)
                {
                    _palletDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSecurePallet
        {
            get => _isSecurePallet;
            set
            {
                if (_isSecurePallet != value)
                {
                    _isSecurePallet = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Restore DialogResultEventArgs as it's used by RequestClose event
    public class DialogResultEventArgs : EventArgs
    {
        public bool Result { get; }

        public DialogResultEventArgs(bool result)
        {
            Result = result;
        }
    }
}
