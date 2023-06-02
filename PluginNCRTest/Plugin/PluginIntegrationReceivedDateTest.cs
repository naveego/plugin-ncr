using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Aunalytics.Sdk.Plugins;
using Xunit;
using Record = Aunalytics.Sdk.Plugins.Record;

namespace PluginNCRTest.Plugin
{
    public class PluginIntegrationReceivedDateTest : PluginIntegrationTest
    {
        [Fact]
        public async Task DiscoverSchemasAllReceivedTest()
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
            Assert.Equal(32, response.Schemas.Count);

            var schema = response.Schemas.FirstOrDefault(s => s.Id == "TransactionDocument_Tenders_HistoricalFromDate_ReceivedDate");
            Assert.NotNull(schema);
            Assert.Equal("TransactionDocument_Tenders_HistoricalFromDate_ReceivedDate", schema.Name);
            Assert.Equal($"", schema.Query);
            // // TESTING: Uncomment
            //Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(10, schema.Properties.Count);

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
        public async Task DiscoverSchemasRefreshReceivedTest()
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
                    GetTestSchema("TransactionDocument_Yesterday_ReceivedDate")
                }
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Equal(1, response.Schemas.Count);

            var schema = response.Schemas[0];
            Assert.Equal("test", schema.Id);
            Assert.Equal("test", schema.Name);
            Assert.Equal("", schema.Query);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(87, schema.Properties.Count);

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
        public async Task ReadStreamReceivedTest()
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

            var schema = GetTestSchema("TransactionDocument_Yesterday_ReceivedDate");

            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {schema},
                SampleSize = 10
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
            Assert.Equal(3107, records.Count);

            //var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            //Assert.Equal("24ee9221-e0b8-45c4-ab05-3c4757cffe0f", record["tlogId"]);
            // Assert.Equal("False", record["isTrainingMode"]);
            // Assert.Equal("572", record["transactionNumber"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamLimitReceivedTest()
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

            var schema = GetTestSchema("TransactionDocument_Yesterday_ReceivedDate");

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
                Limit = 25
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
            Assert.Equal(25, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}