using System;
using System.Threading.Tasks;

namespace PluginNCR.API.Factory
{
    public interface IApiAuthenticator
    {
        //Task<string> GetToken(DateTimeOffset date);
        Task<string> GetNewToken(DateTimeOffset date, string uri, string method);
    }
}