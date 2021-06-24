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
using PluginNCR.API.Factory;
using PluginNCR.DataContracts;
using PluginNCR.Helper;
using RestSharp;

namespace PluginNCR.API.Utility.EndpointHelperEndpoints
{
    public class TransactionDocumentEndpointHelper
    {
       

        private class TransactionDocumentEndpoint : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "productId",
                    "productName",
                    "regularUnitPrice",
                    "extendedUnitPrice",
                    "extendedAmount",
                    "actualAmount",
                    "quantity",
                    "unitOfMeasurement"
                };
                
                var properties = new List<Property>();

                foreach (var staticProperty in staticSchemaProperties)
                {
                    var property = new Property();

                    property.Id = staticProperty;
                    property.Name = staticProperty;
                    property.Type = PropertyType.String;

                    switch (staticProperty)
                    {
                        case ("tlogId"):
                        case ("id"):
                        case ("productId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.IsNullable = false;
                            break;
                        case("regularUnitPrice"):
                        case("extendedUnitPrice"):
                        case("extendedAmount"):
                        case("actualAmount"):
                        case("quantity"):
                            property.IsKey = false;
                            property.TypeAtSource = "double";
                            property.IsNullable = true;
                            break;
                        default:
                            property.IsKey = false;
                            property.TypeAtSource = "string";
                            property.IsNullable = true;
                            break;
                    }
                    properties.Add(property);
                }
                schema.Properties.Clear();
                schema.Properties.AddRange(properties);

                schema.DataFlowDirection = GetDataFlowDirection();
                
                return schema;
            }
        }
        private class TransactionDocumentEndpoint_Historical : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "productId",
                    "productName",
                    "regularUnitPrice",
                    "extendedUnitPrice",
                    "extendedAmount",
                    "actualAmount",
                    "quantity",
                    "unitOfMeasurement"
                };
                
                var properties = new List<Property>();

                foreach (var staticProperty in staticSchemaProperties)
                {
                    var property = new Property();

                    property.Id = staticProperty;
                    property.Name = staticProperty;
                    property.Type = PropertyType.String;

                    switch (staticProperty)
                    {
                        case ("tlogId"):
                        case ("id"):
                        case ("productId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            break;
                        case("regularUnitPrice"):
                        case("extendedUnitPrice"):
                        case("extendedAmount"):
                        case("actualAmount"):
                        case("quantity"):
                            property.IsKey = false;
                            property.TypeAtSource = "double";
                            break;
                        default:
                            property.IsKey = false;
                            property.TypeAtSource = "string";
                            break;
                    }
                    properties.Add(property);
                }
                schema.Properties.Clear();
                schema.Properties.AddRange(properties);

                schema.DataFlowDirection = GetDataFlowDirection();
                
                return schema;
            }
            
            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, bool isDiscoverRead = false)
            {
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
                        using (var response = await apiClient.PostAsync(
                            path
                            , json))
                        {
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

                                //here, query for item details. looks right.
                                var thisTlogId = recordMap["tlogId"];

                                //foreach item in result, return recordmap + tlogrecordmap

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

                                //Query for tlog details - make flat addition
                                var tlogResponse = await apiClient.GetAsync(tlogPath);

                                if (!tlogResponse.IsSuccessStatusCode)
                                {
                                    var error = JsonConvert.DeserializeObject<ApiError>(
                                        await tlogResponse.Content.ReadAsStringAsync());
                                    throw new Exception(error.Message);
                                }

                                var tLogResponseWrapper =
                                    JsonConvert.DeserializeObject<TlogWrapper>(
                                        await tlogResponse.Content.ReadAsStringAsync());

                                var tlogItemRecordMap = new Dictionary<string, object>();

                                if (tLogResponseWrapper.TransactionCategory == "SALE_OR_RETURN")
                                {
                                    try
                                    {
                                        tlogItemRecordMap["tlog"] = recordMap["tlogId"] ?? "";
                                    }
                                    catch
                                    {

                                    }

                                    foreach (var item in tLogResponseWrapper.Tlog.Items)
                                    {
                                        bool validItem = true;
                                        try
                                        {
                                            if (item.IsItemNotOnFile == false)
                                            {
                                                tlogItemRecordMap["id"] =
                                                    String.IsNullOrWhiteSpace(item.Id) ? "null" : item.Id;


                                                tlogItemRecordMap["productId"] =
                                                    String.IsNullOrWhiteSpace(item.ProductId)
                                                        ? "null"
                                                        : item.ProductId;

                                                tlogItemRecordMap["productName"] =
                                                    String.IsNullOrWhiteSpace(item.ProductName)
                                                        ? "null"
                                                        : item.ProductName.Replace("'", "''");
                                                
                                                if (item.RegularUnitPrice != null)
                                                {
                                                    tlogItemRecordMap["regularUnitPrice"] =
                                                        String.IsNullOrWhiteSpace(item.RegularUnitPrice.Amount)
                                                            ? "0"
                                                            : item.RegularUnitPrice.Amount.ToString();
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["regularUnitPrice"] = "0";
                                                }

                                                if (item.ExtendedUnitPrice != null)
                                                {
                                                    tlogItemRecordMap["extendedUnitPrice"] =
                                                        String.IsNullOrWhiteSpace(item.ExtendedUnitPrice.Amount)
                                                            ? "0"
                                                            : item.ExtendedUnitPrice.Amount.ToString();
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["extendedUnitPrice"] = "0";
                                                }

                                                if (item.ExtendedAmount != null)
                                                {
                                                    tlogItemRecordMap["extendedAmount"] =
                                                        String.IsNullOrWhiteSpace(item.ExtendedAmount.Amount)
                                                            ? "0"
                                                            : item.ExtendedAmount.Amount.ToString();
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["extendedAmount"] = "0";
                                                }

                                                if (item.ActualAmount != null)
                                                {
                                                    tlogItemRecordMap["actualAmount"] =
                                                        String.IsNullOrWhiteSpace(item.ActualAmount.Amount)
                                                            ? "0"
                                                            : item.ActualAmount.Amount;
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["actualAmount"] = "0";
                                                }

                                                if (item.Quantity != null)
                                                {
                                                    tlogItemRecordMap["quantity"] =
                                                        String.IsNullOrWhiteSpace(item.Quantity.Quantity)
                                                            ? "0"
                                                            : item.Quantity.Quantity;
                                                    
                                                    tlogItemRecordMap["unitOfMeasurement"] =
                                                        String.IsNullOrWhiteSpace(item.Quantity.UnitOfMeasurement)
                                                            ? "null"
                                                            : item.Quantity.UnitOfMeasurement;   
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["quantity"] = "0";
                                                    tlogItemRecordMap["unitOfMeasurement"] = "null";
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            var debug = e.Message;
                                            validItem = false;
                                        }

                                        if (validItem)
                                        {
                                            yield return new Record
                                            {
                                                Action = Record.Types.Action.Upsert,
                                                DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                            };
                                        }
                                    }
                                }

                                // yield return new Record
                                // {
                                //     Action = Record.Types.Action.Upsert,
                                //     DataJson = JsonConvert.SerializeObject(recordMap)
                                // };
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
                    ShouldGetStaticSchema = true,
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
                    ShouldGetStaticSchema = true,
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
                    Method = Method.POST,
                }
            }
        };

    }
}