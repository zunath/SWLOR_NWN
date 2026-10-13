using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordPendingDamageMatcherTests
{
    private const uint Attacker = 10;
    private const uint Defender = 20;
    private const ulong Frame = 1000;
    private const long NowMs = 1760300000000;
    private const ulong DeferredFrameWindow = 10;
    private const long WeaponWindowMs = 4000;

    [Test]
    public void EngineAppliedWeaponHit_TakesTheWeaponRegistrationOverAnOlderSameAmountRider()
    {
        // A rider queued during the damage roll is registered before the weapon hit itself.
        var pending = new List<CombatRecordPendingDamage>
        {
            Deferred(amount: 12, name: "Source status rider"),
            Weapon(amount: 12)
        };

        var match = Find(pending, amount: 12, isEngineApplied: true);

        pending[match].IsWeaponHit.Should().BeTrue();
    }

    [Test]
    public void ScriptAppliedRider_TakesTheDeferredRegistrationOverASameAmountWeaponHit()
    {
        var pending = new List<CombatRecordPendingDamage>
        {
            Weapon(amount: 12),
            Deferred(amount: 12, name: "Source status rider")
        };

        var match = Find(pending, amount: 12, isEngineApplied: false);

        pending[match].IsWeaponHit.Should().BeFalse();
        pending[match].Attribution.Name.Should().Be("Source status rider");
    }

    [Test]
    public void ExactAmount_WinsWithinTheSameKind()
    {
        var pending = new List<CombatRecordPendingDamage>
        {
            Deferred(amount: 5, name: "Bleed"),
            Deferred(amount: 9, name: "Burn")
        };

        var match = Find(pending, amount: 9, isEngineApplied: false);

        pending[match].Attribution.Name.Should().Be("Burn");
    }

    [Test]
    public void OldestRegistration_WinsWhenNoAmountMatches()
    {
        var pending = new List<CombatRecordPendingDamage>
        {
            Deferred(amount: 5, name: "Bleed"),
            Deferred(amount: 9, name: "Burn")
        };

        var match = Find(pending, amount: 7, isEngineApplied: false);

        pending[match].Attribution.Name.Should().Be("Bleed");
    }

    [Test]
    public void OtherKind_IsOnlyAnExactAmountFallback()
    {
        var pending = new List<CombatRecordPendingDamage> { Deferred(amount: 9, name: "Burn") };

        Find(pending, amount: 14, isEngineApplied: true).Should().Be(-1);
        Find(pending, amount: 9, isEngineApplied: true).Should().Be(0);
    }

    [Test]
    public void OtherSourcesAndTargets_AreIgnored()
    {
        var pending = new List<CombatRecordPendingDamage>
        {
            Weapon(amount: 12) with { Source = 99 },
            Weapon(amount: 12) with { Target = 98 }
        };

        Find(pending, amount: 12, isEngineApplied: true).Should().Be(-1);
    }

    [Test]
    public void ExpiredRegistrations_AreDroppedBeforeMatching()
    {
        var pending = new List<CombatRecordPendingDamage>
        {
            Deferred(amount: 9, name: "Stale") with { Frame = Frame - DeferredFrameWindow - 1 },
            Weapon(amount: 12) with { TimestampMs = NowMs - WeaponWindowMs - 1 },
            Deferred(amount: 9, name: "Fresh")
        };

        var match = Find(pending, amount: 9, isEngineApplied: false);

        pending.Should().HaveCount(1);
        pending[match].Attribution.Name.Should().Be("Fresh");
    }

    [Test]
    public void ScopedHit_LeavesASameAmountDamageShareRegistrationForTheShare()
    {
        // An area ability hits the warded target, registering the share, then hits the share
        // target directly for the same amount.
        var scope = AbilityScope("Force Storm I");
        var pending = new List<CombatRecordPendingDamage>
        {
            Deferred(amount: 12, name: "Force Storm I") with
            {
                Attribution = new CombatRecordAttribution(Attacker, CombatRecordSourceKind.DamageShare, "Force Storm I", 0, 0)
            }
        };

        FindUnder(pending, amount: 12, scope).Should().Be(-1);
    }

    [Test]
    public void ScopedHit_LeavesAStatusRiderRegistration()
    {
        var pending = new List<CombatRecordPendingDamage> { Deferred(amount: 12, name: "Burn") };

        FindUnder(pending, amount: 12, AbilityScope("Force Storm I")).Should().Be(-1);
    }

    [Test]
    public void ScopedHit_SettlesItsOwnRegistrationByExactAmountThenOldest()
    {
        var scope = AbilityScope("Force Storm I");
        var pending = new List<CombatRecordPendingDamage>
        {
            Deferred(amount: 5, name: "Burn"),
            Scoped(amount: 7, scope),
            Scoped(amount: 9, scope)
        };

        FindUnder(pending, amount: 9, scope).Should().Be(2);
        FindUnder(pending, amount: 11, scope).Should().Be(1, "a changed amount falls back to the oldest registration under the scope");
    }

    [Test]
    public void ScopedHit_IgnoresWeaponRegistrations()
    {
        var scope = new CombatRecordAttribution(Attacker, CombatRecordSourceKind.Autoattack, null, 0, 0);
        var pending = new List<CombatRecordPendingDamage> { Weapon(amount: 12) };

        FindUnder(pending, amount: 12, scope).Should().Be(-1);
    }

    private static int FindUnder(List<CombatRecordPendingDamage> pending, int amount, CombatRecordAttribution scope)
    {
        return CombatRecordPendingDamageMatcher.FindScopeRegistration(pending, Attacker, Defender, amount, scope);
    }

    private static CombatRecordAttribution AbilityScope(string ability)
    {
        return new CombatRecordAttribution(Attacker, CombatRecordSourceKind.Ability, ability, 0, 1);
    }

    private static CombatRecordPendingDamage Scoped(int amount, CombatRecordAttribution scope)
    {
        return Deferred(amount, scope.Name) with { Attribution = scope };
    }

    private static int Find(List<CombatRecordPendingDamage> pending, int amount, bool isEngineApplied)
    {
        return CombatRecordPendingDamageMatcher.FindMatch(
            pending, Attacker, Defender, amount, isEngineApplied, Frame, NowMs, DeferredFrameWindow, WeaponWindowMs);
    }

    private static CombatRecordPendingDamage Weapon(int amount)
    {
        return new CombatRecordPendingDamage
        {
            Source = Attacker,
            Target = Defender,
            Amount = amount,
            IsWeaponHit = true,
            Frame = Frame,
            TimestampMs = NowMs,
            Attribution = new CombatRecordAttribution(Attacker, CombatRecordSourceKind.Autoattack, null, 0, 0)
        };
    }

    private static CombatRecordPendingDamage Deferred(int amount, string name)
    {
        return new CombatRecordPendingDamage
        {
            Source = Attacker,
            Target = Defender,
            Amount = amount,
            IsWeaponHit = false,
            Frame = Frame,
            TimestampMs = NowMs,
            Attribution = new CombatRecordAttribution(Attacker, CombatRecordSourceKind.StatusEffect, name, 0, 0)
        };
    }
}
