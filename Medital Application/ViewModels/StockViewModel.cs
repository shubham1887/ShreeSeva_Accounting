using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class StockViewModel : ObservableObject
{
    private readonly IStockService _stockService;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private decimal _totalValue;

    public ObservableCollection<StockResponse> StockItems { get; } = new();

    public StockViewModel(IStockService stockService) => _stockService = stockService;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _stockService.GetCurrentStockAsync(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            StockItems.Clear();
            foreach (var item in items) StockItems.Add(item);
            TotalItems = items.Count;
            TotalValue = items.Sum(i => i.StockValue);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        StatusMessage = "Exporting...";
        var csv = Helpers.ExcelExporter.ExportToCsv(
            StockItems,
            new[] { "Product", "Batch", "Expiry", "Qty", "Rate", "MRP", "Value", "Status" },
            s => new object?[] { s.ProductName, s.BatchNo, s.ExpiryMY, s.CurrentQty, s.ActualRate, s.MRP, s.StockValue, s.ExpiryStatus });
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Stock_{DateTime.Now:yyyyMMdd}.csv");
        await Helpers.ExcelExporter.SaveCsvAsync(path, csv);
        StatusMessage = $"Exported to {path}";
    }
}
