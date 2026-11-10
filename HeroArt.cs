namespace IraqPlatformer;

internal static class HeroArt
{
    private static readonly Color[] Clothes =
    [
        Color.Navy, Color.DarkSlateBlue, Color.Sienna, Color.SteelBlue, Color.Peru,
        Color.OliveDrab, Color.SlateGray, Color.SaddleBrown, Color.DarkMagenta, Color.Olive,
        Color.Tan, Color.RoyalBlue, Color.Chocolate, Color.DarkSlateGray, Color.Maroon,
        Color.IndianRed, Color.DarkSlateGray, Color.DarkOliveGreen, Color.SaddleBrown, Color.MidnightBlue
    ];

    public static void Draw(Graphics g, int id, RectangleF body, int facing, int tick,
        bool walking, bool grounded, bool striking, int hurt)
    {
        if (hurt > 0 && hurt / 4 % 2 == 0) return;
        var state = g.Save();
        g.TranslateTransform(body.X + body.Width / 2, body.Top);
        g.ScaleTransform(facing, body.Height / 46);
        using var clothing = new SolidBrush(Clothes[id - 1]);
        bool kurdish = id is >= 6 and <= 9;
        float stride = walking && grounded ? (float)Math.Sin(tick * .36) * 5 : 0;
        float lift = !grounded ? 4 : 0;

        g.FillEllipse(Brushes.SaddleBrown, -13 + stride, 39 - lift, 12, 7);
        g.FillEllipse(Brushes.SaddleBrown, 2 - stride, 39 - lift * .5f, 12, 7);
        PointF[] robe = [new(-9, 17), new(9, 17), new(13, 40 - lift), new(-13, 40 - lift)];
        g.FillPolygon(kurdish ? clothing : Brushes.AntiqueWhite, robe);
        if (kurdish)
        {
            g.DrawLine(Pens.Black, 0, 29, 0, 40 - lift);
            g.FillRectangle(Brushes.DarkRed, -11, 26, 22, 4);
        }
        else
        {
            g.FillRectangle(clothing, -10, 16, 7, id == 2 ? 23 : 15);
            g.FillRectangle(clothing, 4, 16, 7, id == 2 ? 23 : 15);
            g.DrawLine(Pens.Goldenrod, 9, 18, 9, id == 2 ? 37 : 30);
        }

        using var sleeve = new Pen(kurdish ? clothing : Brushes.AntiqueWhite, 6);
        PointF hand = striking ? new(20, 21) : new(11 - stride, grounded ? 29 : 22);
        g.DrawLine(sleeve, 8, 19, hand.X, hand.Y);
        g.FillEllipse(Brushes.Peru, hand.X - 3, hand.Y - 2, 6, 6);
        if (id != 2 && !kurdish)
        {
            g.FillPolygon(Brushes.Ivory, (Point[])[new Point(-12, 4), new Point(-7, 0), new Point(-4, 21), new Point(-15, 22)]);
            if (id is 5 or 11 or 17)
                g.DrawLine(Pens.Firebrick, -12, 6, -9, 19);
            else if (id is not (1 or 3 or 10 or 13 or 20))
                g.DrawLine(Pens.DimGray, -12, 6, -9, 19);
        }
        g.FillEllipse(Brushes.Peru, -9, 3, 20, 16);
        g.FillEllipse(Brushes.Black, 4, 9, 3, 3);
        g.FillEllipse(Brushes.Black, 1, 14, 9, 3);
        if (id == 2)
        {
            g.FillRectangle(Brushes.Maroon, -9, -3, 18, 8);
            g.DrawLine(Pens.Black, -8, -2, -13, 7);
        }
        else
        {
            g.FillEllipse(kurdish ? clothing : Brushes.Ivory, -12, -1, 24, 8);
            g.DrawLine(Pens.Black, -11, 3, 11, 3);
        }
        g.Restore(state);
    }
}
