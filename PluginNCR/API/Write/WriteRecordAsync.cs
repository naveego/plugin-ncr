using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginHubspot.API.Factory;
using PluginHubspot.API.Utility;

namespace PluginHubspot.API.Write
{
    public static partial class Write
    {
        private static readonly SemaphoreSlim WriteSemaphoreSlim = new SemaphoreSlim(1, 1);

        public static async Task<string> WriteRecordAsync(IApiClient apiClient, Schema schema, Record record,
            IServerStreamWriter<RecordAck> responseStream)
        {
            throw new NotImplementedException("Write not currently supported.");
           
        }
    }
}