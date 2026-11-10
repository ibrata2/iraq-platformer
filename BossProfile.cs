namespace IraqPlatformer;

internal enum BossMove { Charge, Leap, Boulder, Sweep, Dive, Lamps }
internal readonly record struct BossFrame(RectangleF Body, RectangleF Warning, RectangleF Hazard, bool Open);

internal sealed record BossProfile(BossMove Move, Color Color, int Windup, int Distance)
{
    private static readonly BossProfile[] Profiles =
    [
        new(BossMove.Charge, Color.Peru, 40, 226),
        new(BossMove.Boulder, Color.SaddleBrown, 46, 260),
        new(BossMove.Dive, Color.IndianRed, 48, 205),
        new(BossMove.Leap, Color.OrangeRed, 43, 190),
        new(BossMove.Boulder, Color.DarkOrange, 42, 242),
        new(BossMove.Sweep, Color.DarkGoldenrod, 40, 155),
        new(BossMove.Charge, Color.RoyalBlue, 45, 235),
        new(BossMove.Sweep, Color.Coral, 44, 134),
        new(BossMove.Charge, Color.MistyRose, 42, 210),
        new(BossMove.Leap, Color.OliveDrab, 43, 219),
        new(BossMove.Leap, Color.YellowGreen, 37, 199),
        new(BossMove.Charge, Color.Olive, 38, 215),
        new(BossMove.Sweep, Color.DarkOliveGreen, 39, 156),
        new(BossMove.Leap, Color.SeaGreen, 40, 186),
        new(BossMove.Sweep, Color.MediumVioletRed, 42, 192),
        new(BossMove.Dive, Color.Sienna, 41, 215),
        new(BossMove.Charge, Color.BurlyWood, 37, 231),
        new(BossMove.Boulder, Color.SlateGray, 39, 248),
        new(BossMove.Lamps, Color.MediumPurple, 46, 174)
    ];

    public static BossProfile For(int province) => Profiles[province - 1];

    public BossFrame At(float anchor, int tick, int health)
    {
        int windup = Windup - (3 - health) * 5;
        const int attackLength = 40;
        int cycle = tick % (windup + attackLength + 82);
        bool active = cycle >= windup && cycle < windup + attackLength;
        bool open = cycle >= windup + attackLength;
        float motion = Math.Clamp((cycle - windup) / (float)attackLength, 0, 1);
        var body = new RectangleF(anchor, 376, 64, 64);
        var warning = new RectangleF(anchor - Distance, 432, Distance + 64, 8);
        RectangleF hazard = RectangleF.Empty;
        switch (Move)
        {
            case BossMove.Charge:
                body.X -= Distance * motion;
                break;
            case BossMove.Leap:
                body.X -= Distance * motion;
                body.Y -= 102 * (float)Math.Sin(motion * Math.PI);
                break;
            case BossMove.Dive:
                body.X -= Distance * motion;
                body.Y -= (1 - motion) * 96;
                break;
            case BossMove.Boulder:
                if (active) hazard = new RectangleF(anchor - Distance * motion, 409, 31, 31);
                break;
            case BossMove.Sweep:
                warning.Width = Distance;
                if (active)
                    hazard = new RectangleF(anchor - Distance, 421, Distance, 19);
                break;
            case BossMove.Lamps:
                warning = new RectangleF(anchor - Distance + 34, 432, 95, 8);
                if (active) hazard = new RectangleF(warning.X, 375, warning.Width, 65);
                break;
        }

        return new BossFrame(body, cycle < windup ? warning : RectangleF.Empty, hazard, open);
    }
}
