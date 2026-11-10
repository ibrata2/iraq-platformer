using System.Drawing.Drawing2D;

namespace IraqPlatformer;

internal static class ApprovedHeroArt
{
    private static readonly Lazy<Bitmap>[] Sprites = Enumerable.Range(3, 18)
        .Select(id => new Lazy<Bitmap>(() => Extract(id))).ToArray();

    private static Bitmap Extract(int id)
    {
        using var stream = typeof(ApprovedHeroArt).Assembly.GetManifestResourceStream($"Hero{id:D2}.jpg")
            ?? throw new InvalidDataException($"Missing hero {id}");
        using var source = new Bitmap(stream);
        var rect = Rectangle.FromLTRB(9, 29, source.Width - 9, source.Height - 3);
        using var cutout = new Bitmap(rect.Width, rect.Height);
        var visible = new bool[rect.Width * rect.Height];
        for (int y = 0; y < rect.Height; y++)
        for (int x = 0; x < rect.Width; x++)
        {
            Color c = source.GetPixel(rect.X + x, rect.Y + y);
            bool teal = c.G > c.R * 1.17 && c.B > c.R * 1.12 &&
                c.G >= c.B * (id is >= 11 and <= 15 && id != 12 ? .76 : .96) && c.G - c.B < 54;
            bool frameEdge = id >= 11 && (x < 8 || x >= rect.Width - 8 || y < (id >= 16 ? 29 : 13) || y >= rect.Height - 6)
                && c.R > c.B * 1.28 && c.G > c.B * 1.05;
            if (teal || frameEdge) continue;
            cutout.SetPixel(x, y, c);
            visible[y * rect.Width + x] = true;
        }

        var largest = new List<int>();
        var queue = new List<int>();
        for (int pixel = 0; pixel < visible.Length; pixel++)
        {
            if (!visible[pixel]) continue;
            queue.Clear();
            queue.Add(pixel);
            visible[pixel] = false;
            for (int k = 0; k < queue.Count; k++)
            {
                int at = queue[k], x = at % rect.Width;
                if (x > 0) Visit(at - 1);
                if (x + 1 < rect.Width) Visit(at + 1);
                if (at >= rect.Width) Visit(at - rect.Width);
                if (at + rect.Width < visible.Length) Visit(at + rect.Width);
            }
            if (queue.Count > largest.Count) (largest, queue) = (queue, largest);
        }
        void Visit(int pixel)
        {
            if (!visible[pixel]) return;
            visible[pixel] = false;
            queue.Add(pixel);
        }

        int left = largest.Min(i => i % rect.Width);
        int right = largest.Max(i => i % rect.Width);
        int top = largest.Min(i => i / rect.Width);
        int bottom = largest.Max(i => i / rect.Width);
        var sprite = new Bitmap(right - left + 1, bottom - top + 1);
        foreach (int pixel in largest)
        {
            int x = pixel % rect.Width, y = pixel / rect.Width;
            sprite.SetPixel(x - left, y - top, cutout.GetPixel(x, y));
        }
        return sprite;
    }

    public static void Draw(Graphics g, int id, RectangleF body, int facing, int tick,
        bool walking, bool grounded, bool striking, int hurt)
    {
        if (id is 1 or 2)
        {
            BaghdadPreviewArt.DrawHero(g, body, facing, tick, walking, grounded, striking, hurt, id);
            return;
        }
        if (hurt > 0 && hurt / 4 % 2 == 0) return;
        Bitmap sprite = Sprites[id - 3].Value;
        float height = body.Height * 1.48f;
        float width = height * sprite.Width / sprite.Height;
        var state = g.Save();
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        ApplyPose(g, body, facing, tick, walking, grounded, striking);
        g.DrawImage(sprite, new RectangleF(-width / 2, -height, width, height));
        g.Restore(state);
    }
    internal static void ApplyPose(Graphics g, RectangleF body, int facing, int tick,
        bool walking, bool grounded, bool striking)
    {
        float stride = grounded && walking ? (float)Math.Sin(tick * .32f) : 0;
        g.TranslateTransform(body.X + body.Width / 2, body.Bottom - Math.Abs(stride) * 3);
        g.ScaleTransform(facing, 1);
        if (striking)
        {
            g.RotateTransform(13);
            g.ScaleTransform(1.06f, .96f);
        }
        else if (!grounded)
        {
            g.RotateTransform(-8);
            g.ScaleTransform(.94f, 1.04f);
        }
        else if (walking)
        {
            g.RotateTransform(6 + stride * 4);
            g.ScaleTransform(1 + stride * .035f, 1 - stride * .045f);
        }
    }
}
