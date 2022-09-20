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
        public string QueryEndDate { get; set; }
        public string NepApplicationKey { get; set; }
        public string NepCorrelationId { get; set; }
        public string SiteIDs { get; set; }
        public string SecretKey { get; set; }
        public string SharedKey { get; set; }
        public string AuthMethod { get; set; }
        public string DegreeOfParallelism { get; set; }

        /// <summary>
        /// Validates the settings input object
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Validate()
        {
            if(AuthMethod == "Credentials") //use credentials
            {
                if (String.IsNullOrEmpty(ProvUsername))
                {
                    throw new Exception("the ProvUsername property must be set");
                }

                if (String.IsNullOrEmpty(ProvPassword))
                {
                    throw new Exception("the ProvPassword property must be set");
                }
            }

            else if (AuthMethod == "Access Key") //use key
            {
                if (String.IsNullOrEmpty(SecretKey))
                {
                    throw new Exception("the SecretKey property must be set");
                } 
                if (String.IsNullOrEmpty(SharedKey))
                {
                    throw new Exception("the SharedKey property must be set");
                } 
            }
            else
            {
                throw new Exception("the Auth Method property must be set");
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
            if (!string.IsNullOrWhiteSpace(QueryEndDate))
            {
                if (!dateValidationRgx.IsMatch(QueryEndDate))
                {
                    throw new Exception("the QueryEndDate property must match yyyy-MM-dd format");
                }
                
                if (DateTime.Compare(DateTime.Parse(QueryEndDate), DateTime.Parse(QueryStartDate)) == -1)
                {
                    throw new Exception("the QueryEndDate must be equal to or after QueryStartDate");
                }
            }

            try
            {
                if (Int32.Parse(DegreeOfParallelism) <= 0)
                {
                    throw new Exception("degree of parallelism must be a positive integer value");
                }
            }
            catch (Exception e)
            {
                throw new Exception("unable to parse given degree of parallelism. This should be a single number greater than zero");
            }
            
        }
    }
}