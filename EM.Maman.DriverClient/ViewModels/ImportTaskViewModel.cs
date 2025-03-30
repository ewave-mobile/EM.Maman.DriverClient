using EM.Maman.DAL.Test;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
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
        private readonly IUnitOfWork _unitOfWork;
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
                        TaskDetails.SourceFinger = value;
                    }
                    OnPropertyChanged();
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
            Fingers = new ObservableCollection<Finger>();

            // Initialize a new task details object
            TaskDetails = new TaskDetails
            {
                TaskType = Models.Enums.TaskType.Import,
                Status = Models.Enums.TaskStatus.Created,
                CreatedDateTime = DateTime.Now,
                Code = GenerateTaskCode()
            };

            // Load fingers
            LoadFingersAsync();
        }

        private string GenerateTaskCode()
        {
            // Generate a unique code for the task - starting with IMP for import
            return $"IMP-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private async System.Threading.Tasks.Task LoadFingersAsync()
        {
            await System.Threading.Tasks.Task.Delay(100);
            Fingers = new ObservableCollection<Finger>(TestDatabase.Fingers.OrderBy(f => f.Position));
        }

        private IEnumerable<Finger> GetTestFingers()
        {
            return new List<Finger>
    {
        // Left side fingers (Side = 0)
        new Finger { Id = 1, Side = 0, Position = 102, Description = "Finger L1-02", DisplayName = "L02", DisplayColor = "Grey" },
        new Finger { Id = 3, Side = 0, Position = 105, Description = "Finger L1-05", DisplayName = "L05", DisplayColor = "Grey" },
        new Finger { Id = 5, Side = 0, Position = 112, Description = "Finger L1-12", DisplayName = "L12", DisplayColor = "Grey" },
        new Finger { Id = 7, Side = 0, Position = 118, Description = "Finger L1-18", DisplayName = "L18", DisplayColor = "Grey" },
        
        // Right side fingers (Side = 1)
        new Finger { Id = 2, Side = 1, Position = 103, Description = "Finger R1-03", DisplayName = "R03", DisplayColor = "Grey" },
        new Finger { Id = 4, Side = 1, Position = 108, Description = "Finger R1-08", DisplayName = "R08", DisplayColor = "Grey" },
        new Finger { Id = 6, Side = 1, Position = 115, Description = "Finger R1-15", DisplayName = "R15", DisplayColor = "Grey" },
        new Finger { Id = 8, Side = 1, Position = 120, Description = "Finger R1-20", DisplayName = "R20", DisplayColor = "Grey" }
    };
        }


        private void Save()
        {
            // Validate the task details and save
            if (TaskDetails == null || string.IsNullOrWhiteSpace(TaskDetails.Code))
            {
                StatusMessage = "Task code is required.";
                return;
            }

            // If no pallet is selected, create a new one from the input.
            if (TaskDetails.Pallet == null)
            {
                // Create new pallet from UI input
                TaskDetails.Pallet = CreatePalletFromInput();

                // Add it to the test database (simulate a DB insert)
                TestDatabase.AddPallet(TaskDetails.Pallet);

                // Determine the cell to update.
                // For example, you might choose the cell next to the source finger.
                // Here we assume that if a source finger was selected,
                // we find a cell on the same level (e.g. Level 1) with a matching position.
                if (TaskDetails.SourceFinger != null)
                {
                    // Calculate a cell position; adjust the logic as needed.
                    int targetPosition = (TaskDetails.SourceFinger.Position ?? 0) % 100;
                    // For demo purposes, assume Level 1 (HeightLevel == 1). You could also choose based on other criteria.
                    var candidateCell = TestDatabase.Cells.FirstOrDefault(c => c.Position == targetPosition && c.HeightLevel == 1);
                    if (candidateCell != null)
                    {
                        // Add the new pallet to the cell.
                        TestDatabase.AddPalletToCell((int)candidateCell.Id, TaskDetails.Pallet);
                    }
                }
            }

            // Set the task name using the pallet info and the source finger (if any)
            TaskDetails.Name = $"Import {TaskDetails.Pallet.DisplayName} from {TaskDetails.SourceFinger?.DisplayName ?? "manual entry"}";

            // Optionally, after updating the TestDatabase, you may raise an event or call a refresh method on your TrolleyViewModel
            // so that the UI re-reads the cell–pallet associations. For example:
            // TrolleyViewModelInstance.RefreshTestData();

            // Close the dialog with success result
            RequestClose?.Invoke(this, new DialogResultEventArgs(true));
        }


        private Pallet CreatePalletFromInput()
        {
            // Create a new pallet using the data from UI fields
            // These fields would be bound to properties in this view model
            return new Pallet
            {
                DisplayName = PalletDisplayName,
                UldType = UldType,
                UldCode = UldCode,
                UldNumber = UldNumber,
                UldAirline = UldAirline,
                Description = PalletDescription,
                ReceivedDate = DateTime.Now
            };
        }

        private void Cancel()
        {
            // Close the dialog with cancel result
            RequestClose?.Invoke(this, new DialogResultEventArgs(false));
        }

        private bool CanSave()
        {
            return !IsBusy && TaskDetails != null &&
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

    public class DialogResultEventArgs : EventArgs
    {
        public bool Result { get; }

        public DialogResultEventArgs(bool result)
        {
            Result = result;
        }
    }
}

