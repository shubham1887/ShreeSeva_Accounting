using Medital_Application.Models;

namespace Medital_Application.Responses;

public class SaleItemResponse
{
    public string ProductName { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal SaleRate { get; set; }
    public decimal MRP { get; set; }
    public decimal ItemDiscAmt { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? HSNCode { get; set; }
}

public class SaleResponse
{
    public int SaleId { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ItemDiscAmount { get; set; }
    public decimal CashDiscAmount { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalIGST { get; set; }
    public decimal RoundOff { get; set; }
    public decimal NetAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public List<SaleItemResponse> Items { get; set; } = new();
    public string AmountInWords { get; set; } = string.Empty;
}
