using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The stat bonus part over the perk catalog scripted by <see cref="PerkTestBase"/>: the tables its boot handler fills, the bonus and
/// source reads of every kind of creature and the targeted bonus.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkStatBonusServiceTests : PerkTestBase
{
    // ---- stat bonuses ----

    [Test]
    public void GetStatBonus_AddsPerkAndLevelBonusesOfThePlayersLevel()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(8);
        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.AccuracyPercentAdjustment).Should().Be(0);
    }

    [Test]
    public void GetStatBonus_SkipsLevelsTheCreatureDoesNotHave()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Plain] = 1);

        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(5);
        _bench.StatBonus.GetStatBonus(PerkBench.Master, StatType.AttackPercentAdjustment).Should().Be(0, "no record, no level");
    }

    [Test]
    public void GetStatBonus_ReadsTheVariableOfACreatureThatIsNotAPlayer()
    {
        _bench.CacheData();

        _bench.StatBonus.GetStatBonus(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().Be(0);

        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 2;
        _bench.StatBonus.GetStatBonus(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().Be(8);

        _bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)Plain}")] = 3;
        _bench.StatBonus.GetStatBonus(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().Be(5, "level 3 has no level bonus");
    }

    [Test]
    public void GetStatBonus_UsesThePerkLevelOfDroidsAndPossessedPlayers()
    {
        _bench.CacheData();
        _bench.DroidPerks[Plain] = 2;

        _bench.StatBonus.GetStatBonus(PerkBench.DroidCreature, StatType.AttackPercentAdjustment).Should().Be(8);

        _bench.LocalInts[(PerkBench.PossessedDm, $"PERK_LEVEL_{(int)Plain}")] = 1;
        _bench.StatBonus.GetStatBonus(PerkBench.PossessedDm, StatType.AttackPercentAdjustment).Should().Be(5,
            "a possessed DM reads its variable, not a record");
    }

    [Test]
    public void GetStatSources_NamesEachContributingPerk()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        var sources = _bench.StatBonus.GetStatSources(PerkBench.Player, StatType.AttackPercentAdjustment);

        sources.Should().ContainSingle();
        sources[0].Key.Should().Be($"perk:{(int)Plain}");
        sources[0][StatType.AttackPercentAdjustment].Should().Be(8);

        _bench.StatBonus.GetStatSources(PerkBench.Player, StatType.AccuracyPercentAdjustment).Should().BeEmpty();
        _bench.StatBonus.GetStatSources(PerkBench.Player, StatType.Invalid).Should().BeEmpty();
        _bench.StatBonus.GetStatSources(PerkBench.Npc, StatType.AttackPercentAdjustment).Should().BeEmpty("the level is 0");
    }

    [Test]
    public void GetTargetedStatBonus_AppliesTheAdjustmentOnlyToTheNamedPerk()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Targeted] = 1);

        int Bonus(PerkType target) => _bench.StatBonus.GetTargetedStatBonus(
            PerkBench.Player,
            target,
            StatType.DefensePercentAdjustment,
            StatType.PhysicalDefensePercentAdjustment,
            StatType.AccuracyPercentAdjustment);

        Bonus(Marked).Should().Be(7);
        Bonus(Plain).Should().Be(0);
        Bonus(PerkType.Invalid).Should().Be(0);
        _bench.StatBonus.GetTargetedStatBonus(
            PerkBench.Player, Marked, StatType.DefensePercentAdjustment, StatType.PhysicalDefensePercentAdjustment,
            StatType.Invalid).Should().Be(0);
        _bench.StatBonus.GetTargetedStatBonus(
            PerkBench.Master, Marked, StatType.DefensePercentAdjustment, StatType.PhysicalDefensePercentAdjustment,
            StatType.AccuracyPercentAdjustment).Should().Be(0, "the master has no level of the perk");
    }

    [Test]
    public void GetTargetedStatBonus_MatchesTheSecondaryStatToo()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Targeted] = 1);

        _bench.StatBonus.GetTargetedStatBonus(
            PerkBench.Player, Marked, StatType.PhysicalDefensePercentAdjustment, StatType.DefensePercentAdjustment,
            StatType.AccuracyPercentAdjustment).Should().Be(7);
    }

    // ---- the boot handler ----

    [Test]
    public void CachePerkStatBonuses_FillsTheTablesTheCatalogCacheLeavesEmpty()
    {
        _bench.Catalog.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);

        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(0,
            "the tables are filled by their own boot handler, not by the catalog cache");
        _bench.StatBonus.GetStatSources(PerkBench.Player, StatType.AttackPercentAdjustment).Should().BeEmpty();

        _bench.StatBonus.CachePerkStatBonuses();

        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(8);
        _bench.StatBonus.GetStatSources(PerkBench.Player, StatType.AttackPercentAdjustment).Should().ContainSingle();
    }

    [Test]
    public void CachePerkStatBonuses_ReadsEveryPerkInTheOrderTheCatalogBuiltThem()
    {
        var order = new List<PerkType>();
        _bench.Definitions[Plain].StatBonuses.Add(new PerkStatBonus(
            StatType.DefensePercentAdjustment, new PerkStatBonusCalculation(_ => { order.Add(Plain); return 0; })));
        _bench.Definitions[Targeted].PerkLevels[1].StatBonuses.Add(new PerkStatBonus(
            StatType.EvasionPercentAdjustment, new PerkStatBonusCalculation(_ => { order.Add(Targeted); return 0; })));
        _bench.CacheData();
        AddPlayer(player =>
        {
            player.Perks[Plain] = 1;
            player.Perks[Targeted] = 1;
        });

        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.DefensePercentAdjustment);
        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.EvasionPercentAdjustment);

        order.Should().Equal(Plain, Targeted);
    }

    [Test]
    public void CachePerkStatBonuses_OfANewInstanceFillsItsOwnTablesAndLeavesTheOldOnesAlone()
    {
        _bench.CacheData();
        AddPlayer(player => player.Perks[Plain] = 2);
        var reloaded = new TestPerkStatBonusService(_bench, _bench.Catalog, _bench.BeastMastery);

        reloaded.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(0, "a new generation starts with empty tables");
        reloaded.CachePerkStatBonuses();

        reloaded.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(8);
        _bench.StatBonus.GetStatBonus(PerkBench.Player, StatType.AttackPercentAdjustment).Should().Be(8);
    }

    [Test]
    public void CachePerkStatBonuses_IsARerunBootHandlerOnTheCacheEvent()
    {
        var method = typeof(PerkStatBonusService).GetMethod(nameof(PerkStatBonusService.CachePerkStatBonuses))!;

        method.GetCustomAttributes(typeof(BootPhaseAttribute), false).Cast<BootPhaseAttribute>().Single().Reload
            .Should().Be(BootReload.Rerun, "a hot reload builds new tables and nothing else would fill them");
        method.GetCustomAttributes(typeof(NWNEventHandler), false).Cast<NWNEventHandler>().Single().Script
            .Should().Be(ScriptName.OnModuleCacheBefore);
    }
}
