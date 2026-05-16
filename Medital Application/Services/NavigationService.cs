using System.Windows.Controls;

namespace Medital_Application.Services;

public interface INavigationService
{
    void NavigateTo<TView>() where TView : UserControl;
    void NavigateTo(UserControl view);
    UserControl? CurrentView { get; }
    event EventHandler<UserControl>? NavigationChanged;
}

public class NavigationService : INavigationService
{
    private UserControl? _currentView;
    private readonly IServiceProvider _serviceProvider;

    public event EventHandler<UserControl>? NavigationChanged;

    public UserControl? CurrentView => _currentView;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TView>() where TView : UserControl
    {
        var view = (UserControl)_serviceProvider.GetService(typeof(TView))!;
        NavigateTo(view);
    }

    public void NavigateTo(UserControl view)
    {
        _currentView = view;
        NavigationChanged?.Invoke(this, view);
    }
}
