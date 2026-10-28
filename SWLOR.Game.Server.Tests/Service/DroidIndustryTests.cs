using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Industrial droids (plan section 27): the industrial parts and their bills, the Industry sum and the mixing rule,
/// the protocols and their AI slots, droid bays, the hooks later slices read, the follow limit and the recipe IDs.
/// </summary>
public class DroidIndustryTests
{
    private const uint Ship = 0x01000301;

    private static readonly string[] Slots = { "cpu", "hd", "bd", "ar", "lg" };
    private static readonly int[] SlotSubtypes = { 1, 2, 3, 4, 5 };
    private static readonly RecipeCategoryType[] SlotCategories =
    {
        RecipeCategoryType.DroidCPU, RecipeCategoryType.DroidHead, RecipeCategoryType.DroidBody,
        RecipeCategoryType.DroidArms, RecipeCategoryType.DroidLegs,
    };

    // Combat parts of each slot at a tier, for comparisons (the same blueprints droids.py copies).
    private static string CombatResref(int slot, int tier) =>
        new[] { $"d_hm_cpu{tier}_m", $"d_hd_mpv{tier}", $"d_bd_mp{tier}", $"d_ar_mpv{tier}", $"d_lg_mpv{tier}" }[slot];

    private static string IndustrialResref(int slot, int tier) => $"d_in_{Slots[slot]}{tier}";

    private static readonly int[] WeaponAndArmorStats =
        Enum.GetValues<DroidStatSubType>().Where(x => (int)x >= 115 || x == DroidStatSubType.Armor).Select(x => (int)x).ToArray();

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    private static JArray Properties(string resref) =>
        (JArray)JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")))["PropertiesList"]!["value"]!;

    private static Dictionary<int, int> Stats(string resref) =>
        Properties(resref).Where(p => (int)p["PropertyName"]!["value"]! == (int)ItemPropertyType.DroidStat)
            .GroupBy(p => (int)p["Subtype"]!["value"]!)
            .ToDictionary(g => g.Key, g => g.Sum(p => (int)p["CostValue"]!["value"]!));

    private static Dictionary<int, int> DroidStats(IEnumerable<string> parts)
    {
        var total = new Dictionary<int, int>();
        foreach (var (stat, value) in parts.SelectMany(Stats))
            if (stat != (int)DroidStatSubType.Tier)
                total[stat] = total.GetValueOrDefault(stat) + value;
        return total;
    }

    private static int Attributes(Dictionary<int, int> stats) =>
        Enumerable.Range((int)DroidStatSubType.MGT, 6).Sum(stats.GetValueOrDefault);

    private static int Industry(Dictionary<int, int> stats) => stats.GetValueOrDefault((int)DroidStatSubType.Industry);

    // Parts and recipes

    [Test]
    public void EverySlot_HasAnIndustrialPartAtEveryTier_WithARecipeAndABlueprint()
    {
        var recipes = new DroidIndustrialPartRecipes().BuildRecipes().Values.ToDictionary(x => x.Resref);
        recipes.Should().HaveCount(25);

        for (var slot = 0; slot < Slots.Length; slot++)
        for (var tier = 1; tier <= 5; tier++)
        {
            var resref = IndustrialResref(slot, tier);
            recipes.Should().ContainKey(resref);
            var recipe = recipes[resref];
            recipe.Category.Should().Be(SlotCategories[slot], resref);
            recipe.Level.Should().Be(10 * tier - 5, resref);
            recipe.Skill.Should().Be(Server.Service.SkillService.SkillType.Engineering, resref);

            var properties = Properties(resref);
            properties.Single(p => (int)p["PropertyName"]!["value"]! == (int)ItemPropertyType.DroidPart)["Subtype"]!["value"]!
                .Value<int>().Should().Be(SlotSubtypes[slot], resref);
            var stats = Stats(resref);
            stats[(int)DroidStatSubType.Tier].Should().Be(tier, resref);
            stats[(int)DroidStatSubType.Industry].Should().Be(DroidIndustryRules.PartIndustry(tier), resref);
            stats.Keys.Should().NotContain(WeaponAndArmorStats, "industrial parts give no combat skill bonuses: " + resref);
            stats.GetValueOrDefault((int)DroidStatSubType.AISlots).Should()
                .Be(Stats(CombatResref(slot, tier)).GetValueOrDefault((int)DroidStatSubType.AISlots),
                    "protocols compete with combat instructions for the same slots: " + resref);
        }
    }

    [Test]
    public void IndustrialParts_CostAboutHalfTheMaterialsOfACombatPartAtTheSameTier()
    {
        var intermediates = new EngineeringComponentRecipes().BuildRecipes().Values
            .GroupBy(x => x.Resref).ToDictionary(x => x.Key, x => x.First().Components);
        int Raw(Dictionary<string, int> bill) =>
            bill.Sum(x => intermediates.TryGetValue(x.Key, out var inner) ? x.Value * inner.Values.Sum() : x.Value);

        var combat = typeof(IRecipeListDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IRecipeListDefinition).IsAssignableFrom(t) &&
                        new[] { "DroidCPURecipes", "DroidHeadRecipes", "DroidBodyRecipes", "DroidArmsRecipes", "DroidLegsRecipes" }.Contains(t.Name))
            .SelectMany(t => ((IRecipeListDefinition)Activator.CreateInstance(t)!).BuildRecipes().Values)
            .ToDictionary(x => x.Resref);
        var industrial = new DroidIndustrialPartRecipes().BuildRecipes().Values.ToDictionary(x => x.Resref);

        for (var slot = 0; slot < Slots.Length; slot++)
        for (var tier = 1; tier <= 5; tier++)
        {
            var share = Raw(industrial[IndustrialResref(slot, tier)].Components) /
                        (double)Raw(combat[CombatResref(slot, tier)].Components);
            share.Should().BeInRange(0.4, 0.5, $"{IndustrialResref(slot, tier)} against {CombatResref(slot, tier)}");
        }
    }

    [Test]
    public void IndustrialRecipeTypes_TakeTheBlockFrom7700_InOrder_AfterEveryOtherRecipe()
    {
        var industrial = new DroidIndustrialPartRecipes().BuildRecipes().Keys
            .Concat(new DroidInstructionRecipes().BuildRecipes().Keys.Where(x => x.ToString().EndsWith("Protocol1") || x.ToString().EndsWith("Protocol2")))
            .Select(x => (int)x).OrderBy(x => x).ToList();
        industrial.Should().Equal(Enumerable.Range(DroidIndustryTuning.FirstRecipeType, 33));
        DroidIndustryTuning.FirstRecipeType.Should().Be(7700);
        DroidIndustryTuning.LastRecipeType.Should().Be(7732);

        var values = Enum.GetValues<RecipeType>().Select(x => (int)x).ToList();
        values.Should().OnlyHaveUniqueItems("no two recipes may share an ID");
        values.Where(x => x >= DroidIndustryTuning.FirstRecipeType && x <= DroidIndustryTuning.LastRecipeType)
            .Should().BeEquivalentTo(industrial, "the block belongs to the industrial droids alone");
        ((int)RecipeType.InstructionProspectingProtocol2).Should().Be(7732);
        ((int)RecipeType.IndustrialLogisticsCPU1).Should().Be(7700);
    }

    // Industry and mixing

    [Test]
    public void Industry_IsTheSumOfTheParts_AndMixingGivesAMiddlingDroidAtBoth()
    {
        var industrialParts = Enumerable.Range(0, 5).Select(slot => IndustrialResref(slot, 5)).ToList();
        var combatParts = Enumerable.Range(0, 5).Select(slot => CombatResref(slot, 5)).ToList();
        var mixedParts = combatParts.Take(2).Concat(industrialParts.Skip(2)).ToList();

        var industrial = DroidStats(industrialParts);
        var combat = DroidStats(combatParts);
        var mixed = DroidStats(mixedParts);

        Industry(industrial).Should().Be(DroidIndustryTuning.MaximumIndustry);
        Industry(combat).Should().Be(0);
        Industry(mixed).Should().Be(60);
        DroidIndustryRules.SumIndustry(new[] { 20, 20, -5 }).Should().Be(40);

        // Middling at the station...
        var mixedCrew = DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, Industry(mixed));
        mixedCrew.Should().BeGreaterThan(DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, Industry(combat)));
        mixedCrew.Should().BeLessThan(DroidCrewRules.BaseEffectiveness(ShipCrewSeat.Engineer, Industry(industrial)));

        // ...and in a fight.
        Attributes(mixed).Should().BeGreaterThan(Attributes(industrial)).And.BeLessThan(Attributes(combat));
        mixed.GetValueOrDefault((int)DroidStatSubType.HP).Should().Be(DroidIndustryTuning.CombatCpuHP, "it kept a combat CPU");
    }

    [Test]
    public void ACombatDroidBuiltFromIndustrialParts_IsWeakerThanACombatPartDroid()
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var industrial = DroidStats(Enumerable.Range(0, 5).Select(slot => IndustrialResref(slot, tier)));
            var combat = DroidStats(Enumerable.Range(0, 5).Select(slot => CombatResref(slot, tier)));

            industrial[(int)DroidStatSubType.HP].Should().BeLessThan(combat[(int)DroidStatSubType.HP], $"tier {tier}");
            Attributes(industrial).Should().BeLessThan(Attributes(combat), $"tier {tier}");
            WeaponAndArmorStats.Sum(industrial.GetValueOrDefault).Should().Be(0, $"tier {tier}");
            WeaponAndArmorStats.Sum(combat.GetValueOrDefault).Should().BeGreaterThan(0, $"tier {tier}");
            industrial[(int)DroidStatSubType.AISlots].Should().Be(combat[(int)DroidStatSubType.AISlots], $"tier {tier}");
        }
    }

    [Test]
    public void IndustryStat_IsANewDroidStatWithAHakLabel()
    {
        ((int)DroidStatSubType.Industry).Should().Be(25);
        var row = File.ReadAllLines(Path.Combine(Root, "SWLOR_Haks", "sw_2da", "iprp_droidstat.2da"))
            .Select(line => line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries))
            .Single(x => x.Length >= 3 && x[0] == "25");
        row[2].Should().Be("Industry");
    }

    // Protocols

    [Test]
    public void Protocols_AreDroidOnlyInstructionsThatCompeteForAISlots()
    {
        using var perks = new DroidPerkCacheScope();
        var protocols = new[] { PerkType.EfficiencyProtocol, PerkType.DamageControlProtocol, PerkType.SurveyProtocol, PerkType.ProspectingProtocol };
        protocols.Select(x => (int)x).Should().Equal(900, 901, 902, 903);
        DroidIndustryTuning.ProtocolValues.Keys.Should().BeEquivalentTo(protocols);

        foreach (var protocol in protocols)
        {
            var detail = Server.Service.Perk.GetPerkDetails(protocol);
            detail.GroupType.Should().Be(PerkGroupType.Droid, "players and beasts never buy a protocol");
            detail.Category.Should().Be(PerkCategoryType.DroidProtocols);
            detail.PerkLevels.Keys.Should().Equal(1, 2);
            detail.PerkLevels[1].DroidAISlots.Should().Be(1);
            detail.PerkLevels[2].DroidAISlots.Should().Be(2);
            detail.PerkLevels.Values.Should().OnlyContain(x => x.GrantedFeats.Count == 0 && x.Price == 0);
            Server.Service.Perk.GetPerkLevelTier(protocol, 1).Should().Be(1);
            Server.Service.Perk.GetPerkLevelTier(protocol, 2).Should().Be(3);
            DroidIndustryRules.IsProtocol(protocol).Should().BeTrue();
        }

        DroidIndustryRules.IsProtocol(PerkType.Provoke).Should().BeFalse();

        // A tier-3 droid with 3 slots runs a rank-II protocol (2 slots) or a rank-II combat instruction (2 slots), not both.
        var both = new[] { new DroidPerk(PerkType.SurveyProtocol, 2), new DroidPerk(PerkType.Provoke, 2) };
        DroidInstructions.SelectActive(both, 3, 3).Should().ContainSingle();
        DroidInstructions.SelectActive(both, 3, 4).Should().HaveCount(2);
        DroidInstructions.SelectActive(new[] { new DroidPerk(PerkType.SurveyProtocol, 2) }, 2, 10)
            .Should().BeEmpty("rank II needs a tier 3 droid");

        var active = new Dictionary<PerkType, int> { [PerkType.SurveyProtocol] = 2, [PerkType.Provoke] = 2 };
        DroidIndustryRules.SelectProtocols(active).Should().BeEquivalentTo(new Dictionary<PerkType, int> { [PerkType.SurveyProtocol] = 2 });
    }

    [Test]
    public void ProtocolDiscs_CarryTheirProtocolAndRank_AndHaveRecipes()
    {
        var recipes = new DroidInstructionRecipes().BuildRecipes().Values.ToDictionary(x => x.Resref);
        var discs = new (string Resref, PerkType Perk, int Rank)[]
        {
            ("id_efficiency1", PerkType.EfficiencyProtocol, 1), ("id_efficiency2", PerkType.EfficiencyProtocol, 2),
            ("id_dmgcontrol1", PerkType.DamageControlProtocol, 1), ("id_dmgcontrol2", PerkType.DamageControlProtocol, 2),
            ("id_survey1", PerkType.SurveyProtocol, 1), ("id_survey2", PerkType.SurveyProtocol, 2),
            ("id_prospect1", PerkType.ProspectingProtocol, 1), ("id_prospect2", PerkType.ProspectingProtocol, 2),
        };

        foreach (var (resref, perk, rank) in discs)
        {
            var property = Properties(resref).Single();
            ((int)property["PropertyName"]!["value"]!).Should().Be((int)ItemPropertyType.DroidInstruction, resref);
            ((int)property["Subtype"]!["value"]!).Should().Be((int)perk, resref);
            ((int)property["CostValue"]!["value"]!).Should().Be(rank, resref);
            recipes.Should().ContainKey(resref);
            recipes[resref].Level.Should().Be(rank == 1 ? 10 : 30, resref);
            recipes[resref].Category.Should().Be(RecipeCategoryType.DroidInstruction, resref);
        }
    }

    [Test]
    public void ProtocolValues_ReadOnlyTheProtocolsADroidRuns()
    {
        var protocols = new Dictionary<PerkType, int>
        {
            [PerkType.SurveyProtocol] = 1, [PerkType.EfficiencyProtocol] = 2, [PerkType.DamageControlProtocol] = 2,
            [PerkType.ProspectingProtocol] = 1,
        };
        DroidIndustryRules.GetProtocolValue(protocols, PerkType.SurveyProtocol).Should().Be(20);
        DroidIndustryRules.GetProtocolValue(protocols, PerkType.EfficiencyProtocol).Should().Be(40);
        DroidIndustryRules.GetProtocolValue(protocols, PerkType.DamageControlProtocol).Should().Be(10);
        DroidIndustryRules.GetProtocolValue(protocols, PerkType.ProspectingProtocol).Should().Be(5);
        DroidIndustryRules.GetProtocolValue(protocols, PerkType.Provoke).Should().Be(0);
        DroidIndustryRules.GetProtocolValue(null, PerkType.SurveyProtocol).Should().Be(0);
        DroidIndustryRules.GetProtocolValue(new Dictionary<PerkType, int> { [PerkType.SurveyProtocol] = 9 }, PerkType.SurveyProtocol)
            .Should().Be(0, "an unknown rank adds nothing");
    }

    [Test]
    public void SurveyProtocol_SwitchesOnAnOffLauncherOnlyWhenThereIsSomethingToScan()
    {
        var survey = new Dictionary<PerkType, int> { [PerkType.SurveyProtocol] = 1 };
        DroidCrewRules.RunsProbeLauncher(survey, launcherOn: false, somethingToScan: true).Should().BeTrue();
        DroidCrewRules.RunsProbeLauncher(survey, launcherOn: true, somethingToScan: true).Should().BeFalse();
        DroidCrewRules.RunsProbeLauncher(survey, launcherOn: false, somethingToScan: false).Should().BeFalse();
        DroidCrewRules.RunsProbeLauncher(new Dictionary<PerkType, int>(), false, true).Should().BeFalse("no Survey Protocol");
        DroidCrewRules.GetTask(ShipModuleType.ProbeLauncher, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.Survey);
    }

    [Test]
    public void DamageControlProtocol_AddsToRepairsOnlyAtTheEngineerStation()
    {
        var protocols = new Dictionary<PerkType, int> { [PerkType.DamageControlProtocol] = 2 };
        SpaceCrew.GetDroidRepairBonusPercent(new ShipDroidCrew { Seat = ShipCrewSeat.Engineer, Protocols = protocols }).Should().Be(10);
        SpaceCrew.GetDroidRepairBonusPercent(new ShipDroidCrew { Seat = ShipCrewSeat.Gunner, Protocols = protocols }).Should().Be(0);
        SpaceCrew.GetDroidRepairBonusPercent(new ShipDroidCrew { Seat = ShipCrewSeat.Engineer }).Should().Be(0);
        SpaceCrew.GetDroidRepairBonusPercent(null).Should().Be(0);
    }

    // Droid bays

    [Test]
    public void MiningBargeIndustrialAndLogisticsHulls_HaveADroidBay()
    {
        var withBay = new[] { ShipRole.MiningBarge, ShipRole.Industrial, ShipRole.Logistics };
        foreach (var role in Enum.GetValues<ShipRole>())
            DroidIndustryRules.HasDroidBay(role).Should().Be(withBay.Contains(role), role.ToString());

        DroidIndustryRules.BayAddsYield(ShipRole.MiningBarge).Should().BeTrue();
        DroidIndustryRules.BayAddsYield(ShipRole.Industrial).Should().BeTrue();
        DroidIndustryRules.BayAddsYield(ShipRole.Logistics).Should().BeFalse();
        DroidIndustryRules.BayAddsRepairs(ShipRole.Logistics).Should().BeTrue();
        DroidIndustryRules.BayAddsRepairs(ShipRole.Industrial).Should().BeFalse();

        var ships = new RevampPlayerShipDefinition().BuildShips().Values.ToList();
        ships.Where(x => x.HasDroidBay).Select(x => x.Role).Distinct().Should().BeEquivalentTo(withBay);
        ships.Where(x => withBay.Contains(x.Role)).Should().OnlyContain(x => x.HasDroidBay);
    }

    [Test]
    public void ADroidBay_IsADroidOnlyPost()
    {
        try
        {
            ShipCrewSeats.GetSeatName(ShipCrewSeat.DroidBay).Should().Be("Droid Bay");
            ShipCrewSeats.GetPostName(ShipCrewSeat.DroidBay).Should().Be("the droid bay");
            ShipCrewSeats.GetPostName(ShipCrewSeat.Engineer).Should().Be("the Engineer station");
            foreach (var size in Enum.GetValues<ShipSize>())
                ShipCrewSeats.GetSeats(size).Should().NotContain(ShipCrewSeat.DroidBay, "players never board the bay");

            SpaceCrew.AssignSeat(Ship, ShipCrewSeat.DroidBay, 0x01000311).Should().BeFalse();
            SpaceCrew.AssignDroidSeat(Ship, new ShipDroidCrew { Seat = ShipCrewSeat.DroidBay, Industry = 100 }).Should().BeTrue();
            SpaceCrew.AssignDroidSeat(Ship, new ShipDroidCrew { Seat = ShipCrewSeat.DroidBay }).Should().BeFalse("the bay holds one droid");
            SpaceCrew.GetSeatDroid(Ship, ShipCrewSeat.DroidBay).Industry.Should().Be(100);
        }
        finally
        {
            SpaceCrew.ClearDroidCrew(Ship);
        }
    }

    [TestCase(0, 0, 0)]
    [TestCase(20, 0, 5)]
    [TestCase(100, 0, 25)]
    [TestCase(100, 20, 30)]
    [TestCase(100, 40, 35)]
    public void BayYield_IsAQuarterPercentPerIndustry_RaisedByEfficiency(int industry, int efficiency, int expected)
    {
        DroidIndustryRules.BayYieldPercent(industry, efficiency).Should().Be(expected);
    }

    [Test]
    public void BayRepairs_AreATenthPercentPerIndustry_PlusDamageControl()
    {
        DroidIndustryRules.BayRepairPercent(100, 0).Should().Be(10);
        DroidIndustryRules.BayRepairPercent(100, 10).Should().Be(20);
        DroidIndustryRules.BayRepairPercent(0, 5).Should().Be(5);
    }

    [TestCase(3, 25, 4)]
    [TestCase(2, 25, 3)]
    [TestCase(1, 25, 1)]
    [TestCase(4, 35, 5)]
    [TestCase(5, 0, 5)]
    [TestCase(0, 35, 0)]
    public void BayYield_RoundsHalfUp_AndNeverLowersACycle(int amount, int percent, int expected)
    {
        DroidIndustryRules.ApplyPercent(amount, percent).Should().Be(expected);
    }

    // Hooks and the follow limit

    [Test]
    public void Hooks_ForHoldingsStationBuildingsAndSalvageTeams_ReadIndustry()
    {
        DroidIndustryRules.HoldingOutputPercent(100, 0).Should().Be(50);
        DroidIndustryRules.HoldingOutputPercent(100, 40).Should().Be(70);
        DroidIndustryRules.DrillOutputPercent(60, 0).Should().Be(30);
        DroidIndustryRules.ShipyardBuildSpeedPercent(100).Should().Be(30);
        DroidIndustryRules.SalvageTeamSpeedPercent(100).Should().Be(30);
        DroidIndustryRules.SalvageTeamYieldPercent(100).Should().Be(25);
        DroidIndustryRules.HoldingOutputPercent(0, 40).Should().Be(0, "a combat droid adds nothing");
    }

    [Test]
    public void WorkingDroids_NeverTakeTheFollowSlot()
    {
        DroidCrewRules.CountsTowardFollowLimit(spawned: false, posted: true).Should().BeFalse("a station or bay droid is posted, never spawned");
        DroidCrewRules.CountsTowardFollowLimit(spawned: true, posted: false).Should().BeTrue();
        DroidCrewRules.GetActivationError(thisDroidPosted: false, followers: 0).Should().BeEmpty("posted droids leave the slot free");
        DroidCrewRules.GetActivationError(thisDroidPosted: true, followers: 0).Should().Contain("posted aboard a ship");
        DroidCrewRules.FollowLimit.Should().Be(1);
    }
}
