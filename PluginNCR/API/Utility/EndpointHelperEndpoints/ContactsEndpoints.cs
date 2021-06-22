using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginHubspot.API.Factory;
using PluginHubspot.DataContracts;

namespace PluginHubspot.API.Utility.EndpointHelperEndpoints
{
    public class ContactsEndpointHelper
    {
        private class ContactsEndpoint : Endpoint
        {
        }

        public static readonly Dictionary<string, Endpoint> ContactsEndpoints = new Dictionary<string, Endpoint>
        {
            {
                "AllContacts", new ContactsEndpoint
                {
                    Id = "AllContacts",
                    Name = "All Contacts",
                    BasePath = "/crm/v3/",
                    AllPath = "/objects/contacts",
                    PropertiesPath = "/crm/v3/properties/contacts",
                    DetailPath = "/objects/contacts/{0}",
                    DetailPropertyId = "hs_unique_creation_key",
                    SupportedActions = new List<EndpointActions>
                    {
                        EndpointActions.Get
                    },
                    PropertyKeys = new List<string>
                    {
                        "hs_unique_creation_key"
                    }
                }
            },
            {
                "UpsertContacts", new ContactsEndpoint
                {
                    Id = "UpsertContacts",
                    Name = "Upsert Contacts",
                    BasePath = "/crm/v3/objects/contacts",
                    AllPath = "/",
                    PropertiesPath = "/crm/v3/properties/contacts",
                    DetailPath = "/",
                    DetailPropertyId = "hs_unique_creation_key",
                    SupportedActions = new List<EndpointActions>
                    {
                        EndpointActions.Post,
                        EndpointActions.Put
                    },
                    PropertyKeys = new List<string>
                    {
                        "hs_unique_creation_key"
                    }
                }
            },
        };
    }
}