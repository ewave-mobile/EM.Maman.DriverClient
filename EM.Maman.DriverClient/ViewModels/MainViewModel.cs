﻿﻿﻿using EM.Maman.Models.Interfaces.Services;
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
using EM.Maman.DAL;
using EM.Maman.Services;
using Microsoft.Extensions.Logging;
using EM.Maman.Models.Interfaces;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// Main view model that coordinates between different parts of the application
    /// </summary>
    public partial class MainViewModel
    {
        #region Fields and Properties

        private bool _isWarehouseViewActive = true;
        private bool _isMapViewActive = false;
        private RelayCommand _showWarehouseViewCommand;
        private RelayCommand _showTasksViewCommand;
        private RelayCommand _showMapViewCommand;
        private RelayCommand _showTasksListViewCommand;
        private Trolley _currentTrolley;

        /// <summary>
        /// Gets or sets whether the warehouse view is currently active
        /// </summary>
        public bool IsWarehouseViewActive
        {
            get => _isWarehouseViewActive;
            set
            {
                if (_isWarehouseViewActive != value)
                {
                    _isWarehouseViewActive = value;
                    OnPropertyChanged(nameof(IsWarehouseViewActive));
                }
            }
        }
        
        /// <summary>
        /// Gets or sets whether the map view is currently active (vs tasks list view)
        /// </summary>
        public bool IsMapViewActive
        {
            get => _isMapViewActive;
            set
            {
                if (_isMapViewActive != value)
                {
                    _isMapViewActive = value;
                    OnPropertyChanged(nameof(IsMapViewActive));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current trolley
        /// </summary>
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

        /// <summary>
        /// Command to show the warehouse view
        /// </summary>
        public ICommand ShowWarehouseViewCommand => _showWarehouseViewCommand ??= new RelayCommand(_ => ShowWarehouseView(), _ => !IsWarehouseViewActive);

        /// <summary>
        /// Command to show the tasks view
        /// </summary>
        public ICommand ShowTasksViewCommand => _showTasksViewCommand ??= new RelayCommand(_ => ShowTasksView(), _ => IsWarehouseViewActive);
        
        /// <summary>
        /// Command to show the map view within the tasks view
        /// </summary>
        public ICommand ShowMapViewCommand => _showMapViewCommand ??= new RelayCommand(_ => ShowMapView(), _ => !IsMapViewActive && !IsWarehouseViewActive);

        /// <summary>
        /// Command to show the tasks list view within the tasks view
        /// </summary>
        public ICommand ShowTasksListViewCommand => _showTasksListViewCommand ??= new RelayCommand(_ => ShowTasksListView(), _ => IsMapViewActive && !IsWarehouseViewActive);

        /// <summary>
        /// View model for OPC operations
        /// </summary>
        public OpcViewModel OpcVM { get; }

        /// <summary>
        /// View model for trolley state and operations
        /// </summary>
        public TrolleyViewModel TrolleyVM { get; }

        /// <summary>
        /// View model for trolley operations
        /// </summary>
        public TrolleyOperationsViewModel TrolleyOperationsVM { get; }

        /// <summary>
        /// View model for warehouse operations
        /// </summary>
        public WarehouseViewModel WarehouseVM { get; }

        /// <summary>
        /// View model for task management
        /// </summary>
        public TaskViewModel TaskVM { get; }

        #endregion

        #region Commands

        public ICommand MoveTrolleyUpCommand { get; }
        public ICommand MoveTrolleyDownCommand { get; }
        public ICommand TestLoadLeftCellCommand { get; }
        public ICommand TestLoadRightCellCommand { get; }
        public ICommand TestUnloadLeftCellCommand { get; }
        public ICommand TestUnloadRightCellCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class
        /// </summary>
        public MainViewModel(
            IOpcService opcService,
            IUnitOfWork unitOfWork,
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            ILoggerFactory loggerFactory)
        {
            // Initialize trolley
            CurrentTrolley = new Trolley { Id = 1, DisplayName = "Main Trolley", Position = 1 };
            
            // Initialize view models
            TrolleyVM = new TrolleyViewModel();
            WarehouseVM = new WarehouseViewModel();
            
            // Create the OPC view model
            OpcVM = new OpcViewModel(
                opcService, 
                dispatcherService, 
                loggerFactory.CreateLogger<OpcViewModel>());
            
            // Subscribe to position changes from OPC
            OpcVM.PositionChanged += OnPositionChanged;

            // Initialize TrolleyOperationsViewModel
            TrolleyOperationsVM = new TrolleyOperationsViewModel(TrolleyVM, CurrentTrolley);

            // Initialize TaskViewModel with all required dependencies
            TaskVM = new TaskViewModel(
                unitOfWork,
                connectionManager,
                opcService,
                dispatcherService,
                loggerFactory.CreateLogger<TaskViewModel>()
            );

            // Initialize commands
            MoveTrolleyUpCommand = new RelayCommand(_ => TrolleyMethods_MoveTrolleyUp(), _ => CurrentTrolley?.Position > 0);
            MoveTrolleyDownCommand = new RelayCommand(_ => TrolleyMethods_MoveTrolleyDown(), _ => true);
            
            // Initialize test commands
            TestLoadLeftCellCommand = TrolleyOperationsVM.TestLoadLeftCellCommand;
            TestLoadRightCellCommand = TrolleyOperationsVM.TestLoadRightCellCommand;
            TestUnloadLeftCellCommand = TrolleyOperationsVM.TestUnloadLeftCellCommand;
            TestUnloadRightCellCommand = TrolleyOperationsVM.TestUnloadRightCellCommand;

            // Start the asynchronous initialization
            OpcVM.InitializeAsync();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles position changes from the OPC service
        /// </summary>
        private void OnPositionChanged(object sender, int positionValue)
        {
            // Extract level and position
            int level = positionValue / 100;
            int position = positionValue % 100;

            // Update the trolley's position
            if (CurrentTrolley != null)
            {
                CurrentTrolley.Position = position;
                OnPropertyChanged(nameof(CurrentTrolley));
            }

            // Update the TrolleyViewModel with level info
            if (TrolleyVM != null)
            {
                TrolleyVM.UpdateTrolleyPosition(level, position);
            }

            // Update the WarehouseViewModel with level info
            if (WarehouseVM != null)
            {
                WarehouseVM.CurrentLevelNumber = level;
            }
        }

        #endregion

        #region Trolley Methods

        /// <summary>
        /// Moves the trolley up
        /// </summary>
        private void TrolleyMethods_MoveTrolleyUp()
        {
            if (CurrentTrolley.Position > 0)
            {
                CurrentTrolley.Position--;
                OnPropertyChanged(nameof(CurrentTrolley));
                
                // Update commands can execute state
                (MoveTrolleyUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveTrolleyDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Moves the trolley down
        /// </summary>
        private void TrolleyMethods_MoveTrolleyDown()
        {
            if (CurrentTrolley.Position < 23)
            {
                CurrentTrolley.Position++;
                OnPropertyChanged(nameof(CurrentTrolley));
                
                // Update commands can execute state
                (MoveTrolleyUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveTrolleyDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Method to execute when a pick operation is initiated from the UI
        /// </summary>
        public void PickPallet(Cell sourceCell, Pallet pallet)
        {
            // Check if the trolley has an available cell
            if (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.RightCell.IsOccupied)
            {
                // No available cell, show message to user
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

        /// <summary>
        /// Method to execute when a put operation is initiated from the UI
        /// </summary>
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

        #endregion
    }
}
