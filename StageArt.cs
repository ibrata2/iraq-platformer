using System.Drawing.Drawing2D;

namespace IraqPlatformer;

internal enum Scenery { City, Fort, Orchard, Dunes, Marsh, Canal, Cliffs, Mountain, Night }

internal static class StageArt
{
    private static readonly Scenery[] Settings =
    [
        Scenery.City, Scenery.Fort, Scenery.Cliffs, Scenery.Canal, Scenery.Orchard,
        Scenery.Dunes, Scenery.Fort, Scenery.Marsh, Scenery.Cliffs, Scenery.Canal,
        Scenery.Orchard, Scenery.Dunes, Scenery.Marsh, Scenery.Marsh, Scenery.City,
        Scenery.Fort, Scenery.Mountain, Scenery.Mountain, Scenery.Night
    ];

    public static void Draw(Graphics g, Rectangle screen, Stage stage, float camera)
    {
        Scenery scenery = Settings[stage.Province - 1];
        bool night = scenery == Scenery.Night;
        Color horizon = night ? Color.FromArgb(97, 77, 126) : Color.FromArgb(255, 223, 162);
        using var sky = new LinearGradientBrush(screen, night ? Color.FromArgb(23, 31, 70) :
            Color.FromArgb(111, 183, 219), horizon, 90f);
        g.FillRectangle(sky, screen);
        g.FillEllipse(night ? Brushes.LemonChiffon : Brushes.LightGoldenrodYellow, 730 - camera * .009f, 69, 66, 66);

        if (night)
        {
            for (int i = 0; i < 68; i++)
            {
                int x = (i * 137 + 29) % screen.Width;
                int y = 62 + (i * 57) % 150;
                g.FillEllipse(i % 7 == 0 ? Brushes.LightGoldenrodYellow : Brushes.Lavender, x, y, i % 7 == 0 ? 3 : 1, 2);
            }
        }

        Color ridge = scenery switch
        {
            Scenery.Dunes => Color.BurlyWood,
            Scenery.Mountain or Scenery.Night => Color.FromArgb(81, 112, 125),
            Scenery.Cliffs => Color.RosyBrown,
            _ => Color.FromArgb(151, 166, 150)
        };
        using var mountains = new SolidBrush(ridge);
        int offset = (int)(camera * .12f);
        int first = offset / 220;
        for (int i = first - 1; i < first + 7; i++)
        {
            int x = i * 220 - offset;
            int height = scenery is Scenery.Mountain or Scenery.Night ? 176 : 96;
            Point[] peaks = [new(x - 35, 395), new(x + 45, 350 - height),
                new(x + 99, 299 - height / 2), new(x + 155, 365 - height), new(x + 260, 395)];
            g.FillPolygon(mountains, peaks);
        }

        if (scenery is Scenery.Canal or Scenery.Marsh or Scenery.City)
        {
            g.FillRectangle(Brushes.CadetBlue, 0, 362, screen.Width, screen.Height - 362);
            using var ripple = new Pen(Color.FromArgb(115, Color.LightCyan), 2);
            for (int i = 0; i < 25; i++)
            {
                int x = ((i * 83 - (int)(camera * .32f)) % (screen.Width + 80) + screen.Width + 80) % (screen.Width + 80);
                int y = 375 + (i * 13) % 135;
                g.DrawLine(ripple, x - 55, y, x - 16, y);
            }
        }
        else
        {
            using var valley = new SolidBrush(Color.FromArgb(75, ridge));
            g.FillRectangle(valley, 0, 380, screen.Width, screen.Height - 380);
        }

        int near = (int)(camera * .42f);
        first = near / 116;
        for (int i = first - 1; i < first + 10; i++)
        {
            int x = i * 116 - near;
            int height = 74 + Math.Abs((i * 31 + stage.Province * 19) % 65);
            if (scenery is Scenery.City or Scenery.Fort)
                DrawBuilding(g, x, 394 - height, height, stage.Province == 7, scenery == Scenery.City);
            else if (scenery is Scenery.Orchard or Scenery.Canal or Scenery.Marsh)
                DrawTree(g, x, height, scenery == Scenery.Orchard, scenery == Scenery.Marsh);
            else if (night)
            {
                g.FillRectangle(Brushes.DarkSlateBlue, x, 395 - height / 2, 88, height / 2);
                using var glow = new SolidBrush(Color.FromArgb(45, Color.Orange));
                g.FillEllipse(glow, x + 22, 357 - height / 2, 44, 46);
                g.FillEllipse(Brushes.Orange, x + 38, 374 - height / 2, 12, 15);
            }
        }
        if (stage.Province is 13 or 14 or 15)
        {
            int shift = (int)(camera * .31f) % 310;
            for (int i = 0; i <= 4; i++)
            {
                int x = i * 310 + 70 - shift;
                int y = 401 + i % 2 * 18;
                g.FillPolygon(Brushes.SaddleBrown, (Point[])[new(x, y), new(x + 90, y), new(x + 73, y + 12), new(x + 13, y + 12)]);
                if (stage.Province == 15)
                {
                    g.DrawLine(Pens.Sienna, x + 48, y - 49, x + 48, y);
                    g.FillPolygon(Brushes.AntiqueWhite, (Point[])[new(x + 49, y - 47), new(x + 72, y - 7), new(x + 49, y - 7)]);
                }
            }
        }
    }

    public static void DrawTerrain(Graphics g, Rectangle tile, float camera, int screenWidth, int province)
    {
        Rectangle clip = Rectangle.Intersect(tile, new Rectangle((int)camera - 2, 0, screenWidth + 4, 520));
        if (clip.IsEmpty) return;
        Brush stone = province == 7 ? Brushes.SteelBlue : province == 19 ? Brushes.DarkSlateBlue : Brushes.SaddleBrown;
        Brush trim = province == 7 ? Brushes.Goldenrod : province == 19 ? Brushes.SlateGray : Brushes.Peru;
        g.FillRectangle(stone, clip);
        g.FillRectangle(trim, clip.X, tile.Y, clip.Width, 6);
        using var mortar = new Pen(Color.FromArgb(57, Color.Wheat));
        for (int row = tile.Y + 13; row < tile.Bottom; row += 15)
        {
            g.DrawLine(mortar, clip.Left, row, clip.Right, row);
            int offset = (row - tile.Y) / 15 % 2 * 14;
            int first = tile.Left + offset + Math.Max(0, (clip.Left - tile.Left - offset) / 28) * 28;
            for (int x = first; x < clip.Right; x += 28)
                if (x >= clip.Left) g.DrawLine(mortar, x, row, x, Math.Min(tile.Bottom, row + 15));
        }
    }

    private static void DrawBuilding(Graphics g, int x, int top, int height, bool blue, bool balcony)
    {
        g.FillRectangle(blue ? Brushes.SteelBlue : Brushes.BurlyWood, x, top, 111, height);
        g.FillRectangle(blue ? Brushes.Goldenrod : Brushes.Sienna, x, top, 111, 6);
        using var mortar = new Pen(Color.FromArgb(48, Color.SaddleBrown));
        for (int row = 10; row < height; row += 9)
            g.DrawLine(mortar, x + 1, top + row, x + 109, top + row);
        if (balcony)
        {
            g.FillRectangle(Brushes.SaddleBrown, x + 12, top + 20, 83, 33);
            for (int k = 20; k < 90; k += 9)
                g.DrawLine(Pens.Peru, x + k, top + 23, x + k, top + 49);
            g.DrawRectangle(Pens.DarkGoldenrod, x + 12, top + 20, 83, 33);
            g.FillRectangle(Brushes.DarkSlateGray, x + 43, top + 65, 22, Math.Max(0, height - 65));
        }
        else
        {
            for (int k = 0; k < 111; k += 28)
                g.FillRectangle(blue ? Brushes.SteelBlue : Brushes.BurlyWood, x + k, top - 12, 15, 16);
            g.FillRectangle(Brushes.DarkSlateGray, x + 45, top + 29, 16, 25);
        }
    }

    private static void DrawTree(Graphics g, int x, int height, bool orchard, bool reeds)
    {
        if (reeds)
        {
            for (int k = 0; k < 45; k += 5)
                g.DrawLine(Pens.DarkOliveGreen, x + k, 400, x + k + 12, 365 - k % 17);
            return;
        }
        int top = 392 - height;
        g.FillRectangle(Brushes.Sienna, x + 26, top + 19, 9, height - 19);
        g.FillEllipse(orchard ? Brushes.OliveDrab : Brushes.DarkOliveGreen, x, top, 66, 44);
        if (orchard)
        {
            g.FillEllipse(Brushes.Orange, x + 16, top + 13, 7, 8);
            g.FillEllipse(Brushes.OrangeRed, x + 40, top + 24, 7, 8);
        }
    }
}
