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
using Dasync.Collections;

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
                    "isItemNotOnFile",
                    "productName",
                    "regularUnitPrice",
                    "extendedUnitPrice",
                    "extendedAmount",
                    "actualAmount",
                    "quantity",
                    "unitOfMeasurement",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
                            property.IsKey = false;
                            property.TypeAtSource = "bool";
                            property.Type = PropertyType.Bool;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var startDate = DateTime.Now.ToString("yyyy-MM-dd");

                var queryDate = DateTime.Parse(startDate).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};


                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;

                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;


                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                try
                                {
                                    tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                    tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
                                    if (tLogResponseWrapper.Tlog.Customer != null)
                                    {
                                        tlogItemRecordMap["customerId"] =
                                            tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                        tlogItemRecordMap["customerEntryMethod"] =
                                            tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                        tlogItemRecordMap["customerIdentifierData"] =
                                            tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] =
                                            tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                    }
                                    else
                                    {
                                        tlogItemRecordMap["customerId"] = "null";
                                        tlogItemRecordMap["customerEntryMethod"] = "null";
                                        tlogItemRecordMap["customerIdentifierData"] = "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                    }

                                    tlogItemRecordMap["tlog_isSuspended"] = tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                    tlogItemRecordMap["tlog_isTrainingMode"] =
                                        tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                    tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                    tlogItemRecordMap["tlog_isRecalled"] =
                                        tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                    tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                    tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();

                                }
                                catch
                                {
                                    //noop
                                }

                                if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                {
                                    var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                    tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                    tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                    tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                    tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                    tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                    tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                    tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                    tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                }
                                
                                foreach (var item in tLogResponseWrapper.Tlog.Items)
                                {
                                    try
                                    {
                                        tlogItemRecordMap["id"] =
                                            String.IsNullOrWhiteSpace(item.Id) ? "null" : item.Id;


                                        tlogItemRecordMap["productId"] =
                                            String.IsNullOrWhiteSpace(item.ProductId)
                                                ? "null"
                                                : item.ProductId;

                                        tlogItemRecordMap["isItemNotOnFile"] = item.IsItemNotOnFile.ToString();

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
                                        
                                        
                                        tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                        tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                        tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                        tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                        tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                        tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;
                                    }
                                    catch (Exception e)
                                    {
                                        continue;
                                    }

                                    yield return new Record
                                    {
                                        Action = Record.Types.Action.Upsert,
                                        DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                    };
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
                }
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
                    "isItemNotOnFile",
                    "productName",
                    "regularUnitPrice",
                    "extendedUnitPrice",
                    "extendedAmount",
                    "actualAmount",
                    "quantity",
                    "unitOfMeasurement",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
                            property.IsKey = false;
                            property.TypeAtSource = "bool";
                            property.Type = PropertyType.Bool;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var startDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                var queryDate = DateTime.Parse(startDate).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;

                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;


                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                try
                                {
                                    tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                    tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
                                    if (tLogResponseWrapper.Tlog.Customer != null)
                                    {
                                        tlogItemRecordMap["customerId"] =
                                            tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                        tlogItemRecordMap["customerEntryMethod"] =
                                            tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                        tlogItemRecordMap["customerIdentifierData"] =
                                            tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] =
                                            tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                    }
                                    else
                                    {
                                        tlogItemRecordMap["customerId"] = "null";
                                        tlogItemRecordMap["customerEntryMethod"] = "null";
                                        tlogItemRecordMap["customerIdentifierData"] = "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                    }

                                    tlogItemRecordMap["tlog_isSuspended"] = tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                    tlogItemRecordMap["tlog_isTrainingMode"] =
                                        tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                    tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                    tlogItemRecordMap["tlog_isRecalled"] =
                                        tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                    tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                    tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();
                                }
                                catch
                                {
                                    //noop
                                }
                                if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                {
                                    var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                    tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                    tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                    tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                    tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                    tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                    tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                    tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                    tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                }
                                foreach (var item in tLogResponseWrapper.Tlog.Items)
                                {
                                    bool validItem = true;
                                    try
                                    {
                                        tlogItemRecordMap["id"] =
                                            String.IsNullOrWhiteSpace(item.Id) ? "null" : item.Id;

                                        tlogItemRecordMap["isItemNotOnFile"] =
                                            item.IsItemNotOnFile.ToString();

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
                                        
                                        
                                        tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                        tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                        tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                        tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                        tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                        tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;
                                    }
                                    catch (Exception e)
                                    {
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
                }
            }
        }

        private class TransactionDocumentEndpoint_7Days : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "productId",
                    "isItemNotOnFile",
                    "productName",
                    "regularUnitPrice",
                    "extendedUnitPrice",
                    "extendedAmount",
                    "actualAmount",
                    "quantity",
                    "unitOfMeasurement",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
                            property.IsKey = false;
                            property.TypeAtSource = "bool";
                            property.Type = PropertyType.Bool;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");

                var queryDate = startDate + "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;
                    do //while queryDate != today
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

var json = JsonConvert.SerializeObject(readQuery);
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    try
                                    {
                                        tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                        tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
                                        if (tLogResponseWrapper.Tlog.Customer != null)
                                        {
                                            tlogItemRecordMap["customerId"] =
                                                tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                            tlogItemRecordMap["customerEntryMethod"] =
                                                tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                            tlogItemRecordMap["customerIdentifierData"] =
                                                tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                            tlogItemRecordMap["customerInfoValidationMeans"] =
                                                tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                        }
                                        else
                                        {
                                            tlogItemRecordMap["customerId"] = "null";
                                            tlogItemRecordMap["customerEntryMethod"] = "null";
                                            tlogItemRecordMap["customerIdentifierData"] = "null";
                                            tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                        }

                                        tlogItemRecordMap["tlog_isSuspended"] =
                                            tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                        tlogItemRecordMap["tlog_isTrainingMode"] =
                                            tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                        tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                        tlogItemRecordMap["tlog_isRecalled"] =
                                            tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                        tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                        tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();
                                    }
                                    catch
                                    {
                                        //noop
                                    }
                                    if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                    {
                                        var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                        tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                        tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                        tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                        tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                        tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                        tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                        tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                        tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                    }
                                    foreach (var item in tLogResponseWrapper.Tlog.Items)
                                    {
                                        bool validItem = true;
                                        try
                                        {
                                            tlogItemRecordMap["id"] =
                                                String.IsNullOrWhiteSpace(item.Id) ? "null" : item.Id;


                                            tlogItemRecordMap["productId"] =
                                                String.IsNullOrWhiteSpace(item.ProductId)
                                                    ? "null"
                                                    : item.ProductId;

                                            tlogItemRecordMap["isItemNotOnFile"] = item.IsItemNotOnFile.ToString();

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
                                            
                                            
                                            tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                            tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                            tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                            tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                            tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                            tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;
                                        }
                                        catch (Exception e)
                                        {
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
                    "isItemNotOnFile",
                    "productName",
                    "regularUnitPrice",
                    "extendedUnitPrice",
                    "extendedAmount",
                    "actualAmount",
                    "quantity",
                    "unitOfMeasurement",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
                            property.IsKey = false;
                            property.TypeAtSource = "bool";
                            property.Type = PropertyType.Bool;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var queryDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();

                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');


                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    currDayOffset = 0;
                    readQuery.SiteInfoIds = new List<string>() {site};

                    do //while queryDate != queryEndDate
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

var json = JsonConvert.SerializeObject(readQuery);
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
                                    continue;
                                }

                                List<Record> returnRecords = new List<Record>() { };
                                await objectResponseWrapper?.PageContent.ParallelForEachAsync(async objectResponse =>
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    try
                                    {
                                        tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                        tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
                                        if (tLogResponseWrapper.Tlog.Customer != null)
                                        {
                                            tlogItemRecordMap["customerId"] =
                                                tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                            tlogItemRecordMap["customerEntryMethod"] =
                                                tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                            tlogItemRecordMap["customerIdentifierData"] =
                                                tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                            tlogItemRecordMap["customerInfoValidationMeans"] =
                                                tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                        }
                                        else
                                        {
                                            tlogItemRecordMap["customerId"] = "null";
                                            tlogItemRecordMap["customerEntryMethod"] = "null";
                                            tlogItemRecordMap["customerIdentifierData"] = "null";
                                            tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                        }
                                    }
                                    catch
                                    {
                                        //noop
                                    }
                                    if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                    {
                                        var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                        tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                        tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                        tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                        tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                        tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                        tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                        tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                        tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                    }

                                    foreach (var item in tLogResponseWrapper.Tlog.Items)
                                    {
                                        bool validItem = true;
                                        try
                                        {
                                            tlogItemRecordMap["id"] =
                                                String.IsNullOrWhiteSpace(item.Id) ? "null" : item.Id;

                                            tlogItemRecordMap["isItemNotOnFile"] = item.IsItemNotOnFile.ToString();

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
                                            
                                            
                                            tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                            tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                            tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                            tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                            tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                            tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;
                                        }
                                        catch (Exception e)
                                        {
                                            validItem = false;
                                        }

                                        if (validItem)
                                        {
                                            returnRecords.Add(new Record
                                            {
                                                Action = Record.Types.Action.Upsert,
                                                DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                            });
                                        }
                                    }
                                }, maxDegreeOfParallelism: 10);

                                foreach (var record in returnRecords)
                                {
                                    yield return record;
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
                    } while (DateTime.Compare(DateTime.Parse(queryDate), DateTime.Parse(queryEndDate)) < 0);
                }
            }
        }

        private class TransactionDocumentEndpoint_OrderPromos_Historical : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "siteInfoId",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "receiptId",
                    "touchPointGroup",
                    "ticketdate",
                    "ticketmonth",
                    "ticketday",
                    "ticketyear",
                    "productId",
                    "isItemNotOnFile",
                    "departmentId",
                    "quantity",
                    "regularUnitPrice",
                    "actualAmount",
                    "discountAmount",
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
                    "row_str",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                
                var queryDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 1;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

                            var json = JsonConvert.SerializeObject(readQuery);
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
                                    continue;
                                }
                                

                                List<Record> returnRecords = new List<Record>() { };
                                await objectResponseWrapper?.PageContent.ParallelForEachAsync(async objectResponse =>
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

                                        var thisTlogId = recordMap["tlogId"];

                                        var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                        var tender_type_db = tLogResponseWrapper.Tlog.Tenders;

                                        foreach (var tender in tender_type_db)
                                        {
                                            if (tender.Type != "DEBIT_CARD")
                                            {
                                                var id = thisTlogId;
                                                var db0 = tender;
                                            }
                                        }
                                        
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

                                            if (tLogResponseWrapper.Tlog.Customer != null)
                                            {
                                                tlogItemRecordMap["customerId"] =
                                                    tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                                tlogItemRecordMap["customerEntryMethod"] =
                                                    tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                                tlogItemRecordMap["customerIdentifierData"] =
                                                    tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                                tlogItemRecordMap["customerInfoValidationMeans"] =
                                                    tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                            }
                                            else
                                            {
                                                tlogItemRecordMap["customerId"] = "null";
                                                tlogItemRecordMap["customerEntryMethod"] = "null";
                                                tlogItemRecordMap["customerIdentifierData"] = "null";
                                                tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                            }

                                            tlogItemRecordMap["tlog_isSuspended"] =
                                                tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                            tlogItemRecordMap["tlog_isTrainingMode"] =
                                                tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                            tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                            tlogItemRecordMap["tlog_isRecalled"] =
                                                tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                            tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                            tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();
                                        }
                                        catch
                                        {
                                            //noop
                                        }
                                        if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                        {
                                            var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                            tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                            tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                            tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                            tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                            tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                            tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                            tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                            tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                        }
                                        foreach (var item in tLogResponseWrapper.Tlog.Items)
                                        {
                                            bool validItem = true;
                                            try
                                            {
                                                tlogItemRecordMap["id"] = String.IsNullOrWhiteSpace(item.Id)
                                                    ? "null"
                                                    : item.Id;

                                                tlogItemRecordMap["IsItemNotOnFile"] = item.IsItemNotOnFile.ToString();

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
                                                        String.IsNullOrWhiteSpace(tLogResponseWrapper.Tlog
                                                            .TLogTotals.DiscountAmount.Amount)
                                                            ? "0"
                                                            : tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount
                                                                .Amount;
                                                }
                                                else
                                                {
                                                    tlogItemRecordMap["actualAmount"] = "0";
                                                }

                                                tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                                tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                                tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                                tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                                tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                                tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;

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
                                            catch (Exception e)
                                            {
                                                validItem = false;
                                            }

                                            if (validItem)
                                            {
                                                returnRecords.Add(new Record
                                                {
                                                    Action = Record.Types.Action.Upsert,
                                                    DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                                });
                                            }
                                        }
                                    }, maxDegreeOfParallelism: 10);
                                
                                foreach (var record in returnRecords)
                                {
                                    yield return record;
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
                    } while (DateTime.Compare(DateTime.Parse(readQuery.BusinessDay.DateTime), DateTime.Parse(queryEndDate)) < 0);
                }
            }
        }

        private class TransactionDocumentEndpoint_OrderPromos_Yesterday : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "siteInfoId",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "receiptId",
                    "touchPointGroup",
                    "ticketdate",
                    "ticketmonth",
                    "ticketday",
                    "ticketyear",
                    "productId",
                    "isItemNotOnFile",
                    "departmentId",
                    "quantity",
                    "regularUnitPrice",
                    "actualAmount",
                    "discountAmount",
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
                    "row_str",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                var queryDate = DateTime.Parse(startDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};
                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};

                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;

                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;


                        // var json = new StringContent(
                        //     JsonConvert.SerializeObject(readQuery),
                        //     Encoding.UTF8,
                        //     "application/json");
                        
                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    if (tLogResponseWrapper.Tlog.Customer != null)
                                    {
                                        tlogItemRecordMap["customerId"] =
                                            tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                        tlogItemRecordMap["customerEntryMethod"] =
                                            tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                        tlogItemRecordMap["customerIdentifierData"] =
                                            tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] =
                                            tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                    }
                                    else
                                    {
                                        tlogItemRecordMap["customerId"] = "null";
                                        tlogItemRecordMap["customerEntryMethod"] = "null";
                                        tlogItemRecordMap["customerIdentifierData"] = "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                    }

                                    tlogItemRecordMap["tlog_isSuspended"] = tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                    tlogItemRecordMap["tlog_isTrainingMode"] =
                                        tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                    tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                    tlogItemRecordMap["tlog_isRecalled"] =
                                        tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                    tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                    tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();
                                }
                                catch
                                {
                                }
                                if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                {
                                    var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                    tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                    tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                    tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                    tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                    tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                    tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                    tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                    tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                }
                                foreach (var item in tLogResponseWrapper.Tlog.Items)
                                {
                                    bool validItem = true;
                                    try
                                    {
                                        if (item.IsItemNotOnFile == false)
                                        {
                                            tlogItemRecordMap["id"] = String.IsNullOrWhiteSpace(item.Id)
                                                ? "null"
                                                : item.Id;

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
                                                    String.IsNullOrWhiteSpace(tLogResponseWrapper.Tlog
                                                        .TLogTotals.DiscountAmount.Amount)
                                                        ? "0"
                                                        : tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount
                                                            .Amount;
                                            }
                                            else
                                            {
                                                tlogItemRecordMap["actualAmount"] = "0";
                                            }

                                            
                                            tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                            tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                            tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                            tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                            tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                            tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;

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
                }
            }
        }

        private class TransactionDocumentEndpoint_OrderPromos_7Days : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "siteInfoId",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "receiptId",
                    "touchPointGroup",
                    "ticketdate",
                    "ticketmonth",
                    "ticketday",
                    "ticketyear",
                    "productId",
                    "isItemNotOnFile",
                    "departmentId",
                    "quantity",
                    "regularUnitPrice",
                    "actualAmount",
                    "discountAmount",
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
                    "row_str",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");

                var queryDate = startDate + "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                
                
                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != today
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

var json = JsonConvert.SerializeObject(readQuery);
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                        if (tLogResponseWrapper.Tlog.Customer != null)
                                        {
                                            tlogItemRecordMap["customerId"] =
                                                tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                            tlogItemRecordMap["customerEntryMethod"] =
                                                tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                            tlogItemRecordMap["customerIdentifierData"] =
                                                tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                            tlogItemRecordMap["customerInfoValidationMeans"] =
                                                tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                        }
                                        else
                                        {
                                            tlogItemRecordMap["customerId"] = "null";
                                            tlogItemRecordMap["customerEntryMethod"] = "null";
                                            tlogItemRecordMap["customerIdentifierData"] = "null";
                                            tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                        }

                                        tlogItemRecordMap["tlog_isSuspended"] =
                                            tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                        tlogItemRecordMap["tlog_isTrainingMode"] =
                                            tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                        tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                        tlogItemRecordMap["tlog_isRecalled"] =
                                            tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                        tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                        tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();
                                    }
                                    catch
                                    {
                                    }
                                    if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                    {
                                        var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                        tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                        tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                        tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                        tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                        tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                        tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                        tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                        tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                    }
                                    foreach (var item in tLogResponseWrapper.Tlog.Items)
                                    {
                                        bool validItem = true;
                                        try
                                        {
                                            tlogItemRecordMap["id"] = String.IsNullOrWhiteSpace(item.Id)
                                                ? "null"
                                                : item.Id;

                                            tlogItemRecordMap["productId"] =
                                                String.IsNullOrWhiteSpace(item.ProductId)
                                                    ? "null"
                                                    : item.ProductId;

                                            tlogItemRecordMap["isItemNotOnFile"] = item.IsItemNotOnFile.ToString();

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
                                                    String.IsNullOrWhiteSpace(tLogResponseWrapper.Tlog
                                                        .TLogTotals.DiscountAmount.Amount)
                                                        ? "0"
                                                        : tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount
                                                            .Amount;
                                            }
                                            else
                                            {
                                                tlogItemRecordMap["actualAmount"] = "0";
                                            }

                                            
                                            tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                            tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                            tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                            tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                            tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                            tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;

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
                                        catch (Exception e)
                                        {
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
        }

        private class TransactionDocumentEndpoint_OrderPromos_Today : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "siteInfoId",
                    "customerId",
                    "customerEntryMethod",
                    "customerIdentifierData",
                    "customerInfoValidationMeans",
                    "receiptId",
                    "touchPointGroup",
                    "ticketdate",
                    "ticketmonth",
                    "ticketday",
                    "ticketyear",
                    "productId",
                    "isItemNotOnFile",
                    "departmentId",
                    "quantity",
                    "regularUnitPrice",
                    "actualAmount",
                    "discountAmount",
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
                    "row_str",
                    "tlog_isTrainingMode",
                    "tlog_isResumed",
                    "tlog_isVoided",
                    "tlog_isDeleted",
                    "tlog_isRecalled",
                    "tlog_isSuspended",
                    "item_isReturn",
                    "item_isVoided",
                    "item_isRefund",
                    "item_isRefused",
                    "item_isPriceLookup",
                    "item_isOverridden",
                    "taxId",
                    "taxName",
                    "taxType",
                    "taxableAmount",
                    "taxAmount",
                    "taxIsRefund",
                    "taxIsVoided",
                    "taxSequenceNumber"
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
                        case ("taxIsRefund"):
                        case ("taxIsVoided"):
                        case ("item_isReturn"):
                        case ("item_isVoided"):
                        case ("item_isRefused"):
                        case ("item_isPriceLookup"):
                        case ("item_isOverridden"):
                        case ("item_isRefund"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var startDate = DateTime.Now.ToString("yyyy-MM-dd");

                var queryDate = DateTime.Parse(startDate).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;

                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;


                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    if (tLogResponseWrapper.Tlog.Customer != null)
                                    {
                                        tlogItemRecordMap["customerId"] =
                                            tLogResponseWrapper.Tlog.Customer.Id ?? "null";
                                        tlogItemRecordMap["customerEntryMethod"] =
                                            tLogResponseWrapper.Tlog.Customer.EntryMethod ?? "null";
                                        tlogItemRecordMap["customerIdentifierData"] =
                                            tLogResponseWrapper.Tlog.Customer.IdentifierData ?? "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] =
                                            tLogResponseWrapper.Tlog.Customer.InfoValidationMeans ?? "null";
                                    }
                                    else
                                    {
                                        tlogItemRecordMap["customerId"] = "null";
                                        tlogItemRecordMap["customerEntryMethod"] = "null";
                                        tlogItemRecordMap["customerIdentifierData"] = "null";
                                        tlogItemRecordMap["customerInfoValidationMeans"] = "null";
                                    }

                                    tlogItemRecordMap["tlog_isSuspended"] = tLogResponseWrapper.Tlog.IsSuspended.ToString();
                                    tlogItemRecordMap["tlog_isTrainingMode"] =
                                        tLogResponseWrapper.Tlog.IsTrainingMode.ToString();
                                    tlogItemRecordMap["tlog_isResumed"] = tLogResponseWrapper.Tlog.IsResumed.ToString();
                                    tlogItemRecordMap["tlog_isRecalled"] =
                                        tLogResponseWrapper.Tlog.IsRecalled.ToString();
                                    tlogItemRecordMap["tlog_isDeleted"] = tLogResponseWrapper.Tlog.IsDeleted.ToString();
                                    tlogItemRecordMap["tlog_isVoided"] = tLogResponseWrapper.Tlog.IsVoided.ToString();
                                }
                                catch
                                {
                                }
                                if (tLogResponseWrapper.Tlog.TotalTaxes != null && tLogResponseWrapper.Tlog.TotalTaxes.Count > 0)
                                {
                                    var tax = tLogResponseWrapper.Tlog.TotalTaxes[0];
                                    
                                    tlogItemRecordMap["taxId"] = tax.Id ?? "";
                                    tlogItemRecordMap["taxName"] = tax.Name ?? "";
                                    tlogItemRecordMap["taxType"] = tax.TaxType ?? "";
                                    tlogItemRecordMap["taxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                    tlogItemRecordMap["taxAmount"] = tax.Amount.Amount ?? "";
                                    tlogItemRecordMap["taxIsRefund"] = tax.IsRefund;
                                    tlogItemRecordMap["taxIsVoided"] = tax.IsVoided;
                                    tlogItemRecordMap["taxSequenceNumber"] = tax.SequenceNumber ?? "";
                                }
                                foreach (var item in tLogResponseWrapper.Tlog.Items)
                                {
                                    bool validItem = true;
                                    try
                                    {
                                        tlogItemRecordMap["id"] = String.IsNullOrWhiteSpace(item.Id)
                                            ? "null"
                                            : item.Id;

                                        tlogItemRecordMap["productId"] =
                                            String.IsNullOrWhiteSpace(item.ProductId)
                                                ? "null"
                                                : item.ProductId;

                                        tlogItemRecordMap["isItemNotOnFile"] = item.IsItemNotOnFile.ToString();

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
                                                String.IsNullOrWhiteSpace(tLogResponseWrapper.Tlog
                                                    .TLogTotals.DiscountAmount.Amount)
                                                    ? "0"
                                                    : tLogResponseWrapper.Tlog.TLogTotals.DiscountAmount
                                                        .Amount;
                                        }
                                        else
                                        {
                                            tlogItemRecordMap["actualAmount"] = "0";
                                        }

                                        
                                        tlogItemRecordMap["item_isReturn"] = item.IsReturn;
                                        tlogItemRecordMap["item_isVoided"] = item.IsVoided;
                                        tlogItemRecordMap["item_isRefund"] = item.IsRefund;
                                        tlogItemRecordMap["item_isRefused"] = item.IsRefused;
                                        tlogItemRecordMap["item_isPriceLookup"] = item.IsPriceLookup;
                                        tlogItemRecordMap["item_isOverridden"] = item.IsOverridden;

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
                                    catch (Exception e)
                                    {
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
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_Historical : Endpoint
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
                    "tender_isVoided",
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
                        case ("tender_isVoided"):
                            property.IsKey = false;
                            property.TypeAtSource = "boolean";
                            property.Type = PropertyType.Bool;
                            break;
                        case ("tenderAmount"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var queryDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

var json = JsonConvert.SerializeObject(readQuery);
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
                                    continue;
                                }

                                List<Record> returnRecords = new List<Record>() { };
                                await objectResponseWrapper?.PageContent.ParallelForEachAsync(async objectResponse =>
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    if (tLogResponseWrapper.Tlog.Tenders.Count > 0)
                                    {
                                        foreach (var tender in tLogResponseWrapper.Tlog.Tenders)
                                        {
                                            try
                                            {
                                                tlogTenderRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                                tlogTenderRecordMap["type"] = tender.Type ?? "";
                                                tlogTenderRecordMap["usage"] = tender.Usage ?? "";
                                                tlogTenderRecordMap["tenderAmount"] = tender.TenderAmount.Amount;
                                                tlogTenderRecordMap["tender_isVoided"] = tender.IsVoided;
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

                                        returnRecords.Add(new Record
                                        {
                                            Action = Record.Types.Action.Upsert,
                                            DataJson = JsonConvert.SerializeObject(tlogTenderRecordMap)
                                        });
                                    }
                                }, maxDegreeOfParallelism: 10);

                                foreach (var record in returnRecords)
                                {
                                    yield return record;
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
                    } while (DateTime.Compare(DateTime.Parse(queryDate), DateTime.Parse(queryEndDate)) < 0);
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_Today : Endpoint
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
                    "tender_isVoided",
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
                        case ("tender_isVoided"):
                            property.IsKey = false;
                            property.TypeAtSource = "boolean";
                            property.Type = PropertyType.Bool;
                            break;
                        case ("tenderAmount"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var startDate = DateTime.Now.ToString("yyyy-MM-dd");

                var queryDate = DateTime.Parse(startDate).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};
                
                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;

                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;

                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                if (tLogResponseWrapper.Tlog.Tenders.Count > 0)
                                {
                                    foreach (var tender in tLogResponseWrapper.Tlog.Tenders)
                                    {
                                        try
                                        {
                                            tlogTenderRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                            tlogTenderRecordMap["type"] = tender.Type ?? "";
                                            tlogTenderRecordMap["usage"] = tender.Usage ?? "";
                                            tlogTenderRecordMap["tenderAmount"] = tender.TenderAmount.Amount;
                                            tlogTenderRecordMap["tender_isVoided"] = tender.IsVoided;
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
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_Yesterday : Endpoint
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
                    "tender_isVoided",
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
                        case ("tender_isVoided"):
                        case ("item_isReturn"):
                        case ("item_isRefund"):
                            property.IsKey = false;
                            property.TypeAtSource = "boolean";
                            property.Type = PropertyType.Bool;
                            break;
                        case ("tenderAmount"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var startDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                var queryDate = DateTime.Parse(startDate).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;

                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;


                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                if (tLogResponseWrapper.Tlog.Tenders.Count > 0)
                                {
                                    foreach (var tender in tLogResponseWrapper.Tlog.Tenders)
                                    {
                                        try
                                        {
                                            tlogTenderRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                            tlogTenderRecordMap["type"] = tender.Type ?? "";
                                            tlogTenderRecordMap["usage"] = tender.Usage ?? "";
                                            tlogTenderRecordMap["tenderAmount"] = tender.TenderAmount.Amount;
                                            tlogTenderRecordMap["tender_isVoided"] = tender.IsVoided;
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
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_7Days : Endpoint
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
                    "tender_isVoided",
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
                        case ("tender_isVoided"):
                            property.IsKey = false;
                            property.TypeAtSource = "boolean";
                            property.Type = PropertyType.Bool;
                            break;
                        case ("tenderAmount"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");

                var queryDate = startDate + "T00:00:00Z";

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != today
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

                            var json = JsonConvert.SerializeObject(readQuery);
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    if (tLogResponseWrapper.Tlog.Tenders.Count > 0)
                                    {
                                        foreach (var tender in tLogResponseWrapper.Tlog.Tenders)
                                        {
                                            try
                                            {
                                                tlogTenderRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                                tlogTenderRecordMap["type"] = tender.Type ?? "";
                                                tlogTenderRecordMap["usage"] = tender.Usage ?? "";
                                                tlogTenderRecordMap["tenderAmount"] = tender.TenderAmount.Amount;
                                                tlogTenderRecordMap["tender_isVoided"] = tender.IsVoided;
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
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts_Historical : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "loyaltyAccountRow",
                    "loyaltyAccountId",
                    "pointsAwarded",
                    "pointsRedeemed",
                    "programType"
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
                        case ("loyaltyAccountRow"):
                        case ("loyaltyAccountId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var queryDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

var json = JsonConvert.SerializeObject(readQuery);
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
                                    continue;
                                }
                                List<Record> returnRecords = new List<Record>() { };
                                await objectResponseWrapper?.PageContent.ParallelForEachAsync(async objectResponse =>
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    var tlogLoyaltyAccountRecordMap = new Dictionary<string, object>();

                                    if (tLogResponseWrapper.Tlog.LoyaltyAccount.Count > 0)
                                    {
                                        foreach (var loyaltyAccount in tLogResponseWrapper.Tlog.LoyaltyAccount)
                                        {
                                            try
                                            {
                                                tlogLoyaltyAccountRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                                tlogLoyaltyAccountRecordMap["loyaltyAccountRow"] =
                                                    loyaltyAccount.Id ?? "";
                                                tlogLoyaltyAccountRecordMap["loyaltyAccountId"] =
                                                    loyaltyAccount.AccountId ?? "";
                                                tlogLoyaltyAccountRecordMap["pointsAwarded"] =
                                                    loyaltyAccount.PointsAwarded ?? "";
                                                tlogLoyaltyAccountRecordMap["pointsRedeemed"] =
                                                    loyaltyAccount.PointsRedeemed ?? "";
                                                tlogLoyaltyAccountRecordMap["programType"] =
                                                    loyaltyAccount.ProgramType ?? "";
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        returnRecords.Add(new Record
                                        {
                                            Action = Record.Types.Action.Upsert,
                                            DataJson = JsonConvert.SerializeObject(tlogLoyaltyAccountRecordMap)
                                        });
                                    }
                                }, maxDegreeOfParallelism: 10);

                                foreach (var record in returnRecords)
                                {
                                    yield return record;
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
                    } while (DateTime.Compare(DateTime.Parse(queryDate), DateTime.Parse(queryEndDate)) < 0);
                }
            }
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts_Today : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "loyaltyAccountRow",
                    "loyaltyAccountId",
                    "pointsAwarded",
                    "pointsRedeemed",
                    "programType"
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
                        case ("loyaltyAccountRow"):
                        case ("loyaltyAccountId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var queryDate = DateTime.Now.ToString("yyyy-MM-dd") +
                                "T00:00:00Z";


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;
                    
                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;

                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                var tlogLoyaltyAccountRecordMap = new Dictionary<string, object>();

                                if (tLogResponseWrapper.Tlog.LoyaltyAccount.Count > 0)
                                {
                                    foreach (var loyaltyAccount in tLogResponseWrapper.Tlog.LoyaltyAccount)
                                    {
                                        try
                                        {
                                            tlogLoyaltyAccountRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                            tlogLoyaltyAccountRecordMap["loyaltyAccountRow"] = loyaltyAccount.Id ?? "";
                                            tlogLoyaltyAccountRecordMap["loyaltyAccountId"] = loyaltyAccount.AccountId ?? "";
                                            tlogLoyaltyAccountRecordMap["pointsAwarded"] = loyaltyAccount.PointsAwarded ?? "";
                                            tlogLoyaltyAccountRecordMap["pointsRedeemed"] = loyaltyAccount.PointsRedeemed ?? "";
                                            tlogLoyaltyAccountRecordMap["programType"] = loyaltyAccount.ProgramType ?? "";
                                        }
                                        catch
                                        {
                                        }
                                    }

                                    yield return new Record
                                    {
                                        Action = Record.Types.Action.Upsert,
                                        DataJson = JsonConvert.SerializeObject(tlogLoyaltyAccountRecordMap)
                                    };
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
                }
            }
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts_Yesterday : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "loyaltyAccountRow",
                    "loyaltyAccountId",
                    "pointsAwarded",
                    "pointsRedeemed",
                    "programType"
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
                        case ("loyaltyAccountRow"):
                        case ("loyaltyAccountId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var queryDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") +
                                "T00:00:00Z";


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    readQuery.BusinessDay.DateTime = queryDate;
                    currPage = 0;
                    
                    do //while hasMore
                    {
                        readQuery.PageNumber = currPage;

                        var json = JsonConvert.SerializeObject(readQuery);
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

                                var thisTlogId = recordMap["tlogId"];

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                var tlogLoyaltyAccountRecordMap = new Dictionary<string, object>();

                                if (tLogResponseWrapper.Tlog.LoyaltyAccount.Count > 0)
                                {
                                    foreach (var loyaltyAccount in tLogResponseWrapper.Tlog.LoyaltyAccount)
                                    {
                                        try
                                        {
                                            tlogLoyaltyAccountRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                            tlogLoyaltyAccountRecordMap["loyaltyAccountRow"] = loyaltyAccount.Id ?? "";
                                            tlogLoyaltyAccountRecordMap["loyaltyAccountId"] = loyaltyAccount.AccountId ?? "";
                                            tlogLoyaltyAccountRecordMap["pointsAwarded"] = loyaltyAccount.PointsAwarded ?? "";
                                            tlogLoyaltyAccountRecordMap["pointsRedeemed"] = loyaltyAccount.PointsRedeemed ?? "";
                                            tlogLoyaltyAccountRecordMap["programType"] = loyaltyAccount.ProgramType ?? "";
                                        }
                                        catch
                                        {
                                        }
                                    }

                                    yield return new Record
                                    {
                                        Action = Record.Types.Action.Upsert,
                                        DataJson = JsonConvert.SerializeObject(tlogLoyaltyAccountRecordMap)
                                    };
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
                }
            }
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts_7Days : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "loyaltyAccountRow",
                    "loyaltyAccountId",
                    "pointsAwarded",
                    "pointsRedeemed",
                    "programType"
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
                        case ("loyaltyAccountRow"):
                        case ("loyaltyAccountId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema,
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                var queryDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd") + "T00:00:00Z";


                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.BusinessDay.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

var json = JsonConvert.SerializeObject(readQuery);
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

                                    var thisTlogId = recordMap["tlogId"];

                                    var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

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

                                    var tlogLoyaltyAccountRecordMap = new Dictionary<string, object>();

                                    if (tLogResponseWrapper.Tlog.LoyaltyAccount.Count > 0)
                                    {
                                        foreach (var loyaltyAccount in tLogResponseWrapper.Tlog.LoyaltyAccount)
                                        {
                                            try
                                            {
                                                tlogLoyaltyAccountRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                                tlogLoyaltyAccountRecordMap["loyaltyAccountRow"] = loyaltyAccount.Id ?? "";
                                                tlogLoyaltyAccountRecordMap["loyaltyAccountId"] = loyaltyAccount.AccountId ?? "";
                                                tlogLoyaltyAccountRecordMap["pointsAwarded"] = loyaltyAccount.PointsAwarded ?? "";
                                                tlogLoyaltyAccountRecordMap["pointsRedeemed"] = loyaltyAccount.PointsRedeemed ?? "";
                                                tlogLoyaltyAccountRecordMap["programType"] = loyaltyAccount.ProgramType ?? "";
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        yield return new Record
                                        {
                                            Action = Record.Types.Action.Upsert,
                                            DataJson = JsonConvert.SerializeObject(tlogLoyaltyAccountRecordMap)
                                        };
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
                    } while (DateTime.Compare(DateTime.Parse(queryDate), DateTime.Today) < 0);
                }
            }
        }

        public static readonly Dictionary<string, Endpoint> TransactionDocumentEndpoints =
            new Dictionary<string, Endpoint>
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
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_7Days", new TransactionDocumentEndpoint_7Days
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_7Days",
                        Name = "TransactionDocument_7Days",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"<DATE_TIME>\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_Tenders_HistoricalFromDate", new TransactionDocumentEndpoint_Tenders_Historical
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_Tenders_HistoricalFromDate",
                        Name = "TransactionDocument_Tenders_HistoricalFromDate",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_Tenders_Yesterday", new TransactionDocumentEndpoint_Tenders_Yesterday
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_Tenders_Yesterday",
                        Name = "TransactionDocument_Tenders_Yesterday",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_Tenders_7Days", new TransactionDocumentEndpoint_Tenders_7Days
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_Tenders_7Days",
                        Name = "TransactionDocument_Tenders_7Days",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_Tenders_Today", new TransactionDocumentEndpoint_Tenders_Today
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_Tenders_Today",
                        Name = "TransactionDocument_Tenders_Today",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_OrderPromos_HistoricalFromDate", new TransactionDocumentEndpoint_OrderPromos_Historical
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_OrderPromos_HistoricalFromDate",
                        Name = "TransactionDocument_OrderPromos_HistoricalFromDate",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"siteInfoIds\":[\"2304\"],\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_OrderPromos_Today", new TransactionDocumentEndpoint_OrderPromos_Today
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_OrderPromos_Today",
                        Name = "TransactionDocument_OrderPromos_Today",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_OrderPromos_Yesterday", new TransactionDocumentEndpoint_OrderPromos_Yesterday
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_OrderPromos_Yesterday",
                        Name = "TransactionDocument_OrderPromos_Yesterday",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_OrderPromos_7Days", new TransactionDocumentEndpoint_OrderPromos_7Days
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_OrderPromos_7Days",
                        Name = "TransactionDocument_OrderPromos_7Days",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"siteInfoIds\":[\"2304\"],\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_LoyaltyAccounts_HistoricalFromDate", new TransactionDocumentEndpoint_LoyaltyAccounts_Historical
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_LoyaltyAccounts_HistoricalFromDate",
                        Name = "TransactionDocument_LoyaltyAccounts_HistoricalFromDate",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_LoyaltyAccounts_Yesterday", new TransactionDocumentEndpoint_LoyaltyAccounts_Yesterday
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_LoyaltyAccounts_Yesterday",
                        Name = "TransactionDocument_LoyaltyAccounts_Yesterday",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_LoyaltyAccounts_7Days", new TransactionDocumentEndpoint_LoyaltyAccounts_7Days
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_LoyaltyAccounts_7Days",
                        Name = "TransactionDocument_LoyaltyAccounts_7Days",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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
                    "TransactionDocument_LoyaltyAccounts_Today", new TransactionDocumentEndpoint_LoyaltyAccounts_Today
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionDocument_LoyaltyAccounts_Today",
                        Name = "TransactionDocument_LoyaltyAccounts_Today",
                        BasePath = "/transaction-document/2.0/transaction-documents/2.0",
                        AllPath = "/find",
                        PropertiesPath = "/transaction-document/2.0/transaction-documents/2.0/find",
                        PropertiesQuery =
                            "{\"businessDay\":{\"originalOffset\":0},\"pageSize\":10,\"pageNumber\":0}",
                        ReadQuery =
                            "{\"businessDay\":{\"dateTime\": \"" + DateTime.Today.ToString("yyyy-MM-dd") +
                            "T00:00:00Z\",\"originalOffset\":0},\"pageSize\":1000,\"pageNumber\":0}",
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