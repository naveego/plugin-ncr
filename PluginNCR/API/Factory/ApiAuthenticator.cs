using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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

        public async Task<string> GetToken(DateTimeOffset date, string uri = "", string method = "")
        {
            //access key method - one-use key so never a need to assign Token
            if (Settings.AuthMethod == false)
            {
                return await GetNewToken(date, uri, method);
            }
            else
            {
                // check if token is expired or will expire in 5 minutes or less
                if (DateTime.Compare(DateTime.Now.AddMinutes(5), ExpiresAt) >= 0)
                {
                    Token = await GetNewToken(date);
                }
                return Token;
            }
        }

        public async Task<string> GetNewToken(DateTimeOffset date, string uri = "", string method = "")
        {
            try
            {
                //False == access key
                if (Settings.AuthMethod == false)
                {
                    var isoDate = date.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
                    var toSign = method + "\n" + uri;

                    toSign += "\n" + @"application/json";
                    if (!string.IsNullOrWhiteSpace(Settings.NepApplicationKey))
                    {
                        toSign += "\n" + Settings.NepApplicationKey.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(Settings.NepCorrelationId))
                    {
                        toSign += "\n" + Settings.NepCorrelationId.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(Settings.NepOrganization))
                    {
                        toSign += "\n" + Settings.NepOrganization.Trim();
                    }
                    var secret_key = Settings.SecretKey;
                    var shared_key = Settings.SharedKey;
                    var oneTimeSecret = secret_key + isoDate;
                    var keyBytes = Encoding.UTF8.GetBytes(oneTimeSecret);
                    var hmacsha512 = new HMACSHA512(keyBytes);
                    hmacsha512.Initialize();
                    var messageBytes = Encoding.UTF8.GetBytes(toSign);
                    var rawHmac = hmacsha512.ComputeHash(messageBytes);
                    var hmac = Convert.ToBase64String(rawHmac);
                    
                    return $"AccessKey {shared_key}:{hmac}";
                }
                
                //If not accesskey, create token
                var client = new HttpClient();
                // generate request
                var authUri = new Uri(
                    "https://gateway.ncrplatform.com/security/authentication/login");
                
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = authUri,
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
                // Token = content.AccessToken;
                Token = $"AccessToken {Token}";
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