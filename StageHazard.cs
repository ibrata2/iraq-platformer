namespace IraqPlatformer;

internal enum HazardKind { Boulder, FallingStone, Wind, Water }
internal readonly record struct StageHazard(int X, HazardKind Kind, int Phase)
{
    public bool Damaging => Kind is HazardKind.Boulder or HazardKind.FallingStone;
    public int Cycle(int tick) => (tick + Phase) % 230;

    public Rectangle Bounds(int tick)
    {
        int elapsed = Cycle(tick) - 55;
        if (elapsed is < 0 or >= 52) return Rectangle.Empty;
        return Kind switch
        {
            HazardKind.Boulder => new Rectangle(X + 114 - elapsed * 2, 410, 29, 30),
            HazardKind.FallingStone when elapsed < 35 => new Rectangle(X, 205 + elapsed * 7, 26, 27),
            HazardKind.FallingStone => Rectangle.Empty,
            HazardKind.Wind => new Rectangle(X, 240, 115, 200),
            _ => new Rectangle(X, 418, 142, 22)
        };
    }

    public static StageHazard[] Place(StageLayout layout, Stage stage)
    {
        if (stage.Number < 3 || stage is { Province: 5, Number: 3 } or { Province: 3, Number: 5 }) return [];
        var floors = layout.Platforms.Where(p => p.Top == 440).ToArray();
        HazardKind kind = stage.Province switch
        {
            6 or 9 or 12 or 16 or 19 => HazardKind.Wind,
            17 when stage.Number == 4 => HazardKind.FallingStone,
            3 or 17 or 18 => HazardKind.Boulder,
            5 when stage.Number == 5 => HazardKind.Boulder,
            _ => HazardKind.Water
        };
        var hazards = new List<StageHazard>();
        for (int i = 2; i < floors.Length - 1; i += stage.Number >= 6 ? 2 : 3)
            hazards.Add(new StageHazard(floors[i].Left + (kind == HazardKind.FallingStone ? 241 : 355), kind, i * 37));
        return hazards.ToArray();
    }

    public void Draw(Graphics g, int tick)
    {
        if (Cycle(tick) < 55)
        {
            g.FillRectangle(Brushes.Gold, X, 434, Kind == HazardKind.FallingStone ? 26 : 150, 5);
            return;
        }
        Rectangle bounds = Bounds(tick);
        if (bounds.IsEmpty) return;
        if (Damaging)
        {
            g.FillEllipse(Brushes.SlateGray, bounds);
            g.DrawEllipse(Pens.DarkSlateGray, bounds);
        }
        else if (Kind == HazardKind.Wind)
        {
            for (int i = 0; i < 4; i++)
            {
                int left = X + (Cycle(tick) * 4 + i * 38) % 75;
                g.DrawLine(Pens.AntiqueWhite, left, 316 + i * 27, left + 40, 316 + i * 27);
            }
        }
        else
        {
            g.FillRectangle(Brushes.CadetBlue, bounds);
            g.DrawLine(Pens.LightCyan, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
        }
    }
}
