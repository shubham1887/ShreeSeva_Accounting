using Medital_Application.Responses;
using Medital_Application.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Medital_Application.Views;

public partial class LoginView : Window
{
    private readonly LoginViewModel _vm;

    public LoginResponse? LoginResult { get; private set; }

    public LoginView(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LoginSuccessful += OnLoginSuccessful;
        Loaded += (_, _) => TxtUser.Focus();
    }

    private void OnLoginSuccessful(object? sender, LoginResponse response)
    {
        LoginResult = response;
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.LoginSuccessful -= OnLoginSuccessful;
        base.OnClosed(e);
    }
}
