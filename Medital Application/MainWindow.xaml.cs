using Medital_Application.ViewModels;
using System.Windows;

namespace Medital_Application;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) =>
        {
            // Navigate to dashboard on load
            vm.NavigateDashboardCommand.Execute(null);
        };
    }
}
