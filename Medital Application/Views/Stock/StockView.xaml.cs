using Medital_Application.ViewModels;
using System.Windows.Controls;

namespace Medital_Application.Views.Stock;

public partial class StockView : UserControl
{
    public StockView(StockViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
