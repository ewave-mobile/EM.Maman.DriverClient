using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Enums;
using EM.Maman.Models.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Services
{
    public class ConnectionManager : IConnectionManager, IDisposable
    {
        private readonly IOpcService _opcService;
        private readonly IConfiguration _configuration;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<ConnectionManager> _logger;

        private Timer _networkCheckTimer;
        private Timer _serverCheckTimer;
        private Timer _plcReconnectTimer;
        private bool _isDisposed;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        public bool IsPlcConnected { get; private set; }
        public bool IsServerConnected { get; private set; }
        public bool IsNetworkAvailable { get; private set; }
        public bool IsOfflineModeEnabled { get; private set; }

        public ConnectionManager(
            IOpcService opcService,
            IConfiguration configuration,
            IDispatcherService dispatcherService,
            ILogger<ConnectionManager> logger)
        {
            _opcService = opcService;
            _configuration = configuration;
            _dispatcherService = dispatcherService;
            _logger = logger;

            // Subscribe to OPC service connection events
            _opcService.ConnectivityChanged += OpcService_ConnectivityChanged;
            
            // Initialize from configuration
            IsOfflineModeEnabled = _configuration.GetValue<bool>("AppSettings:OfflineMode");
        }

        public async Task ConnectToPlcAsync()
        {
            try
            {
                _logger.LogInformation("Connecting to PLC...");
                string opcServerUrl = _configuration["OpcSettings:ServerUrl"];

                await _opcService.ConnectAsync(opcServerUrl);
                _logger.LogInformation("PLC connection established");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to PLC");
                RaiseConnectionStateChanged(ConnectionType.Plc, false, ex.Message);
                throw;
            }
        }

        public void DisconnectFromPlc()
        {
            try
            {
                _opcService.Disconnect();
                _logger.LogInformation("Disconnected from PLC");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PLC disconnection");
            }
        }

        public async Task CheckServerConnectionAsync()
        {
            try
            {
                // This is a placeholder for an actual server connection check
                // You would typically use your API client to make a simple call
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    string apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
                    var response = await httpClient.GetAsync($"{apiBaseUrl}health");

                    bool wasConnected = IsServerConnected;
                    IsServerConnected = response.IsSuccessStatusCode;

                    if (wasConnected != IsServerConnected)
                    {
                        RaiseConnectionStateChanged(ConnectionType.Server, IsServerConnected);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Server connection check failed");

                if (IsServerConnected)
                {
                    IsServerConnected = false;
                    RaiseConnectionStateChanged(ConnectionType.Server, false, ex.Message);
                }
            }
        }

        public async Task ToggleOfflineModeAsync(bool enable)
        {
            if (IsOfflineModeEnabled != enable)
            {
                IsOfflineModeEnabled = enable;
                _logger.LogInformation($"Offline mode {(enable ? "enabled" : "disabled")}");

                // Update configuration
                // Note: In a real application, you might want to persist this setting

                RaiseConnectionStateChanged(ConnectionType.OfflineMode, enable);

                // If disabling offline mode, try to reconnect
                if (!enable)
                {
                    await CheckNetworkAvailabilityAsync();
                    if (IsNetworkAvailable)
                    {
                        await ConnectToPlcAsync();
                        await CheckServerConnectionAsync();
                    }
                }
            }
        }

        public void StartConnectionMonitoring()
        {
            int networkCheckInterval = _configuration.GetValue<int>("AppSettings:NetworkCheckIntervalSeconds", 30);
            int serverCheckInterval = _configuration.GetValue<int>("AppSettings:ServerCheckIntervalSeconds", 60);
            int plcReconnectInterval = _configuration.GetValue<int>("OpcSettings:ReconnectIntervalSeconds", 60);

            // Timer for network availability checks
            _networkCheckTimer = new Timer(async _ => await CheckNetworkAvailabilityAsync(),
                null, TimeSpan.Zero, TimeSpan.FromSeconds(networkCheckInterval));

            // Timer for server connection checks
            _serverCheckTimer = new Timer(async _ => await CheckServerConnectionAsync(),
                null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(serverCheckInterval));

            // Timer for PLC reconnection attempts if disconnected
            _plcReconnectTimer = new Timer(async _ => await TryReconnectPlcAsync(),
                null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(plcReconnectInterval));

            _logger.LogInformation("Connection monitoring started");
        }

        public void StopConnectionMonitoring()
        {
            _networkCheckTimer?.Dispose();
            _serverCheckTimer?.Dispose();
            _plcReconnectTimer?.Dispose();

            _networkCheckTimer = null;
            _serverCheckTimer = null;
            _plcReconnectTimer = null;

            _logger.LogInformation("Connection monitoring stopped");
        }

        private async Task CheckNetworkAvailabilityAsync()
        {
            try
            {
                bool wasAvailable = IsNetworkAvailable;

                // Check if network interfaces are up
                IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();

                // Additional check: ping a reliable host
                if (IsNetworkAvailable)
                {
                    using (var ping = new Ping())
                    {
                        try
                        {
                            var reply = await ping.SendPingAsync("8.8.8.8", 1000);
                            IsNetworkAvailable = reply.Status == IPStatus.Success;
                        }
                        catch
                        {
                            IsNetworkAvailable = false;
                        }
                    }
                }

                if (wasAvailable != IsNetworkAvailable)
                {
                    _logger.LogInformation($"Network is now {(IsNetworkAvailable ? "available" : "unavailable")}");
                    RaiseConnectionStateChanged(ConnectionType.Network, IsNetworkAvailable);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking network availability");
                IsNetworkAvailable = false;
            }
        }

        private async Task TryReconnectPlcAsync()
        {
            // Only attempt reconnection if:
            // 1. We're not currently connected to the PLC
            // 2. Network is available
            // 3. Not in offline mode
            if (!IsPlcConnected && IsNetworkAvailable && !IsOfflineModeEnabled)
            {
                try
                {
                    _logger.LogInformation("Attempting to reconnect to PLC...");
                    await ConnectToPlcAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PLC reconnection attempt failed");
                }
            }
        }

        private void OpcService_ConnectivityChanged(object sender, bool isConnected)
        {
            _dispatcherService.Invoke(() =>
            {
                if (IsPlcConnected != isConnected)
                {
                    IsPlcConnected = isConnected;
                    _logger.LogInformation($"PLC connection state changed to: {(isConnected ? "Connected" : "Disconnected")}");
                    RaiseConnectionStateChanged(ConnectionType.Plc, isConnected);
                }
            });
        }

        private void RaiseConnectionStateChanged(ConnectionType connectionType, bool isConnected, string message = null)
        {
            _dispatcherService.Invoke(() =>
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(
                    connectionType, isConnected, message));
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    StopConnectionMonitoring();

                    // Unsubscribe from events
                    if (_opcService != null)
                    {
                        _opcService.ConnectivityChanged -= OpcService_ConnectivityChanged;
                    }
                }

                _isDisposed = true;
            }
        }
    }
}
