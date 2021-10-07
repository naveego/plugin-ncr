using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginNCR.DataContracts
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
        
        [JsonProperty("transactionCategories")]
        public List<string> TransactionCategories { get; set; }
    }

    public class ObjectResponse
    {
        [JsonProperty("tlogId")]
        public string TlogId { get; set; }
        
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }


    public class PropertyResponseWrapper //Get Canonical Transaction
    {
        [JsonProperty("pageContent")]
        public List<PropertyResponse> PageContent { get; set; }
        
        [JsonProperty("lastPage")]
        public Boolean LastPage { get; set; }
    }

    public class PropertyResponse //Get Canonical Transaction
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

    

    public class BusinessDay
    {
        [JsonProperty("dateTime")]
        public string DateTime { get; set; }
    }

   

    public class TlogWrapper
    {
        [JsonProperty("transactionCategory")]
        public string TransactionCategory { get; set; }
        
        [JsonProperty("isUpdated")]
        public bool IsUpdated { get; set; }

        [JsonProperty("transactionVersion")] 
        public int TransactionVersion { get; set; }
        
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("siteInfo")]
        public SiteInfo SiteInfo { get; set; }
        
        [JsonProperty("businessDay")]
        public BusinessDay BusinessDay { get; set; }
        
        [JsonProperty("tlog")]
        public TLog Tlog { get; set; }
    }

    
    public class TLog
    {
        [JsonProperty("receiptId")]
        public string ReceiptId { get; set; }
        
        [JsonProperty("totalTaxes")]
        public List<TLogTotalTaxes> TotalTaxes { get; set; }
        
        [JsonProperty("tenders")]
        public List<TLogTenders> Tenders { get; set; }
        
        [JsonProperty("items")]
        public List<TLogItems> Items { get; set; }
        
        [JsonProperty("totals")]
        public TLogTotals TLogTotals { get; set; }
        
        [JsonProperty("employees")]
        public List<TLogEmployees> Employees { get; set; }
        
        [JsonProperty("touchPointGroup")]
        public string TouchPointGroup { get; set; }
        
        [JsonProperty("transactionNumber")]
        public string TransactionNumber { get; set; }
        
        [JsonProperty("isVoided")]
        public bool IsVoided { get; set; }
        
        [JsonProperty("isRecalled")]
        public bool IsRecalled { get; set; }
        
        [JsonProperty("isSuspended")]
        public bool IsSuspended { get; set; }
        
        [JsonProperty("coupons")]
        public List<object> Coupons { get; set; }
        
        [JsonProperty("customer")]
        public Customer Customer { get; set; }
    }

    public class Customer
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("entryMethod")]
        public string EntryMethod { get; set; }
        
        [JsonProperty("identifierData")]
        public string IdentifierData { get; set; }
        
        [JsonProperty("infoValidationMeans")]
        public string InfoValidationMeans { get; set; }
    }

    public class TLogTenders
    {
        [JsonProperty("tenderAmount")]
        public TLogTendersTenderAmount TenderAmount { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("usage")]
        public string Usage { get; set; }
        
        [JsonProperty("isVoided")]
        public bool IsVoided { get; set; }
        
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("typeLabel")]
        public string TypeLabel { get; set; }
        
        [JsonProperty("cardLastFourDigits")]
        public string CardLastFourDigits { get; set; }
    }

    public class TLogTendersTenderAmount
    {
        [JsonProperty("amount")]
        public float Amount { get; set; }
    }
    public class TLogTotals
    {
        [JsonProperty("discountAmount")]
        public TLogAmount DiscountAmount { get; set; }
        
        [JsonProperty("grandAmount")]
        public TLogAmount GrandAmount { get; set; }
    }
    public class TLogTotalTaxes
    {
        [JsonProperty("amount")]
        public TLogTotalTaxesAmount Amount { get; set; }
    }

    public class TLogTotalTaxesAmount
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }
    }
    public class TLogEmployees
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SiteInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class TLogItems
    {
        
        [JsonProperty("isItemNotOnFile")]
        public bool IsItemNotOnFile { get; set; }
        
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("orderNumber")]
        public string OrderNumber { get; set; }
        
        [JsonProperty("productName")]
        public string ProductName { get; set; }
        
        [JsonProperty("productId")]
        public string ProductId { get; set; }
        
        [JsonProperty("departmentId")]
        public string DepartmentId { get; set; }
        
        [JsonProperty("isReturn")]
        public bool IsReturn { get; set; }
        
        [JsonProperty("isVoided")]
        public bool IsVoided { get; set; }
        
        [JsonProperty("isOverridden")]
        public bool IsOverridden { get; set; }
        
        [JsonProperty("isNonSaleItem")]
        public bool IsNonSaleItem { get; set; }
        
        [JsonProperty("weightFromScale")]
        public bool WeightFromScale { get; set; }
        
        [JsonProperty("qtyIsWeight")]
        public bool QtyIsWeight { get; set; }
        
        [JsonProperty("qtyIsFuelGallons")]
        public bool qtyIsFuelGallons { get; set; }
        
        [JsonProperty("itemPromotions")]
        public List<Dictionary<string, object>> ItemPromotions { get; set; }
        
        [JsonProperty("itemDiscounts")]
        public List<TLogItemDiscounts> ItemDiscounts { get; set; }
        
        [JsonProperty("regularUnitPrice")]
        public TLogAmount RegularUnitPrice { get; set; }
        
        [JsonProperty("extendedUnitPrice")]
        public TLogAmount ExtendedUnitPrice { get; set; }
        
        [JsonProperty("extendedAmount")]
        public TLogAmount ExtendedAmount { get; set; }
        
        [JsonProperty("actualAmount")]
        public TLogAmount ActualAmount { get; set; }
        
        [JsonProperty("quantity")]
        public TLogQuantity Quantity { get; set; }
    }

    public class TLogItemDiscounts
    {
        [JsonProperty("discountType")]
        public string DiscountType { get; set; }
    }
    public class TLogAmount
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }
    }

    public class TLogQuantity
    {
        [JsonProperty("quantity")]
        public string Quantity { get; set; }
        
        [JsonProperty("unitOfMeasurement")]
        public string UnitOfMeasurement { get; set; }
        
        [JsonProperty("entryMethod")]
        public string EntryMethod { get; set; }
    }
   
}