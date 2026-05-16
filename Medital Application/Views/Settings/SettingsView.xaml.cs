using System.Windows.Controls;
using Medital_Application.ViewModels;

namespace Medital_Application.Views.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadAsyncCommand.ExecuteAsync(null);
    }
}
