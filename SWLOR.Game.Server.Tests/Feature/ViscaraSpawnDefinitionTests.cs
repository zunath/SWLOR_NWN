using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PropertyService;
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
        ("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES", "redvein_vblade"),
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

        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "rending_cleaver"),
        ("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES", "adrenal_injector"),
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
        ("redvein_vblade", "Red Vein Vibroblade", 1, 23, 36, 45, 23, false),
        ("pulse_calrifle", "Pulse-Frame Calibration Rifle", 7, 38, 46, 45, 30, true),
        ("rending_cleaver", "Rending Cleaver", 13, 42, 39, 45, 30, false),
        ("duel_splitter", "Duelist's Splitter", 12, 27, 41, 45, 29, false),
        ("redvein_pistol", "Red Vein Holdout", 11, 22, 45, 45, 25, true),
        ("sump_vknife", "Sump-Cut Vibroknife", 22, 21, 37, 45, 22, false),
        ("gutter_staff", "Gutterline Staff", 50, 23, 44, 45, 27, false),
        ("servo_pistol", "Servo-Tuned Pistol", 11, 22, 45, 45, 25, true),
        ("cad_rifle", "Cadence Rifle", 7, 38, 46, 45, 30, true),
        ("pulse_conduct", "Pulse Conductor", 50, 23, 44, 45, 27, false),
        ("adrenal_injector", "Adrenal Injector", 58, 41, 40, 45, 28, false),
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
        ("recipe_osvest", "Blueprint: Fanghide Vest", RecipeType.FanghideVest, "os_hidevest", "Fanghide Vest", 16, RecipeCategoryType.Tunic),
        ("recipe_oswrap", "Blueprint: Fanghide Wraps", RecipeType.FanghideWraps, "os_scarwraps", "Fanghide Wraps", 36, RecipeCategoryType.Glove),
        ("recipe_ostread", "Blueprint: Fanghide Treads", RecipeType.FanghideTreads, "os_treads", "Fanghide Treads", 26, RecipeCategoryType.Boots),
        ("recipe_ossash", "Blueprint: Fanghide Sash", RecipeType.FanghideSash, "os_sash", "Fanghide Sash", 21, RecipeCategoryType.Belt),
        ("recipe_osmantle", "Blueprint: Fanghide Mantle", RecipeType.FanghideMantle, "os_mantle", "Fanghide Mantle", 80, RecipeCategoryType.Cloak),
        ("recipe_oscollar", "Blueprint: Fanghide Collar", RecipeType.FanghideCollar, "os_collar", "Fanghide Collar", 19, RecipeCategoryType.Necklace),
        ("recipe_osband", "Blueprint: Fanghide Band", RecipeType.FanghideBand, "os_band", "Fanghide Band", 52, RecipeCategoryType.Ring),
        ("recipe_osguard", "Blueprint: Fanghide Guard", RecipeType.FanghideGuard, "os_guard", "Fanghide Guard", 78, RecipeCategoryType.Bracer),
        ("recipe_osvisor", "Blueprint: Fanghide Visor", RecipeType.FanghideVisor, "os_visor", "Fanghide Visor", 17, RecipeCategoryType.Cap),
        ("recipe_oscharm", "Blueprint: Fanghide Charm", RecipeType.FanghideCharm, "os_charm", "Fanghide Charm", 19, RecipeCategoryType.Necklace),
        ("recipe_ostrophy", "Blueprint: Fangmarked Band", RecipeType.FangmarkedBand, "os_trophy", "Fangmarked Band", 52, RecipeCategoryType.Ring),
        ("recipe_oshide", "Blueprint: Fanghide Binding", RecipeType.FanghideBinding, "os_hideband", "Fanghide Binding", 21, RecipeCategoryType.Belt),
    };

    private static readonly (
        string RecipeResref,
        string RecipeName,
        RecipeType RecipeType,
        string CraftedResref,
        string CraftedName,
        int BaseItem,
        RecipeCategoryType Category)[] StormplumeUniqueRareRecipes =
    {
        ("recipe_spharn", "Blueprint: Skycrest Harness", RecipeType.SkycrestHarness, "sp_harness", "Skycrest Harness", 16, RecipeCategoryType.Tunic),
        ("recipe_spwrap", "Blueprint: Skycrest Talonwraps", RecipeType.SkycrestTalonwraps, "sp_talonwrap", "Skycrest Talonwraps", 36, RecipeCategoryType.Glove),
        ("recipe_spstrid", "Blueprint: Skycrest Striders", RecipeType.SkycrestStriders, "sp_striders", "Skycrest Striders", 26, RecipeCategoryType.Boots),
        ("recipe_spsash", "Blueprint: Skycrest Sash", RecipeType.SkycrestSash, "sp_sash", "Skycrest Sash", 21, RecipeCategoryType.Belt),
        ("recipe_spmant", "Blueprint: Skycrest Mantle", RecipeType.SkycrestMantle, "sp_mantle", "Skycrest Mantle", 80, RecipeCategoryType.Cloak),
        ("recipe_spgorg", "Blueprint: Skycrest Gorget", RecipeType.SkycrestGorget, "sp_gorget", "Skycrest Gorget", 19, RecipeCategoryType.Necklace),
        ("recipe_spband", "Blueprint: Skycrest Band", RecipeType.SkycrestBand, "sp_band", "Skycrest Band", 52, RecipeCategoryType.Ring),
        ("recipe_spguard", "Blueprint: Skycrest Guard", RecipeType.SkycrestGuard, "sp_guard", "Skycrest Guard", 78, RecipeCategoryType.Bracer),
        ("recipe_spvisor", "Blueprint: Skycrest Visor", RecipeType.SkycrestVisor, "sp_crestvis", "Skycrest Visor", 17, RecipeCategoryType.Cap),
        ("recipe_spcharm", "Blueprint: Skycrest Charm", RecipeType.SkycrestCharm, "sp_beakcharm", "Skycrest Charm", 19, RecipeCategoryType.Necklace),
        ("recipe_sptroph", "Blueprint: Skycrest Trophy Band", RecipeType.SkycrestTrophyBand, "sp_trophy", "Skycrest Trophy Band", 52, RecipeCategoryType.Ring),
        ("recipe_spplume", "Blueprint: Skycrest Braid", RecipeType.SkycrestBraid, "sp_plumebraid", "Skycrest Braid", 21, RecipeCategoryType.Belt),
    };

    private static readonly NamedRareEliteSpec[] NamedRareEliteSpecs =
    {
        new("soot_rusk", "VISCARA_VELES_SEWERS", "VISCARA_SOOTLINE_RUSK_RARES", "VISCARA_SOOTLINE_RUSK_TOKEN", "sr_token", "veles_sewers.git.json"),
        new("nara_venn", "VISCARA_WILDWOODS_LOOTERS", "VISCARA_NARA_VENN_RARES", "VISCARA_NARA_VENN_PIN", "nv_pin", "viscarawildwoods.git.json"),
        new("silkshade", "VISCARA_WILDWOODS_KINRATH", "VISCARA_SILKSHADE_RARES", "VISCARA_SILKSHADE_SILK", "ss_silk", "viscarawildwoods.git.json"),
        new("mossback", "VISCARA_WILDWOODS_GIMPASSA", "VISCARA_MOSSBACK_RARES", "VISCARA_MOSSBACK_SHELL", "mb_shell", "viscarawildwoods.git.json"),
        new("tarn_kyric", "VISCARA_WILDWOODS_NORTH_SCOUT", "VISCARA_TARN_KYRIC_RARES", "VISCARA_TARN_KYRIC_BADGE", "tk_badge", "viscara_wwnorth.git.json"),
        new("varo_skeld", "VISCARA_WILDWOODS_RUINED_HUNTER", "VISCARA_VARO_SKELD_RARES", "VISCARA_VARO_SKELD_MASK", "vs_mask", "viscara_wwruined.git.json"),
        new("harrek_voss", "VISCARA_MANDALORIAN_RAIDERS", "VISCARA_HARREK_VOSS_RARES", "VISCARA_HARREK_VOSS_PLATE", "hv_plate", "manda_facility.git.json"),
        new("greyspine", "VISCARA_VALLEY_CAIRNMOGS", "VISCARA_GREYSPINE_RARES", "VISCARA_GREYSPINE_SPINE", "gs_spine", "viscarawildwest.git.json"),
        new("maw_ghal", "VISCARA_COXXION_FLESHEATERS", "VISCARA_MAW_SEER_GHAL_RARES", "VISCARA_MAW_SEER_GHAL_TOTEM", "mg_totem", "v_cox_base.git.json"),
        new("redtail_kor", "VISCARA_DEEPMOUNTAIN_RAIVORS", "VISCARA_REDTAIL_KOR_RARES", "VISCARA_REDTAIL_KOR_CLAW", "rk_claw", "viscaradeepmount.git.json"),
        new("shardeye", "VISCARA_CRYSTAL_SPIDERS", "VISCARA_SHARD_EYE_RARES", "VISCARA_SHARD_EYE", "se_eye", "area.git.json"),
        new("rootcoil", "VISCARA_WESTERN_SWAMPLANDS", "VISCARA_ROOTCOIL_RARES", "VISCARA_ROOTCOIL_VINE", "rc_vine", "viscaranwswamp.git.json"),
        new("mirevein", "VISCARA_EASTERN_SWAMPLANDS", "VISCARA_MIREVEIN_RARES", "VISCARA_MIREVEIN_CORE", "mv_core", "viscaranswamp.git.json"),
        new("vrix7", "VISCARA_SEWERS_DEPTHS_GENERAL", "VISCARA_VRIX7_RARES", "VISCARA_VRIX7_CORE", "vx_core", "visc_sewer_depth.git.json"),
        new("ashwing", "VISCARA_REVANITE_MAZE", "VISCARA_ASHWING_RARES", "VISCARA_ASHWING_ECHO", "ae_echo", "r_prax_sith.git.json"),
    };

    private static readonly (string Resref, string Name, string Description)[] RareEliteGuaranteedComponents =
    {
        ("oldscar_troph", "Fangmarked Trophy", "A scarred crafting trophy used in Fanghide gear recipes."),
        ("stormpl_plume", "Skycrest Quill", "A bright crafting quill used in Skycrest gear recipes."),
        ("sr_token", "Charcoal Token", "A soot-blackened crafting token used in Ashmark furniture recipes and the Faultline Capacitor."),
        ("nv_pin", "Guttermark Pin", "A dark metal pin used in Guttermark furniture recipes and the Ghostkey Relay."),
        ("ss_silk", "Gloam Silk", "A heavy strand of silk used in Gloamweave gear recipes and the Gloam Skewer."),
        ("mb_shell", "Pitted Shell", "A plated shell fragment used in Shellward gear recipes and the Savory Shell Braise."),
        ("tk_badge", "Trailmark Badge", "A trail-worn badge used in Trailmark furniture recipes and the Wayfinder Sensor."),
        ("vs_mask", "Dustfall Mask", "A cracked field mask used in Stonewake furniture recipes and the Stonewake Relay."),
        ("hv_plate", "Battleworn Plate", "A scored armor plate used in Aegisline furniture recipes and the Kinetic Harness."),
        ("gs_spine", "Stonebarb Spine", "A barbed spine used in Stonebarb gear recipes and the Stonebarb Pot Pie."),
        ("mg_totem", "Whisper Totem", "A carved totem used in Veilcarved furniture recipes and the Lucid Splice."),
        ("rk_claw", "Emberclaw", "A curved crafting claw used in Emberclaw gear recipes and the Emberclaw Roast."),
        ("se_eye", "Prism Lens", "A faceted lens used in Prismhide gear recipes and the Prism Consomme."),
        ("rc_vine", "Bogvine Strand", "A tough vine strand used in Marshguard gear recipes and the Marshleaf Broth."),
        ("mv_core", "Bitter Core", "A bitter root core used in Fenbloom gear recipes and the Bitter Fen Tea."),
        ("vx_core", "Surge Core", "A charged core used in Surgewake furniture recipes and the Stormcore Matrix."),
        ("ae_echo", "Resonant Shard", "A resonant shard used in Resonant furniture recipes and the Resonant Broth."),
    };

    private static readonly string[] NamedRareEliteEngineeringCreativeNames =
    {
        "Faultline Capacitor",
        "Ghostkey Relay",
        "Wayfinder Sensor",
        "Stonewake Relay",
        "Kinetic Harness",
        "Lucid Splice",
        "Stormcore Matrix",
    };

    private static readonly string[] RareEliteSourceNameFragments =
    {
        "Old Scar",
        "Stormplume",
        "Sootline",
        "Rusk",
        "Nara",
        "Venn",
        "Blackleaf",
        "Silkshade",
        "Mossback",
        "Tarn",
        "Kyric",
        "Brushstalker",
        "Varo",
        "Skeld",
        "Ruin-Stalker",
        "Harrek",
        "Voss",
        "Iron-Stripe",
        "Ironstride",
        "Greyspine",
        "Rootbreaker",
        "Maw-Seer",
        "Maw",
        "Ghal",
        "Redtail",
        "Kor",
        "Shard-Eye",
        "Rootcoil",
        "Drowned Mass",
        "Mirevein",
        "Tangle",
        "Vrix-7",
        "Vrix",
        "Pulse Butcher",
        "Ashwing",
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

        var stormplume = wildlands.Spawns.Single(spawn => spawn.Resref == "stormplume");
        stormplume.Type.Should().Be(ObjectType.Creature);
        stormplume.Weight.Should().Be(1, "rare spawns should stay on the normal weighted frequency model");
        stormplume.IsRare.Should().BeTrue();

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
    public void StormplumeLoot_DropsOneGuaranteedUniqueRecipeWithLowChanceSecondRoll()
    {
        var tables = new ViscaraLootTableDefinition().BuildLootTables();
        var stormplumeRares = tables["VISCARA_STORMPLUME_RARES"];

        stormplumeRares.IsRare.Should().BeTrue();
        stormplumeRares.Should().HaveCount(StormplumeUniqueRareRecipes.Length);
        stormplumeRares.Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
        stormplumeRares.Select(item => item.Resref)
            .Should()
            .BeEquivalentTo(StormplumeUniqueRareRecipes.Select(item => item.RecipeResref));

        var stormplumePlume = tables["VISCARA_STORMPLUME_PLUME"];
        stormplumePlume.IsRare.Should().BeFalse();
        stormplumePlume.Should().ContainSingle(item =>
            item.Resref == "stormpl_plume" &&
            item.MaxQuantity == 1 &&
            item.Weight == 1);

        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "utc",
            "stormplume.utc.json")));

        GetLocalString(blueprint.RootElement, "LOOT_TABLE_5").Should().Be("VISCARA_STORMPLUME_RARES,100,1");
        GetLocalString(blueprint.RootElement, "LOOT_TABLE_6").Should().Be("VISCARA_STORMPLUME_RARES,10,1");
        GetLocalString(blueprint.RootElement, "LOOT_TABLE_7").Should().Be("VISCARA_STORMPLUME_PLUME,100,1");
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
    public void StormplumeUniqueRareRecipeItems_LearnStormplumeCraftingRecipes()
    {
        var root = FindRepositoryRoot();

        foreach (var item in StormplumeUniqueRareRecipes)
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
        var recipes = new FanghideRecipes().BuildRecipes();

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
    public void StormplumeUniqueRareRecipes_CreateLevelAppropriateWearables()
    {
        var recipes = new SkycrestRecipes().BuildRecipes();

        recipes.Should().HaveCount(StormplumeUniqueRareRecipes.Length);

        foreach (var item in StormplumeUniqueRareRecipes)
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
            recipe.Components["stormpl_plume"].Should().Be(1);
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
        var recipes = new FanghideRecipes().BuildRecipes();

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
    public void StormplumeUniqueRareRecipes_AreDocumentedInSmitheryRecipeBible()
    {
        var root = FindRepositoryRoot();
        using var archive = ZipFile.OpenRead(Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx"));
        var worksheet = ReadWorksheetByName(archive, "Smithery Recipes");
        var sharedStrings = ReadSharedStrings(archive);
        var recipes = new SkycrestRecipes().BuildRecipes();

        foreach (var item in StormplumeUniqueRareRecipes)
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
            GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be("stormpl_plume");
            GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(recipe.Components["stormpl_plume"]);
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
    public void StormplumeUniqueRareItems_AreLevelAppropriateWearables()
    {
        var root = FindRepositoryRoot();

        foreach (var item in StormplumeUniqueRareRecipes)
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
    public void StormplumeUniqueRareItems_HaveUniqueAppearances()
    {
        var root = FindRepositoryRoot();
        var signatures = new List<string>();

        foreach (var item in StormplumeUniqueRareRecipes)
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
        json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Fangmarked Trophy");
        json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(536);
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("oldscar_troph");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("oldscar_troph");
    }

    [Test]
    public void StormplumePlume_IsNewCraftingComponent()
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "uti",
            "stormpl_plume.uti.json")));

        var json = blueprint.RootElement;
        json.GetProperty("__data_type").GetString().Should().Be("UTI ");
        json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Skycrest Quill");
        json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(536);
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("stormpl_plume");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("stormpl_plume");
    }

    [Test]
    public void RareEliteGuaranteedComponents_HaveSpecificCraftingDescriptions()
    {
        var root = FindRepositoryRoot();

        foreach (var component in RareEliteGuaranteedComponents)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{component.Resref}.uti.json")));

            var json = blueprint.RootElement;
            GetLocalizedName(json).Should().Be(component.Name);
            GetLocalizedString(json, "Description").Should().Be(component.Description);
            GetLocalizedString(json, "DescIdentified").Should().Be(component.Description);
        }
    }

    [Test]
    public void ViscaraNamedRareElites_UseWeightedRareEntriesInSingleSpawnTable()
    {
        var tables = new ViscaraSpawnDefinition().BuildSpawnTables();
        var spawnEntries = tables
            .SelectMany(table => table.Value.Spawns.Select(spawn => new { TableId = table.Key, Spawn = spawn }))
            .ToArray();

        foreach (var spec in NamedRareEliteSpecs)
        {
            var entry = spawnEntries
                .Where(candidate => candidate.Spawn.Resref == spec.Resref)
                .Should()
                .ContainSingle($"{spec.Resref} should be wired to one spawn table only")
                .Subject;

            entry.TableId.Should().Be(spec.SpawnTableId);
            entry.Spawn.Type.Should().Be(ObjectType.Creature);
            entry.Spawn.Weight.Should().Be(1, "named rare elites should stay on the normal weighted frequency model");
            entry.Spawn.IsRare.Should().BeTrue();
        }
    }

    [Test]
    public void ViscaraNamedRareEliteSpawnTables_ArePlacedInOneAreaFile()
    {
        var root = FindRepositoryRoot();
        var gitFiles = Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json");

        foreach (var spec in NamedRareEliteSpecs)
        {
            var filesWithTable = gitFiles
                .Where(file => File.ReadAllText(file).Contains($"\"value\": \"{spec.SpawnTableId}\"", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .ToArray();

            filesWithTable.Should().BeEquivalentTo(
                new[] { spec.ModuleFile },
                $"{spec.Resref} should not leak into another area through a reused spawn table");
        }
    }

    [Test]
    public void ViscaraNamedRareEliteLoot_DropsOneGuaranteedUniqueRecipeWithLowChanceSecondRoll()
    {
        var root = FindRepositoryRoot();
        var tables = new ViscaraLootTableDefinition().BuildLootTables();

        foreach (var spec in NamedRareEliteSpecs)
        {
            var expectedRecipeResrefs = GetNamedRareEliteRecipeEntries()
                .Where(entry => entry.RareComponent == spec.ComponentResref)
                .Select(GetNamedRareEliteRecipeResref)
                .ToArray();

            expectedRecipeResrefs.Length.Should().BeInRange(10, 20, $"{spec.Resref} should expose a 10-20 recipe named rare pool");

            var rareTable = tables[spec.RareLootTableId];
            rareTable.IsRare.Should().BeTrue();
            rareTable.Should().HaveCount(expectedRecipeResrefs.Length);
            rareTable.Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
            rareTable.Select(item => item.Resref).Should().BeEquivalentTo(expectedRecipeResrefs);

            var componentTable = tables[spec.ComponentLootTableId];
            componentTable.IsRare.Should().BeFalse();
            componentTable.Should().ContainSingle(item =>
                item.Resref == spec.ComponentResref &&
                item.MaxQuantity == 1 &&
                item.Weight == 1);

            using var utc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "utc",
                $"{spec.Resref}.utc.json")));
            var lootLocals = GetLocalStringsWithPrefix(utc.RootElement, "LOOT_TABLE_").ToArray();

            lootLocals.Should().Contain($"{spec.RareLootTableId},100,1");
            lootLocals.Should().Contain($"{spec.RareLootTableId},10,1");
            lootLocals.Should().Contain($"{spec.ComponentLootTableId},100,1");
        }
    }

    [Test]
    public void ViscaraNamedRareEliteRecipeItems_LearnRegisteredRecipes()
    {
        var root = FindRepositoryRoot();

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            var recipeResref = GetNamedRareEliteRecipeResref(entry);
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{recipeResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            GetLocalizedName(json).Should().StartWith("Blueprint: ");
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(recipeResref);
            GetLocalString(json, "RECIPES").Should().Be(((int)entry.Recipe).ToString());
        }
    }

    [Test]
    public void ViscaraNamedRareEliteRecipes_CreateUnlockedOutputs()
    {
        var recipes = BuildNamedRareEliteRecipes();

        recipes.Should().HaveCount(GetNamedRareEliteRecipeEntries().Count());

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            var recipe = recipes[entry.Recipe];
            recipe.Skill.Should().Be(entry.Skill);
            recipe.Category.Should().Be(entry.Category);
            recipe.Resref.Should().Be(entry.CraftedResref);
            recipe.Level.Should().Be(entry.Level);
            recipe.Quantity.Should().Be(1);
            recipe.EnhancementType.Should().Be(entry.EnhancementType);
            recipe.EnhancementSlots.Should().Be(entry.EnhancementType == RecipeEnhancementType.None ? 0 : 1);
            recipe.Requirements.Count(requirement => requirement.GetType() == typeof(RecipeUnlockRequirement)).Should().Be(1);
            recipe.Components[entry.PrimaryComponent].Should().Be(entry.PrimaryQuantity);
            recipe.Components[entry.SecondaryComponent].Should().Be(entry.SecondaryQuantity);
            recipe.Components[entry.RareComponent].Should().Be(1);
        }
    }

    [Test]
    public void ViscaraNamedRareEliteOutputs_AreNewRecipeCraftedAssets()
    {
        var root = FindRepositoryRoot();

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{entry.CraftedResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            GetLocalizedName(json).Should().NotBeNullOrWhiteSpace();
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be(entry.CraftedResref);
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(entry.CraftedResref);

            if (entry.Skill == SkillType.Smithery)
            {
                var requiresSkill = GetItemProperty(json, ItemPropertyType.RequiresSkill);
                requiresSkill.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be((int)SkillType.Armor);

                var statValues = GetItemPropertyCostValues(json, ItemPropertyType.Defense)
                    .Concat(GetItemPropertyCostValues(json, ItemPropertyType.Stamina))
                    .Concat(GetItemPropertyCostValues(json, ItemPropertyType.FP))
                    .ToArray();
                statValues.Should().HaveCountGreaterThanOrEqualTo(2);

                GetItemPropertyCount(json, ItemPropertyType.DMG).Should().Be(0);
                GetItemPropertyCount(json, ItemPropertyType.Delay).Should().Be(0);
                GetItemPropertyCount(json, ItemPropertyType.UnlimitedAmmunition).Should().Be(0);
            }
            else if (entry.Skill == SkillType.Fabrication)
            {
                json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(29);
                AssertRegisteredPropertyStructure(entry.CraftedResref);
            }
            else if (entry.Skill == SkillType.Agriculture)
            {
                json.GetProperty("Charges").GetProperty("value").GetInt32().Should().Be(1);
                json.GetProperty("PaletteID").GetProperty("value").GetInt32().Should().Be(23);
                json.GetProperty("PropertiesList").GetProperty("value").GetArrayLength().Should().BeGreaterThan(0);
            }
            else
            {
                json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(77);
                json.GetProperty("Charges").GetProperty("value").GetInt32().Should().Be(1);
                json.GetProperty("PaletteID").GetProperty("value").GetInt32().Should().Be(54);

                var activationProperty = GetItemProperty(json, ItemPropertyType.CastSpell);
                activationProperty.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be(335);
            }
        }
    }

    [Test]
    public void ViscaraNamedRareEliteEngineeringOutputs_AreRegisteredUsableItems()
    {
        var items = new Dictionary<string, ItemDetail>();
        var itemDefinitions = new IItemListDefinition[]
        {
            new FaultlineCapacitorItemDefinition(),
            new GhostkeyRelayItemDefinition(),
            new WayfinderSensorItemDefinition(),
            new StonewakeRelayItemDefinition(),
            new KineticHarnessItemDefinition(),
            new LucidSpliceItemDefinition(),
            new StormcoreMatrixItemDefinition(),
        };

        foreach (var itemDefinition in itemDefinitions)
        {
            foreach (var item in itemDefinition.BuildItems())
            {
                items[item.Key] = item.Value;
            }
        }

        var engineeringResrefs = BuildNamedRareEliteEngineeringRecipes()
            .Values
            .Select(recipe => recipe.Resref)
            .ToArray();

        items.Keys.Should().BeEquivalentTo(engineeringResrefs);

        foreach (var resref in engineeringResrefs)
        {
            var item = items[resref];
            item.ApplyAction.Should().NotBeNull();
            item.ReducesItemChargeAction.Should().NotBeNull();
            item.ActivationAnimation.Should().Be(Animation.LoopingGetMid);
            item.RecastGroup.Should().Be(RecastGroup.FieldTool);
            item.RecastCooldown.Should().Be(300f);
        }
    }

    [Test]
    public void ViscaraNamedRareEliteEngineeringTools_UseNonElitePlayerFacingNames()
    {
        var root = FindRepositoryRoot();
        var craftedNames = new List<string>();

        foreach (var entry in GetNamedRareEliteEngineeringRecipeEntries())
        {
            using var craftedBlueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{entry.CraftedResref}.uti.json")));
            using var recipeBlueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{GetNamedRareEliteRecipeResref(entry)}.uti.json")));

            var craftedName = GetLocalizedName(craftedBlueprint.RootElement);
            craftedNames.Add(craftedName);

            var visibleText = string.Join(
                " ",
                craftedName,
                GetLocalizedString(craftedBlueprint.RootElement, "Description"),
                GetLocalizedString(craftedBlueprint.RootElement, "DescIdentified"),
                GetLocalizedName(recipeBlueprint.RootElement),
                GetLocalizedString(recipeBlueprint.RootElement, "Description"),
                GetLocalizedString(recipeBlueprint.RootElement, "DescIdentified"),
                entry.Recipe.ToString());

            foreach (var fragment in RareEliteSourceNameFragments)
            {
                visibleText.Should().NotContain(
                    fragment,
                    "engineering tools and recipes should use reusable non-elite names");
            }
        }

        craftedNames.Should().BeEquivalentTo(NamedRareEliteEngineeringCreativeNames);
    }

    [Test]
    public void ViscaraRareEliteLootItems_UseReusableNonEliteNaming()
    {
        var root = FindRepositoryRoot();
        var visibleTexts = new List<(string Source, string Text)>();

        void AddItemText(string resref)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{resref}.uti.json")));
            var json = blueprint.RootElement;

            visibleTexts.Add((
                resref,
                string.Join(
                    " ",
                    GetLocalizedName(json),
                    GetLocalizedString(json, "Description"),
                    GetLocalizedString(json, "DescIdentified"))));
        }

        void AddRecipeType(RecipeType recipeType)
        {
            visibleTexts.Add((recipeType.ToString(), recipeType.ToString()));
        }

        foreach (var item in OldScarUniqueRareRecipes.Concat(StormplumeUniqueRareRecipes))
        {
            AddItemText(item.RecipeResref);
            AddItemText(item.CraftedResref);
            AddRecipeType(item.RecipeType);
        }

        foreach (var item in GetNamedRareEliteRecipeEntries())
        {
            AddItemText(GetNamedRareEliteRecipeResref(item));
            AddItemText(item.CraftedResref);
            AddRecipeType(item.Recipe);
        }

        foreach (var component in RareEliteGuaranteedComponents)
        {
            AddItemText(component.Resref);
        }

        foreach (var (source, text) in visibleTexts)
        {
            foreach (var fragment in RareEliteSourceNameFragments)
            {
                text.Should().NotContain(
                    fragment,
                    $"{source} should use reusable item naming instead of the source elite name");
            }
        }
    }

    [Test]
    public void ViscaraNamedRareEliteComponents_AreNewCraftingComponents()
    {
        var root = FindRepositoryRoot();

        foreach (var spec in NamedRareEliteSpecs)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{spec.ComponentResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            GetLocalizedName(json).Should().NotBeNullOrWhiteSpace();
            json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(536);
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be(spec.ComponentResref);
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(spec.ComponentResref);
        }
    }

    [Test]
    public void ViscaraNamedRareEliteRecipes_AreDocumentedInRecipeBible()
    {
        var root = FindRepositoryRoot();
        using var archive = ZipFile.OpenRead(Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx"));
        var sharedStrings = ReadSharedStrings(archive);
        var smitheryWorksheet = ReadWorksheetByName(archive, "Smithery Recipes");
        var fabricationWorksheet = ReadWorksheetByName(archive, "Fabrication Recipes");
        var cookingWorksheet = ReadWorksheetByName(archive, "Cooking Recipes");
        var engineeringWorksheet = ReadWorksheetByName(archive, "Engineering Recipes");

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            var worksheet = entry.Skill switch
            {
                SkillType.Smithery => smitheryWorksheet,
                SkillType.Fabrication => fabricationWorksheet,
                SkillType.Agriculture => cookingWorksheet,
                SkillType.Engineering => engineeringWorksheet,
                _ => throw new InvalidOperationException($"Unsupported named rare elite recipe skill: {entry.Skill}")
            };
            var recipeResref = GetNamedRareEliteRecipeResref(entry);
            var craftedName = GetItemLocalizedName(root, entry.CraftedResref);
            var row = FindWorkbookRowByCellText(worksheet, sharedStrings, "B", recipeResref);

            GetWorkbookCellText(worksheet, sharedStrings, $"A{row}").Should().Be(entry.Skill.ToString());
            GetWorkbookCellText(worksheet, sharedStrings, $"B{row}").Should().Be(recipeResref);
            GetWorkbookCellText(worksheet, sharedStrings, $"D{row}").Should().Be(entry.Recipe.ToString());
            GetWorkbookCellText(worksheet, sharedStrings, $"E{row}").Should().Be(GetRecipeCategoryBibleName(entry));
            GetWorkbookCellText(worksheet, sharedStrings, $"G{row}").Should().Be(craftedName);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"H{row}").Should().Be(entry.Level);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"I{row}").Should().Be(1m);
            GetWorkbookCellText(worksheet, sharedStrings, $"J{row}").Should().Be(entry.CraftedResref);

            if (entry.Skill == SkillType.Smithery)
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be(GetSmitheryBlueprintCategory(entry.Category));
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be(RecipeEnhancementType.Armor.ToString());
                GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AC{row}").Should().Be(0m);
            }
            else if (entry.Skill == SkillType.Fabrication)
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be("Furniture Blueprints");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"K{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"L{row}").Should().Be(RecipeEnhancementType.Structure.ToString());
                GetWorkbookCellNumber(worksheet, sharedStrings, $"M{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"O{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"R{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"S{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AD{row}").Should().Be(0m);
            }
            else if (entry.Skill == SkillType.Agriculture)
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be("Cooking Recipes");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(GetRecipeBibleTier(entry.Level));
                GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be(RecipeEnhancementType.Food.ToString());
                GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AD{row}").Should().Be(0m);
            }
            else
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be("Tool Blueprints");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(GetRecipeBibleTier(entry.Level));
                GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be("N/A");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(0m);
                GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AC{row}").Should().Be(0m);
            }
        }
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
        spawnSource.Should().Contain("RemoveActiveSpawn(detail, creature);");
        spawnSource.Should().Contain("activeSpawns.RemoveAll(x => !GetIsObjectValid(x.SpawnObject));");
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
            .Where(spawn => !spawn.IsRare)
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

    private static NamedRareEliteRecipeSpec[] GetNamedRareEliteRecipeEntries()
    {
        return BuildNamedRareEliteRecipes()
            .Select(recipe => CreateNamedRareEliteRecipeSpec(recipe.Key, recipe.Value))
            .ToArray();
    }

    private static NamedRareEliteRecipeSpec[] GetNamedRareEliteEngineeringRecipeEntries()
    {
        return BuildNamedRareEliteEngineeringRecipes()
            .Select(recipe => CreateNamedRareEliteRecipeSpec(recipe.Key, recipe.Value))
            .ToArray();
    }

    private static NamedRareEliteRecipeSpec CreateNamedRareEliteRecipeSpec(RecipeType recipe, RecipeDetail detail)
    {
        var rareComponents = new HashSet<string>(StringComparer.Ordinal)
        {
            "ss_silk",
            "mb_shell",
            "gs_spine",
            "rk_claw",
            "se_eye",
            "rc_vine",
            "mv_core",
            "sr_token",
            "nv_pin",
            "tk_badge",
            "vs_mask",
            "hv_plate",
            "mg_totem",
            "vx_core",
            "ae_echo",
        };
        var rareComponent = detail.Components.Keys.Single(rareComponents.Contains);
        var regularComponents = detail.Components
            .Where(component => component.Key != rareComponent)
            .ToArray();

        regularComponents.Should().HaveCount(2, $"{recipe} should use two regular components and one rare component");

        return new NamedRareEliteRecipeSpec(
            recipe,
            detail.Skill,
            detail.EnhancementType,
            detail.Category,
            detail.Resref,
            detail.Level,
            regularComponents[0].Key,
            regularComponents[0].Value,
            regularComponents[1].Key,
            regularComponents[1].Value,
            rareComponent);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteRecipes()
    {
        return BuildNamedRareEliteSmitheryRecipes()
            .Concat(BuildNamedRareEliteFabricationRecipes())
            .Concat(BuildNamedRareEliteCookingRecipes())
            .Concat(BuildNamedRareEliteEngineeringRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteSmitheryRecipes()
    {
        return new GloamweaveRecipes()
            .BuildRecipes()
            .Concat(new ShellwardRecipes().BuildRecipes())
            .Concat(new StonebarbRecipes().BuildRecipes())
            .Concat(new EmberclawRecipes().BuildRecipes())
            .Concat(new PrismhideRecipes().BuildRecipes())
            .Concat(new MarshguardRecipes().BuildRecipes())
            .Concat(new FenbloomRecipes().BuildRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteFabricationRecipes()
    {
        return new AshmarkFurnitureRecipes()
            .BuildRecipes()
            .Concat(new GuttermarkFurnitureRecipes().BuildRecipes())
            .Concat(new TrailmarkFurnitureRecipes().BuildRecipes())
            .Concat(new StonewakeFurnitureRecipes().BuildRecipes())
            .Concat(new AegislineFurnitureRecipes().BuildRecipes())
            .Concat(new VeilcarvedFurnitureRecipes().BuildRecipes())
            .Concat(new SurgewakeFurnitureRecipes().BuildRecipes())
            .Concat(new ResonantFurnitureRecipes().BuildRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteCookingRecipes()
    {
        return new ForagedProvisionRecipes()
            .BuildRecipes()
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteEngineeringRecipes()
    {
        return new FieldToolRecipes()
            .BuildRecipes()
            .Where(pair => pair.Value.Components.Keys.Any(component =>
                component is "sr_token" or "nv_pin" or "tk_badge" or "vs_mask" or "hv_plate" or "mg_totem" or "vx_core"))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static string GetNamedRareEliteRecipeResref(NamedRareEliteRecipeSpec entry)
    {
        return "bp" + entry.CraftedResref.Replace("_", string.Empty);
    }

    private static void AssertRegisteredPropertyStructure(string resref)
    {
        resref.Should().StartWith("structure_");
        int.TryParse(resref["structure_".Length..], out var structureId).Should().BeTrue();
        Enum.IsDefined(typeof(StructureType), structureId).Should().BeTrue();

        var structure = (StructureType)structureId;
        var detail = structure.GetAttribute<StructureType, StructureAttribute>();
        detail.IsActive.Should().BeTrue();
        detail.Resref.Should().NotBeNullOrWhiteSpace();
        detail.LayoutType.Should().Be(PropertyLayoutType.Invalid);
    }

    private static IEnumerable<string> GetLocalStringsWithPrefix(JsonElement json, string prefix)
    {
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var entries))
        {
            yield break;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            var name = entry.GetProperty("Name").GetProperty("value").GetString();

            if (name != null && name.StartsWith(prefix, StringComparison.Ordinal))
            {
                yield return entry.GetProperty("Value").GetProperty("value").GetString()!;
            }
        }
    }

    private static string GetItemLocalizedName(DirectoryInfo root, string resref)
    {
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "uti",
            $"{resref}.uti.json")));

        return GetLocalizedName(blueprint.RootElement);
    }

    private static string GetLocalizedName(JsonElement json)
    {
        return json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
    }

    private static string GetLocalizedString(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property) ||
            !property.TryGetProperty("value", out var value))
        {
            return string.Empty;
        }

        return value.EnumerateObject()
            .Select(entry => entry.Value.GetString())
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
            ?? string.Empty;
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
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var entries))
        {
            return null;
        }

        foreach (var entry in entries.EnumerateArray())
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

    private static string GetRecipeCategoryBibleName(NamedRareEliteRecipeSpec entry)
    {
        return entry.Skill == SkillType.Fabrication && entry.CraftedResref.EndsWith("_locker", StringComparison.Ordinal)
            ? "MiscellaneousFurniture"
            : entry.Category.ToString();
    }

    private static decimal GetRecipeBibleTier(int level)
    {
        return level switch
        {
            <= 10 => 1m,
            <= 20 => 2m,
            <= 30 => 3m,
            <= 40 => 4m,
            _ => 5m
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

    private sealed record NamedRareEliteSpec(
        string Resref,
        string SpawnTableId,
        string RareLootTableId,
        string ComponentLootTableId,
        string ComponentResref,
        string ModuleFile);

    private sealed record NamedRareEliteRecipeSpec(
        RecipeType Recipe,
        SkillType Skill,
        RecipeEnhancementType EnhancementType,
        RecipeCategoryType Category,
        string CraftedResref,
        int Level,
        string PrimaryComponent,
        int PrimaryQuantity,
        string SecondaryComponent,
        int SecondaryQuantity,
        string RareComponent);

}
