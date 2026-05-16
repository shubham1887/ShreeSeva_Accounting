namespace Medital_Application.Responses;

public class GSTBillItem
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string HSNCode { get; set; } = string.Empty;
    public decimal TaxableAmount { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class HSNSummary
{
    public string HSNCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalQty { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalTax { get; set; }
}

public class GSTReportResponse
{
    public string Period { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalTaxable { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalIGST { get; set; }
    public decimal TotalGST { get; set; }
    public decimal TotalSales { get; set; }
    public List<GSTBillItem> B2BInvoices { get; set; } = new();
    public List<GSTBillItem> B2CInvoices { get; set; } = new();
    public List<HSNSummary> HSNSummaries { get; set; } = new();
    // ITC from purchases
    public decimal PurchaseTaxableAmount { get; set; }
    public decimal PurchaseSGST { get; set; }
    public decimal PurchaseCGST { get; set; }
    public decimal PurchaseIGST { get; set; }
    public decimal NetTaxPayable { get; set; }
}
