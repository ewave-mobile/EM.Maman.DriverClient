using EM.Maman.Common.Constants;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EM.Maman.Services.PlcServices
{
    /// <summary>
    /// Simulated OPC service that mimics PLC behavior for development and testing
    /// </summary>
    public class SimulatedOpcService : IOpcService, IDisposable
    {
        // Events from IOpcService interface
        public event EventHandler<RegisterChangedEventArgs> RegisterChanged;
        public event EventHandler<bool> ConnectivityChanged;

        // Connection state
        public bool IsConnected { get; private set; } = false;

        // Dictionary to store register values
        private readonly ConcurrentDictionary<string, object> _registers = new ConcurrentDictionary<string, object>();

        // Dictionary to track subscribed registers and their callbacks
        private readonly ConcurrentDictionary<string, Action<Register>> _subscriptions = new ConcurrentDictionary<string, Action<Register>>();

        // Dictionary to store control registers and their target registers
        private readonly ConcurrentDictionary<string, SimulatedMotion> _motionSimulations = new ConcurrentDictionary<string, SimulatedMotion>();

        // Simulation timer for automatic updates
        private Timer _simulationTimer;

        // Logger for diagnostic information
        private readonly ILogger<SimulatedOpcService> _logger;

        // Random for noise and variation
        private readonly Random _random = new Random();

        /// <summary>
        /// Constructor for the simulated OPC service
        /// </summary>
        /// <param name="logger">Logger for diagnostic information</param>
        public SimulatedOpcService(ILogger<SimulatedOpcService> logger)
        {
            _logger = logger;
            _logger.LogInformation("Initializing SimulatedOpcService");

            // Initialize with default values for common registers
            InitializeDefaultRegisters();
        }

        /// <summary>
        /// Sets up default register values for the simulation
        /// </summary>
        private void InitializeDefaultRegisters()
        {
            // System registers for watchdog and general status
            _registers[OpcNodes.Watchdog] = 0;
            _registers[OpcNodes.SystemStatus] = "Ready";
            _registers[OpcNodes.ErrorCode] = 0;

            // Maman specific registers based on OpcViewModel
            _registers[OpcNodes.PositionPV] = 0;
            _registers[OpcNodes.Status] = 0;
            _registers[OpcNodes.PositionRequest] = 0;
            _registers[OpcNodes.Control] = 0;

            // Configure motion simulation for the main position control
            ConfigureMotionSimulation();

            // Add any additional registers needed
            _registers[OpcNodes.SafetyCircuit] = true;
            _registers[OpcNodes.EmergencyStop] = false;
            _registers[OpcNodes.PowerOn] = true;
        }

        /// <summary>
        /// Configures a simulated motion axis with position, target, control and status registers
        /// </summary>
        private void ConfigureMotionAxis(string axisName, double initialPosition, double maxPosition)
        {
            string positionRegister = $"ns=2;s={axisName}.Position";
            string targetRegister = $"ns=2;s={axisName}.Target";
            string controlRegister = $"ns=2;s={axisName}.Control";
            string statusRegister = $"ns=2;s={axisName}.Status";
            string speedRegister = $"ns=2;s={axisName}.Speed";

            // Initialize register values
            _registers[positionRegister] = initialPosition;
            _registers[targetRegister] = initialPosition;
            _registers[controlRegister] = false;
            _registers[statusRegister] = "Idle";
            _registers[speedRegister] = 10.0; // Units per second

            // Create a motion simulation for this axis
            _motionSimulations[axisName] = new SimulatedMotion
            {
                AxisName = axisName,
                PositionRegister = positionRegister,
                TargetRegister = targetRegister,
                ControlRegister = controlRegister,
                StatusRegister = statusRegister,
                SpeedRegister = speedRegister,
                MaxPosition = maxPosition,
                MinPosition = 0
            };
        }

        /// <summary>
        /// Configures the specific motion simulation for Maman PLC
        /// </summary>
        private void ConfigureMotionSimulation()
        {
            // Define the node IDs based on your OpcViewModel
            string positionRegister = OpcNodes.PositionPV;
            string targetRegister = OpcNodes.PositionRequest;
            string controlRegister = OpcNodes.Control;
            string statusRegister = OpcNodes.Status;

            // Configure initial values
            double initialPosition = 0;
            double maxPosition = 10000; // Adjust as needed for your application

            // Create a custom speed register if needed
            string speedRegister = OpcNodes.Speed;
            _registers[speedRegister] = 20.0; // Units per second - adjust as needed

            // Create a motion simulation for the main position control
            _motionSimulations["Maman"] = new SimulatedMotion
            {
                AxisName = "Maman",
                PositionRegister = positionRegister,
                TargetRegister = targetRegister,
                ControlRegister = controlRegister,
                StatusRegister = statusRegister,
                SpeedRegister = speedRegister,
                MaxPosition = maxPosition,
                MinPosition = 0
            };

            _logger.LogInformation("Maman motion simulation configured successfully");
        }

        /// <summary>
        /// Simulates connecting to an OPC server
        /// </summary>
        public async Task ConnectAsync(string serverUrl)
        {
            _logger.LogInformation($"Simulating connection to OPC server: {serverUrl}");

            // Simulate connection delay
            await Task.Delay(1000);

            // Start the simulation timer to update registers periodically
            _simulationTimer = new Timer(UpdateSimulation, null, 0, 100); // Update every 100ms

            IsConnected = true;
            ConnectivityChanged?.Invoke(this, IsConnected);

            _logger.LogInformation("Simulated OPC connection established");
        }

        /// <summary>
        /// Simulates disconnecting from the OPC server
        /// </summary>
        public void Disconnect()
        {
            _logger.LogInformation("Disconnecting from simulated OPC server");

            // Stop the simulation timer
            _simulationTimer?.Dispose();
            _simulationTimer = null;

            IsConnected = false;
            ConnectivityChanged?.Invoke(this, IsConnected);

            _logger.LogInformation("Simulated OPC connection closed");
        }

        /// <summary>
        /// Reads a register value from the simulated PLC
        /// </summary>
        public async Task<object> ReadRegisterAsync(string nodeId)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Simulated OPC service is not connected");

            // Simulate a small delay for realistic behavior
            await Task.Delay(1 + _random.Next(6));

            if (_registers.TryGetValue(nodeId, out var value))
            {
                _logger.LogDebug($"Read register {nodeId}: {value}");
                return value;
            }

            // If register doesn't exist, create it with default value
            _logger.LogWarning($"Register {nodeId} not found in simulation, creating with default value");
            var defaultValue = 0;
            _registers[nodeId] = defaultValue;
            return defaultValue;
        }

        /// <summary>
        /// Reads multiple registers at once
        /// </summary>
        public async Task<List<Register>> ReadRegistersAsync(List<string> nodeIds)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Simulated OPC service is not connected");

            // Simulate a small delay
            await Task.Delay(1 + _random.Next(6));

            var registers = new List<Register>();

            foreach (var nodeId in nodeIds)
            {
                if (!_registers.TryGetValue(nodeId, out var value))
                {
                    // If register doesn't exist, create with default value
                    value = 0;
                    _registers[nodeId] = value;
                }

                registers.Add(new Register
                {
                    NodeId = nodeId,
                    Value = value?.ToString(),
                    IsSubscribed = _subscriptions.ContainsKey(nodeId)
                });
            }

            return registers;
        }

        /// <summary>
        /// Writes a value to a register in the simulated PLC
        /// </summary>
        public async Task WriteRegisterAsync(string nodeId, object value)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Simulated OPC service is not connected");

            // Simulate a small delay
            await Task.Delay(1 + _random.Next(6));

            // Update the register value
            _registers[nodeId] = value;
            _logger.LogDebug($"Wrote register {nodeId}: {value}");

            // If this is a control register for motion, it might trigger simulation changes
            foreach (var motion in _motionSimulations.Values)
            {
                if (nodeId == motion.ControlRegister)
                {
                    bool control = Convert.ToBoolean(value);
                    if (control)
                    {
                        // Control is activated, update status
                        _registers[motion.StatusRegister] = "Moving";
                        _logger.LogInformation($"Starting motion for axis {motion.AxisName}");

                        // Notify of the status change
                        NotifyRegisterChanged(motion.StatusRegister);
                    }
                }
                else if (nodeId == motion.TargetRegister)
                {
                    _logger.LogInformation($"Target position for axis {motion.AxisName} set to {value}");
                }
            }

            // Notify any subscribers about the register change
            NotifyRegisterChanged(nodeId);
        }

        /// <summary>
        /// Subscribes to changes in a register, providing callbacks when values change
        /// </summary>
        public void SubscribeToRegister(string nodeId, Action<Register> onValueChanged)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Simulated OPC service is not connected");

            _logger.LogInformation($"Subscribing to register {nodeId}");

            // Store the callback
            _subscriptions[nodeId] = onValueChanged;

            // If register doesn't exist, create it with default value
            if (!_registers.TryGetValue(nodeId, out var value))
            {
                value = 0;
                _registers[nodeId] = value;
            }

            // Initial notification with current value
            onValueChanged(new Register
            {
                NodeId = nodeId,
                Value = value?.ToString(),
                IsSubscribed = true
            });
        }

        /// <summary>
        /// Unsubscribes from a register
        /// </summary>
        public void Unsubscribe(string nodeId)
        {
            _logger.LogInformation($"Unsubscribing from register {nodeId}");
            _subscriptions.TryRemove(nodeId, out _);
        }

        /// <summary>
        /// Periodic simulation update function
        /// </summary>
        private void UpdateSimulation(object state)
        {
            try
            {
                // Increment watchdog counter for liveness
                if (_registers.TryGetValue("ns=2;s=Watchdog", out var watchdogValue))
                {
                    int watchdog = Convert.ToInt32(watchdogValue);
                    watchdog = (watchdog + 1) % 1000; // Cycle 0-999
                    _registers["ns=2;s=Watchdog"] = watchdog;
                    NotifyRegisterChanged("ns=2;s=Watchdog");
                }

                // Update all motion simulations
                foreach (var motion in _motionSimulations.Values)
                {
                    UpdateMotionSimulation(motion);
                }

                // Add any other simulation logic here
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simulation update");
            }
        }

        /// <summary>
        /// Updates a motion simulation based on current register values
        /// </summary>
        private void UpdateMotionSimulation(SimulatedMotion motion)
        {
            try
            {
                if (!_registers.TryGetValue(motion.ControlRegister, out var controlValue) ||
                    !_registers.TryGetValue(motion.PositionRegister, out var posValue) ||
                    !_registers.TryGetValue(motion.TargetRegister, out var targetValue) ||
                    !_registers.TryGetValue(motion.SpeedRegister, out var speedValue))
                {
                    return; // Missing required registers
                }

                bool control = Convert.ToBoolean(controlValue);
                double position = Convert.ToDouble(posValue);
                double target = Convert.ToDouble(targetValue);
                double speed = Convert.ToDouble(speedValue);

                // If control is active, move position toward target
                if (control)
                {
                    // Calculate how much to move in this update (100ms)
                    double step = speed * 0.1; // Speed units per second * 0.1 seconds

                    // Add small random variation for realism
                    step *= (1.0 + (_random.NextDouble() - 0.5) * 0.1); // ±5% variation

                    if (Math.Abs(target - position) <= step)
                    {
                        // We've reached the target
                        position = target;

                        // Update status based on motion.AxisName to handle different status values for different devices
                        if (motion.AxisName == "Maman")
                        {
                            _registers[motion.StatusRegister] = "Idle";
                        }
                        else
                        {
                            _registers[motion.StatusRegister] = "Idle";
                        }

                        // Turn control off once target is reached
                        _registers[motion.ControlRegister] = false;

                        _logger.LogInformation($"Axis {motion.AxisName} reached target position {target}");

                        // Notify of the status and control changes
                        NotifyRegisterChanged(motion.StatusRegister);
                        NotifyRegisterChanged(motion.ControlRegister);
                    }
                    else if (position < target)
                    {
                        // Move toward target (positive direction)
                        position += step;

                        // Update status based on motion.AxisName
                        if (motion.AxisName == "Maman")
                        {
                            _registers[motion.StatusRegister] = "Moving";
                        }
                        else
                        {
                            _registers[motion.StatusRegister] = "Moving+";
                        }
                    }
                    else
                    {
                        // Move toward target (negative direction)
                        position -= step;

                        // Update status based on motion.AxisName
                        if (motion.AxisName == "Maman")
                        {
                            _registers[motion.StatusRegister] = "Moving";
                        }
                        else
                        {
                            _registers[motion.StatusRegister] = "Moving-";
                        }
                    }

                    // Update position register with rounded value for integers if needed
                    if (motion.AxisName == "Maman")
                    {
                        // For Position_PV, we want integer values
                        _registers[motion.PositionRegister] = (int)Math.Round(position);
                    }
                    else
                    {
                        _registers[motion.PositionRegister] = position;
                    }

                    // Notify subscribers of position change
                    NotifyRegisterChanged(motion.PositionRegister);
                    NotifyRegisterChanged(motion.StatusRegister);

                    // For debugging
                    if (motion.AxisName == "Maman")
                    {
                        _logger.LogDebug($"Maman PV: {_registers[motion.PositionRegister]}, Target: {target}, Status: {_registers[motion.StatusRegister]}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating motion simulation for axis {motion.AxisName}");
            }
        }

        /// <summary>
        /// Notifies subscribers about register changes
        /// </summary>
        private void NotifyRegisterChanged(string nodeId)
        {
            if (_subscriptions.TryGetValue(nodeId, out var callback) &&
                _registers.TryGetValue(nodeId, out var value))
            {
                try
                {
                    // Invoke callback with current register value
                    callback(new Register
                    {
                        NodeId = nodeId,
                        Value = value?.ToString(),
                        IsSubscribed = true
                    });

                    // Also raise the general RegisterChanged event
                    RegisterChanged?.Invoke(this, new RegisterChangedEventArgs(nodeId, value));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in register changed callback for {nodeId}");
                }
            }
        }

        /// <summary>
        /// Disposes resources used by the simulation
        /// </summary>
        public void Dispose()
        {
            _simulationTimer?.Dispose();
            _logger.LogInformation("SimulatedOpcService disposed");
        }

        /// <summary>
        /// Internal class to track motion simulation parameters
        /// </summary>
        private class SimulatedMotion
        {
            public string AxisName { get; set; }
            public string PositionRegister { get; set; }
            public string TargetRegister { get; set; }
            public string ControlRegister { get; set; }
            public string StatusRegister { get; set; }
            public string SpeedRegister { get; set; }
            public double MaxPosition { get; set; }
            public double MinPosition { get; set; }
        }
    }
}