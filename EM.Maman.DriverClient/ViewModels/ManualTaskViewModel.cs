using EM.Maman.DAL;
using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public class ManualTaskViewModel : INotifyPropertyChanged
    {
        private bool _isImportSelected = true;
        private ImportTaskViewModel _importVM;
        private ExportTaskViewModel _exportVM;
        private RelayCommand _selectImportCommand;
        private RelayCommand _selectExportCommand;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        public bool IsImportSelected
        {
            get => _isImportSelected;
            set
            {
                if (_isImportSelected != value)
                {
                    _isImportSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public ImportTaskViewModel ImportVM
        {
            get => _importVM;
            set
            {
                if (_importVM != value)
                {
                    _importVM = value;
                    OnPropertyChanged();
                }
            }
        }

        public ExportTaskViewModel ExportVM
        {
            get => _exportVM;
            set
            {
                if (_exportVM != value)
                {
                    _exportVM = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SelectImportCommand => _selectImportCommand ??= new RelayCommand(_ => IsImportSelected = true);
        public ICommand SelectExportCommand => _selectExportCommand ??= new RelayCommand(_ => IsImportSelected = false);

        public ManualTaskViewModel(IUnitOfWork unitOfWork, ImportTaskViewModel importVM, ExportTaskViewModel exportVM)
        {
            // Get the UnitOfWorkFactory from the App's ServiceProvider
            _unitOfWorkFactory = (App.Current as App)?.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            if (_unitOfWorkFactory == null)
            {
                throw new InvalidOperationException("Could not resolve IUnitOfWorkFactory from ServiceProvider");
            }
            ImportVM = importVM;
            ExportVM = exportVM;

            // Generate a unique task code
            string taskCode = $"TASK-{DateTime.Now:yyyyMMdd-HHmmss}";
            ImportVM.TaskDetails.Code = taskCode;
            ExportVM.TaskDetails.Code = taskCode;

            // Set the task creation date
            ImportVM.TaskDetails.CreatedDateTime = DateTime.Now;
            ExportVM.TaskDetails.CreatedDateTime = DateTime.Now;
        
        }
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            // Initialize import view model first
            await ImportVM.InitializeAsync();

            // Then initialize export view model 
            await ExportVM.InitializeAsync();
        }
        public bool Validate()
        {
            if (IsImportSelected)
            {
                // Validate import task
                if (ImportVM.SelectedSourceFinger == null)
                {
                    MessageBox.Show("Please select a source finger.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ImportVM.PalletDisplayName))
                {
                    MessageBox.Show("Please enter a pallet display name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else
            {
                // Validate export task
                if (ExportVM.SelectedPallet == null)
                {
                    MessageBox.Show("Please select a pallet.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (ExportVM.SelectedDestinationFinger == null)
                {
                    MessageBox.Show("Please select a destination finger.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
