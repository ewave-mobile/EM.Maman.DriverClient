using EM.Maman.Models.Dtos;
using EM.Maman.Models.LocalDbModels; // For TraceLog
using Microsoft.Extensions.Configuration; // For IConfiguration
using System;
using System.Diagnostics; // For Stopwatch
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // For ReadFromJsonAsync and PostAsJsonAsync
using System.Text.Json;
using System.Threading.Tasks;

namespace EM.Maman.Services
{
    public class MamanHttpService : IMamanHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IDBLoggerService _dbLoggerService;
        private readonly string _apiBaseUrl;
        private string _currentJwtToken; // Stores the current JWT

        // Define a nested class for the login request payload
        private class LoginRequestPayload
        {
            public string Code { get; set; }
            public int CraneId { get; set; }
        }

        public MamanHttpService(HttpClient httpClient, IConfiguration configuration, IDBLoggerService dbLoggerService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _dbLoggerService = dbLoggerService ?? throw new ArgumentNullException(nameof(dbLoggerService));
            _apiBaseUrl = configuration?["ApiSettings:BaseUrl"] ?? throw new ArgumentNullException("ApiSettings:BaseUrl not configured in appsettings.json");

            if (!_apiBaseUrl.EndsWith("/"))
            {
                _apiBaseUrl += "/";
            }
        }

        public async Task<LoginResultDto> LoginAsync(string employeeCode, int craneId)
        {
            var requestPayload = new LoginRequestPayload { Code = employeeCode, CraneId = craneId };
            string requestUrl = $"{_apiBaseUrl}auth/login";
            
            var traceLog = new TraceLog
            {
                RequestTimestamp = DateTime.UtcNow,
                RequestMethod = "POST",
                RequestUrl = requestUrl,
            };

            HttpResponseMessage response = null;
            string responseContentString = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                traceLog.RequestBody = JsonSerializer.Serialize(requestPayload);

                // Ensure previous token is cleared for login request
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                response = await _httpClient.PostAsJsonAsync(requestUrl, requestPayload);
                
                responseContentString = await response.Content.ReadAsStringAsync();
                traceLog.ResponseStatusCode = (int)response.StatusCode;
                traceLog.ResponseBody = responseContentString;

                if (response.IsSuccessStatusCode)
                {
                    var serverResponse = JsonSerializer.Deserialize<ServerLoginResponse>(responseContentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (serverResponse != null && serverResponse.IsSucceded)
                    {
                        _currentJwtToken = serverResponse.Token; // Store the token
                        // Set token for subsequent requests from this HttpClient instance
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _currentJwtToken);
                        return LoginResultDto.Success(serverResponse);
                    }
                    else
                    {
                        // Handle cases where HTTP status is success but application logic indicates failure (e.g. IsSucceded=false)
                        string errorMessage = $"התחברות נכשלה. שגיאה: {serverResponse?.ErrorCode}"; // Default error
                        if (serverResponse?.ErrorCode == 1) errorMessage = "שם משתמש או סיסמה שגויים."; // Example specific error
                        return LoginResultDto.Failure(errorMessage);
                    }
                }
                else
                {
                    // Handle non-success HTTP status codes
                    string errorMessage = $"שגיאת שרת ({response.StatusCode}). נסה שנית מאוחר יותר.";
                    // Potentially parse error from responseContentString if backend provides structured errors
                    return LoginResultDto.Failure(errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                // Network errors or other HTTP request issues
                traceLog.ResponseBody = $"Network Error: {ex.Message}";
                return LoginResultDto.Failure("שגיאת רשת. בדוק את חיבור האינטרנט שלך.", isNetworkError: true);
            }
            catch (JsonException ex)
            {
                // Error deserializing response
                traceLog.ResponseBody = $"JSON Deserialization Error: {ex.Message}. Raw content: {responseContentString}";
                 return LoginResultDto.Failure("שגיאה בעיבוד תגובת השרת.");
            }
            catch (Exception ex)
            {
                // Catch-all for other unexpected errors
                traceLog.ResponseBody = $"Unexpected Error: {ex.Message}";
                return LoginResultDto.Failure("אירעה שגיאה בלתי צפויה.");
            }
            finally
            {
                stopwatch.Stop();
                traceLog.DurationMs = stopwatch.ElapsedMilliseconds;
                traceLog.ResponseTimestamp = DateTime.UtcNow; // Set here to capture timestamp even on exception
                if (_dbLoggerService != null) // Ensure service is available before logging
                {
                    await _dbLoggerService.LogAsync(traceLog);
                }
            }
        }

        // Example of a generic GET method for future use
        // public async Task<T> GetAsync<T>(string endpoint)
        // {
        //     // Ensure token is set if available
        //     // if (!string.IsNullOrEmpty(_currentJwtToken) && _httpClient.DefaultRequestHeaders.Authorization == null)
        //     // {
        //     //     _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _currentJwtToken);
        //     // }
        //     // ... logging and error handling ...
        //     var response = await _httpClient.GetAsync($"{_apiBaseUrl}{endpoint}");
        //     response.EnsureSuccessStatusCode(); // Or handle errors more gracefully
        //     return await response.Content.ReadFromJsonAsync<T>();
        // }
    }
}
