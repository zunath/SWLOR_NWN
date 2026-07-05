using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.SkillService;
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

    private static readonly (string Resref, string RareLootTableId)[] RepeatableBloodFrenzyRareLootTables =
    {
        ("bf_scavenger", "VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES"),
        ("bf_pulsedroid", "VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES"),
        ("bf_butcher", "VISCARA_SEWERS_DEPTHS_BUTCHER_RARES"),
        ("bf_duelist", "VISCARA_SEWERS_DEPTHS_DUELIST_RARES"),
    };

    private static readonly (string RareLootTableId, string UniqueItemResref)[] BloodFrenzyRareLootDrops =
    {
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "redline_vblade"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "redvein_pistol"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "sump_vknife"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "gutter_staff"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "redvein_wraps"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "stolen_belt"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "codex_mantle"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "rustred_band"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "scav_visor"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "stalk_boots"),
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "redvein_charm"),

        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "pulse_calrifle"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "servo_pistol"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "cad_rifle"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "pulse_conduct"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "time_bracer"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "metro_ring"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "servosync_belt"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "calib_lens"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "pulse_cape"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "frame_boots"),
        ("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES", "spark_gloves"),

        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "butch_cleaver"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "butch_injector"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "stim_splitter"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "black_cleaver"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "adren_harness"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "clot_mask"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "lab_bracer"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "inject_belt"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "suture_gloves"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "adren_pendant"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "blackmkt_boots"),

        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "duel_splitter"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "charm_katar"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "redcircle_star"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "duel_fang"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "circle_twin"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "binding_sash"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "split_boots"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "restr_band"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "circle_mantle"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "duel_grip"),
        ("VISCARA_SEWERS_DEPTHS_DUELIST_RARES", "broken_charm"),
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
        ("redvein_pistol", "Red Vein Holdout", 11, 22, 45, 45, 25, true),
        ("sump_vknife", "Sump-Cut Vibroknife", 22, 21, 37, 45, 22, false),
        ("gutter_staff", "Gutterline Staff", 50, 23, 44, 45, 27, false),
        ("servo_pistol", "Servo-Tuned Pistol", 11, 22, 45, 45, 25, true),
        ("cad_rifle", "Cadence Rifle", 7, 38, 46, 45, 30, true),
        ("pulse_conduct", "Pulse Conductor", 50, 23, 44, 45, 27, false),
        ("butch_injector", "Butcher's Injector", 58, 41, 40, 45, 28, false),
        ("stim_splitter", "Stim-Splitter Axe", 13, 42, 39, 45, 30, false),
        ("black_cleaver", "Blacklab Cleaver", 13, 42, 39, 45, 30, false),
        ("charm_katar", "Charmbreaker Katar", 310, 18, 43, 45, 22, false),
        ("redcircle_star", "Red-Circle Shuriken", 59, 18, 47, 45, 22, false),
        ("duel_fang", "Duelist's Fang", 1, 23, 36, 45, 23, false),
        ("circle_twin", "Circle Twinblade", 12, 27, 41, 45, 29, false),
    };

    private static readonly (
        string Resref,
        int BaseItem,
        int ModelPart1,
        int ModelPart2,
        int ModelPart3)[] BloodFrenzyBlasterAppearances =
    {
        ("pulse_calrifle", 7, 11, 31, 11),
        ("cad_rifle", 7, 31, 164, 23),
        ("redvein_pistol", 11, 231, 101, 61),
        ("servo_pistol", 11, 11, 221, 71),
    };

    private static readonly (
        string Resref,
        string Name,
        int BaseItem)[] BloodFrenzyWearableDrops =
    {
        ("redvein_wraps", "Red Vein Wraps", 36),
        ("stolen_belt", "Sera's Stolen Belt", 21),
        ("codex_mantle", "Codex-Torn Mantle", 80),
        ("rustred_band", "Rust-Red Band", 52),
        ("scav_visor", "Scavenged Visor", 17),
        ("stalk_boots", "Sewer Stalker Boots", 26),
        ("redvein_charm", "Red Vein Charm", 19),
        ("time_bracer", "Timing Bracer", 78),
        ("metro_ring", "Metronome Ring", 52),
        ("servosync_belt", "Servo-Sync Belt", 21),
        ("calib_lens", "Calibrated Lens", 17),
        ("pulse_cape", "Pulse Relay Cape", 80),
        ("frame_boots", "Frame-Step Boots", 26),
        ("spark_gloves", "Sparkline Gloves", 36),
        ("adren_harness", "Adrenal Harness", 16),
        ("clot_mask", "Clotguard Mask", 17),
        ("lab_bracer", "Lab-Burned Bracer", 78),
        ("inject_belt", "Injector Belt", 21),
        ("suture_gloves", "Suture Gloves", 36),
        ("adren_pendant", "Adrenal Pendant", 19),
        ("blackmkt_boots", "Black-Market Boots", 26),
        ("binding_sash", "Binding Sash", 21),
        ("split_boots", "Split-Step Boots", 26),
        ("restr_band", "Restraint Band", 52),
        ("circle_mantle", "Circle Mantle", 80),
        ("duel_grip", "Duelist's Grip", 36),
        ("broken_charm", "Broken Charm", 19),
    };

    private static IEnumerable<string> BloodFrenzyUniqueDropResrefs => BloodFrenzyUniqueDrops
        .Select(entry => entry.Resref)
        .Concat(BloodFrenzyWearableDrops.Select(entry => entry.Resref));

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

    private static readonly (
        string RecipeResref,
        string RecipeName,
        RecipeType RecipeType,
        string CraftedResref,
        string CraftedName,
        int BaseItem,
        RecipeCategoryType Category)[] OldScarUniqueRareRecipes =
    {
        ("recipe_osvest", "Blueprint: Old Scar Hide Vest", RecipeType.OldScarHideVest, "os_hidevest", "Old Scar Hide Vest", 16, RecipeCategoryType.Tunic),
        ("recipe_oswrap", "Blueprint: Scarred Hound Wraps", RecipeType.OldScarWraps, "os_scarwraps", "Scarred Hound Wraps", 36, RecipeCategoryType.Glove),
        ("recipe_ostread", "Blueprint: Old Scar Treads", RecipeType.OldScarTreads, "os_treads", "Old Scar Treads", 26, RecipeCategoryType.Boots),
        ("recipe_ossash", "Blueprint: Old Scar Sash", RecipeType.OldScarSash, "os_sash", "Old Scar Sash", 21, RecipeCategoryType.Belt),
        ("recipe_osmantle", "Blueprint: Old Scar Mantle", RecipeType.OldScarMantle, "os_mantle", "Old Scar Mantle", 80, RecipeCategoryType.Cloak),
        ("recipe_oscollar", "Blueprint: Old Scar Collar", RecipeType.OldScarCollar, "os_collar", "Old Scar Collar", 19, RecipeCategoryType.Necklace),
        ("recipe_osband", "Blueprint: Old Scar Band", RecipeType.OldScarBand, "os_band", "Old Scar Band", 52, RecipeCategoryType.Ring),
        ("recipe_osguard", "Blueprint: Old Scar Guard", RecipeType.OldScarGuard, "os_guard", "Old Scar Guard", 78, RecipeCategoryType.Bracer),
        ("recipe_osvisor", "Blueprint: Old Scar Visor", RecipeType.OldScarVisor, "os_visor", "Old Scar Visor", 17, RecipeCategoryType.Cap),
        ("recipe_oscharm", "Blueprint: Old Scar Charm", RecipeType.OldScarCharm, "os_charm", "Old Scar Charm", 19, RecipeCategoryType.Necklace),
        ("recipe_ostrophy", "Blueprint: Old Scar Trophy Band", RecipeType.OldScarTrophyBand, "os_trophy", "Old Scar Trophy Band", 52, RecipeCategoryType.Ring),
        ("recipe_oshide", "Blueprint: Old Scar Hideband", RecipeType.OldScarHideband, "os_hideband", "Old Scar Hideband", 21, RecipeCategoryType.Belt),
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
    public void Wildlands_UsesWeightedRareOldScarSpawn()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();
        var wildlands = tables["VISCARA_WILDLANDS"];

        var oldScar = wildlands.Spawns.Single(spawn => spawn.Resref == "oldscar_kath");
        oldScar.Type.Should().Be(ObjectType.Creature);
        oldScar.Weight.Should().Be(1, "rare spawns should stay on the normal weighted frequency model");
        oldScar.IsRare.Should().BeTrue();

        wildlands.Spawns.Single(spawn => spawn.Resref == "warocas").Weight.Should().Be(40);
        wildlands.Spawns.Single(spawn => spawn.Resref == "kath_hound").Weight.Should().Be(70);
    }

    [Test]
    public void OldScarLoot_DropsOneGuaranteedUniqueRecipeWithLowChanceSecondRoll()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();
        var oldScarRares = tables["VISCARA_OLD_SCAR_RARES"];

        oldScarRares.IsRare.Should().BeTrue();
        oldScarRares.Should().HaveCount(OldScarUniqueRareRecipes.Length);
        oldScarRares.Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
        oldScarRares.Select(item => item.Resref)
            .Should()
            .BeEquivalentTo(OldScarUniqueRareRecipes.Select(item => item.RecipeResref));

        var oldScarTrophy = tables["VISCARA_OLD_SCAR_TROPHY"];
        oldScarTrophy.IsRare.Should().BeFalse();
        oldScarTrophy.Should().ContainSingle(item =>
            item.Resref == "oldscar_troph" &&
            item.MaxQuantity == 1 &&
            item.Weight == 1);

        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utc",
            "oldscar_kath.utc.json")));

        GetLocalString(blueprint.RootElement, "LOOT_TABLE_5").Should().Be("VISCARA_OLD_SCAR_RARES,100,1");
        GetLocalString(blueprint.RootElement, "LOOT_TABLE_6").Should().Be("VISCARA_OLD_SCAR_RARES,10,1");
        GetLocalString(blueprint.RootElement, "LOOT_TABLE_7").Should().Be("VISCARA_OLD_SCAR_TROPHY,100,1");
    }

    [Test]
    public void OldScarUniqueRareRecipeItems_LearnOldScarCraftingRecipes()
    {
        var root = FindRepositoryRoot();

        foreach (var item in OldScarUniqueRareRecipes)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{item.RecipeResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(item.RecipeName);
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(item.RecipeResref);
            GetLocalString(json, "RECIPES").Should().Be(((int)item.RecipeType).ToString());
        }
    }

    [Test]
    public void OldScarUniqueRareRecipes_CreateLevelAppropriateWearables()
    {
        var recipes = new OldScarRecipes().BuildRecipes();

        recipes.Should().HaveCount(OldScarUniqueRareRecipes.Length);

        foreach (var item in OldScarUniqueRareRecipes)
        {
            var recipe = recipes[item.RecipeType];
            recipe.Skill.Should().Be(SkillType.Smithery);
            recipe.Category.Should().Be(item.Category);
            recipe.Resref.Should().Be(item.CraftedResref);
            recipe.Level.Should().Be(8);
            recipe.Quantity.Should().Be(1);
            recipe.EnhancementType.Should().Be(RecipeEnhancementType.Armor);
            recipe.EnhancementSlots.Should().Be(1);
            recipe.Requirements.Count(requirement => requirement.GetType() == typeof(RecipeUnlockRequirement)).Should().Be(1);
            recipe.Components["oldscar_troph"].Should().Be(1);
            recipe.Components.Should().ContainKey("lth_ruined");
            recipe.Components.Should().ContainKey("fiberp_ruined");
        }
    }

    [Test]
    public void OldScarUniqueRareRecipes_AreDocumentedInSmitheryRecipeBible()
    {
        var root = FindRepositoryRoot();
        using var archive = ZipFile.OpenRead(Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx"));
        var worksheet = ReadWorksheetByName(archive, "Smithery Recipes");
        var sharedStrings = ReadSharedStrings(archive);
        var recipes = new OldScarRecipes().BuildRecipes();

        foreach (var item in OldScarUniqueRareRecipes)
        {
            var recipe = recipes[item.RecipeType];
            var row = FindWorkbookRowByCellText(worksheet, sharedStrings, "D", item.RecipeType.ToString());

            GetWorkbookCellText(worksheet, sharedStrings, $"A{row}").Should().Be("Smithery");
            GetWorkbookCellText(worksheet, sharedStrings, $"B{row}").Should().Be(item.RecipeResref);
            GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be(GetSmitheryBlueprintCategory(item.Category));
            GetWorkbookCellText(worksheet, sharedStrings, $"E{row}").Should().Be(item.Category.ToString());
            GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(1m);
            GetWorkbookCellText(worksheet, sharedStrings, $"G{row}").Should().Be(item.CraftedName);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"H{row}").Should().Be(recipe.Level);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"I{row}").Should().Be(recipe.Quantity);
            GetWorkbookCellText(worksheet, sharedStrings, $"J{row}").Should().Be(recipe.Resref);
            GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be(recipe.EnhancementType.ToString());
            GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(recipe.EnhancementSlots);
            GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be("lth_ruined");
            GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(recipe.Components["lth_ruined"]);
            GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be("fiberp_ruined");
            GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(recipe.Components["fiberp_ruined"]);
            GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be("oldscar_troph");
            GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(recipe.Components["oldscar_troph"]);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"AC{row}").Should().Be(0m);
        }
    }

    [Test]
    public void OldScarUniqueRareItems_AreLevelAppropriateWearables()
    {
        var root = FindRepositoryRoot();

        foreach (var item in OldScarUniqueRareRecipes)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{item.CraftedResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(item.CraftedName);
            json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(item.BaseItem);
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be(item.CraftedResref);
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(item.CraftedResref);

            var requiresSkill = GetItemProperty(json, ItemPropertyType.RequiresSkill);
            requiresSkill.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be((int)SkillType.Armor);
            requiresSkill.GetProperty("CostValue").GetProperty("value").GetInt32().Should().Be(5);

            var statValues = GetItemPropertyCostValues(json, ItemPropertyType.Defense)
                .Concat(GetItemPropertyCostValues(json, ItemPropertyType.Stamina))
                .Concat(GetItemPropertyCostValues(json, ItemPropertyType.FP))
                .ToArray();
            statValues.Should().HaveCountGreaterThanOrEqualTo(2);
            statValues.Should().OnlyContain(value => value >= 1 && value <= 2);

            GetItemPropertyCount(json, ItemPropertyType.DMG).Should().Be(0);
            GetItemPropertyCount(json, ItemPropertyType.Delay).Should().Be(0);
            GetItemPropertyCount(json, ItemPropertyType.UnlimitedAmmunition).Should().Be(0);
        }
    }

    [Test]
    public void OldScarUniqueRareItems_HaveUniqueAppearances()
    {
        var root = FindRepositoryRoot();
        var signatures = new List<string>();

        foreach (var item in OldScarUniqueRareRecipes)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{item.CraftedResref}.uti.json")));

            signatures.Add(GetAppearanceSignature(blueprint.RootElement));
        }

        signatures.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void OldScarTrophy_IsNewCraftingComponent()
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "uti",
            "oldscar_troph.uti.json")));

        var json = blueprint.RootElement;
        json.GetProperty("__data_type").GetString().Should().Be("UTI ");
        json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Old Scar Trophy");
        json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(536);
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("oldscar_troph");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("oldscar_troph");
    }

    [Test]
    public void SpawnSystem_RareEntriesUseWeightedSelectionWithAreaTableCap()
    {
        var root = FindRepositoryRoot();
        var spawnObjectSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "SpawnService",
            "SpawnObject.cs"));
        var builderSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "SpawnService",
            "SpawnTableBuilder.cs"));
        var tableSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "SpawnService",
            "SpawnTable.cs"));
        var spawnSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Spawn.cs"));

        spawnObjectSource.Should().Contain("public bool IsRare { get; set; }");
        builderSource.Should().Contain("public SpawnTableBuilder AsRare()");
        builderSource.Should().Contain("ActiveSpawn.IsRare = true;");
        tableSource.Should().Contain("public SpawnObject GetNextSpawn(bool includeRareSpawns = true)");
        tableSource.Should().Contain("Random.GetRandomWeightedIndex(weights)");
        tableSource.Should().Contain("obj.IsRare && !includeRareSpawns");
        spawnSource.Should().Contain("private static bool HasActiveRareSpawn(uint area, string spawnTableId)");
        spawnSource.Should().Contain("spawnTable.GetNextSpawn(!HasActiveRareSpawn(detail.Area, detail.SpawnTableId))");
        spawnSource.Should().Contain("IsRare = spawnObject.IsRare");
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

        BloodFrenzyRareLootDrops
            .Select(entry => entry.UniqueItemResref)
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

        foreach (var (_, rareLootTableId) in RepeatableBloodFrenzyRareLootTables)
        {
            tables[rareLootTableId].IsRare.Should().BeTrue();
        }

        foreach (var (rareLootTableId, uniqueItemResref) in BloodFrenzyRareLootDrops)
        {
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
    public void BloodFrenzyUniqueWearableDrops_AreArmorSkillFortyFiveItems()
    {
        var root = FindRepositoryRoot();

        foreach (var item in BloodFrenzyWearableDrops)
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

            var requiresSkill = GetItemProperty(json, ItemPropertyType.RequiresSkill);
            requiresSkill.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be((int)SkillType.Armor);
            requiresSkill.GetProperty("CostValue").GetProperty("value").GetInt32().Should().Be(45);

            GetItemPropertyCount(json, ItemPropertyType.DMG).Should().Be(0);
            GetItemPropertyCount(json, ItemPropertyType.Delay).Should().Be(0);
            GetItemPropertyCount(json, ItemPropertyType.UnlimitedAmmunition).Should().Be(0);
        }
    }

    [Test]
    public void BloodFrenzyUniqueDropItems_HaveUniqueAppearances()
    {
        var root = FindRepositoryRoot();
        var signatures = new List<string>();

        foreach (var resref in BloodFrenzyUniqueDropResrefs)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{resref}.uti.json")));

            var signature = GetAppearanceSignature(blueprint.RootElement);
            signature.Should().NotBeNullOrWhiteSpace();
            signatures.Add(signature);
        }

        signatures.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void BloodFrenzyBlasterDrops_UseKnownValidAppearanceTriplets()
    {
        var root = FindRepositoryRoot();

        foreach (var item in BloodFrenzyBlasterAppearances)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{item.Resref}.uti.json")));

            var json = blueprint.RootElement;
            GetBlueprintInt(json, "BaseItem").Should().Be(item.BaseItem);
            GetBlueprintInt(json, "ModelPart1").Should().Be(item.ModelPart1);
            GetBlueprintInt(json, "ModelPart2").Should().Be(item.ModelPart2);
            GetBlueprintInt(json, "ModelPart3").Should().Be(item.ModelPart3);
            GetBlueprintInt(json, "xModelPart1").Should().Be(item.ModelPart1);
            GetBlueprintInt(json, "xModelPart2").Should().Be(item.ModelPart2);
            GetBlueprintInt(json, "xModelPart3").Should().Be(item.ModelPart3);
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

        foreach (var (resref, rareLootTableId) in RepeatableBloodFrenzyRareLootTables)
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

    private static IEnumerable<int> GetItemPropertyCostValues(JsonElement json, ItemPropertyType propertyName)
    {
        return json.GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray()
            .Where(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)propertyName)
            .Select(property => property.GetProperty("CostValue").GetProperty("value").GetInt32());
    }

    private static int GetItemPropertyCount(JsonElement json, ItemPropertyType propertyName)
    {
        return json.GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray()
            .Count(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)propertyName);
    }

    private static int GetBlueprintInt(JsonElement json, string propertyName)
    {
        return json.GetProperty(propertyName).GetProperty("value").GetInt32();
    }

    private static string GetAppearanceSignature(JsonElement json)
    {
        var parts = new List<string>
        {
            json.GetProperty("BaseItem").GetProperty("value").GetInt32().ToString()
        };

        foreach (var propertyName in new[]
                 {
                     "ModelPart1",
                     "ModelPart2",
                     "ModelPart3",
                     "ArmorPart_Belt",
                     "ArmorPart_LBicep",
                     "ArmorPart_LFArm",
                     "ArmorPart_LFoot",
                     "ArmorPart_LHand",
                     "ArmorPart_LShin",
                     "ArmorPart_LShoul",
                     "ArmorPart_LThigh",
                     "ArmorPart_Neck",
                     "ArmorPart_Pelvis",
                     "ArmorPart_RBicep",
                     "ArmorPart_RFArm",
                     "ArmorPart_RFoot",
                     "ArmorPart_RHand",
                     "ArmorPart_Robe",
                     "ArmorPart_RShin",
                     "ArmorPart_RShoul",
                     "ArmorPart_RThigh",
                     "ArmorPart_Torso",
                     "Cloth1Color",
                     "Cloth2Color",
                     "Leather1Color",
                     "Leather2Color",
                     "Metal1Color",
                     "Metal2Color"
                 })
        {
            AddAppearancePart(json, parts, propertyName);
        }

        parts.Count.Should().BeGreaterThan(1, "unique drops need a visible item appearance");

        return string.Join("|", parts);
    }

    private static void AddAppearancePart(JsonElement json, ICollection<string> parts, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property) ||
            !property.TryGetProperty("value", out var value))
        {
            return;
        }

        parts.Add($"{propertyName}:{value.GetInt32()}");
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

    private static XDocument ReadWorkbookXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull($"{entryName} should exist in the combat Bible workbook");

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static XDocument ReadWorksheetByName(ZipArchive archive, string sheetName)
    {
        var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
        var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels");
        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        var sheet = workbook
            .Descendants(workbookNs + "sheet")
            .Single(candidate => candidate.Attribute("name")?.Value == sheetName);
        var relationshipId = sheet.Attribute(relationshipNs + "id")?.Value;
        relationshipId.Should().NotBeNullOrWhiteSpace($"{sheetName} should have a workbook relationship id");

        var target = relationships
            .Descendants(packageRelationshipNs + "Relationship")
            .Single(candidate => candidate.Attribute("Id")?.Value == relationshipId)
            .Attribute("Target")?
            .Value
            .Replace('\\', '/');
        target.Should().NotBeNullOrWhiteSpace($"{sheetName} should resolve to a worksheet XML target");

        var entryName = target!.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : $"xl/{target}";
        return ReadWorkbookXml(archive, entryName);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        var sharedStrings = ReadWorkbookXml(archive, "xl/sharedStrings.xml");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return sharedStrings
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static int FindWorkbookRowByCellText(
        XDocument worksheet,
        IReadOnlyList<string> sharedStrings,
        string column,
        string text)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return worksheet
            .Descendants(ns + "row")
            .Select(row => int.Parse(row.Attribute("r")!.Value, CultureInfo.InvariantCulture))
            .Single(row => GetWorkbookCellText(worksheet, sharedStrings, $"{column}{row}") == text);
    }

    private static string GetWorkbookCellText(XDocument worksheet, IReadOnlyList<string> sharedStrings, string address)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cell = worksheet
            .Descendants(ns + "c")
            .SingleOrDefault(candidate => candidate.Attribute("r")?.Value == address);

        if (cell == null)
            return string.Empty;

        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));

        var value = cell.Element(ns + "v")?.Value;
        if (type == "s" && int.TryParse(value, out var index))
            return sharedStrings[index];

        return value ?? string.Empty;
    }

    private static decimal GetWorkbookCellNumber(XDocument worksheet, IReadOnlyList<string> sharedStrings, string address)
    {
        var text = GetWorkbookCellText(worksheet, sharedStrings, address);
        return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static string GetSmitheryBlueprintCategory(RecipeCategoryType category)
    {
        return category switch
        {
            RecipeCategoryType.Belt or
                RecipeCategoryType.Cloak or
                RecipeCategoryType.Necklace or
                RecipeCategoryType.Ring => "Accessory Blueprints",
            _ => "Armor Blueprints"
        };
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
