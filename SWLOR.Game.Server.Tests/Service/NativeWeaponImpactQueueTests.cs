using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class NativeWeaponImpactQueueTests
{
    private const uint Attacker = 100;
    private const uint Target = 200;
    private const uint OtherTarget = 201;
    private const int PhysicalIndex = 12;
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    [TearDown]
    public void Reset() => NativeWeaponImpactQueue.Clear();

    /// <summary>Each signalled slot's rolled damage, physical only, as the engine's attack data holds it.</summary>
    private static int[][] Slots(params int[] physical) => physical.Select(Damage).ToArray();

    /// <summary>A native damage-type array with only physical damage; untyped entries are 0 in attack data.</summary>
    private static int[] Damage(int physical)
    {
        var damage = new int[13];
        damage[PhysicalIndex] = physical;
        return damage;
    }

    /// <summary>The applied effect's damage integers; the engine stores -1 for types it does not deal.</summary>
    private static int[] EffectDamage(int physical)
    {
        var damage = Enumerable.Repeat(-1, 13).ToArray();
        damage[PhysicalIndex] = physical;
        return damage;
    }

    private static void RunApplied(ulong effectId, int physical, DateTime now, uint target = Target)
    {
        foreach (var impact in NativeWeaponImpactQueue.TakeApplied(Attacker, target, effectId, EffectDamage(physical), now))
            impact();
    }

    [Test]
    public void SplashWork_RunsWhenTheSignalsOwnDamageEffectIsApplied()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 3, Target, () => ran.Add("slot 3"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 4, Target, () => ran.Add("slot 4"), Now);

        NativeWeaponImpactQueue.Bind(Attacker, Target, 3, Slots(5), 1000, 1010, Now);
        NativeWeaponImpactQueue.HasQueued(Attacker, 4, 1).Should().BeTrue("slot 4 was not in this signal");
        RunApplied(999, 5, Now);
        RunApplied(1010, 5, Now);
        ran.Should().BeEmpty("only effects the signal created, IDs 1001 to 1009, are its impact");

        RunApplied(1005, 5, Now.AddMilliseconds(625));
        ran.Should().Equal("slot 3");
        NativeWeaponImpactQueue.HasSignalled.Should().BeFalse();
    }

    [Test]
    public void SeveralSlotsInOneSignal_EachRunWithTheirOwnEffect()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add("slot 0"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => ran.Add("slot 1"), Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5, 7), 50, 60, Now);

        // The later slot's effect may land first; it must release only its own work.
        RunApplied(54, 7, Now.AddMilliseconds(600));
        ran.Should().Equal("slot 1");
        RunApplied(52, 5, Now.AddMilliseconds(625));
        ran.Should().Equal("slot 1", "slot 0");
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void ASlotWithoutWork_KeepsItsEffectFromReleasingAnotherSlotsWork()
    {
        var ran = false;
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => ran = true, Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5, 5), 50, 60, Now);

        RunApplied(52, 5, Now.AddMilliseconds(625));
        ran.Should().BeFalse("slot 0's effect lands first and slot 0 queued no splash");
        RunApplied(54, 5, Now.AddMilliseconds(625));
        ran.Should().BeTrue();
    }

    [Test]
    public void AnEffectMatchingNoSlot_TakesTheSignalsFirstRemainingSlot()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(0), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => ran.Add(1), Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5, 7), 50, 60, Now);

        RunApplied(52, 9, Now);

        ran.Should().Equal(0);
    }

    [Test]
    public void Bind_KeepsEveryActionOfASlotInOrder()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(1), Now);
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(2), Now);

        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), 50, 60, Now);
        RunApplied(51, 5, Now);

        ran.Should().Equal(1, 2);
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Add_ReplacesASlotWhoseEarlierRollWasNeverSignalled()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 2, Target, () => ran.Add("interrupted"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 2, Target, () => ran.Add("current"), Now.AddSeconds(1));

        NativeWeaponImpactQueue.Bind(Attacker, Target, 2, Slots(5), 10, 20, Now.AddSeconds(2));
        RunApplied(15, 5, Now.AddSeconds(2));

        ran.Should().Equal("current");
    }

    [Test]
    public void ForgetSlot_KeepsAnInterruptedRollsWorkOffTheSlotsNextRoll()
    {
        // The interrupted swing queued a splash; the next roll in that slot queues nothing (failed proc or miss).
        NativeWeaponImpactQueue.Add(Attacker, 1, 3, Target, () => Assert.Fail("interrupted swing"), Now);
        var other = false;
        NativeWeaponImpactQueue.Add(Attacker, 1, 4, Target, () => other = true, Now);

        NativeWeaponImpactQueue.ForgetSlot(Attacker, 3);

        NativeWeaponImpactQueue.HasQueued(Attacker, 3, 1).Should().BeFalse();
        NativeWeaponImpactQueue.Bind(Attacker, Target, 3, Slots(5, 6), 10, 20, Now.AddSeconds(1));
        RunApplied(12, 5, Now.AddSeconds(1));
        other.Should().BeFalse();
        RunApplied(14, 6, Now.AddSeconds(1));
        other.Should().BeTrue();
    }

    [Test]
    public void LostEffects_NeverFireAnotherHitsWork()
    {
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => Assert.Fail("lost effect"), Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), 10, 20, Now);
        var later = false;
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => later = true, Now.AddSeconds(2));
        NativeWeaponImpactQueue.Bind(Attacker, Target, 1, Slots(5), 30, 40, Now.AddSeconds(2));

        // The first effect never lands; the later hit's effect must release only the later hit's work.
        RunApplied(35, 5, Now.AddSeconds(2.6));
        later.Should().BeTrue();
        NativeWeaponImpactQueue.TakeApplied(Attacker, Target, 15, EffectDamage(5), Now + NativeWeaponImpactQueue.Lifetime)
            .Should().BeEmpty();
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Bind_DropsWorkQueuedAgainstAnotherTargetAndExpiredWork()
    {
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, OtherTarget, () => Assert.Fail("other target"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => Assert.Fail("expired"), Now);

        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), 10, 20, Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 1, Slots(5), 30, 40, Now + NativeWeaponImpactQueue.Lifetime);
        RunApplied(15, 5, Now);
        RunApplied(35, 5, Now + NativeWeaponImpactQueue.Lifetime);

        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void SignalledWork_RunsOnlyForItsTarget()
    {
        var ran = false;
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran = true, Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), 10, 20, Now);

        RunApplied(15, 5, Now, OtherTarget);
        ran.Should().BeFalse();
        RunApplied(15, 5, Now);
        ran.Should().BeTrue();
    }

    [Test]
    public void Slots_WrapLikeTheEngineByteAttackCursor()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 255, Target, () => ran.Add(255), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 0, Target, () => ran.Add(0), Now);

        NativeWeaponImpactQueue.HasQueued(Attacker, 255, 2).Should().BeTrue();
        NativeWeaponImpactQueue.Bind(Attacker, Target, 255, Slots(5, 6), 10, 20, Now);
        RunApplied(11, 5, Now);
        RunApplied(12, 6, Now);

        ran.Should().Equal(255, 0);
    }

    [Test]
    public void CreateScheduler_QueuesForTheRollsSlot()
    {
        var ran = false;
        NativeWeaponImpactQueue.CreateScheduler(Attacker, 7, Target)(() => ran = true);

        NativeWeaponImpactQueue.HasQueued(Attacker, 6, 1).Should().BeFalse();
        NativeWeaponImpactQueue.HasQueued(Attacker, 7, 1).Should().BeTrue();
        NativeWeaponImpactQueue.Bind(Attacker, Target, 7, Slots(5), 10, 20, DateTime.UtcNow);
        RunApplied(12, 5, DateTime.UtcNow);
        ran.Should().BeTrue();
    }
}
