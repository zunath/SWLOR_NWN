using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage roll modifiers of <c>Combat</c>: the critical roll and its mitigation, the outgoing and target-status
/// modifiers of a damage roll under the shared bonus cap, the attack and defense adjustments around it, and the
/// per-attacker stacks (auto-attack cycle, repeated-target chains, Spotter's Rhythm). Stats come from a table, the
/// status effects from a scripted status service (also behind the <c>StatusEffect</c> facade, which the melee chain's
/// status uses), the other parts are scripted, the temporary modifiers are real and the clock is held still.
/// </summary>
[TestFixture]
public class CombatDamageModifiersServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Other = 33;
    private const uint Gone = 44;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _statusLog = new();
    private readonly List<IStatusEffect> _applied = new();
    private readonly List<string> _feedback = new();
    private readonly List<string> _triggers = new();
    private FacadeContainer _facades = null!;
    private IStatusEffectService _statusEffect = null!;
    private FakeNWScript _engine = null!;
    private TemporaryStatModifierService _modifiers = null!;
    private TestDamageModifiers _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private HashSet<(uint, Type)> _statuses = null!;
    private HashSet<(uint, StatusEffectCategory)> _categories = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _players = null!;
    private Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    private Dictionary<StatType, int> _statusSource = null!;
    private int _abilityScore;
    private int _maxFP;
    private int _currentFP;
    private bool _sideAttack;
    private bool _backAttack;
    private bool _immune;
    private bool _weaponOrForce;
    private bool _longRange;
    private bool _nearby;
    private bool _area;
    private bool _singleTarget;
    private bool _targetHasCategory;
    private bool _triggerReady;
    private int _partySource;
    private int _sourceApplied;
    private int _abilitySourceApplied;
    private int _abilitySourceAppliedBonus;
    private int _skillSelected;
    private int _highResource;
    private int _rangedIgnore;
    private DeflectionSource _deflectionSource;

    private sealed class TestDamageModifiers : CombatDamageModifiersService
    {
        public Func<uint, DeflectionSource, float, bool> Deflection { get; set; } = (_, _, _) => false;
        public List<string> DeflectionCalls { get; } = new();

        public TestDamageModifiers(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IPlayerFeedbackService playerFeedback)
            : base(queries, resources, stat, statusEffect, temporaryStatModifier, ability, playerFeedback, FacadeForwarder.LazyFor<ICombatDamageTakenService>())
        {
        }

        protected override bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds)
        {
            DeflectionCalls.Add($"{creature} {source} {windowSeconds}");
            return Deflection(creature, source, windowSeconds);
        }

        public bool BaseHasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds) =>
            base.HasRecentDeflection(creature, source, windowSeconds);
    }

    [OneTimeSetUp]
    public void BuildFacades()
    {
        // One scripted status service for the fixture, so the melee chain's status (which goes through the StatusEffect
        // facade) and the part see the same statuses; its answers read this fixture's fields.
        _statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.HasStatusEffect), args => args[1] switch
            {
                Type[] types => types.Any(type => _statuses.Contains(((uint)args[0]!, type))),
                Type type => _statuses.Contains(((uint)args[0]!, type)),
                _ => false
            })
            .On(nameof(IStatusEffectService.HasStatusEffectCategory), args =>
                _categories.Contains(((uint)args[0]!, (StatusEffectCategory)args[1]!)))
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
            {
                var type = args[2] as Type ?? args[2]!.GetType();
                if (args[2] is IStatusEffect effect)
                    _applied.Add(effect);
                _statusLog.Add($"Apply {args[0]}>{args[1]} {type.Name} {args[3]}" + (args[4] is CombatDamageType damage ? $" {damage}" : string.Empty));
                _statuses.Add(((uint)args[1]!, type));
                return true;
            })
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args =>
            {
                var type = (Type)args[1]!;
                _statusLog.Add($"Remove {args[0]} {type.Name}" + (args[2] is uint source ? $" from {source}" : string.Empty));
                _statuses.Remove(((uint)args[0]!, type));
                return null;
            });
        _facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton(_statusEffect)));
    }

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // The temporary modifiers aggregate through the Stat facade; the melee chain's status goes through StatusEffect.
        _scopes.Add(_facades.Install());
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _statusLog.Clear();
        _applied.Clear();
        _feedback.Clear();
        _triggers.Clear();
        _stats = new();
        _statuses = new();
        _categories = new();
        _invalid = new();
        _players = new();
        _hitPoints = new();
        _statusSource = new();
        _abilityScore = 0;
        _maxFP = 0;
        _currentFP = 0;
        _sideAttack = _backAttack = _immune = _weaponOrForce = _longRange = _nearby = _area = _singleTarget = false;
        _targetHasCategory = false;
        _triggerReady = true;
        _partySource = _sourceApplied = _abilitySourceApplied = _abilitySourceAppliedBonus = _skillSelected = _highResource = _rangedIgnore = 0;
        _deflectionSource = DeflectionSource.None;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetMaxHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Max : 0);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : 0);
        _engine.SetReturnHandler("GetAbilityScore", _ => _abilityScore);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.HasDamageImmunity), _ => _immune)
            .On(nameof(ICombatQueriesService.IsMatchingSideAttack), _ => _sideAttack)
            .On(nameof(ICombatQueriesService.IsMatchingBackAttack), _ => _backAttack)
            .On(nameof(ICombatQueriesService.IsWeaponOrForceDamage), _ => _weaponOrForce)
            .On(nameof(ICombatQueriesService.IsRangedAbilityLongRangeTarget), _ => _longRange)
            .On(nameof(ICombatQueriesService.IsNearbyTargetWithinDistance), args => _nearby && (float)args[2]! == 8f)
            .On(nameof(ICombatQueriesService.TargetHasAnyStatusEffectCategory), _ => _targetHasCategory)
            .On(nameof(ICombatQueriesService.GetRangedAttackDefenseIgnorePercentAdjustment), _ => _rangedIgnore)
            .On(nameof(ICombatQueriesService.GetStatusSourceStatAdjustment), args => _statusSource.GetValueOrDefault((StatType)args[2]!))
            .On(nameof(ICombatQueriesService.GetStatusSourcePartyStatAdjustment), _ => _partySource)
            .On(nameof(ICombatQueriesService.GetDamageToSourceAppliedStatusTargetAdjustment), _ => _sourceApplied)
            .On(nameof(ICombatQueriesService.GetAbilityDamageToSourceAppliedStatusTargetAdjustment), _ => _abilitySourceApplied)
            .On(nameof(ICombatQueriesService.GetAbilityDamageToSourceAppliedStatusTargetBonus), _ => _abilitySourceAppliedBonus)
            .On(nameof(ICombatQueriesService.GetSkillSelectedStatAdjustment), _ => _skillSelected);
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _triggers.Add($"{args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            })
            .On(nameof(ICombatResourcesService.GetHighResourceStatAdjustment), _ => _highResource);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetMaxFP), _ => _maxFP)
            .On(nameof(IStatService.GetCurrentFP), _ => _currentFP)
            .On(nameof(IStatService.GetStatTypeDeflectionSource), _ => _deflectionSource);
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityService.IsAreaImpact), _ => _area)
            .On(nameof(IAbilityService.IsSingleTargetImpact), _ => _singleTarget);
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out var feedbackCalls);
        feedbackCalls.On(nameof(IPlayerFeedbackService.ShowDiagnosticFloatingText), args =>
        {
            _feedback.Add($"{args[1]} {args[0]}");
            return null;
        });
        _modifiers = new TemporaryStatModifierService(FacadeForwarder.For<IGuiService>());

        _service = new TestDamageModifiers(queries, resources, stat, _statusEffect, _modifiers, ability, feedback);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private void HP(uint creature, int current, int max) => _hitPoints[creature] = (current, max);

    // A percent stage's result, with the rule's float rounding (100 + 30% is 131).
    private static int Pct(int damage, int percent) => CombatRules.ApplyPercentDamageAdjustment(damage, percent);

    private IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    private static AbilityDetail Hostile(SkillType skill = SkillType.Rifle) => new() { IsHostileAbility = true, SkillType = skill };

    // ------------------------------------------------------------------ critical roll

    [Test]
    public void ANormalRollIsDrawnBetweenTheDamageRangeAndKeepsTheCritical()
    {
        var (min, max) = CombatRules.CalculateDamageRange(100, 50, 20, 80, 10, 2);
        Random.SetSeed(7);
        var expected = (int)Random.NextFloat(min, max);
        Random.SetSeed(7);

        _service.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Should().Be((expected, 2, false));
        _triggers.Should().BeEmpty("the defender has no downgrade");
    }

    [Test]
    public void APendingCriticalDowngradeForcesTheMinimumNormalDamageOnce()
    {
        _modifiers.Replace(Defender, StatType.CurrentIncomingAttackMinimumDamage, 1, 6, StatType.CurrentIncomingAttackMinimumDamage);
        var (min, _) = CombatRules.CalculateDamageRange(100, 50, 20, 80, 10, 0);

        _service.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Should().Be((min, 0, true));
        _triggers.Should().BeEmpty("the pending downgrade is used first");
        _modifiers.GetStatAdjustment(Defender, StatType.CurrentIncomingAttackMinimumDamage, StatType.CurrentIncomingAttackMinimumDamage)
            .Should().Be(0);
    }

    [Test]
    public void ADefendersCriticalDowngradeUsesItsTriggerCooldownAndForcesTheMinimum()
    {
        Stat(Defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage, 1);
        Stat(Defender, StatType.IncomingCriticalHitDowngradeCooldownMilliseconds, 4500);
        var (min, _) = CombatRules.CalculateDamageRange(100, 50, 20, 80, 10, 0, 3);

        _service.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2, 3).Should().Be((min, 0, true));
        _triggers.Should().Equal($"{Defender} {StatType.IncomingCriticalHitDowngradeToMinimumDamage} {TimeSpan.FromMilliseconds(4500)}");

        _triggerReady = false;
        _service.TryUseIncomingCriticalHitDowngrade(Defender, 2).Should().BeFalse("on cooldown");
        _service.TryUseIncomingCriticalHitDowngrade(Defender, 0).Should().BeFalse("not a critical");
        _invalid.Add(Gone);
        _service.TryUseIncomingCriticalHitDowngrade(Gone, 2).Should().BeFalse();
        _service.TryUseIncomingCriticalHitDowngrade(Other, 2).Should().BeFalse("no downgrade stat");
        _triggers.Should().HaveCount(2);
        _service.CalculateDamageWithCriticalMitigation(Gone, 100, 50, 20, 80, 10, 0).Damage.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void TheCriticalDamageModifierAddsEveryAdjustmentAndIsCapped()
    {
        _service.ApplyCriticalDamageModifier(Attacker, 100, 0).Should().Be(100, "not a critical");
        _service.ApplyCriticalDamageModifier(Attacker, 0, 2).Should().Be(0);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2).Should().Be(100, "no adjustment");

        Stat(Attacker, StatType.CriticalDamagePercentAdjustment, 10);
        Stat(Attacker, StatType.WeaponCriticalDamagePercentAdjustment, 20);
        Stat(Attacker, StatType.StaffCriticalDamagePercentAdjustment, 40);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Rifle).Should().Be(130);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Staff, criticalDamagePercentAdjustment: 5).Should().Be(175);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Force).Should().Be(110);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, criticalDamagePercentAdjustment: 500).Should().Be(300, "capped at +200%");
    }

    [Test]
    public void TheHighHPTargetCriticalDamageNeedsTheTargetAboveItsThresholdAndShowsAPlayerItsLabel()
    {
        Stat(Attacker, StatType.CriticalDamageHighHPTargetThresholdPercent, 75);
        Stat(Attacker, StatType.CriticalDamageHighHPTargetPercentAdjustment, 30);
        HP(Defender, 80, 100);

        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(130);
        _feedback.Should().BeEmpty("an NPC sees no label");
        _players.Add(Attacker);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(130);
        _feedback.Should().ContainSingle().Which.Should().Contain("High Noon +30% critical damage");

        HP(Defender, 70, 100);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "the target is below 75%");
        HP(Defender, 0, 0);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "no maximum HP");
        _invalid.Add(Gone);
        HP(Gone, 100, 100);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Gone).Should().Be(100);
        _service.ApplyCriticalDamageModifier(Gone, 100, 2, defender: Defender).Should().Be(100);
        Stat(Attacker, StatType.CriticalDamageHighHPTargetThresholdPercent, 0);
        HP(Defender, 100, 100);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "no threshold");
    }

    [Test]
    public void TheTargetStatusCriticalDamageNeedsTheConfiguredCategoryOnTheTarget()
    {
        Stat(Attacker, StatType.CriticalDamageTargetStatusPercentAdjustment, 25);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "no category");
        Stat(Attacker, StatType.CriticalDamageTargetStatusCategory, (int)StatusEffectCategory.Bleeding);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "the target is not bleeding");
        _targetHasCategory = true;
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(125);
        Stat(Attacker, StatType.CriticalDamageTargetStatusPercentAdjustment, 0);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100);
    }

    [Test]
    public void TheSkillCriticalRateAddsStaffWeaponAndLowHPRates()
    {
        Stat(Attacker, StatType.StaffCriticalRatePercentAdjustment, 4);
        Stat(Attacker, StatType.WeaponCriticalRatePercentAdjustment, 3);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Staff).Should().Be(7);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Rifle).Should().Be(3);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(0);

        Stat(Attacker, StatType.LowHPAttackThresholdPercent, 50);
        Stat(Attacker, StatType.LowHPCriticalRatePercentAdjustment, 10);
        HP(Attacker, 60, 100);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(0, "above half HP");
        HP(Attacker, 50, 100);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(10);
        HP(Attacker, 0, 0);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(0, "no maximum HP");
    }

    [Test]
    public void TheAutoAttackCriticalRateAddsTheTargetStatusRateToTheCycleRate()
    {
        _service.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5).Should().Be(5);
        Stat(Attacker, StatType.TargetStatusCriticalRatePercentAdjustment, 8);
        Stat(Attacker, StatType.TargetStatusCriticalRateStatusCategory, (int)StatusEffectCategory.Control);
        _service.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5).Should().Be(5, "the target is not controlled");
        _targetHasCategory = true;
        _service.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5).Should().Be(13);
        _service.GetTargetStatusCriticalRateAdjustment(Attacker, Defender).Should().Be(8);
        _invalid.Add(Gone);
        _service.GetTargetStatusCriticalRateAdjustment(Gone, Defender).Should().Be(0);
        _service.GetTargetStatusCriticalRateAdjustment(Attacker, Gone).Should().Be(0);
        Stat(Attacker, StatType.TargetStatusCriticalRatePercentAdjustment, 0);
        _service.GetTargetStatusCriticalRateAdjustment(Attacker, Defender).Should().Be(0);
    }

    // ------------------------------------------------------------------ attack and defense

    [Test]
    public void TheRangedDefenseIgnoreAndTheBleedingTargetAttackBonusUseTheirStats()
    {
        _rangedIgnore = 25;
        _service.ApplyRangedAttackDefenseIgnore(Attacker, 100, SkillType.Rifle).Should().Be(CombatRules.ApplyDefenseIgnore(100, 25));

        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 0, SkillType.Vibroblade).Should().Be(0);
        Stat(Attacker, StatType.AttackToBleedingTargetPercentAdjustment, 15);
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Vibroblade).Should().Be(99, "not bleeding");
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Vibroblade).Should().Be(99 + 15);
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Rifle).Should().Be(99, "vibroblades only");
        Stat(Attacker, StatType.AttackToBleedingTargetPercentAdjustment, -200);
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Vibroblade).Should().Be(0);
    }

    [Test]
    public void TheSideAndBackAttackBonusesNeedAMatchingPosition()
    {
        Stat(Attacker, StatType.SideAttackDamagePercentAdjustment, 15);
        Stat(Attacker, StatType.SideAttackHitChancePercentAdjustment, 6);
        Stat(Attacker, StatType.SideAttackCriticalRatePercentAdjustment, 7);
        Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 25);
        Stat(Attacker, StatType.BackAttackCriticalRatePercentAdjustment, 9);

        _service.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _service.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _service.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _service.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _service.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);

        _sideAttack = _backAttack = true;
        _service.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99 + 15);
        _service.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);
        _service.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(6);
        _service.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(7);
        _service.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99 + 25);
        _service.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);
        _service.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(9);

        Stat(Attacker, StatType.SideAttackDamagePercentAdjustment, 0);
        Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 0);
        _service.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _service.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _statusLog.Should().BeEmpty("no back attack was primed");
    }

    [Test]
    public void APrimedBackAttackExposesTheTargetOnceWithTheNegatedDefense()
    {
        _backAttack = true;
        Stat(Attacker, StatType.BackAttackExposedPercent, 20);
        _modifiers.Replace(Attacker, StatType.BackAttackExposedPercent, 20, 30);
        _modifiers.Replace(Attacker, StatType.BackAttackExposedDurationSeconds, 8, 30, "primer");

        _service.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 50).Should().Be(50, "only the duration is missing");
        _statusLog.Should().BeEmpty();

        Stat(Attacker, StatType.BackAttackExposedDurationSeconds, 8);
        _service.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 50).Should().Be(50);
        _statusLog.Should().Equal($"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} 8 {CombatDamageType.Physical}");
        ((ExposedStatusEffect)_applied.Single()).StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-20);
        _modifiers.GetStatAdjustment(Attacker, StatType.BackAttackExposedPercent).Should().Be(0, "both halves of the primer are consumed");
        _modifiers.GetStatAdjustment(Attacker, StatType.BackAttackExposedDurationSeconds).Should().Be(0);
    }

    [Test]
    public void ASideAttackIgnoresEvasionOnARollUnderItsScaledAndCappedChance()
    {
        _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(80, "not a side attack");
        _sideAttack = true;
        _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);
        _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(80, "no chance");

        Stat(Attacker, StatType.SideAttackEvasionIgnoreChance, 40);
        _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(80, "no ignore percent");

        Stat(Attacker, StatType.SideAttackEvasionIgnorePercent, 150);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility, (int)AbilityType.Agility + 1);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceMaximum, 100);
        _abilityScore = 60;
        Random.SetSeed(3);
        var roll = Random.D100(1);
        Random.SetSeed(3);
        _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(0, $"a chance of 100 always passes (rolled {roll}) and the ignore is capped at 100%");
        _engine.CallsTo("GetAbilityScore").Single().Arguments[1].Should().Be(AbilityType.Agility);

        Stat(Attacker, StatType.SideAttackEvasionIgnorePercent, 25);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceMaximum, 1);
        for (var seed = 0; seed < 20; seed++)
        {
            Random.SetSeed(seed);
            var expected = Random.D100(1) > 1 ? 80 : 80 - 20;
            Random.SetSeed(seed);
            _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(expected, $"seed {seed}");
        }

        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility, 0);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceMaximum, 0);
        _abilityScore = -50;
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChance, 100);
        _service.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(60, "an unset scaling ability adds nothing");
    }

    [Test]
    public void TheLowHPAndLowFPAttackBonusesNeedTheResourceAtOrBelowTheThreshold()
    {
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(0);
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(0);
        Stat(Attacker, StatType.LowHPAttackThresholdPercent, 40);
        Stat(Attacker, StatType.LowHPAttackPercentAdjustment, 12);
        Stat(Attacker, StatType.LowFPAttackThresholdPercent, 30);
        Stat(Attacker, StatType.LowFPAttackPercentAdjustment, 8);
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(0, "no maximum HP");
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(0, "no maximum FP");

        HP(Attacker, 41, 100);
        _maxFP = 100;
        _currentFP = 31;
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(0);
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(0);
        HP(Attacker, 40, 100);
        _currentFP = 30;
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(12);
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(8);
    }

    [Test]
    public void TheNearbyStatusTargetAttackCountsHostileTargetsWithTheCategoryUpToItsMaximum()
    {
        _service.GetNearbyStatusTargetAttackAdjustment(Attacker).Should().Be(0, "not configured");
        Stat(Attacker, StatType.NearbyStatusTargetAttackPercentPerTarget, 5);
        Stat(Attacker, StatType.NearbyStatusTargetAttackRadiusMeters, 10);
        Stat(Attacker, StatType.NearbyStatusTargetAttackStatusCategory, (int)StatusEffectCategory.Bleeding);
        const uint vanishing = 55;
        const uint friendly = 66;
        var vanishingChecks = 0;
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! switch
        {
            vanishing => ++vanishingChecks == 1,
            0 or NWScript.OBJECT_INVALID => false,
            var other => !_invalid.Contains(other)
        });
        _engine.SetReturnSequence("GetFirstObjectInShape", Attacker);
        _engine.SetReturnSequence("GetNextObjectInShape", vanishing, Defender, friendly, Other, 0u);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args => (uint)args[0]! != friendly);
        foreach (var creature in new[] { Attacker, Defender, friendly })
            _categories.Add((creature, StatusEffectCategory.Bleeding));

        _service.GetNearbyStatusTargetAttackAdjustment(Attacker).Should().Be(5, "the attacker, the vanished, the friendly and the unbleeding do not count");
        _engine.CallsTo("GetFirstObjectInShape").Single().Arguments[1].Should().Be(10f);

        _categories.Add((Other, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.NearbyStatusTargetAttackPercentMaximum, 7);
        _engine.SetReturnSequence("GetFirstObjectInShape", Defender);
        _engine.SetReturnSequence("GetNextObjectInShape", Other, 0u);
        _service.GetNearbyStatusTargetAttackAdjustment(Attacker).Should().Be(7, "two targets, capped at 7");
    }

    [Test]
    public void TheStatusSourceDefenseAndAccuracyModifiersUseTheSourceTables()
    {
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 0).Should().Be(0);
        _service.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 0).Should().Be(0);
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 80).Should().Be(80);
        _service.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 80).Should().Be(80);

        _statusSource[StatType.DefenseAgainstStatusSourcePercentAdjustment] = 25;
        _statusSource[StatType.AccuracyToStatusSourcePercentAdjustment] = -200;
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 80).Should().Be(100);
        _service.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 80).Should().Be(1, "accuracy never drops below 1");
        _statusSource[StatType.DefenseAgainstStatusSourcePercentAdjustment] = -200;
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 80).Should().Be(0);
    }

    // ------------------------------------------------------------------ single stages

    [Test]
    public void TheOrdinaryAttackMultiplierAndTheDamageTypeModifierReadTheirStats()
    {
        Stat(Attacker, StatType.OrdinaryAttackDamagePercentAdjustment, 50);
        _service.ApplyOrdinaryAttackDamageMultiplier(Attacker, 100).Should().Be(CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, 50)).And.Be(150);

        _service.ApplyDamageTypeDealtModifiers(Attacker, 0, CombatDamageType.Poison).Should().Be(0);
        _service.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison).Should().Be(100);
        Stat(Attacker, StatType.PoisonDamageDealtPercentAdjustment, 30);
        _service.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison).Should().Be(Pct(100, 30));
        _service.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Fire).Should().Be(100);
    }

    [Test]
    public void TheAbilityShapeModifierPicksTheSingleTargetOrAreaAdjustment()
    {
        Stat(Attacker, StatType.SingleTargetDamagePercentAdjustment, 10);
        Stat(Attacker, StatType.AreaAbilityDamagePercentAdjustment, 20);

        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, false, null!).Should().Be(110, "an auto-attack is single target");
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, Hostile()).Should().Be(100, "neither shape");
        _area = true;
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, Hostile()).Should().Be(120);
        _singleTarget = true;
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, Hostile()).Should().Be(110);
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, new AbilityDetail()).Should().Be(100, "not hostile");
    }

    [Test]
    public void TheThrowingSingleTargetModifierCoversThrowingAutoAttacksAndSingleTargetThrowingAbilities()
    {
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.ThrowingSingleTargetDamagePercentAdjustment, 15);
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!).Should().Be(Pct(100, 15));
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Rifle, 100, false, null!).Should().Be(100);
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 0, false, null!).Should().Be(0);
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, true, Hostile(SkillType.Throwing)).Should().Be(100, "an area ability");
        _singleTarget = true;
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, true, Hostile(SkillType.Throwing)).Should().Be(Pct(100, 15));
    }

    [Test]
    public void TheMimicryAndSkillAbilityModifiersApplyOnlyToTheirAbilities()
    {
        Stat(Attacker, StatType.MimicryPotencyPercent, 40);
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, true).Should().Be(Pct(100, 40));
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, false).Should().Be(100);
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(100);
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 0, SkillType.Mimicry, true).Should().Be(0);

        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(100, "no skill selected");
        Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle);
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustment, 30);
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(Pct(100, 30));
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Pistol, true).Should().Be(100);
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, false).Should().Be(100);
        _service.ApplySkillAbilityDamageModifier(Attacker, 0, SkillType.Rifle, true).Should().Be(0);
    }

    [Test]
    public void TheAbilityShapeStagesOfTwinBladeThrowingPhysicalAndSkillAreaUseTheirStats()
    {
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true).Should().Be(100);
        Stat(Attacker, StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment, 10);
        Stat(Attacker, StatType.TwinBladeAreaAbilityDamagePercentAdjustment, 20);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, false).Should().Be(110);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, false, true).Should().Be(120);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true).Should().Be(Pct(100, 30));
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.Rifle, 100, true, true).Should().Be(100);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 0, true, true).Should().Be(0);

        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true).Should().Be(100);
        Stat(Attacker, StatType.ThrowingAreaAbilityDamagePercentAdjustment, 25);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true).Should().Be(125);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, false).Should().Be(100);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(100);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 0, true).Should().Be(0);

        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true).Should().Be(100);
        Stat(Attacker, StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment, 35);
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true).Should().Be(Pct(100, 35));
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Fire, 100, true).Should().Be(100);
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, false).Should().Be(100);
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 0, true).Should().Be(0);

        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(100, "no skill selected");
        Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustment, 40);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(Pct(100, 40));
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Pistol, 100, true).Should().Be(100);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, false).Should().Be(100);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 0, true).Should().Be(0);
    }

    [Test]
    public void TheLongRangeAndAfterDeflectionStagesNeedTheirConditions()
    {
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100).Should().Be(100, "not long range");
        _longRange = true;
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.RangedAbilityLongRangeDamagePercentAdjustment, 20);
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100).Should().Be(120);
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);

        Stat(Attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, 30);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100, "no window");
        Stat(Attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds, 4);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100, "no deflection source");
        _deflectionSource = DeflectionSource.Melee;
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100, "no recent deflection");
        _service.Deflection = (_, _, _) => true;
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(Pct(100, 30), "any skill when unset");
        _service.DeflectionCalls.Should().Equal($"{Attacker} {DeflectionSource.Melee} 4", $"{Attacker} {DeflectionSource.Melee} 4");
        Stat(Attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType, (int)SkillType.Lightsaber);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Lightsaber, 100, true).Should().Be(Pct(100, 30));
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Lightsaber, 100, false).Should().Be(100);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Lightsaber, 0, true).Should().Be(0);
    }

    [Test]
    public void TheDeflectionRecordIsReadThroughCombatByDefault()
    {
        _service.BaseHasRecentDeflection(Attacker, DeflectionSource.Melee, 4f).Should().BeFalse("nothing deflected");
        _service.BaseHasRecentDeflection(Attacker, DeflectionSource.None, 4f).Should().BeFalse();
    }

    // ------------------------------------------------------------------ the whole roll

    private int Dealt(
        out int targetStatus,
        int damage = 100,
        SkillType skill = SkillType.Invalid,
        CombatDamageType type = CombatDamageType.Physical,
        bool isAbility = false,
        bool canRandom = true,
        bool landed = false,
        AbilityDetail ability = null!,
        int abilityPercent = 0,
        int lowHPPercent = 0,
        bool ordinary = false) =>
        _service.ApplyDamageDealtModifiers(Attacker, Defender, damage, skill, type, isAbility, canRandom, landed, ability,
            out targetStatus, abilityPercent, lowHPPercent, ordinary);

    [Test]
    public void ARollWithNoModifiersComesThroughUnchangedAndRejectedRollsAreZero()
    {
        Dealt(out var taken).Should().Be(100);
        taken.Should().Be(0);
        Dealt(out taken, damage: 0).Should().Be(0);
        Dealt(out _, damage: -5).Should().Be(-5);
        Dealt(out _, isAbility: true, ability: Hostile(), abilityPercent: -100).Should().Be(0, "an authored zero-damage ability stays zero");
        _immune = true;
        Dealt(out taken).Should().Be(0);
        taken.Should().Be(0);
    }

    [Test]
    public void TheShortOverloadUsesItsDefaultsAndDropsTheTargetStatusAdjustment()
    {
        Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, 20);
        _service.ApplyDamageDealtModifiers(Attacker, Defender, 100).Should().Be(120);
        Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, 50);
        _service.ApplyDamageDealtModifiers(Attacker, Defender, 100, damageType: CombatDamageType.Force).Should().Be(150);
        _service.ApplyDamageDealtModifiers(Attacker, Defender, 100, isLandedAttack: false).Should().Be(120);
    }

    [Test]
    public void EachOutgoingStageOfTheRollAddsItsPercent()
    {
        void Case(string name, Action arrange, int expected, SkillType skill = SkillType.Invalid, bool isAbility = false,
            bool landed = false, CombatDamageType type = CombatDamageType.Physical)
        {
            _stats.Clear();
            _sideAttack = _backAttack = _area = _singleTarget = _longRange = _weaponOrForce = false;
            _service.Deflection = (_, _, _) => false;
            _deflectionSource = DeflectionSource.None;
            arrange();
            Dealt(out _, skill: skill, isAbility: isAbility, landed: landed, ability: isAbility ? Hostile(skill) : null!, type: type)
                .Should().Be(expected, name);
        }

        Case("side attack", () => { _sideAttack = true; Stat(Attacker, StatType.SideAttackDamagePercentAdjustment, 20); }, 120);
        Case("back attack on a landed auto-attack", () => { _backAttack = true; Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 10); }, 110, landed: true);
        Case("no back attack bonus off a landed hit", () => { _backAttack = true; Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 10); }, 100);
        Case("no back attack bonus for an ability", () => { _backAttack = true; Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 10); }, 100, isAbility: true, landed: true);
        Case("twin blade single target ability", () => { _singleTarget = true; Stat(Attacker, StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment, 10); }, 110, SkillType.TwinBlade, true);
        Case("throwing area ability", () => { _area = true; Stat(Attacker, StatType.ThrowingAreaAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Throwing, true);
        Case("skill area ability", () => { _area = true; Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle); Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("physical single target ability", () => { _singleTarget = true; Stat(Attacker, StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("area ability after a deflection", () =>
        {
            _area = true;
            _deflectionSource = DeflectionSource.Melee;
            _service.Deflection = (_, _, _) => true;
            Stat(Attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds, 4);
            Stat(Attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, 10);
        }, 110, SkillType.Lightsaber, true);
        Case("long range ability", () => { _longRange = true; Stat(Attacker, StatType.RangedAbilityLongRangeDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("mimicry potency", () => Stat(Attacker, StatType.MimicryPotencyPercent, 10), 110, SkillType.Mimicry, true);
        Case("skill ability", () => { Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle); Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("outgoing damage", () => Stat(Attacker, StatType.DamageDealtPercentAdjustment, 10), 110);
        Case("poison damage", () => Stat(Attacker, StatType.PoisonDamageDealtPercentAdjustment, 10), 110, type: CombatDamageType.Poison);
        Case("weapon or Force damage", () => { _weaponOrForce = true; Stat(Attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment, 10); }, 110);
        Case("weapon or Force damage needs a weapon or Force", () => Stat(Attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment, 10), 100);
        Case("weapon or Force damage with no adjustment", () => _weaponOrForce = true, 100);
        Case("single target auto-attack", () => Stat(Attacker, StatType.SingleTargetDamagePercentAdjustment, 10), 110);
        Case("throwing auto-attack", () => Stat(Attacker, StatType.ThrowingSingleTargetDamagePercentAdjustment, 10), 110, SkillType.Throwing);
        Case("low HP target", () =>
        {
            HP(Defender, 20, 100);
            Stat(Attacker, StatType.TargetLowHPDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPDamagePercentAdjustment, 10);
        }, 110);
        Case("low HP target with a status", () =>
        {
            HP(Defender, 20, 100);
            _targetHasCategory = true;
            Stat(Attacker, StatType.TargetLowHPStatusDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPStatusDamagePercentAdjustment, 10);
            Stat(Attacker, StatType.TargetLowHPStatusDamageStatusCategory, (int)StatusEffectCategory.Bleeding);
        }, 110);
        Case("low HP target with a status above the threshold", () =>
        {
            HP(Defender, 30, 100);
            Stat(Attacker, StatType.TargetLowHPStatusDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPStatusDamagePercentAdjustment, 10);
            Stat(Attacker, StatType.TargetLowHPStatusDamageStatusCategory, (int)StatusEffectCategory.Bleeding);
        }, 100);
        Case("low HP target with a status and no maximum HP", () =>
        {
            HP(Defender, 0, 0);
            Stat(Attacker, StatType.TargetLowHPStatusDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPStatusDamagePercentAdjustment, 10);
            Stat(Attacker, StatType.TargetLowHPStatusDamageStatusCategory, (int)StatusEffectCategory.Bleeding);
        }, 100);
    }

    [Test]
    public void EachTargetStatusOfTheRollAddsItsPercentOrFlatBonus()
    {
        void Case(string name, Action arrange, int expected, SkillType skill = SkillType.Invalid, bool isAbility = false,
            bool canRandom = true)
        {
            _stats.Clear();
            _statuses.Clear();
            _categories.Clear();
            _statusSource.Clear();
            _sourceApplied = _abilitySourceApplied = _abilitySourceAppliedBonus = _skillSelected = _highResource = 0;
            _nearby = false;
            arrange();
            Dealt(out var taken, skill: skill, isAbility: isAbility, ability: isAbility ? Hostile(skill) : null!, canRandom: canRandom)
                .Should().Be(expected, name);
            taken.Should().Be(0, name);
        }

        Case("status source", () => _statusSource[StatType.DamageToStatusSourcePercentAdjustment] = 10, 110);
        Case("sundered target", () => { _statuses.Add((Defender, typeof(SunderStatusEffect))); Stat(Attacker, StatType.DamageToSunderedTargetPercentAdjustment, 10); }, 110);
        Case("bleeding target", () => { _categories.Add((Defender, StatusEffectCategory.Bleeding)); Stat(Attacker, StatType.DamageToBleedingTargetPercentAdjustment, 10); }, 110);
        Case("bleeding target hit by an ability", () => { _categories.Add((Defender, StatusEffectCategory.Bleeding)); _skillSelected = 7; }, 107, SkillType.Rifle, true);
        Case("debuffed target", () => { _categories.Add((Defender, StatusEffectCategory.Debuff)); Stat(Attacker, StatType.DamageToDebuffedTargetPercentAdjustment, 10); }, 110);
        Case("target of the attacker's status", () => _sourceApplied = 10, 110);
        Case("target of the attacker's status hit by an ability", () => { _abilitySourceApplied = 10; _abilitySourceAppliedBonus = 3; }, 113, SkillType.Rifle, true);
        Case("katar ability on a poisoned target", () => { _statuses.Add((Defender, typeof(DisorientedStatusEffect))); Stat(Attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment, 10); }, 110, SkillType.Katar, true);
        Case("katar poison flat bonus", () =>
        {
            _statuses.Add((Defender, typeof(PoisonStatusEffect)));
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonusChance, 100);
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonus, 9);
        }, 109, SkillType.Katar);
        Case("katar poison flat bonus not rolled", () =>
        {
            _statuses.Add((Defender, typeof(PoisonStatusEffect)));
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonusChance, 100);
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonus, 9);
        }, 100, SkillType.Katar, canRandom: false);
        Case("weakened target", () => { _statuses.Add((Defender, typeof(HamstringStatusEffect))); Stat(Attacker, StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment, 10); }, 110);
        Case("controlled target", () => { _categories.Add((Defender, StatusEffectCategory.Control)); Stat(Attacker, StatType.DamageToControlTargetPercentAdjustment, 10); }, 110);
        Case("high resource ability", () => _highResource = 10, 110, SkillType.Rifle, true);
        Case("rifle on a dazed target", () => { _statuses.Add((Defender, typeof(DazedStatusEffect))); Stat(Attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment, 10); }, 110, SkillType.Rifle);
        Case("ranged weapon on a nearby target", () => { _nearby = true; Stat(Attacker, StatType.RangedDamageToNearbyTargetPercentAdjustment, 10); }, 110, SkillType.Rifle);
        Case("pistol on a knocked-down target", () => { _statuses.Add((Defender, typeof(KnockdownStatusEffect))); Stat(Attacker, StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus, 4); }, 104, SkillType.Pistol);
        Case("staff ability on a blinded target", () => { _statuses.Add((Defender, typeof(BlindStatusEffect))); Stat(Attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment, 10); }, 110, SkillType.Staff, true);
        Case("katar ability on an unpoisoned target", () => Stat(Attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment, 10), 100, SkillType.Katar, true);
    }

    [Test]
    public void TheDefendersDamageTakenAdjustmentsComeBackSeparatelyAndAreAppliedLastWithinTheCombinedBudget()
    {
        void Case(string name, Action arrange, int expectedTaken, SkillType skill = SkillType.Invalid, bool isAbility = false,
            CombatDamageType type = CombatDamageType.Physical)
        {
            _stats.Clear();
            _statusSource.Clear();
            _partySource = 0;
            arrange();
            var dealt = Dealt(out var taken, skill: skill, isAbility: isAbility, ability: isAbility ? Hostile(skill) : null!, type: type);
            taken.Should().Be(expectedTaken, name);
            dealt.Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, expectedTaken), name);
        }

        Case("taken from the status source", () => _statusSource[StatType.DamageTakenFromStatusSourcePercentAdjustment] = 10, 10);
        Case("taken from the source's party", () => _partySource = 15, 15);
        Case("physical taken", () => Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, -20), -20);
        Case("physical ability taken", () => Stat(Defender, StatType.PhysicalAbilityDamageTakenPercentAdjustment, 12), 12, SkillType.Rifle, true);
        Case("Force taken", () => Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, 25), 25, type: CombatDamageType.Force);
        Case("ranged physical taken", () => Stat(Defender, StatType.RangedPhysicalDamageTakenPercentAdjustment, 8), 8, SkillType.Rifle);
        Case("throwing taken", () => Stat(Defender, StatType.ThrowingDamageTakenPercentAdjustment, 6), 6, SkillType.Throwing);
        Case("capped at the combined reduction", () => Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, -99), -85);
    }

    [Test]
    public void ThePercentStagesAreCappedTogetherWhileFlatBonusesStackOnTop()
    {
        Stat(Attacker, StatType.DamageDealtPercentAdjustment, 150);
        Dealt(out _).Should().Be(200, "the bonus is capped at +100%");
        Dealt(out _, abilityPercent: 20, isAbility: true, ability: Hostile(), skill: SkillType.Rifle).Should().Be(200);

        _stats.Clear();
        Dealt(out _, abilityPercent: 20, lowHPPercent: 10, isAbility: true, ability: Hostile(), skill: SkillType.Rifle)
            .Should().Be(CombatRules.ApplyPercentDamageAdjustment(CombatRules.ApplyPercentDamageAdjustment(100, 20), 10));

        Stat(Attacker, StatType.OrdinaryAttackDamagePercentAdjustment, -50);
        Dealt(out _, ordinary: true).Should().Be(50);
        Dealt(out _, ordinary: true, isAbility: true, ability: Hostile(), skill: SkillType.Rifle).Should().Be(100, "abilities are not ordinary attacks");
        Dealt(out _).Should().Be(100);
    }

    // ------------------------------------------------------------------ repeated-target chains

    [Test]
    public void TheRepeatedTargetChainStacksPerHitOnOneTargetAndResetsOnAnother()
    {
        Stat(Attacker, StatType.RepeatedTargetDamageSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 10);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentMax, 25);
        Stat(Attacker, StatType.RepeatedTargetDamageBonusPerHit, 2);
        Stat(Attacker, StatType.RepeatedTargetDamageBonusMax, 3);
        Stat(Attacker, StatType.RepeatedTargetDamageDurationSeconds, 5);

        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110 + 2);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(120 + 3);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(125 + 3, "both bonuses are capped");
        Dealt(out _, skill: SkillType.Rifle).Should().Be(100, "a discarded swing neither builds nor breaks the chain");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(125 + 3, "still capped: the stacks stop at the larger cap");

        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(110 + 2, "a new target starts over");
        _now = _now.AddSeconds(6);
        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(110 + 2, "the chain expired");

        Dealt(out _, skill: SkillType.Pistol, landed: true).Should().Be(100, "the chain is the rifle's");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110 + 2, "another skill broke it");

        Stat(Attacker, StatType.RepeatedTargetDamageAutoAttackOnly, 1);
        Dealt(out _, skill: SkillType.Rifle, landed: true, isAbility: true, ability: Hostile()).Should().Be(100);
        Stat(Attacker, StatType.RepeatedTargetDamageAutoAttackOnly, 0);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(100 + 2, "only the flat bonus is left, and the ability broke the chain");
        Stat(Attacker, StatType.RepeatedTargetDamageBonusPerHit, 0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(100);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 10);
        Stat(Attacker, StatType.RepeatedTargetDamageDurationSeconds, 0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110, "a chain with no duration never expires");
        _now = _now.AddHours(1);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(120);

        _invalid.Add(Gone);
        _service.ApplyDamageDealtModifiers(Attacker, Gone, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(100);
        _service.ApplyDamageDealtModifiers(Attacker, Attacker, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(100, "a creature cannot chain on itself");
    }

    [Test]
    public void TheMeleeChainStacksOnAutoAttacksShowsItsStacksAndClearsWhenUnconfigured()
    {
        Stat(Attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit, 2);
        Stat(Attacker, StatType.MeleeRepeatedTargetDamageBonusMax, 5);
        Stat(Attacker, StatType.MeleeRepeatedTargetDamageStatusEffectIcon, (int)EffectIconType.Haste);

        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(102);
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(104);
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(105, "capped at 5");
        Dealt(out _, skill: SkillType.Vibroblade, landed: true, isAbility: true, ability: Hostile(SkillType.Vibroblade))
            .Should().Be(100, "abilities neither build nor break the chain");
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(105);
        _applied.OfType<MeleeRepeatedTargetDamageStatusEffect>().Select(effect => effect.Stacks).Should().Equal(1, 2, 3, 3);

        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Vibroblade, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(102, "a new target starts over");

        _applied.Clear();
        Dealt(out _, skill: SkillType.Force, landed: true).Should().Be(100, "not a weapon");
        _applied.OfType<MeleeRepeatedTargetDamageStatusEffect>().Should().BeEmpty("the chain's status is cleared");
        Dealt(out _, skill: SkillType.Vibroblade, landed: true).Should().Be(102, "the chain was cleared");
    }

    [Test]
    public void SustainedFireStacksOnWeaponHitsShowsTheBonusAndLetsOtherDamagePassUntouched()
    {
        _players.Add(Attacker);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusPerHit, 3);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusMax, 7);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageDurationSeconds, 4);

        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103);
        Dealt(out _, skill: SkillType.Rifle, landed: true, isAbility: true, ability: Hostile()).Should().Be(106, "ability hits count too");
        Dealt(out _, skill: SkillType.Force, landed: true).Should().Be(100, "non-weapon damage leaves the stack untouched");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(107, "capped at 7");
        _applied.OfType<SustainedFireStatusEffect>().Select(effect => $"{effect.Stacks}/{effect.MaximumStacks} +{effect.DamageBonus}")
            .Should().Equal("1/3 +3", "2/3 +6", "3/3 +7");
        FloatingText().Should().HaveCount(3).And.Contain(text => text.Contains("Sustained Fire 3/3 (+7 DMG)"));
        _statusLog.Should().Contain($"Apply {Attacker}>{Attacker} {nameof(SustainedFireStatusEffect)} 4");

        _now = _now.AddSeconds(5);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103, "the stack expired");
        _players.Remove(Attacker);
        _service.ApplyDamageDealtModifiers(Attacker, Other, 100, SkillType.Rifle, CombatDamageType.Physical, false, true, true, null!, out _)
            .Should().Be(103, "a new target starts over");
        FloatingText().Should().HaveCount(4, "an NPC sees no text");

        Stat(Attacker, StatType.RangedRepeatedTargetDamageDurationSeconds, 0);
        _statusLog.Clear();
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(100);
        _statusLog.Should().Contain($"Remove {Attacker} {nameof(SustainedFireStatusEffect)}");
    }

    [Test]
    public void ClearingTheTrackersRemovesTheirStacksAndStatuses()
    {
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusPerHit, 3);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageBonusMax, 9);
        Stat(Attacker, StatType.RangedRepeatedTargetDamageDurationSeconds, 4);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(106);

        _statusLog.Clear();
        _service.ClearRangedRepeatedTargetDamageTracker(Attacker);
        _statusLog.Should().Equal($"Remove {Attacker} {nameof(SustainedFireStatusEffect)}");
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(103, "the stack starts over");

        _statusLog.Clear();
        _service.ClearAutoAttackCycleCriticalRateTracker(Attacker);
        _statusLog.Should().Equal($"Remove {Attacker} {nameof(AttackCycleTrackerStatusEffect)}");
    }

    // ------------------------------------------------------------------ auto-attack cycle

    [Test]
    public void TheAutoAttackCycleCountsWeaponAttacksAndGrantsItsCriticalRateOnTheLastOne()
    {
        _invalid.Add(Gone);
        _service.PrepareAutoAttackCycleCriticalRate(Gone, SkillType.Rifle).Should().Be(0);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Force).Should().Be(0);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0, "not configured");
        _statusLog.Should().Equal($"Remove {Attacker} {nameof(AttackCycleTrackerStatusEffect)}");

        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRateRequiredCount, 3);
        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment, 25);
        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType, (int)EffectIconType.Haste);
        _statusLog.Clear();
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(25);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0, "the cycle starts over");
        _applied.OfType<AttackCycleTrackerStatusEffect>().Select(effect => effect.Name)
            .Should().Equal("Attack cycle: 1/3", "Attack cycle: 2/3", "Attack cycle: 3/3", "Attack cycle: 1/3");
        _statusLog.Where(line => line.StartsWith("Apply")).Select(line => line.Split(' ').Last())
            .Should().Equal(new[] { "0", "0", "3", "0" }, "the full cycle's tracker shows for 3 seconds");
        _feedback.Should().ContainSingle().Which.Should().Contain("Attack +25% Critical Rate");

        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType, 0);
        _applied.Clear();
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(25, "counted without a tracker icon");
        _applied.Should().BeEmpty();

        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _service.ForgetDamageModifierState(Attacker);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0, "forgotten: 1 of 3 again");
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _service.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(25);
    }

    // ------------------------------------------------------------------ Spotter's Rhythm

    private void ConfigurePressure(int buildSeconds = 6, int graceSeconds = 2, int readySeconds = 10, int bonus = 8)
    {
        Stat(Attacker, StatType.SameTargetPressureBuildSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.SameTargetPressureBuildSeconds, buildSeconds);
        Stat(Attacker, StatType.SameTargetPressureGraceSeconds, graceSeconds);
        Stat(Attacker, StatType.SameTargetPressureReadyDurationSeconds, readySeconds);
        Stat(Attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus, bonus);
    }

    private void Hit(uint target, SkillType skill = SkillType.Rifle, int seconds = 1)
    {
        _service.ApplySameTargetPressureDamageEffects(Attacker, target, skill);
        _now = _now.AddSeconds(seconds);
    }

    [Test]
    public void SteadyHitsOnOneTargetReadySpottersRhythmForTheNextWeaponAbility()
    {
        _players.Add(Attacker);
        ConfigurePressure();
        for (var i = 0; i < 6; i++)
            Hit(Defender);
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0, "6 seconds have not passed at the last hit");
        Hit(Defender);
        _statusLog.Should().Equal($"Apply {Attacker}>{Attacker} {nameof(SpottersRhythmStatusEffect)} 10");
        FloatingText().Should().ContainSingle().Which.Should().Contain("Spotter's Rhythm");

        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8);
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Force).Should().Be(0, "weapon abilities only");
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Other, SkillType.Rifle).Should().Be(0, "another target");
        Hit(Other, seconds: 0);
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8, "a ready state is kept while active");

        _service.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 0);
        _service.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Other, SkillType.Rifle, 50);
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8, "nothing was consumed");
        _service.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 50);
        _statusLog.Last().Should().Be($"Remove {Attacker} {nameof(SpottersRhythmStatusEffect)} from {Attacker}");
        FloatingText().Last().Should().Contain("Spotter's Rhythm (+8 DMG)");
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0);
    }

    [Test]
    public void SpottersRhythmBuildsOnlyFromItsSkillResetsAfterTheGraceAndExpiresWithItsStatus()
    {
        ConfigurePressure();
        Hit(Defender, SkillType.Force);         // 0: not a weapon
        Hit(Defender, SkillType.Pistol);        // 1: not the build skill
        Hit(Defender, seconds: 3);              // 2: the build starts
        Hit(Defender);                          // 5: three seconds is past the grace, so it starts over
        Hit(Defender, seconds: 2);              // 6
        Hit(Defender, seconds: 2);              // 8
        Hit(Defender);                          // 10
        _statusLog.Should().BeEmpty("the build restarted at 5 seconds");
        Hit(Defender, seconds: 0);              // 11: six seconds since the restart
        _statusLog.Should().ContainSingle(line => line.StartsWith("Apply"));

        Hit(Defender, SkillType.Pistol, 0);
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8);
        _statuses.Remove((Attacker, typeof(SpottersRhythmStatusEffect)));
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0, "the status was removed");
        _statusLog.Last().Should().Be($"Remove {Attacker} {nameof(SpottersRhythmStatusEffect)} from {Attacker}");

        for (var i = 0; i < 3; i++)
            Hit(Defender);                      // 11 to 13: a new build starts at 11
        Hit(Other, SkillType.Pistol, 0);        // 14: a pistol hit on another target clears it
        for (var i = 0; i < 6; i++)
            Hit(Defender);                      // 14 to 19: the build starts again at 14
        _statusLog.Count(line => line.StartsWith("Apply")).Should().Be(1, "without the reset it would have readied at 17");
        Hit(Defender);                          // 20
        _statusLog.Count(line => line.StartsWith("Apply")).Should().Be(2);

        _now = _now.AddSeconds(11);
        Hit(Defender, seconds: 0);
        _statusLog.Last().Should().StartWith("Remove", "an expired ready state is cleared before building again");
        _service.ConsumeSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle, 50);
        FloatingText().Should().BeEmpty("an NPC sees no text");
    }

    [Test]
    public void SpottersRhythmNeedsEveryStatAndAValidPairOfCreatures()
    {
        ConfigurePressure(bonus: 0);
        for (var i = 0; i < 8; i++)
            Hit(Defender);
        _invalid.Add(Gone);
        ConfigurePressure(buildSeconds: 1);
        Hit(Gone);
        Hit(Gone);
        Hit(Attacker);
        Hit(Attacker);
        _service.ApplySameTargetPressureDamageEffects(Gone, Defender, SkillType.Rifle);
        _statusLog.Should().BeEmpty();
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0);
    }

    [Test]
    public void ForgettingACreatureClearsItsStacksAndTheRhythmsTargetingIt()
    {
        ConfigurePressure(buildSeconds: 1);
        Hit(Defender);
        Hit(Defender);
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(8);
        Stat(Attacker, StatType.RepeatedTargetDamageSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentPerHit, 10);
        Stat(Attacker, StatType.RepeatedTargetDamagePercentMax, 50);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110);

        _statusLog.Clear();
        _service.ForgetDamageModifierState(Defender);
        _statusLog.Should().Equal(
            $"Remove {Defender} {nameof(SpottersRhythmStatusEffect)} from {Defender}",
            $"Remove {Attacker} {nameof(SpottersRhythmStatusEffect)} from {Attacker}");
        _service.GetSameTargetPressureWeaponAbilityDamageBonus(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(120, "the attacker's own chain is kept");

        _service.ForgetDamageModifierState(Attacker);
        Dealt(out _, skill: SkillType.Rifle, landed: true).Should().Be(110, "forgotten");
    }
}
