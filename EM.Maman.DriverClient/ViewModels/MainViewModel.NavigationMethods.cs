namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing navigation methods for MainViewModel
    public partial class MainViewModel
    {
        /// <summary>
        /// Switch to the warehouse view
        /// </summary>
        private void ShowWarehouseView()
        {
            IsWarehouseViewActive = true;
            (_showWarehouseViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Switch to the tasks view
        /// </summary>
        private void ShowTasksView()
        {
            IsWarehouseViewActive = false;
            (_showWarehouseViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        /// <summary>
        /// Switch to the map view within the tasks view
        /// </summary>
        private void ShowMapView()
        {
            IsMapViewActive = true;
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        /// <summary>
        /// Switch to the tasks list view within the tasks view
        /// </summary>
        private void ShowTasksListView()
        {
            IsMapViewActive = false;
            (_showMapViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_showTasksListViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
