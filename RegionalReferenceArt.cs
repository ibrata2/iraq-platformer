using System.Drawing.Drawing2D;

namespace IraqPlatformer;

internal static class RegionalReferenceArt
{
    private static readonly Lazy<Bitmap> Valley = new(() => Load("HalabjaValley"));
    private static readonly Lazy<Bitmap>[] Boards =
    [
        new(() => Load("maps-01-06")),
        new(() => Load("maps-07-12")),
        new(() => Load("maps-13-18"))
    ];

    private static Bitmap Load(string name)
    {
        using var stream = typeof(RegionalReferenceArt).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(name);
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    public static void Draw(Graphics graphics, Size viewport, Stage stage, float camera)
    {
        if (stage.Province == 19)
        {
            Bitmap valleyArt = Valley.Value;
            float visibleWidth = valleyArt.Height * viewport.Width / (float)viewport.Height;
            float scroll = Math.Clamp((stage.Number - 1) / 12f + camera / 21000f, 0, 1);
            var view = new RectangleF((valleyArt.Width - visibleWidth) * scroll, 0, visibleWidth, valleyArt.Height);
            graphics.DrawImage(valleyArt, new RectangleF(0, 0, viewport.Width, viewport.Height), view, GraphicsUnit.Pixel);
            return;
        }
        int index = stage.Province - 1;
        Bitmap board = Boards[index / 6].Value;
        int panel = index % 6;
        float panelWidth = board.Width / 2f;
        float panelHeight = board.Height / 3f;
        float phase = (float)(1 - Math.Cos(camera * .0008)) / 2f;
        float left = panel % 2 * panelWidth;
        float top = panel / 2 * panelHeight;
        float window = stage.Number switch { <= 3 => .09f, <= 6 => .19f, _ => .30f };
        var source = new RectangleF(left + panelWidth * (window + phase * .06f),
            top + panelHeight * .065f, panelWidth * .51f, panelHeight * .25f);
        var previous = graphics.InterpolationMode;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        // Keep the panorama low in the frame instead of stretching the narrow board vertically.
        float height = Math.Clamp(viewport.Width * source.Height / source.Width * 1.35f, 185, 310);
        float horizon = 440 - height;
        Color skyTop = Color.FromArgb(101, 154, 203);
        Color skyBottom = Color.FromArgb(167, 203, 224);
        using var sky = new LinearGradientBrush(new Rectangle(0, 0, viewport.Width, 440),
            skyTop, skyBottom, 90);
        graphics.FillRectangle(sky, 0, 0, viewport.Width, 440);
        graphics.DrawImage(board, new RectangleF(0, horizon, viewport.Width, height), source, GraphicsUnit.Pixel);
        var fadeArea = new RectangleF(0, horizon, viewport.Width, 48);
        using var fade = new LinearGradientBrush(fadeArea, Color.FromArgb(
            (int)(skyTop.R + (skyBottom.R - skyTop.R) * horizon / 440),
            (int)(skyTop.G + (skyBottom.G - skyTop.G) * horizon / 440),
            (int)(skyTop.B + (skyBottom.B - skyTop.B) * horizon / 440)), Color.Transparent, 90);
        graphics.FillRectangle(fade, fadeArea);
        using var valley = new LinearGradientBrush(new Rectangle(0, 440, viewport.Width, 80),
            Color.FromArgb(53, 106, 120), Color.FromArgb(25, 53, 65), 90);
        graphics.FillRectangle(valley, 0, 440, viewport.Width, 80);
        graphics.InterpolationMode = previous;
    }
    public static void DrawTerrain(Graphics g, Rectangle tile, int province, float camera, int width)
    {
        const int repeat = 92;
        int left = Math.Max(tile.Left, (int)camera);
        int right = Math.Min(tile.Right, (int)camera + width + 1);
        if (left >= right) return;
        int index = province - 1, panel = index % 6;
        Bitmap art = Boards[index / 6].Value;
        float w = art.Width / 2f, h = art.Height / 3f;
        float sampleLeft = (panel % 2 + .016f) * w;
        float sampleTop = (panel / 2 + .881f) * h;
        float sampleWidth = w * .064f;
        float sampleHeight = h * .038f;
        var state = g.Save();
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        int stripHeight = tile.Height >= 30 ? 26 : tile.Height;
        for (int y = tile.Top; y < tile.Bottom; y += stripHeight)
        for (int x = tile.Left + (left - tile.Left) / repeat * repeat; x < right; x += repeat)
        {
            int span = Math.Min(repeat, tile.Right - x);
            int depth = Math.Min(stripHeight, tile.Bottom - y);
            var source = new RectangleF(sampleLeft, sampleTop, sampleWidth * span / repeat,
                sampleHeight * depth / stripHeight);
            g.DrawImage(art, new Rectangle(x, y, span, depth), source, GraphicsUnit.Pixel);
        }

        Color rim = province is 3 or 6 or 9 or 12 ? Color.FromArgb(211, 177, 112) :
            province == 7 ? Color.Goldenrod : Color.FromArgb(145, 170, 65);
        using var edge = new Pen(rim, 3);
        g.DrawLine(Pens.DarkOliveGreen, left, tile.Top + 4, right, tile.Top + 4);
        g.DrawLine(edge, left, tile.Top + 1, right, tile.Top + 1);
        g.Restore(state);
    }
    public static void DrawExit(Graphics g, int x, int province, bool locked)
    {
        var frame = new Rectangle(x + 10, 366, 58, 74);
        if (province <= 18)
            DrawTerrain(g, frame, province, x, 85);
        else
            g.FillRectangle(Brushes.DarkSlateBlue, frame);

        var door = new Rectangle(x + 19, 377, 40, 63);
        using var arch = new GraphicsPath();
        arch.AddArc(door.X, door.Y, door.Width, 36, 180, 180);
        arch.AddLine(door.Right, door.Y + 18, door.Right, door.Bottom);
        arch.AddLine(door.Right, door.Bottom, door.Left, door.Bottom);
        arch.CloseFigure();
        g.FillPath(locked ? Brushes.SaddleBrown : Brushes.MidnightBlue, arch);
        using var rim = new Pen(province == 7 ? Color.Gold : Color.Wheat, 3);
        g.DrawPath(rim, arch);
        if (locked)
        {
            for (int bar = door.Left + 6; bar < door.Right; bar += 7)
                g.DrawLine(Pens.Goldenrod, bar, door.Top + 20, bar, door.Bottom - 1);
            g.FillRectangle(Brushes.Black, door.X + 16, door.Y + 34, 10, 12);
        }
        else
        {
            g.FillRectangle(Brushes.Wheat, x + 1, 358, 3, 82);
            g.FillPolygon(Brushes.Crimson, (Point[])[new(x + 4, 360), new(x + 24, 367), new(x + 4, 376)]);
        }
    }
}
