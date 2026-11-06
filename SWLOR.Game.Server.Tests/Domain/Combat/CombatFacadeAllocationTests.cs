using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat services run per attack and per damage event, so a facade call that only reads a table must not allocate:
/// the facade resolves its instance from a cache and the instance reads its own fields. These tests count the bytes the
/// current thread allocates over many calls.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatFacadeAllocationTests
{
    private const int Calls = 20000;

    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp() => _installed = _container.Install();

    [TearDown]
    public void TearDown() => _installed.Dispose();

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
    public void TheEnmityReadsAndRulesAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Enmity.HasEnmity(5) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.IsThreatSecured(100, 5) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.HasProximityEnmity(5, 100) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.HasNonProximityEnmity(5, 100) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateDamageEnmity(40, 25)).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateAdjustedEnmity(40, 25)).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateHealingEnmity(40, 10, 60)).Should().Be(0);
        Allocated(() => sink += Enmity.CalculateReducedEnmity(40, 25)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheCombatPointReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += CombatPoint.HasCombatPoints(5, 100) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += CombatPoint.GetTaggedCreatureCount(5)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheRecastNameAndVisibilityReadsAllocateNothing()
    {
        Recast.CacheRecastGroups();
        var sink = 0;

        Allocated(() => sink += Recast.IsRecastGroupVisible(RecastGroup.Flash) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Recast.GetRecastGroupName(RecastGroup.Flash).Length).Should().Be(0);
        Allocated(() => sink += Recast.GetRecastGroupDisplayName(RecastGroup.Flash).Length).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheWeaponDelayLookupAllocatesNothing()
    {
        var rifle = _container.Get<IItemService>().RifleBaseItemTypes[0];
        var sink = 0;

        Allocated(() => sink += WeaponDelay.GetWeaponDelay(rifle).HasValue ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }
}
