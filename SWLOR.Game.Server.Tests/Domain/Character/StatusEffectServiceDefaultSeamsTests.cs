using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
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
    private Defaults _defaults = null!;

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
        _defaults = new Defaults();
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
    public void ReapplyFoodHP_AsksStatWhichStopsAtANonPlayer()
    {
        _defaults.CallReapplyFoodHP(Creature);

        _engine.CountOf("GetIsPC").Should().Be(1);
        _engine.CountOf("GetObjectUUID").Should().Be(0);
    }

    [Test]
    public void ApplyCreatureMovementRate_AsksStatWhichStopsAtAnInvalidCreature()
    {
        _defaults.CallApplyCreatureMovementRate(Creature);

        _engine.CountOf("GetIsObjectValid").Should().Be(1);
        _engine.CountOf("GetObjectType").Should().Be(0);
    }

    [Test]
    public void GetCreatureStatAdjustment_IsStatsTotalWhichIncludesStatusEffects()
    {
        var tracker = new CreatureStatusEffect();
        var effect = new ProbeEffect();
        effect.StatGroup.Stats[StatType.Attack] = 6;
        tracker.Add(effect);
        StatusEffectInternals.CreatureEffects[Creature] = tracker;

        _defaults.CallGetCreatureStatAdjustment(Creature, StatType.Attack).Should().Be(6);
    }

    [Test]
    public void GetActiveAbilityImpactSummary_IsNullWithNoActiveImpact()
    {
        _defaults.CallGetActiveAbilityImpactSummary(Creature).Should().BeNull();
    }

    [Test]
    public void ApplyStatusAppliedTargetStaminaDrain_AsksCombatWhichStopsAtAnInvalidActivator()
    {
        _defaults.CallApplyStatusAppliedTargetStaminaDrain(Creature, Creature + 1, StatusEffectCategory.Debuff);

        _engine.CountOf("GetIsObjectValid").Should().Be(1);
    }

    [Test]
    public void ApplyHostileAbilityPartyBuff_AsksCombatWhichStopsAtANonHostileAbility()
    {
        _defaults.CallApplyHostileAbilityPartyBuff(Creature, new AbilityDetail { IsHostileAbility = false }, 0, true);

        _engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void IsAttackDelayReductionSuppressed_ReadsTheSuppressionStat()
    {
        _defaults.CallIsAttackDelayReductionSuppressed(Creature).Should().BeFalse();

        var tracker = new CreatureStatusEffect();
        var effect = new ProbeEffect();
        effect.StatGroup.Stats[StatType.AttackDelayReductionSuppressed] = 1;
        tracker.Add(effect);
        StatusEffectInternals.CreatureEffects[Creature] = tracker;

        _defaults.CallIsAttackDelayReductionSuppressed(Creature).Should().BeTrue();
    }

    [Test]
    public void ScheduleTemporaryHitPointLimitCheck_AsksTheFeatureWhichStopsAtAnInvalidTarget()
    {
        _defaults.CallScheduleTemporaryHitPointLimitCheck(Creature);

        _engine.CountOf("GetIsObjectValid").Should().Be(1);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    private sealed class Defaults : StatusEffectService
    {
        public Defaults()
            : base(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IAbilityService>())
        {
        }

        public void CallReapplyFoodHP(uint player) => ReapplyFoodHP(player);

        public void CallApplyCreatureMovementRate(uint creature) => ApplyCreatureMovementRate(creature);

        public int CallGetCreatureStatAdjustment(uint creature, StatType stat) => GetCreatureStatAdjustment(creature, stat);

        public AbilityImpactSummary CallGetActiveAbilityImpactSummary(uint activator) => GetActiveAbilityImpactSummary(activator);

        public void CallApplyStatusAppliedTargetStaminaDrain(uint activator, uint target, StatusEffectCategory categories) =>
            ApplyStatusAppliedTargetStaminaDrain(activator, target, categories);

        public void CallApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied) =>
            ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);

        public bool CallIsAttackDelayReductionSuppressed(uint creature) => IsAttackDelayReductionSuppressed(creature);

        public void CallScheduleTemporaryHitPointLimitCheck(uint target) => ScheduleTemporaryHitPointLimitCheck(target);
    }
}
