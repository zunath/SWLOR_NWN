using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class NativeWeaponImpactQueueTests
{
    private const uint Attacker = 100;
    private const uint Target = 200;
    private const uint OtherTarget = 201;
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    [TearDown]
    public void Reset() => NativeWeaponImpactQueue.Clear();

    [Test]
    public void Take_RunsOnlyTheSignalledSlotsAgainstTheirTarget()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 3, Target, () => ran.Add("slot 3"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 4, Target, () => ran.Add("slot 4"), Now);

        foreach (var impact in NativeWeaponImpactQueue.Take(Attacker, Target, 3, 1, Now)) impact();
        ran.Should().Equal("slot 3");

        foreach (var impact in NativeWeaponImpactQueue.Take(Attacker, Target, 4, 1, Now)) impact();
        ran.Should().Equal("slot 3", "slot 4");
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Take_KeepsEveryActionOfOneRollInOrder()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(1), Now);
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(2), Now);

        foreach (var impact in NativeWeaponImpactQueue.Take(Attacker, Target, 0, 1, Now)) impact();

        ran.Should().Equal(1, 2);
    }

    [Test]
    public void Add_ReplacesASlotWhoseEarlierRollWasNeverSignalled()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 2, Target, () => ran.Add("interrupted"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 2, Target, () => ran.Add("current"), Now.AddSeconds(1));

        foreach (var impact in NativeWeaponImpactQueue.Take(Attacker, Target, 2, 1, Now.AddSeconds(2))) impact();

        ran.Should().Equal("current");
    }

    [Test]
    public void Take_DropsWorkForAnotherTargetAndExpiredWork()
    {
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, OtherTarget, () => Assert.Fail("other target"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => Assert.Fail("expired"), Now);

        NativeWeaponImpactQueue.Take(Attacker, Target, 0, 1, Now).Should().BeEmpty();
        NativeWeaponImpactQueue.Take(Attacker, Target, 1, 1, Now + NativeWeaponImpactQueue.Lifetime).Should().BeEmpty();
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Take_WrapsLikeTheEngineByteAttackCursor()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 255, Target, () => ran.Add(255), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 0, Target, () => ran.Add(0), Now);

        foreach (var impact in NativeWeaponImpactQueue.Take(Attacker, Target, 255, 2, Now)) impact();

        ran.Should().Equal(255, 0);
    }

    [Test]
    public void CreateScheduler_QueuesForTheRollsSlotUntilItsSignal()
    {
        var ran = false;
        NativeWeaponImpactQueue.CreateScheduler(Attacker, 7, Target)(() => ran = true);

        NativeWeaponImpactQueue.Take(Attacker, Target, 6, 1, DateTime.UtcNow).Should().BeEmpty();
        ran.Should().BeFalse();
        foreach (var impact in NativeWeaponImpactQueue.Take(Attacker, Target, 7, 1, DateTime.UtcNow)) impact();
        ran.Should().BeTrue();
    }
}
