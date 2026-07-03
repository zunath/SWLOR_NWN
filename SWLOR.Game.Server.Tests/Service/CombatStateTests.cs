using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatStateTests
{
    private DateTime _now;

    [SetUp]
    public void SetUp()
    {
        _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        CombatState.ClearAllForTests();
        CombatState.SetClock(() => _now);
    }

    [TearDown]
    public void TearDown()
    {
        CombatState.ResetClock();
        CombatState.ClearAllForTests();
    }

    [Test]
    public void RecentDamageTarget_ExpiresAndRemovesAfterWindow()
    {
        CombatState.TrackRecentDamageTarget(100, 200);

        _now = _now.AddSeconds(5);
        CombatState.HasRecentDamageTarget(100, 200, 6f).Should().BeTrue();

        _now = _now.AddSeconds(2);
        CombatState.HasRecentDamageTarget(100, 200, 6f).Should().BeFalse();

        _now = _now.AddSeconds(-7);
        CombatState.HasRecentDamageTarget(100, 200, 6f).Should().BeFalse();
    }

    [Test]
    public void StatTriggerCooldown_BlocksUntilCooldownExpires()
    {
        CombatState.TryUseStatTrigger(100, StatType.Attack, TimeSpan.FromSeconds(10)).Should().BeTrue();

        _now = _now.AddSeconds(9);
        CombatState.TryUseStatTrigger(100, StatType.Attack, TimeSpan.FromSeconds(10)).Should().BeFalse();

        _now = _now.AddSeconds(2);
        CombatState.TryUseStatTrigger(100, StatType.Attack, TimeSpan.FromSeconds(10)).Should().BeTrue();
    }

    [Test]
    public void GuardedHitAndDeflection_RespectRecentWindows()
    {
        CombatState.TrackGuardedHit(100);
        CombatState.TrackDeflection(100);

        _now = _now.AddSeconds(5);
        CombatState.HasRecentGuardedHit(100, 6f).Should().BeTrue();
        CombatState.HasRecentDeflection(100, 6f).Should().BeTrue();

        _now = _now.AddSeconds(2);
        CombatState.HasRecentGuardedHit(100, 6f).Should().BeFalse();
        CombatState.HasRecentDeflection(100, 6f).Should().BeFalse();
    }

    [Test]
    public void SequenceCounters_ResetAfterTriggering()
    {
        CombatState.IncrementAutoAttackCycle(100, 3).Should().BeFalse();
        CombatState.IncrementAutoAttackCycle(100, 3).Should().BeFalse();
        CombatState.IncrementAutoAttackCycle(100, 3).Should().BeTrue();
        CombatState.IncrementAutoAttackCycle(100, 3).Should().BeFalse();

        CombatState.IncrementAutoAttackCriticalCycle(100, 2).Should().BeFalse();
        CombatState.IncrementAutoAttackCriticalCycle(100, 2).Should().BeTrue();
        CombatState.IncrementAutoAttackCriticalCycle(100, 2).Should().BeFalse();

        CombatState.TrackSameTargetHostileAbilityHit(100, 200, 2).Should().BeFalse();
        CombatState.TrackSameTargetHostileAbilityHit(100, 200, 2).Should().BeTrue();
        CombatState.TrackSameTargetHostileAbilityHit(100, 200, 2).Should().BeFalse();
    }

    [Test]
    public void TimedSequences_RequireHitsWithinWindow()
    {
        CombatState.TrackCriticalHitSequence(100, 2, 3f).Should().BeFalse();
        _now = _now.AddSeconds(2);
        CombatState.TrackCriticalHitSequence(100, 2, 3f).Should().BeTrue();

        CombatState.TrackAreaAbilityTargetHitSequence(100, 200, 2, 3f).Should().BeFalse();
        _now = _now.AddSeconds(4);
        CombatState.TrackAreaAbilityTargetHitSequence(100, 200, 2, 3f).Should().BeFalse();
        _now = _now.AddSeconds(2);
        CombatState.TrackAreaAbilityTargetHitSequence(100, 200, 2, 3f).Should().BeTrue();

        CombatState.TrackHostileAbilitySequence(100, (FeatType)1, 3f).Should().BeFalse();
        _now = _now.AddSeconds(2);
        CombatState.TrackHostileAbilitySequence(100, (FeatType)2, 3f).Should().BeTrue();
    }

    [Test]
    public void AbilityStaminaCost_ExpiresAfterWindow()
    {
        CombatState.TrackAbilityStaminaCost(100, 12);

        CombatState.TryGetRecentAbilityStaminaCost(100, 10f, out var staminaCost).Should().BeTrue();
        staminaCost.Should().Be(12);

        _now = _now.AddSeconds(11);
        CombatState.TryGetRecentAbilityStaminaCost(100, 10f, out _).Should().BeFalse();
    }

    [Test]
    public void AttackSwingDebt_StoresUpdatesAndClears()
    {
        CombatState.GetAttackSwingDebt(100).Should().Be(0f);

        CombatState.UpdateAttackSwingDebt(100, 0.75f);
        CombatState.GetAttackSwingDebt(100).Should().Be(0.75f);

        CombatState.UpdateAttackSwingDebt(100, 0f);
        CombatState.GetAttackSwingDebt(100).Should().Be(0f);

        CombatState.UpdateAttackSwingDebt(100, 0.5f);
        CombatState.ClearAttackSwingDebt(100);
        CombatState.GetAttackSwingDebt(100).Should().Be(0f);
    }

    [Test]
    public void ClearCreature_RemovesStateLinkedToCreature()
    {
        CombatState.TrackRecentDamageTarget(100, 200);
        CombatState.TrackRecentDamageTarget(300, 100);
        CombatState.TrackRecentDamageTaken(100);
        CombatState.TrackGuardedHit(100);
        CombatState.TrackDeflection(100);
        CombatState.TrackAttackActivity(100);
        CombatState.TrackCombatAbilityUse(100);
        CombatState.TrackAbilityStaminaCost(100, 12);
        CombatState.TryUseStatTrigger(100, StatType.Attack, TimeSpan.FromSeconds(30)).Should().BeTrue();
        CombatState.UpdateAttackSwingDebt(100, 0.5f);
        CombatState.TrackRepeatedTargetDamageHit(100, 200, 30f, 5).Should().Be(1);
        CombatState.TrackRepeatedTargetDamageHit(100, 200, 30f, 5).Should().Be(2);

        CombatState.ClearCreature(100);

        CombatState.HasRecentDamageTarget(100, 200, 30f).Should().BeFalse();
        CombatState.HasRecentDamageTarget(300, 100, 30f).Should().BeFalse();
        CombatState.HasRecentDamageTaken(100, 30f).Should().BeFalse();
        CombatState.HasRecentGuardedHit(100, 30f).Should().BeFalse();
        CombatState.HasRecentDeflection(100, 30f).Should().BeFalse();
        CombatState.HasRecentAttackActivity(100, 30f).Should().BeFalse();
        CombatState.HasRecentCombatAbilityUse(100, 30f).Should().BeFalse();
        CombatState.TryGetRecentAbilityStaminaCost(100, 30f, out _).Should().BeFalse();
        CombatState.TryUseStatTrigger(100, StatType.Attack, TimeSpan.FromSeconds(30)).Should().BeTrue();
        CombatState.GetAttackSwingDebt(100).Should().Be(0f);
        CombatState.TrackRepeatedTargetDamageHit(100, 200, 30f, 5).Should().Be(1);
    }
}
