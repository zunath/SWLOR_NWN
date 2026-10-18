using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class KorribanSithFortressExitTests
{
    private const string QuartersArea = "valkorrdung1d";
    private const string CourtyardArea = "valkorrdung1b";
    private const string QuartersExitDoor = "korr_quarters_to_courtyard";

    [Test]
    public void LordsQuartersExit_OpensWithoutAKey()
    {
        using var quarters = LoadModuleJson("git", $"{QuartersArea}.git.json");
        var door = FindDoor(quarters.RootElement, QuartersExitDoor);

        // The entrance from the courtyard is a trigger, so a locked exit traps anyone
        // inside once the door resets (server restart) to its authored state.
        GetInt(door, "Locked").Should().Be(0);
        GetInt(door, "KeyRequired").Should().Be(0);
        GetString(door, "KeyName").Should().BeEmpty();
        GetInt(door, "Lockable").Should().Be(0);

        GetString(door, "LinkedTo").Should().Be("korr_courtyard_from_quarters");
        GetInt(door, "LinkedToFlags").Should().Be(2);
        using var courtyard = LoadModuleJson("git", $"{CourtyardArea}.git.json");
        EnumerateTags(courtyard.RootElement, "WaypointList")
            .Should().Contain("korr_courtyard_from_quarters");
    }

    [TestCase("valkorrdung1a", "korr_fortress_to_courtyard", "valkorrdkey1")]
    [TestCase("valkorrdung1b", "ForgeworksDoor", "valkorrdkey2")]
    [TestCase("valkorrdung1d", "korr_quarter_entry_to_L1", "valkorrdkey3")]
    [TestCase("valkorrdung1d", "RustedDoor", "valkorrdkey4")]
    public void FortressProgressionGates_StillRequireTheirKeys(string area, string doorTag, string keyTag)
    {
        using var git = LoadModuleJson("git", $"{area}.git.json");
        var door = FindDoor(git.RootElement, doorTag);

        GetInt(door, "Locked").Should().Be(1);
        GetInt(door, "KeyRequired").Should().Be(1);
        GetString(door, "KeyName").Should().Be(keyTag);

        using var key = LoadModuleJson("uti", $"{keyTag}.uti.json");
        GetString(key.RootElement, "Tag").Should().Be(keyTag);
    }

    [Test]
    public void PlayerAreaTransitionDoors_OnlyRequireKeysThatExist()
    {
        var root = FindRepositoryRoot();
        var itemTags = Directory
            .EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json")
            .Select(path =>
            {
                using var item = JsonDocument.Parse(File.ReadAllText(path));
                return GetString(item.RootElement, "Tag");
            })
            .ToHashSet(StringComparer.Ordinal);

        var failures = new List<string>();
        foreach (var gitPath in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            var resref = Path.GetFileName(gitPath)[..^".git.json".Length];
            var arePath = Path.Combine(root.FullName, "Module", "are", $"{resref}.are.json");
            if (!File.Exists(arePath))
                continue;

            using (var are = JsonDocument.Parse(File.ReadAllText(arePath)))
            {
                // [Prefab] areas are DM staging copies that players cannot reach.
                if (GetString(are.RootElement, "Name").StartsWith("[Prefab]", StringComparison.Ordinal))
                    continue;
            }

            using var git = JsonDocument.Parse(File.ReadAllText(gitPath));
            if (!git.RootElement.TryGetProperty("Door List", out var doors))
                continue;

            foreach (var door in doors.GetProperty("value").EnumerateArray())
            {
                // A blank key is a deliberately sealed door; only a named key can be missing.
                var keyTag = GetString(door, "KeyName");
                if (!string.IsNullOrEmpty(keyTag) &&
                    GetInt(door, "Locked") == 1 &&
                    GetInt(door, "KeyRequired") == 1 &&
                    !string.IsNullOrEmpty(GetString(door, "LinkedTo")) &&
                    !itemTags.Contains(keyTag))
                {
                    failures.Add($"{resref}: {GetString(door, "Tag")} requires missing key '{keyTag}'");
                }
            }
        }

        failures.Should().BeEmpty();
    }

    private static JsonElement FindDoor(JsonElement git, string tag)
    {
        return git.GetProperty("Door List").GetProperty("value").EnumerateArray()
            .Single(door => GetString(door, "Tag") == tag);
    }

    private static IEnumerable<string> EnumerateTags(JsonElement git, string listName)
    {
        return git.GetProperty(listName).GetProperty("value").EnumerateArray()
            .Select(instance => GetString(instance, "Tag"))
            .ToList();
    }

    private static JsonDocument LoadModuleJson(string folder, string fileName)
    {
        var root = FindRepositoryRoot();
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", folder, fileName)));
    }

    private static int GetInt(JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property)
            ? property.GetProperty("value").GetInt32()
            : 0;
    }

    private static string GetString(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property) ||
            !property.TryGetProperty("value", out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString()!;

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("0", out var localized))
            return localized.GetString()!;

        return string.Empty;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
