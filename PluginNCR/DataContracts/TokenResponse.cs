using Newtonsoft.Json;

namespace PluginNCR.DataContracts
{
    public class TokenResponse
    {
        [JsonProperty("token")]
        public string AccessToken { get; set; }
        
        
        [JsonProperty("remainingTime")]
        public int RemainingTime { get; set; }
    }
}