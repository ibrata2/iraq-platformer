using System.Media;

namespace IraqPlatformer;

internal sealed class RegionalScore : IDisposable
{
    private const int SampleRate = 22050;
    private static readonly int[] Melody = [62, 65, 67, 69, 67, 65, 62, 60, 62, 65, 67, 70, 69, 67, 65, 62];
    private static readonly (int Tempo, int Voice, int Drum)[] Arrangements =
    [
        (108,0,0), (100,0,1), (112,2,1), (111,3,2), (96,4,0),
        (101,1,2), (106,2,0), (94,2,1), (92,4,1), (109,3,0),
        (105,1,0), (99,1,2), (91,4,1), (114,3,1), (122,2,3),
        (120,3,2), (112,4,2), (116,2,2), (90,0,3)
    ];
    private SoundPlayer? player;
    private MemoryStream? audio;
    private int province, stage;
    private bool battle, victory;
    public bool Muted { get; private set; }

    public void Play(int nextProvince, bool nextBattle, int nextStage = 1, bool nextVictory = false)
    {
        if (province == nextProvince && battle == nextBattle && stage == nextStage && victory == nextVictory) return;
        stage = nextStage;
        victory = nextVictory;
        province = nextProvince;
        battle = nextBattle;
        if (!Muted) Start();
    }

    public void Toggle()
    {
        Muted = !Muted;
        if (Muted) Dispose();
        else Start();
    }

    private void Start()
    {
        Dispose();
        audio = Compose(province, battle, stage, victory);
        player = new SoundPlayer(audio);
        if (victory) player.Play();
        else player.PlayLooping();
    }

    internal static MemoryStream Compose(int province, bool battle, int stage = 1, bool victory = false)
    {
        var (tempo, voice, drum) = Arrangements[province - 1];
        int[] melody = victory ? [62, 65, 69, 74] : Melody;
        int beat = SampleRate * 60 / (victory ? 160 : tempo + (stage - 1) * 2 + (battle ? 32 : 0));
        int count = beat * melody.Length;
        var stream = new MemoryStream(44 + 2 * count);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write("RIFF"u8); writer.Write(36 + 2 * count); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate);
        writer.Write(2 * SampleRate); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(2 * count);
        var noise = new Random(province);
        for (int i = 0; i < melody.Length; i++)
        {
            double frequency = 440 * Math.Pow(2, (melody[i] - 69) / 12.0);
            for (int n = 0; n < beat; n++)
            {
                double t = n / (double)SampleRate;
                double u = n / (double)beat;
                double phase = 2 * Math.PI * frequency * t;
                double attack = Math.Min(1, t * 105) * Math.Min(1, (1 - u) * 18);
                double note = voice switch
                {
                    0 => (Math.Sin(phase) + .38 * Math.Sin(phase * 2.01) + .20 * Math.Sin(phase * 3.94)) * Math.Exp(-4 * u),
                    1 => Math.Sin(phase + .23 * Math.Sin(33 * t)) + .34 * Math.Sin(2 * phase),
                    2 => (Math.Sin(phase) + .52 * Math.Sin(2 * phase) + .21 * Math.Sin(3 * phase)) * Math.Exp(-3 * u),
                    3 => Math.Sin(phase) + .38 * Math.Sin(3 * phase) + .16 * Math.Sin(5 * phase),
                    _ => Math.Sin(phase) + .13 * Math.Sin(2 * phase)
                };
                double pulse = u < .5 ? u : u - .5;
                double percussion = Math.Sin(t * (540 - drum * 70) * 2 * Math.PI)
                    * Math.Exp(-pulse * 45) * (i % 4 == 0 ? .18 : .06 + stage * .007);
                if (drum == 3 || battle)
                    percussion += (noise.NextDouble() - .5) * Math.Exp(-pulse * 65) * .27;
                double drone = Math.Sin(2 * Math.PI * 73.416 * (i * beat + n) / SampleRate) * .07;
                writer.Write((short)(15000 * (note * attack * .35 + percussion + drone)));
            }
        }
        stream.Position = 0;
        return stream;
    }

    public void Dispose()
    {
        player?.Stop();
        player?.Dispose();
        audio?.Dispose();
        player = null;
        audio = null;
    }
}
