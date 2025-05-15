using EM.Maman.DAL.Repositories;
using EM.Maman.Models.Interfaces;
using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EM.Maman.DAL
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly LocalMamanDBContext _context;
        private readonly IDbContextFactory<LocalMamanDBContext> _contextFactory;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction _transaction;
        private bool _disposed = false;
        private bool _ownsContext = false;

        // Lazy-loaded repositories
        private ITrolleyRepository _trolleyRepository;
        private ITrolleyCellRepository _trolleyCellRepository;
        private ICellRepository _cellRepository;
        private IFingerRepository _fingerRepository;
        private ILevelRepository _levelRepository;
        private ITaskRepository _taskRepository;
        private IOperationRepository _operationRepository;
        private IPalletRepository _palletRepository;
        private IPalletInCellRepository _palletInCellRepository;
        private IUserRepository _userRepository;
        private IConfigurationRepository _configurationRepository;
        private ITaskTypeRepository _taskTypeRepository;
        private ITraceLogRepository _traceLogRepository;

        // Constructor with IDbContextFactory (preferred)
        public UnitOfWork(IDbContextFactory<LocalMamanDBContext> contextFactory, ILogger<UnitOfWork> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = _contextFactory.CreateDbContext();
            _ownsContext = true;
            _logger.LogDebug("UnitOfWork created with new context from factory");
        }

        // Constructor with direct context injection (for backward compatibility)
        public UnitOfWork(LocalMamanDBContext context, ILogger<UnitOfWork> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownsContext = false;
            _logger.LogDebug("UnitOfWork created with injected context");
        }

        // Legacy constructor for backward compatibility - consider removing in future
        public UnitOfWork(
            LocalMamanDBContext context,
            ITrolleyRepository trolleyRepository,
            ICellRepository cellRepository,
            IFingerRepository fingerRepository,
            ILevelRepository levelRepository,
            ITaskRepository taskRepository,
            IOperationRepository operationRepository,
            IPalletRepository palletRepository,
            IUserRepository userRepository,
            IConfigurationRepository configurationRepository,
            IPalletInCellRepository palletInCellRepository,
            ITaskTypeRepository taskTypeRepository) : this(context, new ConsoleLogger<UnitOfWork>())
        {
            // Override the lazy-loaded repositories with injected ones if provided
            _trolleyRepository = trolleyRepository;
            _cellRepository = cellRepository;
            _fingerRepository = fingerRepository;
            _levelRepository = levelRepository;
            _taskRepository = taskRepository;
            _operationRepository = operationRepository;
            _palletRepository = palletRepository;
            _userRepository = userRepository;
            _configurationRepository = configurationRepository;
            _palletInCellRepository = palletInCellRepository;
            _taskTypeRepository = taskTypeRepository;
        }

        // Lazy-loaded repository properties
        public ITrolleyRepository Trolleys => _trolleyRepository ??= new TrolleyRepository(_context);
        public ITrolleyCellRepository TrolleyCells => _trolleyCellRepository ??= new TrolleyCellRepository(_context);
        public ICellRepository Cells => _cellRepository ??= new CellRepository(_context);
        public IFingerRepository Fingers => _fingerRepository ??= new FingerRepository(_context);
        public ILevelRepository Levels => _levelRepository ??= new LevelRepository(_context);
        public ITaskRepository Tasks => _taskRepository ??= new TaskRepository(_context);
        public IOperationRepository Operations => _operationRepository ??= new OperationRepository(_context);
        public IPalletRepository Pallets => _palletRepository ??= new PalletRepository(_context);
        public IPalletInCellRepository PalletInCells => _palletInCellRepository ??= new PalletInCellRepository(_context);
        public IUserRepository Users => _userRepository ??= new UserRepository(_context);
        public IConfigurationRepository Configurations => _configurationRepository ??= new ConfigurationRepository(_context);
        public ITaskTypeRepository TaskTypes => _taskTypeRepository ??= new TaskTypeRepository(_context);
        public ITraceLogRepository TraceLogs => _traceLogRepository ??= new TraceLogRepository(_context);

        public async Task<int> CompleteAsync()
        {
            try
            {
                _logger.LogDebug("Saving changes to database");
                int result = await _context.SaveChangesAsync();
                _logger.LogDebug("Saved {Count} changes to database", result);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict occurred during save");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public async System.Threading.Tasks.Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogWarning("Transaction already in progress");
                return;
            }

            _transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogDebug("Database transaction started");
        }

        public async System.Threading.Tasks.Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No transaction to commit");
                return;
            }

            try
            {
                await _transaction.CommitAsync();
                _logger.LogDebug("Database transaction committed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing transaction");
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async System.Threading.Tasks.Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No transaction to rollback");
                return;
            }

            try
            {
                await _transaction.RollbackAsync();
                _logger.LogDebug("Database transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back transaction");
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();

                    // Only dispose the context if we created it
                    if (_ownsContext)
                    {
                        _context?.Dispose();
                    }

                    _logger.LogDebug("UnitOfWork disposed");
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }

                if (_ownsContext && _context != null)
                {
                    await _context.DisposeAsync();
                }

                _disposed = true;
                _logger.LogDebug("UnitOfWork disposed asynchronously");
            }

            GC.SuppressFinalize(this);
        }

        public async System.Threading.Tasks.Task ClearAllDataAsync()
        {
            _logger.LogInformation("Attempting to clear all data for workstation initialization...");
            try
            {
                // Order of deletion is important to avoid foreign key constraint violations.
                // Generally, delete records from tables that are referenced by other tables first (join tables),
                // then tables that are referenced, and finally tables that are not referenced by others.

                // 1. Clear join/linking tables or tables with foreign keys that need to be cleared first
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM PalletInCell;"); // Corrected table name
                _logger.LogDebug("Cleared PalletInCell table.");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM TrolleyCells;");
                _logger.LogDebug("Cleared TrolleyCells table.");
                
                // At this point, Tasks might reference Pallets. If so, Tasks should be cleared before Pallets.
                // Or, if Pallets can exist without Tasks, the order might be flexible or PalletId in Task made nullable.
                // Assuming Tasks can be cleared independently or before Pallets.
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Tasks;");
                _logger.LogDebug("Cleared Tasks table.");

                // 2. Clear main entity tables
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM PendingOperation;"); // Corrected table name for PendingOperations
                _logger.LogDebug("Cleared PendingOperation table.");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Pallets;");
                _logger.LogDebug("Cleared Pallets table.");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Trolleys;");
                _logger.LogDebug("Cleared Trolleys table.");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Cells;");
                _logger.LogDebug("Cleared Cells table.");
                
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Fingers;");
                _logger.LogDebug("Cleared Fingers table.");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Levels;");
                _logger.LogDebug("Cleared Levels table.");
                
                // Users, Configurations, TaskTypes are typically not cleared during workstation data reset,
                // but if they were, they would be here.
                // await _context.Database.ExecuteSqlRawAsync("DELETE FROM Users;");
                // await _context.Database.ExecuteSqlRawAsync("DELETE FROM Configurations;"); // Be careful with this, might be needed for re-initialization
                // await _context.Database.ExecuteSqlRawAsync("DELETE FROM TaskTypes;");


                // Note: EF Core does not automatically reset identity seeds when deleting all rows.
                // If you need to reset identity columns (e.g., for SQL Server: DBCC CHECKIDENT ('YourTable', RESEED, 0)),
                // that would require additional provider-specific SQL commands.
                // For simplicity, we are not resetting identity seeds here.

                _logger.LogInformation("Successfully cleared all specified data tables.");
                // No CompleteAsync() call is needed here as ExecuteSqlRawAsync typically runs in its own transaction or commits immediately
                // unless an explicit transaction is managed around this method call.
                // If this method is called within a transaction, CompleteAsync() or CommitTransactionAsync() would be called by the orchestrator.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing all data.");
                throw; // Re-throw to allow calling code to handle
            }
        }
    }

    // Simple console logger for backward compatibility
    internal class ConsoleLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                var message = formatter(state, exception);
                System.Diagnostics.Debug.WriteLine($"[{logLevel}] {typeof(T).Name}: {message}");
                if (exception != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception: {exception}");
                }
            }
        }
    }
}
