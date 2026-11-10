namespace IraqPlatformer;

internal sealed class HeroSelect : Form
{
    private static readonly string[] Names =
    [
        "حمودي البغدادي", "باشا بغداد", "يونس", "أرسلان", "سعد",
        "ديار", "باران", "سوران", "شاهو", "هلال",
        "نواف", "حمّور", "كرار", "حيدر", "سجاد",
        "فلاح", "غازي", "ناصر", "جاسم", "بحري"
    ];

    private readonly SaveSlots saves;
    private readonly IReadOnlyList<Stage> stages;
    private readonly ListBox heroes = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly PictureBox portrait = new() { Dock = DockStyle.Left, Width = 246, SizeMode = PictureBoxSizeMode.Zoom };
    private int shownHero;
    private readonly Button resume = new() { Text = "متابعة", AutoSize = true };
    private readonly Button restart = new() { Text = "لعبة جديدة", AutoSize = true };

    public HeroSelect(SaveSlots saves, IReadOnlyList<Stage> stages)
    {
        this.saves = saves;
        this.stages = stages;
        Text = "رحلة العراق | اختيار الشخصية";
        ClientSize = new Size(680, 565);
        MinimumSize = new Size(600, 480);
        BackColor = Color.FromArgb(18, 39, 49);
        ForeColor = Color.Wheat;
        Font = new Font("Segoe UI", 10);
        heroes.BorderStyle = BorderStyle.None;
        heroes.BackColor = Color.FromArgb(29, 56, 65);
        heroes.ForeColor = Color.Wheat;
        heroes.DrawMode = DrawMode.OwnerDrawFixed;
        heroes.ItemHeight = 24;
        heroes.DrawItem += DrawHeroRow;
        portrait.BackColor = BackColor;
        portrait.Margin = new Padding(8);
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;

        var header = new Label
        {
            Text = "رحلة العراق  |  اختار شخصيتك",
            Dock = DockStyle.Top,
            Height = 48,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(12, 32, 42),
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 15, FontStyle.Bold)
        };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(7) };
        var credits = new Button { Text = "عن اللعبة", AutoSize = true };
        actions.Controls.AddRange([resume, restart, credits]);
        foreach (Button button in actions.Controls.OfType<Button>())
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(37, 71, 78);
            button.ForeColor = Color.Wheat;
            button.Padding = new Padding(8, 1, 8, 1);
        }
        Controls.Add(heroes);
        Controls.Add(portrait);
        Controls.Add(actions);
        Controls.Add(header);
        heroes.SelectedIndexChanged += (_, _) => UpdateButtons();
        resume.Click += (_, _) => Play(false);
        restart.Click += (_, _) => Play(true);
        credits.Click += (_, _) => Credits.Show(this);
        RefreshHeroes(0);
    }

    private void DrawHeroRow(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? Color.FromArgb(112, 76, 33) :
            e.Index % 2 == 0 ? Color.FromArgb(29, 56, 65) : Color.FromArgb(25, 49, 58));
        e.Graphics.FillRectangle(fill, e.Bounds);
        TextRenderer.DrawText(e.Graphics, heroes.Items[e.Index].ToString(), Font,
            Rectangle.Inflate(e.Bounds, -10, 0), Color.Wheat,
            TextFormatFlags.RightToLeft | TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (selected) e.DrawFocusRectangle();
    }

    private void RefreshHeroes(int selection)
    {
        heroes.BeginUpdate();
        try
        {
            heroes.Items.Clear();
            for (int i = 0; i < Names.Length; i++)
            {
                Progress? progress = saves.Load(i + 1, out bool invalid);
                string position = progress is null ? (invalid ? "الحفظ غير قابل للقراءة" : "جديد") :
                    progress.Stage == stages.Count ? "اكتملت الرحلة" :
                    $"{stages[progress.Stage].ProvinceName} {stages[progress.Stage].Code}";
                heroes.Items.Add($"{Names[i]}  |  {position}");
            }
            heroes.SelectedIndex = selection;
        }
        finally { heroes.EndUpdate(); }
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        int id = heroes.SelectedIndex + 1;
        if (id != shownHero)
        {
            portrait.Image?.Dispose();
            portrait.Image = id > 0 ? LoadPortrait(id) : null;
            shownHero = id;
        }
        restart.Enabled = id > 0;
        resume.Enabled = id > 0 && saves.Load(id) is { } progress && progress.Stage < stages.Count;
    }

    private static Bitmap LoadPortrait(int id)
    {
        using Stream stream = typeof(HeroSelect).Assembly.GetManifestResourceStream($"Hero{id:D2}.jpg")
            ?? throw new InvalidDataException("Missing hero portrait.");
        using Image source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) portrait.Image?.Dispose();
        base.Dispose(disposing);
    }

    private void Play(bool newGame)
    {
        int id = heroes.SelectedIndex + 1;
        if (id == 0) return;
        Progress? previous = saves.Load(id, out bool invalid);
        if (!newGame && previous is null) return;
        if (newGame && (previous is not null || invalid) &&
            MessageBox.Show(this, "تريد تستبدل الحفظ الحالي لهذي الشخصية؟", "لعبة جديدة",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;
        Progress progress = newGame ? new Progress() : previous ?? new Progress();
        if (newGame) saves.Save(id, progress);

        using var game = new MovementLab(id, Names[id - 1], progress, stages, next => saves.Save(id, next));
        Hide();
        try { game.ShowDialog(); }
        finally { Show(); RefreshHeroes(id - 1); }
    }
}
