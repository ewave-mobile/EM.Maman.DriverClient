using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic; // Added for List
using System.Windows;

namespace EM.Maman.DriverClient.ViewModels
{
    public class SelectHndDestinationViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ILogger<SelectHndDestinationViewModel> _logger;
        private List<Cell> _allAvailableCells; // Master list of cells

        public ObservableCollection<Finger> AvailableFingers { get; } = new ObservableCollection<Finger>();

        private Finger _selectedFinger;
        public Finger SelectedFinger
        {
            get => _selectedFinger;
            set 
            { 
                _selectedFinger = value; 
                OnPropertyChanged(); 
                if (value != null) SelectedCell = null; 
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Cell> AvailableCells { get; } = new ObservableCollection<Cell>();

        private Cell _selectedCell;
        public Cell SelectedCell
        {
            get => _selectedCell;
            set 
            { 
                _selectedCell = value; 
                OnPropertyChanged(); 
                if (value != null) SelectedFinger = null;
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<int?> AvailableRows { get; } = new ObservableCollection<int?>();
        public ObservableCollection<int?> AvailableLevels { get; } = new ObservableCollection<int?>();

        private int? _selectedRow;
        public int? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (_selectedRow != value)
                {
                    _selectedRow = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private int? _selectedLevel;
        public int? SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                if (_selectedLevel != value)
                {
                    _selectedLevel = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public bool? DialogResult { get; private set; }

        public Pallet LoadedPallet { get; private set; }
        public Cell SourceCell { get; private set; }


        public SelectHndDestinationViewModel(IUnitOfWorkFactory unitOfWorkFactory, ILogger<SelectHndDestinationViewModel> logger, Pallet loadedPallet, Cell sourceCell)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
            _logger = logger;
            LoadedPallet = loadedPallet;
            SourceCell = sourceCell;
            _allAvailableCells = new List<Cell>();

            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
            
            LoadDestinationsAsync();
        }

        private async void LoadDestinationsAsync()
        {
            try
            {
                AvailableFingers.Clear();
                _allAvailableCells.Clear();
                AvailableRows.Clear();
                AvailableLevels.Clear();

                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var fingers = await uow.Fingers.GetAllAsync();
                    foreach (var finger in fingers.OrderBy(f => f.Position))
                    {
                        AvailableFingers.Add(finger);
                    }

                    var allCellsFromDb = await uow.Cells.GetAllAsync();
                    _allAvailableCells.AddRange(allCellsFromDb.Where(c => c.Id != SourceCell?.Id));

                    // Populate AvailableRows
                    AvailableRows.Add(null); // Option for "All Rows"
                    var distinctRows = _allAvailableCells
                                        .Select(c => c.Position)
                                        .Where(p => p.HasValue) // Ensure we only take non-null positions
                                        .Distinct()
                                        .OrderBy(p => p);
                    foreach (var rowNum in distinctRows)
                    {
                        AvailableRows.Add(rowNum);
                    }

                    // Populate AvailableLevels
                    AvailableLevels.Add(null); // Option for "All Levels"
                    var distinctLevels = _allAvailableCells
                                        .Select(c => c.Level)
                                        .Where(l => l.HasValue) // Ensure we only take non-null levels
                                        .Distinct()
                                        .OrderBy(l => l);
                    foreach (var levelNum in distinctLevels)
                    {
                        AvailableLevels.Add(levelNum);
                    }
                }
                // Initial filter
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations for HND retrieval.");
                MessageBox.Show($"Error loading destinations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            AvailableCells.Clear();
            var filtered = _allAvailableCells.AsEnumerable();

            if (SelectedLevel.HasValue)
            {
                filtered = filtered.Where(c => c.Level == SelectedLevel.Value);
            }

            if (SelectedRow.HasValue)
            {
                filtered = filtered.Where(c => c.Position == SelectedRow.Value);
            }

            foreach (var cell in filtered.OrderBy(c => c.Level).ThenBy(c => c.Position).ThenBy(c => c.Order))
            {
                AvailableCells.Add(cell);
            }
        }

        private bool CanConfirm(object parameter)
        {
            return SelectedFinger != null || SelectedCell != null;
        }

        private void Confirm(object parameter)
        {
            if (!CanConfirm(null)) return;
            DialogResult = true;
            CloseDialog(parameter as Window);
        }

        private void Cancel(object parameter)
        {
            DialogResult = false;
            CloseDialog(parameter as Window);
        }

        private void CloseDialog(Window window)
        {
            if (window != null)
            {
                window.DialogResult = DialogResult;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
