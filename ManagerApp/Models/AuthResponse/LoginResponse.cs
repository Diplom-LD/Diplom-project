using Newtonsoft.Json;

namespace ManagerApp.Models.AuthResponse
{
    public class LoginResponse
    {
        [JsonProperty("accessToken")]
        public required string AccessToken { get; set; }
    }
}
