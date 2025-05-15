using EM.Maman.Models.LocalDbModels;
using EM.Maman.Models.Interfaces; // For IUnitOfWork
using System;
using System.Threading.Tasks;

namespace EM.Maman.Services
{
    public class DBLoggerService : IDBLoggerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int MaxBodyLength = 255;

        public DBLoggerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async System.Threading.Tasks.Task LogAsync(TraceLog logEntry)
        {
            if (logEntry == null)
            {
                // Or handle as per application's error handling strategy
                // For now, just return if logEntry is null to prevent NullReferenceException
                return; 
            }

            // Truncate RequestBody if it exceeds MaxBodyLength
            if (logEntry.RequestBody?.Length > MaxBodyLength)
            {
                logEntry.RequestBody = logEntry.RequestBody.Substring(0, MaxBodyLength);
            }

            // Truncate ResponseBody if it exceeds MaxBodyLength
            if (logEntry.ResponseBody?.Length > MaxBodyLength)
            {
                logEntry.ResponseBody = logEntry.ResponseBody.Substring(0, MaxBodyLength);
            }

            try
            {
                // Assuming IUnitOfWork has a generic repository or a specific TraceLog repository
                // If IUnitOfWork directly exposes DbSets or a method to add TraceLog, use that.
                // For example, if there's a TraceLogs repository:
                // await _unitOfWork.TraceLogs.AddAsync(logEntry);
                // Or if UnitOfWork has a generic Add method:
                // await _unitOfWork.Repository<TraceLog>().AddAsync(logEntry);
                
                // For now, let's assume IUnitOfWork has a method to get the context 
                // or a specific repository for TraceLog.
                // This part might need adjustment based on the actual IUnitOfWork implementation.
                // If IUnitOfWork has a direct TraceLogs DbSet property (e.g. from a custom IUnitOfWork interface)
                // _unitOfWork.TraceLogs.Add(logEntry); 
                // await _unitOfWork.CompleteAsync();

                // If IUnitOfWork provides access to the context:
                // _unitOfWork.Context.Set<TraceLog>().Add(logEntry);
                // await _unitOfWork.Context.SaveChangesAsync();
                
                // Placeholder: Actual saving logic depends on IUnitOfWork structure.
                // This will likely involve getting the TraceLog repository from UoW and adding.
                // Example:
                await _unitOfWork.TraceLogs.AddAsync(logEntry); // Assuming IUnitOfWork has a TraceLogs repository property
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as per application's logging strategy
                // For example, log to a file or console, but avoid re-throwing to break the HTTP service flow
                Console.WriteLine($"Error logging to DB: {ex.Message}");
                // Depending on requirements, you might want to re-throw or handle more gracefully
            }
        }
    }
}
