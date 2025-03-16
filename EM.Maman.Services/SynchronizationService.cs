using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Enums;
using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Services
{
    public class SynchronizationService : ISynchronizationService, IDisposable
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommandQueueService _commandQueueService;
        private readonly IConnectionManager _connectionManager;
        private readonly IDispatcherService _dispatcherService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SynchronizationService> _logger;

        private Timer _syncTimer;
        private bool _isSynchronizing;
        private bool _isDisposed;
        private int _pendingOperationsCount;
        private readonly object _syncLock = new object();

        public event EventHandler<SyncProgressEventArgs> SyncProgressChanged;

        public bool IsSynchronizing
        {
            get => _isSynchronizing;
            private set
            {
                if (_isSynchronizing != value)
                {
                    _isSynchronizing = value;
                    _dispatcherService.Invoke(() =>
                        OnSyncProgressChanged(0, _pendingOperationsCount));
                }
            }
        }

        public int PendingOperationsCount
        {
            get => _pendingOperationsCount;
            private set
            {
                if (_pendingOperationsCount != value)
                {
                    _pendingOperationsCount = value;
                    _dispatcherService.Invoke(() =>
                        OnSyncProgressChanged(0, _pendingOperationsCount));
                }
            }
        }

        public SynchronizationService(
            IUnitOfWork unitOfWork,
            ICommandQueueService commandQueueService,
            IConnectionManager connectionManager,
            IDispatcherService dispatcherService,
            IConfiguration configuration,
            ILogger<SynchronizationService> logger)
        {
            _unitOfWork = unitOfWork;
            _commandQueueService = commandQueueService;
            _connectionManager = connectionManager;
            _dispatcherService = dispatcherService;
            _configuration = configuration;
            _logger = logger;

            // Subscribe to connection state changes
            _connectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;

            // Initialize pending operations count
            System.Threading.Tasks.Task.Run(async () => {
                PendingOperationsCount = await GetPendingOperationsCountAsync();
            });
        }

        public async Task<bool> SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            // Don't try to sync if already in progress, offline mode is enabled, or no server connection
            if (IsSynchronizing || _connectionManager.IsOfflineModeEnabled || !_connectionManager.IsServerConnected)
            {
                return false;
            }

            lock (_syncLock)
            {
                // Double-check lock pattern
                if (IsSynchronizing) return false;
                IsSynchronizing = true;
            }

            try
            {
                _logger.LogInformation("Starting synchronization process");

                // Get all pending operations
                var pendingOperations = await _unitOfWork.Operations.GetPendingOperationsAsync();
                var totalOperations = pendingOperations.Count();

                PendingOperationsCount = totalOperations;

                if (totalOperations == 0)
                {
                    _logger.LogInformation("No pending operations to synchronize");
                    return true;
                }

                _logger.LogInformation($"Found {totalOperations} pending operations to synchronize");

                // Process each operation
                int processedCount = 0;
                foreach (var operation in pendingOperations)
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Synchronization was cancelled");
                        return false;
                    }

                    try
                    {
                        // Mark as in progress
                        if (await _commandQueueService.TryDequeueCommandAsync(operation))
                        {
                            // Process based on operation type
                            bool success = await ProcessOperationAsync(operation);

                            if (success)
                            {
                                await _commandQueueService.MarkOperationCompletedAsync(operation.Id);
                                processedCount++;

                                // Update progress
                                OnSyncProgressChanged(processedCount, totalOperations);
                            }
                            else
                            {
                                await _commandQueueService.MarkOperationFailedAsync(operation.Id,
                                    "Operation processing failed");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing operation {operation.Id}");
                        await _commandQueueService.MarkOperationFailedAsync(operation.Id, ex.Message);
                    }
                }

                _logger.LogInformation($"Synchronization completed. Processed {processedCount} of {totalOperations} operations");

                // Update count after sync
                PendingOperationsCount = await GetPendingOperationsCountAsync();

                return processedCount == totalOperations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Synchronization process failed");
                return false;
            }
            finally
            {
                IsSynchronizing = false;
            }
        }

        public async Task<int> GetPendingOperationsCountAsync()
        {
            return await _commandQueueService.GetPendingOperationsCountAsync();
        }

        public void StartAutomaticSync()
        {
            // Get sync interval from config (default to 5 minutes)
            int syncIntervalMinutes = _configuration.GetValue<int>("AppSettings:SyncIntervalMinutes", 5);

            // Create timer for automatic synchronization
            _syncTimer = new Timer(async _ =>
            {
                // Only perform sync if online and not in offline mode
                if (_connectionManager.IsServerConnected && !_connectionManager.IsOfflineModeEnabled)
                {
                    await SynchronizeAsync();
                }
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(syncIntervalMinutes));

            _logger.LogInformation($"Automatic synchronization started (interval: {syncIntervalMinutes} minutes)");
        }

        public void StopAutomaticSync()
        {
            _syncTimer?.Dispose();
            _syncTimer = null;
            _logger.LogInformation("Automatic synchronization stopped");
        }

        private async Task<bool> ProcessOperationAsync(PendingOperation operation)
        {
            _logger.LogInformation($"Processing operation: {operation.CommandType}, ID: {operation.Id}");

            switch (operation.CommandType)
            {
                case "MoveTrolley":
                    var moveTrolleyParams = await _commandQueueService.DeserializeParametersAsync<MoveTrolleyParameters>(operation.Parameters);
                    return await ProcessMoveTrolleyOperationAsync(moveTrolleyParams);

                case "UpdateCell":
                    var updateCellParams = await _commandQueueService.DeserializeParametersAsync<UpdateCellParameters>(operation.Parameters);
                    return await ProcessUpdateCellOperationAsync(updateCellParams);

                case "AddPallet":
                    var addPalletParams = await _commandQueueService.DeserializeParametersAsync<AddPalletParameters>(operation.Parameters);
                    return await ProcessAddPalletOperationAsync(addPalletParams);

                case "MovePallet":
                    var movePalletParams = await _commandQueueService.DeserializeParametersAsync<MovePalletParameters>(operation.Parameters);
                    return await ProcessMovePalletOperationAsync(movePalletParams);

                default:
                    _logger.LogWarning($"Unknown operation type: {operation.CommandType}");
                    return false;
            }
        }

        private async Task<bool> ProcessMoveTrolleyOperationAsync(MoveTrolleyParameters parameters)
        {
            try
            {
                _logger.LogInformation($"Processing MoveTrolley operation: TrolleyId={parameters.TrolleyId}, Position={parameters.Position}");

                // In a real implementation, this would make an API call to the server
                // For now, just simulate a delay
                await System.Threading.Tasks.Task.Delay(500);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MoveTrolley operation");
                return false;
            }
        }

        private async Task<bool> ProcessUpdateCellOperationAsync(UpdateCellParameters parameters)
        {
            try
            {
                _logger.LogInformation($"Processing UpdateCell operation: CellId={parameters.CellId}, IsBlocked={parameters.IsBlocked}");

                // In a real implementation, this would make an API call to the server
                // For now, just simulate a delay
                await System.Threading.Tasks.Task.Delay(500);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process UpdateCell operation");
                return false;
            }
        }

        private async Task<bool> ProcessAddPalletOperationAsync(AddPalletParameters parameters)
        {
            try
            {
                _logger.LogInformation($"Processing AddPallet operation: DisplayName={parameters.DisplayName}, UldCode={parameters.UldCode}");

                // In a real implementation, this would make an API call to the server
                // For now, just simulate a delay
                await System.Threading.Tasks.Task.Delay(500);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process AddPallet operation");
                return false;
            }
        }

        private async Task<bool> ProcessMovePalletOperationAsync(MovePalletParameters parameters)
        {
            try
            {
                _logger.LogInformation($"Processing MovePallet operation: PalletId={parameters.PalletId}, From={parameters.SourceCellId}, To={parameters.DestinationCellId}");

                // In a real implementation, this would make an API call to the server
                // For now, just simulate a delay
                await System.Threading.Tasks.Task.Delay(500);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MovePallet operation");
                return false;
            }
        }

        private void ConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.ConnectionType == ConnectionType.Server && e.IsConnected)
            {
                // Server is now connected, trigger sync if not in offline mode
                if (!_connectionManager.IsOfflineModeEnabled)
                {
                    _dispatcherService.Invoke(async () =>
                    {
                        await SynchronizeAsync();
                    });
                }
            }
        }

        private void OnSyncProgressChanged(int current, int total)
        {
            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(current, total));
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
                    StopAutomaticSync();

                    // Unsubscribe from events
                    if (_connectionManager != null)
                    {
                        _connectionManager.ConnectionStateChanged -= ConnectionManager_ConnectionStateChanged;
                    }
                }

                _isDisposed = true;
            }
        }
    }
}
