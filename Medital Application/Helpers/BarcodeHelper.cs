using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Medital_Application.Helpers;

/// <summary>Pure-.NET Code128-B barcode generator — no external libraries required.</summary>
public static class BarcodeHelper
{
    // Code128-B symbol table (jagged array — STOP symbol has 7 elements, rest have 6)
    // Each row: bar/space widths alternating, starting with a bar
    private static readonly int[][] Code128Table =
    [
        [2,1,2,2,2,2], [2,2,2,1,2,2], [2,2,2,2,2,1], [1,2,1,2,2,3],
        [1,2,1,3,2,2], [1,3,1,2,2,2], [1,2,2,2,1,3], [1,2,2,3,1,2],
        [1,3,2,2,1,2], [2,2,1,2,1,3], [2,2,1,3,1,2], [2,3,1,2,1,2],
        [1,1,2,2,3,2], [1,2,2,1,3,2], [1,2,2,2,3,1], [1,1,3,2,2,2],
        [1,2,3,1,2,2], [1,2,3,2,2,1], [2,2,3,2,1,1], [2,2,1,1,3,2],
        [2,2,1,2,3,1], [2,1,3,2,1,2], [2,2,3,1,1,2], [3,1,2,1,3,1],
        [3,1,1,2,2,2], [3,2,1,1,2,2], [3,2,1,2,2,1], [3,1,2,2,1,2],
        [3,2,2,1,1,2], [3,2,2,2,1,1], [2,1,2,1,2,3], [2,1,2,3,2,1],
        [2,3,2,1,2,1], [1,1,1,3,2,3], [1,3,1,1,2,3], [1,3,1,3,2,1],
        [1,1,2,3,1,3], [1,3,2,1,1,3], [1,3,2,3,1,1], [2,1,1,3,1,3],
        [2,3,1,1,1,3], [2,3,1,3,1,1], [1,1,2,1,3,3], [1,1,2,3,3,1],
        [1,3,2,1,3,1], [1,1,3,1,2,3], [1,1,3,3,2,1], [1,3,3,1,2,1],
        [3,1,3,1,2,1], [2,1,1,3,3,1], [2,3,1,1,3,1], [2,1,3,1,1,3],
        [2,1,3,3,1,1], [2,1,3,1,3,1], [3,1,1,1,2,3], [3,1,1,3,2,1],
        [3,3,1,1,2,1], [3,1,2,1,1,3], [3,1,2,3,1,1], [3,3,2,1,1,1],
        [3,1,4,1,1,1], [2,2,1,4,1,1], [4,3,1,1,1,1], [1,1,1,2,2,4],
        [1,1,1,4,2,2], [1,2,1,1,2,4], [1,2,1,4,2,1], [1,4,1,1,2,2],
        [1,4,1,2,2,1], [1,1,2,2,1,4], [1,1,2,4,1,2], [1,2,2,1,1,4],
        [1,2,2,4,1,1], [1,4,2,1,1,2], [1,4,2,2,1,1], [2,4,1,2,1,1],
        [2,2,1,1,1,4], [4,1,3,1,1,1], [2,4,1,1,1,2], [1,3,4,1,1,1],
        [1,1,1,2,4,2], [1,2,1,1,4,2], [1,2,1,2,4,1], [1,1,4,2,1,2],
        [1,2,4,1,1,2], [1,2,4,2,1,1], [4,1,1,2,1,2], [4,2,1,1,1,2],
        [4,2,1,2,1,1], [2,1,2,1,4,1], [2,1,4,1,2,1], [4,1,2,1,2,1],
        [1,1,1,1,4,3], [1,1,1,3,4,1], [1,3,1,1,4,1], [1,1,4,1,1,3],
        [1,1,4,3,1,1], [4,1,1,1,1,3], [4,1,1,3,1,1], [1,1,3,1,4,1],
        [1,1,4,1,3,1], [3,1,1,1,4,1], [4,1,1,1,3,1], [2,1,1,4,1,2],
        [2,1,1,2,1,4], [2,1,1,2,3,2], [2,3,3,1,1,1,2] // STOP (106) — 7 elements
    ];

    private const int StartB  = 104;
    private const int StopSym = 106;

    public static DrawingImage GenerateCode128(string text, double width = 200, double height = 60)
    {
        var bars = EncodeCode128B(text);
        return RenderToDrawingImage(bars, width, height);
    }

    public static BitmapSource ToBitmap(string text, double width = 300, double height = 80)
    {
        var bars = EncodeCode128B(text);
        var drawing = RenderToDrawingImage(bars, width, height);

        var visual = new System.Windows.Media.DrawingVisual();
        using (var ctx = visual.RenderOpen())
            ctx.DrawImage(drawing, new System.Windows.Rect(0, 0, width, height));

        var rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        return rtb;
    }

    private static List<bool> EncodeCode128B(string text)
    {
        var symbols = new List<int> { StartB };

        foreach (char c in text)
        {
            int code = c - 32;
            if (code < 0 || code > 95)
                throw new ArgumentException($"Character '{c}' cannot be encoded in Code128-B.");
            symbols.Add(code);
        }

        // Checksum
        int checksum = StartB;
        for (int i = 1; i < symbols.Count; i++)
            checksum += symbols[i] * i;
        symbols.Add(checksum % 103);
        symbols.Add(StopSym);

        var bars = new List<bool>();
        foreach (int sym in symbols)
        {
            int[] pattern = Code128Table[sym];
            for (int e = 0; e < pattern.Length; e++)
            {
                bool isBar = (e % 2 == 0);
                for (int w = 0; w < pattern[e]; w++)
                    bars.Add(isBar);
            }
        }

        return bars;
    }

    private static DrawingImage RenderToDrawingImage(List<bool> bars, double totalWidth, double totalHeight)
    {
        double unitWidth = totalWidth / bars.Count;
        var group = new DrawingGroup();

        group.Children.Add(new GeometryDrawing(
            Brushes.White, null,
            new RectangleGeometry(new System.Windows.Rect(0, 0, totalWidth, totalHeight))));

        double x = 0;
        foreach (bool isBar in bars)
        {
            if (isBar)
                group.Children.Add(new GeometryDrawing(
                    Brushes.Black, null,
                    new RectangleGeometry(new System.Windows.Rect(x, 0, unitWidth, totalHeight))));
            x += unitWidth;
        }

        return new DrawingImage(group);
    }
}
