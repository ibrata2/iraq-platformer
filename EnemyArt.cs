namespace IraqPlatformer;

internal static class EnemyArt
{
    public static void Draw(Graphics g, EnemyMotion motion, RectangleF body, int tick)
    {
        var state = g.Save();
        g.TranslateTransform(body.X, body.Y);
        float stride = (float)Math.Sin(tick * .36f) * 3;
        if (motion == EnemyMotion.Fly)
        {
            float wing = 9 + Math.Abs(stride) * 4;
            g.FillEllipse(Brushes.SaddleBrown, 0, 9 - wing / 2, 19, wing);
            g.FillEllipse(Brushes.SaddleBrown, 17, 9 - wing / 2, 19, wing);
            g.FillEllipse(Brushes.Wheat, 2, 10 - wing / 2, 15, wing - 3);
            g.FillEllipse(Brushes.Peru, 19, 10 - wing / 2, 15, wing - 3);
            g.FillEllipse(Brushes.DarkSlateGray, 12, 10, 13, 19);
            g.DrawLine(Pens.SaddleBrown, 15, 12, 10, 5);
            g.DrawLine(Pens.SaddleBrown, 21, 12, 26, 5);
        }
        else if (motion == EnemyMotion.Hop)
        {
            g.FillEllipse(Brushes.DarkOliveGreen, 1, 9, 34, 21);
            g.FillEllipse(Brushes.SeaGreen, 3, 7, 30, 18);
            g.FillEllipse(Brushes.OliveDrab, 4, 5, 9, 12);
            g.FillEllipse(Brushes.OliveDrab, 23, 5, 9, 12);
            g.FillEllipse(Brushes.DarkOliveGreen, 4, 22, 11, 8);
            g.FillEllipse(Brushes.DarkOliveGreen, 21, 22, 11, 8);
            g.DrawArc(Pens.DarkSlateGray, 11, 12, 14, 11, 10, 160);
        }
        else
        {
            for (int leg = 0; leg < 4; leg++)
                g.DrawLine(Pens.SaddleBrown, 5 + 7 * leg, 21, 4 + 7 * leg + stride * (leg % 2 == 0 ? 1 : -1), 29);
            g.FillEllipse(Brushes.DarkRed, 4, 4, 31, 22);
            g.FillEllipse(Brushes.Firebrick, 7, 3, 25, 17);
            g.DrawArc(Pens.Salmon, 8, 5, 20, 13, 195, 118);
            g.DrawLine(Pens.Maroon, 23, 8, 31, 20);
            g.FillEllipse(Brushes.SaddleBrown, 0, 11, 15, 15);
        }

        if (motion == EnemyMotion.Patrol)
        {
            g.FillEllipse(Brushes.Ivory, 3, 14, 6, 6);
            g.FillEllipse(Brushes.Black, 4, 15, 3, 4);
        }
        else
        {
            int left = motion == EnemyMotion.Fly ? 13 : 6;
            int right = motion == EnemyMotion.Fly ? 19 : 24;
            g.FillEllipse(Brushes.Ivory, left, 10, 5, 6);
            g.FillEllipse(Brushes.Ivory, right, 10, 5, 6);
            g.FillEllipse(Brushes.Black, left + 1, 12, 3, 3);
            g.FillEllipse(Brushes.Black, right + 1, 12, 3, 3);
        }
        g.Restore(state);
    }
}
