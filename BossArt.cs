namespace IraqPlatformer;

internal static class BossArt
{
    private static readonly Lazy<Bitmap> Giant = new(() =>
    {
        using var stream = typeof(BossArt).Assembly.GetManifestResourceStream("LanternGiant")
            ?? throw new InvalidDataException("Missing Lantern Giant sprite.");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    });

    public static void Draw(Graphics g, BossProfile profile, int province, BossFrame frame, bool exposed)
    {
        if (profile.Move == BossMove.Lamps)
        {
            DrawLanternGiant(g, frame.Body, exposed);
            return;
        }
        var state = g.Save();
        g.TranslateTransform(frame.Body.Left, frame.Body.Top);
        using var skin = new SolidBrush(profile.Color);
        using var shade = new SolidBrush(ControlPaint.Dark(profile.Color));
        switch (profile.Move)
        {
            case BossMove.Charge:
                g.FillRectangle(shade, 2, 45, 20, 19);
                g.FillRectangle(shade, 41, 45, 20, 19);
                g.FillRectangle(skin, 8, 12, 48, 41);
                if (province == 1)
                {
                    g.FillRectangle(skin, 20, 0, 25, 20);
                    for (int y = 20; y <= 44; y += 12) g.DrawLine(Pens.SaddleBrown, 8, y, 56, y);
                }
                else
                {
                    g.FillEllipse(shade, 1, 9, 32, 30);
                    g.DrawArc(Pens.Ivory, 0, 0, 22, 22, 140, 160);
                    g.DrawArc(Pens.Ivory, 35, 0, 22, 22, 190, 170);
                }
                break;
            case BossMove.Dive:
                g.FillPolygon(shade, (Point[])[new(31, 30), new(1, 0), new(6, 42), new(31, 54)]);
                g.FillPolygon(skin, (Point[])[new(34, 32), new(63, 0), new(59, 46), new(33, 55)]);
                g.FillEllipse(shade, 18, 19, 31, 39);
                break;
            case BossMove.Sweep:
                for (int i = 0; i < 4; i++)
                    g.FillEllipse(shade, i * 15, 47 - i % 2 * 4, 22, 17);
                g.FillEllipse(skin, 10, 16, 49, 41);
                g.FillEllipse(skin, 0, 25, 21, 15);
                if (province == 15)
                {
                    using var tentacle = new Pen(profile.Color, 6);
                    for (int i = 0; i < 3; i++) g.DrawArc(tentacle, i * 16, 35, 25, 23, 0, 200);
                }
                break;
            default:
                g.FillEllipse(shade, 6, 36, 55, 28);
                g.FillEllipse(skin, 3, 15, 56, 40);
                g.FillEllipse(shade, 29, 7, 13, 13);
                g.FillEllipse(skin, 3, 22, 24, 29);
                break;
        }
        g.FillEllipse(Brushes.Ivory, 15, 21, 12, 10);
        g.FillEllipse(Brushes.Black, 16, 23, 5, 6);
        if (exposed)
            g.DrawEllipse(Pens.Gold, 0, 0, 63, 63);
        g.Restore(state);
    }
    private static void DrawLanternGiant(Graphics g, RectangleF body, bool exposed)
    {
        Bitmap sprite = Giant.Value;
        const float height = 92;
        float width = height * sprite.Width / sprite.Height;
        var bounds = Rectangle.Round(new RectangleF(body.X + (body.Width - width) / 2, body.Bottom - height, width, height));
        using var colors = new System.Drawing.Imaging.ImageAttributes();
        if (!exposed)
            colors.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix00 = .64f, Matrix11 = .65f, Matrix22 = .80f });
        g.DrawImage(sprite, bounds, 0, 0, sprite.Width, sprite.Height, GraphicsUnit.Pixel, colors);
        if (exposed)
            g.DrawEllipse(Pens.Gold, bounds.X + width * .34f, bounds.Y + 33, width * .24f, 24);
    }
}
