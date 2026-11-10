namespace IraqPlatformer;

internal enum EnemyMotion { Patrol, Fly, Hop }

internal sealed record Enemy(int Anchor, EnemyMotion Motion, int Phase)
{
    public RectangleF Bounds(int tick)
    {
        double phase = (tick + Phase) * .024;
        float x = Anchor + (float)Math.Sin(phase) * 46;
        float lift = Motion switch
        {
            EnemyMotion.Fly => 62 + (float)Math.Sin(phase * 1.4) * 29,
            EnemyMotion.Hop => Math.Max(0, (float)Math.Sin(phase * 2)) * 42,
            _ => 0
        };
        return new RectangleF(x, 410 - lift, 36, 30);
    }

    public static Enemy[] Place(StageLayout layout, Stage stage)
    {
        var enemies = new List<Enemy>();
        Rectangle[] floors = layout.Platforms.Where(tile => tile.Top == 440).ToArray();
        for (int section = 1; section < floors.Length; section++)
        {
            if (stage.Boss is not null && section == floors.Length - 1) break;
            int x = floors[section].Left + 305;
            var motion = section == 1 || stage.Number < 3
                ? EnemyMotion.Patrol : (EnemyMotion)((section + stage.Province) % 3);
            enemies.Add(new Enemy(x, motion, section * 53));
        }
        return enemies.ToArray();
    }
}
