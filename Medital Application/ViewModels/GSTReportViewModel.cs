using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class GSTReportViewModel : ObservableObject
{
    private readonly IGstService _gstService;

    [ObservableProperty] private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private decimal _totalTaxable;
    [ObservableProperty] private decimal _totalSGST;
    [ObservableProperty] private decimal _totalCGST;
    [ObservableProperty] private decimal _totalIGST;
    [ObservableProperty] private decimal _totalGST;

    public ObservableCollection<GSTBillItem> GSTBills { get; } = new();
    public ObservableCollection<HSNSummary> HSNSummary { get; } = new();

    public GSTReportViewModel(IGstService gstService) => _gstService = gstService;

    [RelayCommand]
    private async Task GenerateGSTRAsync()
    {
        IsLoading = true;
        StatusMessage = "Generating GSTR-1...";
        try
        {
            var req = new DateRangeRequest { FromDate = FromDate, ToDate = ToDate };
            var report = await _gstService.GenerateGSTR1Async(req);

            GSTBills.Clear();
            foreach (var b in report.B2BInvoices.Concat(report.B2CInvoices))
                GSTBills.Add(b);

            HSNSummary.Clear();
            foreach (var h in report.HSNSummaries)
                HSNSummary.Add(h);

            TotalTaxable = report.TotalTaxable;
            TotalSGST = report.TotalSGST;
            TotalCGST = report.TotalCGST;
            TotalIGST = report.TotalIGST;
            TotalGST = report.TotalGST;

            StatusMessage = $"GSTR-1 generated. {GSTBills.Count} bills.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Export()
    {
        StatusMessage = "Export to CSV not yet implemented.";
    }
}
