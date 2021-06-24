using PluginNCR.Helper;

namespace PluginNCR.API.Factory
{
    public interface IApiClientFactory
    {
        IApiClient CreateApiClient(Settings settings);
    }
}