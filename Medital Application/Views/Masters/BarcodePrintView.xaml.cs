using Medital_Application.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Medital_Application.Views.Masters;

public partial class BarcodePrintView : UserControl
{
    // Expose label sizes so XAML x:Static binding can reference them
    public static List<string> LabelSizes => BarcodePrintViewModel.LabelSizes;

    public BarcodePrintView(BarcodePrintViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
