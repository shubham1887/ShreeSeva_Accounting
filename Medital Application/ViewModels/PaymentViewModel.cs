using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class PaymentViewModel : ObservableObject
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ISettingsService _settings;
    private int _currentUserId;

    [ObservableProperty] private string _voucherNo = string.Empty;
    [ObservableProperty] private DateTime _voucherDate = DateTime.Today;
    [ObservableProperty] private int _selectedSupplierId;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _selectedPaymentMode = "Cash";
    [ObservableProperty] private string? _chequeNo;
    [ObservableProperty] private DateTime? _chequeDate;
    [ObservableProperty] private string? _bankName;
    [ObservableProperty] private string? _narration;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Account> Suppliers { get; } = new();

    public PaymentViewModel(IPaymentRepository paymentRepo, IAccountRepository accountRepo, ISettingsService settings)
    {
        _paymentRepo = paymentRepo;
        _accountRepo = accountRepo;
        _settings = settings;
    }

    public void SetCurrentUser(int userId) => _currentUserId = userId;

    public async Task InitializeAsync()
    {
        var fy = await _settings.GetFinancialYearAsync();
        VoucherNo = await _paymentRepo.GetNextVoucherNoAsync(fy);

        var suppliers = await _accountRepo.GetDistributorsAsync();
        Suppliers.Clear();
        foreach (var s in suppliers)
            Suppliers.Add(s);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedSupplierId == 0) { StatusMessage = "Please select a supplier."; return; }
        if (Amount <= 0) { StatusMessage = "Amount must be greater than zero."; return; }

        IsLoading = true;
        try
        {
            var master = new PaymentMaster
            {
                VoucherNo = VoucherNo,
                VoucherDate = VoucherDate,
                AccountId = SelectedSupplierId,
                Amount = Amount,
                PaymentMode = SelectedPaymentMode,
                ChequeNo = ChequeNo,
                ChequeDate = ChequeDate,
                Narration = Narration,
                TenantId = 1,
            };

            var id = await _paymentRepo.CreateAsync(master, new List<PaymentDetail>());
            StatusMessage = $"Payment voucher {VoucherNo} saved successfully.";
            await ClearAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        var fy = await _settings.GetFinancialYearAsync();
        VoucherNo = await _paymentRepo.GetNextVoucherNoAsync(fy);
        VoucherDate = DateTime.Today;
        SelectedSupplierId = 0;
        Amount = 0;
        SelectedPaymentMode = "Cash";
        ChequeNo = null;
        ChequeDate = null;
        BankName = null;
        Narration = null;
    }
}
