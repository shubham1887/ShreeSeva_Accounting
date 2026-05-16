using CommunityToolkit.Mvvm.ComponentModel;
using Medital_Application.Helpers;

namespace Medital_Application.ViewModels;

public partial class SaleItemViewModel : ObservableObject
{
    [ObservableProperty] private int _srNo;
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string _batchNo = string.Empty;
    [ObservableProperty] private string _expiryMY = string.Empty;
    [ObservableProperty] private int? _stockId;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _freeQty;
    [ObservableProperty] private decimal _saleRate;
    [ObservableProperty] private decimal _mrp;
    [ObservableProperty] private decimal _itemDiscPer;
    [ObservableProperty] private decimal _itemDiscAmt;
    [ObservableProperty] private decimal _sgstRate;
    [ObservableProperty] private decimal _cgstRate;
    [ObservableProperty] private decimal _igstRate;
    [ObservableProperty] private decimal _sgstAmount;
    [ObservableProperty] private decimal _cgstAmount;
    [ObservableProperty] private decimal _igstAmount;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _lineTotal;
    [ObservableProperty] private decimal _purchaseRate;
    [ObservableProperty] private string? _hsnCode;
    [ObservableProperty] private bool _isInterState;

    partial void OnQuantityChanged(decimal value) => Recalculate();
    partial void OnSaleRateChanged(decimal value) => Recalculate();
    partial void OnItemDiscPerChanged(decimal value) => Recalculate();
    partial void OnIsInterStateChanged(bool value) => Recalculate();

    private void Recalculate()
    {
        if (Quantity <= 0 || SaleRate <= 0) { LineTotal = 0; return; }
        var gross = Quantity * SaleRate;
        ItemDiscAmt = Math.Round(gross * ItemDiscPer / 100, 2);
        TaxableAmount = gross - ItemDiscAmt;
        var gst = GSTCalculator.Calculate(TaxableAmount, SgstRate, CgstRate, IgstRate, IsInterState);
        SgstAmount = gst.SGSTAmount;
        CgstAmount = gst.CGSTAmount;
        IgstAmount = gst.IGSTAmount;
        LineTotal = Math.Round(TaxableAmount + gst.TotalGST, 2);
    }
}
