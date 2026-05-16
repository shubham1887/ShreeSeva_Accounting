using System.Windows.Controls;
using Medital_Application.ViewModels;

namespace Medital_Application.Views.Reports;

public partial class SalesReportView : UserControl
{
    public SalesReportView(ReportsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
