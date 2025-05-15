using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Added for ILogger extension methods
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class TrolleyOperationsViewModel : INotifyPropertyChanged
    {
        private TrolleyViewModel _trolleyVM;
        private Trolley _currentTrolley;
        private MainViewModel _mainViewModel;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ILogger<TrolleyOperationsViewModel> _logger;

        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
        public TrolleyViewModel TrolleyVM
        {
            get => _trolleyVM;
            set
            {
                if (_trolleyVM != value)
                {
                    _trolleyVM = value;
                    OnPropertyChanged(nameof(TrolleyVM));
                }
            }
        }

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

        // Test Commands for Trolley Cells
        private RelayCommand _testLoadLeftCellCommand;
        private RelayCommand _testLoadRightCellCommand;
        private RelayCommand _testUnloadLeftCellCommand;
        private RelayCommand _testUnloadRightCellCommand;

        public ICommand TestLoadLeftCellCommand => _testLoadLeftCellCommand ??= new RelayCommand(async _ => await LoadPalletFromWarehouseLeftCellAsync(0), _ => true); // Assuming default to outer cell (depth 1)
        public ICommand TestLoadRightCellCommand => _testLoadRightCellCommand ??= new RelayCommand(async _ => await LoadPalletFromWarehouseRightCellAsync(0), _ => true); // Assuming default to outer cell (depth 1)
        public ICommand TestUnloadLeftCellCommand => _testUnloadLeftCellCommand ??= new RelayCommand(async _ => await UnloadLeftCellImplementationAsync(), _ => CanUnloadLeftCell());
        public ICommand TestUnloadRightCellCommand => _testUnloadRightCellCommand ??= new RelayCommand(async _ => await UnloadRightCellImplementationAsync(), _ => CanUnloadRightCell());

        public TrolleyOperationsViewModel(
            TrolleyViewModel trolleyVM, 
            Trolley currentTrolley, 
            IUnitOfWorkFactory unitOfWorkFactory,
            ILogger<TrolleyOperationsViewModel> logger)
        {
            TrolleyVM = trolleyVM;
            CurrentTrolley = currentTrolley;
            _unitOfWorkFactory = unitOfWorkFactory;
            _logger = logger;
            
            // Get the UnitOfWorkFactory from the App's ServiceProvider - This line might be redundant if injected
            // _unitOfWorkFactory = (App.Current as App)?.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            if (_unitOfWorkFactory == null)
            {
                throw new InvalidOperationException("Could not resolve IUnitOfWorkFactory from ServiceProvider");
            }
        }

        // Method to add a pallet to the trolley's left cell
        public async System.Threading.Tasks.Task AddPalletToTrolleyLeftCellAsync(Pallet pallet)
        {
            if (TrolleyVM != null && pallet != null)
            {
                // Update UI
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
                
                // Persist to database
                try
                {
                    using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                    {
                            await unitOfWork.TrolleyCells.AddPalletToTrolleyCellAsync(
                                CurrentTrolley.Id, 
                                EM.Maman.Common.Constants.TrolleyConstants.LeftCellPosition, 
                                (int)pallet.Id);
                        await unitOfWork.CompleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    System.Diagnostics.Debug.WriteLine($"Error persisting pallet to left trolley cell: {ex.Message}");
                    MessageBox.Show($"Error saving trolley state: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Method to add a pallet to the trolley's right cell
        public async System.Threading.Tasks.Task AddPalletToTrolleyRightCellAsync(Pallet pallet)
        {
            if (TrolleyVM != null && pallet != null)
            {
                // Update UI
                TrolleyVM.LoadPalletIntoRightCell(pallet);
                
                // Persist to database
                try
                {
                    using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                    {
                        await unitOfWork.TrolleyCells.AddPalletToTrolleyCellAsync(
                            CurrentTrolley.Id, 
                            EM.Maman.Common.Constants.TrolleyConstants.RightCellPosition, 
                            (int)pallet.Id);
                        await unitOfWork.CompleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    System.Diagnostics.Debug.WriteLine($"Error persisting pallet to right trolley cell: {ex.Message}");
                    MessageBox.Show($"Error saving trolley state: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Method to remove a pallet from the trolley's left cell
        public async System.Threading.Tasks.Task<Pallet> RemovePalletFromTrolleyLeftCellAsync()
        {
            if (TrolleyVM != null)
            {
                // Get the pallet before removing it
                var pallet = TrolleyVM.LeftCell.Pallet;
                
                // Update UI
                TrolleyVM.RemovePalletFromLeftCell();
                
                // Persist to database
                if (pallet != null)
                {
                    try
                    {
                        using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                        {
                            await unitOfWork.TrolleyCells.RemovePalletFromTrolleyCellAsync(
                                CurrentTrolley.Id, 
                                EM.Maman.Common.Constants.TrolleyConstants.LeftCellPosition);
                            await unitOfWork.CompleteAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        System.Diagnostics.Debug.WriteLine($"Error removing pallet from left trolley cell: {ex.Message}");
                        MessageBox.Show($"Error saving trolley state: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                
                return pallet;
            }
            return null;
        }

        // Method to remove a pallet from the trolley's right cell
        public async System.Threading.Tasks.Task<Pallet> RemovePalletFromTrolleyRightCellAsync()
        {
            if (TrolleyVM != null)
            {
                // Get the pallet before removing it
                var pallet = TrolleyVM.RightCell.Pallet;
                
                // Update UI
                TrolleyVM.RemovePalletFromRightCell();
                
                // Persist to database
                if (pallet != null)
                {
                    try
                    {
                        using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                        {
                            await unitOfWork.TrolleyCells.RemovePalletFromTrolleyCellAsync(
                                CurrentTrolley.Id, 
                                EM.Maman.Common.Constants.TrolleyConstants.RightCellPosition);
                            await unitOfWork.CompleteAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        System.Diagnostics.Debug.WriteLine($"Error removing pallet from right trolley cell: {ex.Message}");
                        MessageBox.Show($"Error saving trolley state: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                
                return pallet;
            }
            return null;
        }
        
        // Non-async versions for backward compatibility
        public void AddPalletToTrolleyLeftCell(Pallet pallet)
        {
            // Call the async version without awaiting
            _ = AddPalletToTrolleyLeftCellAsync(pallet);
        }
        
        public void AddPalletToTrolleyRightCell(Pallet pallet)
        {
            // Call the async version without awaiting
            _ = AddPalletToTrolleyRightCellAsync(pallet);
        }
        
        public Pallet RemovePalletFromTrolleyLeftCell()
        {
            // Call the async version and wait for the result
            var task = RemovePalletFromTrolleyLeftCellAsync();
            task.Wait();
            return task.Result;
        }
        
        public Pallet RemovePalletFromTrolleyRightCell()
        {
            // Call the async version and wait for the result
            var task = RemovePalletFromTrolleyRightCellAsync();
            task.Wait();
            return task.Result;
        }

        // Helper method to get the DB Cell entity
        private async Task<Cell> GetDbCellAsync(int trolleyRowPosition, int storageLevel, int sideNumeric, int depthIndex)
        {
            _logger.LogInformation($"GetDbCellAsync called with TrolleyRowPosition: {trolleyRowPosition}, StorageLevel: {storageLevel}, SideNumeric: {sideNumeric}, DepthIndex: {depthIndex}");
            using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
            {
                // Assuming:
                // 'storageLevel' matches Cell.Level (as confirmed)
                // 'sideNumeric' convention: 2 for Left, 1 for Right (as confirmed)
                // 'depthIndex' matches Cell.Depth
                // 'trolleyRowPosition' matches Cell.Position
                var cell = (await uow.Cells.FindAsync(c => 
                                c.Position == trolleyRowPosition &&
                                c.Level == storageLevel && 
                                c.Side == sideNumeric &&         
                                c.Order == depthIndex))
                           .FirstOrDefault();
                
                if (cell == null)
                {
                    _logger.LogWarning($"Database Cell entity not found for TrolleyRowPosition: {trolleyRowPosition}, StorageLevel: {storageLevel}, SideNumeric: {sideNumeric}, DepthIndex: {depthIndex}");
                }
                else
                {
                    _logger.LogInformation($"Found DB Cell ID: {cell.Id} for TrolleyRowPosition: {trolleyRowPosition}, StorageLevel: {storageLevel}, SideNumeric: {sideNumeric}, DepthIndex: {depthIndex}");
                }
                return cell;
            }
        }

        // Helper method to get the current composite row based on trolley position
        private CompositeRow GetCurrentRow()
        {
            if (TrolleyVM?.Rows == null || CurrentTrolley == null)
                return null;

            return TrolleyVM.Rows.FirstOrDefault(row => row.Position == CurrentTrolley.Position);
        }

        // Helper method to update a warehouse cell's pallet
        private void UpdateWarehouseCellPallet(int position, bool isLeftSide, int cellIndex, Pallet pallet)
        {
            // Find the correct row
            var row = TrolleyVM.Rows.FirstOrDefault(r => r.Position == position);
            if (row == null) return;

            // Update the appropriate cell pallet property
            if (isLeftSide)
            {
                switch (cellIndex)
                {
                    case 1:
                        row.LeftCell1Pallet = pallet;
                        break;
                    case 2:
                        row.LeftCell2Pallet = pallet;
                        break;
                    case 3:
                        row.LeftCell3Pallet = pallet;
                        break;
                    case 4:
                        row.LeftCell4Pallet = pallet;
                        break;
                }
            }
            else // Right side
            {
                switch (cellIndex)
                {
                    case 1:
                        row.RightCell1Pallet = pallet;
                        break;
                    case 2:
                        row.RightCell2Pallet = pallet;
                        break;
                    case 3:
                        row.RightCell3Pallet = pallet;
                        break;
                    case 4:
                        row.RightCell4Pallet = pallet;
                        break;
                }
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
        // Renamed to reflect it's for loading from a warehouse cell, takes depthIndex
        public async System.Threading.Tasks.Task LoadPalletFromWarehouseLeftCellAsync(int depthIndexToIgnore = 0) // depthIndexToIgnore is no longer primary driver
        {
            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                _logger.LogWarning($"Operation cancelled: Trolley level mismatch. Current: {TrolleyVM.CurrentLevelNumber}, Selected: {TrolleyVM.SelectedLevelNumber}.");
                MessageBox.Show($"Operation cancelled: Trolley is at Level {TrolleyVM.CurrentLevelNumber} but you are viewing Level {TrolleyVM.SelectedLevelNumber}. " +
                                $"The view will now switch to Level {TrolleyVM.CurrentLevelNumber}. Please try the operation again if you wish.",
                                "Level Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                TrolleyVM.SelectedLevelNumber = TrolleyVM.CurrentLevelNumber; // Switch view to current level
                return; // Stop the operation
            }

            // Pre-loading: If trolley's left cell is occupied, try to move its pallet to the right cell if empty
            if (TrolleyVM.LeftCell.IsOccupied && !TrolleyVM.RightCell.IsOccupied)
            {
                Pallet palletToMove = await RemovePalletFromTrolleyLeftCellAsync();
                if (palletToMove != null)
                {
                    await AddPalletToTrolleyRightCellAsync(palletToMove);
                    _logger.LogInformation($"Moved pallet {palletToMove.DisplayName} from trolley left cell to right cell to make space for loading.");
                    MessageBox.Show($"Moved pallet {palletToMove.DisplayName} from trolley left to right cell.");
                }
            }

            // Finger handling for lowest level
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            if (isLowestLevel && currentRow.LeftFinger != null)
            {
                if (currentRow.LeftFingerPalletCount > 0)
                {
                    if (TrolleyVM.LeftCell.IsOccupied) // Should only happen if right cell was also occupied
                    {
                        MessageBox.Show("Left trolley cell is still occupied. Both trolley cells might be full. Please unload one first.");
                        return;
                    }
                    var random = new Random();
                    int palletId = random.Next(1000, 10000);
                    var pallet = new Pallet { Id = palletId, DisplayName = $"F-{palletId}", UldType = "AKE", UldCode = $"AKE{palletId}LF" };
                    await AddPalletToTrolleyLeftCellAsync(pallet);
                    currentRow.LeftFingerPalletCount--;
                    MessageBox.Show($"Loaded pallet {pallet.DisplayName} from left finger. Remaining pallets: {currentRow.LeftFingerPalletCount}");
                    return;
                }
                else
                {
                    MessageBox.Show("No pallets available on the left finger.");
                    return;
                }
            }

            // Regular cell handling: Find pallet from warehouse cell (smallest order)
            (Pallet warehousePallet, int warehousePalletOrder) = await GetPalletFromWarehouseCellBySmallestOrderAsync(currentRow, true);

            if (warehousePallet != null)
            {
                string palletDisplay = warehousePallet.DisplayName;
                _logger.LogInformation($"Attempting to load pallet {palletDisplay} (Order: {warehousePalletOrder}) from left warehouse cell.");

                if (TrolleyVM.LeftCell.IsOccupied) // Both trolley cells must be occupied now
                {
                    Pallet currentLeftTrolleyPallet = TrolleyVM.LeftCell.Pallet;
                    _logger.LogInformation($"Swapping: Trolley left pallet {currentLeftTrolleyPallet.DisplayName} with warehouse pallet {palletDisplay}.");
                    MessageBox.Show($"Swapping: Pallet {currentLeftTrolleyPallet.DisplayName} (trolley left) with {palletDisplay} (warehouse cell order {warehousePalletOrder})");

                    await RemovePalletFromTrolleyLeftCellAsync(); // Removes currentLeftTrolleyPallet from trolley DB
                    await AddPalletToTrolleyLeftCellAsync(warehousePallet); // Adds warehousePallet to trolley DB

                    // Update UI for warehouse cell: currentLeftTrolleyPallet now in warehouse
                    UpdateWarehouseCellPallet(currentRow.Position, true, warehousePalletOrder + 1, currentLeftTrolleyPallet);
                    // DB: Remove warehousePallet from its original PalletInCell
                    await UpdatePalletInCellDbAsync(warehousePallet, currentRow, true, warehousePalletOrder, false);
                    // DB: Add currentLeftTrolleyPallet to PalletInCell at the emptied warehousePalletOrder
                    await UpdatePalletInCellDbAsync(currentLeftTrolleyPallet, currentRow, true, warehousePalletOrder, true);
                    // No shift needed in warehouse cell as the spot was filled by swap
                }
                else // Trolley left cell is empty, direct load
                {
                    _logger.LogInformation($"Loading: Warehouse pallet {palletDisplay} (Order: {warehousePalletOrder}) to empty trolley left cell.");
                    MessageBox.Show($"Loading: Pallet {palletDisplay} from left warehouse cell (order {warehousePalletOrder}) to trolley");

                    await AddPalletToTrolleyLeftCellAsync(warehousePallet);
                    // Update UI for warehouse cell (now empty at that order)
                    UpdateWarehouseCellPallet(currentRow.Position, true, warehousePalletOrder + 1, null);
                    // DB: Remove warehousePallet from its original PalletInCell
                    await UpdatePalletInCellDbAsync(warehousePallet, currentRow, true, warehousePalletOrder, false);
                    // DB: Shift remaining pallets in the warehouse cell forward
                    await ShiftPalletsForwardInWarehouseCellAsync(currentRow, true, warehousePalletOrder);
                }
            }
            else
            {
                _logger.LogInformation("No pallet found in any left warehouse cell to load.");
                MessageBox.Show("No pallet in any left warehouse cell to load.");
            }
        }

        public async System.Threading.Tasks.Task LoadPalletFromWarehouseRightCellAsync(int depthIndexToIgnore = 0) // depthIndexToIgnore is no longer primary driver
        {
            CompositeRow currentRow = GetCurrentRow();
            if (currentRow == null)
            {
                MessageBox.Show("Trolley is not at a valid position");
                return;
            }

            if (TrolleyVM.CurrentLevelNumber != TrolleyVM.SelectedLevelNumber)
            {
                _logger.LogWarning($"Operation cancelled: Trolley level mismatch. Current: {TrolleyVM.CurrentLevelNumber}, Selected: {TrolleyVM.SelectedLevelNumber}.");
                MessageBox.Show($"Operation cancelled: Trolley is at Level {TrolleyVM.CurrentLevelNumber} but you are viewing Level {TrolleyVM.SelectedLevelNumber}. " +
                                $"The view will now switch to Level {TrolleyVM.CurrentLevelNumber}. Please try the operation again if you wish.",
                                "Level Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                TrolleyVM.SelectedLevelNumber = TrolleyVM.CurrentLevelNumber; // Switch view to current level
                return; // Stop the operation
            }

            // Pre-loading: If trolley's right cell is occupied, try to move its pallet to the left cell if empty
            if (TrolleyVM.RightCell.IsOccupied && !TrolleyVM.LeftCell.IsOccupied)
            {
                Pallet palletToMove = await RemovePalletFromTrolleyRightCellAsync();
                if (palletToMove != null)
                {
                    await AddPalletToTrolleyLeftCellAsync(palletToMove);
                    _logger.LogInformation($"Moved pallet {palletToMove.DisplayName} from trolley right cell to left cell to make space for loading.");
                    MessageBox.Show($"Moved pallet {palletToMove.DisplayName} from trolley right to left cell.");
                }
            }
            
            // Finger handling for lowest level
            bool isLowestLevel = TrolleyVM.CurrentLevelNumber == 1;
            if (isLowestLevel && currentRow.RightFinger != null)
            {
                if (currentRow.RightFingerPalletCount > 0)
                {
                    if (TrolleyVM.RightCell.IsOccupied) // Should only happen if left cell was also occupied
                    {
                        MessageBox.Show("Right trolley cell is still occupied. Both trolley cells might be full. Please unload one first.");
                        return;
                    }
                    var random = new Random();
                    int palletId = random.Next(1000, 10000);
                    var pallet = new Pallet { Id = palletId, DisplayName = $"F-{palletId}", UldType = "PAG", UldCode = $"PAG{palletId}RF" };
                    await AddPalletToTrolleyRightCellAsync(pallet);
                    currentRow.RightFingerPalletCount--;
                    MessageBox.Show($"Loaded pallet {pallet.DisplayName} from right finger. Remaining pallets: {currentRow.RightFingerPalletCount}");
                    return;
                }
                else
                {
                    MessageBox.Show("No pallets available on the right finger.");
                    return;
                }
            }

            // Regular cell handling: Find pallet from warehouse cell (smallest order)
            (Pallet warehousePallet, int warehousePalletOrder) = await GetPalletFromWarehouseCellBySmallestOrderAsync(currentRow, false);

            if (warehousePallet != null)
            {
                string palletDisplay = warehousePallet.DisplayName;
                _logger.LogInformation($"Attempting to load pallet {palletDisplay} (Order: {warehousePalletOrder}) from right warehouse cell.");

                if (TrolleyVM.RightCell.IsOccupied) // Both trolley cells must be occupied now
                {
                    Pallet currentRightTrolleyPallet = TrolleyVM.RightCell.Pallet;
                    _logger.LogInformation($"Swapping: Trolley right pallet {currentRightTrolleyPallet.DisplayName} with warehouse pallet {palletDisplay}.");
                    MessageBox.Show($"Swapping: Pallet {currentRightTrolleyPallet.DisplayName} (trolley right) with {palletDisplay} (warehouse cell order {warehousePalletOrder})");
                    
                    await RemovePalletFromTrolleyRightCellAsync(); // Removes currentRightTrolleyPallet from trolley DB
                    await AddPalletToTrolleyRightCellAsync(warehousePallet); // Adds warehousePallet to trolley DB

                    // Update UI for warehouse cell: currentRightTrolleyPallet now in warehouse
                    UpdateWarehouseCellPallet(currentRow.Position, false, warehousePalletOrder + 1, currentRightTrolleyPallet);
                    // DB: Remove warehousePallet from its original PalletInCell
                    await UpdatePalletInCellDbAsync(warehousePallet, currentRow, false, warehousePalletOrder, false);
                    // DB: Add currentRightTrolleyPallet to PalletInCell at the emptied warehousePalletOrder
                    await UpdatePalletInCellDbAsync(currentRightTrolleyPallet, currentRow, false, warehousePalletOrder, true);
                    // No shift needed in warehouse cell as the spot was filled by swap
                }
                else // Trolley right cell is empty, direct load
                {
                    _logger.LogInformation($"Loading: Warehouse pallet {palletDisplay} (Order: {warehousePalletOrder}) to empty trolley right cell.");
                    MessageBox.Show($"Loading: Pallet {palletDisplay} from right warehouse cell (order {warehousePalletOrder}) to trolley");

                    await AddPalletToTrolleyRightCellAsync(warehousePallet);
                    // Update UI for warehouse cell (now empty at that order)
                    UpdateWarehouseCellPallet(currentRow.Position, false, warehousePalletOrder + 1, null);
                    // DB: Remove warehousePallet from its original PalletInCell
                    await UpdatePalletInCellDbAsync(warehousePallet, currentRow, false, warehousePalletOrder, false);
                    // DB: Shift remaining pallets in the warehouse cell forward
                    await ShiftPalletsForwardInWarehouseCellAsync(currentRow, false, warehousePalletOrder);
                }
            }
            else
            {
                _logger.LogInformation("No pallet found in any right warehouse cell to load.");
                MessageBox.Show("No pallet in any right warehouse cell to load.");
            }
        }

        // Helper method to get a pallet from the warehouse cell location with the smallest order.
        // Returns the pallet and its 0-based order.
        private async Task<(Pallet Pallet, int Order)> GetPalletFromWarehouseCellBySmallestOrderAsync(CompositeRow currentRow, bool isLeft)
        {
            if (currentRow == null) return (null, -1);

            int sideNumeric = isLeft ? 2 : 1; // 2 for Left, 1 for Right

            try
            {
                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Find all cells for this physical location (Position, Level, Side)
                    var cellsInLocation = (await uow.Cells.FindAsync(c =>
                                            c.Position == currentRow.Position &&
                                            c.Level == TrolleyVM.CurrentLevelNumber &&
                                            c.Side == sideNumeric))
                                         .OrderBy(c => c.Order)
                                         .ToList();

                    if (!cellsInLocation.Any())
                    {
                        _logger.LogWarning($"No cells defined for location: Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}");
                        return (null, -1);
                    }

                    // Check each cell by increasing order for a pallet
                    foreach (var cell in cellsInLocation)
                    {
                        var palletInCellEntry = (await uow.PalletInCells.FindAsync(pic => pic.CellId == cell.Id)).FirstOrDefault();
                        if (palletInCellEntry != null && palletInCellEntry.PalletId.HasValue)
                        {
                            var pallet = await uow.Pallets.GetByIdAsync(palletInCellEntry.PalletId.Value);
                            if (pallet != null)
                            {
                                _logger.LogInformation($"Found pallet {pallet.DisplayName} in cell {cell.DisplayName} (Order: {cell.Order}) for location Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}");
                                return (pallet, cell.Order ?? -1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetPalletFromWarehouseCellBySmallestOrderAsync for Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}");
                MessageBox.Show($"Database error finding pallet: {ex.Message}", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            _logger.LogInformation($"No pallet found in any cell for location: Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}");
            return (null, -1);
        }

        // Helper method to shift pallets forward in a warehouse cell location after one is removed.
        private async System.Threading.Tasks.Task ShiftPalletsForwardInWarehouseCellAsync(CompositeRow currentRow, bool isLeft, int emptiedOrder)
        {
            if (currentRow == null) return;
            _logger.LogInformation($"Shifting pallets forward in warehouse cell: Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {(isLeft ? "Left" : "Right")}, from emptied order {emptiedOrder}.");

            int sideNumeric = isLeft ? 2 : 1;

            try
            {
                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    // Get all cells for this physical location, ordered by their current order
                    var cellsInPhysicalLocation = (await uow.Cells.FindAsync(c =>
                                                    c.Position == currentRow.Position &&
                                                    c.Level == TrolleyVM.CurrentLevelNumber &&
                                                    c.Side == sideNumeric))
                                                 .OrderBy(c => c.Order)
                                                 .ToList();

                    if (!cellsInPhysicalLocation.Any()) return;

                    // Iterate from the order immediately after the emptied one
                    for (int currentOrder = emptiedOrder + 1; currentOrder <= cellsInPhysicalLocation.Max(c => c.Order ?? -1); currentOrder++)
                    {
                        var cellToShiftFrom = cellsInPhysicalLocation.FirstOrDefault(c => c.Order == currentOrder);
                        var cellToShiftTo = cellsInPhysicalLocation.FirstOrDefault(c => c.Order == currentOrder - 1);

                        if (cellToShiftFrom != null && cellToShiftTo != null)
                        {
                            var palletInCellEntry = (await uow.PalletInCells.FindAsync(pic => pic.CellId == cellToShiftFrom.Id)).FirstOrDefault();
                            if (palletInCellEntry != null)
                            {
                                // Pallet exists in cellToShiftFrom, move it to cellToShiftTo
                                _logger.LogInformation($"Shifting pallet {palletInCellEntry.PalletId} from cell {cellToShiftFrom.Id} (Order {cellToShiftFrom.Order}) to cell {cellToShiftTo.Id} (Order {cellToShiftTo.Order}).");
                                palletInCellEntry.CellId = cellToShiftTo.Id; // Update the CellId to the new cell (lower order)
                                uow.PalletInCells.Update(palletInCellEntry); // Mark as updated
                                // The original cellToShiftFrom is now effectively empty for this pallet, 
                                // but another pallet might shift into it in the next iteration if depth allows.
                            }
                        }
                    }
                    await uow.CompleteAsync();
                    _logger.LogInformation($"Successfully shifted pallets forward for Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error shifting pallets forward for Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}.");
                MessageBox.Show($"Database error shifting pallets: {ex.Message}", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<int> GetMaxOrderForWarehouseCellAsync(CompositeRow currentRow, bool isLeft)
        {
            // Placeholder - Implementation will query DB for max order for the given physical cell
            // For now, returning a fixed value for testing, e.g., 3 (meaning orders 0, 1, 2, 3 exist)
            // This should be replaced with actual DB query.
            if (currentRow == null) return -1; // Or some other indicator of error/not found

            int sideNumeric = isLeft ? 2 : 1;
            using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
            {
                var cellsInLocation = await uow.Cells.FindAsync(c =>
                                        c.Position == currentRow.Position &&
                                        c.Level == TrolleyVM.CurrentLevelNumber &&
                                        c.Side == sideNumeric);
                if (cellsInLocation.Any())
                {
                    return cellsInLocation.Max(c => c.Order ?? -1);
                }
            }
            return -1; // Default if no cells found or no order defined
        }

        // Helper method to shift pallets deeper in a warehouse cell location when one is added to Order 0.
        private async System.Threading.Tasks.Task ShiftPalletsDeeperInWarehouseCellAsync(Pallet newPalletToStore, CompositeRow currentRow, bool isLeft)
        {
            if (currentRow == null || newPalletToStore == null) return;
            _logger.LogInformation($"Shifting pallets deeper in warehouse cell: Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {(isLeft ? "Left" : "Right")} to accommodate new pallet {newPalletToStore.DisplayName}.");

            int sideNumeric = isLeft ? 2 : 1;
            int maxOrder = await GetMaxOrderForWarehouseCellAsync(currentRow, isLeft);
            if (maxOrder < 0)
            {
                _logger.LogWarning($"Could not determine max order for cell Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}. Aborting shift.");
                // Potentially, this means the cell location isn't defined. If Order 0 is the only one, no shift is needed.
                // For a single-depth cell, maxOrder might be 0.
            }

            try
            {
                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var cellsInPhysicalLocation = (await uow.Cells.FindAsync(c =>
                                                    c.Position == currentRow.Position &&
                                                    c.Level == TrolleyVM.CurrentLevelNumber &&
                                                    c.Side == sideNumeric))
                                                 .OrderByDescending(c => c.Order) // Process from deepest to shallowest
                                                 .ToList();

                    if (!cellsInPhysicalLocation.Any()) return;

                    // Shift existing pallets to make space at Order 0
                    // Iterate from the second to deepest (maxOrder -1, if maxOrder is 0-based for deepest slot) down to Order 0
                    // Example: MaxOrder = 2 (slots 0,1,2). Iterate Order 1, then Order 0.
                    // Pallet at Order 1 moves to Order 2. Pallet at Order 0 moves to Order 1.
                    for (int currentOrder = maxOrder; currentOrder >= 0; currentOrder--)
                    {
                        var cellToShiftFrom = cellsInPhysicalLocation.FirstOrDefault(c => c.Order == currentOrder);
                        var cellToShiftTo = cellsInPhysicalLocation.FirstOrDefault(c => c.Order == currentOrder + 1);

                        if (cellToShiftFrom != null) // We are looking at a pallet in cell 'currentOrder'
                        {
                             var palletInCellEntry = (await uow.PalletInCells.FindAsync(pic => pic.CellId == cellToShiftFrom.Id)).FirstOrDefault();
                             if (palletInCellEntry != null) // If there's a pallet in cell 'currentOrder'
                             {
                                 if (cellToShiftTo != null) // And there is a deeper cell slot 'currentOrder + 1'
                                 {
                                     // Check if the deeper cell 'cellToShiftTo' is already occupied.
                                     var deeperCellOccupant = (await uow.PalletInCells.FindAsync(pic => pic.CellId == cellToShiftTo.Id)).FirstOrDefault();
                                     if (deeperCellOccupant != null)
                                     {
                                         _logger.LogError($"Critical error: Cell {cellToShiftTo.DisplayName} (Order {cellToShiftTo.Order}) is already occupied by pallet {deeperCellOccupant.PalletId} but trying to shift pallet {palletInCellEntry.PalletId} into it. Aborting shift for this pallet.");
                                         // This indicates a problem, perhaps maxOrder is wrong or data is inconsistent.
                                         // Depending on rules, might throw an exception or stop.
                                         continue; // Skip this pallet, or handle error more robustly
                                     }

                                     _logger.LogInformation($"Shifting pallet {palletInCellEntry.PalletId} from cell {cellToShiftFrom.Id} (Order {cellToShiftFrom.Order}) to cell {cellToShiftTo.Id} (Order {cellToShiftTo.Order}).");
                                     palletInCellEntry.CellId = cellToShiftTo.Id;
                                     uow.PalletInCells.Update(palletInCellEntry);
                                 }
                                 else
                                 {
                                     // This pallet is at the maxOrder and no deeper cell exists.
                                     // This situation implies the warehouse cell is full and this pallet would be "pushed out" or operation fails.
                                     // For now, we assume this means an error or the cell is truly full.
                                     _logger.LogWarning($"Pallet {palletInCellEntry.PalletId} at max order {currentOrder} in cell {cellToShiftFrom.DisplayName}. No deeper cell to shift to. This might mean the cell is full.");
                                     // Potentially remove this pallet or flag an error if it cannot be shifted.
                                     // For this implementation, we'll assume an error if a pallet needs to shift beyond maxOrder.
                                     // This case should ideally be caught before starting the shift by checking capacity.
                                 }
                             }
                        }
                    }
                    
                    // After shifting, place the new pallet into Order 0
                    var cellForNewPallet = cellsInPhysicalLocation.FirstOrDefault(c => c.Order == 0);
                    if (cellForNewPallet != null)
                    {
                        // Ensure Order 0 is actually free now
                        var orderZeroOccupant = (await uow.PalletInCells.FindAsync(pic => pic.CellId == cellForNewPallet.Id)).FirstOrDefault();
                        if (orderZeroOccupant != null)
                        {
                             _logger.LogError($"Critical error: Cell {cellForNewPallet.DisplayName} (Order 0) is still occupied by pallet {orderZeroOccupant.PalletId} after shift. Cannot place new pallet {newPalletToStore.DisplayName}.");
                             MessageBox.Show("Error: Could not free up Order 0 cell for the new pallet.", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            await uow.PalletInCells.AddAsync(new PalletInCell { PalletId = newPalletToStore.Id, CellId = cellForNewPallet.Id, StorageDate = DateTime.Now });
                            _logger.LogInformation($"Placed new pallet {newPalletToStore.DisplayName} into cell {cellForNewPallet.DisplayName} (Order 0).");
                        }
                    }
                    else
                    {
                        _logger.LogError($"Critical error: No cell found for Order 0 at Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}. Cannot place new pallet.");
                        MessageBox.Show("Error: No cell definition for Order 0.", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    await uow.CompleteAsync();
                    _logger.LogInformation($"Successfully shifted pallets deeper and placed new pallet for Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error shifting pallets deeper for Pos {currentRow.Position}, Lvl {TrolleyVM.CurrentLevelNumber}, Side {sideNumeric}.");
                MessageBox.Show($"Database error shifting pallets deeper: {ex.Message}", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Helper method to Add or Remove PalletInCell record
        private async System.Threading.Tasks.Task UpdatePalletInCellDbAsync(Pallet pallet, CompositeRow currentRow, bool isLeft, int order, bool isAdding) // Changed depthIndex to order
        {
            if (pallet == null)
            {
                _logger.LogWarning("UpdatePalletInCellDbAsync called with null pallet.");
                return;
            }
            if (currentRow == null)
            {
                _logger.LogWarning($"UpdatePalletInCellDbAsync called with null currentRow for pallet {pallet.Id}.");
                return;
            }

            int sideNumeric = isLeft ? 2 : 1; // 2 for Left, 1 for Right
            _logger.LogInformation($"UpdatePalletInCellDbAsync called for Pallet ID: {pallet.Id}, Row: {currentRow.Position}, Level: {TrolleyVM.CurrentLevelNumber}, Side: {(isLeft ? "Left" : "Right")} (Numeric: {sideNumeric}), Order: {order}, Action: {(isAdding ? "Adding" : "Removing")}");

            Cell dbCell = await GetDbCellAsync(currentRow.Position, TrolleyVM.CurrentLevelNumber, sideNumeric, order);

            if (dbCell == null)
            {
                _logger.LogWarning($"Cannot update PalletInCell: DB Cell not found for Pallet ID: {pallet.Id}, Row: {currentRow.Position}, Level: {TrolleyVM.CurrentLevelNumber}, Side: {sideNumeric}, Order: {order}.");
                return;
            }
            _logger.LogInformation($"Found DB Cell ID: {dbCell.Id} for Pallet ID: {pallet.Id}");

            try
            {
                using (var uow = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    if (isAdding)
                    {
                        var existingEntry = (await uow.PalletInCells.FindAsync(pic => pic.CellId == dbCell.Id)).FirstOrDefault();
                        if (existingEntry != null)
                        {
                            _logger.LogWarning($"Cell {dbCell.Id} (Row: {currentRow.Position}, Level: {TrolleyVM.CurrentLevelNumber}, Side: {sideNumeric}, Order: {order}) is already associated with Pallet ID {existingEntry.PalletId} in PalletInCell table. Current operation is for Pallet ID {pallet.Id}. Removing existing entry before adding new one.");
                            uow.PalletInCells.Remove(existingEntry);
                            // Consider if an immediate CompleteAsync is needed here if your DB/ORM requires it before another operation on the same entity/table.
                            // await uow.CompleteAsync(); // If needed, but usually can be batched.
                        }
                        var palletInCell = new PalletInCell
                        {
                            PalletId = pallet.Id,
                            CellId = dbCell.Id,
                            StorageDate = DateTime.Now
                        };
                        await uow.PalletInCells.AddAsync(palletInCell);
                        _logger.LogInformation($"Attempting to add PalletInCell: Pallet ID {pallet.Id} to Cell ID {dbCell.Id}.");
                    }
                    else // Removing
                    {
                        // When removing, we need to find the entry for the specific pallet in that cell,
                        // or if the cell should only ever have one pallet, find by CellId.
                        // Current logic finds by CellId AND PalletId, which is correct if we only want to remove a *specific* pallet from a cell.
                        var palletInCellEntry = (await uow.PalletInCells.FindAsync(pic => pic.CellId == dbCell.Id && pic.PalletId == pallet.Id)).FirstOrDefault();
                        if (palletInCellEntry != null)
                        {
                            uow.PalletInCells.Remove(palletInCellEntry);
                            _logger.LogInformation($"Attempting to remove PalletInCell: Pallet ID {pallet.Id} from Cell ID {dbCell.Id}.");
                        }
                        else
                        {
                            _logger.LogWarning($"No PalletInCell record found for Pallet ID {pallet.Id} in Cell ID {dbCell.Id} to remove. The cell might be already empty or occupied by a different pallet.");
                        }
                    }
                    await uow.CompleteAsync();
                    _logger.LogInformation($"Successfully completed PalletInCell DB operation for Pallet ID: {pallet.Id}, Cell ID: {dbCell.Id}, Action: {(isAdding ? "Added" : "Removed")}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdatePalletInCellDbAsync for Pallet ID: {pallet.Id}, Cell ID: {dbCell?.Id}, Action: {(isAdding ? "Adding" : "Removing")}.");
                // Optionally, rethrow or handle more gracefully depending on application requirements.
                MessageBox.Show($"Database error updating pallet location: {ex.Message}", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Method to check if left cell can be unloaded
       // private bool CanUnloadLeftCell() => TrolleyVM?.LeftCell?.IsOccupied == true;

        private bool CanUnloadLeftCell() => true;

        // Navigation methods
        public void ExecuteGoToStorageLocation(object parameter)
        {
            if (_mainViewModel != null)
            {
                _mainViewModel.ExecuteGoToStorageLocation(parameter);
            }
        }

        public bool CanExecuteGoToStorageLocation(object parameter)
        {
            if (_mainViewModel != null)
            {
                return _mainViewModel.CanExecuteGoToStorageLocation(parameter);
            }
            return false;
        }

        public void ExecuteChangeDestination(object parameter)
        {
            if (_mainViewModel != null)
            {
                _mainViewModel.ExecuteChangeDestination(parameter);
            }
        }

        public bool CanExecuteChangeDestination(object parameter)
        {
            if (_mainViewModel != null)
            {
                return _mainViewModel.CanExecuteChangeDestination(parameter);
            }
            return false;
        }

        // Trolley movement methods
        public void TrolleyMethods_MoveTrolleyUp()
        {
            if (_mainViewModel != null)
            {
                _mainViewModel.TrolleyMethods_MoveTrolleyUp();
            }
        }

        public void TrolleyMethods_MoveTrolleyDown()
        {
            if (_mainViewModel != null)
            {
                _mainViewModel.TrolleyMethods_MoveTrolleyDown();
            }
        }

        // Method to check if right cell can be unloaded
        private bool CanUnloadRightCell() => TrolleyVM?.RightCell?.IsOccupied == true;
        
        // Method to unload the left cell - calls implementation in UnloadMethods.cs
        private async System.Threading.Tasks.Task TestUnloadLeftCellAsync() => await UnloadLeftCellImplementationAsync();
        
        // Method to unload the right cell - calls implementation in UnloadMethods.cs
        private async System.Threading.Tasks.Task TestUnloadRightCellAsync() => await UnloadRightCellImplementationAsync();
    }
}
