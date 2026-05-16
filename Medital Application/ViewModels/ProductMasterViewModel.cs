using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class ProductMasterViewModel : ObservableObject
{
    private readonly IProductRepository _productRepo;
    private readonly IAccountRepository _accountRepo;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<DrugCategory> Categories { get; } = new();
    public ObservableCollection<Manufacturer> Manufacturers { get; } = new();

    public ProductMasterViewModel(IProductRepository productRepo, IAccountRepository accountRepo)
    {
        _productRepo = productRepo;
        _accountRepo = accountRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _productRepo.SearchAsync(new SearchProductRequest
            {
                SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                PageSize = 100
            });
            Products.Clear();
            foreach (var p in products) Products.Add(p);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProduct == null) return;
        if (SelectedProduct.Id == 0)
        {
            SelectedProduct.ProductCode = string.IsNullOrEmpty(SelectedProduct.ProductCode)
                ? $"PRD{DateTime.Now:mmssff}"
                : SelectedProduct.ProductCode;
            await _productRepo.CreateAsync(SelectedProduct);
        }
        else await _productRepo.UpdateAsync(SelectedProduct);
        StatusMessage = "Product saved.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Product product)
    {
        await _productRepo.DeleteAsync(product.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private void NewProduct() => SelectedProduct = new Product { Unit = "TAB", PackSize = 1, IsNonRx = true };
}
