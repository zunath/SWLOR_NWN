using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The default of each call into unconverted code reaches the real static code. The engine fake reports the creature as
/// gone or not a player, so each call stops at its first check. <c>MayHaveEffectTag</c> reads the native engine and is left
/// to the in-engine tests.
/// </summary>
[TestFixture]
public class StatusEffectServiceDefaultSeamsTests
{
    private const uint Creature = 61250;

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private IDisposable _engineScope = null!;
    private FakeNWScript _engine = null!;
    private CoreDefaults _core = null!;
    private ApplicationDefaults _application = null!;
    private DurationDefaults _duration = null!;
    private NativeDefaults _native = null!;
    private CombatEventsDefaults _combatEvents = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _core = new CoreDefaults();
        _application = new ApplicationDefaults();
        _duration = new DurationDefaults();
        _native = new NativeDefaults();
        _combatEvents = new CombatEventsDefaults();
    }

    [TearDown]
    public void TearDown()
    {
        StatusEffectInternals.CreatureEffects.Remove(Creature);
        _engineScope.Dispose();
        _facadeScope.Dispose();
    }

    [Test]
    public void TheConstructorOnlyStoresItsArguments()
    {
        _engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void ReapplyFoodHP_TellsTheRegisteredListener()
    {
        var listener = new RecordingListener();
        _core.RegisterStatEffectListener(listener);

        _core.CallReapplyFoodHP(Creature);

        listener.Restored.Should().Equal(Creature);
        listener.Changed.Should().BeEmpty();
    }

    [Test]
    public void ApplyCreatureMovementRate_TellsTheRegisteredListener()
    {
        var listener = new RecordingListener();
        _core.RegisterStatEffectListener(listener);

        _core.CallApplyCreatureMovementRate(Creature);

        listener.Changed.Should().Equal(Creature);
        listener.Restored.Should().BeEmpty();
    }

    [Test]
    public void ARegisteredListenerIsKeptOnce()
    {
        var listener = new RecordingListener();
        _core.RegisterStatEffectListener(listener);
        _core.RegisterStatEffectListener(listener);

        _core.CallApplyCreatureMovementRate(Creature);

        listener.Changed.Should().Equal(Creature);
    }

    [Test]
    public void ANoticeWithNoListenerRegisteredFailsAtOnce()
    {
        var restored = () => _core.CallReapplyFoodHP(Creature);
        var changed = () => _core.CallApplyCreatureMovementRate(Creature);

        restored.Should().Throw<InvalidOperationException>();
        changed.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void GetCreatureStatAdjustment_IsStatsTotalWhichIncludesStatusEffects()
    {
        var tracker = new CreatureStatusEffect();
        var effect = new ProbeEffect();
        effect.StatGroup.Stats[StatType.Attack] = 6;
        tracker.Add(effect);
        StatusEffectInternals.CreatureEffects[Creature] = tracker;

        _duration.CallGetCreatureStatAdjustment(Creature, StatType.Attack).Should().Be(6);
    }

    [Test]
    public void GetActiveAbilityImpactSummary_IsNullWithNoActiveImpact()
    {
        _application.CallGetActiveAbilityImpactSummary(Creature).Should().BeNull();
    }

    [Test]
    public void ApplyStatusAppliedTargetStaminaDrain_AsksCombatWhichStopsAtAnInvalidActivator()
    {
        _application.CallApplyStatusAppliedTargetStaminaDrain(Creature, Creature + 1, StatusEffectCategory.Debuff);

        _engine.CountOf("GetIsObjectValid").Should().Be(1);
    }

    [Test]
    public void ApplyHostileAbilityPartyBuff_AsksCombatWhichStopsAtANonHostileAbility()
    {
        _application.CallApplyHostileAbilityPartyBuff(Creature, new AbilityDetail { IsHostileAbility = false }, 0, true);

        _engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void IsAttackDelayReductionSuppressed_ReadsTheSuppressionStat()
    {
        _combatEvents.CallIsAttackDelayReductionSuppressed(Creature).Should().BeFalse();

        var tracker = new CreatureStatusEffect();
        var effect = new ProbeEffect();
        effect.StatGroup.Stats[StatType.AttackDelayReductionSuppressed] = 1;
        tracker.Add(effect);
        StatusEffectInternals.CreatureEffects[Creature] = tracker;

        _combatEvents.CallIsAttackDelayReductionSuppressed(Creature).Should().BeTrue();
    }

    [Test]
    public void ScheduleTemporaryHitPointLimitCheck_AsksTheFeatureWhichStopsAtAnInvalidTarget()
    {
        _native.CallScheduleTemporaryHitPointLimitCheck(Creature);

        _engine.CountOf("GetIsObjectValid").Should().Be(1);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    private sealed class RecordingListener : IStatEffectListener
    {
        public List<uint> Restored { get; } = new();

        public List<uint> Changed { get; } = new();

        public void OnStatusEffectsRestored(uint player) => Restored.Add(player);

        public void OnStatAdjustingEffectsChanged(uint creature) => Changed.Add(creature);
    }

    // Each part's seams run on a subclass of that part, so the pull request that moves a part changes one base type. Until then
    // every part is StatusEffectService and shares the constructor of DefaultsBase. Part 2's search and part 7's ability details
    // are answered by the bench's double and read no unconverted code, so they have no default to run.

    private abstract class DefaultsBase : StatusEffectService
    {
        protected DefaultsBase()
            : base(null!, null!, null!, null!, null!, null!, null!, null!, null!, FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActiveAbilityImpact>(), FacadeForwarder.LazyFor<ICombatAbilityImpactService>())
        {
        }
    }

    /// <summary>Part 1, <c>StatusEffectService</c> (stays): the listener notices.</summary>
    private sealed class CoreDefaults : DefaultsBase
    {
        public void CallReapplyFoodHP(uint player) => ReapplyFoodHP(player);

        public void CallApplyCreatureMovementRate(uint creature) => ApplyCreatureMovementRate(creature);
    }

    /// <summary>Part 3, <c>StatusEffectApplicationService</c>.</summary>
    private sealed class ApplicationDefaults : DefaultsBase
    {
        public AbilityImpactSummary CallGetActiveAbilityImpactSummary(uint activator) => GetActiveAbilityImpactSummary(activator);

        public void CallApplyStatusAppliedTargetStaminaDrain(uint activator, uint target, StatusEffectCategory categories) =>
            ApplyStatusAppliedTargetStaminaDrain(activator, target, categories);

        public void CallApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied) =>
            ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);
    }

    /// <summary>Part 4, <c>StatusEffectDurationService</c>.</summary>
    private sealed class DurationDefaults : DefaultsBase
    {
        public int CallGetCreatureStatAdjustment(uint creature, StatType stat) => GetCreatureStatAdjustment(creature, stat);
    }

    /// <summary>Part 5, <c>StatusEffectNativeService</c>.</summary>
    private sealed class NativeDefaults : DefaultsBase
    {
        public void CallScheduleTemporaryHitPointLimitCheck(uint target) => ScheduleTemporaryHitPointLimitCheck(target);
    }

    /// <summary>Part 6, <c>StatusEffectCombatEventService</c>.</summary>
    private sealed class CombatEventsDefaults : DefaultsBase
    {
        public bool CallIsAttackDelayReductionSuppressed(uint creature) => IsAttackDelayReductionSuppressed(creature);
    }
}
