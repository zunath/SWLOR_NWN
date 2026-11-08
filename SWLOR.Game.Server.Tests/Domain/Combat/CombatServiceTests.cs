using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using CombatFacade = SWLOR.Game.Server.Service.Combat;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// What is left of <c>Combat</c> after its parts were split out (<c>CombatService</c>): the death and exit handlers, which
/// apply the defeated-enemy effects and forget a creature's combat state in every part in a fixed order, the refresh of the
/// stat-driven trackers, the six-second facing cleanup and the damage roll. The parts and services it calls are scripted and
/// write to one log, so a test sees every call and its order; the last tests run the real parts of a container.
/// </summary>
[TestFixture]
public class CombatServiceTests
{
    private const uint Killer = 11;
    private const uint Defeated = 22;
    private const uint Gone = 44;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _log = new();
    private FakeNWScript _engine = null!;
    private CombatService _service = null!;
    private Dictionary<StatType, int> _stats = null!;
    private bool _idleChannel;

    // The forget calls, in the order RemoveStatTriggerCooldowns has made them since P2-CB1 (and TemporaryStatModifier.Clear last).
    private static string[] ForgetSequence(uint creature) => new[]
    {
        $"Resources.ForgetResourceTriggerState {creature}",
        $"Activity.ForgetActivityState {creature}",
        $"DamageTaken.ForgetDamageTakenState {creature}",
        $"Suppression.ForgetSuppressionState {creature}",
        $"Activation.ForgetActivationState {creature}",
        $"HitEffects.ForgetHitEffectState {creature}",
        $"Impact.ForgetImpactState {creature}",
        $"Activity.ForgetOpeningActivityState {creature}",
        $"Resources.ForgetAbilityStaminaCostState {creature}",
        $"AttackTiming.ForgetAttackTimingState {creature}",
        $"DamageModifiers.ForgetDamageModifierState {creature}",
        $"TemporaryStatModifier.Clear {creature}",
    };

    [SetUp]
    public void SetUp()
    {
        _log.Clear();
        _stats = new();
        _idleChannel = true;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID or Gone));
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var activity = Logged<ICombatActivityService>("Activity", out var activityCalls, nameof(ICombatActivityService.ForgetActivityState),
            nameof(ICombatActivityService.ForgetOpeningActivityState), nameof(ICombatActivityService.ScheduleIdleReadinessRefresh));
        activityCalls.On(nameof(ICombatActivityService.HasConfiguredIdleSkillAbilityChannel), args =>
        {
            _log.Add($"Activity.HasConfiguredIdleSkillAbilityChannel {args[0]}");
            return _idleChannel;
        });
        var stat = Logged<IStatService>("Stat", out var statCalls);
        statCalls.On(nameof(IStatService.GetStatAdjustment), args =>
        {
            _log.Add($"Stat.GetStatAdjustment {args[0]} {args[1]}");
            return _stats.GetValueOrDefault((StatType)args[1]!);
        });
        var temporary = Logged<ITemporaryStatModifierService>("TemporaryStatModifier", out var temporaryCalls, nameof(ITemporaryStatModifierService.Clear));
        temporaryCalls.On(nameof(ITemporaryStatModifierService.Consume), args =>
        {
            _log.Add($"TemporaryStatModifier.Consume {args[0]} {args[1]} {args[2]}");
            return 0;
        });

        _service = new CombatService(
            Logged<ICombatResourcesService>("Resources", out _, nameof(ICombatResourcesService.ForgetResourceTriggerState),
                nameof(ICombatResourcesService.ForgetAbilityStaminaCostState)),
            Logged<ICombatSuppressionService>("Suppression", out _, nameof(ICombatSuppressionService.ForgetSuppressionState),
                nameof(ICombatSuppressionService.ReconcileOverwatchStatus)),
            activity,
            Logged<ICombatAttackTimingService>("AttackTiming", out _, nameof(ICombatAttackTimingService.ForgetAttackTimingState)),
            Logged<ICombatDamageModifiersService>("DamageModifiers", out _, nameof(ICombatDamageModifiersService.ForgetDamageModifierState),
                nameof(ICombatDamageModifiersService.ClearRangedRepeatedTargetDamageTracker),
                nameof(ICombatDamageModifiersService.ClearAutoAttackCycleCriticalRateTracker)),
            Logged<ICombatHitEffectsService>("HitEffects", out _, nameof(ICombatHitEffectsService.ForgetHitEffectState),
                nameof(ICombatHitEffectsService.ApplyDefeatedEnemyEffects), nameof(ICombatHitEffectsService.ApplyRecentDamagerDefeatedEnemyEffects)),
            Logged<ICombatDamageTakenService>("DamageTaken", out _, nameof(ICombatDamageTakenService.ForgetDamageTakenState)),
            Logged<ICombatAbilityActivationService>("Activation", out _, nameof(ICombatAbilityActivationService.ForgetActivationState)),
            Logged<ICombatAbilityImpactService>("Impact", out _, nameof(ICombatAbilityImpactService.ForgetImpactState)),
            stat,
            Logged<IStatusEffectService>("StatusEffect", out _, nameof(IStatusEffectService.RemoveStatusEffect)),
            temporary);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    // A stand-in whose listed members write "<label>.<member> <arguments>" to the log and return nothing; any other member
    // throws unless the test scripts it on the returned proxy, so a call the service is not expected to make fails the test.
    private TContract Logged<TContract>(string label, out ScriptedProxy proxy, params string[] members)
        where TContract : class
    {
        var contract = ScriptedProxy.Create<TContract>(out proxy);
        foreach (var member in members)
        {
            proxy.On(member, args =>
            {
                _log.Add($"{label}.{member} {string.Join(" ", args.Select(arg => arg is Type type ? type.Name : arg))}");
                return null;
            });
        }

        return contract;
    }

    // ------------------------------------------------------------------ death and exit

    [Test]
    public void TheDeathHandlerAppliesTheKillersAndTheRecentDamagersEffectsThenForgetsTheDefeatedInEveryPartInOrder()
    {
        _engine.SetReturn("OBJECT_SELF", Defeated);
        _engine.SetReturn("GetLastKiller", Killer);

        _service.ApplyDefeatedEnemyEffects();

        _log.Should().Equal(new[]
        {
            $"HitEffects.ApplyDefeatedEnemyEffects {Killer} {Defeated}",
            $"HitEffects.ApplyRecentDamagerDefeatedEnemyEffects {Defeated}",
        }.Concat(ForgetSequence(Defeated)));
    }

    [TestCase(Gone, Description = "the killer is gone")]
    [TestCase(Defeated, Description = "the defeated creature killed itself")]
    public void TheDeathHandlerAppliesNoKillersEffectsWithoutAnotherValidKiller(uint killer)
    {
        _engine.SetReturn("OBJECT_SELF", Defeated);
        _engine.SetReturn("GetLastKiller", killer);

        _service.ApplyDefeatedEnemyEffects();

        _log.Should().Equal(new[] { $"HitEffects.ApplyRecentDamagerDefeatedEnemyEffects {Defeated}" }.Concat(ForgetSequence(Defeated)));
    }

    [Test]
    public void TheExitHandlerForgetsTheExitingCreatureInEveryPartInOrder()
    {
        _engine.SetReturn("GetExitingObject", Defeated);

        _service.ClearStatTriggerState();

        _log.Should().Equal(ForgetSequence(Defeated));
    }

    [Test]
    public void TheExitHandlerForgetsNothingForAnInvalidObject()
    {
        _engine.SetReturn("GetExitingObject", Gone);

        _service.ClearStatTriggerState();

        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ stat-driven trackers

    private static readonly StatType[] TrackerStats =
    {
        StatType.OpeningAutoAttackIdleSeconds,
        StatType.RangedRepeatedTargetDamageBonusPerHit,
        StatType.RangedRepeatedTargetDamageBonusMax,
        StatType.RangedRepeatedTargetDamageDurationSeconds,
        StatType.RangedAutoAttackCycleCriticalRateRequiredCount,
        StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment,
        StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment,
    };

    private void ConfigureEveryTracker()
    {
        foreach (var stat in TrackerStats)
            _stats[stat] = 1;
        _idleChannel = true;
    }

    [Test]
    public void RefreshDoesNothingForAnInvalidCreature()
    {
        _service.RefreshStatDrivenTrackerEffects(Gone);

        _log.Should().BeEmpty();
    }

    [Test]
    public void RefreshReconcilesOverwatchAndIdleReadinessAndKeepsEveryTrackerWhoseStatsAreConfigured()
    {
        ConfigureEveryTracker();

        _service.RefreshStatDrivenTrackerEffects(Killer);

        _log.Should().Equal(
            $"Suppression.ReconcileOverwatchStatus {Killer}",
            $"Activity.ScheduleIdleReadinessRefresh {Killer}",
            $"Stat.GetStatAdjustment {Killer} {StatType.OpeningAutoAttackIdleSeconds}",
            $"Activity.HasConfiguredIdleSkillAbilityChannel {Killer}",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedRepeatedTargetDamageBonusPerHit}",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedRepeatedTargetDamageBonusMax}",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedRepeatedTargetDamageDurationSeconds}",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedAutoAttackCycleCriticalRateRequiredCount}",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment}",
            $"Stat.GetStatAdjustment {Killer} {StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment}");
    }

    [Test]
    public void RefreshClearsEveryTrackerWhoseStatsAreGoneInOrder()
    {
        _idleChannel = false;

        _service.RefreshStatDrivenTrackerEffects(Killer);

        _log.Should().Equal(
            $"Suppression.ReconcileOverwatchStatus {Killer}",
            $"Activity.ScheduleIdleReadinessRefresh {Killer}",
            $"Stat.GetStatAdjustment {Killer} {StatType.OpeningAutoAttackIdleSeconds}",
            $"StatusEffect.RemoveStatusEffect {Killer} {nameof(OpeningAttackReadyStatusEffect)} False True",
            $"Activity.HasConfiguredIdleSkillAbilityChannel {Killer}",
            $"StatusEffect.RemoveStatusEffect {Killer} {nameof(IdleSkillAbilityReadyStatusEffect)} False True",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedRepeatedTargetDamageBonusPerHit}",
            $"DamageModifiers.ClearRangedRepeatedTargetDamageTracker {Killer}",
            $"Stat.GetStatAdjustment {Killer} {StatType.RangedAutoAttackCycleCriticalRateRequiredCount}",
            $"DamageModifiers.ClearAutoAttackCycleCriticalRateTracker {Killer}",
            $"Stat.GetStatAdjustment {Killer} {StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment}",
            $"TemporaryStatModifier.Consume {Killer} {StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment} {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"TemporaryStatModifier.Consume {Killer} {StatType.PersistentNextSkillAbilityCriticalRateSkillType} {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"TemporaryStatModifier.Consume {Killer} {StatType.PersistentNextSkillAbilityCriticalRateRangedOnly} {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"StatusEffect.RemoveStatusEffect {Killer} {nameof(CriticalRateStackTrackerStatusEffect)} False True");
    }

    [TestCase(StatType.RangedRepeatedTargetDamageBonusPerHit, "DamageModifiers.ClearRangedRepeatedTargetDamageTracker")]
    [TestCase(StatType.RangedRepeatedTargetDamageBonusMax, "DamageModifiers.ClearRangedRepeatedTargetDamageTracker")]
    [TestCase(StatType.RangedRepeatedTargetDamageDurationSeconds, "DamageModifiers.ClearRangedRepeatedTargetDamageTracker")]
    [TestCase(StatType.RangedAutoAttackCycleCriticalRateRequiredCount, "DamageModifiers.ClearAutoAttackCycleCriticalRateTracker")]
    [TestCase(StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment, "DamageModifiers.ClearAutoAttackCycleCriticalRateTracker")]
    [TestCase(StatType.OpeningAutoAttackIdleSeconds, "StatusEffect.RemoveStatusEffect")]
    [TestCase(StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment, "TemporaryStatModifier.Consume")]
    public void RefreshClearsATrackerWhenAnyOneOfItsStatsIsGone(StatType missing, string clearing)
    {
        ConfigureEveryTracker();
        _stats[missing] = 0;

        _service.RefreshStatDrivenTrackerEffects(Killer);

        var cleared = _log.Where(line => line.StartsWith(clearing + " ", StringComparison.Ordinal)).ToArray();
        cleared.Should().NotBeEmpty();
        _log.Where(line => !line.StartsWith("Stat.", StringComparison.Ordinal) && !line.StartsWith("Activity.", StringComparison.Ordinal) &&
                           !line.StartsWith("Suppression.", StringComparison.Ordinal))
            .Should().OnlyContain(line => line.StartsWith(clearing + " ", StringComparison.Ordinal) ||
                                          (clearing == "TemporaryStatModifier.Consume" && line.StartsWith("StatusEffect.RemoveStatusEffect ", StringComparison.Ordinal)),
                "only the tracker whose stat is gone is cleared");
    }

    [Test]
    public void RefreshRemovesTheIdleSkillAbilityReadyStatusOnlyWithoutAConfiguredChannel()
    {
        ConfigureEveryTracker();
        _idleChannel = false;

        _service.RefreshStatDrivenTrackerEffects(Killer);

        _log.Where(line => line.StartsWith("StatusEffect.", StringComparison.Ordinal))
            .Should().Equal($"StatusEffect.RemoveStatusEffect {Killer} {nameof(IdleSkillAbilityReadyStatusEffect)} False True");
    }

    // ------------------------------------------------------------------ the six-second cleanup

    [TestCase(false, 2)]
    [TestCase(true, 0)]
    public void TheSixSecondHandlerDeletesTheSavedFacingOnlyOutOfCombat(bool inCombat, int deletes)
    {
        _engine.SetReturn("OBJECT_SELF", Killer);
        _engine.SetReturn("GetIsInCombat", inCombat);

        _service.ClearCombatState();

        _engine.CallsTo("DeleteLocalFloat").Select(call => $"{call.Arguments[0]} {call.Arguments[1]}")
            .Should().Equal(new[] { $"{Killer} ATTACK_ORIENTATION_X", $"{Killer} ATTACK_ORIENTATION_Y" }.Take(deletes));
        _engine.CallsTo("GetIsInCombat").Single().Arguments.Should().Equal(Killer);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ the damage roll

    [TestCase(100, 45, 40, 100, 10, 0, 0)]
    [TestCase(100, 45, 40, 100, 10, 120, 5)]
    [TestCase(60, 8, 14, 90, 30, 0, 2)]
    public void TheDamageRollDrawsOneValueBetweenTheRuleBounds(int attack, int dmg, int attackStat, int defense, int defenseStat, int critical, int deltaCap)
    {
        const int seed = 4242;
        var (minimum, maximum) = CombatRules.CalculateDamageRange(attack, dmg, attackStat, defense, defenseStat, critical, deltaCap);
        Random.SetSeed(seed);
        var expected = (int)Random.NextFloat(minimum, maximum);
        var next = Random.Next();

        Random.SetSeed(seed);
        _service.CalculateDamage(attack, dmg, attackStat, defense, defenseStat, critical, deltaCap).Should().Be(expected);
        Random.Next().Should().Be(next, "the roll draws exactly one value from the shared generator");
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheDamageRollCapsTheStatDeltaAtZeroByDefault()
    {
        const int seed = 99;
        Random.SetSeed(seed);
        var withDefault = _service.CalculateDamage(100, 45, 40, 100, 10, 0);
        Random.SetSeed(seed);

        _service.CalculateDamage(100, 45, 40, 100, 10, 0, 0).Should().Be(withDefault);
    }

    // ------------------------------------------------------------------ the real parts

    [Test]
    public void TheContainerBuildsTheServiceFromItsPartsAndTheExitHandlerClearsTheirTablesThroughCombat()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        const uint creature = 0xFFFFFFF7;
        const uint other = 0xFFFFFFF8;
        _engine.SetReturn("GetExitingObject", creature);

        facades.Get<ICombatService>().Should().BeOfType<CombatService>();
        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");
        var attacks = CombatMembers.FieldValue<Dictionary<uint, DateTime>>("_lastAttackActivity");
        var cooldowns = CombatMembers.FieldValue<Dictionary<(uint, StatType), DateTime>>("_statTriggerCooldowns");
        var recentTargets = CombatMembers.FieldValue<System.Collections.IDictionary>("_recentDamageTargets");
        debts[creature] = 0.5f;
        debts[other] = 0.25f;
        attacks[creature] = DateTime.UtcNow;
        cooldowns[(creature, StatType.SideAttackSkillType)] = DateTime.UtcNow;
        recentTargets[(other, creature)] = DateTime.UtcNow;

        CombatFacade.ClearStatTriggerState();

        debts.Keys.Should().Equal(other);
        attacks.Should().BeEmpty();
        cooldowns.Should().BeEmpty();
        recentTargets.Count.Should().Be(0, "a record where the creature was the target is forgotten too");
    }

    [Test]
    public void TheDamageRollThroughCombatIsTheServicesRoll()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        const int seed = 7;

        Random.SetSeed(seed);
        var direct = facades.Get<ICombatService>().CalculateDamage(100, 45, 40, 100, 10, 0, 5);
        Random.SetSeed(seed);

        CombatFacade.CalculateDamage(100, 45, 40, 100, 10, 0, 5).Should().Be(direct);
    }
}
