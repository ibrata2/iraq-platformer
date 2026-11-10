namespace IraqPlatformer;

internal enum ToolPath { Return, Roll, Arc, Bubble }
internal sealed record RegionalTool(string Name, ToolPath Path, Color Color)
{
    private static readonly RegionalTool[] Tools =
    [
        new("دوّارة خشب", ToolPath.Return, Color.Peru),
        new("كرة قماش", ToolPath.Roll, Color.Linen),
        new("دوّارة نحاس", ToolPath.Arc, Color.Goldenrod),
        new("فقاعة ماي", ToolPath.Bubble, Color.DeepSkyBlue),
        new("برتقالة نابضة", ToolPath.Arc, Color.Orange),
        new("كرة رمل", ToolPath.Roll, Color.Tan),
        new("قرص الآجر", ToolPath.Roll, Color.RoyalBlue),
        new("صدفة دوّارة", ToolPath.Return, Color.LightCoral),
        new("كرة ملح", ToolPath.Arc, Color.Snow),
        new("فقاعة قافزة", ToolPath.Bubble, Color.Aqua),
        new("حبّة قمح", ToolPath.Arc, Color.Khaki),
        new("دوّارة الرمل", ToolPath.Return, Color.BurlyWood),
        new("مروحة قصب", ToolPath.Return, Color.OliveDrab),
        new("عقدة بردي", ToolPath.Arc, Color.Olive),
        new("كرة تمر", ToolPath.Roll, Color.SaddleBrown),
        new("دوّارة جبلية", ToolPath.Arc, Color.MediumPurple),
        new("كرة صوف", ToolPath.Arc, Color.Bisque),
        new("دوّارة ورق", ToolPath.Return, Color.OrangeRed),
        new("فانوس الوادي", ToolPath.Bubble, Color.Gold)
    ];

    public static RegionalTool For(int province) => Tools[province - 1];

    public RectangleF At(PointF start, int direction, int elapsed)
    {
        float distance = elapsed * 9f;
        float lift = Path switch
        {
            ToolPath.Roll => -16,
            ToolPath.Arc => elapsed * (25 - elapsed) * .32f,
            ToolPath.Bubble => elapsed * 2.4f,
            _ => 0
        };
        if (Path == ToolPath.Return)
            distance = Math.Min(elapsed, 26 - elapsed) * 12f;
        return new RectangleF(start.X + direction * distance - 9, start.Y - lift - 9, 18, 18);
    }
}
