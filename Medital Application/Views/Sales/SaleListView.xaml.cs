using Medital_Application.Services.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace Medital_Application.Views.Sales;

public partial class SaleListView : UserControl
{
    private readonly ISaleService _saleService;

    public SaleListView(ISaleService saleService)
    {
        InitializeComponent();
        _saleService = saleService;
        DpFrom.SelectedDate = DateTime.Today.AddDays(-30);
        DpTo.SelectedDate = DateTime.Today;
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
        var to = DpTo.SelectedDate ?? DateTime.Today;
        var data = await _saleService.GetByDateRangeAsync(from, to);
        SaleGrid.ItemsSource = data;
    }
}
