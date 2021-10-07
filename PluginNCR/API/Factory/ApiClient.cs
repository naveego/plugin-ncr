using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Naveego.Sdk.Logging;
using Newtonsoft.Json;
using PluginNCR.API.Utility;
using PluginNCR.Helper;
using RestSharp;

namespace PluginNCR.API.Factory
{

    public class PostBodyContent
    {
        [JsonProperty("businessDay")] public BusinessDay BusinessDay { get; set; }
        [JsonProperty("siteInfoIds")] public string[] SiteInfoIds { get; set; }
        [JsonProperty("pageSize")] public string PageSize { get; set; }
        //[JsonProperty("date")] public string Date { get; set; }
        
        // [JsonProperty("nep-correlation-id")] public string NepCorrelationId { get; set; }
        // [JsonProperty("Authorization")] public string Authorization { get; set; }
        // [JsonProperty("nep-application-key")] public string NepApplicationKey { get; set; }
        // [JsonProperty("date")] public string Date { get; set; }
        // [JsonProperty("nep-organization")] public string NepOrganization { get; set; }
        // [JsonProperty("content-type")] public string ContentType { get; set; }
        
    }

    public class BusinessDay
    {
        [JsonProperty("dateTime")] public string DateTime { get; set; }
        [JsonProperty("originalOffset")] public string OriginalOffset { get; set; }
    }
    public class ApiClient: IApiClient
    {
        private IApiAuthenticator Authenticator { get; set; }
        private static HttpClient Client { get; set; }
        private Settings Settings { get; set; }

        private const string ApiKeyParam = "hapikey";

        
        
        public ApiClient(HttpClient client, Settings settings)
        {
            Authenticator = new ApiAuthenticator(client, settings);
            Client = client;
            Settings = settings;
            
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task GetAuthToken()
        {
            
        }
        public async Task<string> GetStartDate()
        {
            return Settings.QueryStartDate;
        }

        public async Task<string> GetSiteIds()
        {
            return Settings.SiteIDs;
        }
        
        public async Task TestConnection()
        {
            try
            {
                var uriBuilder = new UriBuilder($"{Constants.BaseApiUrl.TrimEnd('/')}/{Utility.Constants.TestConnectionPath.TrimStart('/')}");
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                
                uriBuilder.Query = query.ToString();
                
                var uri = new Uri(uriBuilder.ToString());
                
                var json =
                    "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}";

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("nep-correlation-id", Settings.NepCorrelationId);
                request.Headers.Add("nep-application-key", Settings.NepApplicationKey);
                request.Headers.Add("nep-organization", Settings.NepOrganization);
                request.Headers.Date = DateTimeOffset.UtcNow;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                
                var token = await Authenticator.GetToken();
                request.Headers.Add("Authorization", $"AccessToken {token}");
                
                // send request
                var client = new HttpClient();
                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();

            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }

      
        public async Task<HttpResponseMessage> GetAsync(string path)
        {
            var uriBuilder = new UriBuilder(path);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            
            uriBuilder.Query = query.ToString();
                
            var uri = new Uri(uriBuilder.ToString());

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };
            
            request.Headers.Add("nep-correlation-id", Settings.NepCorrelationId);
            request.Headers.Add("nep-application-key", Settings.NepApplicationKey);
            request.Headers.Add("nep-organization", Settings.NepOrganization);
            request.Headers.Date = DateTimeOffset.UtcNow;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                
            var token = await Authenticator.GetToken();

            request.Headers.Add("Authorization", $"AccessToken {token}");
            return await Client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> SendAsync(string path, StringContent json)
        {
            var uriBuilder = new UriBuilder(path);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            
            uriBuilder.Query = query.ToString();
                
            var uri = new Uri(uriBuilder.ToString());

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = json
            };
            
            request.Headers.Add("nep-correlation-id", Settings.NepCorrelationId);
            request.Headers.Add("nep-application-key", Settings.NepApplicationKey);
            request.Headers.Add("nep-organization", Settings.NepOrganization);
            request.Headers.Date = DateTimeOffset.UtcNow;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                
            var token = await Authenticator.GetToken();

            request.Headers.Add("Authorization", $"AccessToken {token}");
            return await Client.SendAsync(request);
        }
        public async Task<HttpResponseMessage> PostAsync(string path, StringContent json)
        {
            try
            {
                
                var uriBuilder = new UriBuilder($"{Constants.BaseApiUrl.TrimEnd('/')}/{path.TrimStart('/')}");
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                uriBuilder.Query = query.ToString();
                
                var uri = new Uri(uriBuilder.ToString());
                
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = json
                };
                request.Headers.Add("nep-correlation-id", Settings.NepCorrelationId);
                request.Headers.Add("nep-application-key", Settings.NepApplicationKey);
                request.Headers.Add("nep-organization", Settings.NepOrganization);
                request.Headers.Date = DateTimeOffset.UtcNow;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                
                var token = await Authenticator.GetToken();

                request.Headers.Add("Authorization", $"AccessToken {token}");

                return await Client.SendAsync(request);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }

        public async Task<HttpResponseMessage> PutAsync(string path, StringContent json)
        {
            throw new NotImplementedException("Put is not currently supported by the NCR plugin.");
        }

        public async Task<HttpResponseMessage> PatchAsync(string path, StringContent json)
        {
            try
            {
                //var token = await Authenticator.GetToken();
                var uriBuilder = new UriBuilder($"{Constants.BaseApiUrl.TrimEnd('/')}/{path.TrimStart('/')}");
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                uriBuilder.Query = query.ToString();
                
                var uri = new Uri(uriBuilder.ToString());
                
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Patch,
                    RequestUri = uri,
                    Content = json
                };
                return await Client.SendAsync(request);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }

        public async Task<HttpResponseMessage> DeleteAsync(string path)
        {
            throw new NotImplementedException("Delete is not currently supported by the NCR plugin.");
           
        }
    }
}