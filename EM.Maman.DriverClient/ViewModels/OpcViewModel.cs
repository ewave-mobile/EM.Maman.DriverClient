using EM.Maman.DriverClient.Services;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EM.Maman.DriverClient.ViewModels
{
    /// <summary>
    /// View model for OPC operations and register handling
    /// </summary>
    public class OpcViewModel : INotifyPropertyChanged
    {
        #region Fields and Properties

        private readonly IOpcService _opcService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger _logger;
        private double _plcRegisterValue;
        private int _positionPV;
        private string _status;
        private string _currentStatus;
        private RelayCommand _connectToOpcCommand;
        private RelayCommand _refreshRegistersCommand;
        private RelayCommand _writeRegisterCommand;
        private RelayCommand _subscribeRegisterCommand;

        /// <summary>
        /// Gets the collection of read-only registers
        /// </summary>
        public ObservableCollection<Register> ReadOnlyRegisters { get; } = new ObservableCollection<Register>();

        /// <summary>
        /// Gets the collection of writable registers
        /// </summary>
        public ObservableCollection<Register> WritableRegisters { get; } = new ObservableCollection<Register>();

        /// <summary>
        /// Gets or sets the current PLC register value
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current position PV value
        /// </summary>
        public int PositionPV
        {
            get => _positionPV;
            set
            {
                if (_positionPV != value)
                {
                    _positionPV = value;
                    OnPropertyChanged(nameof(PositionPV));
                    OnPropertyChanged(nameof(PvLevel));
                    OnPropertyChanged(nameof(PvLocation));

                    // Raise the position changed event
                    PositionChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets the current PV level
        /// </summary>
        public int PvLevel => PositionPV / 100;

        /// <summary>
        /// Gets or sets the current PV location
        /// </summary>
        public int PvLocation
        {
            get => PositionPV % 100;
            set
            {
                // Update the full position value
                PositionPV = (PvLevel * 100) + value;
            }
        }

        /// <summary>
        /// Gets or sets the current status
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current status
        /// </summary>
        public string CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (_currentStatus != value)
                {
                    _currentStatus = value;
                    OnPropertyChanged(nameof(CurrentStatus));
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to connect to the OPC server
        /// </summary>
        public ICommand ConnectToOpcCommand => _connectToOpcCommand ??= new RelayCommand(_ => ConnectToOpc(), _ => true);

        /// <summary>
        /// Command to refresh registers
        /// </summary>
        public ICommand RefreshRegistersCommand => _refreshRegistersCommand ??= new RelayCommand(async _ => await RefreshRegistersAsync(), _ => true);

        /// <summary>
        /// Command to write to a register
        /// </summary>
        public ICommand WriteRegisterCommand => _writeRegisterCommand ??= new RelayCommand(WriteRegister, CanWriteRegister);

        /// <summary>
        /// Command to subscribe to a register
        /// </summary>
        public ICommand SubscribeRegisterCommand => _subscribeRegisterCommand ??= new RelayCommand(_ => SubscribeRegister(ReadOnlyRegisters.FirstOrDefault(r => r.NodeId.Contains("Position_PV"))), _ => true);

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the position changes
        /// </summary>
        public event EventHandler<int> PositionChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the OpcViewModel class
        /// </summary>
        public OpcViewModel(IOpcService opcService, IDispatcherService dispatcherService, ILogger logger)
        {
            _opcService = opcService ?? throw new ArgumentNullException(nameof(opcService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to register changes
            _opcService.RegisterChanged += OpcService_RegisterChanged;

            // Initialize registers
            InitializeRegisters();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the registers
        /// </summary>
        private void InitializeRegisters()
        {
            // Clear existing registers
            ReadOnlyRegisters.Clear();
            WritableRegisters.Clear();

            // Add some test registers
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=Channel1.Device1.Position_PV", Value = "0", Name = "Position PV" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=Channel1.Device1.Status", Value = "0", Name = "Status" });
            ReadOnlyRegisters.Add(new Register { NodeId = "ns=2;s=Channel1.Device1.Error", Value = "0", Name = "Error" });

            WritableRegisters.Add(new Register { NodeId = "ns=2;s=Channel1.Device1.Position_SP", Value = "0", Name = "Position SP" });
            WritableRegisters.Add(new Register { NodeId = "ns=2;s=Channel1.Device1.Command", Value = "0", Name = "Command" });
        }

        /// <summary>
        /// Asynchronously connects to the OPC server, refreshes registers, and subscribes.
        /// </summary>
        public async void InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing OPC connection");
                
                // Await the OPC connection.
                await _opcService.ConnectAsync("opc.tcp://172.18.67.32:49320");

                // Once connected, refresh registers.
                await RefreshRegistersAsync();

                // And then subscribe to registers.
                SubscribeRegister(ReadOnlyRegisters.FirstOrDefault(r => r.NodeId.Contains("Position_PV")));
                
                _logger.LogInformation("OPC initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing OPC connection");
                Status = $"Error initializing OPC: {ex.Message}";
            }
        }

        /// <summary>
        /// Refreshes the registers
        /// </summary>
        private async Task RefreshRegistersAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing registers");
                
                // In a real application, this would fetch register values from the OPC server
                foreach (var register in ReadOnlyRegisters.Concat(WritableRegisters))
                {
                    var value = await _opcService.ReadRegisterAsync(register.NodeId);
                    register.Value = value?.ToString() ?? string.Empty;
                }
                
                Status = "Registers refreshed successfully";
                _logger.LogInformation("Registers refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing registers");
                Status = $"Error refreshing registers: {ex.Message}";
                MessageBox.Show($"Error refreshing registers: {ex.Message}");
            }
        }

        /// <summary>
        /// Connects to the OPC server
        /// </summary>
        private void ConnectToOpc()
        {
            try
            {
                _logger.LogInformation("Connecting to OPC server");
                
                // Connect to the OPC server asynchronously
                _opcService.ConnectAsync("opc.tcp://172.18.67.32:49320").ConfigureAwait(false);
                Status = "Connected to OPC server";
                
                _logger.LogInformation("Connected to OPC server successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to OPC server");
                Status = $"Error connecting to OPC server: {ex.Message}";
            }
        }

        /// <summary>
        /// Subscribes to a register
        /// </summary>
        private void SubscribeRegister(Register register)
        {
            if (register != null)
            {
                try
                {
                    _logger.LogInformation($"Subscribing to register: {register.Name}");
                    
                    // Subscribe to the register
                    _opcService.SubscribeToRegister(register.NodeId, updatedRegister => {
                        // Handle register updates
                    });
                    Status = $"Subscribed to {register.Name}";
                    
                    _logger.LogInformation($"Subscribed to register: {register.Name} successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error subscribing to register: {register?.Name}");
                    Status = $"Error subscribing to register: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Handles register changes from the OPC service
        /// </summary>
        private void OpcService_RegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            // Update the register value in the UI
            _dispatcherService.Invoke(() =>
            {
                var register = ReadOnlyRegisters.FirstOrDefault(r => r.NodeId == e.RegisterName);
                if (register != null)
                {
                    register.Value = e.NewValue?.ToString() ?? string.Empty;

                    // Special handling for Position_PV register
                    if (register.NodeId.Contains("Position_PV"))
                    {
                        if (int.TryParse(register.Value, out int positionValue))
                        {
                            PositionPV = positionValue;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Determines whether a register can be written to
        /// </summary>
        private bool CanWriteRegister(object parameter)
        {
            return true; // In a real application, add validation logic
        }

        /// <summary>
        /// Writes to a register
        /// </summary>
        private void WriteRegister(object parameter)
        {
            try
            {
                _logger.LogInformation($"Writing value {PlcRegisterValue} to Position SP");
                
                // Write to the register asynchronously
                _opcService.WriteRegisterAsync("ns=2;s=Channel1.Device1.Position_SP", PlcRegisterValue).ConfigureAwait(false);
                Status = $"Wrote value {PlcRegisterValue} to Position SP";
                
                _logger.LogInformation($"Wrote value {PlcRegisterValue} to Position SP successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to register");
                Status = $"Error writing to register: {ex.Message}";
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
