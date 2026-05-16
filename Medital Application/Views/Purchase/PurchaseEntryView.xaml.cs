using Medital_Application.ViewModels;
using System.Windows.Controls;

namespace Medital_Application.Views.Purchase;

public partial class PurchaseEntryView : UserControl
{
    public PurchaseEntryView(PurchaseEntryViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
