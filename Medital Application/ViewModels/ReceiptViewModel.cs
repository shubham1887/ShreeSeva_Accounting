using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class ReceiptViewModel : ObservableObject
{
    private readonly IReceiptRepository _receiptRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _voucherNo = string.Empty;
    [ObservableProperty] private DateTime _voucherDate = DateTime.Today;
    [ObservableProperty] private int _selectedCustomerId;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _selectedPaymentMode = "Cash";
    [ObservableProperty] private string? _chequeNo;
    [ObservableProperty] private DateTime? _chequeDate;
    [ObservableProperty] private string? _narration;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Account> Customers { get; } = new();

    public ReceiptViewModel(IReceiptRepository receiptRepo, IAccountRepository accountRepo, ISettingsService settings)
    {
        _receiptRepo = receiptRepo;
        _accountRepo = accountRepo;
        _settings = settings;
    }

    public async Task InitializeAsync()
    {
        var fy = await _settings.GetFinancialYearAsync();
        VoucherNo = await _receiptRepo.GetNextVoucherNoAsync(fy);

        var customers = await _accountRepo.GetCustomersAsync();
        Customers.Clear();
        foreach (var c in customers)
            Customers.Add(c);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCustomerId == 0) { StatusMessage = "Please select a customer."; return; }
        if (Amount <= 0) { StatusMessage = "Amount must be greater than zero."; return; }

        IsLoading = true;
        try
        {
            var master = new ReceiptMaster
            {
                VoucherNo = VoucherNo,
                VoucherDate = VoucherDate,
                AccountId = SelectedCustomerId,
                Amount = Amount,
                PaymentMode = SelectedPaymentMode,
                ChequeNo = ChequeNo,
                ChequeDate = ChequeDate,
                Narration = Narration,
                TenantId = 1,
            };

            await _receiptRepo.CreateAsync(master, new List<ReceiptDetail>());
            StatusMessage = $"Receipt voucher {VoucherNo} saved successfully.";
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
        VoucherNo = await _receiptRepo.GetNextVoucherNoAsync(fy);
        VoucherDate = DateTime.Today;
        SelectedCustomerId = 0;
        Amount = 0;
        SelectedPaymentMode = "Cash";
        ChequeNo = null;
        ChequeDate = null;
        Narration = null;
    }
}
