using Newtonsoft.Json;

namespace PluginHubspot.DataContracts
{
    public class TokenResponse
    {
        [JsonProperty("token")]
        public string AccessToken { get; set; }
        
        
        [JsonProperty("remainingTime")]
        public int RemainingTime { get; set; }
    }
}