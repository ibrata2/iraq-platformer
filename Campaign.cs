using System.Text.Json;
using System.Text.RegularExpressions;

namespace IraqPlatformer;

internal sealed record Stage(int Province, int Number, string Description, string? Boss, string ProvinceName)
{
    public string Code => $"\u200E{Province}–{Number}\u200E";
    public string Title => Description.Split(" → ", 2)[0];
}

internal static class Campaign
{
    public static IReadOnlyList<Stage> Load()
    {
        using var stream = typeof(Campaign).Assembly.GetManifestResourceStream("IraqPlatformer.Content.LevelMaps.md")
            ?? throw new InvalidDataException("Missing campaign.");
        using var reader = new StreamReader(stream);
        var stages = new List<Stage>();
        int province = 0;
        string provinceName = "";
        while (reader.ReadLine() is { } line)
        {
            var heading = Regex.Match(line, @"^## ([0-9]{2}) (.+?) — ");
            if (heading.Success)
            {
                province = int.Parse(heading.Groups[1].Value);
                provinceName = heading.Groups[2].Value;
            }

            var entry = Regex.Match(line, @"^([1-8])\. (.+)$");
            if (province != 0 && entry.Success)
                stages.Add(new Stage(province, int.Parse(entry.Groups[1].Value), entry.Groups[2].Value, null, provinceName));

            const string boss = "   - **وحش ٨–٨:** ";
            if (line.StartsWith(boss, StringComparison.Ordinal))
                stages[^1] = stages[^1] with { Boss = line[boss.Length..] };
        }

        if (province != 19 || stages.Count != 152)
            throw new InvalidDataException("Invalid campaign length.");

        for (int index = 0; index < stages.Count; index++)
        {
            Stage stage = stages[index];
            if (stage.Province != index / 8 + 1 || stage.Number != index % 8 + 1 ||
                (stage.Number == 8) != !string.IsNullOrWhiteSpace(stage.Boss))
                throw new InvalidDataException($"Invalid stage at index {index}.");
        }

        return stages.AsReadOnly();
    }
}

internal sealed record Progress(int Stage = 0, int Checkpoint = 0, int Lives = 3,
    int Coins = 0, int Score = 0, int Gems = 0, ulong Pickups = 0, bool Grown = false, int Charges = 0, uint Defeated = 0, bool Lit = false, bool BossDefeated = false);

internal sealed class SaveSlots(string directory)
{
    private string PathFor(int hero)
    {
        if (hero is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(hero));
        return Path.Combine(directory, $"hero-{hero:D2}.json");
    }

    public Progress? Load(int hero) => Load(hero, out _);

    public Progress? Load(int hero, out bool invalid)
    {
        string path = PathFor(hero);
        invalid = false;
        if (!File.Exists(path)) return null;
        try
        {
            Progress? progress = JsonSerializer.Deserialize<Progress>(File.ReadAllText(path));
            if (progress is { Stage: >= 0 and <= 152, Checkpoint: >= 0 and <= 2, Lives: > 0,
                Coins: >= 0, Score: >= 0, Gems: >= 0, Charges: >= 0 and <= 5 })
                return progress;
        }
        catch (JsonException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        invalid = true;
        return null;
    }

    public void Save(int hero, Progress progress)
    {
        string path = PathFor(hero);
        Directory.CreateDirectory(directory);
        string temp = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(progress));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
