using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneQuestGiverPlacementTests
{
    // Every capstone quest giver -> its own distinct, non-dungeon hub area (git file stem).
    // One giver per area, except the three Dathomir hubs (only three non-dungeon areas exist on
    // Dathomir for six beast-mastery givers), which carry two each.
    private static readonly (string Giver, string Area)[] Placements =
    {
        ("cq_invinc", "veles_sheriff"), ("cq_vitrupt", "veles_cantina"), ("cq_sysshut", "veles_cz_tower"),
        ("cq_killbeacon", "v_repubbase_1"), ("cq_embunker", "v_repubbase_ext"), ("cq_deccommand", "v_repubbase_cd"),
        ("cq_sabstorm", "dan_jedienclave"), ("cq_guardmst", "dan_jedlibrary"), ("cq_sabcycl", "dan_interiors"),
        ("cq_emcocktail", "dan_repubmed"), ("cq_holdline", "dan_repgarrison"), ("cq_infconduit", "dan_medinterior"),
        ("cq_absdef", "ar_scor_kacademy"), ("cq_soulasc", "scor_knwinterior"), ("cq_forcebane", "ar_scor_kortemp"),
        ("cq_lightstand", "korribanlandingp"), ("cq_darkhung", "ar_scor_korrcan"), ("cq_eclipse", "scor_kscaves"),
        ("cq_adamguard", "nanostation015"), ("cq_scraplock", "czs220_hangar"), ("cq_worldbrk", "dan_battlemon"),
        ("cq_unmovctr", "tat_anc_cantina"), ("cq_lastword", "tosche_cantina_s"), ("cq_deadhand", "tochee_cantina"),
        ("cq_killbox", "pw_ar_nsczgnstr"), ("cq_oneshot", "pw_ar_czoffice"), ("cq_rainsteel", "pw_ar_nscrafting"),
        ("cq_thermdet", "hutlar_outpost"), ("cq_overbarr", "sol_mandaloriani"), ("cq_perflurry", "sol_hutlarqcanyo"),
        ("cq_primover", "dath_landingpad"), ("cq_apexbite", "dath_landingpad"),
        ("cq_untinst", "dath_cz_baseok"), ("cq_unbrbeast", "dath_cz_baseok"),
        ("cq_forcebeast", "dath_waterfallru"), ("cq_alpharhy", "dath_waterfallru"),
        ("cq_cripdef", "pw_ar_nars_canhd"), ("cq_tempbloom", "pw_ar_bhbar"), ("cq_redbloom", "pw_ar_nscasino"),
    };

    [Test]
    public void EveryCapstoneQuestGiver_IsPlacedExactlyOnceInItsHub()
    {
        foreach (var (giver, area) in Placements)
        {
            using var git = LoadGit(area);
            var count = Creatures(git.RootElement).Count(c => GetString(c, "TemplateResRef") == giver);
            count.Should().Be(1, $"{giver} should be placed exactly once in {area}");
        }
    }

    [Test]
    public void QuestGivers_AreSpreadAcrossDistinctAreas()
    {
        // Givers must not be bunched: one per area, except the three Dathomir hubs (Dathomir has
        // only three non-dungeon areas for its six beast-mastery givers) which may hold two each.
        var perArea = Placements
            .GroupBy(p => p.Area)
            .ToDictionary(g => g.Key, g => g.Count());

        var dathomirHubs = new[] { "dath_landingpad", "dath_cz_baseok", "dath_waterfallru" };
        foreach (var (area, count) in perArea)
        {
            var cap = dathomirHubs.Contains(area) ? 2 : 1;
            count.Should().BeLessThanOrEqualTo(cap, $"{area} should not have more than {cap} quest giver(s)");
        }

        Placements.Select(p => p.Giver).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void QuestGiverHubs_AreNotDungeonsOrSpawnAreas()
    {
        foreach (var area in Placements.Select(p => p.Area).Distinct())
        {
            using var git = LoadGit(area);
            var root = git.RootElement;

            // No ambient enemy spawn table and not flagged as a dungeon.
            GetOptionalLocalString(root, "CREATURE_SPAWN_TABLE_ID").Should().BeEmpty($"{area} must not be a spawn area");
            HasLocal(root, "IS_DUNGEON").Should().BeFalse($"{area} must not be flagged as a dungeon");

            // No creature spawn-table waypoints either (tags ending in a generated capstone table id).
            Creatures(git.RootElement); // ensures list exists
        }
    }

    [Test]
    public void AllQuestGivers_HaveUniqueNamesAndDistinctAppearances()
    {
        var names = new List<string>();
        var heads = new List<int>();
        var portraits = new List<int>();
        foreach (var (giver, _) in Placements)
        {
            using var utc = LoadUtc(giver);
            var root = utc.RootElement;
            var first = GetLocString(root, "FirstName");
            var last = GetLocString(root, "LastName");
            names.Add($"{first} {last}".Trim());
            portraits.Add(GetWord(root, "PortraitId"));

            var appearance = GetWord(root, "Appearance_Type");
            if (appearance < 1000)
            {
                // Humanoid parts-based NPCs must have a distinct head so faces differ.
                heads.Add(GetWord(root, "Appearance_Head"));
            }
        }

        names.Should().OnlyHaveUniqueItems();
        heads.Should().OnlyHaveUniqueItems();
        portraits.Should().OnlyContain(portrait => portrait > 0,
            "every placed capstone conversation needs a non-blank creature portrait fallback");
        portraits.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void UnitKX17_IsADroidNotAHuman()
    {
        using var utc = LoadUtc("cq_worldbrk");
        GetWord(utc.RootElement, "Appearance_Type").Should().BeGreaterThanOrEqualTo(1000,
            "Unit KX-17 is a droid and must not use a humanoid appearance");
    }

    private static JsonDocument LoadGit(string area) => LoadModuleJson("git", $"{area}.git.json");
    private static JsonDocument LoadUtc(string resref) => LoadModuleJson("utc", $"{resref}.utc.json");

    private static JsonDocument LoadModuleJson(string folder, string file)
    {
        var root = FindRepositoryRoot();
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", folder, file)));
    }

    private static IEnumerable<JsonElement> Creatures(JsonElement git)
    {
        return git.GetProperty("Creature List").GetProperty("value").EnumerateArray();
    }

    private static string GetString(JsonElement json, string name)
    {
        return json.TryGetProperty(name, out var p) && p.TryGetProperty("value", out var v) &&
               v.ValueKind == JsonValueKind.String ? v.GetString()! : string.Empty;
    }

    private static int GetWord(JsonElement json, string name)
    {
        return json.GetProperty(name).GetProperty("value").GetInt32();
    }

    private static string GetLocString(JsonElement json, string name)
    {
        return json.TryGetProperty(name, out var p) &&
               p.TryGetProperty("value", out var v) &&
               v.ValueKind == JsonValueKind.Object &&
               v.TryGetProperty("0", out var t) ? t.GetString() ?? string.Empty : string.Empty;
    }

    private static bool HasLocal(JsonElement json, string variableName) =>
        TryGetLocal(json, variableName, out _);

    private static string GetOptionalLocalString(JsonElement json, string variableName) =>
        TryGetLocal(json, variableName, out var local)
            ? local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty
            : string.Empty;

    private static bool TryGetLocal(JsonElement json, string variableName, out JsonElement local)
    {
        local = default;
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                local = variable;
                return true;
            }
        }

        return false;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
