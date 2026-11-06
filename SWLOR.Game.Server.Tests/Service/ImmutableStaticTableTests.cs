using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The constant tables of the converted services are <c>static readonly</c> fields of immutable types (the no-static-state
/// rule accepts those). These tests pin what the tables hold and the order callers iterate them in, which must match the
/// <c>Dictionary</c> and array tables of master, because frozen collections do not promise insertion order.
/// </summary>
public class ImmutableStaticTableTests
{
    [Test]
    public void FishingRodTier_lists_the_rods_in_the_order_of_master()
    {
        FishingRodTier.All.Select(pair => (pair.Key, pair.Value)).Should().Equal(
            (FishingRodType.Bamboo, 0), (FishingRodType.Carbon, 0), (FishingRodType.Clothespole, 0),
            (FishingRodType.Composite, 10), (FishingRodType.Ebisu, 10), (FishingRodType.Fastwater, 10),
            (FishingRodType.GlassFiber, 20), (FishingRodType.Halcyon, 20), (FishingRodType.Judge, 20),
            (FishingRodType.MazeMonger, 30), (FishingRodType.Willow, 30), (FishingRodType.Yew, 30),
            (FishingRodType.Butterfly, 40), (FishingRodType.Tranquility, 40), (FishingRodType.LuShang, 40));
    }

    [Test]
    public void Style_stats_keep_the_order_the_tables_were_written_in()
    {
        CircuitStyles.StatsOf(CircuitStyle.Overclock).Keys.Should().Equal(
            StatType.CircuitExtraLoadPerBuildAction, StatType.CircuitLoadBoostPercent);
        CircuitStyles.StatsOf(CircuitStyle.SafeMode).Keys.Should().Equal(
            StatType.CircuitOverloadGrace, StatType.CircuitLoadBoostPercent);
        // A zero grants nothing, so a style lists only the stats it grants, in this order.
        StatType[] forgeOrder = { StatType.ForgeRangeShift, StatType.ForgePressOutputPercent, StatType.ForgeHoneOutputPercent };
        foreach (var style in ForgeStyles.All)
        {
            var keys = ForgeStyles.StatsOf(style).Keys.ToList();
            keys.Should().NotBeEmpty();
            keys.Should().Equal(forgeOrder.Where(keys.Contains));
        }

        GatheringStyles.StatsOf(GatheringStyle.StripMining).Keys.Should().Equal(
            StatType.ProspectingStrikeTimeReduction, StatType.ProspectingSeamWorkTimeReduction, StatType.ProspectingStrikeStabilityReduction);
        GatheringStyles.StatsOf(GatheringStyle.CarefulHands).Keys.Should().Equal(
            StatType.SearchChancePoints, StatType.SearchDigRareWeight);
    }

    [Test]
    public void Kitchen_resrefs_keep_the_order_of_the_table()
    {
        var resrefs = KitchenComponentTable.Resrefs.ToList();

        resrefs.Take(4).Should().Equal(KitchenComponentTable.Water, "ae_echo", "alzabi", "amphi_blood");
        resrefs.TakeLast(3).Should().Equal("yotbean", "zafmlug_bass", "zitah");
        resrefs.Should().OnlyHaveUniqueItems();
        resrefs.Should().OnlyContain(resref => KitchenComponentTable.Contains(resref));
        KitchenComponentTable.Contains("ALZABI").Should().BeTrue("the lookup ignores case, as master's does");
    }

    [Test]
    public void Public_lists_keep_their_order_and_contents()
    {
        IncubationProjection.MainPurityStats.Should().Equal(
            IncubationStatType.AttackPurity, IncubationStatType.AccuracyPurity, IncubationStatType.EvasionPurity,
            IncubationStatType.LearningPurity, IncubationStatType.PhysicalDefensePurity, IncubationStatType.ForceDefensePurity,
            IncubationStatType.XPPenalty);
        IncubationProjection.ResistancePurityStats.Should().HaveCount(8).And.StartWith(IncubationStatType.FireResistancePurity);
        IncubationProjection.GetResistanceType(IncubationStatType.IceResistancePurity).Should().Be(ResistanceType.Ice);
        IncubationProjection.GetStatName(IncubationStatType.XPPenalty).Should().Be("XP penalty");

        CircuitStyles.All.Should().Equal(CircuitStyle.Overclock, CircuitStyle.SafeMode);
        ForgeStyles.All.Should().Equal(ForgeStyle.HotForging, ForgeStyle.ColdWorking);
        GatheringStyles.All.Should().Equal(GatheringStyle.StripMining, GatheringStyle.CarefulHands);
        FabricatorHeads.AllParts.Should().Equal(SubAssembly.Frame, SubAssembly.Shell, SubAssembly.Fittings);
        FabricatorHeads.AllHeads.Should().Equal(FabricatorHead.Cutter, FabricatorHead.Extruder, FabricatorHead.Bonder);
        ProspectingGems.Resrefs.Should().Equal("jade", "agate", "citrine", "ruby", "emerald");
        ForgeRules.ActionIds.Should().Equal(
            ForgeActionIds.RampPlasma, ForgeActionIds.HydroPress, ForgeActionIds.VibroHone, ForgeActionIds.Rebond, ForgeActionIds.CryoQuench);
        CircuitPerkLines.Overclocker.First().Should().Be(PerkType.ConductiveWiring);
        CircuitPerkLines.Technician.Last().Should().Be(PerkType.CircuitFailsafe);
        ForgePerkLines.Forgemaster.Should().HaveCount(9);
        ForgePerkLines.Artisan.Should().HaveCount(9);
        GatheringPerkLines.Prospector.Should().HaveCount(6);
        GatheringPerkLines.TreasureSeeker.Should().HaveCount(8);
    }

    [Test]
    public void Gathering_loadouts_list_no_perks_first_and_then_the_perked_ones_in_order()
    {
        GatheringLoadout.All.First().Should().BeSameAs(GatheringLoadout.None);
        GatheringLoadout.All.Skip(1).Should().Equal(GatheringLoadout.Perked);
        GatheringLoadout.Perked.Select(loadout => loadout.Name).Should().Equal(
            "Prospector, no style", "Prospector, Strip Mining", "Treasure Seeker, no style", "Treasure Seeker, Careful Hands",
            "Both lines, no style", "Both lines, Strip Mining", "Both lines, Careful Hands");
    }

    [Test]
    public void Rank_milestones_read_the_same_steps()
    {
        CraftRankMilestones.RefineryManagementLevelForRank(0).Should().Be(1);
        CraftRankMilestones.RefineryManagementLevelForRank(45).Should().Be(6);
        CraftRankMilestones.ProspectingYieldTierForRank(29).Should().Be(1);
        CraftRankMilestones.ProspectingYieldTierForRank(45).Should().Be(3);
        CraftRankMilestones.ProspectingYieldMilestoneRanks.Should().Equal(15, 30, 45);
        CraftRankMilestones.ProspectingSurveyMilestoneRanks.Should().Equal(25, 50);
        CraftRankMilestones.PromisingChanceForRank(35).Should().Be(39);
        CraftRankMilestones.SearchSchedule().Select(step => step.Rank).Should().Equal(0, 10, 20, 30, 40);
    }

    [Test]
    public void Shared_values_are_one_object_as_on_master()
    {
        ReferenceEquals(CraftSettlement.None, CraftSettlement.None).Should().BeTrue();
        ReferenceEquals(GuiColor.Red, GuiColor.Red).Should().BeTrue();
        ReferenceEquals(KitchenComponentTable.Unlisted, KitchenComponentTable.Unlisted).Should().BeTrue();
        ReferenceEquals(CircuitPerkStats.None, CircuitPerkStats.None).Should().BeTrue();
        ReferenceEquals(CircuitStyles.All, CircuitStyles.All).Should().BeTrue();
        GuiColor.Red.Should().Be(new GuiColor(255, 0, 0));
        GuiColor.STMColor.Should().Be(new GuiColor(0, 139, 0));
    }

    /// <summary>
    /// A table read allocates nothing. Before the tables were fields, each of these reads built a new collection (the
    /// Kitchen table alone is 183 entries).
    /// </summary>
    [Test]
    public void Reading_a_table_allocates_nothing()
    {
        for (var warmUp = 0; warmUp < 10; warmUp++)
            ReadTables();

        var before = GC.GetAllocatedBytesForCurrentThread();
        const int reads = 1000;
        for (var read = 0; read < reads; read++)
            ReadTables();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        TestContext.Out.WriteLine($"AllocatedBytesPerRead={allocated / (double)reads}");
        allocated.Should().Be(0);
    }

    private static int ReadTables()
    {
        return KitchenComponentTable.Get("alzabi").Kind.GetHashCode()
            + CircuitComponentGrades.GradeOf("jade")
            + CraftRankMilestones.ProspectingYieldTierForRank(30)
            + FishingRodTier.RequiredRank(FishingRodType.Yew);
    }
}
