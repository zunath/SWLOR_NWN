using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The shared bench of the ability hit parts of <c>Combat</c> (the hit roll and the hit adjustments). Stats come from a table,
/// the other parts and services are scripted and record what they were asked, and the shared <c>Random</c> is seeded so a test
/// knows the numbers the roll draws (and how many).
/// </summary>
public abstract class CombatAbilityHitTestBase
{
    protected const uint Attacker = 11;
    protected const uint Defender = 22;
    protected const uint Gone = 44;
    protected const uint Weapon = 501;

    protected readonly List<IDisposable> _scopes = new();
    protected readonly List<string> _log = new();
    protected FacadeContainer _facades = null!;
    protected FakeNWScript _engine = null!;
    protected CombatAbilityHitService _service = null!;
    protected CombatAbilityHitAdjustmentService _adjustment = null!;
    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected Dictionary<StatType, int> _skillSelected = null!;
    protected HashSet<(uint, Type)> _statuses = null!;
    protected HashSet<(uint, StatusEffectCategory)> _categories = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _players = null!;
    protected bool _hostile;
    protected bool _notFacing;
    protected bool _triggerReady;
    protected int _accuracy;
    protected int _evasion;
    protected int _evasionIgnore;
    protected int _statusAccuracy;
    protected int _traitAccuracy;
    protected int _minimumHitRate;
    protected int _targeted;
    protected int _physicalForce;
    protected int _sideHit;
    protected int _idleHit;
    protected int _suppressionAccuracy;
    protected int _longRangeHit;
    protected int _longRangeCritical;
    protected int _skillCritical;
    protected int _targetStatusCritical;
    protected int _sideCritical;
    protected int _backCritical;
    protected int _forceAffinity;

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
        var rate = ScriptedProxy.Create<ICombatRateModifiersService>(out var rateCalls);
        rateCalls
            .On(nameof(ICombatRateModifiersService.ApplySideAttackEvasionIgnore), args => (int)args[3]! - _evasionIgnore)
            .On(nameof(ICombatRateModifiersService.GetSideAttackHitChanceAdjustment), _ => _sideHit)
            .On(nameof(ICombatRateModifiersService.GetTargetStatusCriticalRateAdjustment), _ => _targetStatusCritical)
            .On(nameof(ICombatRateModifiersService.GetSideAttackCriticalRateAdjustment), _ => _sideCritical)
            .On(nameof(ICombatRateModifiersService.GetBackAttackCriticalRateAdjustment), _ => _backCritical);
        var modifiers = ScriptedProxy.Create<ICombatDamageModifiersService>(out var modifierCalls);
        modifierCalls
            .On(nameof(ICombatDamageModifiersService.ApplyStatusSourceAccuracyModifiers), args => (int)args[2]! + _statusAccuracy)
            .On(nameof(ICombatDamageModifiersService.GetSkillCriticalRatePercentAdjustment), _ => _skillCritical);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)));
        var rating = ScriptedProxy.Create<ICombatRatingService>(out var ratingCalls);
        ratingCalls
            .On(nameof(ICombatRatingService.GetAccuracy), args =>
            {
                _log.Add($"Accuracy {args[0]} {args[1]} {args[2]} {args[3]} {args[4]} {args[5]}");
                return _accuracy;
            })
            .On(nameof(ICombatRatingService.GetEvasion), args =>
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
        var ability = ScriptedProxy.Create<IAbilityImpactState>(out var abilityCalls);
        abilityCalls.On(nameof(IAbilityImpactState.GetWeaponTraitAccuracy), _ => _traitAccuracy);
        var perk = ScriptedProxy.Create<IPerkForceAffinityService>(out var perkCalls);
        perkCalls.On(nameof(IPerkForceAffinityService.GetForceAffinityHitChanceAdjustment), args =>
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

        _adjustment = new CombatAbilityHitAdjustmentService(queries, resources, suppression, modifiers, rate, stat, statusEffect, temporary);
        _service = new CombatAbilityHitService(queries, suppression, activity, modifiers, rate, rating, ability, perk, traits, _adjustment);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    protected void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    protected IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    protected static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }
}
