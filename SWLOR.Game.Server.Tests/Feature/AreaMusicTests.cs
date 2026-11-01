using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class AreaMusicTests
{
    // Areas silent day and night through an empty ambientmusic.2da row, left until the owner picks a track.
    private static readonly HashSet<string> SilentThroughEmptyRow = new(StringComparer.OrdinalIgnoreCase)
    {
        "coxxian_hq",        // Coxxion HQ: Mountain Valley entrance and Coxxion Base use different tracks.
        "dath_tribevill",    // Dathomir Tribe Village: both neighboring areas are silent.
        "druz_shalim",       // [Prefab] Druzer: its only neighbor is silent.
        "ossus_cavefacili",  // [Prefab] Ossus Cave Facility: interior, no matching interior track.
        "pref_facilidark",   // Generic prefab with no neighbors.
        "pref_facility",     // Generic prefab with no neighbors.
        "pref_interior",     // Generic prefab with no neighbors.
        "space_korriban",    // Korriban Orbit: each orbit has its own track and the neighbors disagree.
        "valkorrdung1b",     // Sith Fortress Courtyard: the three connected fortress areas disagree.
        "vrotrbescloudext",  // Bespin prefabs: the whole cluster points at empty rows.
        "vrotrbescloudint",
        "vrotrbesgasmine",
        "vrotrbeslanding",
        "vrotrbesporttown",
        "vrotrkorrdnkside",  // [Prefab] Korriban cantina: entered from a desert exterior.
    };

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

    private static (int Day, int Night) MusicTracks(string area)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "git", $"{area}.git.json")));
        var properties = document.RootElement.GetProperty("AreaProperties").GetProperty("value");
        return (properties.GetProperty("MusicDay").GetProperty("value").GetInt32(),
            properties.GetProperty("MusicNight").GetProperty("value").GetInt32());
    }

    private static IEnumerable<string> Areas() => Directory
        .EnumerateFiles(Path.Combine(Root, "Module", "git"), "*.git.json")
        .Select(path => Path.GetFileName(path)[..^".git.json".Length]);

    // Rows whose Resource column names a track; row 0 ("None") and blank rows play nothing.
    private static HashSet<int> PlayableTracks() => File
        .ReadLines(Path.Combine(Root, "SWLOR_Haks", "sw_2da", "ambientmusic.2da"))
        .Skip(3)
        .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        .Where(columns => columns.Length > 2 && columns[2] != "****")
        .Select(columns => int.Parse(columns[0]))
        .ToHashSet();

    private static bool PointsAtEmptyRow((int Day, int Night) tracks, HashSet<int> playable) =>
        tracks.Day != 0 && !playable.Contains(tracks.Day) || tracks.Night != 0 && !playable.Contains(tracks.Night);

    [Test]
    public void HyperDiveCantina_PlaysMaxRebo1DayAndNight()
    {
        // Track 0 is "None", so a missing day track stopped the music at every dawn.
        MusicTracks("pw_ar_nars_canhd").Should().Be((457, 457));
    }

    [Test]
    public void Areas_PlayMusicDayAndNightOrNeither()
    {
        var oneSided = Areas()
            .Select(area => (Area: area, Tracks: MusicTracks(area)))
            .Where(x => (x.Tracks.Day == 0) != (x.Tracks.Night == 0))
            .Select(x => $"{x.Area} day {x.Tracks.Day} night {x.Tracks.Night}");

        oneSided.Should().BeEmpty("the area music would stop at dawn or dusk");
    }

    [Test]
    public void Areas_PointMusicAtPlayableTracks()
    {
        var playable = PlayableTracks();
        playable.Should().Contain(457);

        var broken = Areas()
            .Where(area => !SilentThroughEmptyRow.Contains(area))
            .Select(area => (Area: area, Tracks: MusicTracks(area)))
            .Where(x => PointsAtEmptyRow(x.Tracks, playable))
            .Select(x => $"{x.Area} day {x.Tracks.Day} night {x.Tracks.Night}");

        broken.Should().BeEmpty("an empty or missing ambientmusic.2da row plays nothing");
    }

    [Test]
    public void SilentThroughEmptyRowAllowList_HasOnlyAreasSilentDayAndNightThroughEmptyRows()
    {
        var playable = PlayableTracks();

        var stale = SilentThroughEmptyRow
            .Where(area => !File.Exists(Path.Combine(Root, "Module", "git", $"{area}.git.json")) ||
                MusicTracks(area) is var tracks && (!PointsAtEmptyRow(tracks, playable) ||
                    playable.Contains(tracks.Day) || playable.Contains(tracks.Night)))
            .ToList();

        // A track picked for only one side would play on that side and cut out on the other.
        stale.Should().BeEmpty("fixed or removed areas leave the allow-list, and a fix sets day and night together");
    }
}
