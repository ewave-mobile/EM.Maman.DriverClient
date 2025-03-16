using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EM.Maman.Models.LocalDbModels;
using System.Text.Json;

namespace EM.Maman.Services
{
    public class CommandQueueService : Models.Interfaces.Services.ICommandQueueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CommandQueueService> _logger;

        public CommandQueueService(
            IUnitOfWork unitOfWork,
            ILogger<CommandQueueService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<long> EnqueueCommandAsync<T>(string commandType, T parameters, int? userId = null)
        {
            try
            {
                var operation = new PendingOperation
                {
                    CommandType = commandType,
                    Parameters = JsonSerializer.Serialize(parameters),
                    CreatedAt = DateTime.Now,
                  //  Status = OperationStatus.Pending,
                    UserId = userId,
                    RetryCount = 0
                };

              //  await _unitOfWork.Operations.AddAsync(operation);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation($"Command enqueued: {commandType}, ID: {operation.Id}");
                return operation.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to enqueue command: {commandType}");
                throw;
            }
        }

        public async Task<bool> TryDequeueCommandAsync(PendingOperation operation)
        {
            if (operation == null)
                return false;

            try
            {
                // Mark as in progress
            //    operation.Status = OperationStatus.InProgress;
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to dequeue operation {operation.Id}");
                return false;
            }
        }

        public async Task<T> DeserializeParametersAsync<T>(string serializedParameters)
        {
            if (string.IsNullOrEmpty(serializedParameters))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(serializedParameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize parameters");
                throw;
            }
        }

        public async System.Threading.Tasks.Task MarkOperationCompletedAsync(long operationId)
        {
            try
            {
                var operation = await _unitOfWork.Operations.GetByIdAsync(operationId);
                if (operation != null)
                {
                    operation.Status = Models.Enums.OperationStatus.Completed.ToString();
                    operation.CompletedAt = DateTime.Now;
                    await _unitOfWork.CompleteAsync();
                    _logger.LogInformation($"Operation {operationId} marked as completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to mark operation {operationId} as completed");
                throw;
            }
        }

        public async System.Threading.Tasks.Task MarkOperationFailedAsync(long operationId, string error)
        {
            try
            {
                var operation = await _unitOfWork.Operations.GetByIdAsync(operationId);
                if (operation != null)
                {
                    operation.Status = Models.Enums.OperationStatus.Failed.ToString();
                    operation.ErrorMessage = error;
                    await _unitOfWork.CompleteAsync();
                    _logger.LogWarning($"Operation {operationId} marked as failed: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to mark operation {operationId} as failed");
                throw;
            }
        }

        public async Task<int> GetPendingOperationsCountAsync()
        {
            try
            {
                return await _unitOfWork.Operations.GetPendingOperationsCountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending operations count");
                return 0;
            }
        }
    }
}
