using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginNCR.API.Factory;
using PluginNCR.API.Utility.EndpointHelperEndpoints;
using PluginNCR.DataContracts;
using RestSharp;

namespace PluginNCR.API.Utility
{
    public static class EndpointHelper
    {
        private static readonly Dictionary<string, Endpoint> Endpoints = new Dictionary<string, Endpoint>();

        static EndpointHelper()
        {
             TransactionDocumentEndpointHelper.TransactionDocumentEndpoints.ToList().ForEach(x => Endpoints.TryAdd(x.Key, x.Value));
         
        }

        public static Dictionary<string, Endpoint> GetAllEndpoints()
        {
            return Endpoints;
        }

        public static Endpoint? GetEndpointForId(string id)
        {
            return Endpoints.ContainsKey(id) ? Endpoints[id] : null;
        }

        public static Endpoint? GetEndpointForSchema(Schema schema)
        {
            var endpointMetaJson = JsonConvert.DeserializeObject<dynamic>(schema.PublisherMetaJson);
            string endpointId = endpointMetaJson.Id;
            return GetEndpointForId(endpointId);
        }
    }

    public abstract class Endpoint
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string PropertiesPath { get; set; } = "";
        public string PropertiesQuery { get; set; } = "";
        public string ReadQuery { get; set; } = "";
        public string BasePath { get; set; } = "";
        public string AllPath { get; set; } = "";
        public Method Method { get; set; }
        public string? DetailPath { get; set; }
        public string? DetailPropertyId { get; set; }
        public List<string> PropertyKeys { get; set; } = new List<string>();

        public virtual bool ShouldGetStaticSchema { get; set; } = false;

        protected virtual string WritePathPropertyId { get; set; } = "hs_unique_creation_key";

        protected virtual List<string> RequiredWritePropertyIds { get; set; } = new List<string>
        {
            // "hs_unique_creation_key"
        };

        public List<EndpointActions> SupportedActions { get; set; } = new List<EndpointActions>();

        public virtual Task<Count> GetCountOfRecords(IApiClient apiClient)
        {
            return Task.FromResult(new Count
            {
                Kind = Count.Types.Kind.Unavailable,
            });
        }

        public virtual async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, bool isDiscoverRead = false)
        {
            var hasMore = false;
            bool collectFromDate = false;
            
            var endpoint = EndpointHelper.GetEndpointForSchema(schema);

            var currPage = 0;
            
            var readQuery =
                JsonConvert.DeserializeObject<PostBody>(endpoint.ReadQuery);
            
            var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
            
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
            // do
            // {
            //     readQuery.PageNumber = currPage;
            //     
            //     var json = new StringContent(
            //         //endpoint.ReadQuery.Replace("\"pageNumber\":0", $"\"pageNumber\":{currPage}"),
            //         JsonConvert.SerializeObject(readQuery),
            //         Encoding.UTF8,
            //         "application/json"
            //     );
            //     var path = $"{BasePath.TrimEnd('/')}/{AllPath.TrimStart('/')}";
            //
            //     var response = await apiClient.PostAsync(
            //         path
            //         , json);
            //
            //     if (!response.IsSuccessStatusCode)
            //     {
            //         var error = JsonConvert.DeserializeObject<ApiError>(await response.Content.ReadAsStringAsync());
            //         throw new Exception(error.Message);
            //     }
            //
            //     var objectResponseWrapper =
            //         JsonConvert.DeserializeObject<ObjectResponseWrapper>(
            //             await response.Content.ReadAsStringAsync());
            //
            //    
            //
            //     if (objectResponseWrapper?.PageContent.Count == 0)
            //     {
            //         yield break;
            //     }
            //
            //     foreach (var objectResponse in objectResponseWrapper?.PageContent)
            //     {
            //         var recordMap = new Dictionary<string, object>();
            //
            //         //foreach (var objectProperty in objectResponse.Properties)
            //         foreach (var objectProperty in objectResponse)
            //         {
            //             try
            //             {
            //                 recordMap[objectProperty.Key] = objectProperty.Value.ToString() ?? "";
            //             }
            //             catch
            //             {
            //                 recordMap[objectProperty.Key] = "";
            //             }
            //
            //         }
            //
            //         yield return new Record
            //         {
            //             Action = Record.Types.Action.Upsert,
            //             DataJson = JsonConvert.SerializeObject(recordMap)
            //         };
            //     }
            //
            //     if (objectResponseWrapper.LastPage.ToLower() == "true" || currPage >= 9)
            //     {
            //         hasMore = false;
            //     }
            //     else
            //     {
            //         currPage++;
            //         hasMore = true;
            //     }
            // } while (hasMore);
            
        }

        public virtual async Task<string> WriteRecordAsync(IApiClient apiClient, Schema schema, Record record,
            IServerStreamWriter<RecordAck> responseStream)
        {
             var recordMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson);

            foreach (var requiredPropertyId in RequiredWritePropertyIds)
            {
                if (!recordMap.ContainsKey(requiredPropertyId))
                {
                    var errorMessage = $"Record did not contain required property {requiredPropertyId}";
                    var errorAck = new RecordAck
                    {
                        CorrelationId = record.CorrelationId,
                        Error = errorMessage
                    };
                    await responseStream.WriteAsync(errorAck);

                    return errorMessage;
                }

                if (recordMap.ContainsKey(requiredPropertyId) && recordMap[requiredPropertyId] == null)
                {
                    var errorMessage = $"Required property {requiredPropertyId} was NULL";
                    var errorAck = new RecordAck
                    {
                        CorrelationId = record.CorrelationId,
                        Error = errorMessage
                    };
                    await responseStream.WriteAsync(errorAck);

                    return errorMessage;
                }
            }
            
            var postObject = new Dictionary<string, object>();

            foreach (var property in schema.Properties)
            {
                object value = "";

                var propertyMetaJson = JsonConvert.DeserializeObject<PropertyMetaJson>(property.PublisherMetaJson);
                var readOnlyProperty = propertyMetaJson?.ModificationMetaData?.ReadOnlyValue ?? false;

                if (propertyMetaJson.Calculated || propertyMetaJson.IsKey || readOnlyProperty || !recordMap.ContainsKey(property.Id))
                {
                    continue;
                }
                
                if (recordMap.ContainsKey(property.Id))
                {
                    value = recordMap[property.Id];
                }

                postObject.TryAdd(property.Id, value);
            }

            var postObjectWrapper = new UpsertObjectWrapper
            {
                Properties = postObject
            };

            var objstr = JsonConvert.SerializeObject(postObjectWrapper);

            var json = new StringContent(
                JsonConvert.SerializeObject(postObjectWrapper),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response;

            if (!recordMap.ContainsKey(WritePathPropertyId) || recordMap.ContainsKey(WritePathPropertyId) &&
                recordMap[WritePathPropertyId] == null)
            {
                response =
                    await apiClient.PostAsync($"{BasePath.TrimEnd('/')}", json);
            }
            else
            {
                response =
                    await apiClient.PatchAsync($"{BasePath.TrimEnd('/')}/{recordMap[WritePathPropertyId]}", json);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                var errorAck = new RecordAck
                {
                    CorrelationId = record.CorrelationId,
                    Error = errorMessage
                };
                await responseStream.WriteAsync(errorAck);

                return errorMessage;
            }

            var ack = new RecordAck
            {
                CorrelationId = record.CorrelationId,
                Error = ""
            };
            await responseStream.WriteAsync(ack);

            return "";
        }

        public virtual Task<Schema> GetStaticSchemaAsync(IApiClient apiClient, Schema schema)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> IsCustomProperty(IApiClient apiClient, string propertyId)
        {
            return Task.FromResult(false);
        }

        public Schema.Types.DataFlowDirection GetDataFlowDirection()
        {
            // if (CanRead() && CanWrite())
            // {
            //     return Schema.Types.DataFlowDirection.ReadWrite;
            // }
            //
            // if (CanRead() && !CanWrite())
            // {
            //     return Schema.Types.DataFlowDirection.Read;
            // }
            //
            // if (!CanRead() && CanWrite())
            // {
            //     return Schema.Types.DataFlowDirection.Write;
            // }

            return Schema.Types.DataFlowDirection.Read;
        }


        private bool CanRead()
        {
            return SupportedActions.Contains(EndpointActions.Get);
        }

        private bool CanWrite()
        {
            return SupportedActions.Contains(EndpointActions.Post) ||
                   SupportedActions.Contains(EndpointActions.Put) ||
                   SupportedActions.Contains(EndpointActions.Delete);
        }
    }

    public enum EndpointActions
    {
        Get,
        Post,
        Put,
        Delete
    }
}