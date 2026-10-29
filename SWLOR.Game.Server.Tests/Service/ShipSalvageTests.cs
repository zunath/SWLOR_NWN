using System.Collections;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The salvage update (plan section 12, W6a): yields into the ship's hold, gas clouds, wrecks and salvage, the new
/// materials, and the section 16 mining fixes.
/// </summary>
public class ShipSalvageTests
{
    // Storage-first routing

    [Test]
    public void Routing_TopsUpExistingStacksBeforeOpeningNewOnes()
    {
        var existing = new[] { 8, 10, 3 };
        var plan = ShipYieldRouting.Plan(existing, 10, 5, 6);

        plan.TopUps.Should().Equal((0, 10), (2, 7));
        plan.NewStacks.Should().BeEmpty();
        plan.Overflow.Should().Be(0);
        plan.Stored(existing).Should().Be(6);
    }

    [Test]
    public void Routing_OpensNewStacksWhileTheHoldHasFreeEntries()
    {
        var plan = ShipYieldRouting.Plan(new int[0], 10, 2, 25);

        plan.NewStacks.Should().Equal(10, 10);
        plan.Overflow.Should().Be(5, "the rest goes to the pilot only once the hold is full");
    }

    [Test]
    public void Routing_AFullHoldSendsEverythingToThePilot()
    {
        var existing = new[] { 10 };
        var plan = ShipYieldRouting.Plan(existing, 10, 0, 4);

        plan.TopUps.Should().BeEmpty();
        plan.NewStacks.Should().BeEmpty();
        plan.Overflow.Should().Be(4);
        plan.Stored(existing).Should().Be(0);
    }

    [Test]
    public void Routing_ItemsThatDoNotStackTakeOneEntryEach()
    {
        var plan = ShipYieldRouting.Plan(new[] { 1 }, 1, 3, 4);

        plan.TopUps.Should().BeEmpty();
        plan.NewStacks.Should().Equal(1, 1, 1);
        plan.Overflow.Should().Be(1);
    }

    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void Routing_OnlyTheOwnerOrAPlayerWithStorageAccessUsesTheHold(bool owner, bool access, bool expected)
    {
        ShipYieldRouting.CanUseHold(owner, access).Should().Be(expected);
    }

    [Test]
    public void EveryYield_GoesThroughTheShipHold()
    {
        var actions = ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        var salvage = ReadSource("Feature", "ShipModuleDefinition", "ShipSalvageActions.cs");

        foreach (var method in new[]
                 {
                     ExtractMethod(actions, "public static void MineAsteroid("),
                     ExtractMethod(actions, "public static void StripAsteroid("),
                     ExtractMethod(salvage, "public static void HarvestGasCloud("),
                     ExtractMethod(salvage, "public static void SalvageWreck(")
                 })
        {
            method.Should().Contain("ShipHold.DeliverYield(activator,");
            method.Should().NotContain("CreateItemOnObject", "yields reach the pilot only through the hold's overflow");
        }

        var hold = ReadSource("Service", "ShipHold.cs");
        hold.Should().Contain("ShipYieldRouting.Plan(");
        hold.Should().Contain("ShipYieldRouting.CanUseHold(");
    }

    // Section 16 mining fixes

    [Test]
    public void TheExtractionHold_IsReleasedWhenTheCycleEndsNotWhenItIsScheduled()
    {
        var actions = ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        var salvage = ReadSource("Feature", "ShipModuleDefinition", "ShipSalvageActions.cs");

        foreach (var method in new[]
                 {
                     ExtractMethod(actions, "public static void MineAsteroid("),
                     ExtractMethod(actions, "public static void StripAsteroid("),
                     ExtractMethod(salvage, "public static void HarvestGasCloud("),
                     ExtractMethod(salvage, "public static void SalvageWreck(")
                 })
        {
            var delay = method.IndexOf("DelayCommand(", StringComparison.Ordinal);
            method.IndexOf("HoldExtraction(target, activator)", StringComparison.Ordinal).Should().BeInRange(0, delay);
            method.IndexOf("ReleaseExtraction(target, activator)", StringComparison.Ordinal).Should().BeGreaterThan(delay);
            method.Should().NotContain("SetLocalBool(target, Mined, false)");
        }
    }

    [Test]
    public void TheStripMiner_ChecksTheShipAfterItsDelayAndAlwaysChecksTheTier()
    {
        var actions = ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        var strip = ExtractMethod(actions, "public static void StripAsteroid(");

        strip.Should().Contain("shipStatus == null");
        strip.Should().NotContain("Space.GetShipStatus(activator).Industrial");
        actions.Should().Contain("public static string StripMinerError(uint activator, uint target, int tier)");
    }

    [TestCase(0, 0, 1)]
    [TestCase(7, 0, 2)]
    [TestCase(0, 11, 2, Description = "11 / 6 is 1, not 1.83")]
    [TestCase(8, 12, 5)]
    public void TheExtractionBaseAmount_IsAWholeNumber(int industrial, int moduleBonus, int expected)
    {
        SpaceCombatMath.ExtractionBaseAmount(industrial, moduleBonus).Should().Be(expected);
    }

    [TestCase(1, 4, 4, Description = "1 ore uses 1 unit: four cycles")]
    [TestCase(3, 6, 9, Description = "3 ore uses 2 units: three cycles")]
    [TestCase(4, 3, 4, Description = "4 ore uses 3 units: one cycle")]
    [TestCase(2, 5, 6, Description = "2 ore uses 2 units: three cycles")]
    public void TheStripMiner_YieldsWhatTheLaserWouldOverTheWholeAsteroid(int perCycle, int units, int expected)
    {
        SpaceCombatMath.StripMinerYield(perCycle, units).Should().Be(expected);

        var laserTotal = 0;
        for (var remaining = units; remaining > 0; remaining -= SpaceCombatMath.MiningUnitsPerCycle(perCycle))
            laserTotal += perCycle;
        laserTotal.Should().Be(expected);
    }

    // Wrecks

    [Test]
    public void TheWreck_LastsFiveMinutesAndTheClaimSixtySeconds()
    {
        ShipSalvageRules.WreckSeconds.Should().Be(300);
        ShipSalvageRules.ClaimSeconds.Should().Be(60);
    }

    [TestCase(0, "killer", true)]
    [TestCase(0, "partner", true)]
    [TestCase(0, "stranger", false)]
    [TestCase(59, "stranger", false)]
    [TestCase(60, "stranger", true)]
    [TestCase(240, "stranger", true)]
    public void TheKillingParty_HasTheWreckToItselfForTheClaim(int secondsLater, string player, bool expected)
    {
        var destroyed = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        var claimants = new HashSet<string> { "killer", "partner" };

        ShipSalvageRules.CanSalvage(claimants, player, destroyed, destroyed.AddSeconds(secondsLater)).Should().Be(expected);
    }

    [Test]
    public void AWreckNoPlayerKilled_IsOpenToEveryone()
    {
        var now = DateTime.UtcNow;
        ShipSalvageRules.CanSalvage(new HashSet<string>(), "anyone", now, now).Should().BeTrue();
        ShipSalvageRules.ClaimSecondsLeft(now, now.AddSeconds(15)).Should().Be(45);
        ShipSalvageRules.ClaimSecondsLeft(now, now.AddSeconds(90)).Should().Be(0);
    }

    [TestCase(ShipSize.Capital, ShipSize.Small, false)]
    [TestCase(ShipSize.Capital, ShipSize.Medium, false)]
    [TestCase(ShipSize.Capital, ShipSize.Large, true)]
    [TestCase(ShipSize.Capital, ShipSize.Capital, true)]
    [TestCase(ShipSize.Large, ShipSize.Small, true)]
    [TestCase(ShipSize.Small, ShipSize.Small, true)]
    public void ACapitalWreck_NeedsALargeOrCapitalSalvager(ShipSize wreck, ShipSize salvager, bool expected)
    {
        ShipSalvageRules.HullCanSalvage(wreck, salvager).Should().Be(expected);
    }

    [TestCase("t1fighter", 3, 1)]
    [TestCase("t3capital", 35, 3)]
    [TestCase("t6carrier", 60, 6)]
    [TestCase("sithcap1", 65, 6)]
    [TestCase("mandocap2", 65, 6)]
    [TestCase("unknown", 25, 3)]
    public void TheWreckTier_IsTheFactionTier(string tag, int level, int expected)
    {
        ShipSalvageRules.WreckTier(tag, level).Should().Be(expected);
    }

    [Test]
    public void EveryPresetNpcShip_LeavesAWreckOfATierTheSalvagersReach()
    {
        foreach (var preset in RevampNpcShipCatalog.Ships)
        {
            var tier = ShipSalvageRules.WreckTier(preset.CreatureTag, preset.Level);
            tier.Should().BeInRange(1, 6, preset.CreatureTag);
            ShipSalvageRules.RequiredModuleTier(tier).Should().BeInRange(1, 5, preset.CreatureTag);
            ShipSalvageRules.ParseDifficulty(preset.Difficulty).ToString().Should().Be(preset.Difficulty, preset.CreatureTag);
        }
    }

    [TestCase(ShipSize.Small, WreckDifficulty.Normal, 3)]
    [TestCase(ShipSize.Medium, WreckDifficulty.Normal, 4)]
    [TestCase(ShipSize.Large, WreckDifficulty.Elite, 8)]
    [TestCase(ShipSize.Large, WreckDifficulty.Boss, 10)]
    [TestCase(ShipSize.Capital, WreckDifficulty.Boss, 14)]
    public void BiggerAndHarderWrecks_HoldMoreCycles(ShipSize size, WreckDifficulty difficulty, int expected)
    {
        ShipSalvageRules.WreckCycles(size, difficulty).Should().Be(expected);
    }

    [TestCase(1, "ship_scrap_1")]
    [TestCase(3, "ship_scrap_3")]
    [TestCase(5, "ship_scrap_5")]
    [TestCase(6, "ship_scrap_5")]
    public void TheScrap_IsTheWrecksTier(int tier, string expected)
    {
        ShipSalvageRules.ShipScrapResref(tier).Should().Be(expected);
        ShipSalvageRules.ScrapYield(tier, 3).Should().Equal(expected, expected, expected);
    }

    [TestCase(0, 0, 0, 1)]
    [TestCase(0, 0, 3, 4)]
    [TestCase(8, 12, 2, 7)]
    public void SalvageExpert_AddsItemsToEveryCycle(int industrial, int moduleBonus, int salvageExpert, int expected)
    {
        ShipSalvageRules.SalvageItemsPerCycle(industrial, moduleBonus, salvageExpert).Should().Be(expected);
    }

    [TestCase(0, 0, 0, 1)]
    [TestCase(0, 0, 2, 3)]
    [TestCase(4, 6, 1, 4)]
    public void GasSiphoning_AddsGasToEveryCycle(int industrial, int moduleBonus, int siphoning, int expected)
    {
        ShipSalvageRules.GasPerCycle(industrial, moduleBonus, siphoning).Should().Be(expected);
    }

    [TestCase(WreckDifficulty.Normal, 0, null)]
    [TestCase(WreckDifficulty.Elite, 10, "SPACE_ELITE_T4")]
    [TestCase(WreckDifficulty.Boss, 20, "SPACE_BOSS_T4")]
    public void RareFinds_ComeFromEliteAndBossWrecks(WreckDifficulty difficulty, int percent, string table)
    {
        ShipSalvageRules.RareFindPercent(difficulty).Should().Be(percent);
        ShipSalvageRules.RareFindTable(4, difficulty).Should().Be(table);
        ShipSalvageRules.RollsRareFind(difficulty, Math.Max(0, percent - 1)).Should().Be(percent > 0);
        ShipSalvageRules.RollsRareFind(difficulty, percent).Should().BeFalse();
    }

    [Test]
    public void TheRareFindTables_Exist()
    {
        var tables = new SpaceLootTableDefinition().BuildLootTables();
        for (var tier = 1; tier <= 6; tier++)
        {
            tables.Should().ContainKey(ShipSalvageRules.RareFindTable(tier, WreckDifficulty.Elite));
            tables.Should().ContainKey(ShipSalvageRules.RareFindTable(tier, WreckDifficulty.Boss));
        }
    }

    [TestCase(4, 0, 0)]
    [TestCase(5, 0, 4)]
    [TestCase(6, 0, 4)]
    [TestCase(5, 25, 25, Description = "the Debris Field zone sets its own chance")]
    [TestCase(3, 25, 0)]
    public void Aurodium_ComesFromTierFiveWrecksAtALowRate(int tier, int areaPercent, int expected)
    {
        ShipSalvageRules.AurodiumPercent(tier, areaPercent).Should().Be(expected);
        ShipSalvageRules.RollsAurodium(tier, areaPercent, 3).Should().Be(expected > 3);
    }

    [Test]
    public void EverySalvageCycle_RollsASalvagedComponentWithTraitHunter()
    {
        var salvage = ReadSource("Feature", "ShipModuleDefinition", "ShipSalvageActions.cs");

        var cycle = ExtractMethod(salvage, "public static void SalvageWreck(");
        cycle.Should().Contain("var components = RollSalvagedComponents(activator, wreck);");
        cycle.Should().Contain("ShipHold.DeliverYield(activator, yield, components);");
        cycle.Should().Contain("ShipSalvageRules.RareFindPercent(wreck.Difficulty) > 0 &&",
            "a Precision salvager never gives a normal wreck, which has no rare-find table, a rare find");
        ExtractMethod(salvage, "private static List<(string Resref, ShipComponentTraitType Trait)> RollSalvagedComponents(")
            .Should().Contain("Stat.GetStatAdjustment(activator, StatType.SpaceRareTraitChance)");
    }

    [Test]
    public void TheHold_StoresEachSalvagedComponentWithItsTrait()
    {
        var hold = ReadSource("Service", "ShipHold.cs");
        var store = ExtractMethod(hold, "private static string StoreComponent(");
        store.Should().Contain("ShipComponents.SetTraits(item, new[] { component.Trait });");
        store.Should().Contain("Quantity = 1");
        ExtractMethod(hold, "public static void DeliverYield(").Should().Contain("ShipComponents.SetTraits(item, new[] { trait });",
            "a component that does not fit the hold reaches the pilot with its trait");
    }

    [Test]
    public void LaunchedFighters_LeaveNoWreck()
    {
        ReadSource("Service", "SpaceNpcRoles.cs").Should().Contain("SetLocalBool(fighter, NoWreckVariable, true)");
        var wrecks = ReadSource("Service", "SpaceWrecks.cs");
        wrecks.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureDeathBefore)]");
        wrecks.Should().Contain("AssignCommand(wreck, () => DelayCommand(ShipSalvageRules.WreckSeconds, () => DestroyObject(wreck)))");
    }

    // Modules

    [TestCase(ShipModuleFamily.Salvager, "sm_salv", "iit_smsalvager", RecipeType.ShipSalvager00)]
    [TestCase(ShipModuleFamily.GasHarvester, "sm_gas", "iit_smgasharvest", RecipeType.ShipGasHarvester00)]
    public void TheNewFamilies_HaveTenGatheringRanks(ShipModuleFamily family, string prefix, string texture, RecipeType firstRecipe)
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == family).ToList();

        steps.Select(x => x.Rank).Should().Equal(0, 5, 10, 15, 20, 25, 30, 35, 40, 45);
        foreach (var step in steps)
        {
            step.Tag.Should().Be($"{prefix}_{step.Rank:00}");
            step.Skill.Should().Be(SkillType.Gathering);
            step.Row.Should().Be(ShipModulePowerType.High);
            step.Activation.Should().Be(ShipModuleActivation.Toggle);
            step.CycleSeconds.Should().Be(6.0);
            step.CapacitorCost.Should().Be(3);
            step.Capital.Should().BeFalse();
            step.Texture.Should().Be(texture);
            step.MiningTier.Should().Be(step.Rank / 10 + 1);
            step.RecipeType.Should().Be(firstRecipe + step.Rank / 5, "the new families' recipes follow the hulls'");
        }

        ((int)RecipeType.ShipSalvager00).Should().Be(7328);
        ((int)RecipeType.ShipGasHarvester45).Should().Be(7347);
    }

    [Test]
    public void TheNewModules_RunOnTheRapidExtractionCycle()
    {
        var modules = new RevampModuleDefinition().BuildShipModules();

        modules["sm_salv_00"].Type.Should().Be(ShipModuleType.Salvager);
        modules["sm_gas_45"].Type.Should().Be(ShipModuleType.GasHarvester);
        var source = ReadSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs");
        ExtractMethod(source, "private void GasHarvester(").Should().Contain("ShipModuleActions.MiningCycleSeconds(activator,");
        ExtractMethod(source, "private void Salvager(").Should().Contain("ShipModuleActions.MiningCycleSeconds(activator,");
        ExtractMethod(ReadSource("Feature", "ShipModuleDefinition", "ShipSalvageActions.cs"), "public static void HarvestGasCloud(")
            .Should().Contain("StatType.SpaceGasYieldBonus");
        ExtractMethod(ReadSource("Feature", "ShipModuleDefinition", "ShipSalvageActions.cs"), "public static void SalvageWreck(")
            .Should().Contain("StatType.SpaceSalvageYieldBonus");
    }

    [Test]
    public void GasCloudsAndWrecks_AreTargetableSpaceObjects()
    {
        var objects = new SalvageSpaceObjectDefinition().BuildSpaceObjects();

        objects.Should().ContainKeys(SalvageSpaceObjectDefinition.GasCloudTag, Space.WreckTag);
    }

    // Blueprints, loot and the refinery

    [TestCase("spc_gascloud_t3", "spc_gascloud", 479, 3)]
    [TestCase("spc_gascloud_t4", "spc_gascloud", 479, 4)]
    public void TheGasClouds_YieldTibannaAtTheirTier(string resref, string tag, int appearance, int tier)
    {
        var utp = ReadBlueprint("utp", resref);

        utp.GetProperty("Tag").GetProperty("value").GetString().Should().Be(tag);
        utp.GetProperty("Appearance").GetProperty("value").GetInt32().Should().Be(appearance);
        utp.GetProperty("OnClick").GetProperty("value").GetString().Should().Be("spc_target");
        var vars = Vars(utp);
        vars[ShipSalvageActions.GasCloudLootTableVariable].Should().Be("GAS_TIBANNA");
        vars[ShipSalvageActions.GasCloudTierVariable].Should().Be(tier.ToString());

        var table = new HarvestingLootTableDefinition().BuildLootTables()["GAS_TIBANNA"];
        table.Select(x => x.Resref).Should().Equal(ShipSalvageItems.TibannaGas);
    }

    [Test]
    public void TheGasCloudFog_IsAVisibleEmitterNoClickCanPick()
    {
        var utp = ReadBlueprint("utp", ShipSalvageActions.GasCloudVisualResref);

        utp.GetProperty("Appearance").GetProperty("value").GetInt32().Should().Be(4411, "\"VFX: Fog - Large, Heavy\" reads as a gas cloud");
        utp.GetProperty("Useable").GetProperty("value").GetInt32().Should().Be(0, "clicks go to the invisible cloud under it");
        utp.GetProperty("Static").GetProperty("value").GetInt32().Should().Be(0, "it is removed with its cloud");
    }

    [TestCase("spc_wreck", 20583)]
    [TestCase("spc_wreck_cap", 20494)]
    public void TheWrecks_UseExistingDebrisAppearances(string resref, int appearance)
    {
        var utp = ReadBlueprint("utp", resref);

        utp.GetProperty("Tag").GetProperty("value").GetString().Should().Be(Space.WreckTag);
        utp.GetProperty("Appearance").GetProperty("value").GetInt32().Should().Be(appearance);
        utp.GetProperty("OnClick").GetProperty("value").GetString().Should().Be("spc_target");
        resref.Should().Be(resref == "spc_wreck" ? Space.WreckResref : Space.CapitalWreckResref);
    }

    [TestCase("ore_durelium", "Durelium Ore", 222)]
    [TestCase("gas_tibanna", "Tibanna Gas", 223)]
    [TestCase("aurodium", "Aurodium", 224)]
    [TestCase("ship_scrap_1", "Ship Scrap (Tier 1)", 225)]
    [TestCase("ship_scrap_5", "Ship Scrap (Tier 5)", 225)]
    public void TheNewMaterials_AreStackableEssNineItems(string resref, string name, int model)
    {
        var uti = ReadBlueprint("uti", resref);

        uti.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(name);
        uti.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(535);
        uti.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(model);
        uti.GetProperty("Tag").GetProperty("value").GetString().Should().Be(resref);
    }

    [Test]
    public void Durelium_DropsOnlyFromTheTierOneAndTwoAsteroids()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();
        foreach (var (name, table) in tables.Where(x => x.Key.StartsWith("ASTEROID_")))
        {
            var lowTier = name.StartsWith("ASTEROID_TILARIUM") || name.StartsWith("ASTEROID_CURRIAN");
            var beltAsteroid = name.StartsWith("ASTEROID_DURELIUM_1") || name.StartsWith("ASTEROID_DURELIUM_2");
            var durelium = table.SingleOrDefault(x => x.Resref == "ore_durelium");
            if (!lowTier && !beltAsteroid)
            {
                durelium.Should().BeNull(name);
                continue;
            }

            durelium.Should().NotBeNull(name);
            durelium!.IsRare.Should().BeFalse();
            if (beltAsteroid)
                table.First().Resref.Should().Be("ore_durelium", $"{name}: the Viscara Asteroid Belt's main ore (W6c)");
            else
                durelium.Weight.Should().BeLessThan(table.First().Weight / 2, $"{name}: a modest share beside the ore");
        }
    }

    [Test]
    public void TheRefinery_TurnsEachTierOfScrapIntoThatTiersElectronics()
    {
        var field = typeof(RefineryViewModel).GetField("_ores", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ores = (IDictionary)field.GetValue(null)!;
        var expected = new[] { "elec_ruined", "elec_flawed", "elec_good", "elec_imperfect", "elec_high" };

        for (var tier = 1; tier <= 5; tier++)
        {
            var detail = ores[ShipSalvageItems.ShipScrap[tier - 1]]!;
            var type = detail.GetType();
            type.GetProperty("RefinedItemResref")!.GetValue(detail).Should().Be(expected[tier - 1]);
            type.GetProperty("RequiredLevel")!.GetValue(detail).Should().Be(tier);
            File.Exists(Path.Combine(Root(), "Module", "uti", expected[tier - 1] + ".uti.json")).Should().BeTrue();
        }
    }

    [Test]
    public void TheMidRimNebula_SpawnsGasCloudsBesideItsAsteroids()
    {
        var table = new SpaceResourceSpawnDefinition().BuildSpawnTables()["SPACE_RESOURCES_MIDRIM_NEBULA"];

        table.Spawns.Select(x => x.Resref).Should().Contain(new[] { "spc_gascloud_t3", "spc_gascloud_t4", "asteroid_gostian" });
        File.ReadAllText(Path.Combine(Root(), "Module", "git", "space_midrim1.git.json"))
            .Should().Contain("\"SPACE_RESOURCES_MIDRIM_NEBULA\"");
    }

    // Helpers

    private static Dictionary<string, string> Vars(JsonElement blueprint) =>
        blueprint.GetProperty("VarTable").GetProperty("value").EnumerateArray().ToDictionary(
            x => x.GetProperty("Name").GetProperty("value").GetString()!,
            x => x.GetProperty("Value").GetProperty("value").ToString());

    private static JsonElement ReadBlueprint(string folder, string resref)
    {
        var path = Path.Combine(Root(), "Module", folder, $"{resref}.{folder}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    // Zone hazards (W11o): standalone minefields and pre-placed wreck sites

    [Test]
    public void ZoneHazards_AreModuleWaypointsWithTheirTiersInTheTunedAreas()
    {
        SpaceHazardTuning.Placements.Where(x => x.WaypointTag == SpaceHazardTuning.MinefieldTag).Select(x => x.AreaResref)
            .Should().BeEquivalentTo(new[] { "space_viscbelt", "space_midrimneb", "space_korrdebris" }.Concat(WildSpaceTuning.Systems.Select(x => x.AreaResref)));
        SpaceHazardTuning.Placements.Where(x => x.WaypointTag == SpaceHazardTuning.WreckSiteTag).Select(x => x.AreaResref)
            .Should().Contain(WildSpaceTuning.KnownOrbits).And.Contain("space_korrdebris").And.Contain(WildSpaceTuning.Systems.Select(x => x.AreaResref));

        foreach (var group in SpaceHazardTuning.Placements.GroupBy(x => x.AreaResref))
        {
            using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "git", $"{group.Key}.git.json"), System.Text.Encoding.Latin1));
            var root = git.RootElement;
            var waypoints = root.GetProperty("WaypointList").GetProperty("value").EnumerateArray().ToList();
            var others = waypoints.Where(x => !x.GetProperty("Tag").GetProperty("value").GetString()!.StartsWith("SPACE_"))
                .Select(x => (X: x.GetProperty("XPosition").GetProperty("value").GetSingle(), Y: x.GetProperty("YPosition").GetProperty("value").GetSingle()))
                .Concat(root.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
                    .Select(x => (X: x.GetProperty("X").GetProperty("value").GetSingle(), Y: x.GetProperty("Y").GetProperty("value").GetSingle())))
                .ToList();
            foreach (var placement in group)
            {
                var placed = waypoints.Where(x => x.GetProperty("Tag").GetProperty("value").GetString() == placement.WaypointTag).ToList();
                placed.Should().HaveCount(placement.Count, $"{group.Key} {placement.WaypointTag}");
                foreach (var waypoint in placed)
                {
                    var tier = waypoint.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                        .Single(x => x.GetProperty("Name").GetProperty("value").GetString() == SpaceHazardTuning.TierVariable)
                        .GetProperty("Value").GetProperty("value").GetInt32();
                    tier.Should().Be(placement.Tier);
                    var x = waypoint.GetProperty("XPosition").GetProperty("value").GetSingle();
                    var y = waypoint.GetProperty("YPosition").GetProperty("value").GetSingle();
                    x.Should().BeInRange(30, 290);
                    y.Should().BeInRange(30, 290);
                    others.Min(o => Math.Sqrt(Math.Pow(o.X - x, 2) + Math.Pow(o.Y - y, 2))).Should().BeGreaterThan(40,
                        $"a hazard in {group.Key} stands clear of its lanes, docks, planets and decor");
                }
            }
        }

        var trade = TradeCatalog.Orbits.ToDictionary(x => x.AreaResref, x => x.Tier);
        foreach (var placement in SpaceHazardTuning.Placements.Where(x => trade.ContainsKey(x.AreaResref)))
            placement.Tier.Should().BeLessThanOrEqualTo(trade[placement.AreaResref], "a hazard never hits above its area's tier");
    }

    [Test]
    public void WildSpaceSystems_HoldMinefieldsAndWreckSitesAtTheWildSpaceContentTier()
    {
        foreach (var system in WildSpaceTuning.Systems)
        {
            var placements = SpaceHazardTuning.Placements.Where(x => x.AreaResref == system.AreaResref).ToList();
            placements.Single(x => x.WaypointTag == SpaceHazardTuning.MinefieldTag).Count.Should().Be(3, system.AreaResref);
            placements.Where(x => x.WaypointTag == SpaceHazardTuning.WreckSiteTag).Sum(x => x.Count).Should().BeGreaterThanOrEqualTo(2, system.AreaResref);
            placements.Should().OnlyContain(x => x.Tier == WildSpaceTuning.ContentTier, "Wild Space content is tier 5 (tier 5.5 in the area)");
        }

        SpaceHazardRules.MinelayerTag(5, true).Should().Be("rrminelayer", "a Wild Space mine hits as the Rift Raider Minelayer");
        SpaceHazardRules.MinelayerTag(5).Should().Be("t5minelayer");
        RevampNpcShipCatalog.Ships.Single(x => x.CreatureTag == SpaceHazardRules.MinelayerTag(5, true)).MineDmg
            .Should().BeGreaterThanOrEqualTo(RevampNpcShipCatalog.Ships.Single(x => x.CreatureTag == "t5minelayer").MineDmg);
        ReadSource("Service", "SpaceHazards.cs").Should().Contain("WildSpace = IsWildSpace(area)");
    }

    [Test]
    public void FieldMines_HitAsTheirTiersMinelayerAndReArm()
    {
        SpaceHazardRules.SiteTier(0).Should().Be(1);
        SpaceHazardRules.SiteTier(9).Should().Be(6);
        var presets = RevampNpcShipCatalog.Ships.ToDictionary(x => x.CreatureTag);
        for (var tier = 1; tier <= 6; tier++)
        {
            var minelayer = presets[SpaceHazardRules.MinelayerTag(tier)];
            minelayer.Role.Should().Be("Minelayer");
            minelayer.MineDmg.Should().BeGreaterThan(0);
        }

        var now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        SpaceHazardRules.MineRearmAt(now).Should().Be(now.AddSeconds(300));
        SpaceHazardRules.WreckRespawnAt(now).Should().Be(now.AddMinutes(30));
        SpaceHazardTuning.MinesPerField.Should().Be(4);

        var hazards = ReadSource("Service", "SpaceHazards.cs");
        hazards.Should().Contain("SpaceHazardRules.MinelayerTag(mine.Tier, mine.WildSpace)").And.Contain("Dmg = preset.MineDmg")
            .And.Contain("ShipShotBuilder.Build(torpedo,").And.Contain("CreateWreck(GetLocation(site.Waypoint), SpaceHazardTuning.WreckName");
        var roles = ReadSource("Service", "SpaceNpcRoles.cs");
        roles.Should().Contain("!mine.Field && (!GetIsObjectValid(mine.Owner)", "a field mine has no minelayer and no lifetime")
            .And.Contain("DetonateFieldMine(mine, victims[0])");
        SpaceHazardTuning.WreckDifficulty.Should().Be(WreckDifficulty.Normal);
        SpaceHazardTuning.WreckSize.Should().Be(ShipSize.Medium);
        ShipSalvageRules.WreckCycles(SpaceHazardTuning.WreckSize, SpaceHazardTuning.WreckDifficulty).Should().Be(4);
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);

        var depth = 0;
        for (var index = source.IndexOf('{', start); index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(start, index - start + 1);
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository.");
    }
}
