using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using PluginNCR.API.Factory;
using PluginNCR.API.Utility;

namespace PluginNCR.API.Discover
{
    public static partial class Discover
    {
        public static Task<Count> GetCountOfRecords(IApiClient apiClient, Endpoint? endpoint)
        {
            return endpoint != null
                ? endpoint.GetCountOfRecords(apiClient)
                : Task.FromResult(new Count {Kind = Count.Types.Kind.Unavailable});
        }
    }
}