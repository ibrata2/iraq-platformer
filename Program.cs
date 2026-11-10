namespace IraqPlatformer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var stages = Campaign.Load();
        if (args is ["--verify", var directoryForVerification])
        {
            ApplicationConfiguration.Initialize();
            try { Verification.Run(stages, directoryForVerification); }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                Environment.ExitCode = 1;
            }
            return;
        }
        ApplicationConfiguration.Initialize();
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IraqPlatformer", "Saves");
        Application.Run(new HeroSelect(new SaveSlots(directory), stages));
    }
}

internal sealed class MovementLab : Form
{
    private StageLayout layout = null!;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly RegionalScore music = new();
    private readonly HashSet<Keys> held = [];

    private readonly string hero;
    private readonly bool silent;
    private readonly int heroId;
    private readonly IReadOnlyList<Stage> stages;
    private readonly Action<Progress> save;
    private Progress progress;
    private float x, y, vx, vy;
    private bool grounded, jump, paused;
    private int strike, facing = 1;
    private static readonly string[] BossNames =
    [
        "حارس الطابوق", "دبّ السور", "عثّة القلعة", "سلطعون الساقية", "خنفساء البرتقال",
        "عقرب الكثبان", "ثور الآجر الأزرق", "سرطان البحيرة", "مدرّع الملح", "ضفدع السدّة",
        "جرادة الحصاد", "ورل البادية", "وحش القصب", "سلحفاة الهور", "أخطبوط المرسى",
        "صقر القلعة", "كبش الوادي", "عملاق الضباب", "عملاق المصابيح"
    ];
    private int bossHealth, bossTick, hurt;
    private BossProfile bossProfile = null!;
    private BossFrame bossFrame;
    private bool inHalabja, arenaLit;
    private int SecretLamp => layout.SecretLedge.Left - 182;
    private int ArenaLamp => layout.Gate - 405;
    private int ArenaFlag => layout.Platforms.Last(tile => tile.Top == 440).Left + 20;
    private bool SecretVisible => !inHalabja || progress.Lit;
    private bool BossExposed => bossFrame.Open && (!inHalabja || arenaLit);
    private bool strikeHit, ranged;
    private RegionalTool tool = null!;
    private PointF launch;
    private int launchDirection;
    private Reward[] rewards = [];
    private int mushroomIndex;
    private int mushroomDrop;
    private Rectangle MushroomBox => new(rewards[mushroomIndex].Bounds.X - 4, 304, 31, 28);
    private bool MushroomCollected => (progress.Pickups & (1UL << mushroomIndex)) != 0;
    private Enemy[] enemies = [];
    private int stageTick;
    private readonly Dictionary<Rectangle, int> crumbleStarts = new();
    private HashSet<Rectangle> fragile = [];
    private StageHazard[] hazards = [];
    private int riding = -1;
    private int coyote;
    private bool IsDefeated(int index) => (progress.Defeated & (1U << index)) != 0;
    private RectangleF Attack
    {
        get
        {
            if (ranged) return tool.At(launch, launchDirection, 26 - strike);
            return new RectangleF(facing > 0 ? x + 19 : x - 39, y - 10, 52, 56);
        }
    }
    private RectangleF Body => new(x, y + (progress.Grown ? -8 : 0), 32, progress.Grown ? 54 : 46);

    public MovementLab(int heroId, string hero, Progress progress, IReadOnlyList<Stage> stages, Action<Progress> save, bool silent = false)
    {
        this.heroId = heroId;
        this.hero = hero;
        this.silent = silent;
        this.progress = progress;
        this.stages = stages;
        this.save = save;
        if (silent) music.Toggle();
        LoadStage();
        ClientSize = new Size(960, 520);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        DoubleBuffered = true;
        KeyDown += (_, e) =>
        {
            if (!held.Add(e.KeyCode)) return;
            if (e.KeyCode == Keys.Escape)
            {
                paused = !paused;
                jump = false;
                held.Remove(Keys.Left);
                held.Remove(Keys.Right);
                held.Remove(Keys.A);
                held.Remove(Keys.D);
                Invalidate();
                return;
            }
            if (paused)
            {
                if (e.KeyCode == Keys.Enter) Close();
                return;
            }
            if (e.KeyCode == Keys.E) InteractWithLamp();
            if (e.KeyCode == Keys.M) music.Toggle();
            if (e.KeyCode == Keys.F1)
            {
                timer.Stop();
                try { Credits.Show(this); }
                finally { held.Clear(); timer.Start(); }
                return;
            }
            if (e.KeyCode is Keys.Space or Keys.Up or Keys.W) jump = true;
            if (e.KeyCode is Keys.X or Keys.J && strike == 0)
            {
                StartAttack(e.KeyCode == Keys.J);
            }
        };
        KeyUp += (_, e) => held.Remove(e.KeyCode);
        Deactivate += (_, _) => { held.Clear(); jump = false; };
        FormClosing += (_, _) => timer.Stop();
        timer.Tick += (_, _) =>
        {
            if (paused) return;
            Step();
            Invalidate();
        };
        timer.Start();
    }

    private void StartAttack(bool useTool)
    {
        strike = 14;
        strikeHit = false;
        ranged = useTool && progress.Charges > 0;
        if (!ranged) return;
        progress = progress with { Charges = progress.Charges - 1 };
        strike = 26;
        launchDirection = facing;
        launch = new PointF(x + 16 + 20 * facing, y + 21);
    }

    internal void VerifyRegionalTool()
    {
        timer.Stop();
        Rectangle pickup = rewards.Single(reward => reward.Kind == RewardKind.Tool).Bounds;
        x = pickup.Left;
        y = 394;
        Step();
        if (progress.Charges != 5) throw new InvalidDataException("Tool pickup failed.");
        x = enemies[0].Bounds(stageTick + 1).Left - 51;
        facing = 1;
        StartAttack(useTool: true);
        for (int i = 0; i < 8; i++) Step();
        if (!IsDefeated(0) || progress.Charges != 4 || progress.Score != 100)
            throw new InvalidDataException("Projectile hit or charge accounting failed.");
        for (int i = 0; i < 30; i++) Step();
        if (progress.Score != 100 || !TakeHit())
            throw new InvalidDataException("Projectile score repeated or save did not run.");
    }

    internal void VerifyEnemyCombat()
    {
        timer.Stop();
        RectangleF first = enemies[0].Bounds(1);
        x = first.Left;
        y = first.Top - 49;
        vy = 6;
        Step();
        if (!IsDefeated(0) || progress.Score != 100 || vy >= 0)
            throw new InvalidDataException("Enemy stomp or bounce failed.");

        RectangleF second = enemies[1].Bounds(stageTick + 1);
        x = second.Left - 36;
        y = 394;
        vx = vy = 0;
        strike = 14;
        facing = 1;
        Step();
        if (!IsDefeated(1) || progress.Score != 200 || !strikeHit)
            throw new InvalidDataException("Enemy strike failed.");
        Step();
        if (progress.Score != 200 || !TakeHit())
            throw new InvalidDataException("Enemy score repeated or damage failed.");
        if (!IsDefeated(0) || !IsDefeated(1) || progress.Lives != 2)
            throw new InvalidDataException("Enemy defeats were lost on reload.");
    }

    private void InteractWithLamp()
    {
        if (!inHalabja || y <= 340) return;
        if (Math.Abs(x + 16 - SecretLamp) < 54 && !progress.Lit)
        {
            progress = progress with { Lit = true };
            save(progress);
        }
        if (bossHealth > 0 && Math.Abs(x + 16 - ArenaLamp) < 54 &&
            bossFrame.Hazard.IsEmpty)
            arenaLit = true;
    }

    internal void VerifyHalabjaLamps()
    {
        timer.Stop();
        if (SecretVisible) throw new InvalidDataException("Secret starts visible.");
        x = SecretLamp - 16;
        y = 394;
        InteractWithLamp();
        LoadStage();
        if (!SecretVisible) throw new InvalidDataException("Secret lamp reset.");
        x = ArenaLamp - 16;
        y = 394;
        InteractWithLamp();
        if (!arenaLit) throw new InvalidDataException("Arena lamp did not light.");
        bossTick = Enumerable.Range(0, 180).First(t =>
            !bossProfile.At(layout.Gate - 190, t, bossHealth).Hazard.IsEmpty);
        UpdateBoss();
        InteractWithLamp();
        if (arenaLit) throw new InvalidDataException("Arena hazard did not suppress lamp.");
        bossTick = Enumerable.Range(0, 180).First(t =>
            bossProfile.At(layout.Gate - 190, t, bossHealth).Open);
        UpdateBoss();
        InteractWithLamp();
        if (!BossExposed) throw new InvalidDataException("Lamp did not expose the boss.");
    }

    internal void VerifySecretReward()
    {
        timer.Stop();
        int gemIndex = Array.FindIndex(rewards, reward => reward.Kind == RewardKind.Gem);
        Rectangle gem = rewards[gemIndex].Bounds;
        x = gem.Left - 8;
        y = layout.SecretLedge.Top - 46;
        Step();
        if (progress.Gems != 1 || progress.Score != 500 || (progress.Pickups & (1UL << gemIndex)) == 0)
            throw new InvalidDataException("Secret gem pickup failed.");
        if (!TakeHit()) throw new InvalidDataException("Expected reload after damage.");
        x = gem.Left - 8;
        y = layout.SecretLedge.Top - 46;
        Step();
        if (progress.Gems != 1 || progress.Score != 500)
            throw new InvalidDataException("Secret gem counted twice.");
    }

    internal void VerifyMushroom()
    {
        timer.Stop();
        x = MushroomBox.Left;
        y = 394;
        grounded = true;
        jump = true;
        for (int tick = 0; tick < 100 && !progress.Grown; tick++) Step();
        if (!progress.Grown || !MushroomCollected || TakeHit() || progress.Grown || progress.Lives != 3)
            throw new InvalidDataException("Mushroom or growth protection failed.");
        if (!TakeHit() || progress.Lives != 2 || !MushroomCollected)
            throw new InvalidDataException("Collected mushroom returned after losing a life.");
    }

    internal void VerifyTraversal(bool combat = false, bool allowRetry = false)
    {
        timer.Stop();
        Rectangle[] floors = layout.Platforms.Where(tile => tile.Top == 440).ToArray();
        int start = progress.Stage;
        if (!combat) hazards = [];
        held.Add(Keys.Right);
        int maxTicks = allowRetry ? 7200 : 2400;
        for (int tick = 0; tick < maxTicks && progress.Stage == start; tick++)
        {
            if ((grounded || coyote > 1 && vy >= 0) && (vx == 0 || floors.Take(floors.Length - 1).Any(tile =>
                x >= tile.Left && tile.Right - x is >= -12 and <= 22)))
                jump = true;
            if (combat && grounded && hazards.Where(h => h.Kind == HazardKind.Boulder)
                .Select(h => h.Bounds(stageTick + 6))
                .Any(b => !b.IsEmpty && b.Left >= Body.Right && b.Left - Body.Right < 72))
                jump = true;
            if (combat && strike == 0 && enemies.Where((_, i) => !IsDefeated(i))
                .Any(enemy => new RectangleF(x + 17, y - 11, 64, 58).IntersectsWith(enemy.Bounds(stageTick + 1))))
            {
                strike = 14;
                strikeHit = false;
            }
            bool waitForStone = combat && hazards.Any(h => h.Kind == HazardKind.FallingStone &&
                h.Cycle(stageTick) is >= 27 and < 90 && h.X - Body.Right is >= 0 and < 40);
            if (waitForStone || combat && !grounded && vy > 0 && coyote <= 1 &&
                floors.Take(floors.Length - 1).Any(tile => x >= tile.Left && tile.Right - x is >= 6 and <= 48))
                held.Remove(Keys.Right);
            else
                held.Add(Keys.Right);
            float beforeX = x, beforeY = y;
            int beforeLives = progress.Lives;
            Step();
            if ((!allowRetry && progress.Lives < 3) || (beforeLives == 1 && progress.Lives == 3))
                throw new InvalidDataException($"Traversal of stage {start + 1} lost a life at x={beforeX}, y={beforeY}.");
        }
        if (progress.Stage != start + 1 || !allowRetry && progress.Lives < 3)
            throw new InvalidDataException($"Traversal of stage {start + 1} failed at x={x}, y={y}, stage={progress.Stage}, lives={progress.Lives}.");
    }

    internal void PositionArenaPreview()
    {
        timer.Stop();
        x = layout.Gate - 435;
        y = 394;
        facing = 1;
    }

    internal void VerifyBossCombat()
    {
        timer.Stop();
        int stageIndex = progress.Stage;
        x = layout.Gate + 5;
        y = 394;
        Step();
        if (progress.Stage != stageIndex || x >= layout.Gate || bossHealth != 3)
            throw new InvalidDataException("Locked boss exit allowed a transition.");
        for (int hit = 0; hit < 3; hit++)
        {
            int health = bossHealth;
            float anchor = layout.Gate - 190;
            bossTick = Enumerable.Range(0, 180).First(tick => bossProfile.At(anchor, tick, health).Open);
            BossFrame target = bossProfile.At(anchor, bossTick, health);
            arenaLit = true;
            x = target.Body.Left - 37;
            y = 394;
            facing = 1;
            strike = 14;
            strikeHit = false;
            if (UpdateBoss() || bossHealth != health - 1)
                throw new InvalidDataException("Boss strike failed.");
            UpdateBoss();
            if (bossHealth != health - 1)
                throw new InvalidDataException("One strike counted twice.");
        }
        if (!progress.BossDefeated || progress.Score != 2000)
            throw new InvalidDataException("Boss victory was not recorded.");

        LoadStage();
        if (bossHealth != 0)
            throw new InvalidDataException("Defeated boss returned after reloading.");
        x = layout.Gate - 1;
        y = 394;
        held.Add(Keys.Right);
        for (int tick = 0; tick < 10 && progress.Stage == stageIndex; tick++) Step();
        if (progress.Stage != stageIndex + 1 || progress.BossDefeated || progress.Score != 2000)
            throw new InvalidDataException("Unlocked boss exit did not save progress.");
    }

    private void Step()
    {
        int direction = (held.Contains(Keys.Right) || held.Contains(Keys.D) ? 1 : 0)
            - (held.Contains(Keys.Left) || held.Contains(Keys.A) ? 1 : 0);
        if (direction != 0) facing = direction;
        vx = direction == 0 ? vx * .75f : Math.Clamp(vx + direction * .85f, -6.5f, 6.5f);
        if (grounded) coyote = 5;
        else if (coyote > 0) coyote--;
        if (jump && coyote > 0)
        {
            coyote = 0;
            vy = -13f;
            riding = -1;
        }
        jump = false;
        if (riding >= 0)
        {
            var platform = layout.Moving[riding];
            Rectangle previous = platform.At(stageTick);
            Rectangle next = platform.At(stageTick + 1);
            x += next.X - previous.X;
            y += next.Y - previous.Y;
        }
        stageTick++;
        foreach (StageHazard hazard in hazards)
        {
            if (!Body.IntersectsWith(hazard.Bounds(stageTick))) continue;
            if (hazard.Kind == HazardKind.Wind && !grounded)
                vx = Math.Clamp(vx - .42f, -6.5f, 6.5f);
            if (hazard.Kind == HazardKind.Water && grounded)
                vx *= .72f;
        }
        riding = -1;

        int rightLimit = bossHealth > 0 ? layout.Gate - 34 : layout.Width - 32;
        x = Math.Clamp(x + vx, 0, rightLimit);
        foreach (Rectangle tile in layout.Platforms)
        {
            if (tile.Top < 440) continue;
            if (!new RectangleF(x, y, 32, 46).IntersectsWith(tile)) continue;
            x = vx > 0 ? tile.Left - 32 : tile.Right;
            vx = 0;
            break;
        }

        float previousBottom = Body.Bottom;
        float previousTop = Body.Top;
        vy = Math.Min(15, vy + .62f);
        y += vy;
        grounded = false;
        Rectangle box = MushroomBox;
        if (vy < 0 && mushroomDrop == 0 && !MushroomCollected &&
            previousTop >= box.Bottom && Body.Top <= box.Bottom &&
            Body.Right > box.Left && Body.Left < box.Right)
        {
            y += box.Bottom - Body.Top;
            vy = 0;
            mushroomDrop = 30;
        }
        else if (mushroomDrop > 1)
            mushroomDrop--;

        foreach (Rectangle tile in layout.Platforms)
        {
            if (!SecretVisible && tile == layout.SecretLedge || CrumbleAge(tile) is >= 32 and < 154) continue;
            if (tile.Top < 440 && (vy <= 0 || previousBottom > tile.Top)) continue;
            if (!new RectangleF(x, y, 32, 46).IntersectsWith(tile)) continue;
            grounded = vy > 0;
            if (grounded && fragile.Contains(tile) && (CrumbleAge(tile) == -1 || CrumbleAge(tile) >= 154))
                crumbleStarts[tile] = stageTick;
            y = grounded ? tile.Top - 46 : tile.Bottom;
            vy = 0;
            break;
        }

        for (int i = 0; !grounded && i < layout.Moving.Length; i++)
        {
            var platform = layout.Moving[i];
            Rectangle tile = platform.At(stageTick);
            if (vy >= 0 && previousBottom <= Math.Max(tile.Top, platform.At(stageTick - 1).Top)
                && Body.Bottom >= tile.Top && Body.Right > tile.Left && Body.Left < tile.Right)
            {
                y = tile.Top - 46;
                vy = 0;
                grounded = true;
                riding = i;
            }
        }

        for (int i = 0; i < rewards.Length; i++)
        {
            if ((progress.Pickups & (1UL << i)) == 0 &&
                (i != mushroomIndex || mushroomDrop == 1) && (!rewards[i].Hidden || SecretVisible) && Body.IntersectsWith(rewards[i].Bounds))
                progress = rewards[i].Collect(progress, i);
        }
        if (hurt == 0 && hazards.Any(h => h.Damaging && Body.IntersectsWith(h.Bounds(stageTick))) && TakeHit())
            return;
        if (UpdateEnemies(previousBottom) || bossHealth > 0 && UpdateBoss()) return;
        if (hurt > 0) hurt--;

        if (y > 560)
        {
            int lives = progress.Lives - 1;
            progress = lives > 0 ? progress with { Lives = lives, Grown = false } : progress with { Lives = 3, Checkpoint = 0, Grown = false };
            save(progress);
            LoadStage();
            return;
        }

        if (grounded && x >= layout.Flag && x < layout.Flag + 55 && progress.Checkpoint == 0)
        {
            progress = progress with { Checkpoint = 1 };
            save(progress);
        }

        if (grounded && stages[progress.Stage].Boss is not null && x >= ArenaFlag && progress.Checkpoint < 2)
        {
            progress = progress with { Checkpoint = 2 };
            save(progress);
        }

        if (grounded && x >= layout.Gate && bossHealth == 0)
        {
            progress = progress with { Stage = progress.Stage + 1, Checkpoint = 0, Pickups = 0, Defeated = 0, Lit = false, BossDefeated = false };
            save(progress);
            if (progress.Stage == stages.Count)
            {
                timer.Stop();
                if (!silent)
                {
                    MessageBox.Show(this, "اكتملت الرحلة!", "العراق");
                    Close();
                }
                return;
            }
            LoadStage();
        }
        if (strike > 0) strike--;
    }

    private void LoadStage()
    {
        Stage stage = stages[progress.Stage];
        inHalabja = stage.Province == 19;
        arenaLit = false;
        music.Play(stage.Province, false, stage.Number);
        layout = StageLayout.Build(stage);
        hazards = StageHazard.Place(layout, stage);
        rewards = Reward.Place(layout);
        mushroomIndex = Array.FindIndex(rewards, reward => reward.Kind == RewardKind.Mushroom);
        mushroomDrop = 0;
        enemies = Enemy.Place(layout, stage);
        stageTick = 0;
        crumbleStarts.Clear();
        bool useFragile = stage is { Province: 1, Number: 3 } or { Province: 3, Number: 5 } ||
            (stage.Province is 2 or 7 or 12 && stage.Number >= 5);
        fragile = useFragile
            ? layout.Platforms.Where(tile => tile.Top is > 312 and < 390 &&
                Math.Abs(tile.Left - layout.SecretLedge.Left) > 190).ToHashSet()
            : [];
        riding = -1;
        coyote = 0;
        Text = $"رحلة العراق | {hero} | {stage.ProvinceName} {stage.Code} | {stage.Title}";
        x = progress.Checkpoint switch { 2 => ArenaFlag, 1 => layout.Flag, _ => 60 };
        y = 394;
        vx = vy = 0;
        grounded = jump = false;
        strike = hurt = bossTick = 0;
        strikeHit = ranged = false;
        tool = RegionalTool.For(stage.Province);
        bossHealth = stage.Boss is null || progress.BossDefeated ? 0 : 3;
        bossProfile = BossProfile.For(stage.Province);
        bossFrame = bossProfile.At(layout.Gate - 190, 0, bossHealth);
    }



    private bool UpdateBoss()
    {
        float anchor = layout.Gate - 190;
        if (x < anchor - 330) return false;
        if (bossTick == 0) music.Play(stages[progress.Stage].Province, true, 8);
        bossFrame = bossProfile.At(anchor, bossTick++, bossHealth);
        if (!bossFrame.Hazard.IsEmpty && inHalabja) arenaLit = false;
        if (BossExposed && strike > 0 && !strikeHit && Attack.IntersectsWith(bossFrame.Body))
        {
            strikeHit = true;
            if (--bossHealth == 0)
            {
                progress = progress with { Score = progress.Score + 2000, BossDefeated = true };
                save(progress);
                music.Play(stages[progress.Stage].Province, false, 8, nextVictory: true);
                return false;
            }
        }
        return hurt == 0 && (Body.IntersectsWith(bossFrame.Body) || Body.IntersectsWith(bossFrame.Hazard)) && TakeHit();
    }

    private bool UpdateEnemies(float previousBottom)
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            if (IsDefeated(i)) continue;
            RectangleF bounds = enemies[i].Bounds(stageTick);
            bool touch = Body.IntersectsWith(bounds);
            bool stomp = touch && vy > 0 && previousBottom <= bounds.Top + 5;
            bool hit = strike > 0 && !strikeHit && Attack.IntersectsWith(bounds);
            if (stomp || hit)
            {
                progress = progress with { Defeated = progress.Defeated | (1U << i), Score = progress.Score + 100 };
                if (stomp) { y = bounds.Top - 46; vy = -9; }
                if (hit) strikeHit = true;
            }
            else if (touch && hurt == 0 && TakeHit()) return true;
        }
        return false;
    }

    private bool TakeHit()
    {
        if (progress.Grown)
        {
            progress = progress with { Grown = false };
            hurt = 65;
            return false;
        }
        int lives = progress.Lives - 1;
        progress = lives > 0 ? progress with { Lives = lives } : progress with { Lives = 3, Checkpoint = 0 };
        save(progress);
        LoadStage();
        hurt = 60;
        return true;
    }

    private int CrumbleAge(Rectangle tile) => crumbleStarts.TryGetValue(tile, out int start) ? stageTick - start : -1;

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        float camera = Math.Clamp(x - 360, 0, Math.Max(0, layout.Width - ClientSize.Width));
        bool baghdadPreview = stages[progress.Stage].Province == 1;
        if (baghdadPreview) BaghdadPreviewArt.DrawBackground(g, ClientSize.Width, camera, stages[progress.Stage].Number);
        else if (stages[progress.Stage].Province < 19)
            RegionalReferenceArt.Draw(g, ClientSize, stages[progress.Stage], camera);
        else StageArt.Draw(g, ClientRectangle, stages[progress.Stage], camera);
        g.TranslateTransform(-camera, 0);
        foreach (Rectangle tile in layout.Platforms)
        {
            if (!SecretVisible && tile == layout.SecretLedge || CrumbleAge(tile) is >= 32 and < 154) continue;
            if (baghdadPreview) BaghdadPreviewArt.DrawTerrain(g, tile, camera, ClientSize.Width);
            else if (stages[progress.Stage].Province < 19)
                RegionalReferenceArt.DrawTerrain(g, tile, stages[progress.Stage].Province, camera, ClientSize.Width);
            else StageArt.DrawTerrain(g, tile, camera, ClientSize.Width, stages[progress.Stage].Province);
            if (fragile.Contains(tile))
            {
                Pen crack = CrumbleAge(tile) >= 0 && CrumbleAge(tile) < 32 ? Pens.OrangeRed : Pens.Khaki;
                g.DrawLines(crack, (Point[])[new(tile.Left + 19, tile.Top + 3),
                    new(tile.Left + 27, tile.Top + 8), new(tile.Left + 23, tile.Top + 14)]);
            }
        }
        foreach (StageHazard hazard in hazards) hazard.Draw(g, stageTick);
        Rectangle box = MushroomBox;
        g.FillRectangle(mushroomDrop == 0 && !MushroomCollected ? Brushes.Goldenrod : Brushes.Peru, box);
        g.DrawRectangle(Pens.SaddleBrown, box);
        if (mushroomDrop == 0 && !MushroomCollected)
            g.DrawString("?", Font, Brushes.White, box.X + 9, box.Y + 5);
        else if (mushroomDrop > 1)
            RewardArt.Draw(g, RewardKind.Mushroom, new Rectangle(box.X + 5, box.Bottom + 3 * (30 - mushroomDrop), 23, 18));
        Rectangle secret = layout.SecretLedge;
        if (SecretVisible)
            g.DrawLine(Pens.Goldenrod, secret.Left + 8, secret.Top + 4, secret.Right - 8, secret.Top + 4);
        if (inHalabja)
        {
            DrawLamp(g, SecretLamp, progress.Lit);
            if (bossHealth > 0) DrawLamp(g, ArenaLamp, arenaLit);
        }

        foreach (MovingPlatform platform in layout.Moving)
        {
            Rectangle tile = platform.At(stageTick);
            if (platform.TravelY > 20)
                g.DrawLine(Pens.SaddleBrown, tile.Left + tile.Width / 2, 260, tile.Left + tile.Width / 2, tile.Top);
            g.FillRectangle(Brushes.SaddleBrown, tile);
            g.FillRectangle(Brushes.Goldenrod, tile.X, tile.Y, tile.Width, 4);
            g.DrawRectangle(Pens.Black, tile);
        }

        for (int i = 0; i < rewards.Length; i++)
        {
            Reward reward = rewards[i];
            if ((progress.Pickups & (1UL << i)) != 0 || i == mushroomIndex && mushroomDrop != 1 || reward.Hidden && (!SecretVisible || Math.Abs(reward.Bounds.X - x) > 150)) continue;
            RewardArt.Draw(g, reward.Kind, reward.Bounds);
        }
        for (int i = 0; i < enemies.Length; i++)
        {
            if (IsDefeated(i)) continue;
            RectangleF bounds = enemies[i].Bounds(stageTick);
            EnemyArt.Draw(g, enemies[i].Motion, bounds, stageTick);
        }
        ApprovedHeroArt.Draw(g, heroId, Body, facing, stageTick, Math.Abs(vx) > .6f, grounded, strike > 0, hurt);
        if (strike > 0 && ranged && !strikeHit)
        {
            using var projectile = new SolidBrush(tool.Color);
            g.FillEllipse(projectile, Attack);
            g.DrawEllipse(Pens.SaddleBrown, Attack);
        }
        else if (strike > 0 && !ranged)
            g.DrawArc(Pens.DarkOrange, facing > 0 ? x + 26 : x - 30, y + 8, 36, 30, -90, 180);
        g.FillRectangle(Brushes.DarkSlateGray, layout.Flag, 365, 4, 75);
        g.FillRectangle(progress.Checkpoint >= 1 ? Brushes.LimeGreen : Brushes.Orange, layout.Flag + 4, 365, 28, 18);
        if (stages[progress.Stage].Boss is not null)
        {
            g.FillRectangle(Brushes.DarkSlateGray, ArenaFlag, 365, 4, 75);
            g.FillRectangle(progress.Checkpoint == 2 ? Brushes.LimeGreen : Brushes.Orange, ArenaFlag + 4, 365, 28, 18);
        }
        if (bossHealth > 0)
        {
            g.FillRectangle(Brushes.DimGray, layout.Gate, 335, 8, 105);
            if (stages[progress.Stage].Province <= 18)
                BaghdadPreviewArt.DrawGuardian(g, bossFrame.Body, BossExposed, stages[progress.Stage].Province);
            else BossArt.Draw(g, bossProfile, stages[progress.Stage].Province, bossFrame, BossExposed);
            if (!bossFrame.Warning.IsEmpty && bossTick > 0)
                g.FillRectangle(Brushes.Gold, bossFrame.Warning);
            if (!bossFrame.Hazard.IsEmpty)
                g.FillRectangle(Brushes.OrangeRed, bossFrame.Hazard);

        }
        RegionalReferenceArt.DrawExit(g, layout.Gate, stages[progress.Stage].Province, bossHealth > 0);
        g.ResetTransform();
        g.FillRectangle(Brushes.MidnightBlue, 0, 0, ClientSize.Width, 52);
        using var rightToLeft = new StringFormat(StringFormatFlags.DirectionRightToLeft)
        {
            Trimming = StringTrimming.EllipsisCharacter
        };
        Stage stage = stages[progress.Stage];
        g.DrawString($"{stage.ProvinceName}  |  {stage.Code}  |  {stage.Title}",
            Font, Brushes.LemonChiffon, new RectangleF(510, 6, ClientSize.Width - 526, 19), rightToLeft);
        g.DrawString($"المحاولات {progress.Lives}     الدنانير {progress.Coins}     الجواهر {progress.Gems}     النقاط {progress.Score}",
            Font, Brushes.White, new RectangleF(400, 27, ClientSize.Width - 416, 19), rightToLeft);
        g.DrawString("مشي A/D   قفز Space   ضربة X   أداة J   صوت M",
            Font, Brushes.White, new RectangleF(12, 27, 380, 19), rightToLeft);
        if (progress.Charges > 0)
            g.DrawString($"{tool.Name} ×{progress.Charges}", Font, Brushes.LemonChiffon,
                new RectangleF(12, 6, 235, 19), rightToLeft);
        g.DrawString("Esc وقفة", Font, Brushes.LemonChiffon,
            new RectangleF(256, 6, 126, 19), rightToLeft);

        if (bossHealth > 0 && x >= ArenaFlag - 60)
            DrawBossStatus(g, stage.Province);
        if (paused)
        {
            using var shade = new SolidBrush(Color.FromArgb(190, 12, 18, 38));
            g.FillRectangle(shade, ClientRectangle);
            using var title = new Font(Font.FontFamily, 28, FontStyle.Bold);
            using var centered = new StringFormat(StringFormatFlags.DirectionRightToLeft)
            {
                Alignment = StringAlignment.Center
            };
            g.DrawString("توقّف مؤقت", title, Brushes.LemonChiffon,
                new RectangleF(0, 169, ClientSize.Width, 60), centered);
            g.DrawString("اضغط Esc للمتابعة أو Enter للرجوع لاختيار الشخصية",
                Font, Brushes.White, new RectangleF(0, 245, ClientSize.Width, 30), centered);
        }
    }

    private void DrawBossStatus(Graphics g, int province)
    {
        int left = (ClientSize.Width - 264) / 2;
        using var backing = new SolidBrush(Color.FromArgb(223, 18, 22, 43));
        g.FillRectangle(backing, left, 60, 264, 56);
        using var centered = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Center };
        g.DrawString(BossNames[province - 1], Font, Brushes.LemonChiffon, new RectangleF(left, 65, 264, 17), centered);
        string hint = BossExposed ? "اضرب هسه" : inHalabja && bossFrame.Open ? "شغّل المصباح" : "انتبه للهجمة";
        g.DrawString(hint, Font, BossExposed ? Brushes.Gold : Brushes.White, new RectangleF(left, 84, 264, 16), centered);
        for (int i = 0; i < 3; i++)
            g.FillRectangle(i < bossHealth ? Brushes.IndianRed : Brushes.DimGray, left + 49 + 57 * i, 104, 52, 5);
    }

    private void DrawLamp(Graphics g, int position, bool lit)
    {
        g.FillRectangle(Brushes.DarkSlateBlue, position - 3, 403, 6, 37);
        g.FillEllipse(lit ? Brushes.Gold : Brushes.SlateGray, position - 10, 389, 20, 22);
        if (!lit && Math.Abs(x + 16 - position) < 54)
            g.DrawString("E", Font, Brushes.White, position - 4, 369);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            music.Dispose();
        }
        base.Dispose(disposing);
    }
}
