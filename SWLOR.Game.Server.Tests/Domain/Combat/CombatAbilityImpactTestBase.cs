using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability impact parts of <c>Combat</c> over one set of scripted leaves: the base damage bonuses, the riders of a successful hit,
/// the area impacts and the finished impact effects. Stats come from tables, every other part and service is scripted and writes what it
/// was asked into one log (so a test sees the order of the riders, the damage, the healing and the status calls), the clock is held
/// still and the shared <c>Random</c> is seeded where a chance is rolled. The five parts and the stay-part are built for real over the
/// same leaves.
/// </summary>
public abstract class CombatAbilityImpactTestBase
{
    protected const uint Attacker = 11;
    protected const uint Defender = 22;
    protected const uint Other = 33;
    protected const uint Gone = 44;
    protected const uint Ally = 55;
    protected const uint Third = 66;

    protected readonly List<IDisposable> _scopes = new();
    protected readonly List<string> _log = new();
    protected FacadeContainer _facades = null!;
    protected IPartyService _partyProxy = null!;
    protected IPartyRoster _rosterProxy = null!;
    protected FakeNWScript _engine = null!;
    protected CombatAbilityImpactService _service = null!;
    protected CombatAbilityDamageBonusService _damageBonus = null!;
    protected CombatAbilityHitRidersService _hitRiders = null!;
    protected CombatAbilityAreaImpactService _areaImpactPart = null!;
    protected CombatAbilityWeaponRidersService _weaponRiders = null!;
    protected CombatAbilityStatusAppliedService _statusApplied = null!;
    protected DateTime _now;
    /// <summary>Forgets a creature the way the cleanup does in the ability impact step: the hit riders part, then the area impact part.</summary>
    protected void ForgetImpactState(uint creature)
    {
        _hitRiders.ForgetCreatureState(creature, CombatForgetPhase.AbilityImpact);
        _areaImpactPart.ForgetCreatureState(creature, CombatForgetPhase.AbilityImpact);
    }

    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected Dictionary<(uint, StatType), int> _permanent = null!;
    protected Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    protected Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> _impactSources = null!;
    protected Dictionary<StatType, int> _temporary = null!;
    protected HashSet<(uint, Type)> _statuses = null!;
    protected HashSet<(uint, Type, uint)> _sourceStatuses = null!;
    protected HashSet<(uint, StatusEffectCategory)> _categories = null!;
    protected Dictionary<uint, CreatureStatusEffect> _creatureEffects = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _dead = null!;
    protected List<uint> _nearby = null!;
    protected List<uint> _shape = null!;
    protected Dictionary<uint, int> _hitPoints = null!;
    protected Dictionary<uint, float> _distances = null!;
    protected Dictionary<uint, int> _stamina = null!;
    protected HashSet<uint> _partyMembers = null!;
    protected HashSet<StatType> _blockedTriggers = null!;
    protected CombatAbilityStaminaCostState _costState;
    protected AbilityImpactSequence _sequence;
    protected uint _attackTarget;
    protected uint _highestEnmity;
    protected bool _areaImpact;
    protected bool _pulseReady;
    protected bool _statusApplies;
    protected bool _reversalCut;
    protected bool _heavyTrigger;
    protected bool _perkTypes;
    protected bool _perkCategory;
    protected bool _noGrantedDeflection;
    protected int _firstHitBonus;
    protected int _directBonus;
    protected int _guardedBonus;
    protected int _highResource;
    protected int _skillSelected;
    protected int _restoredFP;
    protected int _restoredStamina;
    protected int _staminaDrained;

    [OneTimeSetUp]
    public void BuildFacades()
    {
        // The friendly target search of the party buff asks the party roster (GameServices.PartyRoster), and so does the
        // instance; the container's party service answers the same for the parts that still read it.
        _partyProxy = ScriptedProxy.Create<IPartyService>(out var partyCalls);
        partyCalls.On(nameof(IPartyService.IsInParty), args =>
        {
            _log.Add($"InParty {args[0]} {args[1]}");
            return _partyMembers.Contains((uint)args[1]!);
        });
        var roster = _rosterProxy = ScriptedProxy.Create<IPartyRoster>(out var rosterCalls);
        rosterCalls.On(nameof(IPartyRoster.IsInParty), args =>
        {
            _log.Add($"InParty {args[0]} {args[1]}");
            return _partyMembers.Contains((uint)args[1]!);
        });
        _facades = new FacadeContainer(services => services
            .Replace(ServiceDescriptor.Singleton(_partyProxy))
            .Replace(ServiceDescriptor.Singleton(roster)));
    }

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // The impact profile is read through Combat from the queries part of the installed container.
        _scopes.Add(_facades.Install());
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _log.Clear();
        _stats = new();
        _permanent = new();
        _sources = new();
        _impactSources = new();
        _temporary = new();
        _statuses = new();
        _sourceStatuses = new();
        _categories = new();
        _creatureEffects = new();
        _invalid = new();
        _dead = new();
        _nearby = new();
        _shape = new();
        _hitPoints = new() { [Attacker] = 50 };
        _distances = new();
        _stamina = new();
        _partyMembers = new();
        _blockedTriggers = new();
        _costState = null;
        _sequence = null;
        _attackTarget = NWScript.OBJECT_INVALID;
        _highestEnmity = NWScript.OBJECT_INVALID;
        _areaImpact = _reversalCut = _heavyTrigger = _perkTypes = _perkCategory = _noGrantedDeflection = false;
        _pulseReady = _statusApplies = true;
        _firstHitBonus = _directBonus = _guardedBonus = _highResource = _skillSelected = 0;
        _restoredFP = _restoredStamina = -1;
        _staminaDrained = 0;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args => (uint)args[0]! != Attacker && (uint)args[0]! != Ally);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.GetValueOrDefault((uint)args[0]!, _nearby.Contains((uint)args[0]!) ? 10 : 0));
        _engine.SetReturnHandler("GetDistanceBetween", args => _distances.GetValueOrDefault((uint)args[1]!, 1f));
        _engine.SetReturnHandler("GetAttackTarget", _ => _attackTarget);
        // AbilityTargeting finds the nearby creatures one by one; these are alive, hostile and in range.
        _engine.SetReturnHandler("GetNearestCreatureToLocation", args => _nearby.ElementAtOrDefault((int)args[3]! - 1) is var found and not 0
            ? found
            : NWScript.OBJECT_INVALID);
        var shapeIndex = 0;
        _engine.SetReturnHandler("GetFirstObjectInShape", _ =>
        {
            shapeIndex = 0;
            return _shape.Count > 0 ? _shape[shapeIndex++] : NWScript.OBJECT_INVALID;
        });
        _engine.SetReturnHandler("GetNextObjectInShape", _ => shapeIndex < _shape.Count ? _shape[shapeIndex++] : NWScript.OBJECT_INVALID);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _service = BuildService();
    }

    protected CombatAbilityImpactService BuildService()
    {
        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.AbilityAppliedAnyStatus), args =>
            {
                var matches = (Type[])args[2]!;
                return matches.Contains((Type)args[0]!) || ((IEnumerable<Type>)args[1]!).Any(matches.Contains);
            })
            .On(nameof(ICombatQueriesService.AbilityAppliedAnyStatusCategory), args =>
                args[0] as Type == typeof(DazedStatusEffect) || ((IEnumerable<Type>)args[1]!).Contains(typeof(DazedStatusEffect)))
            .On(nameof(ICombatQueriesService.AbilityMatchesAnyPerkTypeStat), _ => _perkTypes)
            .On(nameof(ICombatQueriesService.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger), _ => _heavyTrigger)
            .On(nameof(ICombatQueriesService.AbilityMatchesPerkCategoryStat), _ => _perkCategory)
            .On(nameof(ICombatQueriesService.AbilityMatchesReversalCutTrigger), _ => _reversalCut)
            .On(nameof(ICombatQueriesService.GetSkillSelectedStatAdjustment), args =>
            {
                _log.Add($"SkillSelected {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}");
                return _skillSelected;
            });
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.ApplyAbilityRestoredBothResourcesEffects), args => Log($"RestoredBoth {args[0]}"))
            .On(nameof(ICombatResourcesService.ApplyAbilityRestoredFPEffects), args => Log($"RestoredFP {args[0]}"))
            .On(nameof(ICombatResourcesService.GetHighResourceStatAdjustment), args =>
            {
                _log.Add($"HighResource {args[0]} {args[1]} {args[2]} {(args[3] as AbilityDetail)?.SkillType.ToString() ?? "null"}");
                return _highResource;
            })
            .On(nameof(ICombatResourcesService.ReduceFPFromStat), args => Log($"LoseFP {args[0]} {args[1]} {args[2]}"))
            .On(nameof(ICombatResourcesService.ReduceStaminaFromStat), args => Log($"LoseStamina {args[0]} {args[1]} {args[2]}"))
            .On(nameof(ICombatResourcesService.RestoreAbilityHitStaminaFromStat), args =>
            {
                _log.Add($"HitStamina {args[0]} {args[1]} {args[2]}" + (args.Length > 3 ? $" {(args[3] as AbilityDetail)?.SkillType.ToString() ?? "null"}" : string.Empty));
                return _restoredStamina < 0 ? (int)args[2]! : _restoredStamina;
            })
            .On(nameof(ICombatResourcesService.RestoreFPFromStat), args =>
            {
                _log.Add($"FP {args[0]} {args[1]} {args[2]}");
                return _restoredFP < 0 ? (int)args[2]! : _restoredFP;
            })
            .On(nameof(ICombatResourcesService.TryGetAbilityStaminaCostState), args =>
            {
                args[2] = _costState;
                return _costState != null;
            })
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _log.Add($"Trigger {args[0]} {args[1]} {args[2]}");
                return !_blockedTriggers.Contains((StatType)args[1]!);
            });
        var timing = ScriptedProxy.Create<ICombatAttackTimingService>(out var timingCalls);
        timingCalls
            .On(nameof(ICombatAttackTimingService.GrantNextAutoAttackNoDelay), args => Log($"NoDelay {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextSkillAbilityBonuses), args => Log($"NextSkill {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantStatusAppliedNextAttackDamageBonus), args => Log($"NextAttack {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.ShowReadyCue), args => Log($"Cue {string.Join(" ", args)}"));
        var activity = ScriptedProxy.Create<ICombatActivityService>(out var activityCalls);
        activityCalls
            .On(nameof(ICombatActivityService.ApplyFirstHostileAbilityHitCount), args => Log($"FirstHitCount {args[0]} {((AbilityDetail)args[1]!).SkillType}"))
            .On(nameof(ICombatActivityService.GetFirstHostileAbilityHitDamageBonus), args =>
            {
                _log.Add($"FirstHitBonus {args[0]}");
                return _firstHitBonus;
            })
            .On(nameof(ICombatActivityService.GetDirectDamageToStatusCategoryOrStealthBonus), args =>
            {
                _log.Add($"DirectBonus {args[0]} {args[1]}");
                return _directBonus;
            });
        var autoAttackEffects = ScriptedProxy.Create<ICombatAutoAttackEffectsService>(out var autoAttackEffectCalls);
        autoAttackEffectCalls.On(nameof(ICombatAutoAttackEffectsService.ApplySourceStatusStackEffects), args => Log($"SourceStack {args[0]} {args[1]}"));
        var defeatedEnemyEffects = ScriptedProxy.Create<ICombatDefeatedEnemyEffectsService>(out var defeatedEnemyEffectCalls);
        defeatedEnemyEffectCalls.On(nameof(ICombatDefeatedEnemyEffectsService.SpreadPoisonFromTarget), args => Log($"SpreadPoison {args[0]} {args[1]}"));
        var triggeredDamage = ScriptedProxy.Create<ICombatTriggeredDamageService>(out var triggeredDamageCalls);
        triggeredDamageCalls.On(nameof(ICombatTriggeredDamageService.ApplyTriggeredDamage), args =>
        {
            _log.Add($"Triggered {string.Join(" ", args.Select(arg => arg ?? "null"))}");
            return (int)args[2]!;
        });
        var guardedHitEffects = ScriptedProxy.Create<ICombatGuardedHitEffectsService>(out var guardedHitEffectsCalls);
        var damageTakenEffects = ScriptedProxy.Create<ICombatDamageTakenEffectsService>(out var damageTakenEffectsCalls);
        damageTakenEffectsCalls.On(nameof(ICombatDamageTakenEffectsService.ApplyAbilityGrantedAttackDeflectionEffects), args => Log($"GrantedDeflection {args[0]} {args[1]}"));
        guardedHitEffectsCalls
            .On(nameof(ICombatGuardedHitEffectsService.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus), args =>
            {
                _log.Add($"GuardedBonus {args[0]} {args[1]}");
                return _guardedBonus;
            });
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatAdjustmentExcludingTemporaryModifiers), args => _permanent.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatSources), args => _sources.TryGetValue(((uint)args[0]!, (StatType)args[1]!), out var found)
                ? found
                : Array.Empty<StatAdjustmentSource>())
            .On(nameof(IStatService.AggregateStatAdjustment), args => StatRules.AggregateStatAdjustment((StatType)args[0]!, (int)args[1]!, (int)args[2]!))
            .On(nameof(IStatService.GetGrantedDeflectionStatType), args => _noGrantedDeflection
                ? StatType.Invalid
                : StatRules.GetGrantedDeflectionStatType((StatType)args[0]!))
            .On(nameof(IStatService.GetStatTypeDeflectionSource), args => StatRules.GetStatTypeDeflectionSource((StatType)args[0]!))
            .On(nameof(IStatService.GetCurrentStamina), args =>
            {
                _log.Add($"Stamina {args[0]}");
                return _stamina.GetValueOrDefault((uint)args[0]!);
            })
            .On(nameof(IStatService.ReduceStamina), args =>
            {
                _log.Add($"ReduceStamina {args[0]} {args[1]}");
                var creature = (uint)args[0]!;
                _stamina[creature] = _stamina.GetValueOrDefault(creature) - (_staminaDrained > 0 ? _staminaDrained : (int)args[1]!);
                return null;
            });
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.HasStatusEffect), args => args.Length == 3 && args[2] is uint source
                ? _sourceStatuses.Contains(((uint)args[0]!, (Type)args[1]!, source))
                : _statuses.Contains(((uint)args[0]!, (Type)args[1]!)))
            .On(nameof(IStatusEffectService.HasStatusEffectCategory), args => _categories.Contains(((uint)args[0]!, (StatusEffectCategory)args[1]!)))
            .On(nameof(IStatusEffectService.RegisterAppliedListener), args => Log($"Listener {args[0]!.GetType().Name}"))
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args => Log($"Remove {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]}"))
            .On(nameof(IStatusEffectService.RemoveFirstBeneficialCombatStatusEffect), args =>
            {
                _log.Add($"RemoveBeneficial {args[0]} {args[1]}");
                return true;
            })
            .On(nameof(IStatusEffectService.GetCreatureStatusEffects), args =>
                _creatureEffects.TryGetValue((uint)args[0]!, out var effects) ? effects : new CreatureStatusEffect { StatGroup = new StatGroup() });
        var statusDuration = ScriptedProxy.Create<IStatusEffectDurationService>(out var statusDurationCalls);
        statusDurationCalls.On(nameof(IStatusEffectDurationService.ExtendStatusEffectDuration), args =>
        {
            _log.Add($"Extend {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]}");
            return true;
        });
        var statusApplication = ScriptedProxy.Create<IStatusEffectApplicationService>(out var statusApplicationCalls);
        statusApplicationCalls.On(nameof(IStatusEffectApplicationService.ApplyStatusEffect), args =>
            {
                var type = args[2] as Type ?? args[2]!.GetType();
                var detail = args[2] switch
                {
                    DamageDealtAdjustmentStatusEffect dealt => $" {dealt.Name} {dealt.Icon} {dealt.CleanseTypes} {dealt.ResistanceType}",
                    FoggyMindStatusEffect => " foggy",
                    SunderStatusEffect sunder => $" {sunder.StatGroup.Stats.GetValueOrDefault(StatType.PhysicalDefensePercentAdjustment)}",
                    ChallengeEnmityStatusEffect challenge => $" {challenge.StatGroup.Stats.GetValueOrDefault(StatType.EnmityToStatusSourcePercentAdjustment)}",
                    HostileAbilityPartyBuffStatusEffect buff => $" {buff.Name}",
                    _ => string.Empty
                };
                _log.Add($"Apply {args[0]}>{args[1]} {type.Name}{detail} {args[3]}" + (args.Length > 4 && args[4] is not (null or ResistanceType.Invalid) ? $" {args[4]}" : string.Empty));
                return _statusApplies;
            });
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls
            .On(nameof(ITemporaryStatModifierService.Consume), args =>
            {
                var statType = (StatType)args[1]!;
                _log.Add($"Consume {args[0]} {statType} {args[2]}");
                var value = _temporary.GetValueOrDefault(statType);
                _temporary.Remove(statType);
                return value;
            })
            .On(nameof(ITemporaryStatModifierService.GetStatAdjustment), args => _temporary.GetValueOrDefault((StatType)args[1]!))
            .On(nameof(ITemporaryStatModifierService.Replace), args => Log($"Replace {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}"))
            .On(nameof(ITemporaryStatModifierService.AddCapped), args =>
            {
                _log.Add($"AddCapped {string.Join(" ", args)}");
                return (int)args[2]!;
            });
        var ability = ScriptedProxy.Create<IAbilityImpactState>(out var abilityCalls);
        var pairedRangedTraits = ScriptedProxy.Create<IPairedRangedTraitsService>(out var pairedRangedTraitsCalls);
        pairedRangedTraitsCalls
            .On(nameof(IPairedRangedTraitsService.RecordSuccessfulWeaponAbilityHit), args => Log($"Record {args[0]} {args[1]} {args[2]}"));
        abilityCalls
            .On(nameof(IAbilityImpactState.IsAreaImpact), _ => _areaImpact)
            .On(nameof(IAbilityImpactState.TryTriggerAreaAbilityPulse), args =>
            {
                _log.Add($"Pulse {args[0]}");
                return _pulseReady;
            })
            .On(nameof(IAbilityImpactState.GetAbilityImpactSequence), _ => _sequence)
            .On(nameof(IAbilityImpactState.GetAbilityImpactStatSources), args => _impactSources.TryGetValue((StatType)args[1]!, out var found)
                ? found
                : Array.Empty<StatAdjustmentSource>());
        var enmity = ScriptedProxy.Create<IEnmityChangeService>(out var enmityCalls);
        enmityCalls.On(nameof(IEnmityChangeService.ModifyEnmity), args => Log($"Enmity {args[0]} {args[1]} {args[2]}"));
        var enmityAttack = ScriptedProxy.Create<IEnmityAttackService>(out var enmityAttackCalls);
        enmityAttackCalls.On(nameof(IEnmityAttackService.GetHighestEnmityAttackTarget), args =>
        {
            _log.Add($"HighestEnmity {args[0]}");
            return _highestEnmity;
        });
        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out var recorderCalls);
        recorderCalls.On(nameof(ICombatRecorderService.BeginStatTriggerAttribution), args =>
        {
            _log.Add($"Attribute {args[0]} {args[1]}");
            return default(CombatRecordAttributionScope);
        });

        _damageBonus = new CombatAbilityDamageBonusService(queries, resources, activity, guardedHitEffects, stat, statusEffect, temporary);
        _hitRiders = new CombatAbilityHitRidersService(resources, timing, damageTakenEffects, stat, statusApplication, temporary);
        _areaImpactPart = new CombatAbilityAreaImpactService(queries, resources, triggeredDamage, stat, statusEffect, statusApplication, statusDuration, temporary, ability);
        _weaponRiders = new CombatAbilityWeaponRidersService(queries, resources, defeatedEnemyEffects, stat, statusEffect, statusApplication, temporary, enmity);
        _statusApplied = new CombatAbilityStatusAppliedService(resources, timing, stat, statusEffect, statusApplication, temporary, ability, recorder);
        return new CombatAbilityImpactService(activity, _hitRiders, _areaImpactPart, autoAttackEffects, stat, enmityAttack, _rosterProxy, pairedRangedTraits, _weaponRiders, _statusApplied);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    protected object Log(string line)
    {
        _log.Add(line);
        return null;
    }

    protected void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    protected void Stat(StatType stat, int value) => Stat(Attacker, stat, value);

    protected static StatAdjustmentSource Source(string key, params (StatType Stat, int Value)[] stats) =>
        new(key, stats.ToDictionary(pair => pair.Stat, pair => pair.Value));

    protected IEnumerable<string> Lines(string prefix) => _log.Where(line => line.StartsWith(prefix, StringComparison.Ordinal));

    protected static AbilityDetail Hostile(SkillType skill = SkillType.Rifle, bool area = false, float maxRange = 10f) => new()
    {
        SkillType = skill,
        IsHostileAbility = true,
        IsAreaAbility = area,
        IsSingleTargetAbility = !area,
        MaxRange = maxRange
    };

    protected void Riders(
        AbilityDetail ability,
        SkillType skill,
        int damage = 10,
        bool statusApplied = false,
        Type primary = null,
        Type[] additional = null,
        bool firstHit = false,
        bool firstTarget = true,
        StatusEffectCategory applied = StatusEffectCategory.None,
        bool allowsHitProcs = true,
        bool attackingAlly = false,
        CombatDamageType damageType = CombatDamageType.Physical,
        uint target = Defender) =>
        _service.ApplySuccessfulAbilityImpactRiders(Attacker, target, ability, skill, damageType, damage, statusApplied, primary!,
            additional ?? Array.Empty<Type>(), firstHit, firstTarget, applied, allowsHitProcs, attackingAlly);

    // The part that Private reads a member of by name.
    protected abstract object PrivateTarget { get; }

    protected object Private(string name, params object[] arguments) =>
        PrivateTarget.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(PrivateTarget, arguments);

    protected IDictionary<(uint, uint), int> SameTargetCounts() =>
        (IDictionary<(uint, uint), int>)typeof(CombatAbilityHitRidersService)
            .GetField("_sameTargetHostileAbilityHitCounts", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_hitRiders)!;

    protected System.Collections.IDictionary HitSequences() =>
        (System.Collections.IDictionary)typeof(CombatAbilityAreaImpactService)
            .GetField("_areaAbilityTargetHitSequences", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_areaImpactPart)!;

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    protected static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    protected IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    protected IEnumerable<VisualEffect> Visuals() => _engine.CallsTo("EffectVisualEffect").Select(call => (VisualEffect)call.Arguments[0]!);

    protected static T WithSource<T>(T effect, uint source)
        where T : StatusEffectBase
    {
        typeof(StatusEffectBase).GetProperty(nameof(StatusEffectBase.Source))!.SetValue(effect, source);
        return effect;
    }

    protected void Effects(uint creature, params IStatusEffect[] effects)
    {
        var table = new CreatureStatusEffect { StatGroup = new StatGroup() };
        foreach (var effect in effects)
            table.Add(effect);
        _creatureEffects[creature] = table;
    }

    protected void NearTargetStats()
    {
        Stat(StatType.RangedAbilityHitNearTargetRangeMeters, 5);
        Stat(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, -10);
        Stat(StatType.RangedAbilityHitNearTargetDurationSeconds, 6);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef, 77);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectIcon, (int)EffectIconType.DamageDecrease);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes, (int)StatusEffectCleanseType.Purify);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectResistanceType, (int)ResistanceType.Mind);
    }

    protected static AbilityImpactSummary Summary(SkillType skill, int targets, bool area = true) => new()
    {
        Ability = Hostile(skill, area),
        SkillType = skill,
        IsAreaAbility = area,
        IsSingleTargetAbility = !area,
        ImpactedTargetCount = targets
    };
}
