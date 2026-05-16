using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Helpers;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;

    [ObservableProperty] private DateTime _fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalGST;
    [ObservableProperty] private decimal _totalAmount;

    public ObservableCollection<SaleResponse> Sales { get; } = new();
    public ObservableCollection<PurchaseResponse> Purchases { get; } = new();
    public ObservableCollection<StockResponse> Stock { get; } = new();

    public ReportsViewModel(IReportService reportService) => _reportService = reportService;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading...";
        try
        {
            var req = new DateRangeRequest { FromDate = FromDate, ToDate = ToDate };
            var salesList = await _reportService.GetSalesReportAsync(req);
            Sales.Clear();
            foreach (var s in salesList) Sales.Add(s);
            TotalSales = salesList.Sum(s => s.NetAmount);
            TotalGST = salesList.Sum(s => s.TotalSGST + s.TotalCGST + s.TotalIGST);
            StatusMessage = $"Loaded {salesList.Count} records.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var headers = new[] { "VoucherNo", "Date", "Party", "GrossAmount", "Discount", "SGST", "CGST", "IGST", "NetAmount", "Mode" };
            var csv = ExcelExporter.ExportToCsv(Sales, headers, s => new object?[]
            {
                s.VoucherNo, s.VoucherDate.ToString("dd-MMM-yyyy"), s.AccountName,
                s.GrossAmount, s.ItemDiscAmount, s.TotalSGST, s.TotalCGST, s.TotalIGST, s.NetAmount, s.PaymentMode
            });
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"SalesReport_{DateTime.Today:yyyyMMdd}.csv");
            await ExcelExporter.SaveCsvAsync(path, csv);
            StatusMessage = $"Exported to {path}";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }
}
