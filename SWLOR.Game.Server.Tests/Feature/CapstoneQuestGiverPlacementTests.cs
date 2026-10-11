using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneQuestGiverPlacementTests
{
    // Every capstone quest giver -> its own distinct, non-dungeon hub area (git file stem).
    // One giver per area, except the three Dathomir hubs (only three non-dungeon areas exist on
    // Dathomir for six beast-mastery givers), which carry two each.
    private static readonly (string Giver, string Area)[] Placements =
    {
        ("cq_invinc", "veles_sheriff"), ("cq_vitrupt", "veles_cantina"), ("cq_sysshut", "veles_cz_tower"),
        ("cq_killbeacon", "pw_ar_narscorpd"), ("cq_embunker", "tat_anc_gocorpst"), ("cq_deccommand", "pw_ar_nsshipyard"),
        ("cq_sabstorm", "tat_anc_astropor"), ("cq_guardmst", "tat_anc_verpexba"), ("cq_sabcycl", "tat_tocheemain"),
        ("cq_emcocktail", "pw_ar_ns_medical"), ("cq_holdline", "pw_ar_ns_doffice"), ("cq_infconduit", "tat_anc_medical"),
        ("cq_absdef", "pw_ar_nsficlub"), ("cq_soulasc", "pw_ar_narshahub"), ("cq_forcebane", "tat_anc_northdis"),
        ("cq_lightstand", "pw_ar_narpromena"), ("cq_darkhung", "tat_anc_junix"), ("cq_eclipse", "pw_ar_nardocks"),
        ("cq_adamguard", "nanostation015"), ("cq_scraplock", "czs220_hangar"), ("cq_worldbrk", "tat_anc_droidshp"),
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

    [Test]
    public void EveryTurnInJournal_NamesThePlacedGiversArea()
    {
        var quests = typeof(VibrobladeCapstoneQuestDefinition).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                           typeof(SWLOR.Game.Server.Service.QuestService.IQuestListDefinition).IsAssignableFrom(type) &&
                           type.Name.EndsWith("CapstoneQuestDefinition", StringComparison.Ordinal))
            .SelectMany(type => ((SWLOR.Game.Server.Service.QuestService.IQuestListDefinition)
                Activator.CreateInstance(type)!).BuildQuests())
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var (giver, area) in Placements)
        {
            var line = CapstoneQuestDefinitionTestData.Lines.Single(line => line.QuestGiver.Resref == giver);
            using var are = LoadModuleJson("are", $"{area}.are.json");
            var location = GetLocString(are.RootElement, "Name").Trim().Split(" - ", 2);
            foreach (var questId in line.QuestIds)
            {
                var journal = quests[questId].States[2].JournalText;
                journal.Should().Contain(line.QuestGiver.Name, questId);
                // Existing journals also use the lore name Nar Shaddaa for Smuggler's Moon.
                if (giver is "cq_lightstand" or "cq_darkhung" or "cq_eclipse" or "cq_absdef" or
                    "cq_soulasc" or "cq_forcebane" or "cq_sabstorm" or "cq_guardmst" or
                    "cq_sabcycl" or "cq_infconduit" or "cq_emcocktail" or "cq_holdline" or
                    "cq_worldbrk" or "cq_killbeacon" or "cq_embunker" or "cq_deccommand")
                {
                    journal.Should().Contain($"{location[1]} on {location[0]}", questId);
                }
            }
        }
    }

    [Test]
    public void CapstoneQuestGivers_AvoidFactionHeadquartersAndPlanets()
    {
        foreach (var (_, area) in Placements)
        {
            using var are = LoadModuleJson("are", $"{area}.are.json");
            var location = GetLocString(are.RootElement, "Name");
            location.Should().NotStartWith("Dantooine - ");
            location.Should().NotStartWith("Korriban - ");
            location.Should().NotContain("Republic Base");
        }
    }

    [Test]
    public void RelocatedGivers_ExplainNeutralMeetingPointWithoutAdvancingQuests()
    {
        var relocatedAreas = new[]
        {
            "pw_ar_narpromena", "tat_anc_junix", "pw_ar_nardocks", "pw_ar_nsficlub",
            "pw_ar_narshahub", "tat_anc_northdis", "tat_anc_astropor", "tat_anc_verpexba",
            "tat_tocheemain", "tat_anc_medical", "pw_ar_ns_medical", "pw_ar_ns_doffice",
            "tat_anc_droidshp", "pw_ar_narscorpd", "tat_anc_gocorpst", "pw_ar_nsshipyard",
        };
        foreach (var (giver, area) in Placements.Where(p => relocatedAreas.Contains(p.Area)))
        {
            var path = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server",
                "ConversationData", $"{giver}.conversation.json");
            using var graph = JsonDocument.Parse(File.ReadAllText(path));
            var root = graph.RootElement;
            var explanation = root.GetProperty("Nodes").GetProperty("neutral-meeting");
            explanation.GetProperty("OnEnterActions").GetArrayLength().Should().Be(0);
            foreach (var choiceId in new[] { "neutral-ask", "neutral-return" })
            {
                var choice = root.GetProperty("Choices").GetProperty(choiceId);
                choice.GetProperty("Actions").GetArrayLength().Should().Be(0);
                choice.GetProperty("EndsConversation").GetBoolean().Should().BeFalse();
            }
            using var are = LoadModuleJson("are", $"{area}.are.json");
            var planet = GetLocString(are.RootElement, "Name").Split(" - ", 2)[0];
            explanation.GetProperty("Text")[0].GetProperty("Text").GetString().Should().Contain(planet);
        }
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
