namespace IraqPlatformer;

internal static class Verification
{
    public static void Run(IReadOnlyList<Stage> stages, string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (Stage stage in stages)
        {
            StageLayout layout = StageLayout.Build(stage);
            Reward[] rewards = Reward.Place(layout);
            Enemy[] enemies = Enemy.Place(layout, stage);
            Require(rewards.Length <= 64 && enemies.Length <= 32, "Save bitset capacity");
            Require(rewards.Count(r => r.Kind == RewardKind.Gem) == 1, "Stage gem");
            Require(layout.Platforms.Any(p => p.Top == 440 && p.Left <= layout.Flag && p.Right >= layout.Flag + 32), "Checkpoint floor");
            Require(layout.Platforms.Any(p => p.Top == 440 && p.Left <= layout.Gate && p.Right >= layout.Gate + 32), "Exit floor");
            if (stage.Boss is not null)
            {
                BossProfile boss = BossProfile.For(stage.Province);
                var frames = Enumerable.Range(0, 210).Select(tick => boss.At(layout.Gate - 190, tick, 3)).ToArray();
                Require(frames.Any(f => f.Open) && frames.Any(f => !f.Warning.IsEmpty), "Boss phases");
                Rectangle arena = layout.Platforms.Last(tile => tile.Top == 440);
                Require(frames.All(f => f.Body.Left >= arena.Left && f.Body.Right <= layout.Gate), "Boss bounds");
                Progress? victory = null, exit = null;
                using (var game = new MovementLab(1, "Verification", new Progress(Stage: stage.Province * 8 - 1),
                    stages, next =>
                    {
                        if (next.BossDefeated) victory = next;
                        else exit = next;
                    }, silent: true))
                    game.VerifyBossCombat();
                Require(victory is { BossDefeated: true, Score: 2000 }, "Boss victory save");
                Require(exit is { Score: 2000, Checkpoint: 0 } && exit.Stage == stage.Province * 8, "Boss exit save");
                using var exploration = RegionalScore.Compose(stage.Province, false);
                using var battle = RegionalScore.Compose(stage.Province, true);
                using var victoryCue = RegionalScore.Compose(stage.Province, false, 8, victory: true);
                Require(exploration.Length > battle.Length && battle.Length > victoryCue.Length && victoryCue.Length > 44, "Music arrangement");
            }
        }

        int[] regionalHero = [1, 3, 4, 5, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 6, 7, 8, 9];
        for (int index = 0; index < stages.Count; index++)
        {
            using var game = new MovementLab(index == 7 ? 2 : regionalHero[index / 8], index == 7 ? "باشا بغداد" : stages[index].ProvinceName, new Progress(Stage: index),
                stages, _ => { }, silent: true);
            if (stages[index].Boss is not null) game.PositionArenaPreview();
            using var frame = new Bitmap(game.Width, game.Height);
            game.DrawToBitmap(frame, new Rectangle(Point.Empty, frame.Size));
            Color topBar = frame.GetPixel(frame.Width / 2, 49);
            Require(topBar.ToArgb() == Color.MidnightBlue.ToArgb(), $"Stage {index + 1} render");
            if (stages[index].Number is 1 or 8 || index == 3)
                frame.Save(Path.Combine(directory, $"stage-{stages[index].Province}-{stages[index].Number}.png"),
                    System.Drawing.Imaging.ImageFormat.Png);
        }

        using (var sheet = new Bitmap(1000, 760))
        {
            using var graphics = Graphics.FromImage(sheet);
            graphics.Clear(Color.Wheat);
            for (int id = 1; id <= 20; id++)
            {
                int column = (id - 1) % 5, row = (id - 1) / 5;
                var body = new RectangleF(column * 200 + 74, row * 190 + 70, 52, 103);
                ApprovedHeroArt.Draw(graphics, id, body, 1, 0, false, true, false, 0);
                graphics.DrawString(id.ToString("D2"), SystemFonts.DefaultFont, Brushes.Black, column * 200 + 8, row * 190 + 8);
            }
            sheet.Save(Path.Combine(directory, "heroes-in-game.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        var slots = new SaveSlots(directory);
        for (int hero = 1; hero <= 20; hero++)
            slots.Save(hero, new Progress(Stage: hero, Coins: hero * 2, Pickups: 1UL << hero));
        using (var game = new MovementLab(1, "Verification", new Progress(Defeated: uint.MaxValue),
            stages, next => slots.Save(1, next), silent: true))
            game.VerifyTraversal();
        Require(new SaveSlots(directory).Load(1) is { Stage: 1, Checkpoint: 0 }, "Opening exit persistence");
        using (var game = new MovementLab(1, "Verification", new Progress(),
            stages, next => slots.Save(1, next), silent: true))
            game.VerifyTraversal(combat: true);
        Require(slots.Load(1) is { Stage: 1, Score: >= 100 }, "Opening route with enemies");
        foreach (int index in new[] { 3, 18, 43, 115 })
        {
            using var game = new MovementLab(1, "Verification", new Progress(Stage: index),
                stages, next => slots.Save(1, next), silent: true);
            game.VerifyTraversal(combat: true);
            Require(slots.Load(1)?.Stage == index + 1, "Integrated route progress");
        }
        var integration = new List<string>();
        foreach (Stage stage in stages)
        {
            int index = (stage.Province - 1) * 8 + stage.Number - 1;
            using var game = new MovementLab(1, "Verification", new Progress(Stage: index, BossDefeated: stage.Boss is not null),
                stages, next => slots.Save(1, next), silent: true);
            try
            {
                game.VerifyTraversal(combat: true, allowRetry: true);
                Require(slots.Load(1)?.Stage == index + 1, "Integrated route progress");
                integration.Add($"{stage.Province}-{stage.Number}: PASS");
            }
            catch (InvalidDataException error)
            {
                integration.Add($"{stage.Province}-{stage.Number}: FAIL {error.Message}");
            }
        }
        File.WriteAllLines(Path.Combine(directory, "integrated-routes.txt"), integration);
        for (int index = 1; index < stages.Count; index++)
        {
            using var game = new MovementLab(1, "Verification",
                new Progress(Stage: index, Defeated: uint.MaxValue, BossDefeated: true),
                stages, next => slots.Save(1, next), silent: true);
            game.VerifyTraversal();
        }
        using (var game = new MovementLab(1, "Verification", new Progress(Defeated: uint.MaxValue),
            stages, next => slots.Save(1, next), silent: true))
            game.VerifyMushroom();
        Require(slots.Load(1) is { Lives: 2, Grown: false, Pickups: not 0 }, "Mushroom persistence");
        using (var game = new MovementLab(1, "Verification", new Progress(),
            stages, next => slots.Save(1, next), silent: true))
            game.VerifyEnemyCombat();
        Require(slots.Load(1) is { Lives: 2, Defeated: 3, Score: 200 }, "Enemy defeat persistence");
        using (var game = new MovementLab(1, "Verification", new Progress(Stage: stages.Count - 1),
            stages, next => slots.Save(1, next), silent: true))
            game.VerifyHalabjaLamps();
        Require(slots.Load(1) is { Lit: true }, "Halabja lamp persistence");
        using (var game = new MovementLab(1, "Verification", new Progress(Defeated: uint.MaxValue),
            stages, next => slots.Save(1, next), silent: true))
            game.VerifySecretReward();
        Require(slots.Load(1) is { Gems: 1, Score: 500 }, "Secret gem persistence");
        for (int province = 0; province < 19; province++)
        {
            using var game = new MovementLab(1, "Verification", new Progress(Stage: province * 8, Defeated: uint.MaxValue - 1),
                stages, next => slots.Save(1, next), silent: true);
            game.VerifyRegionalTool();
            Require(slots.Load(1) is { Charges: 4, Score: 100 }, "Tool charge persistence");
        }
        slots.Save(1, new Progress(Stage: stages.Count, Score: 2000));
        slots = new SaveSlots(directory);
        Require(slots.Load(1) == new Progress(Stage: stages.Count, Score: 2000), "Campaign completion");
        for (int hero = 2; hero <= 20; hero++)
            Require(slots.Load(hero) == new Progress(Stage: hero, Coins: hero * 2, Pickups: 1UL << hero), "Independent hero save");
        File.WriteAllText(Path.Combine(directory, "hero-20.json"), "{bad");
        Require(slots.Load(20, out bool invalid) is null && invalid && slots.Load(19) is not null, "Corrupt save isolation");
        using (var picker = new HeroSelect(slots, stages))
        using (var preview = new Bitmap(picker.Width, picker.Height))
        {
            _ = picker.Handle;
            foreach (Control child in picker.Controls)
            {
                _ = child.Handle;
                foreach (Control nested in child.Controls) _ = nested.Handle;
            }
            picker.PerformLayout();
            picker.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
            preview.Save(Path.Combine(directory, "hero-select.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        File.WriteAllText(Path.Combine(directory, "verified.txt"), $"Validated {stages.Count} layouts, 19 bosses and music pairs, 20 save slots, 152 route traversals without combat or timed hazards, and 19 boss combat/save scenarios, mushroom growth, and enemy combat persistence.");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidDataException(label);
    }
}
