using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginHubspot.DataContracts
{
    public class ObjectResponseWrapper
    {
        [JsonProperty("lastPage")]
        public string LastPage { get; set; }
        
        [JsonProperty("pageNumber")]
        public string PageNumber { get; set; }
        
        [JsonProperty("totalPages")]
        public string TotalPages { get; set; }
        
        [JsonProperty("totalResults")]
        public string TotalResults { get; set; }
            
        [JsonProperty("pageContent")]
        public List<Dictionary<string, object>> PageContent { get; set; }
        //public List<ObjectResponse> PageContent { get; set; }
        
        // [JsonProperty("paging")]
        // public PagingResponse Paging { get; set; }
    }

    public class PostBody
    {
        [JsonProperty("businessDay")]
        public BusinessDay BusinessDay { get; set; }
        
        [JsonProperty("siteInfoIds")]
        public List<string> SiteInfoIds { get; set; }
        
        [JsonProperty("pageSize")]
        public string PageSize { get; set; }
        
        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }
    }

    public class ObjectResponse
    {
        [JsonProperty("tlogId")]
        public string TlogId { get; set; }
        
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }


    public class PropertyResponseWrapper
    {
        [JsonProperty("pageContent")]
        public List<PropertyResponse> PageContent { get; set; }
        
        [JsonProperty("lastPage")]
        public Boolean LastPage { get; set; }
    }

    public class PropertyResponse
    {
        [JsonProperty("transactionType")]
        public string TransactionType { get; set; }
        
        [JsonProperty("businessDayUtc")]
        public DateTime BusinessDayUtc { get; set; }
        
        [JsonProperty("employeeIds")]
        public List<string> EmployeeIds { get; set; }
        
        [JsonProperty("employeeNames")]
        public List<string> EmployeeNames { get; set; }
        
        [JsonProperty("endTransactionDateTimeUtc")]
        public DateTime EndTransactionDateTimeUtc { get; set; }
        
        [JsonProperty("closeDateTimeUtc")]
        public DateTime CloseDateTimeUtc { get; set; }
        
        [JsonProperty("grandAmount")]
        public double GrandAmount { get; set; }
        
        [JsonProperty("siteInfoId")]
        public string SiteInfoId { get; set; }
        
        [JsonProperty("tlogId")]
        public string TLogId { get; set; }
        
        [JsonProperty("touchPointId")]
        public string TouchPointId { get; set; }
        
        [JsonProperty("isTrainingMode")]
        public bool IsTrainingMode { get; set; }
        
        [JsonProperty("transactionNumber")]
        public string TransactionNumber { get; set; }
        
        [JsonProperty("isVoided")]
        public bool IsVoided { get; set; }
        
        [JsonProperty("isSuspended")]
        public bool IsSuspended { get; set; }
        
        [JsonProperty("itemCount")]
        public int ItemCount { get; set; }
        
        [JsonProperty("transactionCategory")]
        public string TransactionCategory { get; set; }
        
        [JsonProperty("receivedDateTimeUtc")]
        public DateTime ReceivedDateTimeUtc { get; set; }
    }

    public partial class CanonicalTransaction
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("modelVersion")]
        public long ModelVersion { get; set; }

        [JsonProperty("siteInfo")]
        public SiteInfo SiteInfo { get; set; }

        [JsonProperty("transactionNumber")]
        public long TransactionNumber { get; set; }

        [JsonProperty("openDateTimeUtc")]
        public CloseDateTimeUtc OpenDateTimeUtc { get; set; }

        [JsonProperty("documentExpirationDate")]
        public CloseDateTimeUtc DocumentExpirationDate { get; set; }

        [JsonProperty("closeDateTimeUtc")]
        public CloseDateTimeUtc CloseDateTimeUtc { get; set; }

        [JsonProperty("touchPointId")]
        public string TouchPointId { get; set; }

        [JsonProperty("touchPointType")]
        public string TouchPointType { get; set; }

        [JsonProperty("touchPointGroup")]
        public string TouchPointGroup { get; set; }

        [JsonProperty("dataProviderName")]
        public string DataProviderName { get; set; }

        [JsonProperty("dataProviderVersion")]
        public string DataProviderVersion { get; set; }

        [JsonProperty("businessDay")]
        public BusinessDay BusinessDay { get; set; }

        [JsonProperty("isTrainingMode")]
        public bool IsTrainingMode { get; set; }

        [JsonProperty("linkedTransactions")]
        public List<object> LinkedTransactions { get; set; }

        [JsonProperty("tlog")]
        public Tlog Tlog { get; set; }

        [JsonProperty("transactionCategory")]
        public string TransactionCategory { get; set; }

        [JsonProperty("isUpdated")]
        public bool IsUpdated { get; set; }

        [JsonProperty("transactionVersion")]
        public long TransactionVersion { get; set; }
    }

    public partial class BusinessDay
    {
        [JsonProperty("dateTime")]
        public string DateTime { get; set; }
    }

    public partial class CloseDateTimeUtc
    {
        [JsonProperty("dateTime")]
        public DateTimeOffset DateTime { get; set; }

        [JsonProperty("originalOffset")]
        public string OriginalOffset { get; set; }
    }

    public partial class SiteInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }

    public partial class Tlog
    {
        [JsonProperty("customProperties")]
        public CustomProperties CustomProperties { get; set; }
    }

    public partial class CustomProperties
    {
        [JsonProperty("emeraldSettlementAccounts")]
        public List<EmeraldSettlementAccount> EmeraldSettlementAccounts { get; set; }
    }

    public partial class EmeraldSettlementAccount
    {
        [JsonProperty("tenderTotals")]
        public List<Total> TenderTotals { get; set; }

        [JsonProperty("totals")]
        public List<Total> Totals { get; set; }

        [JsonProperty("accountTotals")]
        public bool AccountTotals { get; set; }

        [JsonProperty("period")]
        public Period Period { get; set; }

        [JsonProperty("account")]
        public Account Account { get; set; }

        [JsonProperty("approvalReason", NullValueHandling = NullValueHandling.Ignore)]
        public string ApprovalReason { get; set; }
    }

    public partial class Account
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }

    public partial class Period
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("startDateTime")]
        public BusinessDay StartDateTime { get; set; }

        [JsonProperty("endDateTime")]
        public BusinessDay EndDateTime { get; set; }
    }

    public partial class Total
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("tenderId", NullValueHandling = NullValueHandling.Ignore)]
        public long? TenderId { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public long? Count { get; set; }
    }
    // public class NextResponse
    // {
    //     [JsonProperty("after")]
    //     public string After { get; set; }
    //     
    //     [JsonProperty("link")]
    //     public string Link { get; set; }
    // }
}