using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CantinaAreaMusicTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        }
    }

    private static JsonElement Load(string type, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return document.RootElement.Clone();
    }

    private static (int Day, int Night) MusicTracks(string area)
    {
        var properties = Load("git", area).GetProperty("AreaProperties").GetProperty("value");
        return (properties.GetProperty("MusicDay").GetProperty("value").GetInt32(),
            properties.GetProperty("MusicNight").GetProperty("value").GetInt32());
    }

    private static IEnumerable<string> CantinaAreas() => Directory
        .EnumerateFiles(Path.Combine(Root, "Module", "are"), "*.are.json")
        .Select(path => Path.GetFileName(path)[..^".are.json".Length])
        .Where(area => Load("are", area).GetProperty("Name").GetProperty("value")
            .TryGetProperty("0", out var name) &&
            name.GetString()!.Contains("Cantina", StringComparison.OrdinalIgnoreCase));

    [Test]
    public void HyperDiveCantina_PlaysMaxRebo1DayAndNight()
    {
        // Track 0 is "None", so a missing day track stopped the music at every dawn.
        MusicTracks("pw_ar_nars_canhd").Should().Be((457, 457));
    }

    [Test]
    public void CantinaAreas_WithMusicPlayItDayAndNight()
    {
        var areas = CantinaAreas().ToList();
        areas.Should().Contain("pw_ar_nars_canhd");

        var oneSided = areas
            .Select(area => (Area: area, Tracks: MusicTracks(area)))
            .Where(x => (x.Tracks.Day == 0) != (x.Tracks.Night == 0))
            .Select(x => $"{x.Area} day {x.Tracks.Day} night {x.Tracks.Night}");

        oneSided.Should().BeEmpty("the area music would stop at dawn or dusk");
    }
}
