namespace Medital_Application.Models;

public class UserRight
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int UserId { get; set; }
    public bool CanSale { get; set; } = true;
    public bool CanSaleEdit { get; set; }
    public bool CanSaleDelete { get; set; }
    public bool CanPurchase { get; set; } = true;
    public bool CanPurchaseEdit { get; set; }
    public bool CanPurchaseDelete { get; set; }
    public bool CanReceipt { get; set; } = true;
    public bool CanPayment { get; set; } = true;
    public bool CanCreditNote { get; set; } = true;
    public bool CanDebitNote { get; set; } = true;
    public bool CanJournal { get; set; }
    public bool CanStockAdjust { get; set; }
    public bool CanProductMaster { get; set; } = true;
    public bool CanAccountMaster { get; set; } = true;
    public bool CanDoctorMaster { get; set; } = true;
    public bool CanPatientMaster { get; set; } = true;
    public bool CanReports { get; set; } = true;
    public bool CanGSTReports { get; set; } = true;
    public bool CanUserMgmt { get; set; }
    public bool CanSettings { get; set; }
    public bool CanBackup { get; set; }
    public bool CanDayClose { get; set; } = true;
    public bool CanViewCost { get; set; }
    public bool CanChangeRate { get; set; } = true;
    public bool CanGiveDiscount { get; set; } = true;
    public decimal MaxDiscountPer { get; set; } = 100;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
