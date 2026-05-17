using Medital_Application.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Medital_Application.Views.Sales;

public partial class CounterSaleView : UserControl
{
    private readonly CounterSaleViewModel _vm;

    public CounterSaleView(CounterSaleViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        Loaded += async (_, _) => await vm.InitializeAsync();

        PreviewKeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.F2:
                    TxtSearch.Focus();
                    e.Handled = true;
                    break;
                case Key.F10:
                    vm.SaveCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F12:
                    vm.PrintCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    vm.ClearCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        };

        TxtSearch.TextChanged += async (_, _) =>
        {
            if (TxtSearch.Text.Length >= 2)
                await vm.SearchProductCommand.ExecuteAsync(null);
            else
                vm.ClearSearchResults();
        };
    }
}
