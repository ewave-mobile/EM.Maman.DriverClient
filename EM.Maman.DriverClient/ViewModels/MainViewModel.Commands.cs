using System.Windows.Input;
using EM.Maman.DriverClient.Services;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        #region Command Fields

        private RelayCommand _showWarehouseViewCommand;
        private RelayCommand _showTasksViewCommand;
        private RelayCommand _showMapViewCommand;
        private RelayCommand _showTasksListViewCommand;
        private RelayCommand _showAuthenticationDialogCommand;
        private RelayCommand _goToStorageLocationCommand;
        private RelayCommand _changeDestinationCommand;
        private RelayCommand _goToRetrievalLocationCommand;
        private RelayCommand _changeSourceCommand;
        private RelayCommand _openCreateTaskDialogCommand;

        #endregion

        #region Command Properties

        public ICommand ShowWarehouseViewCommand => _showWarehouseViewCommand ??= new RelayCommand(_ => ShowWarehouseView(), _ => !IsWarehouseViewActive);
        public ICommand ShowTasksViewCommand => _showTasksViewCommand ??= new RelayCommand(_ => ShowTasksView(), _ => IsWarehouseViewActive);
        public ICommand ShowMapViewCommand => _showMapViewCommand ??= new RelayCommand(_ => ShowMapView(), _ => !IsMapViewActive && !IsWarehouseViewActive);
        public ICommand ShowTasksListViewCommand => _showTasksListViewCommand ??= new RelayCommand(_ => ShowTasksListView(), _ => IsMapViewActive && !IsWarehouseViewActive);
        public ICommand ShowAuthenticationDialogCommand => _showAuthenticationDialogCommand ??= new RelayCommand(ExecuteShowAuthenticationDialog);
        public ICommand GoToStorageLocationCommand => _goToStorageLocationCommand ??= new RelayCommand(ExecuteGoToStorageLocation, CanExecuteGoToStorageLocation);
        public ICommand ChangeDestinationCommand => _changeDestinationCommand ??= new RelayCommand(ExecuteChangeDestination, CanExecuteChangeDestination);
        public ICommand GoToRetrievalLocationCommand => _goToRetrievalLocationCommand ??= new RelayCommand(ExecuteGoToRetrievalLocation, CanExecuteGoToRetrievalLocation);
        public ICommand ChangeSourceCommand => _changeSourceCommand ??= new RelayCommand(ExecuteChangeSource, CanExecuteChangeSource);
        public ICommand OpenCreateTaskDialogCommand => _openCreateTaskDialogCommand ??= new RelayCommand(ExecuteOpenCreateTaskDialog);

        // Trolley Commands
        public ICommand MoveTrolleyUpCommand { get; private set; }
        public ICommand MoveTrolleyDownCommand { get; private set; }
        public ICommand TestLoadLeftCellCommand { get; private set; }
        public ICommand TestLoadRightCellCommand { get; private set; }
        public ICommand TestUnloadLeftCellCommand { get; private set; }
        public ICommand TestUnloadRightCellCommand { get; private set; }

        #endregion

        #region Command Initialization

        private void InitializeCommands()
        {
            // Initialize trolley movement commands
            MoveTrolleyUpCommand = new RelayCommand(_ => TrolleyMethods_MoveTrolleyUp(), _ => CurrentTrolley?.Position > 0);
            MoveTrolleyDownCommand = new RelayCommand(_ => TrolleyMethods_MoveTrolleyDown(), _ => true);

            // Initialize test commands
            TestLoadLeftCellCommand = TrolleyOperationsVM.TestLoadLeftCellCommand;
            TestLoadRightCellCommand = TrolleyOperationsVM.TestLoadRightCellCommand;
            TestUnloadLeftCellCommand = TrolleyOperationsVM.TestUnloadLeftCellCommand;
            TestUnloadRightCellCommand = TrolleyOperationsVM.TestUnloadRightCellCommand;
        }

        #endregion
    }
}
