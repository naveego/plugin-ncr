using System.Threading.Tasks;

namespace PluginNCR.API.Factory
{
    public interface IApiAuthenticator
    {
        Task<string> GetToken();
    }
}