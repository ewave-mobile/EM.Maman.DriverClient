namespace EM.Maman.Models.Dtos
{
    // Represents the raw response from the server's auth/login endpoint
    public class ServerLoginResponse
    {
        public int UserID { get; set; }
        public bool IsSucceded { get; set; } // Note: Typo in original "isSucceded", matching it here.
        public int ErrorCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmployeeCode { get; set; }
        public int? RoleID { get; set; }
        public string Token { get; set; }
    }

    public class LoginResultDto
    {
        public bool IsSuccess { get; private set; }
        public ServerLoginResponse UserData { get; private set; } // Contains all fields from server
        public string ErrorMessage { get; private set; } // For UI display, in Hebrew
        public bool IsNetworkError { get; private set; } = false;

        // Success constructor
        public LoginResultDto(ServerLoginResponse userData, string successMessage = null)
        {
            IsSuccess = true;
            UserData = userData;
            ErrorMessage = successMessage; // Optional success message
        }

        // Failure constructor
        public LoginResultDto(string errorMessage, bool isNetworkError = false)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
            IsNetworkError = isNetworkError;
        }

        public static LoginResultDto Success(ServerLoginResponse userData, string message = null)
        {
            return new LoginResultDto(userData, message);
        }

        public static LoginResultDto Failure(string errorMessage, bool isNetworkError = false)
        {
            return new LoginResultDto(errorMessage, isNetworkError);
        }
    }
}
