using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginNCR.DataContracts
{
    public class ObjectResponseWrapper
    {
        [JsonProperty("lastPage", NullValueHandling = NullValueHandling.Ignore)]
        public string LastPage { get; set; }
        
        [JsonProperty("pageNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string PageNumber { get; set; }
        
        [JsonProperty("totalPages", NullValueHandling = NullValueHandling.Ignore)]
        public string TotalPages { get; set; }
        
        [JsonProperty("totalResults", NullValueHandling = NullValueHandling.Ignore)]
        public string TotalResults { get; set; }
            
        [JsonProperty("pageContent", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, object>> PageContent { get; set; }
    }

    public class PostBody
    {
        [JsonProperty("businessDay", NullValueHandling = NullValueHandling.Ignore)]
        public BusinessDay BusinessDay { get; set; }
        
        [JsonProperty("siteInfoIds", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> SiteInfoIds { get; set; }
        
        [JsonProperty("pageSize", NullValueHandling = NullValueHandling.Ignore)]
        public string PageSize { get; set; }
        
        [JsonProperty("pageNumber", NullValueHandling = NullValueHandling.Ignore)]
        public int PageNumber { get; set; }
        
        [JsonProperty("transactionCategories", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> TransactionCategories { get; set; }
    }

    public class ObjectResponse
    {
        [JsonProperty("tlogId", NullValueHandling = NullValueHandling.Ignore)]
        public string TlogId { get; set; }
        
        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Properties { get; set; }
    }


    public class PropertyResponseWrapper //Get Canonical Transaction
    {
        [JsonProperty("pageContent", NullValueHandling = NullValueHandling.Ignore)]
        public List<PropertyResponse> PageContent { get; set; }
        
        [JsonProperty("lastPage", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean LastPage { get; set; }
    }

    public class PropertyResponse //Get Canonical Transaction
    {
        [JsonProperty("transactionType", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionType { get; set; }
        
        [JsonProperty("businessDayUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime BusinessDayUtc { get; set; }
        
        [JsonProperty("employeeIds", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> EmployeeIds { get; set; }
        
        [JsonProperty("employeeNames", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> EmployeeNames { get; set; }
        
        [JsonProperty("endTransactionDateTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime EndTransactionDateTimeUtc { get; set; }
        
        [JsonProperty("closeDateTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CloseDateTimeUtc { get; set; }
        
        [JsonProperty("grandAmount", NullValueHandling = NullValueHandling.Ignore)]
        public double GrandAmount { get; set; }
        
        [JsonProperty("siteInfoId", NullValueHandling = NullValueHandling.Ignore)]
        public string SiteInfoId { get; set; }
        
        [JsonProperty("tlogId", NullValueHandling = NullValueHandling.Ignore)]
        public string TLogId { get; set; }
        
        [JsonProperty("touchPointId", NullValueHandling = NullValueHandling.Ignore)]
        public string TouchPointId { get; set; }
        
        [JsonProperty("isTrainingMode", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsTrainingMode { get; set; }
        
        [JsonProperty("transactionNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionNumber { get; set; }
        
        [JsonProperty("isVoided", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsVoided { get; set; }
        
        [JsonProperty("isSuspended", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsSuspended { get; set; }
        
        [JsonProperty("itemCount", NullValueHandling = NullValueHandling.Ignore)]
        public int ItemCount { get; set; }
        
        [JsonProperty("transactionCategory", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionCategory { get; set; }
        
        [JsonProperty("receivedDateTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime ReceivedDateTimeUtc { get; set; }
    }

    

    public class BusinessDay
    {
        [JsonProperty("dateTime", NullValueHandling = NullValueHandling.Ignore)]
        public string DateTime { get; set; }
    }

   

    public class TlogWrapper
    {
        [JsonProperty("transactionCategory", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionCategory { get; set; }
        
        [JsonProperty("isUpdated", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsUpdated { get; set; }

        [JsonProperty("transactionVersion", NullValueHandling = NullValueHandling.Ignore)] 
        public int TransactionVersion { get; set; }
        
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        
        [JsonProperty("siteInfo", NullValueHandling = NullValueHandling.Ignore)]
        public SiteInfo SiteInfo { get; set; }
        
        [JsonProperty("businessDay", NullValueHandling = NullValueHandling.Ignore)]
        public BusinessDay BusinessDay { get; set; }
        
        [JsonProperty("tlog", NullValueHandling = NullValueHandling.Ignore)]
        public TLog Tlog { get; set; }
    }

    
    public class TLog
    {
        [JsonProperty("receiptId", NullValueHandling = NullValueHandling.Ignore)]
        public string ReceiptId { get; set; }
        
        [JsonProperty("totalTaxes", NullValueHandling = NullValueHandling.Ignore)]
        public List<TLogTotalTaxes> TotalTaxes { get; set; }
        
        [JsonProperty("tenders", NullValueHandling = NullValueHandling.Ignore)]
        public List<TLogTenders> Tenders { get; set; }
        
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<TLogItems> Items { get; set; }
        
        [JsonProperty("totals", NullValueHandling = NullValueHandling.Ignore)]
        public TLogTotals TLogTotals { get; set; }
        
        [JsonProperty("employees", NullValueHandling = NullValueHandling.Ignore)]
        public List<TLogEmployees> Employees { get; set; }
        
        [JsonProperty("touchPointGroup", NullValueHandling = NullValueHandling.Ignore)]
        public string TouchPointGroup { get; set; }
        
        [JsonProperty("transactionNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionNumber { get; set; }
        
        [JsonProperty("isVoided", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsVoided { get; set; }
        
        [JsonProperty("isRecalled", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsRecalled { get; set; }
        
        [JsonProperty("isSuspended", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsSuspended { get; set; }
        
        [JsonProperty("isTrainingMode", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsTrainingMode { get; set; }
        
        [JsonProperty("isResumed", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsResumed { get; set; }
        
        [JsonProperty("isDeleted", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsDeleted { get; set; }
        
        [JsonProperty("coupons", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> Coupons { get; set; }
        
        [JsonProperty("customer", NullValueHandling = NullValueHandling.Ignore)]
        public Customer Customer { get; set; }
        
        [JsonProperty("loyaltyAccount", NullValueHandling = NullValueHandling.Ignore)]
        public List<LoyaltyAccount> LoyaltyAccount { get; set; }
    }

    public class LoyaltyAccount
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        
        [JsonProperty("accountId", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountId { get; set; }
        
        [JsonProperty("pointsAwarded", NullValueHandling = NullValueHandling.Ignore)]
        public string PointsAwarded { get; set; }
        
        [JsonProperty("pointsRedeemed", NullValueHandling = NullValueHandling.Ignore)]
        public string PointsRedeemed { get; set; }
        
        [JsonProperty("programType", NullValueHandling = NullValueHandling.Ignore)]
        public string ProgramType { get; set; }
    }

    public class Customer
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        
        [JsonProperty("entryMethod", NullValueHandling = NullValueHandling.Ignore)]
        public string EntryMethod { get; set; }
        
        [JsonProperty("identifierData", NullValueHandling = NullValueHandling.Ignore)]
        public string IdentifierData { get; set; }
        
        [JsonProperty("infoValidationMeans", NullValueHandling = NullValueHandling.Ignore)]
        public string InfoValidationMeans { get; set; }
    }

    public class TLogTenders
    {
        [JsonProperty("tenderAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogTendersTenderAmount TenderAmount { get; set; }
        
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
        
        [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
        public string Usage { get; set; }
        
        [JsonProperty("isVoided", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsVoided { get; set; }
        
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        
        [JsonProperty("typeLabel", NullValueHandling = NullValueHandling.Ignore)]
        public string TypeLabel { get; set; }
        
        [JsonProperty("cardLastFourDigits", NullValueHandling = NullValueHandling.Ignore)]
        public string CardLastFourDigits { get; set; }
        
        [JsonProperty("maskedCardNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string MaskedCardNumber { get; set; }
    }

    public class TLogTendersTenderAmount
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public float Amount { get; set; }
    }
    public class TLogTotals
    {
        [JsonProperty("discountAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount DiscountAmount { get; set; }
        
        [JsonProperty("grandAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount GrandAmount { get; set; }
    }
    public class TLogTotalTaxes
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogTotalTaxesAmount Amount { get; set; }
        
        [JsonProperty("taxableAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogTotalTaxesAmount TaxableAmount { get; set; }
        
        [JsonProperty("isRefund", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsRefund { get; set; }
        
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; } 
        
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        
        [JsonProperty("taxType", NullValueHandling = NullValueHandling.Ignore)]
        public string TaxType { get; set; }
        
        [JsonProperty("isVoided", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsVoided { get; set; }
        
        [JsonProperty("taxPercent", NullValueHandling = NullValueHandling.Ignore)]
        public string TaxPercent { get; set; }
        
        [JsonProperty("sequenceNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string SequenceNumber { get; set; }
    }

    public class TLogTotalTaxesAmount
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public string Amount { get; set; }
    }
    public class TLogTotalTaxesTaxableAmount
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public string Amount { get; set; }
    }
    public class TLogEmployees
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }

    public class SiteInfo
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
    }

    public class TLogItems
    {
        
        [JsonProperty("isItemNotOnFile", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsItemNotOnFile { get; set; }
        
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        
        [JsonProperty("orderNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string OrderNumber { get; set; }
        
        [JsonProperty("productName", NullValueHandling = NullValueHandling.Ignore)]
        public string ProductName { get; set; }
        
        [JsonProperty("productId", NullValueHandling = NullValueHandling.Ignore)]
        public string ProductId { get; set; }
        
        [JsonProperty("departmentId", NullValueHandling = NullValueHandling.Ignore)]
        public string DepartmentId { get; set; }
        
        [JsonProperty("isReturn", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsReturn { get; set; }
        
        [JsonProperty("isVoided", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsVoided { get; set; }
        
        [JsonProperty("isRefund", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsRefund { get; set; }
        
        [JsonProperty("isOverridden", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsOverridden { get; set; }
        
        [JsonProperty("isRefused", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsRefused { get; set; }
        
        [JsonProperty("isPriceLookup", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsPriceLookup { get; set; }
        
        [JsonProperty("isNonSaleItem", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsNonSaleItem { get; set; }
        
        [JsonProperty("weightFromScale", NullValueHandling = NullValueHandling.Ignore)]
        public bool WeightFromScale { get; set; }
        
        [JsonProperty("qtyIsWeight", NullValueHandling = NullValueHandling.Ignore)]
        public bool QtyIsWeight { get; set; }
        
        [JsonProperty("qtyIsFuelGallons", NullValueHandling = NullValueHandling.Ignore)]
        public bool qtyIsFuelGallons { get; set; }
        
        [JsonProperty("itemPromotions", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, object>> ItemPromotions { get; set; }
        
        [JsonProperty("itemDiscounts", NullValueHandling = NullValueHandling.Ignore)]
        public List<TLogItemDiscounts> ItemDiscounts { get; set; }
        
        [JsonProperty("regularUnitPrice", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount RegularUnitPrice { get; set; }
        
        [JsonProperty("extendedUnitPrice", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount ExtendedUnitPrice { get; set; }
        
        [JsonProperty("extendedAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount ExtendedAmount { get; set; }
        
        [JsonProperty("actualAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount ActualAmount { get; set; }
        
        [JsonProperty("quantity", NullValueHandling = NullValueHandling.Ignore)]
        public TLogQuantity Quantity { get; set; }
        
        [JsonProperty("itemTaxes", NullValueHandling = NullValueHandling.Ignore)]
        public ItemTax[] ItemTaxes { get; set; }
    }

    public class ItemTax
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("taxType", NullValueHandling = NullValueHandling.Ignore)]
        public string TaxType { get; set; }

        [JsonProperty("taxableAmount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount TaxableAmount { get; set; }

        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public TLogAmount Amount { get; set; }

        [JsonProperty("isRefund", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsRefund { get; set; }

        [JsonProperty("taxPercent", NullValueHandling = NullValueHandling.Ignore)]
        public string TaxPercent { get; set; }

        [JsonProperty("isVoided", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsVoided { get; set; }

        [JsonProperty("sequenceNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string SequenceNumber { get; set; }
    }
    
    public class TLogItemDiscounts
    {
        [JsonProperty("discountType", NullValueHandling = NullValueHandling.Ignore)]
        public string DiscountType { get; set; }
    }
    public class TLogAmount
    {
        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public string Amount { get; set; }
    }

    public class TLogQuantity
    {
        [JsonProperty("quantity", NullValueHandling = NullValueHandling.Ignore)]
        public string Quantity { get; set; }
        
        [JsonProperty("unitOfMeasurement", NullValueHandling = NullValueHandling.Ignore)]
        public string UnitOfMeasurement { get; set; }
        
        [JsonProperty("entryMethod", NullValueHandling = NullValueHandling.Ignore)]
        public string EntryMethod { get; set; }
    }
   
}