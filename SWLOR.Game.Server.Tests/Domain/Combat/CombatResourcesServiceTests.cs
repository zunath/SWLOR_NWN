using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The FP, stamina and healing bookkeeping of <c>Combat</c>: stat trigger cooldowns, damage-derived healing and its
/// per-hit and per-window caps, the stamina a hostile ability paid and what its hits may refund, and the riders that
/// restored FP or stamina grant. The engine and the object plugin are recording fakes, the clock is held still, and the
/// converted services, the temporary stat modifiers included, are scripted proxies over tables the test fills.
/// </summary>
[TestFixture]
public class CombatResourcesServiceTests
{
    private const uint Creature = 11;
    private const uint Target = 22;
    private const uint OtherTarget = 23;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeObjectPlugin _objects = null!;
    private CombatResourcesService _service = null!;
    private Dictionary<(uint, StatType, StatType), int> _temporary = null!;
    private List<string> _temporaryCalls = null!;
    private DateTime _now;

    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<uint, int> _remainingTargetHP = null!;
    private AbilityImpactSequence? _sequence;
    private AbilityImpactSummary? _activeSummary;
    private IReadOnlyList<StatAdjustmentSource> _impactSources = null!;
    private int _maxHP;
    private int _currentHP;
    private int _maxFP;
    private int _currentFP;
    private int _maxStamina;
    private int _currentStamina;
    private ScriptedProxy _statCalls = null!;
    private ScriptedProxy _statusCalls = null!;
    private ScriptedProxy _recorderCalls = null!;
    private ScriptedProxy _queryCalls = null!;

    [SetUp]
    public void SetUp()
    {
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _stats = new();
        _remainingTargetHP = new();
        _sequence = null;
        _activeSummary = null;
        _impactSources = Array.Empty<StatAdjustmentSource>();
        _maxHP = 200;
        _currentHP = 100;
        _maxFP = 50;
        _currentFP = 40;
        _maxStamina = 60;
        _currentStamina = 50;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != 0);
        _engine.SetReturnHandler("GetMaxHitPoints", _ => _maxHP);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => _currentHP);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _objects = new FakeObjectPlugin();
        _objects.SetReturnHandler("GetCurrentHitPoints", _ => _currentHP);
        _scopes.Add(ObjectPluginRuntime.Override(_objects));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out _queryCalls);
        _queryCalls.On(nameof(ICombatQueriesService.GetAbilitySkillType), args => ((AbilityDetail?)args[1])?.SkillType ?? SkillType.Invalid);
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityService.GetRemainingDamageTargetHP), args => _remainingTargetHP.GetValueOrDefault((uint)args[1]!))
            .On(nameof(IAbilityService.GetAbilityImpactSequence), _ => _sequence)
            .On(nameof(IAbilityService.GetAbilityImpactStatSources), _ => _impactSources)
            .On(nameof(IAbilityService.GetActiveAbilityImpactSummary), _ => _activeSummary);
        var stat = ScriptedProxy.Create<IStatService>(out _statCalls);
        _statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.ApplyHealingReceivedAdjustment), args => (int)args[1]!)
            .On(nameof(IStatService.CalculateEffectiveHealingAmount), args => Math.Min((int)args[0]!, (int)args[2]! - (int)args[1]!))
            .On(nameof(IStatService.ApplyHealingReceivedEffects), _ => null)
            .On(nameof(IStatService.RestoreFP), args => (int)args[1]!)
            .On(nameof(IStatService.RestoreStamina), args => (int)args[1]!)
            .On(nameof(IStatService.ReduceFP), _ => null)
            .On(nameof(IStatService.ReduceStamina), _ => null)
            .On(nameof(IStatService.GetMaxFP), _ => _maxFP)
            .On(nameof(IStatService.GetCurrentFP), _ => _currentFP)
            .On(nameof(IStatService.GetMaxStamina), _ => _maxStamina)
            .On(nameof(IStatService.GetCurrentStamina), _ => _currentStamina);
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out _statusCalls);
        _statusCalls.On(nameof(IStatusEffectService.ApplyStatusEffect), _ => true);
        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out _recorderCalls);
        _recorderCalls
            .On(nameof(ICombatRecorderService.BeginRiderAttribution), _ => default(CombatRecordAttributionScope))
            .On(nameof(ICombatRecorderService.BeginStatTriggerAttribution), _ => default(CombatRecordAttributionScope));

        _temporary = new();
        _temporaryCalls = new();
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls
            .On(nameof(ITemporaryStatModifierService.GetStatAdjustment), args =>
                _temporary.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!, (StatType)args[2]!)))
            .On(nameof(ITemporaryStatModifierService.Consume), args =>
            {
                _temporaryCalls.Add($"Consume {args[0]} {args[1]} {args[2]}");
                _temporary.Remove(((uint)args[0]!, (StatType)args[1]!, (StatType)args[2]!), out var value);
                return value;
            })
            .On(nameof(ITemporaryStatModifierService.Replace), args =>
            {
                _temporaryCalls.Add($"Replace {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}");
                return null;
            });

        _service = new CombatResourcesService(queries, ability, stat, statusEffect, recorder, temporary);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private void Stat(StatType stat, int value) => _stats[(Creature, stat)] = value;

    private IEnumerable<int> Heals() => _engine.CallsTo("EffectHeal").Select(call => (int)call.Arguments[0]!);

    private IEnumerable<(string Member, object?[] Arguments)> StatCalls(string member) =>
        _statCalls.Calls.Where(call => call.Member == member);

    private static AbilityDetail StaminaAbility(SkillType skill = SkillType.Rifle) => new()
    {
        IsHostileAbility = true,
        SkillType = skill,
        Requirements = new List<IAbilityActivationRequirement> { new AbilityRequirementStamina(10) },
    };

    [Test]
    public void AStatTriggerWaitsOutItsCooldownPerCreatureAndStat()
    {
        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 0).Should().BeTrue();
        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 0).Should().BeTrue("no cooldown never blocks");

        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 10).Should().BeTrue();
        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, TimeSpan.FromSeconds(3)).Should().BeFalse();
        _service.TryUseStatTrigger(Target, StatType.SideAttackSkillType, 10).Should().BeTrue("each creature has its own cooldown");
        _service.TryUseStatTrigger(Creature, StatType.PhysicalDamageImmunity, 10).Should().BeTrue("each stat has its own cooldown");

        _now = _now.AddSeconds(9.9);
        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 10).Should().BeFalse();
        _now = _now.AddSeconds(0.1);
        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 10).Should().BeTrue();
    }

    [Test]
    public void ForgettingTheTriggerStateClearsTheCreaturesCooldownsAndBudgets()
    {
        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 10);
        _service.TryUseStatTrigger(Target, StatType.SideAttackSkillType, 10);
        _service.HealFromMaxHP(Creature, 12);
        Heals().Should().Equal(24);

        _service.ForgetResourceTriggerState(Creature);

        _service.TryUseStatTrigger(Creature, StatType.SideAttackSkillType, 10).Should().BeTrue();
        _service.TryUseStatTrigger(Target, StatType.SideAttackSkillType, 10).Should().BeFalse("another creature keeps its cooldown");
        _service.HealFromMaxHP(Creature, 12);
        Heals().Should().Equal(24, 24);
    }

    [Test]
    public void HealingFromMaxHPSharesTheDefeatedEnemyWindow()
    {
        _service.HealFromMaxHP(Creature, 0);
        _service.HealFromMaxHP(Creature, 10);
        _service.HealFromMaxHP(Creature, 10);

        Heals().Should().Equal(new[] { 20, 4 }, "the window allows 12% of 200 HP");
        StatCalls(nameof(IStatService.ApplyHealingReceivedEffects)).Select(call => call.Arguments[1]).Should().Equal(20, 4);
        _engine.CallsTo("ApplyEffectToObject").Should().HaveCount(2);

        _service.HealFromMaxHP(Creature, 10);
        Heals().Should().HaveCount(2, "the window is spent");
        _now = _now.AddSeconds(CombatResourcesService.CombatHealingWindowSeconds);
        _service.HealFromMaxHP(Creature, 10);
        Heals().Should().Equal(20, 4, 20);
    }

    [Test]
    public void CrossingAnHPThresholdComparesTheHPBeforeAndAfterTheDamage()
    {
        _currentHP = 90;
        _service.DidCrossHPThreshold(Creature, 20, 50).Should().BeTrue("110 of 200 HP fell to 90, below 100");
        _service.DidCrossHPThreshold(Creature, 5, 50).Should().BeFalse("95 was already below 100");
        _service.DidCrossHPThreshold(Creature, 20, 40).Should().BeFalse("90 is still above 80");
        _service.DidCrossHPThreshold(Creature, 20, 46).Should().BeTrue();
        _currentHP = 0;
        _service.DidCrossHPThreshold(Creature, 200, 50).Should().BeFalse("a dead creature crosses nothing");
        _currentHP = 90;
        _maxHP = 0;
        _service.DidCrossHPThreshold(Creature, 20, 46).Should().BeFalse();
    }

    [Test]
    public void DamageDerivedHealingNeedsAnOpenScope()
    {
        _service.ApplyDamageDerivedHealing(Creature, 50, 50).Should().Be(0);

        using (_service.BeginDamageDerivedHealing(Creature, Target))
        {
            _service.ApplyDamageDerivedHealing(Creature, 0, 50).Should().Be(0);
            _service.ApplyDamageDerivedHealing(Creature, 50, 0).Should().Be(0);
            _service.ApplyDamageDerivedHealing(Creature, 50, 50).Should().Be(0, "a target with no HP left gives no damage to heal from");
        }

        Heals().Should().BeEmpty();
    }

    [Test]
    public void PassiveHealingIsCappedByTheTargetsHPPerHitAndPerWindow()
    {
        _remainingTargetHP[Target] = 30;
        int first;
        using (_service.BeginDamageDerivedHealing(Creature, Target))
        {
            first = _service.ApplyDamageDerivedHealing(Creature, 80, 50);
            _service.ApplyDamageDerivedHealing(Creature, 80, 50).Should().Be(0, "the hit's passive allowance is spent");
        }

        first.Should().Be(12, "half of the 30 HP the target had is 15, and a passive hit heals at most 6% of 200");
        _recorderCalls.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Creature, CombatRecordSourceKind.Lifesteal);
        Heals().Should().Equal(12);

        _remainingTargetHP[Target] = 1000;
        for (var hit = 0; hit < 4; hit++)
        {
            using (_service.BeginDamageDerivedHealing(Creature, Target))
                _service.ApplyDamageDerivedHealing(Creature, 100, 100);
        }

        Heals().Sum().Should().Be(48, "a window heals at most 24% of 200");
    }

    [Test]
    public void CooldownHealingHasItsOwnPerHitCap()
    {
        _remainingTargetHP[Target] = 1000;
        using (_service.BeginDamageDerivedHealing(Creature, Target))
        {
            _service.ApplyDamageDerivedHealing(Creature, 100, 100, passiveHealingCooldownSeconds: 5).Should().Be(24);
            _service.ApplyDamageDerivedHealing(Creature, 100, 100).Should().Be(12, "the passive cap is separate");
        }
    }

    [Test]
    public void ActivatedHealingSharesTheCastsAllowanceOrItsOwn()
    {
        _remainingTargetHP[Target] = 1000;
        using (_service.BeginDamageDerivedHealing(Creature, Target))
        {
            _service.ApplyDamageDerivedHealing(Creature, 100, 20, isActivatedHealing: true).Should().Be(20);
            _service.ApplyDamageDerivedHealing(Creature, 100, 20, isActivatedHealing: true).Should().Be(10, "15% of 200 per scope");
        }

        _sequence = new AbilityImpactSequence();
        _sequence.TakeDamageDerivedHealing(_maxHP, 25);
        using (_service.BeginDamageDerivedHealing(Creature, Target))
            _service.ApplyDamageDerivedHealing(Creature, 100, 20, isActivatedHealing: true).Should().Be(5, "the cast already used 25 of 30");
    }

    [Test]
    public void HealingStopsAtFullHPAndTheCapOfTheDamageDealt()
    {
        _remainingTargetHP[Target] = 1000;
        _currentHP = 195;
        using (_service.BeginDamageDerivedHealing(Creature, Target))
            _service.ApplyDamageDerivedHealing(Creature, 100, 100).Should().Be(5);

        _currentHP = 0;
        using (_service.BeginDamageDerivedHealing(Creature, Target))
            _service.ApplyDamageDerivedHealing(Creature, 10, 100).Should().Be(5, "at most half the damage dealt heals");
    }

    [Test]
    public void ScopesNestForTheSameTargetAndStackForAnother()
    {
        _remainingTargetHP[Target] = 40;
        _remainingTargetHP[OtherTarget] = 1000;
        var outer = _service.BeginDamageDerivedHealing(Creature, Target);
        var sameTarget = _service.BeginDamageDerivedHealing(Creature, Target);
        _remainingTargetHP[Target] = 10;
        _service.RefreshDamageDerivedHealingTargetHP(Creature, Target);
        sameTarget.Dispose();
        sameTarget.Dispose();

        var other = _service.BeginDamageDerivedHealing(Creature, OtherTarget);
        _service.ApplyDamageDerivedHealing(Creature, 100, 10).Should().Be(10, "the inner scope reads its own target");
        other.Dispose();

        _service.ApplyDamageDerivedHealing(Creature, 100, 100).Should().Be(5,
            "the outer scope is back, with the HP refreshed before any damage: half of 10");
        _remainingTargetHP[Target] = 1000;
        _service.RefreshDamageDerivedHealingTargetHP(Creature, Target);
        _service.RefreshDamageDerivedHealingTargetHP(Creature, OtherTarget);
        _service.ApplyDamageDerivedHealing(Creature, 100, 100).Should().Be(0, "the snapshot froze when healing started");
        outer.Dispose();

        _service.ApplyDamageDerivedHealing(Creature, 100, 100).Should().Be(0, "every scope is closed");
        _service.RefreshDamageDerivedHealingTargetHP(Creature, Target);
    }

    [Test]
    public void StatTriggeredResourceChangesAreCreditedToTheirStat()
    {
        _service.RestoreFPFromStat(Creature, StatType.SideAttackSkillType, 4).Should().Be(4);
        _service.RestoreStaminaFromStat(Creature, StatType.PhysicalDamageImmunity, 5).Should().Be(5);
        _service.ReduceFPFromStat(Creature, StatType.SideAttackSkillType, 2);
        _service.ReduceStaminaFromStat(Creature, StatType.PhysicalDamageImmunity, 3);

        _recorderCalls.Calls.Select(call => call.Arguments[1]).Should().Equal(
            StatType.SideAttackSkillType, StatType.PhysicalDamageImmunity, StatType.SideAttackSkillType, StatType.PhysicalDamageImmunity);
        StatCalls(nameof(IStatService.ReduceFP)).Single().Arguments[1].Should().Be(2);
        StatCalls(nameof(IStatService.ReduceStamina)).Single().Arguments[1].Should().Be(3);
    }

    [Test]
    public void AHostileStaminaAbilityRemembersWhatItPaidForThirtyFiveSeconds()
    {
        var ability = StaminaAbility();
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 12);

        _service.GetAbilityStaminaPaid(Creature, ability).Should().Be(12);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(12);
        _service.TryGetAbilityStaminaCostState(Creature, ability, out var state).Should().BeTrue();
        state.Cost.Should().Be(12);
        state.SpentAt.Should().Be(_now);

        _now = _now.AddSeconds(35);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(12);
        _now = _now.AddSeconds(1);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(0, "the record expired and is gone");
        _now = _now.AddSeconds(-1);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(0);
        _service.GetAbilityStaminaPaid(Creature, null!).Should().Be(0);
    }

    [Test]
    public void OnlyAValidCreaturesHostileAbilityIsTrackedAndAFreeCastClearsIt()
    {
        var ability = StaminaAbility();
        _service.ApplyAbilityStaminaCostFPRestore(0u, ability, 12);
        _service.GetPaidAbilityStaminaCost(0u, ability).Should().Be(0);
        var friendly = new AbilityDetail { SkillType = SkillType.Rifle };
        _service.ApplyAbilityStaminaCostFPRestore(Creature, friendly, 12);
        _service.GetPaidAbilityStaminaCost(Creature, friendly).Should().Be(0);
        _service.ApplyAbilityStaminaCostFPRestore(Creature, null!, 12);

        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 12);
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 0);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(0);
    }

    [Test]
    public void ANonCriticalRangedCostIsKeptApartAndRefundedOnACritical()
    {
        Stat(StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment, 3);
        var ability = StaminaAbility();
        _service.GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(Creature, ability).Should().Be(3);
        _service.GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(Creature, StaminaAbility(SkillType.Vibroblade)).Should().Be(0);
        _service.GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(Creature, new AbilityDetail { SkillType = SkillType.Rifle }).Should().Be(0);

        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 12);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(9);
        _service.GetAbilityStaminaPaid(Creature, ability).Should().Be(12);

        _service.RefundCriticalRangedAbilityStaminaCost(Creature, ability).Should().Be(3);
        _service.RefundCriticalRangedAbilityStaminaCost(Creature, ability).Should().Be(0, "it refunds once");
        _service.RefundCriticalRangedAbilityStaminaCost(Creature, StaminaAbility()).Should().Be(0);
        _recorderCalls.Calls.Single().Arguments.Should().Equal(Creature, StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment);
    }

    [Test]
    public void StaminaSpentOnAMatchingSkillRestoresFPAndItsRiders()
    {
        Stat(StatType.AbilityStaminaCostFPRestorePercentSkillType, (int)SkillType.Rifle);
        Stat(StatType.AbilityStaminaCostFPRestorePercent, 50);
        Stat(StatType.AbilityRestoredFPHastePercentAdjustment, 10);
        Stat(StatType.AbilityRestoredFPHasteDurationSeconds, 6);

        _service.ApplyAbilityStaminaCostFPRestore(Creature, StaminaAbility(), 12);

        StatCalls(nameof(IStatService.RestoreFP)).Single().Arguments[1].Should().Be(6);
        var status = _statusCalls.Calls.Single();
        status.Arguments[2].Should().BeOfType<EnergizedFormsHasteStatusEffect>();
        status.Arguments[3].Should().Be(6f);

        _service.ApplyAbilityStaminaCostFPRestore(Creature, StaminaAbility(SkillType.Pistol), 12);
        Stat(StatType.AbilityStaminaCostFPRestorePercent, 1);
        _service.ApplyAbilityStaminaCostFPRestore(Creature, StaminaAbility(), 12);
        StatCalls(nameof(IStatService.RestoreFP)).Should().HaveCount(1, "a pistol does not match and 1% of 12 rounds to nothing");
    }

    [Test]
    public void FPSpentOnAMatchingSkillRestoresStamina()
    {
        Stat(StatType.AbilityFPCostStaminaRestorePercentSkillType, (int)SkillType.Force);
        Stat(StatType.AbilityFPCostStaminaRestorePercent, 25);
        var force = new AbilityDetail { SkillType = SkillType.Force };

        _service.ApplyAbilityFPCostStaminaRestore(Creature, force, 0);
        _service.ApplyAbilityFPCostStaminaRestore(Creature, null!, 8);
        _service.ApplyAbilityFPCostStaminaRestore(Creature, new AbilityDetail { SkillType = SkillType.Rifle }, 8);
        _service.ApplyAbilityFPCostStaminaRestore(Creature, force, 3);
        _service.ApplyAbilityFPCostStaminaRestore(Creature, force, 8);

        StatCalls(nameof(IStatService.RestoreStamina)).Select(call => call.Arguments[1]).Should().Equal(2);
        _recorderCalls.Calls.Single().Arguments[1].Should().Be(StatType.AbilityFPCostStaminaRestorePercent);
    }

    [Test]
    public void HitStaminaRefundsKeepAtLeastOneStaminaOfTheCost()
    {
        var ability = StaminaAbility();
        _service.RestoreAbilityHitStamina(Creature, ability, 5).Should().Be(0, "nothing was paid");
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 10);

        _service.RestoreAbilityHitStamina(Creature, ability, 0).Should().Be(0);
        _service.RestoreAbilityHitStamina(Creature, ability, 6).Should().Be(6);
        _service.RestoreAbilityHitStamina(Creature, ability, 6, sendFeedback: false).Should().Be(3);
        _service.RestoreAbilityHitStamina(Creature, ability, 6).Should().Be(0);
        _service.RestoreAbilityHitStamina(Creature, ability, 6, retainMinimumCost: false).Should().Be(1);
        StatCalls(nameof(IStatService.RestoreStamina)).Select(call => call.Arguments[3]).Should().Equal(true, false, true);
    }

    [Test]
    public void AnAbilityWithoutAStaminaCostRestoresInFull()
    {
        _service.RestoreAbilityHitStamina(Creature, new AbilityDetail { IsHostileAbility = true }, 7).Should().Be(7);
        _service.RestoreAbilityHitStamina(Creature, null!, 7).Should().Be(7);
        _service.RestoreAbilityHitStamina(Creature, new AbilityDetail(), 7, sendFeedback: false).Should().Be(7);
        StatCalls(nameof(IStatService.RestoreStamina)).Select(call => call.Arguments[3]).Should().Equal(true, true, false);
    }

    [Test]
    public void StatTriggeredHitStaminaUsesTheAbilityGivenOrTheActiveImpact()
    {
        var ability = StaminaAbility();
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 10);
        _activeSummary = new AbilityImpactSummary { Ability = ability };

        _service.RestoreAbilityHitStaminaFromStat(Creature, StatType.SideAttackSkillType, 4).Should().Be(4);
        _service.RestoreAbilityHitStaminaFromStat(Creature, StatType.PhysicalDamageImmunity, 4, ability).Should().Be(4);
        _service.RestoreAbilityHitStaminaFromStat(Creature, StatType.PhysicalDamageImmunity, 4, ability).Should().Be(1);
        _recorderCalls.Calls.Select(call => call.Arguments[1]).Should().Equal(
            StatType.SideAttackSkillType, StatType.PhysicalDamageImmunity, StatType.PhysicalDamageImmunity);

        _activeSummary = null;
        _service.RestoreAbilityHitStaminaFromStat(Creature, StatType.SideAttackSkillType, 4).Should().Be(4, "no active impact restores in full");
    }

    [Test]
    public void ADeferredImpactKeepsTheCostUntilItCompletes()
    {
        var ability = StaminaAbility();
        _service.DeferAbilityStaminaCostContext(Creature, ability);
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 10);
        _service.DeferAbilityStaminaCostContext(Creature, ability);
        _service.DeferAbilityStaminaCostContext(Creature, ability);

        _service.CompleteAbilityStaminaCostContext(Creature, ability);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(10, "two impacts are still pending");
        _service.CompleteDeferredAbilityStaminaCostContext(Creature, ability);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(10);
        _service.CompleteDeferredAbilityStaminaCostContext(Creature, ability);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(0);

        _service.CompleteDeferredAbilityStaminaCostContext(Creature, ability);
        _service.CompleteDeferredAbilityStaminaCostContext(Creature, null!);
        _service.CompleteAbilityStaminaCostContext(Creature, null!);
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 10);
        _service.CompleteAbilityStaminaCostContext(Creature, ability);
        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(0, "nothing was deferred");
    }

    [Test]
    public void ForgettingTheStaminaStateRemovesOnlyThatCreaturesRecords()
    {
        var ability = StaminaAbility();
        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 10);
        _service.ApplyAbilityStaminaCostFPRestore(Target, ability, 8);

        _service.ForgetAbilityStaminaCostState(Creature);

        _service.GetPaidAbilityStaminaCost(Creature, ability).Should().Be(0);
        _service.GetPaidAbilityStaminaCost(Target, ability).Should().Be(8);
    }

    [Test]
    public void TheNextAbilityFPCostAdjustmentAppliesToItsStoredSkillAndIsConsumedOnce()
    {
        _temporary[(Creature, StatType.NextAbilityFPCostAdjustmentSkillType, StatType.NextAbilityFPCostAdjustmentSkillType)] = (int)SkillType.Force;
        _temporary[(Creature, StatType.NextAbilityFPCostAdjustment, StatType.NextAbilityFPCostAdjustmentSkillType)] = -3;

        _service.GetNextAbilityFPCostAdjustment(Creature, SkillType.Invalid).Should().Be(0);
        _service.GetNextAbilityFPCostAdjustment(Creature, SkillType.Rifle).Should().Be(0);
        _service.GetNextAbilityFPCostAdjustment(Creature, SkillType.Force).Should().Be(-3);
        _service.ConsumeNextAbilityFPCostAdjustment(Creature, SkillType.Rifle).Should().Be(0);
        _temporaryCalls.Should().BeEmpty();

        _service.ConsumeNextAbilityFPCostAdjustment(Creature, SkillType.Force).Should().Be(-3);
        _temporaryCalls.Should().Equal(
            $"Consume {Creature} {StatType.NextAbilityFPCostAdjustment} {StatType.NextAbilityFPCostAdjustmentSkillType}",
            $"Consume {Creature} {StatType.NextAbilityFPCostAdjustmentSkillType} {StatType.NextAbilityFPCostAdjustmentSkillType}");
        _service.GetNextAbilityFPCostAdjustment(Creature, SkillType.Force).Should().Be(0);
    }

    [Test]
    public void RestoredResourceRidersNeedAValueAndADuration()
    {
        _service.ApplyFPRestoredEffects(Creature);
        _service.ApplyStaminaRestoredEffects(Creature);
        _service.ApplyAbilityRestoredFPEffects(Creature);
        _service.ApplyAbilityRestoredBothResourcesEffects(Creature);
        _statusCalls.Calls.Should().BeEmpty();
        _temporaryCalls.Should().BeEmpty();

        Stat(StatType.RestoredFPForceAttackPercentAdjustment, 5);
        Stat(StatType.RestoredFPForceAttackDurationSeconds, 4);
        Stat(StatType.RestoredStaminaAttackPercentAdjustment, 6);
        Stat(StatType.RestoredStaminaAttackDurationSeconds, 3);
        Stat(StatType.AbilityRestoredBothResourcesHastePercentAdjustment, 10);
        Stat(StatType.AbilityRestoredBothResourcesHasteDurationSeconds, 8);
        _service.ApplyFPRestoredEffects(Creature);
        _service.ApplyStaminaRestoredEffects(Creature);
        _service.ApplyAbilityRestoredBothResourcesEffects(Creature);

        _statusCalls.Calls.Select(call => call.Arguments[2]!.GetType()).Should().Equal(
            typeof(ForceLensForceAttackStatusEffect), typeof(ForceLensAttackStatusEffect));
        _statusCalls.Calls.Select(call => call.Arguments[3]).Should().Equal(4f, 3f);
        _temporaryCalls.Should().Equal(
            $"Replace {Creature} {StatType.AttackDelayReductionPercent} 10 8 {StatType.AbilityRestoredBothResourcesHastePercentAdjustment}");
    }

    [Test]
    public void FPAndStaminaThresholdsCompareWithTheMaximums()
    {
        _service.IsCurrentFPAtOrAbovePercent(Creature, 80).Should().BeTrue();
        _service.IsCurrentFPAtOrAbovePercent(Creature, 81).Should().BeFalse();
        _service.IsCurrentFPAndStaminaAbovePercent(Creature, 79).Should().BeTrue();
        _service.IsCurrentFPAndStaminaAbovePercent(Creature, 80).Should().BeFalse();
        _service.IsCurrentFPAndStaminaAbovePercent(Creature, 0).Should().BeFalse();
        _service.IsCurrentFPAndStaminaAtOrBelowPercent(Creature, 84).Should().BeTrue();
        _service.IsCurrentFPAndStaminaAtOrBelowPercent(Creature, 79).Should().BeFalse();
        _service.IsCurrentFPAndStaminaAtOrBelowPercent(Creature, 0).Should().BeFalse();

        _maxFP = 0;
        _service.IsCurrentFPAtOrAbovePercent(Creature, 10).Should().BeFalse();
        _service.IsCurrentFPAndStaminaAbovePercent(Creature, 10).Should().BeFalse();
        _service.IsCurrentFPAndStaminaAtOrBelowPercent(Creature, 90).Should().BeFalse();
    }

    [Test]
    public void TheNonCriticalRangedCostCountsAsStaminaForTheHighResourceCheck()
    {
        Stat(StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment, 5);
        var ability = StaminaAbility();
        _currentStamina = 46;
        _service.IsCurrentFPAndStaminaAbovePercent(Creature, 79, ability).Should().BeFalse();

        _service.ApplyAbilityStaminaCostFPRestore(Creature, ability, 12);
        _service.IsCurrentFPAndStaminaAbovePercent(Creature, 79, ability).Should().BeTrue("the 5 held back count");
    }

    [Test]
    public void HighResourceAdjustmentsSumTheSourcesWhoseThresholdIsMet()
    {
        _impactSources = new[]
        {
            new StatAdjustmentSource("a", new Dictionary<StatType, int> { [StatType.SideAttackSkillType] = 50, [StatType.DamageTakenPercentAdjustment] = 4 }),
            new StatAdjustmentSource("b", new Dictionary<StatType, int> { [StatType.SideAttackSkillType] = 90, [StatType.DamageTakenPercentAdjustment] = 7 }),
        };

        _service.GetHighResourceStatAdjustment(Creature, StatType.DamageTakenPercentAdjustment, StatType.SideAttackSkillType, null!)
            .Should().Be(4, "FP and stamina are above 50% but not 90%");
    }
}
