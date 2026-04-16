using System.Text.Json.Serialization;

namespace AutoPortal.Models
{
    public class LoginConfig
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("portalUrl")]
        public string PortalUrl { get; set; } = "http://10.189.108.11/";

        [JsonPropertyName("autoLogin")]
        public bool AutoLogin { get; set; } = false;
    }
}
