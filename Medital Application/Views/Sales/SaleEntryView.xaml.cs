using Medital_Application.ViewModels;
using System.Windows.Controls;

namespace Medital_Application.Views.Sales;

public partial class SaleEntryView : UserControl
{
    public SaleEntryView(SaleEntryViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
