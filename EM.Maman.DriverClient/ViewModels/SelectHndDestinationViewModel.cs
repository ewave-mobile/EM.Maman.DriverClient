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
using System.Windows;

namespace EM.Maman.DriverClient.ViewModels
{
    public class SelectHndDestinationViewModel : INotifyPropertyChanged
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ILogger<SelectHndDestinationViewModel> _logger;

        private ObservableCollection<Finger> _availableFingers;
        public ObservableCollection<Finger> AvailableFingers
        {
            get => _availableFingers;
            set { _availableFingers = value; OnPropertyChanged(); }
        }

        private Finger _selectedFinger;
        public Finger SelectedFinger
        {
            get => _selectedFinger;
            set { _selectedFinger = value; OnPropertyChanged(); if (value != null) SelectedCell = null; } // Clear other if one is selected
        }

        // For simplicity, we might just list all cells or provide a more structured way to select them.
        // Grouping by Level and then showing cells might be too complex for a simple dialog.
        // Let's assume for now we can list all cells or filter them.
        private ObservableCollection<Cell> _availableCells;
        public ObservableCollection<Cell> AvailableCells
        {
            get => _availableCells;
            set { _availableCells = value; OnPropertyChanged(); }
        }

        private Cell _selectedCell;
        public Cell SelectedCell
        {
            get => _selectedCell;
            set { _selectedCell = value; OnPropertyChanged(); if (value != null) SelectedFinger = null; } // Clear other if one is selected
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

            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);

            AvailableFingers = new ObservableCollection<Finger>();
            AvailableCells = new ObservableCollection<Cell>();
            
            LoadDestinationsAsync();
        }

        private async void LoadDestinationsAsync()
        {
            try
            {
                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var fingers = await uow.Fingers.GetAllAsync();
                    foreach (var finger in fingers.OrderBy(f => f.Position))
                    {
                        AvailableFingers.Add(finger);
                    }

                    // Loading all cells might be too much. Consider filtering or a different selection method.
                    // For now, let's load all that are not the source cell.
                    var cells = await uow.Cells.GetAllAsync();
                    foreach (var cell in cells.Where(c => c.Id != SourceCell?.Id).OrderBy(c => c.Level).ThenBy(c => c.Position).ThenBy(c => c.Order))
                    {
                        AvailableCells.Add(cell);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations for HND retrieval.");
                MessageBox.Show($"Error loading destinations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
