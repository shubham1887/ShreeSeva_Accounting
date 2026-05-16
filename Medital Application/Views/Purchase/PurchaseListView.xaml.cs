using Medital_Application.Services.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace Medital_Application.Views.Purchase;

public partial class PurchaseListView : UserControl
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseListView(IPurchaseService purchaseService)
    {
        InitializeComponent();
        _purchaseService = purchaseService;
        DpFrom.SelectedDate = DateTime.Today.AddDays(-30);
        DpTo.SelectedDate = DateTime.Today;
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
        var to = DpTo.SelectedDate ?? DateTime.Today;
        var data = await _purchaseService.GetByDateRangeAsync(from, to);
        PurchaseGrid.ItemsSource = data;
    }
}
