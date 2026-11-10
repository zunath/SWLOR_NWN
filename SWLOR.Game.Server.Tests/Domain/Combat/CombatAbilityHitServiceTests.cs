using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability hit roll of <c>Combat</c>: hit chance, critical rate and their rolls, the test overrides of the hit rolls,
/// and the riders of a guarded hit, a ranged ability hit and an evaded Force ability. Stats come from a table, the other
/// parts and services are scripted and record what they were asked, and the shared <c>Random</c> is seeded so a test
/// knows the numbers the roll draws (and how many).
/// </summary>
[TestFixture]
public class CombatAbilityHitServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Gone = 44;
    private const uint Weapon = 501;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _log = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private CombatAbilityHitService _service = null!;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<StatType, int> _skillSelected = null!;
    private HashSet<(uint, Type)> _statuses = null!;
    private HashSet<(uint, StatusEffectCategory)> _categories = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _players = null!;
    private bool _hostile;
    private bool _notFacing;
    private bool _triggerReady;
    private int _accuracy;
    private int _evasion;
    private int _evasionIgnore;
    private int _statusAccuracy;
    private int _traitAccuracy;
    private int _minimumHitRate;
    private int _targeted;
    private int _physicalForce;
    private int _sideHit;
    private int _idleHit;
    private int _suppressionAccuracy;
    private int _longRangeHit;
    private int _longRangeCritical;
    private int _skillCritical;
    private int _targetStatusCritical;
    private int _sideCritical;
    private int _backCritical;
    private int _forceAffinity;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // The impact profile is read through Combat from the queries part of the installed container.
        _scopes.Add(_facades.Install());
        _log.Clear();
        _stats = new();
        _skillSelected = new();
        _statuses = new();
        _categories = new();
        _invalid = new();
        _players = new();
        _hostile = true;
        _notFacing = false;
        _triggerReady = true;
        _accuracy = _evasion = _evasionIgnore = _statusAccuracy = _traitAccuracy = _minimumHitRate = 0;
        _targeted = _physicalForce = _sideHit = _idleHit = _suppressionAccuracy = _longRangeHit = _longRangeCritical = 0;
        _skillCritical = _targetStatusCritical = _sideCritical = _backCritical = _forceAffinity = 0;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => _hostile);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.GetMinimumHitRate), _ => _minimumHitRate)
            .On(nameof(ICombatQueriesService.GetPhysicalAndForceAbilityHitChanceAdjustment), _ => _physicalForce)
            .On(nameof(ICombatQueriesService.GetRelevantSkillWeapon), _ => Weapon)
            .On(nameof(ICombatQueriesService.GetSkillSelectedStatAdjustment), args =>
            {
                _log.Add($"Selected {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}");
                return _skillSelected.GetValueOrDefault((StatType)args[3]!);
            })
            .On(nameof(ICombatQueriesService.GetTargetedAbilityAdjustment), args =>
            {
                _log.Add($"Targeted {args[0]} {args[1]} {args[4]}");
                return _targeted;
            })
            .On(nameof(ICombatQueriesService.IsTargetNotFacingAttacker), _ => _notFacing);
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _log.Add($"Trigger {args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            })
            .On(nameof(ICombatResourcesService.RestoreStaminaFromStat), args =>
            {
                _log.Add($"Stamina {args[0]} {args[1]} {args[2]}");
                return (int)args[2]!;
            });
        var suppression = ScriptedProxy.Create<ICombatSuppressionService>(out var suppressionCalls);
        suppressionCalls
            .On(nameof(ICombatSuppressionService.ConsumeSuppressionRangedAttackAccuracyAdjustment), _ => _suppressionAccuracy)
            .On(nameof(ICombatSuppressionService.GetRangedAbilityLongRangeHitChanceAdjustment), _ => _longRangeHit)
            .On(nameof(ICombatSuppressionService.GetRangedAbilityLongRangeCriticalRateAdjustment), _ => _longRangeCritical);
        var activity = ScriptedProxy.Create<ICombatActivityService>(out var activityCalls);
        activityCalls.On(nameof(ICombatActivityService.GetIdleAbilityHitChanceAdjustment), _ => _idleHit);
        var modifiers = ScriptedProxy.Create<ICombatDamageModifiersService>(out var modifierCalls);
        modifierCalls
            .On(nameof(ICombatDamageModifiersService.ApplySideAttackEvasionIgnore), args => (int)args[3]! - _evasionIgnore)
            .On(nameof(ICombatDamageModifiersService.ApplyStatusSourceAccuracyModifiers), args => (int)args[2]! + _statusAccuracy)
            .On(nameof(ICombatDamageModifiersService.GetSideAttackHitChanceAdjustment), _ => _sideHit)
            .On(nameof(ICombatDamageModifiersService.GetSkillCriticalRatePercentAdjustment), _ => _skillCritical)
            .On(nameof(ICombatDamageModifiersService.GetTargetStatusCriticalRateAdjustment), _ => _targetStatusCritical)
            .On(nameof(ICombatDamageModifiersService.GetSideAttackCriticalRateAdjustment), _ => _sideCritical)
            .On(nameof(ICombatDamageModifiersService.GetBackAttackCriticalRateAdjustment), _ => _backCritical);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetAccuracy), args =>
            {
                _log.Add($"Accuracy {args[0]} {args[1]} {args[2]} {args[3]} {args[4]} {args[5]}");
                return _accuracy;
            })
            .On(nameof(IStatService.GetEvasion), args =>
            {
                _log.Add($"Evasion {args[0]} {args[1]} {args[2]}");
                return _evasion;
            });
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.HasStatusEffect), args => args[1] is Type type && _statuses.Contains(((uint)args[0]!, type)))
            .On(nameof(IStatusEffectService.HasStatusEffectCategory), args =>
                _categories.Contains(((uint)args[0]!, (StatusEffectCategory)args[1]!)));
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls.On(nameof(ITemporaryStatModifierService.Replace), args =>
        {
            _log.Add($"Replace {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}");
            return null;
        });
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls.On(nameof(IAbilityService.GetWeaponTraitAccuracy), _ => _traitAccuracy);
        var perk = ScriptedProxy.Create<IPerkService>(out var perkCalls);
        perkCalls.On(nameof(IPerkService.GetForceAffinityHitChanceAdjustment), args =>
        {
            _log.Add($"Affinity {args[0]} {args[1]}");
            return _forceAffinity;
        });
        var traits = ScriptedProxy.Create<IPairedRangedTraitsService>(out var traitCalls);
        traitCalls.On(nameof(IPairedRangedTraitsService.RecordAccuracyEvasionMiss), args =>
        {
            _log.Add($"Miss {args[0]}");
            return null;
        });

        _service = new CombatAbilityHitService(queries, resources, suppression, activity, modifiers, stat, statusEffect, temporary,
            ability, perk, traits);
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

    private IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    private static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    // ------------------------------------------------------------------ critical rate and roll

    [Test]
    public void TheAbilityCriticalRateAddsEverySourceThroughTheRule()
    {
        _skillCritical = 2;
        _skillSelected[StatType.AbilityCriticalRatePercentAdjustment] = 3;
        _longRangeCritical = 4;
        _targetStatusCritical = 5;
        _sideCritical = 6;
        _backCritical = 7;

        _service.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 1, Defender)
            .Should().Be(CombatRules.CalculateAbilityCriticalChance(1 + 2 + 3 + 4 + 5 + 6 + 7));
        _log.Should().Contain($"Selected {Attacker} {SkillType.Rifle} {StatType.AbilityCriticalRatePercentAdjustmentSkillType} {StatType.AbilityCriticalRatePercentAdjustment} True");

        _invalid.Add(Gone);
        _service.GetAbilityCriticalRate(Gone, SkillType.Rifle, false).Should().Be(0);
    }

    [Test]
    public void TheTwinBladeAreaThrowingAndPositionalCriticalBonusesNeedTheirConditions()
    {
        Stat(Attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment, 10);
        Stat(Attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment, 20);
        Stat(Attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment, 40);
        Stat(Attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment, 80);
        var none = CombatRules.CalculateAbilityCriticalChance(0);

        _service.GetAbilityCriticalRate(Attacker, SkillType.TwinBlade, false, 0, Defender).Should().Be(none, "not an area ability");
        _service.GetAbilityCriticalRate(Attacker, SkillType.TwinBlade, true, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(10));
        _service.GetAbilityCriticalRate(Attacker, SkillType.Throwing, true, 0, Defender).Should().Be(none, "the target neither bleeds nor is disoriented");
        _statuses.Add((Defender, typeof(DisorientedStatusEffect)));
        _service.GetAbilityCriticalRate(Attacker, SkillType.Throwing, true, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(20));
        _statuses.Clear();
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        _service.GetAbilityCriticalRate(Attacker, SkillType.Throwing, false, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(20));
        _service.GetAbilityCriticalRate(Attacker, SkillType.Throwing, false).Should().Be(none, "no defender");

        _categories.Clear();
        _notFacing = true;
        _service.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(40));
        _service.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false).Should().Be(none, "no defender to face");
        _notFacing = false;
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _service.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(80));
    }

    [Test]
    public void TheCriticalRatingRollsOnceAgainstThePositiveRateAndNotAtAll()
    {
        _skillCritical = 30;
        var rate = CombatRules.CalculateAbilityCriticalChance(30);
        rate.Should().BeGreaterThan(0);

        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] <= rate);
        var rolls = Rolls(seed, 2);
        _service.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(CombatRules.StandardCriticalRating);
        Random.D100(1).Should().Be(rolls[1], "the rating drew exactly one roll");

        seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > rate);
        rolls = Rolls(seed, 2);
        _service.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(0);
        Random.D100(1).Should().Be(rolls[1]);

        _invalid.Add(Gone);
        rolls = Rolls(3, 1);
        _service.CalculateAbilityCriticalRating(Gone, SkillType.Rifle, false).Should().Be(0);
        Random.D100(1).Should().Be(rolls[0], "a rate of zero draws nothing");
    }

    // ------------------------------------------------------------------ hit roll

    [Test]
    public void AnInvalidRollIsAHitAtFullRateWithoutDrawing()
    {
        _invalid.Add(Gone);
        var rolls = Rolls(5, 1);

        _service.TryResolveAbilityHit(Gone, Defender, SkillType.Rifle, PerkType.Invalid, out var hitRate).Should().BeTrue();
        hitRate.Should().Be(100);
        _service.TryResolveAbilityHit(Attacker, Gone, SkillType.Rifle, PerkType.Invalid, out hitRate).Should().BeTrue();
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Invalid, PerkType.Invalid, out hitRate).Should().BeTrue();
        hitRate.Should().Be(100);
        Random.D100(1).Should().Be(rolls[0]);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheHitRateAddsEveryAdjustmentAndTheRollDrawsOnce()
    {
        _accuracy = 200;
        _traitAccuracy = 7;
        _statusAccuracy = 3;
        _evasion = 150;
        _evasionIgnore = 10;
        _skillSelected[StatType.AbilityHitChancePercentAdjustment] = 2;
        _skillSelected[StatType.IncomingAbilityHitChancePercentAdjustment] = -4;
        _longRangeHit = 3;
        _targeted = 5;
        _physicalForce = 6;
        _sideHit = 1;
        _idleHit = 8;
        _suppressionAccuracy = 9;
        _minimumHitRate = 20;
        Stat(Attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment, 11);
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        var expected = CombatRules.CalculateHitRate(200 + 7 + 3 + 4, 150 - 10, 1 + 2 + 3 + 5 + 6 - 4 + 1 + 8 + 9 + 11, 20);

        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] <= expected);
        var rolls = Rolls(seed, 2);
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.DoubleShot, out var hitRate, 1, 30, AbilityType.Might, 4)
            .Should().BeTrue();
        hitRate.Should().Be(expected);
        Random.D100(1).Should().Be(rolls[1], "the hit roll drew exactly one number");
        _log.Should().Contain($"Accuracy {Attacker} {Weapon} {AbilityType.Might} {SkillType.Rifle} 30 False");
        _log.Should().Contain($"Evasion {Defender} {SkillType.Invalid} {SkillType.Rifle}");
        _log.Should().Contain($"Targeted {Attacker} {PerkType.DoubleShot} {StatType.TargetedAbilityHitChancePercentAdjustment}");
        _log.Should().Contain($"Selected {Defender} {SkillType.Rifle} {StatType.IncomingAbilityHitChancePercentAdjustmentSkillType} {StatType.IncomingAbilityHitChancePercentAdjustment} True");
        _log.Should().NotContain(line => line.StartsWith("Affinity") || line.StartsWith("Miss"));
    }

    [Test]
    public void AForceAbilityUsesWillpowerAccuracyAndItsAffinity()
    {
        _forceAffinity = 12;
        _evasion = 1000;
        var expected = CombatRules.CalculateHitRate(0, 1000, 12, 0);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > expected);
        Rolls(seed, 1);

        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.ForceLightning, out var hitRate).Should().BeFalse();
        hitRate.Should().Be(expected);
        _log.Should().Contain($"Accuracy {Attacker} {Weapon} {AbilityType.Willpower} {SkillType.Force} -1 True");
        _log.Should().Contain($"Affinity {Attacker} {PerkType.ForceLightning}");
        _log.Should().Contain($"Miss {Defender}", "a missed hostile ability counts as an accuracy or evasion miss");
    }

    [Test]
    public void AMissOnAFriendlyTargetIsNotRecordedAndTheOverridesDecideWithoutDrawing()
    {
        _hostile = false;
        _evasion = 1000;
        var expected = CombatRules.CalculateHitRate(0, 1000, 0, 0);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > expected);
        Rolls(seed, 1);
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out _).Should().BeFalse();
        _log.Should().NotContain(line => line.StartsWith("Miss"));

        _service.SetAbilityHitResolutionOverride(true);
        var rolls = Rolls(9, 1);
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out var hitRate).Should().BeTrue();
        hitRate.Should().Be(expected, "the rate is still computed");
        Random.D100(1).Should().Be(rolls[0], "a forced outcome draws no roll");

        _service.SetAbilityHitResolutionOverride(false);
        _evasion = 0;
        _hostile = true;
        _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Rifle, PerkType.Invalid, out _).Should().BeFalse();
        _log.Should().Contain($"Miss {Defender}");
        _service.SetAbilityHitResolutionOverride(null);
    }

    [Test]
    public void TheAutoAttackOverrideIsKeptForTheNativeRoll()
    {
        _service.GetAutoAttackHitResolutionOverride().Should().BeNull();
        _service.SetAutoAttackHitResolutionOverride(false);
        _service.GetAutoAttackHitResolutionOverride().Should().BeFalse();
        _service.SetAutoAttackHitResolutionOverride(null);
        _service.GetAutoAttackHitResolutionOverride().Should().BeNull();
    }

    [Test]
    public void AnExplicitImpactProfileGivesTheAccuracyOfItsCapturedWeapon()
    {
        var weapon = new WeaponShotProfile(Weapon, SkillType.Pistol, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        using var profile = new WeaponImpactProfile(Attacker, weapon, new WeaponImpactActivation(), accuracyAdjustment: 6);
        _traitAccuracy = 4;
        _statusAccuracy = 2;
        _accuracy = 1;

        _service.SetAbilityHitResolutionOverride(true);
        try
        {
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Pistol, PerkType.Invalid, out var hitRate).Should().BeTrue();
            hitRate.Should().Be(CombatRules.CalculateHitRate(90 + 6 + 4 + 2, 0, 0, 0));
        }
        finally
        {
            _service.SetAbilityHitResolutionOverride(null);
        }

        _log.Should().NotContain(line => line.StartsWith("Accuracy"), "the captured accuracy replaces the equipped weapon's");
    }

    [Test]
    public void ThePerkTargetingBranchOfTheSharedAdjustmentAddsTheTargetedBonus()
    {
        // Both callers pass false today; the branch is kept as it was.
        var method = typeof(CombatAbilityHitService).GetMethod("GetAbilityHitOrCriticalAdjustment", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _targeted = 9;
        _skillSelected[StatType.AbilityHitChancePercentAdjustment] = 1;

        method.Invoke(_service, new object[]
        {
            Attacker, SkillType.Rifle, PerkType.DoubleShot, StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustmentPerkType, StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
            StatType.AbilityHitChancePercentAdjustment, true, Defender
        }).Should().Be(1 + 9);
        method.Invoke(_service, new object[]
        {
            Attacker, SkillType.Rifle, PerkType.DoubleShot, StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustmentPerkType, StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
            StatType.AccuracyPercentAdjustment, false, Defender
        }).Should().Be(0, "only the hit chance and critical rate stats read the long-range bonus");
    }

    // ------------------------------------------------------------------ evaded Force ability

    [Test]
    public void AnEvadedForceAbilityGrantsForceDefenseAndStaminaOnItsCooldown()
    {
        Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 15);
        Stat(Defender, StatType.ForceAbilityEvadedDurationSeconds, 6);
        Stat(Defender, StatType.ForceAbilityEvadedStaminaRestore, 3);
        Stat(Defender, StatType.ForceAbilityEvadedCooldownSeconds, 12);
        _service.SetAbilityHitResolutionOverride(false);
        try
        {
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _).Should().BeFalse();
            _log.Should().ContainInOrder(
                $"Trigger {Defender} {StatType.ForceAbilityEvadedForceDefensePercentAdjustment} 12",
                $"Replace {Defender} {StatType.ForceDefensePercentAdjustment} 15 6 {StatType.ForceAbilityEvadedForceDefensePercentAdjustment}",
                $"Stamina {Defender} {StatType.ForceAbilityEvadedStaminaRestore} 3");

            _log.Clear();
            _triggerReady = false;
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().NotContain(line => line.StartsWith("Replace") || line.StartsWith("Stamina"), "on cooldown");

            _log.Clear();
            _triggerReady = true;
            Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().Contain($"Stamina {Defender} {StatType.ForceAbilityEvadedStaminaRestore} 3");
            _log.Should().NotContain(line => line.StartsWith("Replace"));

            _log.Clear();
            Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 15);
            Stat(Defender, StatType.ForceAbilityEvadedStaminaRestore, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().Contain(line => line.StartsWith("Replace"));
            _log.Should().NotContain(line => line.StartsWith("Stamina"));

            _log.Clear();
            Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().NotContain(line => line.StartsWith("Trigger"), "nothing to grant");

            Stat(Defender, StatType.ForceAbilityEvadedStaminaRestore, 3);
            Stat(Defender, StatType.ForceAbilityEvadedDurationSeconds, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().NotContain(line => line.StartsWith("Trigger"), "no duration");
        }
        finally
        {
            _service.SetAbilityHitResolutionOverride(null);
        }
    }

    // ------------------------------------------------------------------ riders

    [Test]
    public void AGuardedHitReadiesTheNextAttackWithTheLongerWindowAndTellsAPlayer()
    {
        Stat(Attacker, StatType.GuardedHitNextAttackDMGBonus, 10);
        Stat(Attacker, StatType.GuardedHitNextAttackCriticalRatePercentAdjustment, 10);
        Stat(Attacker, StatType.GuardedHitNextAttackWindowSeconds, 30);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackDMGBonus, 8);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackEnmityBonus, 40);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackWindowSeconds, 20);

        _service.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().Equal(
            $"Replace {Attacker} {StatType.NextAttackGuardedHitDMGBonus} 18 30 {StatType.NextAttackGuardedHitDMGBonus}",
            $"Replace {Attacker} {StatType.NextAttackGuardedHitCriticalRatePercentAdjustment} 10 30 {StatType.NextAttackGuardedHitDMGBonus}",
            $"Replace {Attacker} {StatType.NextAttackGuardedHitEnmityBonus} 40 30 {StatType.NextAttackGuardedHitDMGBonus}");
        FloatingText().Should().BeEmpty("an NPC sees no text");

        _players.Add(Attacker);
        _service.ApplyGuardedHitNextAttackEffects(Attacker);
        FloatingText().Should().ContainSingle().Which.Should().Contain("Counter Ready: +18 DMG, +10% Crit, +40 Enmity");

        _engine.ClearCalls();
        Stat(Attacker, StatType.GuardedHitNextAttackCriticalRatePercentAdjustment, 0);
        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackEnmityBonus, 0);
        _service.ApplyGuardedHitNextAttackEffects(Attacker);
        FloatingText().Should().ContainSingle().Which.Should().Contain("Counter Ready: +18 DMG").And.NotContain("Crit").And.NotContain("Enmity");
    }

    [Test]
    public void AGuardedHitWithoutAWindowOrABonusReadiesNothing()
    {
        Stat(Attacker, StatType.GuardedHitNextAttackDMGBonus, 10);
        _service.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().BeEmpty("no window");

        Stat(Attacker, StatType.GuardedHitNextAttackDMGBonus, 0);
        Stat(Attacker, StatType.GuardedHitNextAttackWindowSeconds, 30);
        _service.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().BeEmpty("no bonus");

        Stat(Attacker, StatType.GuardedHitSecondaryNextAttackEnmityBonus, 5);
        _service.ApplyGuardedHitNextAttackEffects(Attacker);
        _log.Should().HaveCount(3, "an Enmity bonus alone readies the attack");
    }

    [Test]
    public void ARangedAbilityLowersTheTargetsPhysicalDefense()
    {
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionPercent, 20);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds, 9);

        _service.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
        _log.Should().Equal($"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -20 9 {StatType.RangedAbilityTargetDefenseReductionPercent}");

        _log.Clear();
        _service.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Vibroblade);
        _invalid.Add(Gone);
        _service.ApplyRangedAbilityTargetDefenseReduction(Gone, Defender, SkillType.Rifle);
        _service.ApplyRangedAbilityTargetDefenseReduction(Attacker, Gone, SkillType.Rifle);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds, 0);
        _service.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds, 9);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionPercent, 0);
        _service.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheSunderedTargetBonusesNeedAValidSunderedTarget()
    {
        Stat(Attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment, 5);
        Stat(Attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment, 6);

        _service.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
        _service.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _service.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(5);
        _service.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(6);
        _invalid.Add(Defender);
        _service.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
        _service.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
    }

    [Test]
    public void TheContainerBuildsThePartWithItsInjectedNeighbours()
    {
        _facades.Get<ICombatAbilityHitService>().Should().BeOfType<CombatAbilityHitService>();
    }
}
