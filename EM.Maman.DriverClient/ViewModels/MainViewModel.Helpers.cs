using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        private async Task<long?> GetTaskTypeIdAsync(int taskTypeCode)
        {
            try
            {
                // Create a new UnitOfWork instance for this operation
                using (var unitOfWork = _unitOfWorkFactory.CreateUnitOfWork())
                {
                    var taskTypeEntity = (await unitOfWork.TaskTypes.FindAsync(tt => tt.Code == taskTypeCode)).FirstOrDefault();

                    if (taskTypeEntity != null)
                    {
                        return taskTypeEntity.Id;
                    }
                    else
                    {
                        _logger.LogWarning("Could not find TaskType in database with Code {TaskTypeCode}.", taskTypeCode);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving TaskType ID for Code {TaskTypeCode}.", taskTypeCode);
                return null;
            }
        }
    }
}
