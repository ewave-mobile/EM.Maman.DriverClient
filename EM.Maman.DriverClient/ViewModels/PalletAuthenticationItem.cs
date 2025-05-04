using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.CustomModels; // Added for TaskDetails
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
            set { _palletDetails = value; OnPropertyChanged(); }
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
    }
}
