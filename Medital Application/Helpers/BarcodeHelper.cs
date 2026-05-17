using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Medital_Application.Helpers;

/// <summary>Pure-.NET Code128-B barcode generator — no external libraries required.</summary>
public static class BarcodeHelper
{
    // ── Code128-B symbol table (index = symbol value 0-106)
    // Each row: 6 integers = bar/space widths (bar,space,bar,space,bar,space)
    private static readonly int[,] Code128Table =
    {
        {2,1,2,2,2,2}, {2,2,2,1,2,2}, {2,2,2,2,2,1}, {1,2,1,2,2,3},
        {1,2,1,3,2,2}, {1,3,1,2,2,2}, {1,2,2,2,1,3}, {1,2,2,3,1,2},
        {1,3,2,2,1,2}, {2,2,1,2,1,3}, {2,2,1,3,1,2}, {2,3,1,2,1,2},
        {1,1,2,2,3,2}, {1,2,2,1,3,2}, {1,2,2,2,3,1}, {1,1,3,2,2,2},
        {1,2,3,1,2,2}, {1,2,3,2,2,1}, {2,2,3,2,1,1}, {2,2,1,1,3,2},
        {2,2,1,2,3,1}, {2,1,3,2,1,2}, {2,2,3,1,1,2}, {3,1,2,1,3,1},
        {3,1,1,2,2,2}, {3,2,1,1,2,2}, {3,2,1,2,2,1}, {3,1,2,2,1,2},
        {3,2,2,1,1,2}, {3,2,2,2,1,1}, {2,1,2,1,2,3}, {2,1,2,3,2,1},
        {2,3,2,1,2,1}, {1,1,1,3,2,3}, {1,3,1,1,2,3}, {1,3,1,3,2,1},
        {1,1,2,3,1,3}, {1,3,2,1,1,3}, {1,3,2,3,1,1}, {2,1,1,3,1,3},
        {2,3,1,1,1,3}, {2,3,1,3,1,1}, {1,1,2,1,3,3}, {1,1,2,3,3,1},
        {1,3,2,1,3,1}, {1,1,3,1,2,3}, {1,1,3,3,2,1}, {1,3,3,1,2,1},
        {3,1,3,1,2,1}, {2,1,1,3,3,1}, {2,3,1,1,3,1}, {2,1,3,1,1,3},
        {2,1,3,3,1,1}, {2,1,3,1,3,1}, {3,1,1,1,2,3}, {3,1,1,3,2,1},
        {3,3,1,1,2,1}, {3,1,2,1,1,3}, {3,1,2,3,1,1}, {3,3,2,1,1,1},
        {3,1,4,1,1,1}, {2,2,1,4,1,1}, {4,3,1,1,1,1}, {1,1,1,2,2,4},
        {1,1,1,4,2,2}, {1,2,1,1,2,4}, {1,2,1,4,2,1}, {1,4,1,1,2,2},
        {1,4,1,2,2,1}, {1,1,2,2,1,4}, {1,1,2,4,1,2}, {1,2,2,1,1,4},
        {1,2,2,4,1,1}, {1,4,2,1,1,2}, {1,4,2,2,1,1}, {2,4,1,2,1,1},
        {2,2,1,1,1,4}, {4,1,3,1,1,1}, {2,4,1,1,1,2}, {1,3,4,1,1,1},
        {1,1,1,2,4,2}, {1,2,1,1,4,2}, {1,2,1,2,4,1}, {1,1,4,2,1,2},
        {1,2,4,1,1,2}, {1,2,4,2,1,1}, {4,1,1,2,1,2}, {4,2,1,1,1,2},
        {4,2,1,2,1,1}, {2,1,2,1,4,1}, {2,1,4,1,2,1}, {4,1,2,1,2,1},
        {1,1,1,1,4,3}, {1,1,1,3,4,1}, {1,3,1,1,4,1}, {1,1,4,1,1,3},
        {1,1,4,3,1,1}, {4,1,1,1,1,3}, {4,1,1,3,1,1}, {1,1,3,1,4,1},
        {1,1,4,1,3,1}, {3,1,1,1,4,1}, {4,1,1,1,3,1}, {2,1,1,4,1,2},
        {2,1,1,2,1,4}, {2,1,1,2,3,2}, {2,3,3,1,1,1,2} // STOP (106) — 7 elements
    };

    private const int StartB  = 104;
    private const int StopSym = 106;

    // ─────────────────────────────────────────────────────────────────
    // Public: generate barcode as DrawingImage
    // ─────────────────────────────────────────────────────────────────
    public static DrawingImage GenerateCode128(string text, double width = 200, double height = 60)
    {
        var bars = EncodeCode128B(text);
        return RenderToDrawingImage(bars, width, height);
    }

    // ─────────────────────────────────────────────────────────────────
    // Public: render to BitmapSource (for preview / saving)
    // ─────────────────────────────────────────────────────────────────
    public static BitmapSource ToBitmap(string text, double width = 300, double height = 80)
    {
        var bars = EncodeCode128B(text);
        var drawing = RenderToDrawingImage(bars, width, height);

        var drawingVisual = new System.Windows.Media.DrawingVisual();
        using (var ctx = drawingVisual.RenderOpen())
        {
            ctx.DrawImage(drawing, new System.Windows.Rect(0, 0, width, height));
        }

        var rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(drawingVisual);
        return rtb;
    }

    // ─────────────────────────────────────────────────────────────────
    // Encode text as Code128-B bar pattern
    // Returns list of booleans: true = black bar, false = white space
    // Each symbol contributes alternating bar/space elements.
    // ─────────────────────────────────────────────────────────────────
    private static List<bool> EncodeCode128B(string text)
    {
        // Build symbol list: START_B, data chars, checksum, STOP
        var symbols = new List<int> { StartB };

        foreach (char c in text)
        {
            int code = c - 32; // Code128-B: ASCII 32-127 → symbol 0-95
            if (code < 0 || code > 95)
                throw new ArgumentException($"Character '{c}' (0x{(int)c:X2}) is not encodable in Code128-B.");
            symbols.Add(code);
        }

        // Checksum
        int checksum = StartB;
        for (int i = 1; i < symbols.Count; i++)
            checksum += symbols[i] * i;
        checksum %= 103;
        symbols.Add(checksum);
        symbols.Add(StopSym);

        // Expand each symbol into widths → bar/space boolean list
        var bars = new List<bool>();
        for (int si = 0; si < symbols.Count; si++)
        {
            int sym = symbols[si];
            bool isStop = sym == StopSym;
            int elemCount = isStop ? 7 : 6;

            for (int e = 0; e < elemCount; e++)
            {
                bool isBar = (e % 2 == 0); // even = bar, odd = space
                int width = Code128Table[sym, e];
                for (int w = 0; w < width; w++)
                    bars.Add(isBar);
            }
        }

        // Termination bar (2 units wide)
        bars.Add(true);
        bars.Add(true);

        return bars;
    }

    // ─────────────────────────────────────────────────────────────────
    // Render list<bool> bar pattern into a DrawingImage
    // ─────────────────────────────────────────────────────────────────
    private static DrawingImage RenderToDrawingImage(List<bool> bars, double totalWidth, double totalHeight)
    {
        double unitWidth = totalWidth / bars.Count;

        var group = new DrawingGroup();
        // White background
        group.Children.Add(new GeometryDrawing(
            Brushes.White,
            null,
            new RectangleGeometry(new System.Windows.Rect(0, 0, totalWidth, totalHeight))));

        double x = 0;
        foreach (bool isBar in bars)
        {
            if (isBar)
            {
                group.Children.Add(new GeometryDrawing(
                    Brushes.Black,
                    null,
                    new RectangleGeometry(new System.Windows.Rect(x, 0, unitWidth, totalHeight))));
            }
            x += unitWidth;
        }

        return new DrawingImage(group);
    }
}
