using EM.Maman.Models.CustomModels;
using EM.Maman.Models.LocalDbModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using EM.Maman.Models.Enums; // Required for StorageTypeEnum
using System.Collections.ObjectModel; // Added for ObservableCollection
using System; // Added for Action
using System.Linq; // Added for FirstOrDefault

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Represents a pallet ready for retrieval task.
    /// </summary>
    public class PalletRetrievalTaskItem : INotifyPropertyChanged
    {
        private Pallet _palletDetails;
        private TaskDetails _retrievalTask;
        private ICommand _goToRetrievalCommand;
        private ICommand _changeSourceCommand;
        private ICommand _unloadCommand;
        private ICommand _selectCellCommand; 
        private object _dataContext;
        private bool _canExecuteChangeSourceCommand;

        private ObservableCollection<Finger> _availableFingers;
        private Finger _selectedFinger;

        public Pallet PalletDetails
        {
            get => _palletDetails;
            set
            {
                if (_palletDetails != value)
                {
                    _palletDetails = value;
                    OnPropertyChanged();
                    // Notify that dependent properties have also changed
                    OnPropertyChanged(nameof(PalletTypeDisplayName));
                    OnPropertyChanged(nameof(PalletTypeTagBrush));
                    OnPropertyChanged(nameof(DisplayDetail));
                    OnPropertyChanged(nameof(DisplayAppearance));
                    OnPropertyChanged(nameof(DisplayManifest));
                }
            }
        }

        /// <summary>
        /// The retrieval task associated with this pallet.
        /// </summary>
        public TaskDetails RetrievalTask
        {
            get => _retrievalTask;
            set { _retrievalTask = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to navigate the trolley to the retrieval source/destination.
        /// </summary>
        public ICommand GoToRetrievalCommand
        {
            get => _goToRetrievalCommand;
            set { _goToRetrievalCommand = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to allow changing the source location.
        /// </summary>
        public ICommand ChangeSourceCommand
        {
            get => _changeSourceCommand;
            set { _changeSourceCommand = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to initiate unloading of the pallet at its destination.
        /// </summary>
        public ICommand UnloadCommand
        {
            get => _unloadCommand;
            set { _unloadCommand = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the DataContext for binding in XAML.
        /// </summary>
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets whether the ChangeSourceCommand can be executed.
        /// </summary>
        public bool CanExecuteChangeSourceCommand
        {
            get => _canExecuteChangeSourceCommand;
            set
            {
                if (_canExecuteChangeSourceCommand != value)
                {
                    _canExecuteChangeSourceCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Finger> AvailableFingers
        {
            get => _availableFingers;
            set { _availableFingers = value; OnPropertyChanged(); }
        }

        public Finger SelectedFinger
        {
            get => _selectedFinger;
            set
            {
                if (_selectedFinger != value)
                {
                    _selectedFinger = value;
                    if (RetrievalTask != null)
                    {
                        if (_selectedFinger != null)
                        {
                            RetrievalTask.DestinationFinger = _selectedFinger;
                            RetrievalTask.DestinationCell = null; // Clear cell if finger is selected
                        }
                        // If _selectedFinger is null, it implies no finger is actively selected in the ComboBox.
                        // We don't automatically null out RetrievalTask.DestinationFinger here,
                        // as it might have been set by other means (e.g. loaded from DB).
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DestinationDisplay)); // Update display based on new selection
                }
            }
        }

        public ICommand SelectCellCommand
        {
            get => _selectCellCommand;
            set { _selectCellCommand = value; OnPropertyChanged(); }
        }

        public PalletRetrievalTaskItem(
            Pallet pallet,
            TaskDetails retrievalTask,
            ObservableCollection<Finger> availableFingers,
            ICommand goToRetrievalCommand,
            ICommand changeSourceCommand,
            ICommand unloadCommand,
            ICommand selectCellCommand
            )
        {
            PalletDetails = pallet;
            RetrievalTask = retrievalTask;
            AvailableFingers = availableFingers;
            GoToRetrievalCommand = goToRetrievalCommand;
            ChangeSourceCommand = changeSourceCommand;
            UnloadCommand = unloadCommand;
            SelectCellCommand = selectCellCommand; 

            DataContext = this;
            CanExecuteChangeSourceCommand = false; 

            // Initialize SelectedFinger if a finger is already set as the destination in RetrievalTask
            if (RetrievalTask?.DestinationFinger != null && AvailableFingers != null)
            {
                // Use a local variable to avoid triggering the setter's logic during construction
                var initialSelectedFinger = AvailableFingers.FirstOrDefault(f => f.Id == RetrievalTask.DestinationFinger.Id);
                if (initialSelectedFinger != null)
                {
                    _selectedFinger = initialSelectedFinger;
                }
            }
        }

        public string PalletTypeDisplayName
        {
            get
            {
                if (RetrievalTask == null || PalletDetails == null) return string.Empty;
                return RetrievalTask.IsExportTask ? "מכס" : "קירוק";
            }
        }

        public Brush PalletTypeTagBrush
        {
            get
            {
                if (RetrievalTask == null || PalletDetails == null) return Brushes.Transparent;
                return RetrievalTask.IsExportTask ? new SolidColorBrush(Color.FromRgb(255, 243, 224)) : 
                       new SolidColorBrush(Color.FromRgb(227, 242, 253)); 
            }
        }

        public Brush PalletTypeTagForegroundBrush
        {
            get
            {
                if (RetrievalTask == null || PalletDetails == null) return Brushes.Black;
                return RetrievalTask.IsExportTask ? new SolidColorBrush(Color.FromRgb(255, 152, 0)) :   
                       new SolidColorBrush(Color.FromRgb(25, 118, 210)); 
            }
        }

        public string DisplayDetail // "פרט"
        {
            get
            {
                if (RetrievalTask == null || RetrievalTask.Pallet == null) return string.Empty;
                return !string.IsNullOrWhiteSpace(RetrievalTask.Pallet.UldCode) ? RetrievalTask.Pallet.UldCode : RetrievalTask.Pallet.Id.ToString();
            }
        }

        public string DisplayAppearance // "מופע"
        {
            get
            {
                if (RetrievalTask == null || RetrievalTask.Pallet == null) return string.Empty;
                return RetrievalTask.DisplayDetail2;
            }
        }

        public string DisplayManifest // "מצהר"
        {
            get
            {
                if (RetrievalTask == null || RetrievalTask.Pallet == null) return string.Empty;
                if (RetrievalTask.IsExportTask)
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(RetrievalTask.Pallet.ExportSwbPrefix)) parts.Add(RetrievalTask.Pallet.ExportSwbPrefix);
                    if (!string.IsNullOrWhiteSpace(RetrievalTask.Pallet.ExportAwbNumber)) parts.Add(RetrievalTask.Pallet.ExportAwbNumber);
                    return string.Join(" ", parts);
                }
                return RetrievalTask.Pallet.ImportManifest ?? string.Empty;
            }
        }

        public string DestinationDisplay
        {
            get
            {
                if (RetrievalTask?.DestinationFinger != null)
                {
                    return $"פינגר {RetrievalTask.DestinationFinger.DisplayName}";
                }
                if (RetrievalTask?.DestinationCell != null)
                {
                    return $"תא {RetrievalTask.DestinationCell.Level}-{RetrievalTask.DestinationCell.Position}";
                }
                return "לא ידוע";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
