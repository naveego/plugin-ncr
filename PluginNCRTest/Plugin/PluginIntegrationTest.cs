using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginNCR.API.Read;
using PluginNCR.API.Utility;
using PluginNCR.DataContracts;
using PluginNCR.Helper;
using Xunit;
using Record = Naveego.Sdk.Plugins.Record;

namespace PluginNCRTest.Plugin
{
    public class PluginIntegrationTest
    {
        private Settings GetSettings(bool oAuth = false)
        {
            return new Settings()
                {
                    ProvUsername = @"",
                    ProvPassword = @"",
                    NepApplicationKey = @"",
                    NepOrganization = @"",
                    NepCorrelationId = @"",
                    QueryStartDate = "",
                    QueryEndDate = "",
                    SiteIDs = "",
                    AuthMethod = false,
                    SecretKey = "",
                    SharedKey = "",
                };
        }

        private ConnectRequest GetConnectSettings(bool oAuth = false)
        {
            var settings = GetSettings(oAuth);
            
            return new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(settings)
            };
        }

        private Schema GetTestSchema(string endpointId = null, string id = "test", string name = "test")
        {
            Endpoint endpoint = endpointId == null? EndpointHelper.GetEndpointForId("TransactionDocument_Tenders_7Days")
                : EndpointHelper.GetEndpointForId(endpointId);

            return new Schema
            {
                Id = id,
                Name = name,
                PublisherMetaJson = JsonConvert.SerializeObject(endpoint),
            };
        }

       

        [Fact]
        public async Task ConnectTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginNCR.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("", response.SettingsError);
            Assert.Equal("", response.ConnectionError);
            Assert.Equal("", response.OauthError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        

        [Fact]
        public async Task DiscoverSchemasAllTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginNCR.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Equal(12, response.Schemas.Count);
            //
             var schema = response.Schemas[0];
             Assert.Equal($"TransactionDocument_Tenders_HistoricalFromDate", schema.Id);
             Assert.Equal("TransactionDocument_Tenders_HistoricalFromDate", schema.Name);
            // Assert.Equal($"", schema.Query);
             Assert.Equal(10, schema.Sample.Count);
             Assert.Equal(9, schema.Properties.Count);
            //
             var property = schema.Properties[0];
             Assert.Equal("tlogId", property.Id);
             Assert.Equal("tlogId", property.Name);
             Assert.Equal("", property.Description);
             Assert.Equal(PropertyType.String, property.Type);
             Assert.True(property.IsKey);
             Assert.False(property.IsNullable);
            //
            // var schema2 = response.Schemas[1];
            // Assert.Equal($"Custom Name", schema2.Id);
            // Assert.Equal("Custom Name", schema2.Name);
            // Assert.Equal($"", schema2.Query);
            // Assert.Equal(10, schema2.Sample.Count);
            // Assert.Equal(17, schema2.Properties.Count);
            
            
            // var property2 = schema2.Properties[0];
            // Assert.Equal("field1", property2.Id);
            // Assert.Equal("field1", property2.Name);
            // Assert.Equal("", property2.Description);
            // Assert.Equal(PropertyType.String, property2.Type);
            // Assert.False(property2.IsKey);
            // Assert.True(property2.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasRefreshTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginNCR.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh =
                {
                    GetTestSchema("TransactionDocument_Yesterday")
                }
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Equal(1, response.Schemas.Count);
            //
             var schema = response.Schemas[0];
             Assert.Equal("test", schema.Id);
             Assert.Equal("test", schema.Name);
             Assert.Equal("", schema.Query);
             Assert.Equal(10, schema.Sample.Count);
             Assert.Equal(77, schema.Properties.Count);
            //
             var property = schema.Properties[0];
             Assert.Equal("tlogId", property.Id);
             Assert.Equal("tlogId", property.Name);
             Assert.Equal("", property.Description);
             Assert.Equal(PropertyType.String, property.Type);
             Assert.True(property.IsKey);
             Assert.False(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginNCR.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var schema = GetTestSchema("TransactionItemTaxes_Yesterday");

            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {schema}
            };

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();
            
            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            
            //NOTE - endpoint queries are based on live data and current date.
            //Assertations will be incorrect often

            
            Assert.Equal(7600, records.Count);

            //var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            //Assert.Equal("24ee9221-e0b8-45c4-ab05-3c4757cffe0f", record["tlogId"]);
            // Assert.Equal("False", record["isTrainingMode"]);
            // Assert.Equal("572", record["transactionNumber"]);
            
           
            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

       

        [Fact]
        public async Task ReadStreamLimitTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginNCR.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var schema = GetTestSchema("TransactionDocument_Yesterday");

            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {schema}
            };

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
                Limit = 1
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(1, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}