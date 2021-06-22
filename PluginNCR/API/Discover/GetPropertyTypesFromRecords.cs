using System;
using System.Collections.Generic;
using System.Linq;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json.Linq;

namespace PluginHubspot.API.Discover
{
    public static partial class Discover
    {
        /// <summary>
        /// Gets the Naveego type from the provided records
        /// </summary>
        /// <param name="records"></param>
        /// <returns>The property type</returns>
        private static Dictionary<string, PropertyType> GetPropertyTypesFromRecords(
            List<Dictionary<string, object>> records)
        {
            try
            {
                // build up a dictionary of the count of each type for each property
                var discoveredTypes = new Dictionary<string, Dictionary<PropertyType, int>>();

                foreach (var record in records)
                {
                    foreach (var recordKey in record.Keys)
                    {
                        if (!discoveredTypes.ContainsKey(recordKey))
                        {
                            discoveredTypes.Add(recordKey, new Dictionary<PropertyType, int>
                            {
                                {PropertyType.String, 0},
                                {PropertyType.Bool, 0},
                                {PropertyType.Integer, 0},
                                {PropertyType.Float, 0},
                                {PropertyType.Json, 0},
                                {PropertyType.Datetime, 0}
                            });
                        }

                        var value = record[recordKey];

                        if (value == null)
                            continue;

                        switch (value)
                        {
                            case bool _:
                                discoveredTypes[recordKey][PropertyType.Bool]++;
                                break;
                            case long _:
                                discoveredTypes[recordKey][PropertyType.Integer]++;
                                break;
                            case double _:
                                discoveredTypes[recordKey][PropertyType.Float]++;
                                break;
                            case JToken _:
                                discoveredTypes[recordKey][PropertyType.Json]++;
                                break;
                            default:
                            {
                                if (DateTime.TryParse(value.ToString(), out DateTime d))
                                {
                                    discoveredTypes[recordKey][PropertyType.Datetime]++;
                                }
                                else
                                {
                                    discoveredTypes[recordKey][PropertyType.String]++;
                                }

                                break;
                            }
                        }
                    }
                }

                // return object
                var outTypes = new Dictionary<string, PropertyType>();

                // get the most frequent type of each property
                foreach (var typesDic in discoveredTypes)
                {
                    var type = typesDic.Value.First(x => x.Value == typesDic.Value.Values.Max()).Key;
                    outTypes.Add(typesDic.Key, type);
                }

                return outTypes;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }
    }
}