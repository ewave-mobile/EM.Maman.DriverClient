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
                    // Update the trolley's position to the modulo of PositionPV.
                    if (CurrentTrolley != null)
                    {
                        CurrentTrolley.Position = _positionPV % 100;
                        OnPropertyChanged(nameof(CurrentTrolley));
                    }
                    OnPropertyChanged(nameof(PositionPV));
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

        private void OpcService_RegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            if (e.RegisterName == "MyRegister")
            {
                if (double.TryParse(e.NewValue.ToString(), out double result))
                {
                    PlcRegisterValue = result;
                }
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

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
