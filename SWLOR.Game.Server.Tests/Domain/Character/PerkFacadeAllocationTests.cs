using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The perk reads run on every attack and stat rebuild, so a facade call that only reads a table must not allocate: the
/// facade resolves its instance from a cache and the instance reads its own fields. These tests count the bytes the
/// current thread allocates over many calls, with the real cache build over a small catalog. The reads that ask the
/// engine about a creature (a player's level, the force affinity of a character) are not here: the recording engine fake
/// allocates a record per call, so their cost says nothing about the service.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkFacadeAllocationTests
{
    private const int Calls = 20000;

    private PerkBench _bench = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new PerkBench();
        var plain = new PerkDetail { Type = PerkType.AlphaRhythm, Name = "A", Category = PerkCategoryType.General, IsActive = true };
        plain.StatBonuses.Add(new PerkStatBonus(StatType.AttackPercentAdjustment, 5));
        plain.PerkLevels[1] = new PerkLevel { GrantedFeats = { PerkBench.PassiveFeat } };
        plain.PerkLevels[2] = new PerkLevel { GrantedFeats = { PerkBench.ActiveFeatOne } };
        _bench.Definitions[PerkType.AlphaRhythm] = plain;
        _bench.Abilities[PerkBench.ActiveFeatOne] = new AbilityDetail
        {
            Name = "One",
            EffectiveLevelPerkType = PerkType.AlphaRhythm,
            RecastGroup = RecastGroup.Flash,
        };

        _container = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IPerkService>(_bench.Service)));
        _installed = _container.Install();
        _bench.Service.CacheData();
        _bench.Service.CachePerkFeatData();
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _bench.Dispose();
    }

    private static long Allocated(Action call)
    {
        for (var i = 0; i < 100; i++)
            call();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Calls; i++)
            call();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Test]
    public void TheCatalogReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Perk.GetPerkLevelTier(PerkType.AlphaRhythm, 1)).Should().Be(0);
        Allocated(() => sink += Perk.GetPerkLevelTier(PerkType.Invalid, 1)).Should().Be(0);
        Allocated(() => sink += Perk.GetPerkDetails(PerkType.AlphaRhythm).Name.Length).Should().Be(0);
        Allocated(() => sink += Perk.TryGetPerkDetails(PerkType.AlphaRhythm, out var detail) ? detail.PerkLevels.Count : 0).Should().Be(0);
        Allocated(() => sink += (int)Perk.GetPerkCategoryType(PerkType.AlphaRhythm)).Should().Be(0);
        Allocated(() => sink += Perk.GetPerkCategoryDetails(PerkCategoryType.General).Name.Length).Should().Be(0);
        Allocated(() => sink += Perk.GetCharacterType(CharacterType.Standard).Name.Length).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheFeatReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Perk.GetGrantedFeatsForPerkLevel(PerkType.AlphaRhythm, 2).Count).Should().Be(0);
        Allocated(() => sink += Perk.GetGrantedFeatsForPerkLevel(PerkType.AlphaRhythm, 40).Count).Should().Be(0);
        Allocated(() => sink += Perk.GetGrantedFeatsForPerkLevel(PerkType.Invalid, 1).Count).Should().Be(0);
        Allocated(() => sink += Perk.GetCurrentActiveAbilityFeats(PerkType.AlphaRhythm, 2).Count).Should().Be(0);
        Allocated(() => sink += Perk.GetAllActiveAbilityFeats(PerkType.AlphaRhythm).Count).Should().Be(0);
        Allocated(() => sink += (int)Perk.GetActiveAbilityRecastGroup(PerkType.AlphaRhythm)).Should().Be(0);
        Allocated(() => sink += Perk.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, PerkType.AlphaRhythm, 2) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Perk.IsCurrentActiveAbilityFeat(PerkBench.PassiveFeat, PerkType.AlphaRhythm, 1) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Perk.IsCurrentActiveAbilityFeat(PerkBench.ActiveFeatOne, PerkType.AlphaRhythm, 0) ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheStatReadsThatFindNoBonusAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Perk.GetStatBonus(PerkBench.Player, StatType.AccuracyPercentAdjustment)).Should().Be(0);
        Allocated(() => sink += Perk.GetStatSources(PerkBench.Player, StatType.AccuracyPercentAdjustment).Count).Should().Be(0);
        Allocated(() => sink += Perk.GetTargetedStatBonus(
            PerkBench.Player, PerkType.AlphaRhythm, StatType.AttackPercentAdjustment, StatType.AccuracyPercentAdjustment,
            StatType.EvasionPercentAdjustment)).Should().Be(0);
        Allocated(() => sink += Perk.GetTargetedStatBonus(
            PerkBench.Player, PerkType.Invalid, StatType.AttackPercentAdjustment, StatType.AccuracyPercentAdjustment,
            StatType.EvasionPercentAdjustment)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheForceAffinityReadsOfAPerkWithoutASideAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Perk.TryGetForceSideAffinity(PerkBench.Player, PerkType.AlphaRhythm, out var side) ? side : 0).Should().Be(0);
        Allocated(() => sink += Perk.TryGetForceSideAffinity(PerkBench.Player, PerkType.Invalid, out var side) ? side : 0).Should().Be(0);
        Allocated(() => sink += (int)Perk.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, PerkType.AlphaRhythm)).Should().Be(0);
        Allocated(() => sink += Perk.GetForceAffinityHitChanceAdjustment(PerkBench.Player, PerkType.AlphaRhythm)).Should().Be(0);
        Allocated(() => sink += Perk.ApplyForceAffinityMagnitude(PerkBench.Player, PerkType.AlphaRhythm, 0)).Should().Be(0);
        Allocated(() => sink += (int)Perk.ApplyForceAffinityMagnitude(PerkBench.Player, PerkType.AlphaRhythm, 0f)).Should().Be(0);
        Allocated(() => sink += Perk.GetPerkLevel(PerkBench.Player, PerkType.Invalid)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>The copies the old static class made on these reads are still made, as before the conversion.</summary>
    [Test]
    public void TheListingsStillCopyTheirTable()
    {
        var sink = 0;

        Allocated(() => sink += Perk.GetAllPerks().Count).Should().BeGreaterThan(0);
        Allocated(() => sink += Perk.GetAllPerkCategories().Count).Should().BeGreaterThan(0);
        Allocated(() => sink += Perk.GetAllActivePerks(PerkGroupType.Player).Count).Should().BeGreaterThan(0);
        // Enum.IsDefined boxes its argument, as it did in the old class.
        Allocated(() => sink += Perk.IsPerkInCategory(PerkType.AlphaRhythm, (int)PerkCategoryType.General) ? 1 : 0).Should().BeGreaterThan(0);
        GC.KeepAlive(sink);
    }
}
