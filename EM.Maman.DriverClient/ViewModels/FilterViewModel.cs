using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

// Assuming EM.Maman.DriverClient.ViewModels.RelayCommand exists and is suitable.
// Also assuming a ViewModelBase or similar INotifyPropertyChanged implementation exists.
// If ViewModelBase is not in this exact namespace, adjust the using statement or its location.

namespace EM.Maman.DriverClient.ViewModels
{
    // Basic ViewModelBase for INotifyPropertyChanged (if not already existing in your project)
    // If you have a common ViewModelBase, this can be removed or adjusted.
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FlightFilterItemViewModel : ViewModelBase
    {
        private string _flightId;
        public string FlightId
        {
            get => _flightId;
            set { _flightId = value; OnPropertyChanged(); }
        }

        private string _palletCountText;
        public string PalletCountText
        {
            get => _palletCountText;
            set { _palletCountText = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private string _sideColor;
        public string SideColor 
        {
            get => _sideColor;
            set { _sideColor = value; OnPropertyChanged(); }
        }
    }

    public class FilterViewModel : ViewModelBase // Replace ViewModelBase with your actual base if different
    {
        public ObservableCollection<string> ImportExportOptions { get; set; }
        public ObservableCollection<string> CargoTypeOptions { get; set; }
        public ObservableCollection<string> CellTypeOptions { get; set; }
        public ObservableCollection<string> ExecutionTypeOptions { get; set; }

        private string _selectedImportExport;
        public string SelectedImportExport
        {
            get => _selectedImportExport;
            set { _selectedImportExport = value; OnPropertyChanged(); /* TODO: Trigger filtering logic */ }
        }

        private string _selectedCargoType;
        public string SelectedCargoType
        {
            get => _selectedCargoType;
            set { _selectedCargoType = value; OnPropertyChanged(); /* TODO: Trigger filtering logic */ }
        }

        private string _selectedCellType;
        public string SelectedCellType
        {
            get => _selectedCellType;
            set { _selectedCellType = value; OnPropertyChanged(); /* TODO: Trigger filtering logic */ }
        }

        private string _selectedExecutionType;
        public string SelectedExecutionType
        {
            get => _selectedExecutionType;
            set { _selectedExecutionType = value; OnPropertyChanged(); /* TODO: Trigger filtering logic */ }
        }

        private bool _showFlights = true;
        public bool ShowFlights
        {
            get => _showFlights;
            set { _showFlights = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FlightFilterItemViewModel> FlightItems { get; set; }
        public ICommand AddFlightCommand { get; }

        public FilterViewModel()
        {
            ImportExportOptions = new ObservableCollection<string> { "הכל", "ייבוא", "ייצוא" };
            SelectedImportExport = ImportExportOptions[0];

            CargoTypeOptions = new ObservableCollection<string> { "הכל", "רגיל", "מסוכן", "בעל חיים" }; // Example, use EM.Maman.Models.Enums.CargoType if applicable
            SelectedCargoType = CargoTypeOptions[0];

            CellTypeOptions = new ObservableCollection<string> { "הכל", "רגיל", "מקורר" }; // Example
            SelectedCellType = CellTypeOptions[0];

            ExecutionTypeOptions = new ObservableCollection<string> { "הכל", "ידני", "מערכת" };
            SelectedExecutionType = ExecutionTypeOptions[0];

            FlightItems = new ObservableCollection<FlightFilterItemViewModel>
            {
                new FlightFilterItemViewModel { FlightId = "0111", PalletCountText = "12 פריטים", IsSelected = true, SideColor = "#4CAF50" }, // Green
                new FlightFilterItemViewModel { FlightId = "786", PalletCountText = "12 פריטים", IsSelected = true, SideColor = "#2196F3" },  // Blue
                new FlightFilterItemViewModel { FlightId = "A345", PalletCountText = "12 פריטים", IsSelected = false, SideColor = "#9C27B0" }, // Purple
                new FlightFilterItemViewModel { FlightId = "A3466", PalletCountText = "12 פריטים", IsSelected = true, SideColor = "#FFC107" }  // Yellow
            };
            // Ensure you have a RelayCommand implementation available in your project.
            // Using the one from EM.Maman.DriverClient/ViewModels/RelayCommand.cs
            AddFlightCommand = new RelayCommand(ExecuteAddFlight); 
        }

        private void ExecuteAddFlight(object parameter)
        {
            // Placeholder for Add Flight logic
            FlightItems.Add(new FlightFilterItemViewModel { FlightId = "טיסה חדשה", PalletCountText = "0 פריטים", SideColor = "#795548" }); // Brown
        }
    }
}
