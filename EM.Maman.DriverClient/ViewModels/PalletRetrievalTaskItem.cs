using EM.Maman.Models.CustomModels;
using EM.Maman.Models.LocalDbModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Represents a pallet ready for retrieval task.
    /// </summary>
    public class PalletRetrievalTaskItem : INotifyPropertyChanged
    {
        private Pallet _palletDetails;
        private TaskDetails _retrievalTask;
        private ICommand _goToRetrievalCommand; // Will be used for "Go To Destination" for items ready for delivery
        private ICommand _changeSourceCommand;
        private ICommand _unloadCommand; // New command for unloading
        private object _dataContext;
        private bool _canExecuteChangeSourceCommand;

        public Pallet PalletDetails
        {
            get => _palletDetails;
            set { _palletDetails = value; OnPropertyChanged(); }
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
        /// Command to navigate the trolley to the retrieval source.
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

        public PalletRetrievalTaskItem(Pallet pallet, TaskDetails retrievalTask)
        {
            PalletDetails = pallet;
            RetrievalTask = retrievalTask;
            DataContext = this;
            CanExecuteChangeSourceCommand = false; // Default to disabled
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
