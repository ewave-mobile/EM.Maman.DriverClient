using EM.Maman.Models.Dtos; // For LoginResultDto
using System.Threading.Tasks;

namespace EM.Maman.Services
{
    public interface IMamanHttpService
    {
        Task<LoginResultDto> LoginAsync(string employeeCode, int craneId);
        Task<TasksApiResponseDto> GetTasksAsync();
        
        // Placeholder for a method to set the token after successful login
        // This might be handled internally or explicitly called.
        // For now, the service will manage the token internally after login.
        // void SetToken(string token); 

        // Future methods for other API calls would go here, e.g.:
        // Task<SomeOtherDto> GetSomeDataAsync();
        // Task<bool> PostSomeDataAsync(SomeRequestDto data);
    }
}
