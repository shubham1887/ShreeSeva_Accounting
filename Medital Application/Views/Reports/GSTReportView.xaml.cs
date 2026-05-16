using System.Windows.Controls;
using Medital_Application.ViewModels;

namespace Medital_Application.Views.Reports;

public partial class GSTReportView : UserControl
{
    public GSTReportView(GSTReportViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
