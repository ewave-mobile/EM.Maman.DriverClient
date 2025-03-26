using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using EM.Maman.DriverClient.Services;
using System.Windows;
using EM.Maman.Models.DisplayModels;

namespace EM.Maman.DriverClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields and Properties

        public ObservableCollection<Register> ReadOnlyRegisters { get; } = new();
        public ObservableCollection<Register> WritableRegisters { get; } = new();

        private int _positionPV;
        public int PositionPV
        {
            get => _positionPV;
            set
            {
                if (_positionPV != value)
                {
                    _positionPV = value;

                    // Extract level and position
                    int pvLevel = value / 100;
                    int pvPosition = value % 100;

                    // Update the trolley's position
                    if (CurrentTrolley != null)
                    {
                        CurrentTrolley.Position = pvPosition;
                        OnPropertyChanged(nameof(CurrentTrolley));
                    }

                    // THIS IS THE KEY ADDITION - update the TrolleyViewModel with level info
                    if (TrolleyVM != null)
                    {
                        TrolleyVM.UpdateTrolleyPosition(pvLevel, pvPosition);
                    }

                    OnPropertyChanged(nameof(PositionPV));
                    OnPropertyChanged(nameof(PvLevel));
                    OnPropertyChanged(nameof(PvLocation));
                }
            }
        }

        public int PvLevel => PositionPV / 100;

        public int PvLocation
        {
            get => PositionPV % 100;
            set
            {
                _currentTrolley.Position = value;
                OnPropertyChanged(nameof(PvLocation));
                OnPropertyChanged(nameof(CurrentTrolley));
            }
        }

        public string CurrentStatus { get; set; }
        public string Status { get; set; }

        private readonly IOpcService _opcService;
        private readonly IDispatcherService _dispatcherService;
        private double _plcRegisterValue;
        public double PlcRegisterValue
        {
            get => _plcRegisterValue;
            set
            {
                if (_plcRegisterValue != value)
                {
                    _plcRegisterValue = value;
                    OnPropertyChanged(nameof(PlcRegisterValue));
                }
            }
        }

        private Trolley _currentTrolley;
        public Trolley CurrentTrolley
        {
            get => _currentTrolley;
            set
            {
                if (_currentTrolley != value)
                {
                    _currentTrolley = value;
                    OnPropertyChanged(nameof(CurrentTrolley));
                }
            }
        }

        public TrolleyViewModel TrolleyVM { get; set; }

        #endregion

        #region Commands

        public ICommand MoveTrolleyUpCommand { get; }
        public ICommand MoveTrolleyDownCommand { get; }
        public ICommand SubscribeRegisterCommand { get; }
        public ICommand ConnectToOpcCommand { get; }
        public ICommand RefreshRegistersCommand { get; }
        public ICommand WriteRegisterCommand { get; }

        #endregion

        #region Constructor

        public MainViewModel(IOpcService opcService)
        {
            _opcService = opcService;
            _dispatcherService = new DispatcherService(); // Ideally injected via DI.

            _opcService.RegisterChanged += OpcService_RegisterChanged;

            InitializeRegisters();

            WriteRegisterCommand = new RelayCommand(WriteRegister, CanWriteRegister);
            ConnectToOpcCommand = new RelayCommand(_ => ConnectToOpc(), _ => true);
            MoveTrolleyUpCommand = new RelayCommand(_ => MoveTrolleyUp(), _ => CurrentTrolley.Position > 0);
            MoveTrolleyDownCommand = new RelayCommand(_ => MoveTrolleyDown(), _ => true);
            SubscribeRegisterCommand = new RelayCommand(_ => SubscribeRegister(ReadOnlyRegisters.FirstOrDefault(r => r.NodeId.Contains("Position_PV"))), _ => true);
            RefreshRegistersCommand = new RelayCommand(async _ => await RefreshRegistersAsync(), _ => true);

            TrolleyVM = new TrolleyViewModel();
            CurrentTrolley = new Trolley { Id = 1, DisplayName = "Main Trolley", Position = 1 };
            SubscribeToCellChanges();
            // Start the asynchronous initialization.
            InitializeOpcAsync();
        }

        /// <summary>
        /// Asynchronously connects to the OPC server, refreshes registers, and subscribes.
        /// </summary>
        private async void InitializeOpcAsync()
        {
            try
            {
                // Await the OPC connection.
                await _opcService.ConnectAsync("opc.tcp://172.18.67.32:49320");

                // Once connected, refresh registers.
                await RefreshRegistersAsync();

                // And then subscribe to registers.
                SubscribeRegister(ReadOnlyRegisters.FirstOrDefault(r => r.NodeId.Contains("Position_PV")));
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately (logging, user notification, etc.)
            }
        }

        #endregion

        #region Trolley Movement

        private void MoveTrolleyUp()
        {
            if (CurrentTrolley.Position > 0)
            {
                CurrentTrolley.Position--;
                OnPropertyChanged(nameof(CurrentTrolley));
            }
        }

        private void MoveTrolleyDown()
        {
            if (CurrentTrolley.Position < 23)
            {
                CurrentTrolley.Position++;
                OnPropertyChanged(nameof(CurrentTrolley));
            }
        }
        // Add these methods to the MainViewModel class to handle trolley cell operations

        // Method to add a pallet to the trolley's left cell
        public void AddPalletToTrolleyLeftCell(Pallet pallet)
        {
            if (TrolleyVM != null)
            {
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
            }
        }

        // Method to add a pallet to the trolley's right cell
        public void AddPalletToTrolleyRightCell(Pallet pallet)
        {
            if (TrolleyVM != null)
            {
                TrolleyVM.LoadPalletIntoRightCell(pallet);
            }
        }

        // Method to remove a pallet from the trolley's left cell
        public Pallet RemovePalletFromTrolleyLeftCell()
        {
            if (TrolleyVM != null)
            {
                return TrolleyVM.RemovePalletFromLeftCell();
            }
            return null;
        }

        // Method to remove a pallet from the trolley's right cell
        public Pallet RemovePalletFromTrolleyRightCell()
        {
            if (TrolleyVM != null)
            {
                return TrolleyVM.RemovePalletFromRightCell();
            }
            return null;
        }

        // Method to execute when a pick operation is initiated from the UI
        public void PickPallet(Cell sourceCell, Pallet pallet)
        {
            // Check if the trolley has an available cell
            if (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.RightCell.IsOccupied)
            {
                // No available cell, show message to user
                // In a real app, use a message service or similar
                MessageBox.Show("Trolley has no available cells. Please unload a cell first.");
                return;
            }

            // Determine which cell to use (left first if available)
            if (!TrolleyVM.LeftCell.IsOccupied)
            {
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
            }
            else
            {
                TrolleyVM.LoadPalletIntoRightCell(pallet);
            }

            // In a real application, you would remove the pallet from the source cell
            // through the repository and update the database
        }

        // Method to execute when a put operation is initiated from the UI
        public void PutPallet(Cell destinationCell, string cellSide)
        {
            Pallet pallet = null;

            // Determine which trolley cell to unload from
            if (cellSide == "Left" && TrolleyVM.LeftCell.IsOccupied)
            {
                pallet = TrolleyVM.RemovePalletFromLeftCell();
            }
            else if (cellSide == "Right" && TrolleyVM.RightCell.IsOccupied)
            {
                pallet = TrolleyVM.RemovePalletFromRightCell();
            }

            if (pallet == null)
            {
                // No pallet found in the selected cell
                MessageBox.Show("No pallet in the selected trolley cell.");
                return;
            }

            // In a real application, you would update the destination cell through the repository
            // and update the database
        }

        // Add these properties and initialization to the MainViewModel class

        // Test Commands for Trolley Cells
        private RelayCommand _testLoadLeftCellCommand;
        private RelayCommand _testLoadRightCellCommand;
        private RelayCommand _testUnloadLeftCellCommand;
        private RelayCommand _testUnloadRightCellCommand;

        public ICommand TestLoadLeftCellCommand => _testLoadLeftCellCommand ??= new RelayCommand(_ => TestLoadLeftCell(), _ => true);
        public ICommand TestLoadRightCellCommand => _testLoadRightCellCommand ??= new RelayCommand(_ => TestLoadRightCell(), _ => true);
        public ICommand TestUnloadLeftCellCommand => _testUnloadLeftCellCommand ??= new RelayCommand(_ => TestUnloadLeftCell(), _ => CanUnloadLeftCell());
        public ICommand TestUnloadRightCellCommand => _testUnloadRightCellCommand ??= new RelayCommand(_ => TestUnloadRightCell(), _ => CanUnloadRightCell());

        // Add these methods to handle the test commands

        // Helper method to get the current composite row based on trolley position
        // Helper method to get the current composite row based on trolley position
        private CompositeRow GetCurrentRow()
        {
            if (TrolleyVM?.Rows == null || CurrentTrolley == null)
                return null;

            return TrolleyVM.Rows.FirstOrDefault(row => row.Position == CurrentTrolley.Position);
        }

        // Helper method to update a warehouse cell's pallet
        private void UpdateWarehouseCellPallet(int position, bool isLeftSide, bool isOuterCell, Pallet pallet)
        {
            // Find the correct row
            var row = TrolleyVM.Rows.FirstOrDefault(r => r.Position == position);
            if (row == null) return;

            // Update the appropriate cell pallet property
            if (isLeftSide)
            {
                if (isOuterCell)
                    row.LeftOuterPallet = pallet;
                else
                    row.LeftInnerPallet = pallet;
            }
            else // Right side
            {
                if (isOuterCell)
                    row.RightOuterPallet = pallet;
                else
                    row.RightInnerPallet = pallet;
            }

            // Notify the UI that the row has changed (needed since we're modifying a property of a property)
            var rowIndex = TrolleyVM.Rows.IndexOf(row);
            if (rowIndex >= 0)
            {
                TrolleyVM.Rows.RemoveAt(rowIndex);
                TrolleyVM.Rows.Insert(rowIndex, row);
            }
        }

        // Modified TestLoadLeftCell method with improved visual updates and level handling
        private void TestLoadLeftCell()
        {
            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            // Check if we're at the appropriate level for the warehouse cells
            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                MessageBox.Show($"Trolley is at level {TrolleyVM.CurrentLevelNumber} but viewing level {TrolleyVM.SelectedLevelNumber}. " +
                               "Please ensure you're viewing the correct level.");
            }

            // Check if there's a pallet in the left outer cell
            bool hasOuterPallet = currentRow.LeftOuterPallet != null;

            if (hasOuterPallet)
            {
                // Get pallet info from the warehouse cell
                Pallet warehousePallet = currentRow.LeftOuterPallet;
                string palletDisplay = warehousePallet.DisplayName;

                // Check if trolley cell already has a pallet
                if (TrolleyVM.LeftCell.IsOccupied)
                {
                    // Create a reference to the current trolley pallet before replacing it
                    Pallet trolleyPallet = TrolleyVM.LeftCell.Pallet;
                    string trolleyPalletDisplay = trolleyPallet.DisplayName;

                    MessageBox.Show($"Swapping: Pallet {trolleyPalletDisplay} moved to left outer cell and pallet {palletDisplay} moved to trolley");

                    // Swap the pallets (visually update both the trolley and warehouse cells)
                    var tempPallet = warehousePallet;

                    // Update warehouse cell with trolley's pallet
                    UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, true, trolleyPallet);

                    // Update trolley cell with warehouse pallet
                    TrolleyVM.LoadPalletIntoLeftCell(tempPallet);
                }
                else
                {
                    MessageBox.Show($"Loading: Pallet {palletDisplay} from left outer cell to trolley");

                    // Load the pallet into the trolley cell
                    TrolleyVM.LoadPalletIntoLeftCell(warehousePallet);

                    // Remove the pallet from the warehouse cell
                    UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, true, null);
                }
            }
            else
            {
                MessageBox.Show("No pallet in the left outer cell to load");

                // Check inner cell as an alternative (if requested by user)
                bool hasInnerPallet = currentRow.LeftInnerPallet != null;
                if (hasInnerPallet)
                {
                    var result = MessageBox.Show(
                        "Would you like to use the pallet from the inner cell instead?",
                        "Use Inner Cell",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Get pallet from inner cell
                        Pallet innerPallet = currentRow.LeftInnerPallet;

                        if (TrolleyVM.LeftCell.IsOccupied)
                        {
                            // Swap with trolley pallet
                            Pallet trolleyPallet = TrolleyVM.LeftCell.Pallet;

                            UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, false, trolleyPallet);
                            TrolleyVM.LoadPalletIntoLeftCell(innerPallet);

                            MessageBox.Show($"Swapped with inner cell pallet: {innerPallet.DisplayName}");
                        }
                        else
                        {
                            // Just load from inner cell
                            TrolleyVM.LoadPalletIntoLeftCell(innerPallet);
                            UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, false, null);

                            MessageBox.Show($"Loaded from inner cell: {innerPallet.DisplayName}");
                        }
                        return;
                    }
                }

                //// For demonstration purposes when no warehouse pallet is available
                //if (!TrolleyVM.LeftCell.IsOccupied)
                //{
                //    var random = new Random();
                //    int palletId = random.Next(1000, 10000);
                //    var pallet = new Pallet
                //    {
                //        Id = palletId,
                //        DisplayName = $"L-{palletId}",
                //        UldType = "AKE",
                //        UldCode = $"AKE{palletId}LT"
                //    };

                //    TrolleyVM.LoadPalletIntoLeftCell(pallet);
                //}
            }
        }

        // Modified TestLoadRightCell method with improved visual updates and level handling
        private void TestLoadRightCell()
        {
            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            // Check if we're at the appropriate level for the warehouse cells
            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                MessageBox.Show($"Trolley is at level {TrolleyVM.CurrentLevelNumber} but viewing level {TrolleyVM.SelectedLevelNumber}. " +
                               "Please ensure you're viewing the correct level.");
            }

            // Check if there's a pallet in the right outer cell
            bool hasOuterPallet = currentRow.RightOuterPallet != null;

            if (hasOuterPallet)
            {
                // Get pallet info from the warehouse cell
                Pallet warehousePallet = currentRow.RightOuterPallet;
                string palletDisplay = warehousePallet.DisplayName;

                // Check if trolley cell already has a pallet
                if (TrolleyVM.RightCell.IsOccupied)
                {
                    // Create a reference to the current trolley pallet before replacing it
                    Pallet trolleyPallet = TrolleyVM.RightCell.Pallet;
                    string trolleyPalletDisplay = trolleyPallet.DisplayName;

                    MessageBox.Show($"Swapping: Pallet {trolleyPalletDisplay} moved to right outer cell and pallet {palletDisplay} moved to trolley");

                    // Swap the pallets (visually update both the trolley and warehouse cells)
                    var tempPallet = warehousePallet;

                    // Update warehouse cell with trolley's pallet
                    UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, true, trolleyPallet);

                    // Update trolley cell with warehouse pallet
                    TrolleyVM.LoadPalletIntoRightCell(tempPallet);
                }
                else
                {
                    MessageBox.Show($"Loading: Pallet {palletDisplay} from right outer cell to trolley");

                    // Load the pallet into the trolley cell
                    TrolleyVM.LoadPalletIntoRightCell(warehousePallet);

                    // Remove the pallet from the warehouse cell
                    UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0 , false, true, null);
                }
            }
            else
            {
                MessageBox.Show("No pallet in the right outer cell to load");

                // Check inner cell as an alternative (if requested by user)
                bool hasInnerPallet = currentRow.RightInnerPallet != null;
                if (hasInnerPallet)
                {
                    var result = MessageBox.Show(
                        "Would you like to use the pallet from the inner cell instead?",
                        "Use Inner Cell",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Get pallet from inner cell
                        Pallet innerPallet = currentRow.RightInnerPallet;

                        if (TrolleyVM.RightCell.IsOccupied)
                        {
                            // Swap with trolley pallet
                            Pallet trolleyPallet = TrolleyVM.RightCell.Pallet;

                            UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, false, trolleyPallet);
                            TrolleyVM.LoadPalletIntoRightCell(innerPallet);

                            MessageBox.Show($"Swapped with inner cell pallet: {innerPallet.DisplayName}");
                        }
                        else
                        {
                            // Just load from inner cell
                            TrolleyVM.LoadPalletIntoRightCell(innerPallet);
                            UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, false, null);

                            MessageBox.Show($"Loaded from inner cell: {innerPallet.DisplayName}");
                        }
                        return;
                    }
                }

                // For demonstration purposes when no warehouse pallet is available
                //if (!TrolleyVM.RightCell.IsOccupied)
                //{
                //    var random = new Random();
                //    int palletId = random.Next(1000, 10000);
                //    var pallet = new Pallet
                //    {
                //        Id = palletId,
                //        DisplayName = $"R-{palletId}",
                //        UldType = "PAG",
                //        UldCode = $"PAG{palletId}RT"
                //    };

                //    TrolleyVM.LoadPalletIntoRightCell(pallet);
                //}
            }
        }

        // Modified TestUnloadLeftCell method with improved visual updates and level handling
        private void TestUnloadLeftCell()
        {
            // Check if trolley left cell has a pallet
            if (!TrolleyVM.LeftCell.IsOccupied)
            {
                MessageBox.Show("No pallet in left trolley cell to unload");
                return;
            }

            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            // Check if we're at the appropriate level for the warehouse cells
            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                MessageBox.Show($"Trolley is at level {TrolleyVM.CurrentLevelNumber} but viewing level {TrolleyVM.SelectedLevelNumber}. " +
                               "Please ensure you're viewing the correct level.");
            }

            // Get the trolley pallet info before removing it
            Pallet trolleyPallet = TrolleyVM.LeftCell.Pallet;
            string palletDisplayName = trolleyPallet.DisplayName;

            // Check if the warehouse outer cell has a pallet
            bool outerCellHasPallet = currentRow.LeftOuterPallet != null;

            if (outerCellHasPallet)
            {
                // Get the warehouse pallet info
                Pallet warehousePallet = currentRow.LeftOuterPallet;
                string warehousePalletDisplay = warehousePallet.DisplayName;

                // In a real system, you'd swap the pallets
                MessageBox.Show($"Swapping: Pallet {palletDisplayName} moved to left outer cell and pallet {warehousePalletDisplay} moved to trolley");

                // Swap the pallets
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, true, trolleyPallet);
                TrolleyVM.LoadPalletIntoLeftCell(warehousePallet);
            }
            else
            {
                // Check inner cell if outer is empty
                bool innerCellHasPallet = currentRow.LeftInnerPallet != null;

                if (innerCellHasPallet)
                {
                    // Offer to swap with inner cell pallet
                    var result = MessageBox.Show(
                        "Outer cell is empty but inner cell has a pallet. Would you like to swap with inner cell?",
                        "Swap with Inner Cell",
                        MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Swap with inner cell pallet
                        Pallet innerPallet = currentRow.LeftInnerPallet;
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, false, trolleyPallet);
                        TrolleyVM.LoadPalletIntoLeftCell(innerPallet);

                        MessageBox.Show($"Swapped with inner cell pallet: {innerPallet.DisplayName}");
                        return;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return; // Cancel the operation
                    }
                    // If No, continue to outer cell placement
                }

                // In a real system, you'd move the pallet to the outer cell
                MessageBox.Show($"Unloading: Pallet {palletDisplayName} moved to left outer cell");

                // Update the warehouse cell with the trolley pallet
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, true, true, trolleyPallet);

                // Remove the pallet from the trolley
                TrolleyVM.RemovePalletFromLeftCell();
            }
        }

        // Modified TestUnloadRightCell method with improved visual updates and level handling
        private void TestUnloadRightCell()
        {
            // Check if trolley right cell has a pallet
            if (!TrolleyVM.RightCell.IsOccupied)
            {
                MessageBox.Show("No pallet in right trolley cell to unload");
                return;
            }

            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            // Check if we're at the appropriate level for the warehouse cells
            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                MessageBox.Show($"Trolley is at level {TrolleyVM.CurrentLevelNumber} but viewing level {TrolleyVM.SelectedLevelNumber}. " +
                               "Please ensure you're viewing the correct level.");
            }

            // Get the trolley pallet info before removing it
            Pallet trolleyPallet = TrolleyVM.RightCell.Pallet;
            string palletDisplayName = trolleyPallet.DisplayName;

            // Check if the warehouse outer cell has a pallet
            bool outerCellHasPallet = currentRow.RightOuterPallet != null;

            if (outerCellHasPallet)
            {
                // Get the warehouse pallet info
                Pallet warehousePallet = currentRow.RightOuterPallet;
                string warehousePalletDisplay = warehousePallet.DisplayName;

                // In a real system, you'd swap the pallets
                MessageBox.Show($"Swapping: Pallet {palletDisplayName} moved to right outer cell and pallet {warehousePalletDisplay} moved to trolley");

                // Swap the pallets
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, true, trolleyPallet);
                TrolleyVM.LoadPalletIntoRightCell(warehousePallet);
            }
            else
            {
                // Check inner cell if outer is empty
                bool innerCellHasPallet = currentRow.RightInnerPallet != null;

                if (innerCellHasPallet)
                {
                    // Offer to swap with inner cell pallet
                    var result = MessageBox.Show(
                        "Outer cell is empty but inner cell has a pallet. Would you like to swap with inner cell?",
                        "Swap with Inner Cell",
                        MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Swap with inner cell pallet
                        Pallet innerPallet = currentRow.RightInnerPallet;
                        UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, false, trolleyPallet);
                        TrolleyVM.LoadPalletIntoRightCell(innerPallet);

                        MessageBox.Show($"Swapped with inner cell pallet: {innerPallet.DisplayName}");
                        return;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return; // Cancel the operation
                    }
                    // If No, continue to outer cell placement
                }

                // In a real system, you'd move the pallet to the outer cell
                MessageBox.Show($"Unloading: Pallet {palletDisplayName} moved to right outer cell");

                // Update the warehouse cell with the trolley pallet
                UpdateWarehouseCellPallet(CurrentTrolley.Position ?? 0, false, true, trolleyPallet);

                // Remove the pallet from the trolley
                TrolleyVM.RemovePalletFromRightCell();
            }
        }

        private bool CanUnloadLeftCell()
        {
            return TrolleyVM?.LeftCell?.IsOccupied ?? false;
        }

        private bool CanUnloadRightCell()
        {
            return TrolleyVM?.RightCell?.IsOccupied ?? false;
        }

        // Don't forget to update command CanExecute when cell state changes
        private void UpdateCommandStates()
        {
            (_testUnloadLeftCellCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (_testUnloadRightCellCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Make sure to call UpdateCommandStates whenever trolley cell state changes.
        // This can be done by subscribing to the PropertyChanged event of TrolleyVM.LeftCell and RightCell
        // (For this demo, you could add the subscription in the constructor)
        #endregion

        #region OPC and Register Handling

        private async void ConnectToOpc()
        {
            await _opcService.ConnectAsync("opc.tcp://172.18.67.32:49320");
        }

        private void WriteRegister(object parameter)
        {
            _opcService.WriteRegisterAsync("MyRegister", PlcRegisterValue);
        }

        private bool CanWriteRegister(object parameter)
        {
            return true;
        }

        // Modified version of the OpcService_RegisterChanged method
        private void OpcService_RegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            if (e.RegisterName == "MyRegister")
            {
                if (double.TryParse(e.NewValue.ToString(), out double result))
                {
                    PlcRegisterValue = result;
                }
            }

            // For Position_PV updates
            if (e.RegisterName.Contains("Position_PV") || e.RegisterName.Contains("position_pv"))
            {
                _dispatcherService.Invoke(() =>
                {
                    if (int.TryParse(e.NewValue.ToString(), out var newPv))
                    {
                        // This will now trigger the enhanced PositionPV setter
                        PositionPV = newPv;
                    }
                });
            }

            // You might want to add registers for trolley cell states
            // For example, if the PLC has registers indicating what's in each trolley cell
            if (e.RegisterName.Contains("TrolleyLeftCell"))
            {
                _dispatcherService.Invoke(() =>
                {
                    // Parse the value and update the trolley's left cell accordingly
                    // This is a simplified example - in a real app, you'd handle this based on your PLC data format
                    if (int.TryParse(e.NewValue.ToString(), out var palletId) && palletId > 0)
                    {
                        // In a real application, you would look up the pallet by ID and add it to the trolley
                        // For demo purposes, we'll create a sample pallet
                        var pallet = new Pallet { Id = palletId, DisplayName = $"PLT-{palletId}", UldType = "AKE" };
                        TrolleyVM.LoadPalletIntoLeftCell(pallet);
                    }
                    else
                    {
                        // If the value is 0 or invalid, remove any pallet from the cell
                        TrolleyVM.RemovePalletFromLeftCell();
                    }
                });
            }

            if (e.RegisterName.Contains("TrolleyRightCell"))
            {
                _dispatcherService.Invoke(() =>
                {
                    if (int.TryParse(e.NewValue.ToString(), out var palletId) && palletId > 0)
                    {
                        var pallet = new Pallet { Id = palletId, DisplayName = $"PLT-{palletId}", UldType = "AKE" };
                        TrolleyVM.LoadPalletIntoRightCell(pallet);
                    }
                    else
                    {
                        TrolleyVM.RemovePalletFromRightCell();
                    }
                });
            }
        }


        private void SubscribeRegister(Register register)
        {
            try
            {
                _opcService.SubscribeToRegister(register.NodeId, (updatedRegister) =>
                {
                    _dispatcherService.Invoke(() =>
                    {
                        var roReg = ReadOnlyRegisters.FirstOrDefault(r => r.NodeId == updatedRegister.NodeId);
                        if (roReg != null)
                        {
                            roReg.Value = updatedRegister.Value;
                            roReg.IsSubscribed = true;
                        }
                        var wrReg = WritableRegisters.FirstOrDefault(r => r.NodeId == updatedRegister.NodeId);
                        if (wrReg != null)
                        {
                            wrReg.Value = updatedRegister.Value;
                            wrReg.IsSubscribed = true;
                        }
                        if (updatedRegister.NodeId.Contains("Position_PV"))
                        {
                            
                            if (int.TryParse(updatedRegister.Value, out var newPv))
                                PositionPV = newPv;
                        }
                        if (updatedRegister.NodeId.Contains("status"))
                        {
                            CurrentStatus = updatedRegister.Value;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                // Handle subscription exceptions.
            }
        }

        private async System.Threading.Tasks.Task RefreshRegistersAsync()
        {
            try
            {
                var roNodeIds = ReadOnlyRegisters.Select(r => r.NodeId).ToList();
                var roReadResult = await _opcService.ReadRegistersAsync(roNodeIds);
                ReadOnlyRegisters.Clear();
                foreach (var reg in roReadResult)
                {
                    ReadOnlyRegisters.Add(reg);
                    if (reg.NodeId.Contains("Position_PV") && int.TryParse(reg.Value, out var parsedInt))
                        PositionPV = parsedInt;
                    if (reg.NodeId.Contains("status"))
                        CurrentStatus = reg.Value;
                }

                var wNodeIds = WritableRegisters.Select(r => r.NodeId).ToList();
                var wReadResult = await _opcService.ReadRegistersAsync(wNodeIds);
                WritableRegisters.Clear();
                foreach (var reg in wReadResult)
                {
                    WritableRegisters.Add(reg);
                }
            }
            catch (Exception ex)
            {
                // Handle refresh errors.
            }
        }

        private void InitializeRegisters()
        {
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.Position_PV", Value = "" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.HeightLeft", Value = "" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.HeightRight", Value = "" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.In_Out", Value = "" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.WatchDog", Value = "" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.status", Value = "" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.counter", Value = "" });

            WritableRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.PositionRequest", Value = "" });
            WritableRegisters.Add(new Register { NodeId = "ns=2;s=s7.s7 300.maman.control", Value = "" });
        }
        private void SubscribeToCellChanges()
        {
            if (TrolleyVM != null)
            {
                // Subscribe to left cell changes
                if (TrolleyVM.LeftCell != null)
                {
                    TrolleyVM.LeftCell.PropertyChanged += Cell_PropertyChanged;
                }

                // Subscribe to right cell changes
                if (TrolleyVM.RightCell != null)
                {
                    TrolleyVM.RightCell.PropertyChanged += Cell_PropertyChanged;
                }
            }
        }
        // Handler for cell property changes
        private void Cell_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrolleyCell.IsOccupied) || e.PropertyName == nameof(TrolleyCell.Pallet))
            {
                // Update command states whenever cell occupancy changes
                UpdateCommandStates();
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
