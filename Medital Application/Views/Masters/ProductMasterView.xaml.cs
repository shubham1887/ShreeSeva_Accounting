using Medital_Application.ViewModels;
using System.Windows.Controls;

namespace Medital_Application.Views.Masters;

public partial class ProductMasterView : UserControl
{
    public ProductMasterView(ProductMasterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
