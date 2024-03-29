using System.Collections.Generic;
using Aunalytics.Sdk.Plugins;
using PluginNCR.API.Factory;
using PluginNCR.API.Utility;

namespace PluginNCR.API.Read
{
    public static partial class Read
    {
        public static async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit)
        {
            var endpoint = EndpointHelper.GetEndpointForSchema(schema);
            var records = endpoint?.ReadRecordsAsync(apiClient, schema, limit);
            if (records != null)
            {
                await foreach (var record in records)
                {
                    yield return record;
                }
            }
        }
    }
}