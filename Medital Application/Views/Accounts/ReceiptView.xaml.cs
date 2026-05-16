using System.Windows.Controls;
using System.Windows.Input;
using Medital_Application.ViewModels;

namespace Medital_Application.Views.Accounts;

public partial class ReceiptView : UserControl
{
    private readonly ReceiptViewModel _vm;

    public ReceiptView(ReceiptViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F10) vm.SaveAsyncCommand.Execute(null);
            else if (e.Key == Key.Escape) vm.ClearAsyncCommand.Execute(null);
        };
        Loaded += async (_, _) => await vm.InitializeAsync();
    }
}
