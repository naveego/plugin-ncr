using System;
using System.Collections.Generic;
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
        public string NepCorrelationId { get; set; }
        public string SiteIDs { get; set; }

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

            if (String.IsNullOrEmpty(SiteIDs))
            {
                throw new Exception("the SiteIDs property must be set");
            }

            string[] siteIds;
            try
            {
                siteIds = SiteIDs.Replace(" ", "").Split(',');
            }
            catch(Exception e)
            {
                throw new Exception("Error forming CSV of Site IDs. Error: " + e.Message);
            }

            if (siteIds.Length <= 0)
            {
                throw new Exception("Error: empty csv of site IDs found. Example value: 2304, 2315, 2340");
            }
            
            Regex dateValidationRgx = new Regex(@"^(19|20)\d\d-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])$");

            if (!dateValidationRgx.IsMatch(QueryStartDate))
            {
                throw new Exception("the QueryStartDate property must match yyyy-MM-dd format");
            }
            if (DateTime.Compare(DateTime.Parse(QueryStartDate), DateTime.Today) > 0)
            {
                throw new Exception("the QueryStartDate must be equal to or before today");
            }
        }
    }
}