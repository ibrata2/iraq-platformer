namespace IraqPlatformer;

internal enum RewardKind { Coin, Mushroom, Gem, Heart, Tool }
internal readonly record struct Reward(Rectangle Bounds, RewardKind Kind, bool Hidden = false)
{
    public static Reward[] Place(StageLayout layout)
    {
        var rewards = new List<Reward>();
        Rectangle[] floors = layout.Platforms.Where(tile => tile.Top == 440).ToArray();
        for (int i = 1; i < floors.Length; i++)
        {
            Rectangle floor = floors[i];
            for (int c = 0; c < 3; c++)
                rewards.Add(new Reward(new Rectangle(floor.Left + 16 + c * 28, 410 - (c == 1 ? 16 : 0), 15, 19), RewardKind.Coin));
        }

        Rectangle first = floors[1];
        rewards.Add(new Reward(new Rectangle(first.Left + 260, 415, 23, 25), RewardKind.Mushroom));
        rewards.Add(new Reward(new Rectangle(floors[^2].Left + 225, 413, 24, 27), RewardKind.Tool));
        Rectangle ledge = layout.SecretLedge;
        rewards.Add(new Reward(new Rectangle(ledge.Right - 26, ledge.Top - 23, 20, 20), RewardKind.Gem, true));
        rewards.Add(new Reward(new Rectangle(ledge.Right - 53, ledge.Top - 19, 17, 17), RewardKind.Heart, true));
        return rewards.ToArray();
    }

    public Progress Collect(Progress progress, int index)
    {
        progress = progress with { Pickups = progress.Pickups | (1UL << index) };
        return Kind switch
        {
            RewardKind.Coin => progress with
            {
                Coins = progress.Coins + 1,
                Score = progress.Score + 10,
                Lives = progress.Lives + ((progress.Coins + 1) % 100 == 0 ? 1 : 0)
            },
            RewardKind.Mushroom => progress with { Grown = true },
            RewardKind.Gem => progress with { Gems = progress.Gems + 1, Score = progress.Score + 500 },
            RewardKind.Heart => progress with { Lives = progress.Lives + 1 },
            RewardKind.Tool => progress with { Charges = 5 },
            _ => progress
        };
    }
}
