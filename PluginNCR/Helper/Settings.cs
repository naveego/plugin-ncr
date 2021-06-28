using System;
using System.Text.RegularExpressions;

namespace PluginNCR.Helper
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
            
            Regex dateValidationRgx = new Regex(@"^(19|20)\d\d-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])$");

            if (!dateValidationRgx.IsMatch(QueryStartDate))
            {
                throw new Exception("the QueryStartDate property must match yyyy-MM-dd format");
            }
            if (DateTime.Parse(QueryStartDate) > DateTime.Today)
            {
                throw new Exception("the QueryStartDate must be equal to or before today");
            }
        }
    }
}