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
/// The force affinity part over the perk catalog scripted by <see cref="PerkTestBase"/>: the clamped affinity, the side of a perk, the
/// multipliers and the hit chance, and the rounded magnitude.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkForceAffinityServiceTests : PerkTestBase
{
    // ---- force affinity ----

    [Test]
    public void GetForceAffinity_IsZeroForNpcsAndDmsAndClampedForPlayers()
    {
        _bench.StatAdjustments[(PerkBench.Npc, StatType.ForceAffinity)] = 8;
        _bench.StatAdjustments[(PerkBench.Dm, StatType.ForceAffinity)] = 8;
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 25;
        _bench.StatAdjustments[(PerkBench.Master, StatType.ForceAffinity)] = -25;

        _bench.Affinity.GetForceAffinity(PerkBench.Npc).Should().Be(0);
        _bench.Affinity.GetForceAffinity(PerkBench.Dm).Should().Be(0);
        _bench.Affinity.GetForceAffinity(PerkBench.Player).Should().Be(10);
        _bench.Affinity.GetForceAffinity(PerkBench.Master).Should().Be(-10);
    }

    [Test]
    public void TryGetForceSideAffinity_FollowsTheSideOfThePerk()
    {
        _bench.CacheData();
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 6;

        _bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, Light, out var light).Should().BeTrue();
        light.Should().Be(6);
        _bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, Dark, out var dark).Should().BeTrue();
        dark.Should().Be(-6);
        _bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, Plain, out var plain).Should().BeFalse();
        plain.Should().Be(0);
        _bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, PerkType.Invalid, out _).Should().BeFalse();
        _bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, PerkType.ThrowRock, out _).Should().BeFalse("not a cached perk");

        _bench.Definitions[Light].ForceAffinityType = (ForceAffinityType)7;
        _bench.CacheData();
        _bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, Light, out _).Should().BeFalse("an undefined side has no affinity");
    }

    [Test]
    public void ForceAffinityMultipliers_ScaleAndCap()
    {
        _bench.CacheData();
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 10;
        _bench.StatAdjustments[(PerkBench.Master, StatType.ForceAffinity)] = -7;

        _bench.Affinity.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, Light).Should().BeApproximately(1.5f, 0.0001f);
        _bench.Affinity.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, Dark).Should().BeApproximately(0.5f, 0.0001f);
        _bench.Affinity.GetForceAffinityMagnitudeMultiplier(PerkBench.Master, Light).Should().BeApproximately(0.65f, 0.0001f);
        _bench.Affinity.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, Plain).Should().Be(1f);
        _bench.Affinity.GetForceAffinityRestorativeMagnitudeMultiplier(PerkBench.Player, Light).Should().Be(1.25f);
        _bench.Affinity.GetForceAffinityRestorativeMagnitudeMultiplier(PerkBench.Master, Light).Should().BeApproximately(0.65f, 0.0001f);
        _bench.Affinity.GetForceAffinityHitChanceAdjustment(PerkBench.Player, Light).Should().Be(5);
        _bench.Affinity.GetForceAffinityHitChanceAdjustment(PerkBench.Master, Light).Should().Be(-4, "the half is floored");
        _bench.Affinity.GetForceAffinityHitChanceAdjustment(PerkBench.Player, Plain).Should().Be(0);
    }

    [Test]
    public void ApplyForceAffinityMagnitude_RoundsAwayFromZeroAndKeepsNonPositiveAmounts()
    {
        _bench.CacheData();
        _bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = 10;
        _bench.StatAdjustments[(PerkBench.Master, StatType.ForceAffinity)] = 1;

        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Light, 10).Should().Be(15);
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Dark, 1).Should().Be(1, "never below 1");
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Light, 0).Should().Be(0);
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Light, -4).Should().Be(-4);
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Plain, 9).Should().Be(9, "a neutral multiplier changes nothing");
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Master, Light, 10).Should().Be(11);
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Light, 10f).Should().BeApproximately(15f, 0.0001f);
        _bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, Light, -3f).Should().Be(-3f);
        _bench.Affinity.ApplyForceAffinityRestorativeMagnitude(PerkBench.Player, Light, 100).Should().Be(125);
        _bench.Affinity.ApplyForceAffinityRestorativeMagnitude(PerkBench.Player, Light, 0).Should().Be(0);
    }

    [Test]
    public void GetForceAffinity_ReadsTheCreatureStatAdjustmentOfTheStatService()
    {
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls.On(nameof(IStatService.GetStatAdjustment), _ => 4);
        var affinity = new PerkForceAffinityService(_bench.Catalog, stat);

        affinity.GetForceAffinity(PerkBench.Player).Should().Be(4);

        statCalls.Calls.Single().Arguments.Should().Equal(PerkBench.Player, StatType.ForceAffinity);
    }

    [Test]
    public void TryGetForceSideAffinity_ReadsThePerkFromTheCatalogOfTheStayPart()
    {
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls.On(nameof(IStatService.GetStatAdjustment), _ => -3);
        var affinity = new PerkForceAffinityService(_bench.Catalog, stat);

        affinity.TryGetForceSideAffinity(PerkBench.Player, Light, out _).Should().BeFalse("the catalog has not been cached");

        _bench.CacheData();
        affinity.TryGetForceSideAffinity(PerkBench.Player, Light, out var light).Should().BeTrue();
        affinity.TryGetForceSideAffinity(PerkBench.Player, Dark, out var dark).Should().BeTrue();

        light.Should().Be(-3);
        dark.Should().Be(3);
    }
}
