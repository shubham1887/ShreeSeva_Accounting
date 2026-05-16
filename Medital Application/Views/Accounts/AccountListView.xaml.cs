using Medital_Application.ViewModels;
using System.Windows.Controls;

namespace Medital_Application.Views.Accounts;

public partial class AccountListView : UserControl
{
    public AccountListView(AccountsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadAsyncCommand.ExecuteAsync(null);
    }
}
