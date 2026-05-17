using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Medital_Application.Helpers;

namespace Medital_Application.ViewModels;

// ── Item model for the DataGrid ────────────────────────────────────────────
public partial class BarcodeProductItem : ObservableObject
{
    public int    ProductId   { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode     { get; set; } = string.Empty;
    public decimal Mrp        { get; set; }
    public decimal SaleRate   { get; set; }

    [ObservableProperty] private int  _copies = 1;
    [ObservableProperty] private bool _isSelected;
}

// ── ViewModel ──────────────────────────────────────────────────────────────
public partial class BarcodePrintViewModel : ObservableObject
{
    private readonly IProductRepository _productRepo;

    [ObservableProperty] private string  _searchText       = string.Empty;
    [ObservableProperty] private string  _selectedLabelSize = "2x1 inch";
    [ObservableProperty] private bool    _isLoading;
    [ObservableProperty] private string  _statusMessage    = string.Empty;

    public ObservableCollection<BarcodeProductItem> Products { get; } = new();

    public static readonly List<string> LabelSizes = new()
    {
        "2x1 inch", "3x1.5 inch", "4x2 inch"
    };

    public BarcodePrintViewModel(IProductRepository productRepo)
    {
        _productRepo = productRepo;
    }

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading products…";
        try
        {
            var list = await _productRepo.SearchAsync(new SearchProductRequest
            {
                SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                PageSize   = 200
            });
            Products.Clear();
            foreach (var p in list)
            {
                Products.Add(new BarcodeProductItem
                {
                    ProductId   = p.Id,
                    ProductName = p.ProductName,
                    Barcode     = p.Barcode ?? p.ProductCode,
                    Mrp         = p.DefaultMRP,
                    SaleRate    = p.DefaultSaleRate,
                    Copies      = 1
                });
            }
            StatusMessage = $"{Products.Count} products loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private void SelectAll()
    {
        bool allSelected = Products.All(p => p.IsSelected);
        foreach (var p in Products) p.IsSelected = !allSelected;
    }

    [RelayCommand]
    private void Print()
    {
        var selected = Products.Where(p => p.IsSelected && p.Copies > 0).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Please select at least one product and set Copies > 0.",
                            "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var (labelW, labelH) = ParseLabelSize(SelectedLabelSize);
        var paginator = new BarcodeLabelPaginator(selected, labelW, labelH, dlg);
        dlg.PrintDocument(paginator, "Barcode Labels — Shree Seva Medical");
        StatusMessage = "Print job sent.";
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static (double widthPx, double heightPx) ParseLabelSize(string size)
    {
        const double dpi = 96;
        return size switch
        {
            "3x1.5 inch" => (3 * dpi, 1.5 * dpi),
            "4x2 inch"   => (4 * dpi, 2 * dpi),
            _            => (2 * dpi, 1 * dpi) // 2x1 inch default
        };
    }
}

// ── Custom DocumentPaginator for label printing ────────────────────────────
internal sealed class BarcodeLabelPaginator : DocumentPaginator
{
    private readonly List<BarcodeProductItem> _items;
    private readonly double _labelW;
    private readonly double _labelH;
    private readonly PrintDialog _dlg;

    // Expand each item by Copies count into a flat list
    private readonly List<BarcodeProductItem> _pages;

    public BarcodeLabelPaginator(
        List<BarcodeProductItem> items,
        double labelW, double labelH,
        PrintDialog dlg)
    {
        _items  = items;
        _labelW = labelW;
        _labelH = labelH;
        _dlg    = dlg;

        _pages = new List<BarcodeProductItem>();
        foreach (var item in items)
            for (int i = 0; i < item.Copies; i++)
                _pages.Add(item);

        PageSize = new Size(
            dlg.PrintableAreaWidth,
            dlg.PrintableAreaHeight);
    }

    public override bool IsPageCountValid => true;
    public override int  PageCount        => _pages.Count;
    public override Size PageSize         { get; set; }
    public override IDocumentPaginatorSource? Source => null;

    public override DocumentPage GetPage(int pageNumber)
    {
        var item = _pages[pageNumber];
        var visual = BuildLabelVisual(item, _labelW, _labelH);
        return new DocumentPage(visual, PageSize,
            new Rect(PageSize), new Rect(0, 0, _labelW, _labelH));
    }

    private static System.Windows.Media.DrawingVisual BuildLabelVisual(
        BarcodeProductItem item, double w, double h)
    {
        var dv = new System.Windows.Media.DrawingVisual();
        using var ctx = dv.RenderOpen();

        // Background
        ctx.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 0.5), new Rect(0, 0, w, h));

        // Barcode image (top 55% of label height)
        double barcodeH = h * 0.55;
        double barcodeW = w - 8;
        try
        {
            var barcodeText = string.IsNullOrWhiteSpace(item.Barcode) ? item.ProductId.ToString() : item.Barcode;
            var img = BarcodeHelper.GenerateCode128(barcodeText, barcodeW, barcodeH);
            ctx.DrawImage(img, new Rect(4, 2, barcodeW, barcodeH));
        }
        catch
        {
            // Fallback: draw placeholder rectangle
            ctx.DrawRectangle(Brushes.LightGray, null, new Rect(4, 2, barcodeW, barcodeH));
        }

        // Product name text
        double textY = barcodeH + 4;
        var tf = new Typeface("Segoe UI");
        var name = new FormattedText(
            item.ProductName,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            tf, 7, Brushes.Black,
            VisualTreeHelper.GetDpi(dv).PixelsPerDip);
        name.MaxTextWidth  = w - 8;
        name.MaxLineCount  = 1;
        name.Trimming      = TextTrimming.CharacterEllipsis;
        ctx.DrawText(name, new System.Windows.Point(4, textY));

        // MRP text
        var mrp = new FormattedText(
            $"MRP: ₹{item.Mrp:N2}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            tf, 7, Brushes.Black,
            VisualTreeHelper.GetDpi(dv).PixelsPerDip);
        ctx.DrawText(mrp, new System.Windows.Point(4, textY + 10));

        return dv;
    }
}
