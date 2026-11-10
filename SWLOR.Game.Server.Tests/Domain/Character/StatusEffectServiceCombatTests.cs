using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>What the attack and damage paths ask of the status effects.</summary>
[TestFixture]
public class StatusEffectServiceCombatTests : StatusEffectServiceTestBase
{
    private FakeEventsPlugin _events = null!;
    private IDisposable _eventsScope = null!;

    [SetUp]
    public void InstallEvents()
    {
        _events = new FakeEventsPlugin();
        _eventsScope = EventsPluginRuntime.Override(_events);
    }

    [TearDown]
    public void RemoveEvents() => _eventsScope.Dispose();

    private void DamageEvent(uint attacker, uint defender, string damage, string damageType)
    {
        Bench.Engine.SetReturn("OBJECT_SELF", attacker);
        Bench.Engine.SetReturn("StringToObject", defender);
        _events.SetReturnHandler("GetEventData", args => (string)args[0]! switch
        {
            "DEFENDER" => defender.ToString(),
            "DAMAGE" => damage,
            "DAMAGE_TYPE" => damageType,
            _ => string.Empty
        });
    }

    [Test]
    public void TheDamageHandler_TellsBothSidesThenRunsTheAttackersOnHitEffects()
    {
        var onHit = Applied<ProbeEffect>(Caster, Caster, probe =>
        {
            probe.ActivationValue = SWLOR.Game.Server.Domain.Character.StatusEffectActivationType.OnHit;
            probe.NameValue = "Hit";
        });
        var taken = Applied<OtherProbeEffect>(Target, Target, probe => probe.NameValue = "Taken");
        DamageEvent(Caster, Target, "12", ((int)CombatDamageType.Fire).ToString());

        Service.OnDealtDamage();

        Log.Should().Equal(
            $"OnDamageDealt:Hit:{Caster}:{Target}:12:{CombatDamageType.Fire}",
            $"OnDamageTaken:Taken:{Target}:{Caster}:12:{CombatDamageType.Fire}",
            $"OnHit:Hit:{Caster}:{Target}:12");
        onHit.Name.Should().Be("Hit");
        taken.Name.Should().Be("Taken");
    }

    [Test]
    public void TheDamageHandler_ReadsAnUnparsableTypeAsPhysical()
    {
        Applied<OtherProbeEffect>(Target, Target, probe => probe.NameValue = "Taken");
        DamageEvent(Caster, Target, "3", "not a number");

        Service.OnDealtDamage();

        Log.Should().Equal($"OnDamageTaken:Taken:{Target}:{Caster}:3:{CombatDamageType.Physical}");
    }

    [TestCase(0u, 100u, 200u)]
    [TestCase(5u, Invalid, 200u)]
    [TestCase(5u, 100u, Invalid)]
    public void NoDamageOrAMissingSideNotifiesNobody(uint damage, uint attacker, uint defender)
    {
        Applied<ProbeEffect>(Caster, Caster);
        Applied<OtherProbeEffect>(Target, Target);

        Service.NotifyDamageStatusEffects(attacker, defender, (int)damage, CombatDamageType.Fire);
        Service.NotifyPreDamageStatusEffects(attacker, defender, (int)damage, CombatDamageType.Fire);

        Log.Should().BeEmpty();
    }

    [Test]
    public void OnlyDirectDamageReachesALegacyCallback()
    {
        Applied<ProbeEffect>(Caster, Caster);

        Service.NotifyDamageStatusEffects(Caster, Target, 5, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered);
        Log.Should().BeEmpty();

        Service.NotifyDamageStatusEffects(Caster, Target, 5, CombatDamageType.Fire);
        Log.Should().ContainSingle();
    }

    [Test]
    public void BeforeAHit_TheDefendersEffectsRunThenTheMeleeTechniquePrepares()
    {
        Applied<PreDamageProbeEffect>(Target, Target, probe => probe.NameValue = "Pre");
        Applied<ProbeEffect>(Target, Target);

        Service.NotifyPreDamageStatusEffects(Caster, Target, 7, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered);

        Log.Should().Equal(
            $"OnBeforeDamageTaken:Pre:{Target}:{Caster}:7:{CombatDamageType.Fire}:{CombatDamageDeliveryType.Triggered}",
            $"PrepareDirectDamage:{Caster}:{Target}:{CombatDamageDeliveryType.Triggered}");
    }

    [Test]
    public void AnAttackAttempt_TellsTheEffectsAndRemovesWhatItExhausted()
    {
        var spent = Applied<AttackAttemptProbeEffect>(Caster, Caster, probe => probe.NameValue = "Spent");
        var left = Applied<ChargedAttackAttemptProbeEffect>(Caster, Caster, probe => probe.NameValue = "Left");
        var impact = new AbilityImpactSummary { CriticalHitCount = 2 };

        Service.NotifyAttackAttemptStatusEffects(Caster, SkillType.Rifle, impact);

        Log.Should().Contain($"OnAttackAttempted:Spent:{Caster}:{SkillType.Rifle}:2");
        Log.Should().Contain($"OnAttackAttempted:Left:{Caster}:{SkillType.Rifle}:2");
        Log.Should().Contain($"Remove:Spent:{Caster}:False");
        Service.GetCreatureStatusEffects(Caster).GetAllEffects().Should().Equal(left);
        spent.IsFlaggedForRemoval.Should().BeTrue();
    }

    public class ChargedAttackAttemptProbeEffect : AttackAttemptProbeEffect
    {
        public ChargedAttackAttemptProbeEffect() => Charges = 99;
    }

    [Test]
    public void AnAttackAttemptByAnInvalidAttackerDoesNothing()
    {
        Applied<AttackAttemptProbeEffect>(Caster, Caster);
        Bench.Invalid.Add(Caster);

        Service.NotifyAttackAttemptStatusEffects(Caster, SkillType.Rifle);

        Log.Should().BeEmpty();
    }

    [Test]
    public void TheLimitedDelayReduction_AddsEveryMatchingEffectAndTakesTheFewestCharges()
    {
        Applied<LimitedDelayProbeEffect>(Caster, Caster, probe =>
        {
            probe.AttackDelayReductionPercent = 25;
            probe.RemainingAttacks = 3;
        });
        Applied<OtherLimitedDelayProbeEffect>(Caster, Caster, probe =>
        {
            probe.AttackDelayReductionPercent = 10;
            probe.RemainingAttacks = 1;
        });
        Applied<ChargelessDelayProbeEffect>(Caster, Caster);

        Service.TryGetLimitedAttackDelayReduction(Caster, SkillType.Rifle, out var reduction, out var remaining).Should().BeTrue();
        reduction.Should().Be(35);
        remaining.Should().Be(1);

        Service.TryGetLimitedAttackDelayReduction(Caster, SkillType.Force, out reduction, out remaining).Should().BeFalse();
        reduction.Should().Be(0);
        remaining.Should().Be(0);
    }

    public class ChargelessDelayProbeEffect : LimitedDelayProbeEffect
    {
        public ChargelessDelayProbeEffect()
        {
            AttackDelayReductionPercent = 50;
            RemainingAttacks = 0;
        }
    }

    public class ZeroPercentDelayProbeEffect : LimitedDelayProbeEffect
    {
        public ZeroPercentDelayProbeEffect() => AttackDelayReductionPercent = 0;
    }

    [Test]
    public void AZeroPercentReductionIsNotAReduction()
    {
        Applied<ZeroPercentDelayProbeEffect>(Caster, Caster);

        Service.TryGetLimitedAttackDelayReduction(Caster, SkillType.Rifle, out var reduction, out var remaining).Should().BeFalse();

        reduction.Should().Be(0);
        remaining.Should().Be(3);
    }

    [Test]
    public void TheNoDelayEffect_TakesTheFewestChargesOfAMatchingEffect()
    {
        Applied<LimitedNoDelayProbeEffect>(Caster, Caster, probe => probe.RemainingAttacks = 2);

        Service.TryGetLimitedAttackNoDelay(Caster, SkillType.Rifle, out var remaining).Should().BeTrue();
        remaining.Should().Be(2);

        Service.TryGetLimitedAttackNoDelay(Caster, SkillType.Force, out remaining).Should().BeFalse();
        remaining.Should().Be(0);
    }

    [Test]
    public void ASuppressedAttacker_GetsNeitherDelayEffect()
    {
        Applied<LimitedDelayProbeEffect>(Caster, Caster);
        Applied<LimitedNoDelayProbeEffect>(Caster, Caster);
        Service.AttackDelaySuppressed.Add(Caster);

        Service.TryGetLimitedAttackDelayReduction(Caster, SkillType.Rifle, out var reduction, out var delayRemaining).Should().BeFalse();
        Service.TryGetLimitedAttackNoDelay(Caster, SkillType.Rifle, out var noDelayRemaining).Should().BeFalse();

        reduction.Should().Be(0);
        delayRemaining.Should().Be(0);
        noDelayRemaining.Should().Be(0);
    }

    [Test]
    public void AGuardedHit_ReachesOnlyTheDefendersGuardEffects()
    {
        Service.OnGuardedHit(Target, Caster, 5);
        Log.Should().BeEmpty();

        Applied<GuardedHitProbeEffect>(Target, Target, probe => probe.NameValue = "Guard");
        Applied<ProbeEffect>(Target, Target);

        Service.OnGuardedHit(Target, Caster, 5);

        Log.Should().Equal($"OnGuardedHit:Guard:{Target}:{Caster}:5");
    }
}
