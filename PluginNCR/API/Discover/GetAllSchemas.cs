using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginNCR.API.Factory;
using PluginNCR.API.Utility;
using PluginNCR.DataContracts;
using PluginNCR.Helper;

namespace PluginNCR.API.Discover
{
    public static partial class Discover
    {
        public static async IAsyncEnumerable<Schema> GetAllSchemas(IApiClient apiClient, Settings settings,
            int sampleSize = 5)
        {
            var allEndpoints = EndpointHelper.GetAllEndpoints();

            foreach (var endpoint in allEndpoints.Values)
            {
                // base schema to be added to
                var schema = new Schema
                {
                    Id = endpoint.Id,
                    Name = endpoint.Name,
                    Description = "",
                    PublisherMetaJson = JsonConvert.SerializeObject(endpoint),
                    DataFlowDirection = endpoint.GetDataFlowDirection()
                };

                schema = await GetSchemaForEndpoint(apiClient, schema, endpoint);

                // get sample and count
                yield return await AddSampleAndCount(apiClient, schema, sampleSize, endpoint);
            }
        }

        private static async Task<Schema> AddSampleAndCount(IApiClient apiClient, Schema schema,
            int sampleSize, Endpoint? endpoint)
        {
            if (endpoint == null)
            {
                return schema;
            }

            //No sampling
            // add sample and count
            // var records = Read.Read.ReadRecordsAsync(apiClient, schema, sampleSize);
            // schema.Sample.AddRange(await records.ToListAsync());
            // schema.Count = await GetCountOfRecords(apiClient, endpoint);
            
             return schema;
        }

        private static async Task<Schema> GetSchemaForEndpoint(IApiClient apiClient, Schema schema, Endpoint? endpoint)
        {
            if (endpoint == null)
            {
                return schema;
            }

            if (endpoint.ShouldGetStaticSchema)
            {
                return await endpoint.GetStaticSchemaAsync(apiClient, schema);
            }

            var query = endpoint.PropertiesQuery;
            
            StringContent propertiesQuery = new StringContent(query, Encoding.UTF8, "application/json");
            // invoke properties api

            
            var response = await apiClient.SendAsync(Constants.BaseApiUrl + endpoint.PropertiesPath, propertiesQuery);

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonConvert.DeserializeObject<ApiError>(await response.Content.ReadAsStringAsync());
                throw new Exception(error.Message);
            }

            var objectPropertiesResponse =
                JsonConvert.DeserializeObject<PropertyResponseWrapper>(
                    await response.Content.ReadAsStringAsync());

            var properties = new List<Property>();
            
            foreach (var srcProperty in objectPropertiesResponse.PageContent[0].GetType().GetProperties())
            {
                var property = new Property();

                property.Id = srcProperty.Name;
                property.Name = srcProperty.Name;
                property.Type = GetPropertyType(srcProperty.PropertyType.ToString());
                property.TypeAtSource = srcProperty.PropertyType.ToString();
                
                
                switch (property.Name.ToLower())
                {
                    case ("tlogid"):
                    case ("touchpointid"):
                    case ("siteinfood"):
                    case ("employeeids"):
                        property.IsKey = true;
                        break;
                }
                
                properties.Add(property);
            }
            
            schema.Properties.Clear();
            schema.Properties.AddRange(properties);

            if (schema.Properties.Count == 0)
            {
                schema.Description = Constants.EmptySchemaDescription;
            }

            schema.DataFlowDirection = endpoint.GetDataFlowDirection();

            return schema;
        }
    }
}