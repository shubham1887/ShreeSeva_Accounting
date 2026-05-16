using Medital_Application.Data;
using Medital_Application.Repositories;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Services;
using Medital_Application.Services.Interfaces;
using Medital_Application.ViewModels;
using Medital_Application.Views;
using Medital_Application.Views.Accounts;
using Medital_Application.Views.Purchase;
using Medital_Application.Views.Reports;
using Medital_Application.Views.Sales;
using Medital_Application.Views.Settings;
using Medital_Application.Views.Stock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace Medital_Application;

public partial class App : Application
{
    private IServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // Build DI container
        var services = new ServiceCollection();
        ConfigureServices(services, config);
        _services = services.BuildServiceProvider();

        // Initialize database
        var dbInit = _services.GetRequiredService<DatabaseInitializer>();
        await dbInit.InitializeAsync();

        // Auto backup check
        try
        {
            var backup = _services.GetRequiredService<IBackupService>();
            await backup.AutoBackupIfDueAsync();
        }
        catch { /* Non-fatal */ }

        // Show Login
        var loginVm = _services.GetRequiredService<LoginViewModel>();
        var loginView = new LoginView(loginVm);
        loginView.ShowDialog();

        if (loginView.LoginResult == null)
        {
            Shutdown();
            return;
        }

        // Show Main Window
        var mainVm = _services.GetRequiredService<MainViewModel>();
        mainVm.SetCurrentUser(loginView.LoginResult);

        var mainWindow = _services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Logging
        services.AddLogging(logging => logging.AddConsole());

        // Configuration
        services.AddSingleton(config);

        // Infrastructure
        services.AddSingleton<IDbConnectionFactory>(sp =>
            new DbConnectionFactory(config));
        services.AddSingleton<DatabaseInitializer>();

        // Repositories
        services.AddTransient<ISettingsRepository, SettingsRepository>();
        services.AddTransient<IProductRepository, ProductRepository>();
        services.AddTransient<IAccountRepository, AccountRepository>();
        services.AddTransient<IStockRepository, StockRepository>();
        services.AddTransient<ISaleRepository, SaleRepository>();
        services.AddTransient<IPurchaseRepository, PurchaseRepository>();
        services.AddTransient<IReceiptRepository, ReceiptRepository>();
        services.AddTransient<IPaymentRepository, PaymentRepository>();
        services.AddTransient<ICreditNoteRepository, CreditNoteRepository>();
        services.AddTransient<IDebitNoteRepository, DebitNoteRepository>();
        services.AddTransient<IReportRepository, ReportRepository>();
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IDoctorRepository, DoctorRepository>();
        services.AddTransient<IPatientRepository, PatientRepository>();
        services.AddTransient<IJournalRepository, JournalRepository>();
        services.AddTransient<IQuotationRepository, QuotationRepository>();

        // Services
        services.AddTransient<ISettingsService, SettingsService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<ISaleService, SaleService>();
        services.AddTransient<IPurchaseService, PurchaseService>();
        services.AddTransient<IStockService, StockService>();
        services.AddTransient<IGstService, GstService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddTransient<IBackupService, BackupService>();
        services.AddTransient<IPrintService, PrintService>();
        services.AddTransient<IWhatsAppService, WhatsAppService>();
        services.AddSingleton<INavigationService>(sp =>
        {
            var nav = new NavigationService(sp);
            return nav;
        });

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SaleEntryViewModel>();
        services.AddTransient<PurchaseEntryViewModel>();
        services.AddTransient<StockViewModel>();
        services.AddTransient<AccountsViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<GSTReportViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<DoctorViewModel>();
        services.AddTransient<PatientViewModel>();
        services.AddTransient<ProductMasterViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<PaymentViewModel>();
        services.AddTransient<ReceiptViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<SaleEntryView>();
        services.AddTransient<SaleListView>();
        services.AddTransient<PurchaseEntryView>();
        services.AddTransient<PurchaseListView>();
        services.AddTransient<StockView>();
        services.AddTransient<AccountListView>();
        services.AddTransient<ReceiptView>();
        services.AddTransient<PaymentView>();
        services.AddTransient<SalesReportView>();
        services.AddTransient<GSTReportView>();
        services.AddTransient<SettingsView>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_services is IDisposable d) d.Dispose();
        base.OnExit(e);
    }
}
