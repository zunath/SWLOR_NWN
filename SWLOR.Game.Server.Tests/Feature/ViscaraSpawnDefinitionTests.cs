using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

public class ViscaraSpawnDefinitionTests
{
    private static readonly string[] GeneralPurposeBloodFrenzyResrefs =
    {
        "bf_scavenger",
        "bf_pulsedroid",
        "bf_duelist",
    };

    private static readonly string[] AllBloodFrenzyResrefs =
    {
        "bf_scavenger",
        "bf_pulsedroid",
        "bf_butcher",
        "bf_duelist",
        "bf_kess",
    };

    private static readonly (string TableId, string Resref, string WaypointName, string TableName)[] BloodFrenzySpawnWaypoints =
    {
        ("VISCARA_SEWERS_DEPTHS_GENERAL", "bf_sd_general", "Viscara Sewers Depths - General Spawn", "Viscara Sewers Depths - General"),
    };

    private static readonly (string Resref, string LootTableId)[] BloodFrenzyLootTables =
    {
        ("bf_scavenger", "VISCARA_SEWERS_DEPTHS_SCAVENGER"),
        ("bf_pulsedroid", "VISCARA_SEWERS_DEPTHS_PULSE_DROID"),
        ("bf_butcher", "VISCARA_SEWERS_DEPTHS_BUTCHER"),
        ("bf_duelist", "VISCARA_SEWERS_DEPTHS_DUELIST"),
        ("bf_kess", "VISCARA_SEWERS_DEPTHS_KING"),
    };

    private static readonly string[] BloodFrenzyPhysicalProofItems =
    {
        "redvein_codex",
        "pulse_metron",
        "adren_glass",
        "bf_charm_frag",
    };

    [Test]
    public void VelesSewers_DoesNotIncludeBloodFrenzyCapstoneEnemies()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();

        tables["VISCARA_VELES_SEWERS"]
            .Spawns
            .Select(spawn => spawn.Resref)
            .Should()
            .NotIntersectWith(AllBloodFrenzyResrefs);
    }

    [Test]
    public void BloodFrenzyGeneralPurposeEnemies_UseSingleDedicatedSpawnTable()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();

        foreach (var (tableId, _, _, tableName) in BloodFrenzySpawnWaypoints)
        {
            tables[tableId].Name.Should().Be(tableName);
        }

        tables.Keys.Should().NotContain("VISCARA_SEWERS_DEPTHS_" + "ENTRY");
        tables.Keys.Should().NotContain("VISCARA_SEWERS_DEPTHS_" + "CIRCLE");

        tables["VISCARA_SEWERS_DEPTHS_GENERAL"]
            .Spawns
            .Select(spawn => spawn.Resref)
            .Should()
            .BeEquivalentTo(GeneralPurposeBloodFrenzyResrefs);

        tables.Keys.Should().NotContain("VISCARA_SEWERS_DEPTHS_" + "LAB");
    }

    [Test]
    public void SeraVonn_IsNotWiredThroughSpawnTables()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();

        tables.Keys.Should().NotContain("SERA_" + "VONN");

        var root = FindRepositoryRoot();
        var velesInterior = File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "git",
            "velesinterior.git.json"));

        velesInterior.Should().NotContain("Sera Vonn Spawn");
        velesInterior.Should().NotContain("\"SERA_" + "VONN\"");
    }

    [Test]
    public void BloodFrenzyBosses_DoNotUseAmbientSpawnTables()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();

        tables.Values
            .SelectMany(table => table.Spawns)
            .Select(spawn => spawn.Resref)
            .Should()
            .NotIntersectWith(new[] { "bf_butcher", "bf_kess" });
    }

    [Test]
    public void BloodFrenzySpawnTables_HavePaletteWaypointTemplates()
    {
        var root = FindRepositoryRoot();
        var paletteResrefs = GetWaypointPaletteResrefs(root)
            .ToArray();

        foreach (var (tableId, resref, waypointName, _) in BloodFrenzySpawnWaypoints)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "utw",
                $"{resref}.utw.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTW ");
            json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(waypointName);
            json.GetProperty("PaletteID").GetProperty("value").GetInt32().Should().Be(0);
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be(tableId);
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(resref);

            paletteResrefs.Should().Contain(resref);
        }

        paletteResrefs.Should().NotContain("bf_red_" + "cellar");
        paletteResrefs.Should().NotContain("bf_red_" + "circle");
        paletteResrefs.Should().NotContain("bf_red_" + "stim");
    }

    [Test]
    public void BloodFrenzyLootTables_AreScopedToSewersDepths()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        BloodFrenzyLootTables
            .Select(entry => entry.LootTableId)
            .Should()
            .OnlyHaveUniqueItems();

        foreach (var (_, lootTableId) in BloodFrenzyLootTables)
        {
            tables.Should().ContainKey(lootTableId);
        }

    }

    [Test]
    public void BloodFrenzyProofItems_AreNotLootDrops()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        foreach (var (_, lootTableId) in BloodFrenzyLootTables)
        {
            tables[lootTableId]
                .Select(item => item.Resref)
                .Should()
                .NotIntersectWith(BloodFrenzyPhysicalProofItems);
        }
    }

    [Test]
    public void PulseFrameTrainingDroidLoot_DoesNotDropMandalorianItems()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        tables["VISCARA_SEWERS_DEPTHS_PULSE_DROID"]
            .Select(item => item.Resref)
            .Should()
            .OnlyContain(resref =>
                !resref.StartsWith("m_", StringComparison.OrdinalIgnoreCase) &&
                !resref.StartsWith("mando_", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void PulseFrameTrainingDroid_PlaysFireballExplosionOnDeath()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();
        var spawn = tables["VISCARA_SEWERS_DEPTHS_GENERAL"]
            .Spawns
            .Single(spawn => spawn.Resref == "bf_pulsedroid");

        spawn.Animators.Should().ContainSingle(animator =>
            animator.Event.Value == AnimationEvent.CreatureOnDeath.Value &&
            animator.Duration == DurationType.Instant &&
            animator.Vfx == VisualEffect.Fnf_Fireball);
    }

    [Test]
    public void BloodFrenzyCreatureBlueprints_UseSewersDepthsLootTables()
    {
        var root = FindRepositoryRoot();

        foreach (var (resref, lootTableId) in BloodFrenzyLootTables)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "utc",
                $"{resref}.utc.json")));

            GetLocalString(blueprint.RootElement, "LOOT_TABLE_1").Should().Be($"{lootTableId},100,1");
        }
    }

    private static IEnumerable<string> GetWaypointPaletteResrefs(DirectoryInfo root)
    {
        using var palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "waypointpalcus.itp.json")));

        return EnumerateResrefs(palette.RootElement).ToArray();
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        return json.GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Single(entry => entry.GetProperty("Name").GetProperty("value").GetString() == variableName)
            .GetProperty("Value")
            .GetProperty("value")
            .GetString()!;
    }

    private static IEnumerable<string> EnumerateResrefs(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var resref in EnumerateResrefs(item))
                {
                    yield return resref;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("RESREF", out var resref))
            {
                yield return resref.GetProperty("value").GetString()!;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nestedResref in EnumerateResrefs(property.Value))
                {
                    yield return nestedResref;
                }
            }
        }
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
