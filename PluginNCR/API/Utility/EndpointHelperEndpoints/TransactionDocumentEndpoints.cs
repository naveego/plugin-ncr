using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
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
        //create another tier of nesting for each endpoint - only diff is time and staticschemas?
        
        private class TransactionDocumentEndpoint : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "id",
                    "siteInfoId",
                    "closeDateTimeUtc",
                    "receivedDateTimeUtc",
                    "endTransactionDateTimeUtc",
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
                    "quantityUnitOfMeasurement",
                    "quantityEntryMethod",
                    "regularUnitPrice",
                    "regularUnitPrice.quantity",
                    "regularUnitPrice.unitOfMeasurement",
                    "extendedUnitPrice",
                    "extendedUnitPrice.quantity",
                    "extendedUnitPrice.unitOfMeasurement",
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit, string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);
                var currPage = 0;
                var currDayOffset = 0;
                uint recordCount = 0;
                var safeLimit = limit > 0 ? (int)limit : Int32.MaxValue;
                var queryDate = startDate;
                var queryEndDate = endDate;
                var degreeOfParallelism = Int32.Parse(await apiClient.GetDegreeOfParallelism());

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);
                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');
                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    var timeAtSiteChange = DateTime.Now;
                    if (limit > 0 && recordCount >= limit)
                    {
                        break;
                    }
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 1;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.DateWrapper.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
                        currDayOffset = currDayOffset + 1;
                        currPage = 0;

                        do //while hasMore
                        {
                            readQuery.PageNumber = currPage;

                            var json = JsonConvert.SerializeObject(readQuery);
                            
                            HttpResponseMessage response = null;
                            
                            response = await apiClient.PostAsync(
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
                                continue;
                            }

                            var pageContent = limit > 0 ? objectResponseWrapper?.PageContent.Take(safeLimit - (int)recordCount) : 
                                objectResponseWrapper?.PageContent;
                            
                            List<Record> returnRecords = new List<Record>() { };
                            
                            await pageContent.ParallelForEachAsync(async objectResponse =>
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
                                var closeDateTime = recordMap["closeDateTimeUtc"].ToString() ?? "";
                                var receivedDateTime = recordMap["receivedDateTimeUtc"].ToString() ?? "";
                                var endTransactionDateTimeUtc = recordMap["endTransactionDateTimeUtc"].ToString() ?? "";

                                var tlogPath = Constants.BaseApiUrl + BasePath + '/' + thisTlogId;

                                var tlogResponse = await apiClient.GetAsync(tlogPath);

                                if (!tlogResponse.IsSuccessStatusCode)
                                {
                                    var error = JsonConvert.DeserializeObject<ApiError>(
                                        await tlogResponse.Content.ReadAsStringAsync());
                                    throw new Exception(error.Message);
                                }
                                
                                TlogWrapper tLogResponseWrapper = new TlogWrapper();
                                var tlogResponsePulledSuccessfully = false;
                                var retryCount = 0; //note for future adjustments: it is common to see outages of 10min or more with NCR.
                                while (!tlogResponsePulledSuccessfully && retryCount < 20)
                                {
                                    try
                                    {
                                        tLogResponseWrapper =
                                            JsonConvert.DeserializeObject<TlogWrapper>(
                                                await tlogResponse.Content.ReadAsStringAsync());
                                        tlogResponsePulledSuccessfully = true;
                                    }
                                    catch (Exception e)
                                    {
                                        retryCount++;
                                        Thread.Sleep(1000*60);
                                        tlogResponse = await apiClient.GetAsync(tlogPath);
                                    }
                                }

                                if (retryCount >= 20)
                                {
                                    //critical failure
                                    var db = tlogResponse;
                                }
                                
                                var tlogItemRecordMap = new Dictionary<string, object>();
                                
                                try
                                {
                                    tlogItemRecordMap["tlogId"] = recordMap["tlogId"] ?? "";
                                    tlogItemRecordMap["siteInfoId"] = tLogResponseWrapper.SiteInfo.Id ?? "";
                                    tlogItemRecordMap["receiptId"] = tLogResponseWrapper.Tlog.ReceiptId ?? "";
                                    tlogItemRecordMap["touchPointGroup"] =
                                        tLogResponseWrapper.Tlog.TouchPointGroup ?? "";
                                    
                                    
                                    tlogItemRecordMap["closeDateTimeUtc"] = recordMap["closeDateTimeUtc"].ToString() ?? "";
                                    tlogItemRecordMap["receivedDateTimeUtc"] = recordMap["receivedDateTimeUtc"].ToString() ?? "";
                                    tlogItemRecordMap["endTransactionDateTimeUtc"] = recordMap["endTransactionDateTimeUtc"].ToString() ?? "";

                                    var date_time = tLogResponseWrapper.DateWrapper.DateTime;
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
                                var items = limit > 0
                                    ? tLogResponseWrapper.Tlog.Items.Take(safeLimit-(int)recordCount)
                                    : tLogResponseWrapper.Tlog.Items;
                                
                                foreach (var item in items)
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
                                                string.IsNullOrWhiteSpace(item.Quantity.Quantity)
                                                    ? "0"
                                                    : item.Quantity.Quantity;
                                            tlogItemRecordMap["quantityUnitOfMeasurement"] =
                                                item.Quantity.UnitOfMeasurement ?? "";
                                            tlogItemRecordMap["quantityEntryMethod"] = item.Quantity.EntryMethod ?? "";
                                        }

                                        if (item.RegularUnitPrice != null)
                                        {
                                            tlogItemRecordMap["regularUnitPrice"] =
                                                string.IsNullOrWhiteSpace(item.RegularUnitPrice.Amount)
                                                    ? "0"
                                                    : item.RegularUnitPrice.Amount.ToString();

                                            if (item.RegularUnitPrice.UnitPriceQuantity != null)
                                            {
                                                tlogItemRecordMap["regularUnitPrice.quantity"] =
                                                    item.RegularUnitPrice.UnitPriceQuantity.Quantity.ToString() ?? "";

                                                tlogItemRecordMap["regularUnitPrice.unitOfMeasurement"] =
                                                    item.RegularUnitPrice.UnitPriceQuantity.UnitOfMeasurement ?? "";
                                            }
                                            else
                                            {
                                                tlogItemRecordMap["regularUnitPrice.quantity"] = "";
                                                tlogItemRecordMap[
                                                    "regularUnitPrice.unitOfMeasurement"] = "";
                                            }
                                        }
                                        else
                                        {
                                            tlogItemRecordMap["regularUnitPrice"] = "0";
                                        }

                                        if (item.RegularUnitPrice != null)
                                        {
                                            tlogItemRecordMap["extendedUnitPrice"] =
                                                string.IsNullOrWhiteSpace(item.ExtendedUnitPrice.Amount)
                                                    ? "0"
                                                    : item.ExtendedUnitPrice.Amount.ToString();

                                            if (item.ExtendedUnitPrice.UnitPriceQuantity != null)
                                            {
                                                tlogItemRecordMap["extendedUnitPrice.quantity"] =
                                                    item.ExtendedUnitPrice.UnitPriceQuantity.Quantity.ToString() ?? "";

                                                tlogItemRecordMap["extendedUnitPrice.unitOfMeasurement"] =
                                                    item.ExtendedUnitPrice.UnitPriceQuantity.UnitOfMeasurement ?? "";
                                            }
                                            else
                                            {
                                                tlogItemRecordMap["extendedUnitPrice.quantity"] = "";
                                                tlogItemRecordMap[
                                                    "extendedUnitPrice.unitOfMeasurement"] = "";
                                            }
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
                                            tlogItemRecordMap["discountAmount"] = "0";
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
                                        recordCount++;
                                        if (recordCount > limit && limit > 0)
                                        {
                                            hasMore = false;
                                            break;
                                        }
                                        else
                                        {
                                            returnRecords.Add(new Record
                                            {
                                                Action = Record.Types.Action.Upsert,
                                                DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                            });
                                        }
                                    }
                                }
                            }, maxDegreeOfParallelism: degreeOfParallelism);
                            
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
                            
                         } while (hasMore && (limit == 0 || (int)recordCount < limit));
                    } while (DateTime.Compare(DateTime.Parse(readQuery.DateWrapper.DateTime), DateTime.Parse(queryEndDate)) < 0 && (limit == 0 || (int)recordCount < limit));
                }
            }
        }

        private class TransactionDocumentEndpoint_Historical : TransactionDocumentEndpoint
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();
                
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
        private class TransactionDocumentEndpoint_Yesterday : TransactionDocumentEndpoint
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateYesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateYesterday, queryDateYesterday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_7Days : TransactionDocumentEndpoint
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                var queryEndDate = DateTime.Today.ToString("yyyy-MM-dd") +
                                   "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);


                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_Today : TransactionDocumentEndpoint
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateToday = DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateToday, queryDateToday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
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
                    "tender_isVoided",
                    "typeLabel",
                    "cardLastFourDigits",
                    "maskedCardNumber",
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit, string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                uint recordCount = 0;
                var safeLimit = limit > 0 ? (int)limit : Int32.MaxValue;
                
                var queryDate = startDate;
                var queryEndDate = endDate;
                var degreeOfParallelism = Int32.Parse(await apiClient.GetDegreeOfParallelism());

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    if (limit > 0 && recordCount >= limit)
                    {
                        break;
                    }
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.DateWrapper.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
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
                                
                                var pageContent = limit > 0
                                    ? objectResponseWrapper?.PageContent.Take(safeLimit-(int)recordCount)
                                    : objectResponseWrapper?.PageContent;
                                
                                
                                await pageContent.ParallelForEachAsync(async objectResponse =>
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

                                    TlogWrapper tLogResponseWrapper = new TlogWrapper();
                                    var tlogResponsePulledSuccessfully = false;
                                    var retryCount = 0; //note for future adjustments: it is common to see outages of 10min or more with NCR.
                                    while (!tlogResponsePulledSuccessfully && retryCount < 20)
                                    {
                                        try
                                        {
                                            tLogResponseWrapper =
                                                JsonConvert.DeserializeObject<TlogWrapper>(
                                                    await tlogResponse.Content.ReadAsStringAsync());
                                            tlogResponsePulledSuccessfully = true;
                                        }
                                        catch (Exception e)
                                        {
                                            retryCount++;
                                            Thread.Sleep(1000*60);
                                            tlogResponse = await apiClient.GetAsync(tlogPath);
                                        }
                                    }

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
                                                tlogTenderRecordMap["maskedCardNumber"] = tender.MaskedCardNumber ?? "";

                                                recordCount++;
                                                if (recordCount > limit && limit > 0)
                                                {
                                                    hasMore = false;
                                                    break;
                                                }
                                                else
                                                {
                                                    returnRecords.Add(new Record
                                                    {
                                                        Action = Record.Types.Action.Upsert,
                                                        DataJson = JsonConvert.SerializeObject(tlogTenderRecordMap)
                                                    }); 
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                }, maxDegreeOfParallelism: degreeOfParallelism);

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
                        } while (hasMore && (limit == 0 || recordCount < limit));
                    } while (DateTime.Compare(DateTime.Parse(readQuery.DateWrapper.DateTime), DateTime.Parse(queryEndDate)) < 0 && (limit == 0 || (int)recordCount < limit));
                }
            }
        }
        private class TransactionDocumentEndpoint_Tenders_Historical : TransactionDocumentEndpoint_Tenders
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();
                
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_Today : TransactionDocumentEndpoint_Tenders
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateToday = DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateToday, queryDateToday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_Yesterday : TransactionDocumentEndpoint_Tenders
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                
                var queryDateYesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateYesterday, queryDateYesterday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_Tenders_7Days : TransactionDocumentEndpoint_Tenders
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                var queryEndDate = DateTime.Today.ToString("yyyy-MM-dd") +
                                   "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);


                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts : Endpoint
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit, string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                uint recordCount = 0;
                var safeLimit = limit > 0 ? (int)limit : Int32.MaxValue;
                
                var queryDate = startDate;
                var queryEndDate = endDate;
                var degreeOfParallelism = Int32.Parse(await apiClient.GetDegreeOfParallelism());

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    if (limit > 0 && recordCount >= limit)
                    {
                        break;
                    }
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 0;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.DateWrapper.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
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
                                
                                var pageContent = limit > 0
                                    ? objectResponseWrapper?.PageContent.Take(safeLimit-(int)recordCount)
                                    : objectResponseWrapper?.PageContent;
                                
                                await pageContent.ParallelForEachAsync(async objectResponse =>
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

                                    TlogWrapper tLogResponseWrapper = new TlogWrapper();
                                    var tlogResponsePulledSuccessfully = false;
                                    var retryCount = 0; //note for future adjustments: it is common to see outages of 10min or more with NCR.
                                    while (!tlogResponsePulledSuccessfully && retryCount < 20)
                                    {
                                        try
                                        {
                                            tLogResponseWrapper =
                                                JsonConvert.DeserializeObject<TlogWrapper>(
                                                    await tlogResponse.Content.ReadAsStringAsync());
                                            tlogResponsePulledSuccessfully = true;
                                        }
                                        catch (Exception e)
                                        {
                                            retryCount++;
                                            Thread.Sleep(1000*60);
                                            tlogResponse = await apiClient.GetAsync(tlogPath);
                                        }
                                    }

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
                                                
                                                recordCount++;
                                                if (recordCount > limit && limit > 0)
                                                {
                                                    hasMore = false;
                                                    break;
                                                }
                                                else
                                                {
                                                    returnRecords.Add(new Record
                                                    {
                                                        Action = Record.Types.Action.Upsert,
                                                        DataJson = JsonConvert.SerializeObject(tlogLoyaltyAccountRecordMap)
                                                    });
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        
                                    }
                                }, maxDegreeOfParallelism: degreeOfParallelism);

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
                        } while (hasMore && (limit == 0 || recordCount < limit));
                    } while (DateTime.Compare(DateTime.Parse(readQuery.DateWrapper.DateTime), DateTime.Parse(queryEndDate)) < 0 && (limit == 0 || (int)recordCount < limit));
                }
            }
        }
        private class TransactionDocumentEndpoint_LoyaltyAccounts_Historical : TransactionDocumentEndpoint_LoyaltyAccounts
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();
                
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
        private class TransactionDocumentEndpoint_LoyaltyAccounts_Today : TransactionDocumentEndpoint_LoyaltyAccounts
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateToday = DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateToday, queryDateToday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts_Yesterday : TransactionDocumentEndpoint_LoyaltyAccounts
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateYesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateYesterday, queryDateYesterday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_LoyaltyAccounts_7Days : TransactionDocumentEndpoint_LoyaltyAccounts
        {
            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit, string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                var queryEndDate = DateTime.Today.ToString("yyyy-MM-dd") +
                                   "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);


                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }

        private class TransactionDocumentEndpoint_TransactionItemTaxes : Endpoint
        {
            public override async Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
            {
                List<string> staticSchemaProperties = new List<string>()
                {
                    "tlogId",
                    "itemId",
                    "itemTaxId",
                    "itemTaxName",
                    "itemTaxType",
                    "itemTaxableAmount",
                    "itemTaxAmount",
                    "temTaxIsRefund",
                    "itemTaxPercent",
                    "itemTaxIsVoided",
                    "itemTaxSequenceNumber"
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
                        case ("itemId"):
                        case ("itemTaxId"):
                            property.IsKey = true;
                            property.TypeAtSource = "string";
                            property.Type = PropertyType.String;
                            break;
                        case ("itemTaxIsRefund"):
                        case ("itemTaxIsVoided"):
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

            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit, string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var hasMore = false;
                var endpoint = EndpointHelper.GetEndpointForSchema(schema);

                var currPage = 0;
                var currDayOffset = 0;
                uint recordCount = 0;
                var safeLimit = limit > 0 ? (int)limit : Int32.MaxValue;
                
                var queryDate = startDate;
                var queryEndDate = endDate;
                var degreeOfParallelism = Int32.Parse(await apiClient.GetDegreeOfParallelism());

                var readQuery =
                    JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);

                var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";

                var tempSiteList = await apiClient.GetSiteIds();
                var workingSiteList = tempSiteList.Replace(" ", "").Split(',');

                readQuery.TransactionCategories = new List<string>() {"SALE_OR_RETURN"};

                foreach (var site in workingSiteList)
                {
                    if (limit > 0 && recordCount >= limit)
                    {
                        break;
                    }
                    readQuery.SiteInfoIds = new List<string>() {site};
                    currDayOffset = 1;

                    do //while queryDate != queryEndDate
                    {
                        readQuery.DateWrapper.DateTime = DateTime.Parse(queryDate).AddDays(currDayOffset).ToString("yyyy-MM-dd") + "T00:00:00Z";
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
                                var pageContent = limit > 0
                                    ? objectResponseWrapper?.PageContent.Take(safeLimit-(int)recordCount)
                                    : objectResponseWrapper?.PageContent;

                                await pageContent.ParallelForEachAsync(async objectResponse =>
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

                                    TlogWrapper tLogResponseWrapper = new TlogWrapper();
                                    var tlogResponsePulledSuccessfully = false;
                                    var retryCount = 0; //note for future adjustments: it is common to see outages of 10min or more with NCR.
                                    while (!tlogResponsePulledSuccessfully && retryCount < 20)
                                    {
                                        try
                                        {
                                            tLogResponseWrapper =
                                                JsonConvert.DeserializeObject<TlogWrapper>(
                                                    await tlogResponse.Content.ReadAsStringAsync());
                                            tlogResponsePulledSuccessfully = true;
                                        }
                                        catch (Exception e)
                                        {
                                            retryCount++;
                                            Thread.Sleep(1000*60);
                                            tlogResponse = await apiClient.GetAsync(tlogPath);
                                        }
                                    }

                                    var tlogItemRecordMap = new Dictionary<string, object>();

                                    tlogItemRecordMap["tlogId"] = recordMap["tlogId"];
                                    
                                    var items = limit > 0
                                        ? tLogResponseWrapper.Tlog.Items.Take(safeLimit-(int)recordCount)
                                        : tLogResponseWrapper.Tlog.Items;
                                    
                                    foreach (var item in items)
                                    {
                                        tlogItemRecordMap["itemId"] = String.IsNullOrWhiteSpace(item.Id)
                                            ? "null"
                                            : item.Id;
                                        if (item.ItemTaxes == null)
                                        {
                                            tlogItemRecordMap["itemTaxSequenceNumber"] = "1";
                                            tlogItemRecordMap["itemTaxId"] = "0";
                                            tlogItemRecordMap["itemTaxName"] = "null";
                                            tlogItemRecordMap["itemTaxType"] = "null";
                                            tlogItemRecordMap["itemTaxableAmount"] = "0";
                                            tlogItemRecordMap["itemTaxAmount"] = "0";
                                            tlogItemRecordMap["itemTaxIsRefund"] = false;
                                            tlogItemRecordMap["itemTaxPercent"] = "0";
                                            tlogItemRecordMap["itemTaxIsVoided"] =false;
                                        }
                                        else
                                        {
                                            foreach (var tax in item.ItemTaxes)
                                            {
                                                tlogItemRecordMap["itemTaxSequenceNumber"] = tax.SequenceNumber ?? "";
                                                tlogItemRecordMap["itemTaxId"] = tax.Id ?? "";
                                                tlogItemRecordMap["itemTaxName"] = tax.Name ?? "";
                                                tlogItemRecordMap["itemTaxType"] = tax.TaxType ?? "";
                                                tlogItemRecordMap["itemTaxableAmount"] = tax.TaxableAmount.Amount ?? "";
                                                tlogItemRecordMap["itemTaxAmount"] = tax.Amount.Amount ?? "";
                                                tlogItemRecordMap["itemTaxIsRefund"] = tax.IsRefund;
                                                tlogItemRecordMap["itemTaxPercent"] = tax.TaxPercent ?? "";
                                                tlogItemRecordMap["itemTaxIsVoided"] = tax.IsVoided;
                                                
                                                recordCount++;
                                                if (recordCount > limit && limit > 0)
                                                {
                                                    hasMore = false;
                                                    break;
                                                }
                                                else
                                                {
                                                    returnRecords.Add(new Record
                                                    {
                                                        Action = Record.Types.Action.Upsert,
                                                        DataJson = JsonConvert.SerializeObject(tlogItemRecordMap)
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }, maxDegreeOfParallelism: degreeOfParallelism);
                                
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
                         } while (hasMore && (limit == 0 || recordCount < limit));
                    } while (DateTime.Compare(DateTime.Parse(readQuery.DateWrapper.DateTime), DateTime.Parse(queryEndDate)) < 0 && (limit == 0 || (int)recordCount < limit));
                }
            }
        }

        private class
            TransactionDocumentEndpoint_TransactionItemTaxes_Historical :
                TransactionDocumentEndpoint_TransactionItemTaxes
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = await apiClient.GetStartDate();
                var queryEndDate = await apiClient.GetEndDate();
                
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
        private class
            TransactionDocumentEndpoint_TransactionItemTaxes_Today :
                TransactionDocumentEndpoint_TransactionItemTaxes
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateToday = DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateToday, queryDateToday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
        private class
            TransactionDocumentEndpoint_TransactionItemTaxes_Yesterday :
                TransactionDocumentEndpoint_TransactionItemTaxes
        {
            public override async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit,
                string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryDateYesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryDateYesterday, queryDateYesterday, isDiscoverRead);

                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
        private class
            TransactionDocumentEndpoint_TransactionItemTaxes_7Days :
                TransactionDocumentEndpoint_TransactionItemTaxes
        {
            public async override IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit, string startDate = "", string endDate = "",
                bool isDiscoverRead = false)
            {
                var queryStartDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                var queryEndDate = DateTime.Today.ToString("yyyy-MM-dd") +
                                   "T00:00:00Z";
                var records = base.ReadRecordsAsync(apiClient, schema, limit, queryStartDate, queryEndDate, isDiscoverRead);


                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
        public static readonly Dictionary<string, Endpoint> TransactionDocumentEndpoints =
            new Dictionary<string, Endpoint>
            {
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
                },
                {
                    "TransactionItemTaxes_Today", new TransactionDocumentEndpoint_TransactionItemTaxes_Today
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionItemTaxes_Today",
                        Name = "TransactionItemTaxes_Today",
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
                    "TransactionItemTaxes_Yesterday", new TransactionDocumentEndpoint_TransactionItemTaxes_Yesterday
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionItemTaxes_Yesterday",
                        Name = "TransactionItemTaxes_Yesterday",
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
                    "TransactionItemTaxes_7Days", new TransactionDocumentEndpoint_TransactionItemTaxes_7Days
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionItemTaxes_7Days",
                        Name = "TransactionItemTaxes_7Days",
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
                    "TransactionItemTaxes_HistoricalFromDate", new TransactionDocumentEndpoint_TransactionItemTaxes_Historical
                    {
                        ShouldGetStaticSchema = true,
                        Id = "TransactionItemTaxes_HistoricalFromDate",
                        Name = "TransactionItemTaxes_HistoricalFromDate",
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