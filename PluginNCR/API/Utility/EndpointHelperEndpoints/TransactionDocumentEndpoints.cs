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
       

        private class TransactionDocumentEndpoint_Today : Endpoint
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
            
            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = DateTime.Now.ToString("yyyy-MM-dd");
                
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
                                // yield break;
                                continue;
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
                                        tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                        tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
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
                                            else
                                            {
                                                validItem = false;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            var debug = e.Message;
                                            validItem = false;
                                        }

                                        if (validItem)
                                        {
                                            if (string.IsNullOrWhiteSpace(tlogItemRecordMap["id"].ToString()) ||
                                                string.IsNullOrWhiteSpace(tlogItemRecordMap["productId"].ToString()) ||
                                                string.IsNullOrWhiteSpace(recordMap["tlogId"].ToString()))
                                            {
                                                //Just used as debug breakpoint
                                                var noop = tlogItemRecordMap;
                                            }
                                            yield return new Record
                                            {
                                                Action = Record.Types.Action.Upsert,
                                                DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                            };
                                        }
                                    }
                                }
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

        private class TransactionDocumentEndpoint_Yesterday : Endpoint
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
            
            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                
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
                                // yield break;
                                continue;
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
                                        tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                        tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
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
                                            else
                                            {
                                                validItem = false;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            var debug = e.Message;
                                            validItem = false;
                                        }

                                        if (validItem)
                                        {
                                            if (string.IsNullOrWhiteSpace(tlogItemRecordMap["id"].ToString()) ||
                                                string.IsNullOrWhiteSpace(tlogItemRecordMap["productId"].ToString()) ||
                                                string.IsNullOrWhiteSpace(recordMap["tlogId"].ToString()))
                                            {
                                                //Just used as debug breakpoint
                                                var noop = tlogItemRecordMap;
                                            }
                                            yield return new Record
                                            {
                                                Action = Record.Types.Action.Upsert,
                                                DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                            };
                                        }
                                    }
                                }
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

                    switch (staticProperty)
                    {
                        case ("tlogId"):
                        case ("id"):
                        case ("productId"):
                            property.IsKey = true;
                            property.Type = PropertyType.String;
                            property.TypeAtSource = "string";
                            break;
                        case("regularUnitPrice"):
                        case("extendedUnitPrice"):
                        case("extendedAmount"):
                        case("actualAmount"):
                        case("quantity"):
                            property.IsKey = false;
                            property.Type = PropertyType.Float;
                            property.TypeAtSource = "double";
                            break;
                        default:
                            property.IsKey = false;
                            property.Type = PropertyType.String;
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
                                // yield break;
                                continue;
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
                                        tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                        tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
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
                                            else
                                            {
                                                validItem = false;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            var debug = e.Message;
                                            validItem = false;
                                        }

                                        if (validItem)
                                        {
                                            if (string.IsNullOrWhiteSpace(tlogItemRecordMap["id"].ToString()) ||
                                                string.IsNullOrWhiteSpace(tlogItemRecordMap["productId"].ToString()) ||
                                                string.IsNullOrWhiteSpace(recordMap["tlogId"].ToString()))
                                            {
                                                //Just used as debug breakpoint
                                                var noop = tlogItemRecordMap;
                                            }
                                            yield return new Record
                                            {
                                                Action = Record.Types.Action.Upsert,
                                                DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                            };
                                        }
                                    }
                                }
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
        private class TransactionDocumentEndpoint_OrderPromos : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "siteInfoId",
                    "receiptId",
                    "touchPointGroup",
                    "ticketdate",
                    "ticketmonth",
                    "ticketday",
                    "ticketyear",
                    "productId",
                    "departmentId",
                    "quantity",
                    "regularUnitPrice",
                    "actualAmount",
                    "discountAmount",
                    "isReturn",
                    "isVoided",
                    "discountType",
                    "03record",
                    "rtntype",
                    "fscard",
                    "rtnsurchperc",
                    "multunit",
                    "notaxamt",
                    "ignore_transaction",
                    "non_merchandise",
                    "subtract",
                    "negative",
                    "upcharge",
                    "additive",
                    "delivery_charges",
                    "manual_discount",
                    "percent_discount",
                    "cost_plus_item_dept",
                    "foodstampable_item",
                    "store_promo",
                    "plu_transaction_discount",
                    "department_transaction_discount",
                    "promo_type_given",
                    "promo_type_reduction_given",
                    "promo_type_offer_given",
                    "multi_saver",
                    "ext_promo",
                    "not_net_promo",
                    "member_discount",
                    "discount_flag",
                    "points_given_as_a_reward",
                    "customer_acct_discount",
                    "extended_trs",
                    "auto_discount",
                    "delayed_promo",
                    "report_as_tender",
                    "not_netted_promo_frequent_shopper",
                    "row_str"
                };
                
                var properties = new List<Property>();

                foreach (var staticProperty in staticSchemaProperties)
                {
                    var property = new Property();

                    property.Id = staticProperty;
                    property.Name = staticProperty;

                    switch (staticProperty)
                    {
                        case ("tlogId"):
                        case ("id"):
                        case ("productId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
                            break;
                        case ("isReturn"):
                        case ("isVoided"):
                            property.IsKey = false;
                            property.TypeAtSource = "bool";
                            property.Type = PropertyType.Bool;
                            break;
                        default:
                            property.IsKey = false;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
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
                                // yield break;
                                continue;
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
                                        tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                        tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
                                        tlogItemRecordMap["receiptId"] = tLogResponseWrapper.Tlog.ReceiptId ?? "";
                                        tlogItemRecordMap["touchPointGroup"] =
                                            tLogResponseWrapper.Tlog.TouchPointGroup ?? "";
                                        
                                        var date_time = tLogResponseWrapper.BusinessDay.DateTime;
                                        tlogItemRecordMap["ticketdate"] = date_time.Substring(0, 10);
                                        tlogItemRecordMap["ticketmonth"] = date_time.Substring(5, 2);
                                        tlogItemRecordMap["ticketday"] = date_time.Substring(8, 2);
                                        tlogItemRecordMap["ticketyear"] = date_time.Substring(0, 4);
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
                                                tlogItemRecordMap["productId"] =
                                                    String.IsNullOrWhiteSpace(item.ProductId)
                                                        ? "null"
                                                        : item.ProductId;
                                                
                                                tlogItemRecordMap["departmentId"] =
                                                    String.IsNullOrWhiteSpace(item.DepartmentId)
                                                        ? "null"
                                                        : item.DepartmentId;

                                                if (item.Quantity != null)
                                                {
                                                    tlogItemRecordMap["quantity"] =
                                                        String.IsNullOrWhiteSpace(item.Quantity.Quantity)
                                                            ? "0"
                                                            : item.Quantity.Quantity;
                                                }
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

                                                if (tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount != null)
                                                {
                                                    tlogItemRecordMap["discountAmount"] =
                                                        String.IsNullOrWhiteSpace(tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount.Amount)
                                                            ? "0"
                                                            : tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount.Amount;
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["actualAmount"] = "0";
                                                }
                                                
                                                tlogItemRecordMap["isReturn"] = item.IsReturn;
                                                
                                                tlogItemRecordMap["isVoided"] = item.IsVoided;

                                                if (item.ItemDiscounts != null)
                                                {
                                                    //Concat list for safety - original request unclear
                                                    tlogItemRecordMap["discountType"] = string.Join(",",
                                                        item.ItemDiscounts.Select(x => x.DiscountType));
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["discountType"] = "";
                                                }
                                                
                                                // Placeholder rows - all null data by design
                                                // These were cols in previous hive that no longer exist
                                                
                                                tlogItemRecordMap["03record"] = "null";
                                                tlogItemRecordMap["rtntype"] = "null";
                                                tlogItemRecordMap["fscard"] = "null";
                                                tlogItemRecordMap["rtnsurchperc"] = "null";
                                                tlogItemRecordMap["multunit"] = "null";
                                                tlogItemRecordMap["notaxamt"] = "null";
                                                tlogItemRecordMap["ignore_transaction"] = "null";
                                                tlogItemRecordMap["non_merchandise"] = "null";
                                                tlogItemRecordMap["subtract"] = "null";
                                                tlogItemRecordMap["negative"] = "null";
                                                tlogItemRecordMap["upcharge"] = "null";
                                                tlogItemRecordMap["additive"] = "null";
                                                tlogItemRecordMap["delivery_charges"] = "null";
                                                tlogItemRecordMap["manual_discount"] = "null";
                                                tlogItemRecordMap["percent_discount"] = "null";
                                                tlogItemRecordMap["cost_plus_item_dept"] = "null";
                                                tlogItemRecordMap["foodstampable_item"] = "null";
                                                tlogItemRecordMap["store_promo"] = "null";
                                                tlogItemRecordMap["plu_transaction_discount"] = "null";
                                                tlogItemRecordMap["department_transaction_discount"] = "null";
                                                tlogItemRecordMap["promo_type_given"] = "null";
                                                tlogItemRecordMap["promo_type_reduction_given"] = "null";
                                                tlogItemRecordMap["promo_type_offer_given"] = "null";
                                                tlogItemRecordMap["multi_saver"] = "null";
                                                tlogItemRecordMap["ext_promo"] = "null";
                                                tlogItemRecordMap["not_net_promo"] = "null";
                                                tlogItemRecordMap["member_discount"] = "null";
                                                tlogItemRecordMap["discount_flag"] = "null";
                                                tlogItemRecordMap["points_given_as_a_reward"] = "null";
                                                tlogItemRecordMap["customer_acct_discount"] = "null";
                                                tlogItemRecordMap["extended_trs"] = "null";
                                                tlogItemRecordMap["auto_discount"] = "null";
                                                tlogItemRecordMap["delayed_promo"] = "null";
                                                tlogItemRecordMap["report_as_tender"] = "null";
                                                tlogItemRecordMap["not_netted_promo_frequent_shopper"] = "null";
                                                tlogItemRecordMap["row_str"] = "null";
                                            }
                                            else
                                            {
                                                validItem = false;
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
        private class TransactionDocumentEndpoint_Tenders : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "type",
                    "usage",
                    "tenderAmount",
                    "isVoided",
                    "typeLabel",
                    "cardLastFourDigits",
                    "name"
                };
                
                var properties = new List<Property>();

                foreach (var staticProperty in staticSchemaProperties)
                {
                    var property = new Property();

                    property.Id = staticProperty;
                    property.Name = staticProperty;

                    switch (staticProperty)
                    {
                        case ("tlogId"):
                        case ("id"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
                            break;
                        case("tenderAmount"):
                            property.IsKey = false;
                            property.TypeAtSource = "double";
                            property.Type = PropertyType.Float;
                            break;
                        default:
                            property.IsKey = false;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
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

                                var tlogTenderRecordMap = new Dictionary<string, object>();

                                if (tLogResponseWrapper.TransactionCategory == "SALE_OR_RETURN")
                                {
                                    if(tLogResponseWrapper.Tlog.Tenders.Count > 0)
                                    {
                                        foreach (var tender in tLogResponseWrapper.Tlog.Tenders)
                                        {
                                            try
                                            {
                                                tlogTenderRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                                tlogTenderRecordMap["type"] = tender.Type ?? "";
                                                tlogTenderRecordMap["usage"] = tender.Usage ?? "";
                                                tlogTenderRecordMap["tenderAmount"] = tender.TenderAmount.Amount;
                                                tlogTenderRecordMap["isVoided"] = tender.IsVoided;
                                                tlogTenderRecordMap["typeLabel"] = tender.TypeLabel ?? "";
                                                tlogTenderRecordMap["cardLastFourDigits"] =
                                                    tender.CardLastFourDigits ?? "";
                                                tlogTenderRecordMap["name"] = tender.Name ?? "";
                                                tlogTenderRecordMap["id"] = tender.Id ?? "";
                                            }
                                            catch
                                            {

                                            }
                                        }
                                        yield return new Record
                                        {
                                            Action = Record.Types.Action.Upsert,
                                            DataJson = JsonConvert.SerializeObject(tlogTenderRecordMap)
                                        };
                                    }
                                }
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
                "TransactionDocument_Today", new TransactionDocumentEndpoint_Today
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
                        //Note - this is defined as a GET as opposed to POST to appear as a read endpoint in UI
                        EndpointActions.Get
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.GET
                }
                
            },
            {
                "TransactionDocument_Yesterday", new TransactionDocumentEndpoint_Yesterday
                {
                    ShouldGetStaticSchema = true,
                    Id = "TransactionDocument_Yesterday",
                    Name = "TransactionDocument_Yesterday",
                    BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                    AllPath = "/find",
                    PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                    PropertiesQuery = 
                        "{\"businessDay\":{\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":10,\"pageNumber\":0}",
                    ReadQuery = 
                        "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") + "T00:00:00Z\",\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":1000,\"pageNumber\":0}",
                    SupportedActions = new List<EndpointActions>
                    {
                        //Note - this is defined as a GET as opposed to POST to appear as a read endpoint in UI
                        EndpointActions.Get
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.GET
                }
                
            }, 
            {
                "TransactionDocument_HistoricalFromDate", new TransactionDocumentEndpoint_Historical
                {
                    ShouldGetStaticSchema = true,
                    Id = "TransactionDocument_HistoricalFromDate",
                    Name = "TransactionDocument_HistoricalFromDate",
                    BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                    AllPath = "/find",
                    
                    PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                    PropertiesQuery = 
                        "{\"businessDay\":{\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":10,\"pageNumber\":0}",
                    ReadQuery = 
                        "{\"businessDay\":{\"dateTime\": \"<DATE_TIME>\",\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":1000,\"pageNumber\":0}",
                    SupportedActions = new List<EndpointActions>
                    {
                        EndpointActions.Get,
                        EndpointActions.Post,
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.GET
                }
            },
            {
                "TransactionDocument_Tenders", new TransactionDocumentEndpoint_Tenders
                {
                    ShouldGetStaticSchema = true,
                    Id = "TransactionDocument_Tenders",
                    Name = "TransactionDocument_Tenders",
                    BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                    AllPath = "/find",
                    PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                    PropertiesQuery = 
                        "{\"businessDay\":{\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":10,\"pageNumber\":0}",
                    ReadQuery = 
                        "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z\",\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":1000,\"pageNumber\":0}",
                    SupportedActions = new List<EndpointActions>
                    {
                        //Note - this is defined as a GET as opposed to POST to appear as a read endpoint in UI
                        EndpointActions.Get
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.GET
                }
            },
            {
                "TransactionDocument_OrderPromos", new TransactionDocumentEndpoint_OrderPromos
                {
                    ShouldGetStaticSchema = true,
                    Id = "TransactionDocument_OrderPromos",
                    Name = "TransactionDocument_OrderPromos",
                    BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                    AllPath = "/find",
                    PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                    PropertiesQuery = 
                        "{\"businessDay\":{\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":10,\"pageNumber\":0}",
                    ReadQuery = 
                        "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z\",\"originalOffset\":0},\"siteInfoIds\":[\"2315\"],\"pageSize\":1000,\"pageNumber\":0}",
                    SupportedActions = new List<EndpointActions>
                    {
                        //Note - this is defined as a GET as opposed to POST to appear as a read endpoint in UI
                        EndpointActions.Get
                    },
                    PropertyKeys = new List<string>
                    {
                        "tlogId"
                    },
                    Method = Method.GET
                }
            }
        };
    }
}