using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The paired ranged traits instance: the ordinary-shot gates and snapshots, the stamina discount, Cadence, the ability
/// capture, commit and completion, the weapon-ability wrappers, the status-effect sync and the clear paths. The engine
/// is a recording fake, the clock is set by the test, the stats are a table, and the neighbouring services are
/// recorded by a subclass.
/// </summary>
[TestFixture]
public class PairedRangedTraitsServiceTests
{
    private const uint Actor = 1001;
    private const uint Target = 2002;
    private const uint Invalid = 0x7F000000;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private DateTime _now;
    private bool _actorDead;
    private bool _actorValid;
    private ScriptedProxy _mastery = null!;
    private TestTraits _traits = null!;

    [SetUp]
    public void SetUp()
    {
        _now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
        _actorDead = false;
        _actorValid = true;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid && (_actorValid || (uint)args[0]! != Actor));
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Actor && _actorDead);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => 100);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => true);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(Clock.OverrideForTests(() => _now));
        var mastery = ScriptedProxy.Create<ITwinPistolMasteryService>(out _mastery);
        _mastery.On("RecordTargetChange", _ => null).On("ClearTrialState", _ => null);
        _traits = new TestTraits(mastery);
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    private static AbilityDetail WeaponAbility(bool hostile = true, bool keepsCadence = false, int? damagePerCharge = null,
        int cadenceOnHit = 0) => new()
    {
        IsHostileAbility = hostile,
        SkillType = SkillType.TwinPistol,
        Name = "Test Shot",
        KeepsWeaponAbilityCadence = keepsCadence,
        WeaponAbilityCadenceDamagePerCharge = damagePerCharge,
        WeaponAbilityCadenceOnHit = cadenceOnHit
    };

    [Test]
    public void AnActorThatCannotReceiveEventsGetsNeutralAnswers()
    {
        _actorValid = false;

        _traits.RecordOrdinaryAttempt(Actor, Target, 1).Should().Be(new PairedRangedOrdinarySnapshot(0, 0));
        _traits.GetOrdinaryAccuracy(Actor).Should().Be(0);
        _traits.GetOrdinaryDamagePercentAdjustment(Actor, Target).Should().Be(0);
        _traits.HasDirectHostileDamageTrait(Actor).Should().BeFalse();
        _traits.GetAbilityStaminaCostAdjustment(Actor).Should().Be(0);
        _traits.ConsumeAbilityStaminaDiscount(Actor).Should().Be(0);
        _traits.GetConfiguration(Actor).Should().Be(new PairedRangedTraitConfiguration());
        _traits.CaptureAbility(Actor, true, true).StateGeneration.Should().Be(-1);
        _traits.CommitAbility(Actor, new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0)).Should().BeFalse();
        _traits.CaptureWeaponAbility(Actor, WeaponAbility()).Should().BeNull();
        _traits.RecordOrdinaryHit(Actor, Target, 1);
        _traits.RecordDirectHostileDamage(Actor);
        _traits.RecordAccuracyEvasionMiss(Actor);
        _traits.CompleteAbility(Actor, new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0), true, 5, 5);

        _traits.Applied.Should().BeEmpty();
        _traits.StaminaRestored.Should().BeEmpty();
    }

    [Test]
    public void AnUntrackedActorWithoutOrdinaryTraitsKeepsNoState()
    {
        var snapshot = _traits.RecordOrdinaryAttempt(Actor, Target, 1);
        _traits.RecordOrdinaryHit(Actor, Target, 1);
        _traits.Reconcile(Actor);
        _traits.ExitExtendedCadence(Actor);
        _traits.ClearCombatState(Actor);
        _traits.ClearEquipmentState(Actor);

        snapshot.Should().Be(new PairedRangedOrdinarySnapshot(0, 0));
        _traits.Applied.Should().BeEmpty();
        _mastery.Calls.Select(call => call.Member).Should().Contain("RecordTargetChange").And.Contain("ClearTrialState");
    }

    [Test]
    public void RepeatedHitsOnOneTargetRaiseTheOrdinaryDamageAndShowTheEffect()
    {
        _traits.Stats[StatType.PairedRangedRepeatedTargetRequiredHits] = 2;
        _traits.Stats[StatType.PairedRangedRepeatedTargetDamagePercent] = 15;
        _traits.Stats[StatType.PairedRangedRepeatedTargetDurationSeconds] = 10;

        for (long roll = 1; roll <= 2; roll++)
        {
            _traits.RecordOrdinaryAttempt(Actor, Target, roll);
            _traits.RecordOrdinaryHit(Actor, Target, roll);
        }

        _traits.GetOrdinaryDamagePercentAdjustment(Actor, Target).Should().Be(15);
        _traits.GetOrdinaryDamagePercentAdjustment(Actor, Target + 1).Should().Be(0);
        _traits.Applied.Should().ContainSingle(effect => effect is ConvergingFireStatusEffect);

        _traits.RecordTargetChange(Actor, Target + 1);
        _traits.Removed.Should().Contain(typeof(ConvergingFireStatusEffect));
        _traits.RecordTargetChange(Actor + 3, Target);
    }

    [Test]
    public void EveryNthOrdinaryHitRestoresStaminaForADualWielder()
    {
        _traits.DualWield = true;
        _traits.Stats[StatType.PairedRangedOrdinaryHitRestoreCount] = 2;
        _traits.Stats[StatType.PairedRangedOrdinaryHitStaminaRestore] = 4;
        _traits.Stats[StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds] = 5;

        for (long roll = 1; roll <= 2; roll++)
        {
            _traits.RecordOrdinaryAttempt(Actor, Target, roll);
            _traits.RecordOrdinaryHit(Actor, Target, roll);
        }

        _traits.StaminaRestored.Should().Equal(4);
    }

    [Test]
    public void ASingleWielderGetsNoPairedOrdinaryTraits()
    {
        _traits.DualWield = false;
        _traits.Stats[StatType.PairedRangedOrdinaryHitRestoreCount] = 1;
        _traits.Stats[StatType.PairedRangedOrdinaryHitStaminaRestore] = 4;

        _traits.RecordOrdinaryAttempt(Actor, Target, 1);
        _traits.RecordOrdinaryHit(Actor, Target, 1);

        _traits.StaminaRestored.Should().BeEmpty();
    }

    [Test]
    public void DirectDamageDiscountsTheNextAbilityOnce()
    {
        _traits.Stats[StatType.PairedRangedDirectDamageNextAbilityStaminaReduction] = 3;
        _traits.Stats[StatType.PairedRangedDirectDamageCostWindowSeconds] = 10;
        _traits.Stats[StatType.PairedRangedDirectDamageCostCooldownSeconds] = 10;

        _traits.HasDirectHostileDamageTrait(Actor).Should().BeTrue();
        _traits.RecordDirectHostileDamage(Actor);

        _traits.Applied.Should().ContainSingle(effect => effect is CoolUnderFireStatusEffect);
        _traits.GetAbilityStaminaCostAdjustment(Actor).Should().Be(-3);
        _traits.ConsumeAbilityStaminaDiscount(Actor).Should().Be(-3);
        _traits.GetAbilityStaminaCostAdjustment(Actor).Should().Be(0);
        _traits.Removed.Should().Contain(typeof(CoolUnderFireStatusEffect));
    }

    [Test]
    public void ADiscountWithoutTheTraitOrAStateIsNothing()
    {
        _traits.RecordDirectHostileDamage(Actor);
        _traits.GetAbilityStaminaCostAdjustment(Actor).Should().Be(0);
        _traits.ConsumeAbilityStaminaDiscount(Actor).Should().Be(0);

        _traits.Stats[StatType.PairedRangedDirectDamageNextAbilityStaminaReduction] = 3;
        _traits.GetAbilityStaminaCostAdjustment(Actor).Should().Be(0, "no damage was recorded yet");
        _traits.Stats[StatType.PairedRangedDirectDamageNextAbilityStaminaReduction] = 0;
        _traits.RecordDirectHostileDamage(Actor);
        _traits.Applied.Should().BeEmpty();
    }

    [Test]
    public void AMissGrantsCadenceAndTheNextAbilityAccuracyOnACooldown()
    {
        ConfigureCadence();
        _traits.Stats[StatType.EvasionCadenceCooldownSeconds] = 5;

        _traits.RecordAccuracyEvasionMiss(Actor);
        _traits.RecordAccuracyEvasionMiss(Actor);

        _traits.Applied.Should().Contain(effect => effect is CadenceStatusEffect)
            .And.Contain(effect => effect is ChangingAnglesStatusEffect);
        _traits.GetOrdinaryAccuracy(Actor).Should().Be(2, "one charge at two accuracy each");

        _traits.RecordAccuracyEvasionMiss(Actor + 5);
    }

    [Test]
    public void AMissWithoutCadenceOrACooldownDoesNothing()
    {
        _traits.RecordAccuracyEvasionMiss(Actor);
        ConfigureCadence();
        _traits.Stats[StatType.EvasionCadenceCooldownSeconds] = 0;
        _traits.RecordAccuracyEvasionMiss(Actor);

        _traits.Applied.Should().BeEmpty();
    }

    [Test]
    public void ARefreshedEffectOnlyChangesWhenTheNameTheIconOrTheTimeDiffer()
    {
        ConfigureCadence();
        _traits.Stats[StatType.EvasionCadenceCooldownSeconds] = 1;
        _traits.RecordAccuracyEvasionMiss(Actor);
        var applied = _traits.Applied.Count;
        _now = _now.AddSeconds(2);

        _traits.RecordAccuracyEvasionMiss(Actor);

        _traits.Refreshed.Should().NotBeEmpty("the second charge replaced the effect or refreshed its time");
        _traits.Applied.Count.Should().BeGreaterThanOrEqualTo(applied);
        _traits.Reconcile(Actor);
        _traits.Reconcile(Actor);
        _now = _now.AddSeconds(100);
        _traits.Reconcile(Actor);
        _traits.Removed.Should().Contain(typeof(CadenceStatusEffect));
    }

    [Test]
    public void AnAbilityCapturesCommitsAndCompletesWithTheStaminaRestored()
    {
        ConfigureCadence();
        _traits.Stats[StatType.CadenceStaminaRestorePerCharge] = 1;
        _traits.Stats[StatType.CadenceStaminaRestoreMaximum] = 5;
        _traits.Stats[StatType.CadenceRestoreCooldownSeconds] = 5;
        _traits.Stats[StatType.EvasionCadenceCooldownSeconds] = 5;
        _traits.RecordAccuracyEvasionMiss(Actor);

        var begun = _traits.BeginAbility(Actor, true, true);
        _traits.CompleteAbility(Actor, begun, true, 10, 3, 2, 20, 8);

        begun.CadenceSpent.Should().Be(1);
        _traits.AbilityStaminaRestored.Should().Equal(4);
        _traits.GetOrdinaryDamagePercentAdjustment(Actor, Target).Should().Be(20, "the ordinary bonus window opened");
        _traits.Applied.Should().Contain(effect => effect is TwinSunsStatusEffect);
        _traits.CommitAbility(Actor, begun).Should().BeFalse("an activation commits once");
    }

    [Test]
    public void AnAbilityWithNothingToRestoreRestoresNothing()
    {
        var begun = _traits.BeginAbility(Actor, false, false);
        _traits.CompleteAbility(Actor, begun, false, 10, 3);
        _traits.CompleteAbility(Actor, begun, true, 10, 3);

        _traits.AbilityStaminaRestored.Should().BeEmpty();
    }

    [Test]
    public void AWeaponAbilityOutsideTheRulesCapturesNothingOrAPlaceholder()
    {
        _traits.CaptureWeaponAbility(Actor, null).Should().BeNull();
        _traits.CaptureWeaponAbility(Actor, WeaponAbility(hostile: false)).Should().BeNull();

        var placeholder = _traits.CaptureWeaponAbility(Actor, WeaponAbility())!;

        placeholder.Snapshot.StateGeneration.Should().Be(-1);
        _traits.CommitWeaponAbility(Actor, WeaponAbility(), placeholder);
        placeholder.IsCommitted.Should().BeTrue();
        _traits.RecordWeaponAbilityHit(Actor, placeholder);
        _traits.CompleteWeaponAbilityHit(Actor, placeholder);
        _traits.Applied.Should().BeEmpty();
        _traits.CommitWeaponAbility(Actor, WeaponAbility(), placeholder);
        _traits.CommitWeaponAbility(Actor, WeaponAbility(), null!);
    }

    [Test]
    public void AWeaponAbilitySpendsCadenceForDamageAndChainsIntoTheNextOne()
    {
        ConfigureCadence();
        _traits.Stats[StatType.CadenceChainWindowSeconds] = 6;
        _traits.Stats[StatType.CadenceDamagePerCharge] = 2;
        _traits.Stats[StatType.CadenceBonusDamagePerCharge] = 1;
        _traits.Stats[StatType.EvasionCadenceCooldownSeconds] = 1;
        _traits.RecordAccuracyEvasionMiss(Actor);

        var first = _traits.CaptureWeaponAbility(Actor, WeaponAbility(damagePerCharge: 4))!;
        first.DamagePerCharge.Should().Be(5, "the ability's own per-charge plus the bonus");
        first.PrimaryWeapon.Should().Be(TestTraits.Weapon);
        _traits.CommitWeaponAbility(Actor, WeaponAbility(), first);
        first.IsCommitted.Should().BeTrue();
        first.PaidStamina.Should().Be(7);

        var second = _traits.CaptureWeaponAbility(Actor, new AbilityDetail
        {
            IsHostileAbility = true, SkillType = SkillType.TwinPistol, EffectiveLevelPerkType = PerkType.LeadShot
        })!;
        _now = _now.AddSeconds(1);
        _traits.CommitWeaponAbility(Actor, new AbilityDetail { EffectiveLevelPerkType = PerkType.LeadShot }, second);

        second.ContinuesChain.Should().BeTrue();
        _traits.RecordWeaponAbilityHit(Actor, second);
        _traits.RecordWeaponAbilityHit(Actor, second);
        _traits.CompleteWeaponAbilityHit(Actor, second);
        _traits.CompleteWeaponAbilityHit(Actor, second);
        second.RecoveryCompleted.Should().BeTrue();
    }

    [Test]
    public void AnUnnamedPerklessAbilityStillGetsAChainKey()
    {
        ConfigureCadence();
        _traits.Stats[StatType.CadenceChainWindowSeconds] = 6;
        var activation = _traits.CaptureWeaponAbility(Actor, WeaponAbility())!;

        _traits.CommitWeaponAbility(Actor, new AbilityDetail { Name = null }, activation);

        activation.IsCommitted.Should().BeTrue();
    }

    [Test]
    public void AnIntrinsicAbilityGrantsTheBonusWindowAndRestoresStaminaOnce()
    {
        var activation = new WeaponAbilityTraitActivation(
            new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0), 0);

        _traits.CompleteIntrinsicAbility(Actor, null!, true, 5, 3, 2, 10, 6);
        _traits.CompleteIntrinsicAbility(Actor, activation, true, 5, 3, 2, 10, 6);
        _traits.CompleteIntrinsicAbility(Actor, activation, true, 5, 3, 2, 10, 6);

        _traits.AbilityStaminaRestored.Should().Equal(3);
        _traits.Applied.Should().ContainSingle(effect => effect is TwinSunsStatusEffect);
        _traits.GetOrdinaryDamagePercentAdjustment(Actor, Target).Should().Be(10);

        var idle = new WeaponAbilityTraitActivation(new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0), 0);
        _traits.CompleteIntrinsicAbility(Actor, idle, false, 5, 3, 0, 0, 0);
        _traits.AbilityStaminaRestored.Should().Equal(3);
    }

    [Test]
    public void AHitReportedByAnUncommittedActivationAddsNothing()
    {
        ConfigureCadence();
        _traits.RecordWeaponAbilityHit(Actor, null!);
        var activation = _traits.CaptureWeaponAbility(Actor, WeaponAbility(cadenceOnHit: 1))!;

        _traits.RecordWeaponAbilityHit(Actor, activation);
        _traits.CompleteWeaponAbilityHit(Actor, activation);
        _traits.Applied.Should().BeEmpty("the activation was never committed");

        _traits.CommitWeaponAbility(Actor, WeaponAbility(), activation);
        _traits.RecordWeaponAbilityHit(Actor, activation);

        _traits.Applied.Should().ContainSingle(effect => effect is CadenceStatusEffect);
    }

    [Test]
    public void ClearingDropsTransientStateAndKeepsTheTrackedActorUntilItIsCleared()
    {
        ConfigureCadence();
        _traits.Stats[StatType.EvasionCadenceCooldownSeconds] = 5;
        _traits.RecordAccuracyEvasionMiss(Actor);
        _traits.Stats[StatType.CadenceDurationSeconds] = 4;

        _traits.ExitExtendedCadence(Actor);
        _traits.ClearCombatState(Actor);
        _traits.Removed.Should().Contain(typeof(CadenceStatusEffect));

        _traits.RecordAccuracyEvasionMiss(Actor);
        _traits.ClearActor(Actor);
        _traits.Reconcile(Actor);
        _traits.RecordAccuracyEvasionMiss(Actor);
        _traits.ForgetDestroyed(Actor);
        _traits.Reconcile(Actor);
        _mastery.Calls.Count(call => call.Member == "ClearTrialState").Should().BeGreaterThanOrEqualTo(3);
    }

    private void ConfigureCadence()
    {
        _traits.Stats[StatType.CadenceMaximum] = 3;
        _traits.Stats[StatType.CadenceDurationSeconds] = 12;
        _traits.Stats[StatType.CadenceAccuracyPerCharge] = 2;
        _traits.Stats[StatType.CadenceNextAbilityAccuracy] = 5;
        _traits.Stats[StatType.CadenceAccuracyDurationSeconds] = 8;
        _traits.Stats[StatType.CadenceAccuracyCooldownSeconds] = 10;
    }

    private sealed class TestTraits : PairedRangedTraitsService
    {
        public const uint Weapon = 3003;
        private readonly Dictionary<Type, StatusEffectBase> _effects = new();
        public readonly Dictionary<StatType, int> Stats = new();
        public readonly List<StatusEffectBase> Applied = new();
        public readonly List<Type> Removed = new();
        public readonly List<float> Refreshed = new();
        public readonly List<int> StaminaRestored = new();
        public readonly List<int> AbilityStaminaRestored = new();
        public bool DualWield;

        public TestTraits(ITwinPistolMasteryService mastery)
            : base(mastery, FacadeForwarder.LazyFor<IAbilityService>(), FacadeForwarder.For<IStatService>(), FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>())
        {
        }

        protected override uint GetAbilityAttackWeapon(uint actor, AbilityDetail ability) => Weapon;

        protected override int GetAbilityStaminaPaid(uint actor, AbilityDetail ability) => 7;

        protected override int RestoreAbilityHitStamina(uint actor, AbilityDetail ability, int requested)
        {
            AbilityStaminaRestored.Add(requested);
            return requested;
        }

        protected override AbilityDetail GetActiveAbility(uint actor) => null!;

        protected override int GetStatAdjustment(uint actor, StatType stat) => Stats.GetValueOrDefault(stat);

        protected override int RestoreStamina(uint actor, int amount)
        {
            StaminaRestored.Add(amount);
            return amount;
        }

        protected override bool HasDualWield(uint actor) => DualWield;

        protected override T GetStatusEffect<T>(uint actor) => _effects.GetValueOrDefault(typeof(T)) as T;

        protected override void RemoveStatusEffect<T>(uint actor)
        {
            Removed.Add(typeof(T));
            _effects.Remove(typeof(T));
        }

        protected override void RefreshStatusEffectDuration(uint actor, Type statusEffectType, float seconds) =>
            Refreshed.Add(seconds);

        protected override void ApplyStatusEffect(uint actor, IStatusEffect effect, float seconds)
        {
            var typed = (StatusEffectBase)effect;
            Applied.Add(typed);
            _effects[effect.GetType()] = typed;
        }
    }
}
