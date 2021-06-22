using PluginHubspot.Helper;

namespace PluginHubspot.API.Factory
{
    public interface IApiClientFactory
    {
        IApiClient CreateApiClient(Settings settings);
    }
}