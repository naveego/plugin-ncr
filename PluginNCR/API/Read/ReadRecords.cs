using System.Collections.Generic;
using System.Linq;
using Naveego.Sdk.Plugins;
using PluginNCR.API.Factory;
using PluginNCR.API.Utility;

namespace PluginNCR.API.Read
{
    public static partial class Read
    {
        public static async IAsyncEnumerable<Record> ReadRecordsAsync(IApiClient apiClient, Schema schema, int limit)
        {
            var endpoint = EndpointHelper.GetEndpointForSchema(schema);
            if (endpoint == null) yield break;

            var records = endpoint.ReadRecordsAsync(apiClient, schema, limit);
            if (records != null)
            {
                await foreach (var record in records)
                    yield return record;
            }
        }
    }
}