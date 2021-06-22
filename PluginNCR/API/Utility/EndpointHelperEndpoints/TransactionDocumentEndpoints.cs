using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginHubspot.API.Factory;
using PluginHubspot.DataContracts;
using PluginHubspot.Helper;
using RestSharp;

namespace PluginHubspot.API.Utility.EndpointHelperEndpoints
{
    public class TransactionDocumentEndpointHelper
    {
       

        private class TransactionDocumentEndpoint : Endpoint
        {
        }
        private class TransactionDocumentEndpoint_Historical : Endpoint
        {
            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, bool isDiscoverRead = false)
            {
                var after = "";
                var hasMore = false;

                
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = await apiClient.GetStartDate();
                
                var queryDate = startDate + "T00:00:00Z";
                
                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);
                
                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                do //while queryDate != today
                {
                    queryDate = DateTime.Parse(startDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                    currDayOffset = currDayOffset + 1;
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;
                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;
                        
                        
                        var json = new StringContent(
                            //endpoint.PropertiesQuery.Replace("\"pageNumber\":0", $"\"pageNumber\":{currPage}"),
                            //propertiesQuery.ToString(),
                            JsonConvert.SerializeObject(readQuery),
                            Encoding.UTF8,
                            "application/json"
                        );
                        var response = await apiClient.PostAsync(
                            path
                            , json);

                        if (!response.IsSuccessStatusCode)
                        {
                            var error = JsonConvert.DeserializeObject<ApiError>(
                                await response.Content.ReadAsStringAsync());
                            throw new Exception(error.Message);
                        }

                        var objectResponseWrapper =
                            JsonConvert.DeserializeObject<ObjectResponseWrapper>(
                                await response.Content.ReadAsStringAsync());

                        if (objectResponseWrapper?.PageContent.Count == 0)
                        {
                            yield break;
                        }

                        foreach (var objectResponse in objectResponseWrapper?.PageContent)
                        {
                            var recordMap = new Dictionary<string, object>();

                            foreach (var objectProperty in objectResponse)
                            {
                                try
                                {
                                    recordMap[objectProperty.Key] = objectProperty.Value.ToString() ?? "";
                                }
                                catch
                                {
                                    recordMap[objectProperty.Key] = "";
                                }

                            }

                            yield return new Record
                            {
                                Action = Record.Types.Action.Upsert,
                                DataJson = JsonConvert.SerializeObject(recordMap)
                            };
                        }

                        if (objectResponseWrapper.LastPage.ToLower() == "true" || currPage >= 9)
                        {
                            hasMore = false;
                        }
                        else
                        {
                            currPage++;
                            hasMore = true;
                        }
                    } while (hasMore);
                } while (DateTime.Parse(queryDate).ToString("yyyy-MM-dd") != DateTime.Today.ToString("yyyy-MM-dd"));
            }
        }
        
        public static readonly Dictionary<string, Endpoint> TransactionDocumentEndpoints = new Dictionary<string, Endpoint>
        {
            {
                "TransactionDocument_Today", new TransactionDocumentEndpoint
                {
                    Id = "TransactionDocument_Today",
                    Name = "TransactionDocument_Today",
                    BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                    AllPath = "/find",
                    PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                    PropertiesQuery = 
                        "{\"businessDay\":{\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":10,\"pageNumber\":0}",
                    ReadQuery = 
                        "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z\",\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":1000,\"pageNumber\":0}",
                    SupportedActions = new List<EndpointActions>
                    {
                        EndpointActions.Post
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.POST
                }
            }
            , 
            {
                "TransactionDocument_HistoricalFromDate", new TransactionDocumentEndpoint_Historical
                {
                    Id = "TransactionDocument_HistoricalFromDate",
                    Name = "TransactionDocument_HistoricalFromDate",
                    BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                    AllPath = "/find",
                    //Date should be from user settings
                    PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                    PropertiesQuery = 
                        "{\"businessDay\":{\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":10,\"pageNumber\":0}",
                    ReadQuery = 
                        "{\"businessDay\":{\"dateTime\": \"<DATE_TIME>\",\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":1000,\"pageNumber\":0}",
                    SupportedActions = new List<EndpointActions>
                    {
                        EndpointActions.Post
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.POST
                }
            }
        };

    }
}