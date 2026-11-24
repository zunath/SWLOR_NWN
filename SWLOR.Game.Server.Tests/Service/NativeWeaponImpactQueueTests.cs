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

    /// <summary>A scheduled combat damage effect; the engine stores -1 for damage types it does not deal.</summary>
    private static (ulong Id, int[] Damage) Effect(ulong id, int physical)
    {
        var damage = Enumerable.Repeat(-1, 13).ToArray();
        damage[PhysicalIndex] = physical;
        return (id, damage);
    }

    private static void RunApplied(ulong effectId, DateTime now, uint target = Target)
    {
        foreach (var impact in NativeWeaponImpactQueue.TakeApplied(Attacker, target, effectId, now)) impact();
    }

    [Test]
    public void SplashWork_RunsWhenItsOwnDamageEffectIsApplied()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 3, Target, () => ran.Add("slot 3"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 4, Target, () => ran.Add("slot 4"), Now);

        NativeWeaponImpactQueue.Bind(Attacker, Target, 3, Slots(5), new[] { Effect(1005, 5) }, Now).Should().BeEmpty();
        NativeWeaponImpactQueue.HasQueued(Attacker, 4, 1).Should().BeTrue("slot 4 was not in this signal");
        RunApplied(1004, Now);
        RunApplied(1006, Now);
        ran.Should().BeEmpty("only the slot's own effect is its impact");

        RunApplied(1005, Now.AddMilliseconds(625));
        ran.Should().Equal("slot 3");
        NativeWeaponImpactQueue.HasBound.Should().BeFalse();
    }

    [Test]
    public void EqualDamageSlots_KeepTheirOwnEffectsWhenTheyLandOutOfOrder()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add("slot 0"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => ran.Add("slot 1"), Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5, 5), new[] { Effect(52, 5), Effect(54, 5) }, Now);

        RunApplied(54, Now.AddMilliseconds(600));
        ran.Should().Equal("slot 1");
        RunApplied(52, Now.AddMilliseconds(625));
        ran.Should().Equal("slot 1", "slot 0");
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void ALostEarlierEffect_NeitherFiresNorStealsTheLaterSlotsSplash()
    {
        var ran = false;
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => ran = true, Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5, 5), new[] { Effect(52, 5), Effect(54, 5) }, Now);

        // Slot 0 queued no splash and its effect never lands; slot 1's effect releases slot 1's splash.
        RunApplied(54, Now.AddMilliseconds(625));
        ran.Should().BeTrue();
    }

    [Test]
    public void OnlyTheEarlierEqualDamageSlotProcced_SplashWaitsForThatSlotsEffect()
    {
        var ran = false;
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran = true, Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5, 5), new[] { Effect(52, 5), Effect(54, 5) }, Now);

        RunApplied(54, Now.AddMilliseconds(600));
        ran.Should().BeFalse("slot 1's effect is not slot 0's impact");
        RunApplied(52, Now.AddMilliseconds(625));
        ran.Should().BeTrue();
    }

    [Test]
    public void AssignEffectsToSlots_SkipsSlotsWithoutAnEffectByDamage()
    {
        // Slot 1 missed (no damage, no effect); the second effect belongs to slot 2.
        NativeWeaponImpactQueue.AssignEffectsToSlots(Slots(5, 0, 7), new[] { Effect(54, 7), Effect(52, 5) })
            .Should().Equal(52UL, null, 54UL);
        // Effects that match no remaining slot by damage still go to the next slot in order.
        NativeWeaponImpactQueue.AssignEffectsToSlots(Slots(5, 7), new[] { Effect(52, 9), Effect(54, 9) })
            .Should().Equal(52UL, 54UL);
        NativeWeaponImpactQueue.AssignEffectsToSlots(Slots(5), Array.Empty<(ulong, int[])>())
            .Should().Equal(new ulong?[] { null });
    }

    [Test]
    public void Bind_ReturnsWorkWhoseSlotGotNoEffect()
    {
        var ran = false;
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran = true, Now);

        var unbound = NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), Array.Empty<(ulong, int[])>(), Now);

        unbound.Should().HaveCount(1);
        unbound[0]();
        ran.Should().BeTrue();
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Bind_KeepsEveryActionOfASlotInOrder()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(1), Now);
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran.Add(2), Now);

        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), new[] { Effect(51, 5) }, Now);
        RunApplied(51, Now);

        ran.Should().Equal(1, 2);
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Add_ReplacesASlotWhoseEarlierRollWasNeverSignalled()
    {
        var ran = new List<string>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 2, Target, () => ran.Add("interrupted"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 2, Target, () => ran.Add("current"), Now.AddSeconds(1));

        NativeWeaponImpactQueue.Bind(Attacker, Target, 2, Slots(5), new[] { Effect(15, 5) }, Now.AddSeconds(2));
        RunApplied(15, Now.AddSeconds(2));

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
        NativeWeaponImpactQueue.Bind(Attacker, Target, 3, Slots(5, 6), new[] { Effect(12, 5), Effect(14, 6) },
            Now.AddSeconds(1)).Should().BeEmpty();
        RunApplied(12, Now.AddSeconds(1));
        other.Should().BeFalse();
        RunApplied(14, Now.AddSeconds(1));
        other.Should().BeTrue();
    }

    [Test]
    public void LostEffects_ExpireWithoutFiringAnotherHitsWork()
    {
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => Assert.Fail("lost effect"), Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), new[] { Effect(15, 5) }, Now);
        var later = false;
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => later = true, Now.AddSeconds(2));
        NativeWeaponImpactQueue.Bind(Attacker, Target, 1, Slots(5), new[] { Effect(35, 5) }, Now.AddSeconds(2));

        RunApplied(35, Now.AddSeconds(2.6));
        later.Should().BeTrue();
        NativeWeaponImpactQueue.TakeApplied(Attacker, Target, 15, Now + NativeWeaponImpactQueue.Lifetime).Should().BeEmpty();
        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Bind_DropsWorkQueuedAgainstAnotherTargetAndExpiredWork()
    {
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, OtherTarget, () => Assert.Fail("other target"), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 1, Target, () => Assert.Fail("expired"), Now);

        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), new[] { Effect(15, 5) }, Now).Should().BeEmpty();
        NativeWeaponImpactQueue.Bind(Attacker, Target, 1, Slots(5), new[] { Effect(35, 5) }, Now + NativeWeaponImpactQueue.Lifetime)
            .Should().BeEmpty();
        RunApplied(15, Now);
        RunApplied(35, Now + NativeWeaponImpactQueue.Lifetime);

        NativeWeaponImpactQueue.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void BoundWork_RunsOnlyForItsTarget()
    {
        var ran = false;
        NativeWeaponImpactQueue.Add(Attacker, 1, 0, Target, () => ran = true, Now);
        NativeWeaponImpactQueue.Bind(Attacker, Target, 0, Slots(5), new[] { Effect(15, 5) }, Now);

        RunApplied(15, Now, OtherTarget);
        ran.Should().BeFalse();
        RunApplied(15, Now);
        ran.Should().BeTrue();
    }

    [Test]
    public void Slots_WrapLikeTheEngineByteAttackCursor()
    {
        var ran = new List<int>();
        NativeWeaponImpactQueue.Add(Attacker, 1, 255, Target, () => ran.Add(255), Now);
        NativeWeaponImpactQueue.Add(Attacker, 2, 0, Target, () => ran.Add(0), Now);

        NativeWeaponImpactQueue.HasQueued(Attacker, 255, 2).Should().BeTrue();
        NativeWeaponImpactQueue.Bind(Attacker, Target, 255, Slots(5, 6), new[] { Effect(11, 5), Effect(12, 6) }, Now);
        RunApplied(11, Now);
        RunApplied(12, Now);

        ran.Should().Equal(255, 0);
    }

    [Test]
    public void CreateScheduler_QueuesForTheRollsSlot()
    {
        var ran = false;
        NativeWeaponImpactQueue.CreateScheduler(Attacker, 7, Target)(() => ran = true);

        NativeWeaponImpactQueue.HasQueued(Attacker, 6, 1).Should().BeFalse();
        NativeWeaponImpactQueue.HasQueued(Attacker, 7, 1).Should().BeTrue();
        NativeWeaponImpactQueue.Bind(Attacker, Target, 7, Slots(5), new[] { Effect(12, 5) }, DateTime.UtcNow);
        RunApplied(12, DateTime.UtcNow);
        ran.Should().BeTrue();
    }
}
