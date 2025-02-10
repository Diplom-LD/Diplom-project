using Newtonsoft.Json;

namespace ManagerApp.Models.AuthResponse
{
    public class LoginResponse
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
