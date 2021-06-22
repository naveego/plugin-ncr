using System.Threading.Tasks;

namespace PluginHubspot.API.Factory
{
    public interface IApiAuthenticator
    {
        Task<string> GetToken();
        
    }
}