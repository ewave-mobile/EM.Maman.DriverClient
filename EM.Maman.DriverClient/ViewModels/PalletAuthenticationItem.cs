using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.CustomModels; // Added for TaskDetails
using EM.Maman.Models.Enums; // Added for UpdateType
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Represents a pallet waiting for authentication at a finger.
    /// </summary>
    public class PalletAuthenticationItem : INotifyPropertyChanged
    {
        private Pallet _palletDetails;
        private TaskDetails _originalTask; // Task that brought the pallet here
        private ICommand _authenticateCommand;

        public Pallet PalletDetails
        {
            get => _palletDetails;
            set 
            { 
                _palletDetails = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayDetail1Label));
                OnPropertyChanged(nameof(DisplayDetail1Value));
                OnPropertyChanged(nameof(DisplayDetail2Value));
                OnPropertyChanged(nameof(DisplayDetail3Label));
                OnPropertyChanged(nameof(DisplayDetail3Value));
            }
        }

        public TaskDetails OriginalTask
        {
            get => _originalTask;
            set { _originalTask = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to initiate the authentication process for this pallet.
        /// This will likely be handled by the MainViewModel, passing this item as a parameter.
        /// </summary>
        public ICommand AuthenticateCommand
        {
            get => _authenticateCommand;
            set { _authenticateCommand = value; OnPropertyChanged(); } // Allow setting from MainViewModel
        }

        public PalletAuthenticationItem(Pallet pallet, TaskDetails originalTask)
        {
            PalletDetails = pallet;
            OriginalTask = originalTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // New dynamic properties for display
        public string DisplayDetail1Label
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? "שטר מטען" : "פרט";
        }
        public string DisplayDetail1Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export
                   ? PalletDetails?.ExportAwbNumber
                   : PalletDetails?.ImportUnit;
        }

        public string DisplayDetail2Value // Label "מופע" is static in XAML
        {
            get => PalletDetails?.UpdateType == UpdateType.Export
                   ? PalletDetails?.ExportAwbAppearance
                   : PalletDetails?.ImportAppearance;
        }

        public string DisplayDetail3Label
        {
            get => PalletDetails?.UpdateType == UpdateType.Export ? "אחסון" : "מצהר";
        }
        public string DisplayDetail3Value
        {
            get => PalletDetails?.UpdateType == UpdateType.Export
                   ? PalletDetails?.ExportAwbStorage
                   : PalletDetails?.ImportManifest;
        }
    }
}
