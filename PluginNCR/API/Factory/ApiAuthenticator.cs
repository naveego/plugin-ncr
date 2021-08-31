using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Naveego.Sdk.Logging;
using Newtonsoft.Json;
using PluginNCR.DataContracts;
using PluginNCR.Helper;

namespace PluginNCR.API.Factory
{
    public class ApiAuthenticator: IApiAuthenticator
    {
        private HttpClient Client { get; set; }
        private Settings Settings { get; set; }
        private string Token { get; set; }
        private DateTime ExpiresAt { get; set; }
        
        public ApiAuthenticator(HttpClient client, Settings settings)
        {
            Client = client;
            Settings = settings;
            ExpiresAt = DateTime.Now;
            Token = "";
        }

        public async Task<string> GetToken()
        {
             // check if token is expired or will expire in 5 minutes or less
             if (DateTime.Compare(DateTime.Now.AddMinutes(5), ExpiresAt) >= 0)
             {
                 return await GetNewToken();
             }
            
             return Token;
        }

        private async Task<string> GetNewToken()
        {
            try
            {
                var client = new HttpClient();
                // client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                // generate request
                var uri = new Uri(
                    "https://gateway.ncrplatform.com/security/authentication/login");
                    // "https://gateway.ncrplatform.com/transaction-document/2.0/transaction-documents/2.0/find");
               
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    //Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("nep-correlation-id", Settings.NepCorrelationId);
                request.Headers.Add("nep-application-key", Settings.NepApplicationKey);
                request.Headers.Add("nep-organization", Settings.NepOrganization);
                request.Headers.Date = DateTimeOffset.UtcNow;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                var authenticationString = $"{Settings.ProvUsername}:{Settings.ProvPassword}";
                var base64EncodedAuthenticationString =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
                request.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");

                //var response = await Client.PostAsync(AuthUrl, body);
                var response = await client.SendAsync(request);
                
                response.EnsureSuccessStatusCode();
                
                var content = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());
                    
                // update expiration and saved token
                ExpiresAt = DateTime.Now.AddSeconds(content.RemainingTime);
                Token = content.AccessToken;

                return Token;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }
    }
}