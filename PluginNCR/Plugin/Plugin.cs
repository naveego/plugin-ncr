using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginHubspot.API.Discover;
using PluginHubspot.API.Factory;
using PluginHubspot.API.Read;
using PluginHubspot.API.Write;
using PluginHubspot.DataContracts;
using PluginHubspot.Helper;

namespace PluginHubspot.Plugin
{
    public class Plugin : Publisher.PublisherBase
    {
        private readonly ServerStatus _server;
        private TaskCompletionSource<bool> _tcs;
        private readonly IApiClientFactory _apiClientFactory;
        private IApiClient _apiClient;

        public Plugin(HttpClient client = null)
        {
            _apiClientFactory = new ApiClientFactory(client ?? new HttpClient());
            _server = new ServerStatus
            {
                Connected = false,
                WriteConfigured = false
            };
        }
        
        /// <summary>
        /// Configures the plugin
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureResponse> Configure(ConfigureRequest request, ServerCallContext context)
        {
            Logger.Debug("Got configure request");
            Logger.Debug(JsonConvert.SerializeObject(request, Formatting.Indented));
            
            // ensure all directories are created
            Directory.CreateDirectory(request.TemporaryDirectory);
            Directory.CreateDirectory(request.PermanentDirectory);
            Directory.CreateDirectory(request.LogDirectory);
            
            // configure logger
            Logger.SetLogLevel(request.LogLevel);
            Logger.Init(request.LogDirectory);

            _server.Config = request;

            return Task.FromResult(new ConfigureResponse());
        }
        

        /// <summary>
        /// Establishes a connection with Campaigner.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>A message indicating connection success</returns>
        public override async Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            // for setting the log level
            // Logger.SetLogLevel(Logger.LogLevel.Debug);

            Logger.SetLogPrefix("connect");
            
            // validate settings passed in
            try
            {
                _server.Settings = JsonConvert.DeserializeObject<Settings>(request.SettingsJson);
                
                
                _server.Settings.Validate();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = "",
                    OauthError = "",
                    SettingsError = e.Message
                };
            }

            // get api client
            try
            {
                _apiClient = _apiClientFactory.CreateApiClient(_server.Settings);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = "",
                    OauthError = "",
                    SettingsError = e.Message
                };
            }

            // test cluster factory
            try
            {
                await _apiClient.TestConnection();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);

                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = e.Message,
                    OauthError = "",
                    SettingsError = ""
                };
            }

            _server.Connected = true;

            return new ConnectResponse
            {
                OauthStateJson = request.OauthStateJson,
                ConnectionError = "",
                OauthError = "",
                SettingsError = ""
            };
        }

        public override async Task ConnectSession(ConnectRequest request,
            IServerStreamWriter<ConnectResponse> responseStream, ServerCallContext context)
        {
            Logger.SetLogPrefix("connect_session");
            Logger.Info("Connecting session...");

            // create task to wait for disconnect to be called
            _tcs?.SetResult(true);
            _tcs = new TaskCompletionSource<bool>();

            // call connect method
            var response = await Connect(request, context);

            await responseStream.WriteAsync(response);

            Logger.Info("Session connected.");

            // wait for disconnect to be called
            await _tcs.Task;
        }


        /// <summary>
        /// Discovers schemas located in the users Campaigner instance
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>Discovered schemas</returns>
        public override async Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
            ServerCallContext context)
        {
            Logger.SetLogPrefix("discover");
            Logger.Info("Discovering Schemas...");

            var sampleSize = checked((int) request.SampleSize);

            DiscoverSchemasResponse discoverSchemasResponse = new DiscoverSchemasResponse();

            // only return requested schemas if refresh mode selected
            if (request.Mode == DiscoverSchemasRequest.Types.Mode.All)
            {
                // get all schemas
                try
                {
                    var schemas = Discover.GetAllSchemas(_apiClient, _server.Settings, sampleSize);

                    discoverSchemasResponse.Schemas.AddRange(await schemas.ToListAsync());

                    Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");

                    return discoverSchemasResponse;
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message, context);
                    return new DiscoverSchemasResponse();
                }
            }

            try
            {
                var refreshSchemas = request.ToRefresh;

                Logger.Info($"Refresh schemas attempted: {refreshSchemas.Count}");

                var schemas = Discover.GetRefreshSchemas(_apiClient, refreshSchemas, sampleSize);

                discoverSchemasResponse.Schemas.AddRange(await schemas.ToListAsync());

                // return all schemas 
                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
                return discoverSchemasResponse;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return new DiscoverSchemasResponse();
            }
        }

        /// <summary>
        /// Configures the plugin for a real time read
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureRealTimeResponse> ConfigureRealTime(ConfigureRealTimeRequest request, ServerCallContext context)
        {
            Logger.Info("Configuring real time...");
            
            var schemaJson = Read.GetSchemaJson();
            var uiJson = Read.GetUIJson();

            // if first call 
            if (string.IsNullOrWhiteSpace(request.Form.DataJson) || request.Form.DataJson == "{}")
            {
                return Task.FromResult(new ConfigureRealTimeResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        DataErrorsJson = "",
                        Errors = { },
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson,
                    }
                });
            }
            
            return Task.FromResult(new ConfigureRealTimeResponse
            {
                Form = new ConfigurationFormResponse
                {
                    DataJson = request.Form.DataJson,
                    DataErrorsJson = "",
                    Errors = { },
                    SchemaJson = schemaJson,
                    UiJson = uiJson,
                    StateJson = request.Form.StateJson,
                }
            });
        }
        
        /// <summary>
        /// Publishes a stream of data for a given schema
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ReadStream(ReadRequest request, IServerStreamWriter<Record> responseStream,
            ServerCallContext context)
        {
            try
            {
                var schema = request.Schema;
                var limit = request.Limit;
                var limitFlag = request.Limit != 0;
                var jobId = request.JobId;
                long recordsCount = 0;

                Logger.SetLogPrefix(jobId);
                
                Logger.Debug(JsonConvert.SerializeObject(request.RealTimeStateJson, Formatting.Indented));

                if (!string.IsNullOrWhiteSpace(request.RealTimeSettingsJson))
                {
                    recordsCount = await Read.ReadRecordsRealTimeAsync(_apiClient, request, responseStream, context);
                }
                else
                {
                    var records = Read.ReadRecordsAsync(_apiClient, schema);

                    await foreach (var record in records)
                    {
                        // stop publishing if the limit flag is enabled and the limit has been reached or the server is disconnected
                        if (limitFlag && recordsCount == limit || !_server.Connected)
                        {
                            break;
                        }

                        // publish record
                        await responseStream.WriteAsync(record);
                        recordsCount++;
                    }
                }
                
                Logger.Info($"Published {recordsCount} records");
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
            }
        }
        
        /// <summary>
        /// Prepares writeback settings to write to Campaigner
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<PrepareWriteResponse> PrepareWrite(PrepareWriteRequest request,
            ServerCallContext context)
        {
            Logger.SetLogPrefix(request.DataVersions.JobId);
            Logger.Info("Preparing write...");
            _server.WriteConfigured = false;

            _server.WriteSettings = new WriteSettings
            {
                CommitSLA = request.CommitSlaSeconds,
                Schema = request.Schema,
                Replication = request.Replication,
                DataVersions = request.DataVersions,
            };

            _server.WriteConfigured = true;

            Logger.Debug(JsonConvert.SerializeObject(_server.WriteSettings, Formatting.Indented));
            Logger.Info("Write prepared.");
            return new PrepareWriteResponse();
        }

        /// <summary>
        /// Writes records to Campaigner
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task WriteStream(IAsyncStreamReader<Record> requestStream,
            IServerStreamWriter<RecordAck> responseStream, ServerCallContext context)
        {
            try
            {
                Logger.Info("Writing records to Campaigner...");

                var schema = _server.WriteSettings.Schema;
                var inCount = 0;

                // get next record to publish while connected and configured
                while (await requestStream.MoveNext(context.CancellationToken) && _server.Connected &&
                       _server.WriteConfigured)
                {
                    var record = requestStream.Current;
                    inCount++;

                    Logger.Debug($"Got record: {record.DataJson}");

                    if (_server.WriteSettings.IsReplication())
                    {
                        throw new System.NotSupportedException();
                    }
                    else
                    {
                        // send record to source system
                        // add await for unit testing 
                        // removed to allow multiple to run at the same time
                        Task.Run(async () =>
                                await Write.WriteRecordAsync(_apiClient, schema, record, responseStream),
                            context.CancellationToken);
                    }
                }

                Logger.Info($"Wrote {inCount} records to Campaigner.");
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
            }
        }

        /// <summary>
        /// Handles disconnect requests from the agent
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<DisconnectResponse> Disconnect(DisconnectRequest request, ServerCallContext context)
        {
            // clear connection
            _server.Connected = false;
            _server.Settings = null;

            // alert connection session to close
            if (_tcs != null)
            {
                _tcs.SetResult(true);
                _tcs = null;
            }

            Logger.Info("Disconnected");
            return Task.FromResult(new DisconnectResponse());
        }
    }
}