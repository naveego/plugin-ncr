using System;

namespace PluginHubspot.Helper
{
    public class Settings
    {
        public string ProvUsername { get; set; }
        public string ProvPassword { get; set; }
        public string NepApplicationKey { get; set; }
        public string NepOrganization { get; set; }
        public string QueryStartDate { get; set; }
        //public string SecretKey { get; set; }
       // public string SharedKey { get; set; }
        //public string NepEnterpriseUnit { get; set; }
        //public string NepAccessKey { get; set; }
        
        public string ContentType { get; set; }
        public string NepCorrelationId { get; set; }
        //public string Json { get; set; }

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
            // if (String.IsNullOrEmpty(NepApplicationKey))
            // {
            //     throw new Exception("the NepApplicationKey property must be set");
            // }
            if (String.IsNullOrEmpty(NepOrganization))
            {
                throw new Exception("the NepOrganization property must be set");
            }
            // if (String.IsNullOrEmpty(SecretKey))
            // {
            //     throw new Exception("the SecretKey property must be set");
            // }
            // if (String.IsNullOrEmpty(SharedKey))
            // {
            //     throw new Exception("the SharedKey property must be set");
            // }
            // if (String.IsNullOrEmpty(NepEnterpriseUnit))
            // {
            //     throw new Exception("the NepEnterpriseUnit property must be set");
            // }
            
        }
    }
}