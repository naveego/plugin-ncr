using Newtonsoft.Json;

namespace PluginHubspot.DataContracts
{
    public class PropertyMetaJson
    {
        [JsonProperty("hasUniqueValue")]
        public bool IsKey { get; set; }
        
        [JsonProperty("calculated")]
        public bool Calculated { get; set; }
        
        [JsonProperty("modificationMetadata")]
        public ModificationMetaData ModificationMetaData { get; set; }
    }

    public class ModificationMetaData
    {
        [JsonProperty("archivable")]
        public bool Archivable { get; set; }
        
        [JsonProperty("readOnlyDefinition")]
        public bool ReadOnlyDefinition { get; set; }
        
        [JsonProperty("readOnlyValue")]
        public bool ReadOnlyValue { get; set; }
    }
}