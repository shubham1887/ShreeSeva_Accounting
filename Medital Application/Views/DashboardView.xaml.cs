using Medital_Application.ViewModels;
using System.Windows.Controls;

namespace Medital_Application.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _vm;

    public DashboardView(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
