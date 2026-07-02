using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum.Item;
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

    private static readonly (string Resref, string RareLootTableId, string UniqueItemResref)[] RepeatableBloodFrenzyRareLootTables =
    {
        ("bf_scavenger", "VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "redline_vblade"),
        ("bf_pulsedroid", "VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "pulse_calrifle"),
        ("bf_butcher", "VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "butch_cleaver"),
        ("bf_duelist", "VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "duel_splitter"),
    };

    private static readonly (
        string Resref,
        string Name,
        int BaseItem,
        int Damage,
        int RequiredSkillSubtype,
        int RequiredSkill,
        int Delay,
        bool HasUnlimitedAmmunition)[] BloodFrenzyUniqueDrops =
    {
        ("redline_vblade", "Redline Vibroblade", 1, 23, 36, 45, 23, false),
        ("pulse_calrifle", "Pulse-Frame Calibration Rifle", 7, 38, 46, 45, 30, true),
        ("butch_cleaver", "Butcher's Cleaver", 13, 42, 39, 45, 30, false),
        ("duel_splitter", "Duelist's Splitter", 12, 27, 41, 45, 29, false),
    };

    private static IEnumerable<string> BloodFrenzyLootTableIds => BloodFrenzyLootTables
        .Select(entry => entry.LootTableId)
        .Concat(RepeatableBloodFrenzyRareLootTables.Select(entry => entry.RareLootTableId));

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
            .Concat(RepeatableBloodFrenzyRareLootTables.Select(entry => entry.RareLootTableId))
            .Should()
            .OnlyHaveUniqueItems();

        foreach (var lootTableId in BloodFrenzyLootTableIds)
        {
            tables.Should().ContainKey(lootTableId);
        }

    }

    [Test]
    public void BloodFrenzyProofItems_AreNotLootDrops()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        foreach (var lootTableId in BloodFrenzyLootTableIds)
        {
            tables[lootTableId]
                .Select(item => item.Resref)
                .Should()
                .NotIntersectWith(BloodFrenzyPhysicalProofItems);
        }
    }

    [Test]
    public void BloodFrenzySewersDepthsLoot_DoesNotDropMandalorianItems()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        foreach (var lootTableId in BloodFrenzyLootTableIds)
        {
            tables[lootTableId]
                .Select(item => item.Resref)
                .Should()
                .OnlyContain(resref =>
                    !resref.StartsWith("m_", StringComparison.OrdinalIgnoreCase) &&
                    !resref.StartsWith("mando_", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Test]
    public void BloodFrenzyUniqueLootTables_AreRareItemDrops()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        foreach (var (_, rareLootTableId, uniqueItemResref) in RepeatableBloodFrenzyRareLootTables)
        {
            tables[rareLootTableId].IsRare.Should().BeTrue();
            tables[rareLootTableId].Should().ContainSingle(item =>
                item.Resref == uniqueItemResref &&
                item.IsRare &&
                item.MaxQuantity == 1);
        }
    }

    [Test]
    public void BloodFrenzyUniqueDropItems_AreBetweenOphidianAndChiroStats()
    {
        var root = FindRepositoryRoot();

        foreach (var item in BloodFrenzyUniqueDrops)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{item.Resref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(item.Name);
            json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(item.BaseItem);
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be(item.Resref);
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(item.Resref);

            GetItemPropertyCostValue(json, ItemPropertyType.DMG).Should().Be(item.Damage);
            GetItemPropertyCostValue(json, ItemPropertyType.Delay).Should().Be(item.Delay);

            var requiresSkill = GetItemProperty(json, ItemPropertyType.RequiresSkill);
            requiresSkill.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be(item.RequiredSkillSubtype);
            requiresSkill.GetProperty("CostValue").GetProperty("value").GetInt32().Should().Be(item.RequiredSkill);

            var unlimitedAmmunitionCount = GetItemPropertyCount(json, ItemPropertyType.UnlimitedAmmunition);
            unlimitedAmmunitionCount.Should().Be(item.HasUnlimitedAmmunition ? 1 : 0);
        }
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

        foreach (var (resref, rareLootTableId, _) in RepeatableBloodFrenzyRareLootTables)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "utc",
                $"{resref}.utc.json")));

            GetLocalString(blueprint.RootElement, "LOOT_TABLE_2").Should().Be($"{rareLootTableId},5,1");
        }

        using var kess = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utc",
            "bf_kess.utc.json")));

        TryGetLocalString(kess.RootElement, "LOOT_TABLE_2").Should().BeNull();
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
        return TryGetLocalString(json, variableName)
               ?? throw new InvalidOperationException($"Could not find local string '{variableName}'.");
    }

    private static string TryGetLocalString(JsonElement json, string variableName)
    {
        foreach (var entry in json.GetProperty("VarTable").GetProperty("value").EnumerateArray())
        {
            if (entry.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                return entry.GetProperty("Value").GetProperty("value").GetString();
            }
        }

        return null;
    }

    private static JsonElement GetItemProperty(JsonElement json, ItemPropertyType propertyName)
    {
        return json.GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray()
            .Single(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)propertyName);
    }

    private static int GetItemPropertyCostValue(JsonElement json, ItemPropertyType propertyName)
    {
        return GetItemProperty(json, propertyName)
            .GetProperty("CostValue")
            .GetProperty("value")
            .GetInt32();
    }

    private static int GetItemPropertyCount(JsonElement json, ItemPropertyType propertyName)
    {
        return json.GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray()
            .Count(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)propertyName);
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
