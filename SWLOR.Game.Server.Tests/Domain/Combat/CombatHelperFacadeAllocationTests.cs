using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The Combat helpers run per attack (the eligibility rules, the trait target-change hooks, the pair and grip
/// classification, the equipment stat copies), so a call through <c>GameServices</c> that only reads a table or a pure rule must not
/// allocate: the accessor resolves its instance from a per-service slot and the instance reads its own fields. These tests count the
/// bytes the current thread allocates over many calls to the engine-free hot members.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatHelperFacadeAllocationTests
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
    public void TheEligibilityRuleLookupAllocatesNothing()
    {
        var sink = 0;

        Allocated(() => sink += WeaponAttackEligibility.IsAllowed(BaseItem.Pistol, 1, 2, WeaponAttackPhase.Start) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += WeaponAttackEligibility.IsAllowed(BaseItem.PowerGlove, 1, 2, WeaponAttackPhase.Resolve) ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheTargetChangeHooksOfUntrackedActorsAllocateNothing()
    {
        OrdinaryAttackTraits.RecordTargetChange(5, 100);
        PairedRangedTraits.RecordTargetChange(5, 100);

        Allocated(() => OrdinaryAttackTraits.RecordTargetChange(5, 100)).Should().Be(0);
        Allocated(() => PairedRangedTraits.RecordTargetChange(5, 100)).Should().Be(0);
        Allocated(() => TwinPistolMastery.RecordTargetChange(5, 100)).Should().Be(0);
    }

    [Test]
    public void ThePairAndGripClassificationAllocatesNothing()
    {
        var sink = 0;

        Allocated(() => sink += TwinPistolEquipment.IsPair(BaseItem.Pistol, BaseItem.CompanionPistol) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += TwinPistolEquipment.IsPairItem(BaseItem.Pistol, true) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += TwinPistolEquipment.IsMirroredProperty(ItemPropertyType.DMG) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += PowerGloveEquipment.GetEquipmentStatCopies(BaseItem.PowerGlove)).Should().Be(0);
        GC.KeepAlive(sink);
    }
}
