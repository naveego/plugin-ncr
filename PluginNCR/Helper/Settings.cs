using System;

namespace PluginHubspot.Helper
{
    public class Settings
    {
        public string ProvUsername { get; set; }
        public string ProvPassword { get; set; }
        public string NepOrganization { get; set; }
        public string QueryStartDate { get; set; }
        public string NepApplicationKey { get; set; }
        
        public string ContentType { get; set; }
        public string NepCorrelationId { get; set; }

        /// <summary>
        /// Validates the settings input object
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Validate()
        {
            if (String.IsNullOrEmpty(ProvUsername))
            {
                throw new Exception("the ProvUsername property must be set");
            }
            if (String.IsNullOrEmpty(ProvPassword))
            {
                throw new Exception("the ProvPassword property must be set");
            }
            if (String.IsNullOrEmpty(NepOrganization))
            {
                throw new Exception("the NepOrganization property must be set");
            }
            if (String.IsNullOrEmpty(NepCorrelationId))
            {
                throw new Exception("the NepCorrelationId property must be set");
            }
            if (String.IsNullOrEmpty(NepApplicationKey))
            {
                throw new Exception("the NepApplicationKey property must be set");
            }
            if (String.IsNullOrEmpty(QueryStartDate))
            {
                throw new Exception("the QueryStartDate property must be set");
            }
            else
            {
                if (DateTime.Parse(QueryStartDate) > DateTime.Today)
                {
                    throw new Exception("the QueryStartDate must be equal to or before today");
                }
            }
        }
    }
}