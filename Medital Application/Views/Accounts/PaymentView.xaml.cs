using System.Windows.Controls;
using System.Windows.Input;
using Medital_Application.ViewModels;

namespace Medital_Application.Views.Accounts;

public partial class PaymentView : UserControl
{
    private readonly PaymentViewModel _vm;

    public PaymentView(PaymentViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F10) vm.SaveCommand.Execute(null);
            else if (e.Key == Key.Escape) vm.ClearCommand.Execute(null);
        };
        Loaded += async (_, _) => await vm.InitializeAsync();
    }
}
