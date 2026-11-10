namespace IraqPlatformer;

internal static class RewardArt
{
    public static void Draw(Graphics g, RewardKind kind, Rectangle bounds)
    {
        var state = g.Save();
        g.TranslateTransform(bounds.Left, bounds.Top);
        g.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
        switch (kind)
        {
            case RewardKind.Coin:
                g.FillEllipse(Brushes.SaddleBrown, 0, 0, 23, 24);
                g.FillEllipse(Brushes.Goldenrod, 2, 1, 19, 21);
                g.FillEllipse(Brushes.Gold, 4, 3, 15, 17);
                g.DrawEllipse(Pens.LemonChiffon, 5, 4, 12, 15);
                g.FillRectangle(Brushes.DarkGoldenrod, 10, 7, 3, 10);
                break;
            case RewardKind.Mushroom:
                g.FillEllipse(Brushes.SaddleBrown, 5, 10, 14, 14);
                g.FillEllipse(Brushes.Wheat, 7, 10, 10, 12);
                g.FillPie(Brushes.Firebrick, 0, 0, 24, 25, 180, 180);
                g.FillEllipse(Brushes.Ivory, 4, 5, 5, 5);
                g.FillEllipse(Brushes.Ivory, 12, 2, 7, 6);
                g.FillEllipse(Brushes.Ivory, 19, 8, 3, 3);
                break;
            case RewardKind.Gem:
                g.FillPolygon(Brushes.Indigo, (Point[])[new(0,8),new(6,1),new(18,1),new(24,8),new(12,24)]);
                g.FillPolygon(Brushes.Orchid, (Point[])[new(3,8),new(7,3),new(17,3),new(21,8),new(12,20)]);
                g.FillPolygon(Brushes.LavenderBlush, (Point[])[new(7,3),new(17,3),new(14,8),new(5,8)]);
                g.DrawLine(Pens.MediumPurple, 14, 8, 12, 20);
                break;
            case RewardKind.Heart:
                g.FillPolygon(Brushes.DarkGreen, (Point[])[new(0,7),new(4,2),new(9,2),new(12,6),new(15,2),new(20,2),new(24,7),new(22,14),new(12,23),new(2,14)]);
                g.FillPolygon(Brushes.LimeGreen, (Point[])[new(2,8),new(5,4),new(9,4),new(12,8),new(15,4),new(19,4),new(22,8),new(20,13),new(12,20),new(4,13)]);
                g.FillEllipse(Brushes.Honeydew, 5, 6, 5, 3);
                break;
            case RewardKind.Tool:
                g.FillRectangle(Brushes.MidnightBlue, 0, 1, 24, 22);
                g.FillRectangle(Brushes.SteelBlue, 2, 3, 20, 18);
                g.DrawRectangle(Pens.LightSkyBlue, 3, 4, 18, 16);
                g.FillPolygon(Brushes.Gold, (Point[])[new(6,13),new(11,5),new(19,13),new(15,17),new(11,11),new(8,16)]);
                break;
        }
        g.Restore(state);
    }
}
