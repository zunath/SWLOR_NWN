using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability impact part of <c>Combat</c>: the base damage bonuses of an impact, the riders of a successful ability hit, the
/// Spear Disabler suppression riders, the status-applied stamina drain and the effects of a finished impact by target count.
/// Stats come from tables, every other part and service is scripted and writes what it was asked into one log (so a test sees
/// the order of the riders, the damage, the healing and the status calls), the clock is held still and the shared
/// <c>Random</c> is seeded where a chance is rolled.
/// </summary>
[TestFixture]
public class CombatAbilityImpactServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Other = 33;
    private const uint Gone = 44;
    private const uint Ally = 55;
    private const uint Third = 66;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _log = new();
    private FacadeContainer _facades = null!;
    private IPartyService _partyProxy = null!;
    private FakeNWScript _engine = null!;
    private CombatAbilityImpactService _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<(uint, StatType), int> _permanent = null!;
    private Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    private Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> _impactSources = null!;
    private Dictionary<StatType, int> _temporary = null!;
    private HashSet<(uint, Type)> _statuses = null!;
    private HashSet<(uint, Type, uint)> _sourceStatuses = null!;
    private HashSet<(uint, StatusEffectCategory)> _categories = null!;
    private Dictionary<uint, CreatureStatusEffect> _creatureEffects = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _dead = null!;
    private List<uint> _nearby = null!;
    private List<uint> _shape = null!;
    private Dictionary<uint, int> _hitPoints = null!;
    private Dictionary<uint, float> _distances = null!;
    private Dictionary<uint, int> _stamina = null!;
    private HashSet<uint> _partyMembers = null!;
    private HashSet<StatType> _blockedTriggers = null!;
    private CombatAbilityStaminaCostState? _costState;
    private AbilityImpactSequence? _sequence;
    private uint _attackTarget;
    private uint _highestEnmity;
    private bool _areaImpact;
    private bool _pulseReady;
    private bool _statusApplies;
    private bool _reversalCut;
    private bool _heavyTrigger;
    private bool _perkTypes;
    private bool _perkCategory;
    private bool _noGrantedDeflection;
    private int _firstHitBonus;
    private int _directBonus;
    private int _guardedBonus;
    private int _highResource;
    private int _skillSelected;
    private int _restoredFP;
    private int _restoredStamina;
    private int _staminaDrained;

    [OneTimeSetUp]
    public void BuildFacades()
    {
        // The friendly target search of the party buff asks the party facade; the instance takes the same scripted service.
        _partyProxy = ScriptedProxy.Create<IPartyService>(out var partyCalls);
        partyCalls.On(nameof(IPartyService.IsInParty), args =>
        {
            _log.Add($"InParty {args[0]} {args[1]}");
            return _partyMembers.Contains((uint)args[1]!);
        });
        _facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton(_partyProxy)));
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

    private CombatAbilityImpactService BuildService()
    {
        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.AbilityAppliedAnyStatus), args =>
            {
                var matches = (Type[])args[2]!;
                return matches.Contains((Type)args[0]!) || ((IEnumerable<Type>)args[1]!).Any(matches.Contains);
            })
            .On(nameof(ICombatQueriesService.AbilityAppliedAnyStatusCategory), args =>
                args[0] == typeof(DazedStatusEffect) || ((IEnumerable<Type>)args[1]!).Contains(typeof(DazedStatusEffect)))
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
        var hitEffects = ScriptedProxy.Create<ICombatHitEffectsService>(out var hitEffectCalls);
        hitEffectCalls
            .On(nameof(ICombatHitEffectsService.ApplySourceStatusStackEffects), args => Log($"SourceStack {args[0]} {args[1]}"))
            .On(nameof(ICombatHitEffectsService.ApplyTriggeredDamage), args =>
            {
                _log.Add($"Triggered {string.Join(" ", args.Select(arg => arg ?? "null"))}");
                return (int)args[2]!;
            })
            .On(nameof(ICombatHitEffectsService.SpreadPoisonFromTarget), args => Log($"SpreadPoison {args[0]} {args[1]}"));
        var damageTaken = ScriptedProxy.Create<ICombatDamageTakenService>(out var damageTakenCalls);
        damageTakenCalls
            .On(nameof(ICombatDamageTakenService.ApplyAbilityGrantedAttackDeflectionEffects), args => Log($"GrantedDeflection {args[0]} {args[1]}"))
            .On(nameof(ICombatDamageTakenService.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus), args =>
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
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
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
            })
            .On(nameof(IStatusEffectService.ExtendStatusEffectDuration), args =>
            {
                _log.Add($"Extend {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]}");
                return true;
            })
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args => Log($"Remove {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]}"))
            .On(nameof(IStatusEffectService.RemoveFirstBeneficialCombatStatusEffect), args =>
            {
                _log.Add($"RemoveBeneficial {args[0]} {args[1]}");
                return true;
            })
            .On(nameof(IStatusEffectService.GetCreatureStatusEffects), args =>
                _creatureEffects.TryGetValue((uint)args[0]!, out var effects) ? effects : new CreatureStatusEffect { StatGroup = new StatGroup() });
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
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityService.RecordSuccessfulWeaponAbilityHit), args => Log($"Record {args[0]} {args[1]} {args[2]}"))
            .On(nameof(IAbilityService.IsAreaImpact), _ => _areaImpact)
            .On(nameof(IAbilityService.TryTriggerAreaAbilityPulse), args =>
            {
                _log.Add($"Pulse {args[0]}");
                return _pulseReady;
            })
            .On(nameof(IAbilityService.GetAbilityImpactSequence), _ => _sequence)
            .On(nameof(IAbilityService.GetAbilityImpactStatSources), args => _impactSources.TryGetValue((StatType)args[1]!, out var found)
                ? found
                : Array.Empty<StatAdjustmentSource>());
        var enmity = ScriptedProxy.Create<IEnmityService>(out var enmityCalls);
        enmityCalls
            .On(nameof(IEnmityService.ModifyEnmity), args => Log($"Enmity {args[0]} {args[1]} {args[2]}"))
            .On(nameof(IEnmityService.GetHighestEnmityAttackTarget), args =>
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

        return new CombatAbilityImpactService(queries, resources, timing, activity, hitEffects, damageTaken, stat, statusEffect,
            temporary, ability, enmity, _partyProxy, recorder);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private object? Log(string line)
    {
        _log.Add(line);
        return null;
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private void Stat(StatType stat, int value) => Stat(Attacker, stat, value);

    private static StatAdjustmentSource Source(string key, params (StatType Stat, int Value)[] stats) =>
        new(key, stats.ToDictionary(pair => pair.Stat, pair => pair.Value));

    private IEnumerable<string> Lines(string prefix) => _log.Where(line => line.StartsWith(prefix, StringComparison.Ordinal));

    private static AbilityDetail Hostile(SkillType skill = SkillType.Rifle, bool area = false, float maxRange = 10f) => new()
    {
        SkillType = skill,
        IsHostileAbility = true,
        IsAreaAbility = area,
        IsSingleTargetAbility = !area,
        MaxRange = maxRange
    };

    private void Riders(
        AbilityDetail ability,
        SkillType skill,
        int damage = 10,
        bool statusApplied = false,
        Type? primary = null,
        Type[]? additional = null,
        bool firstHit = false,
        bool firstTarget = true,
        StatusEffectCategory applied = StatusEffectCategory.None,
        bool allowsHitProcs = true,
        bool attackingAlly = false,
        CombatDamageType damageType = CombatDamageType.Physical,
        uint target = Defender) =>
        _service.ApplySuccessfulAbilityImpactRiders(Attacker, target, ability, skill, damageType, damage, statusApplied, primary!,
            additional ?? Array.Empty<Type>(), firstHit, firstTarget, applied, allowsHitProcs, attackingAlly);

    private object? Private(string name, params object[] arguments) =>
        typeof(CombatAbilityImpactService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_service, arguments);

    private IDictionary<(uint, uint), int> SameTargetCounts() =>
        (IDictionary<(uint, uint), int>)typeof(CombatAbilityImpactService)
            .GetField("_sameTargetHostileAbilityHitCounts", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;

    private System.Collections.IDictionary HitSequences() =>
        (System.Collections.IDictionary)typeof(CombatAbilityImpactService)
            .GetField("_areaAbilityTargetHitSequences", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    private static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    private IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    private IEnumerable<VisualEffect> Visuals() => _engine.CallsTo("EffectVisualEffect").Select(call => (VisualEffect)call.Arguments[0]!);

    private static T WithSource<T>(T effect, uint source)
        where T : StatusEffectBase
    {
        typeof(StatusEffectBase).GetProperty(nameof(StatusEffectBase.Source))!.SetValue(effect, source);
        return effect;
    }

    private void Effects(uint creature, params IStatusEffect[] effects)
    {
        var table = new CreatureStatusEffect { StatGroup = new StatGroup() };
        foreach (var effect in effects)
            table.Add(effect);
        _creatureEffects[creature] = table;
    }

    // ------------------------------------------------------------------ base damage bonus

    [Test]
    public void TheBaseDamageBonusIsZeroWithoutAnAbilityOrAValidActivator()
    {
        Stat(StatType.LightsaberOffenseAreaDamageBonus, 5);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, null!, SkillType.Lightsaber).Should().Be(0);
        _invalid.Add(Attacker);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber, true), SkillType.Lightsaber).Should().Be(0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AHostileImpactAddsItsSkillBonusThenTheFirstHitDirectGuardedAndHighResourceBonusesInThatOrder()
    {
        Stat(StatType.StaffCrusherFinisherDamageBonus, 3);
        _firstHitBonus = 5;
        _directBonus = 7;
        _guardedBonus = 11;
        _highResource = 13;
        var ability = Hostile(SkillType.Staff);

        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, ability, SkillType.Staff).Should().Be(3 + 5 + 7 + 11 + 13);
        _log.Should().Equal(
            $"FirstHitBonus {Attacker}",
            $"DirectBonus {Attacker} {Defender}",
            $"GuardedBonus {Attacker} {SkillType.Staff}",
            $"HighResource {Attacker} {StatType.HighFPAndStaminaAbilityDamageBonus} {StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent} Staff");
    }

    [Test]
    public void ANonHostileImpactAddsOnlyTheGuardedHitBonus()
    {
        Stat(StatType.StaffCrusherFinisherDamageBonus, 3);
        _firstHitBonus = 5;
        _guardedBonus = 11;
        _highResource = 13;
        var ability = new AbilityDetail { SkillType = SkillType.Staff };

        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, ability, SkillType.Staff).Should().Be(11);
        _log.Should().Equal($"GuardedBonus {Attacker} {SkillType.Staff}");
    }

    [Test]
    public void ALightsaberImpactAddsTheAreaBonusAndTheDebuffedTargetBonusForASingleTarget()
    {
        Stat(StatType.LightsaberOffenseAreaDamageBonus, 4);
        Stat(StatType.LightsaberOffenseDebuffedTargetDamageBonus, 6);
        _categories.Add((Defender, StatusEffectCategory.Debuff));

        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber, true), SkillType.Lightsaber).Should().Be(4);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber).Should().Be(6);
        _categories.Clear();
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber).Should().Be(0);
        _categories.Add((Defender, StatusEffectCategory.Debuff));
        _invalid.Add(Defender);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber).Should().Be(0);
    }

    [TestCase(0, 0)]
    [TestCase(1, 10)]
    [TestCase(2, 22)]
    [TestCase(3, 22)]
    public void AVibroknifeImpactAddsTheToxicCoatingBonusByRank(int rank, int bonus)
    {
        Stat(StatType.VibroknifeSaboteurToxicCoatingRank, rank);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Vibroknife), SkillType.Vibroknife).Should().Be(bonus);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife)
            .Should().Be(0, "only a hostile ability is coated");
    }

    [Test]
    public void ASaberstaffImpactAddsTheConduitFlareBonus()
    {
        Stat(StatType.SaberstaffConduitFlareDamageBonus, 8);
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Saberstaff), SkillType.Saberstaff).Should().Be(8);
    }

    [Test]
    public void ATwinBladeImpactConsumesTheReversalCutBonusOnlyWhenTheAbilityTriggersIt()
    {
        _temporary[StatType.TwinBladeDuelistReversalCutDamageBonus] = 9;
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.TwinBlade), SkillType.TwinBlade).Should().Be(0);
        Lines("Consume").Should().BeEmpty();

        _reversalCut = true;
        _service.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.TwinBlade), SkillType.TwinBlade).Should().Be(9);
        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.TwinBladeDuelistReversalCutDamageBonus} {StatType.TwinBladeDuelistReversalCut}");
    }

    [Test]
    public void TheHighResourceBonusAsksTheResourcesPartWithTheAbilityOrNone()
    {
        _highResource = 4;
        _service.GetHighResourceAbilityDamageBonus(Attacker, Hostile(SkillType.Pistol)).Should().Be(4);
        _service.GetHighResourceAbilityDamageBonus(Attacker).Should().Be(4);
        _log.Should().Equal(
            $"HighResource {Attacker} {StatType.HighFPAndStaminaAbilityDamageBonus} {StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent} Pistol",
            $"HighResource {Attacker} {StatType.HighFPAndStaminaAbilityDamageBonus} {StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent} null");
    }

    // ------------------------------------------------------------------ costly ability bonus

    [Test]
    public void TheCostlyAbilityBonusNeedsAPaidCostAtLeastTheMinimumForTheSelectedSkill()
    {
        var ability = Hostile();
        Stat(StatType.CostlyAbilityDamageBonus, 12);
        _service.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0, "nothing was paid");

        _costState = new CombatAbilityStaminaCostState { Cost = 8 };
        _service.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0, "no minimum is set");

        Stat(StatType.CostlyAbilityDamageMinimumStaminaCost, 10);
        _service.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0, "the cost is below the minimum");

        _costState = new CombatAbilityStaminaCostState { Cost = 10 };
        _service.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(12, "any skill when none is selected");

        Stat(StatType.CostlyAbilityDamageBonusSkillType, (int)SkillType.Pistol);
        _service.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0);
        _service.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Pistol).Should().Be(12);
    }

    // ------------------------------------------------------------------ successful impact riders: guards and order

    [Test]
    public void NoRiderRunsWithoutAValidActivatorTargetAndAbility()
    {
        Riders(null!, SkillType.Rifle);
        _invalid.Add(Gone);
        _service.ApplySuccessfulAbilityImpactRiders(Gone, Defender, Hostile(), SkillType.Rifle, CombatDamageType.Physical, 10, true, null!,
            Array.Empty<Type>(), true, true, StatusEffectCategory.Debuff);
        Riders(Hostile(), SkillType.Rifle, target: Gone);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnImpactThatAlreadyRanItsHitRidersOnThisTargetRunsNoSecondSet()
    {
        var weapon = new WeaponShotProfile(501, SkillType.Rifle, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        using var profile = new WeaponImpactProfile(Attacker, weapon, new WeaponImpactActivation());

        Riders(Hostile(), SkillType.Rifle);
        Lines("Record").Should().ContainSingle();
        _log.Clear();
        Riders(Hostile(), SkillType.Rifle);
        _log.Should().BeEmpty("the profile's activation already ran the hit riders on this target");
        Riders(Hostile(), SkillType.Rifle, target: Other);
        Lines("Record").Should().Equal($"Record {Attacker} {Other} {SkillType.Rifle}");
    }

    // Every rider of a successful hit is configured, so each one writes to the log; the log must keep the order the riders ran
    // in as static members of Combat: record, party buff, first hit count, source stacks, next auto-attack, same-target hits,
    // the status riders (the skill's, the perk category's, the guarded-hit exposed), the status-applied effects, the target
    // status effects, the near-target effects, the costly ability effects, the damage riders, the area pulse and the area hit
    // sequence.
    [Test]
    public void TheRidersOfASuccessfulHitRunInTheirOrder()
    {
        // An area ability that also counts as single-target, so Throwing's single-target status rider runs too.
        var ability = Hostile(SkillType.Throwing, area: true);
        ability.IsSingleTargetAbility = true;
        _areaImpact = true;
        _sequence = new AbilityImpactSequence();
        _nearby.AddRange(new[] { Other, Third });
        // Party buff.
        Stat(StatType.HostileAbilityPartyBuffDamagePercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 1234);
        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.DamageIncrease);
        // Next auto-attack without delay.
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills, 1);
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds, 6);
        // Same-target hits.
        Stat(StatType.SameTargetHostileAbilityHitCountRequired, 1);
        Stat(StatType.SameTargetHostileAbilityStaminaRestore, 2);
        // Throwing status rider, the perk category's Covering Claws and the guarded-hit Exposed.
        Stat(StatType.ThrowingDeadeyeMarkingToss, 1);
        _perkCategory = true;
        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment, 25);
        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds, 9);
        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Throwing;
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDurationSeconds] = 4;
        // Status-applied effects.
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextSkillAbilitySkillType, (int)SkillType.Throwing);
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Stat(StatType.StatusAppliedNextSkillAbilityWindowSeconds, 8);
        // Target status effects.
        Stat(StatType.AbilityTargetStatusRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.AbilityTargetStatusDurationSeconds, 5);
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        // Near-target effects.
        Stat(StatType.RangedAbilityHitNearTargetRangeMeters, 5);
        Stat(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, -10);
        Stat(StatType.RangedAbilityHitNearTargetDurationSeconds, 6);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef, 77);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectIcon, (int)EffectIconType.DamageDecrease);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes, (int)StatusEffectCleanseType.Purify);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectResistanceType, (int)ResistanceType.Mind);
        // Costly ability effects.
        _costState = new CombatAbilityStaminaCostState { Cost = 20 };
        Stat(StatType.CostlyAbilityStatusMinimumStaminaCost, 10);
        Stat(StatType.CostlyAbilityExposedDurationSeconds, 7);
        // Damage riders: Foggy Mind drain and the area fragmentation, then Throwing's cluster storm.
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDurationSeconds)] = 9;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationPulseSeconds)] = 3;
        Stat(StatType.ThrowingBombardierClusterStormDamageBonus, 4);
        Stat(StatType.ThrowingBombardierClusterStormMaximumTargets, 1);
        // Area pulse.
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = new[]
        {
            Source("pulse", (StatType.AreaAbilityPulseDamage, 6), (StatType.AreaAbilityPulseRadiusMeters, 4))
        };
        // Area hit sequence.
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 1);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);

        Riders(ability, SkillType.Throwing, damage: 10, statusApplied: true, firstHit: true, applied: StatusEffectCategory.Debuff);

        _log.Where(line => !line.StartsWith("Stamina ", StringComparison.Ordinal)).Should().Equal(
            $"Record {Attacker} {Defender} {SkillType.Throwing}",
            $"Trigger {Attacker} {StatType.HostileAbilityPartyBuffDamagePercent} 12",
            $"Apply {Attacker}>{Attacker} HostileAbilityPartyBuffStatusEffect {new HostileAbilityPartyBuffStatusEffect(5, 0, 1234, EffectIconType.DamageIncrease).Name} 30",
            $"FirstHitCount {Attacker} {SkillType.Throwing}",
            $"SourceStack {Attacker} {Defender}",
            $"NoDelay {Attacker} 6",
            $"HitStamina {Attacker} {StatType.SameTargetHostileAbilityStaminaRestore} 2 Throwing",
            $"Apply {Attacker}>{Defender} MarkingTossStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} CoveringClawsStatusEffect 9 {CombatDamageType.Physical}",
            $"Consume {Attacker} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 4 {CombatDamageType.Physical}",
            $"NextSkill {Attacker} {SkillType.Throwing} 3 0 8 0 {StatusEffectCategory.None}",
            $"NextAttack {Attacker} 0 0",
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment}",
            $"Apply {Attacker}>{Defender} DamageDealtAdjustmentStatusEffect {new DamageDealtAdjustmentStatusEffect(-10, 77, EffectIconType.DamageDecrease, StatusEffectCleanseType.Purify, ResistanceType.Mind).Name} DamageDecrease Purify Mind 6 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 7 {CombatDamageType.Physical}",
            $"LoseFP {Defender} {StatType.AbilityResourceDrainFoggyMindFP} 2",
            $"Apply {Attacker}>{Defender} FragmentationStatusEffect 9 {CombatDamageType.Physical}",
            $"Triggered {Attacker} {Other} 4 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Pulse {Attacker}",
            $"Triggered {Attacker} {Other} 6 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Third} 6 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 5 {CombatDamageType.Physical}");
        Visuals().Should().Equal(VisualEffect.Vfx_Fnf_Howl_War_Cry);
    }

    [Test]
    public void ARiderSetWithoutHitProcsOrAFirstHitBonusSkipsTheSourceStacksAndTheFirstHitCount()
    {
        var suppressed = Hostile();
        suppressed.SuppressesSourceStatusStackRiders = true;

        Riders(Hostile(), SkillType.Rifle, allowsHitProcs: false);
        Riders(suppressed, SkillType.Rifle);
        Riders(new AbilityDetail { SkillType = SkillType.Rifle }, SkillType.Rifle, firstHit: true);
        Riders(Hostile(), SkillType.Rifle, firstHit: false);

        Lines("SourceStack").Should().ContainSingle("only the last hit allows procs from a hostile ability that keeps its stacks");
        Lines("FirstHitCount").Should().ContainSingle("the non-hostile hit still counts the first hit it was given");
        Lines("Record").Should().HaveCount(4);
    }

    // ------------------------------------------------------------------ party buff

    [Test]
    public void ThePartyBuffNeedsADamagingOrStatusHitAndAFullConfiguration()
    {
        var ability = Hostile();
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Stat(StatType.HostileAbilityPartyBuffAccuracyPercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        _log.Should().BeEmpty("the icon is missing");

        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.AttackIncrease);
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 0, false);
        _service.ApplyHostileAbilityPartyBuff(Attacker, new AbilityDetail(), 10, true);
        _service.ApplyHostileAbilityPartyBuff(Attacker, null!, 10, true);
        _log.Should().BeEmpty("a miss or a friendly ability buffs nobody");

        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 0, true);
        Lines("Apply").Should().ContainSingle().Which.Should().StartWith($"Apply {Attacker}>{Attacker} HostileAbilityPartyBuffStatusEffect");
    }

    [Test]
    public void ThePartyBuffReachesTheFriendlyTargetsOnceASequenceAndOnlyOffCooldown()
    {
        var ability = Hostile();
        Stat(StatType.HostileAbilityPartyBuffDamagePercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.DamageIncrease);
        _shape.AddRange(new[] { Attacker, Ally, Other });
        _hitPoints[Ally] = 20;
        _hitPoints[Other] = 20;
        _partyMembers.Add(Ally);
        _sequence = new AbilityImpactSequence();

        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Lines("Apply").Select(line => line.Split(' ')[1]).Should().Equal($"{Attacker}>{Attacker}", $"{Attacker}>{Ally}");

        _log.Clear();
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        _log.Should().BeEmpty("the sequence already buffed the party");

        _sequence = null;
        _blockedTriggers.Add(StatType.HostileAbilityPartyBuffDamagePercent);
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Lines("Apply").Should().BeEmpty("the buff is on cooldown");
        Lines("Trigger").Should().ContainSingle();
    }

    // ------------------------------------------------------------------ first hit, source stacks, next auto-attack, same target

    [Test]
    public void TheNextAutoAttackGrantUsesAllSkillsOrTheSelectedSkillAndNeedsADuration()
    {
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, null!);
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType, (int)SkillType.Pistol);
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        _log.Should().BeEmpty("no duration");

        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds, 4);
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, new AbilityDetail());
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills, 1);
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        _log.Should().Equal($"NoDelay {Attacker} {SkillType.Pistol} 4", $"NoDelay {Attacker} 4");
    }

    [Test]
    public void TheSameTargetHitsCountPerTargetAndRestoreStaminaAtTheRequiredCount()
    {
        var ability = Hostile();
        Private("ApplySameTargetHostileAbilityHitEffects", Attacker, Defender, new AbilityDetail());
        Private("ApplySameTargetHostileAbilityHitEffects", Attacker, Defender, ability);
        SameTargetCounts().Should().BeEmpty("nothing is configured");

        Stat(StatType.SameTargetHostileAbilityHitCountRequired, 3);
        Stat(StatType.SameTargetHostileAbilityStaminaRestore, 4);
        Private("ApplySameTargetHostileAbilityHitEffects", Attacker, Defender, ability);
        Private("ApplySameTargetHostileAbilityHitEffects", Attacker, Defender, ability);
        Private("ApplySameTargetHostileAbilityHitEffects", Attacker, Other, ability);
        SameTargetCounts()[(Attacker, Defender)].Should().Be(2);
        SameTargetCounts()[(Attacker, Other)].Should().Be(1);
        Lines("HitStamina").Should().BeEmpty();

        Private("ApplySameTargetHostileAbilityHitEffects", Attacker, Defender, ability);
        SameTargetCounts()[(Attacker, Defender)].Should().Be(0);
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.SameTargetHostileAbilityStaminaRestore} 4 Rifle");
    }

    // ------------------------------------------------------------------ status riders by skill

    [Test]
    public void AHeavyVibrobladeDefenseAbilityAddsEnmityAndCrushingBlow()
    {
        Stat(StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, 30);
        Stat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlow, 1);
        _perkTypes = true;
        Riders(Hostile(SkillType.HeavyVibroblade), SkillType.HeavyVibroblade);
        Lines("Enmity").Should().BeEmpty("the ability is not a Defense trigger");

        _heavyTrigger = true;
        Riders(Hostile(SkillType.HeavyVibroblade), SkillType.HeavyVibroblade);
        _perkTypes = false;
        Stat(StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, 0);
        Riders(Hostile(SkillType.HeavyVibroblade), SkillType.HeavyVibroblade);
        Lines("Enmity").Should().Equal($"Enmity {Attacker} {Defender} 30");
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} CrushingBlowStatusEffect 30 {CombatDamageType.Physical}");
    }

    [Test]
    public void AForceImpactThatAppliedADarkStatusCollapsesWill()
    {
        Riders(Hostile(SkillType.Force), SkillType.Force, primary: typeof(NightmareField1StatusEffect));
        Stat(StatType.DarkManipulatorCollapseWill, 1);
        Riders(Hostile(SkillType.Force), SkillType.Force, primary: typeof(DazedStatusEffect));
        Lines("Apply").Should().BeEmpty();

        Riders(Hostile(SkillType.Force), SkillType.Force, primary: typeof(DazedStatusEffect), additional: new[] { typeof(EclipseOfResolve1StatusEffect) });
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 30 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceErosionStatusEffect 30 {CombatDamageType.Force}");
    }

    [Test]
    public void AKatarImpactOnAPoisonedTargetSpreadsThePoison()
    {
        Riders(Hostile(SkillType.Katar), SkillType.Katar, damage: 0);
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        Riders(Hostile(SkillType.Katar), SkillType.Katar, damage: 0);
        Lines("SpreadPoison").Should().Equal($"SpreadPoison {Attacker} {Defender}");
    }

    [Test]
    public void ALightsaberOffenseImpactSundersDisorientsDisruptsAndPurifiesInThatOrder()
    {
        Stat(StatType.LightsaberOffenseSunderDurationSeconds, 10);
        Stat(StatType.LightsaberOffenseDisorientedDurationSeconds, 6);
        Stat(StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds, 8);
        Stat(StatType.LightsaberOffensePurify, 1);
        Stat(StatType.LightsaberOffensePurifyCooldownSeconds, 30);
        var harmful = WithSource(new DazedStatusEffect(), Other);
        Effects(Attacker, new HostileAbilityPartyBuffStatusEffect(5, 0, 1, EffectIconType.DamageIncrease), harmful);

        Riders(new AbilityDetail { SkillType = SkillType.Lightsaber }, SkillType.Lightsaber);
        Lines("Apply").Should().BeEmpty("a friendly ability carries no offense riders");

        Riders(Hostile(SkillType.Lightsaber), SkillType.Lightsaber);
        _log.Where(line => line.StartsWith("Apply", StringComparison.Ordinal) || line.StartsWith("Remove", StringComparison.Ordinal) || line.StartsWith("Trigger", StringComparison.Ordinal))
            .Should().Equal(
                $"Apply {Attacker}>{Defender} SunderStatusEffect -15 10 {CombatDamageType.Physical}",
                $"Apply {Attacker}>{Defender} DisorientedStatusEffect 6 {ResistanceType.Mind}",
                $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 8 {CombatDamageType.Force}",
                $"Trigger {Attacker} {StatType.LightsaberOffensePurify} 30",
                $"Remove {Attacker} DazedStatusEffect {Other} False",
                $"Apply {Attacker}>{Defender} DazedStatusEffect 30 {CombatDamageType.Force}");
    }

    [Test]
    public void ALightsaberSunderIsNotAppliedOverAnEqualOrStrongerSunder()
    {
        Stat(StatType.LightsaberOffenseSunderDurationSeconds, 10);
        Effects(Defender, new SunderStatusEffect(15));
        Riders(Hostile(SkillType.Lightsaber, area: true), SkillType.Lightsaber);
        Effects(Defender, new SunderStatusEffect(10));
        Riders(Hostile(SkillType.Lightsaber, area: true), SkillType.Lightsaber);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} SunderStatusEffect -15 10 {CombatDamageType.Physical}");
        ((bool)Private("HasSunderPenaltyAtLeast", Other, 1)!).Should().BeFalse("a creature with no Sunder has no penalty");
    }

    [Test]
    public void APurifyNeedsItsTriggerAndAHarmfulStatusToTransfer()
    {
        Stat(StatType.LightsaberOffensePurify, 1);
        Effects(Attacker, new HostileAbilityPartyBuffStatusEffect(5, 0, 1, EffectIconType.DamageIncrease));
        Private("ApplyLightsaberOffensePurify", Attacker, Defender);
        Lines("Remove").Should().BeEmpty("a buff is not a debuff, a control or a bleed");

        Effects(Attacker, new DazedStatusEffect());
        _blockedTriggers.Add(StatType.LightsaberOffensePurify);
        Private("ApplyLightsaberOffensePurify", Attacker, Defender);
        Lines("Remove").Should().BeEmpty("Purify is on cooldown");

        Stat(StatType.LightsaberOffensePurify, 0);
        _log.Clear();
        Private("ApplyLightsaberOffensePurify", Attacker, Defender);
        _log.Should().BeEmpty();
    }

    [Test]
    public void APistolSkirmisherDisorientsOnlyFromAnAreaOrCloseAbility()
    {
        Stat(StatType.PistolSkirmisherDisorientedDurationSeconds, 5);
        Riders(new AbilityDetail { SkillType = SkillType.Pistol }, SkillType.Pistol, damage: 0);
        Riders(Hostile(SkillType.Pistol, maxRange: 10f), SkillType.Pistol, damage: 0);
        Riders(Hostile(SkillType.Pistol, maxRange: 5f), SkillType.Pistol, damage: 0);
        Riders(Hostile(SkillType.Pistol, area: true, maxRange: 10f), SkillType.Pistol, damage: 0);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} DisorientedStatusEffect 5 {ResistanceType.Mind}",
            $"Apply {Attacker}>{Defender} DisorientedStatusEffect 5 {ResistanceType.Mind}");
    }

    [Test]
    public void ARiflePacificationImpactNeutralizesOverwatchesAndPinsByRank()
    {
        Stat(StatType.RiflePacificationNeutralizingShot, 1);
        Stat(StatType.RiflePacificationOverwatch, 1);
        Stat(StatType.RiflePacificationPinningFireRank, 2);
        Riders(Hostile(SkillType.Rifle), SkillType.Rifle, damage: 0);
        _log.Where(line => line.StartsWith("Apply", StringComparison.Ordinal) || line.StartsWith("RemoveBeneficial", StringComparison.Ordinal))
            .Should().Equal(
                $"RemoveBeneficial {Defender} False",
                $"Apply {Attacker}>{Defender} DisorientedStatusEffect 30 {ResistanceType.Mind}",
                $"Apply {Attacker}>{Defender} FoggyMindStatusEffect foggy 30 {ResistanceType.Mind}",
                $"Apply {Attacker}>{Defender} KnockdownStatusEffect 6 {ResistanceType.Trauma}");
        _engine.CallsTo("AssignCommand").Select(call => (uint)call.Arguments[0]!).Should().Equal(Defender);

        _log.Clear();
        Stat(StatType.RiflePacificationNeutralizingShot, 0);
        Stat(StatType.RiflePacificationOverwatch, 0);
        Stat(StatType.RiflePacificationPinningFireRank, 1);
        Riders(Hostile(SkillType.Rifle), SkillType.Rifle, damage: 0);
        Stat(StatType.RiflePacificationPinningFireRank, 0);
        Riders(Hostile(SkillType.Rifle), SkillType.Rifle, damage: 0);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} DazedStatusEffect 30 {ResistanceType.Mind}");
    }

    [Test]
    public void ASaberstaffImpactRunsTheConduitThenTheTempestRider()
    {
        Stat(StatType.SaberstaffConduitAreaConduitFlare, 1);
        Stat(StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds, 4);
        Stat(StatType.SaberstaffTempestForceGyre, 1);
        Stat(StatType.SaberstaffTempestForceGyreDurationSeconds, 6);
        Riders(Hostile(SkillType.Saberstaff, area: true), SkillType.Saberstaff, damage: 0);
        Riders(Hostile(SkillType.Saberstaff), SkillType.Saberstaff, damage: 0);
        Riders(new AbilityDetail { SkillType = SkillType.Saberstaff, IsAreaAbility = true }, SkillType.Saberstaff, damage: 0);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 4 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceErosionStatusEffect 6 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 4 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceErosionStatusEffect 6 {CombatDamageType.Force}");

        _log.Clear();
        Stat(StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds, 0);
        Stat(StatType.SaberstaffTempestForceGyreDurationSeconds, 0);
        Riders(Hostile(SkillType.Saberstaff, area: true), SkillType.Saberstaff, damage: 0);
        Stat(StatType.SaberstaffConduitAreaConduitFlare, 0);
        Stat(StatType.SaberstaffTempestForceGyre, 0);
        Riders(Hostile(SkillType.Saberstaff, area: true), SkillType.Saberstaff, damage: 0);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ASpearImpactRunsTheDamageThenTheDisablerRiders()
    {
        Stat(StatType.SpearDamageBreachStrike, 1);
        Stat(StatType.SpearDamageCripplingDefense, 1);
        Stat(StatType.SpearDisablerForceNullification, 1);
        Stat(StatType.SpearDisablerForcebane, 1);
        Stat(StatType.SpearDisablerFractureStrike, 1);
        Riders(Hostile(SkillType.Spear, area: true), SkillType.Spear, damage: 0, primary: typeof(ForceDisruptionStatusEffect));
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} BreachStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} CrippledDefenseStatusEffect 45 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 30 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForcebaneStatusEffect 45 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} FracturedFocusStatusEffect 30 {CombatDamageType.Force}");

        _log.Clear();
        Riders(Hostile(SkillType.Spear), SkillType.Spear, damage: 0, primary: typeof(ForceSuppressionStatusEffect));
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} BreachStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} ForcebaneStatusEffect 45 {CombatDamageType.Force}");

        _log.Clear();
        _stats.Clear();
        Riders(Hostile(SkillType.Spear, area: true), SkillType.Spear, damage: 0, primary: typeof(ForceDisruptionStatusEffect));
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void TheSpearDisablerSuppressionRidersNeedValidCreaturesAndRunFractureBeforeForcebane()
    {
        Stat(StatType.SpearDisablerFractureStrike, 1);
        Stat(StatType.SpearDisablerForcebane, 1);
        _invalid.Add(Gone);
        _service.ApplySpearDisablerSuppressionRiders(Gone, Defender);
        _service.ApplySpearDisablerSuppressionRiders(Attacker, Gone);
        _log.Should().BeEmpty();

        _service.ApplySpearDisablerSuppressionRiders(Attacker, Defender);
        Stat(StatType.SpearDisablerFractureStrike, 0);
        Stat(StatType.SpearDisablerForcebane, 0);
        _service.ApplySpearDisablerSuppressionRiders(Attacker, Defender);
        _log.Should().Equal(
            $"Apply {Attacker}>{Defender} FracturedFocusStatusEffect 30 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForcebaneStatusEffect 45 {CombatDamageType.Force}");
    }

    [Test]
    public void AStaffThrowingTwinBladeAndVibroknifeImpactApplyTheirStatuses()
    {
        Stat(StatType.StaffCrusherFinisherDazedDurationSeconds, 3);
        Riders(Hostile(SkillType.Staff), SkillType.Staff, damage: 0);
        Stat(StatType.ThrowingDeadeyeMarkingToss, 1);
        Riders(Hostile(SkillType.Throwing, area: true), SkillType.Throwing, damage: 0);
        Riders(Hostile(SkillType.Throwing), SkillType.Throwing, damage: 0);
        _reversalCut = true;
        _temporary[StatType.TwinBladeDuelistReversalCutDazedDurationSeconds] = 4;
        Riders(Hostile(SkillType.TwinBlade), SkillType.TwinBlade, damage: 0);
        Riders(Hostile(SkillType.TwinBlade), SkillType.TwinBlade, damage: 0);
        _reversalCut = false;
        Riders(Hostile(SkillType.TwinBlade), SkillType.TwinBlade, damage: 0);
        Stat(StatType.VibroknifeShadowMarkedForDeath, 1);
        Riders(Hostile(SkillType.Vibroknife, area: true), SkillType.Vibroknife, damage: 0);
        Riders(Hostile(SkillType.Vibroknife), SkillType.Vibroknife, damage: 0);

        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} DazedStatusEffect 3 {ResistanceType.Mind}",
            $"Apply {Attacker}>{Defender} MarkingTossStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} DazedStatusEffect 4 {ResistanceType.Mind}",
            $"Apply {Attacker}>{Defender} MarkedForDeathStatusEffect 30 {CombatDamageType.Physical}");
        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.TwinBladeDuelistReversalCutDazedDurationSeconds} {StatType.TwinBladeDuelistReversalCut}",
            $"Consume {Attacker} {StatType.TwinBladeDuelistReversalCutDazedDurationSeconds} {StatType.TwinBladeDuelistReversalCut}");
    }

    [Test]
    public void AVibroknifeSaboteurImpactCoatsAndSapsADebuffedTargetOffCooldown()
    {
        Stat(StatType.VibroknifeSaboteurToxicCoatingRank, 1);
        Stat(StatType.VibroknifeSaboteurSapVitalityRank, 3);
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(DazedStatusEffect));
        _log.Where(line => !line.StartsWith("Record", StringComparison.Ordinal)).Should().Equal(
            $"Apply {Attacker}>{Defender} ToxinStatusEffect 30 {CombatDamageType.Poison}",
            $"Trigger {Defender} {StatType.VibroknifeSaboteurSapVitalityRank} 6",
            $"LoseStamina {Defender} {StatType.VibroknifeSaboteurSapVitalityRank} 3");

        _log.Clear();
        _blockedTriggers.Add(StatType.VibroknifeSaboteurSapVitalityRank);
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(DazedStatusEffect));
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(ExposedStatusEffect));
        Stat(StatType.VibroknifeSaboteurToxicCoatingRank, 0);
        Stat(StatType.VibroknifeSaboteurSapVitalityRank, 0);
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(DazedStatusEffect));
        Lines("LoseStamina").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle("the second application did not apply a debuff, and the third has no rank");
    }

    [Test]
    public void ThePerkCategoryEnmityStatusNeedsAMatchingAbilityAndAFullConfiguration()
    {
        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment, 25);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0);
        _perkCategory = true;
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0);
        Lines("Apply").Should().BeEmpty("no duration");

        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds, 9);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} CoveringClawsStatusEffect 9 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheGuardedHitExposedStatusIsConsumedOnlyForItsSkillAndOnlyWithADuration()
    {
        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Spear;
        Riders(new AbilityDetail(), SkillType.Rifle, damage: 0);
        Lines("Consume").Should().BeEmpty();

        Riders(new AbilityDetail(), SkillType.Spear, damage: 0);
        Lines("Consume").Should().ContainSingle();
        Lines("Apply").Should().BeEmpty("the stored duration was zero");

        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Spear;
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDurationSeconds] = 6;
        Riders(new AbilityDetail(), SkillType.Spear, damage: 0);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} ExposedStatusEffect 6 {CombatDamageType.Physical}");
    }

    // ------------------------------------------------------------------ status-applied effects

    [Test]
    public void TheStatusAppliedEffectsNeedTheRequiredCategory()
    {
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Control);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Lines("NextSkill").Should().BeEmpty();

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff | StatusEffectCategory.Control);
        Lines("NextSkill").Should().ContainSingle();
    }

    [Test]
    public void TheStatusAppliedEffectsGrantTheNextSkillAndAttackBonusesShowTheCueAndRunTheSelfTargetAndAllyStagesInOrder()
    {
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextSkillAbilitySkillType, (int)SkillType.Katar);
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Stat(StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(StatType.StatusAppliedNextSkillAbilityWindowSeconds, 8);
        Stat(StatType.StatusAppliedNextAttackDamageBonus, 6);
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 10);
        _temporary[StatType.NextAttackStatusAppliedDMGBonus] = 2;
        Stat(StatType.StatusAppliedSelfDurationSeconds, 12);
        Stat(StatType.StatusAppliedSelfAttackDeflection, 1);
        Stat(StatType.StatusAppliedSelfDefensePercentAdjustment, 5);
        Stat(StatType.StatusAppliedSelfEvasionPercentAdjustment, 6);
        Stat(StatType.StatusAppliedSelfForceAttackPercentAdjustment, 7);
        Stat(StatType.StatusAppliedSelfHastePercentAdjustment, 8);
        Stat(StatType.StatusAppliedSelfEnmityPercentAdjustment, 9);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 4);
        Stat(StatType.StatusAppliedTargetDurationSeconds, 5);
        Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.StatusAppliedTargetAccuracyPercentAdjustment, -15);
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds, 20);

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff, attackingAlly: true);

        var deflection = StatRules.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection);
        _log.Where(line => !line.StartsWith("Record", StringComparison.Ordinal)).Should().Equal(
            $"NextSkill {Attacker} {SkillType.Katar} 3 5 8 0 {StatusEffectCategory.None}",
            $"Cue {Attacker} True 6 {ReadyCue.Damage("Charged Blows", 6)}",
            $"NextAttack {Attacker} 6 10",
            $"Replace {Attacker} {deflection} 1 12 {StatType.StatusAppliedSelfAttackDeflection}",
            $"Replace {Attacker} {StatType.PhysicalDefensePercentAdjustment} 5 12 {StatType.StatusAppliedSelfDefensePercentAdjustment}",
            $"Replace {Attacker} {StatType.EvasionPercentAdjustment} 6 12 {StatType.StatusAppliedSelfEvasionPercentAdjustment}",
            $"Replace {Attacker} {StatType.ForceAttackPercentAdjustment} 7 12 {StatType.StatusAppliedSelfForceAttackPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 8 12 {StatType.StatusAppliedSelfHastePercentAdjustment}",
            $"Replace {Attacker} {StatType.EnmityPercentAdjustment} 9 12 {StatType.StatusAppliedSelfEnmityPercentAdjustment}",
            $"HitStamina {Attacker} {StatType.StatusAppliedSelfStaminaRestore} 4",
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment}",
            $"Replace {Defender} {StatType.AccuracyPercentAdjustment} -15 5 {StatType.StatusAppliedTargetAccuracyPercentAdjustment}",
            $"Apply {Attacker}>{Defender} ChallengeEnmityStatusEffect 40 20");
    }

    [Test]
    public void TheReadyCueShowsOnlyWithANextAttackWindowAndTheSelfAndTargetStagesNeedTheirDurations()
    {
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextAttackDamageBonus, 6);
        Stat(StatType.StatusAppliedSelfDefensePercentAdjustment, 5);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 4);
        Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.StatusAppliedSelfDurationSeconds, 12);
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 10);

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Lines("Cue").Should().Equal(new[] { $"Cue {Attacker} False 6 {ReadyCue.Damage("Charged Blows", 6)}" }, "nothing was armed before");
        Lines("Replace").Should().Equal(
            new[] { $"Replace {Attacker} {StatType.PhysicalDefensePercentAdjustment} 5 12 {StatType.StatusAppliedSelfDefensePercentAdjustment}" },
            "the target stage has no duration and the zero self stats are skipped");

        _log.Clear();
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 0);
        Stat(StatType.StatusAppliedSelfDurationSeconds, 0);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 0);
        Stat(StatType.StatusAppliedTargetDurationSeconds, 5);
        _invalid.Add(Gone);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff, target: Defender);
        Lines("Cue").Should().BeEmpty();
        Lines("Replace").Should().Equal(
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment}");
        Lines("NextAttack").Should().Equal(new[] { $"NextAttack {Attacker} 6 0" }, "the grant still runs and keeps its own window check");

        _log.Clear();
        Private("ApplyStatusAppliedTargetEffects", Attacker, Gone);
        Private("ReplaceTemporaryStat", Attacker, StatType.Invalid, 5, 5, StatType.Invalid);
        Private("ReplaceTemporaryStat", Attacker, StatType.EvasionPercentAdjustment, 5, 0, StatType.Invalid);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheAttackingAllyEnmityDoesNotReplaceAStrongerChallengeFromTheSameSource()
    {
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().BeEmpty("no duration");

        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds, 20);
        _invalid.Add(Gone);
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Gone);
        Effects(Defender,
            WithSource(new ChallengeEnmityStatusEffect(60), Attacker),
            WithSource(new SunderStatusEffect(5), Attacker));
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().BeEmpty("the activator's own Challenge is stronger");

        Effects(Defender, WithSource(new ChallengeEnmityStatusEffect(60), Other), WithSource(new ChallengeEnmityStatusEffect(30), Attacker));
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} ChallengeEnmityStatusEffect 40 20");
    }

    [Test]
    public void ATargetIsAttackingAnAllyThroughItsAttackTargetOrItsEnmityTable()
    {
        _service.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender).Should().BeFalse("the activator has no rider that needs it");
        _log.Should().BeEmpty();

        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        _attackTarget = Ally;
        _partyMembers.Add(Ally);
        _service.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender).Should().BeTrue();
        _log.Should().Equal($"InParty {Attacker} {Ally}");

        _log.Clear();
        _attackTarget = NWScript.OBJECT_INVALID;
        _highestEnmity = Ally;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeTrue();
        _log.Should().Equal($"HighestEnmity {Defender}", $"InParty {Attacker} {Ally}");

        _highestEnmity = Attacker;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeFalse("the creature itself is not its ally");
        _highestEnmity = NWScript.OBJECT_INVALID;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeFalse("nothing is attacked");
        _attackTarget = Other;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeFalse("the attacked creature is not in the party");
        _invalid.Add(Gone);
        _service.IsAttackingAllyOf(Gone, Attacker).Should().BeFalse();
        _service.IsAttackingAllyOf(Defender, Gone).Should().BeFalse();
    }

    // ------------------------------------------------------------------ target status, near target, costly

    [Test]
    public void TheTargetStatusEffectNeedsAHostileAbilityAValidTargetAndTheRequiredCategory()
    {
        Stat(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.AbilityTargetStatusDurationSeconds, 5);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        Stat(StatType.AbilityTargetStatusRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, new AbilityDetail());
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, null!);
        _invalid.Add(Defender);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _log.Should().Equal($"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment}");
    }

    private void NearTargetStats()
    {
        Stat(StatType.RangedAbilityHitNearTargetRangeMeters, 5);
        Stat(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, -10);
        Stat(StatType.RangedAbilityHitNearTargetDurationSeconds, 6);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef, 77);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectIcon, (int)EffectIconType.DamageDecrease);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes, (int)StatusEffectCleanseType.Purify);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectResistanceType, (int)ResistanceType.Mind);
    }

    [TestCase(StatType.RangedAbilityHitNearTargetRangeMeters, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetDurationSeconds, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectIcon, -1)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectResistanceType, 0)]
    public void TheNearTargetEffectNeedsEveryPartOfItsConfiguration(StatType missing, int value)
    {
        NearTargetStats();
        Stat(missing, value);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheNearTargetEffectAppliesOnlyFromAHostileRangedAbilityWithinRange()
    {
        NearTargetStats();
        _invalid.Add(Gone);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, new AbilityDetail(), SkillType.Pistol);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, null!, SkillType.Pistol);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Gone, Hostile(SkillType.Pistol), SkillType.Pistol);
        _distances[Defender] = 6f;
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol);
        _log.Should().BeEmpty();

        _distances[Defender] = 5f;
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol);
        Lines("Apply").Should().ContainSingle().Which.Should().Contain("DamageDealtAdjustmentStatusEffect").And.EndWith($" 6 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheCostlyHitRestoresStaminaOncePerCostAndExposesForTheSelectedSkills()
    {
        var ability = Hostile();
        Stat(StatType.CostlyAbilityHitStaminaRestore, 5);
        Stat(StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost, 10);
        Stat(StatType.CostlyAbilityStatusMinimumStaminaCost, 15);
        Stat(StatType.CostlyAbilityExposedDurationSeconds, 4);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, new AbilityDetail(), SkillType.Rifle);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, null!, SkillType.Rifle);
        _log.Should().BeEmpty("nothing was paid");

        _costState = new CombatAbilityStaminaCostState { Cost = 12 };
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _costState.StaminaRestoreApplied.Should().BeTrue();
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _log.Should().Equal($"HitStamina {Attacker} {StatType.CostlyAbilityHitStaminaRestore} 5 Rifle");

        _log.Clear();
        _costState = new CombatAbilityStaminaCostState { Cost = 20 };
        Stat(StatType.CostlyAbilityHitStaminaRestoreSkillType, (int)SkillType.Pistol);
        Stat(StatType.CostlyAbilityStatusSkillType, (int)SkillType.Rifle);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _log.Should().Equal($"Apply {Attacker}>{Defender} ExposedStatusEffect 4 {CombatDamageType.Physical}");

        _log.Clear();
        _stats.Clear();
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _log.Should().BeEmpty("nothing is configured");
    }

    // ------------------------------------------------------------------ damage riders

    [Test]
    public void ANonDamagingImpactRunsNoDamageRider()
    {
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        Stat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 5);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheFoggyMindDrainNeedsAHostileAbilityAndAFoggyTarget()
    {
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        Stat(StatType.AbilityResourceDrainFoggyMindStamina, 3);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, new AbilityDetail());
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, null!);
        _invalid.Add(Defender);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 0);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        Stat(StatType.AbilityResourceDrainFoggyMindStamina, 0);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        _log.Should().Equal(
            $"LoseFP {Defender} {StatType.AbilityResourceDrainFoggyMindFP} 2",
            $"LoseStamina {Defender} {StatType.AbilityResourceDrainFoggyMindStamina} 3",
            $"LoseStamina {Defender} {StatType.AbilityResourceDrainFoggyMindStamina} 3");
    }

    [Test]
    public void TheDamageRidersRunDrainRefreshSpreadSplashAndFragmentationBeforeTheSkillRiders()
    {
        Random.SetSeed(7);
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        _sourceStatuses.Add((Defender, typeof(BleedStatusEffect), Attacker));
        _skillSelected = 4;
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, 100);
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 12);
        _impactSources[StatType.BleedingTargetAbilitySplashDamage] = new[]
        {
            Source("splash", (StatType.BleedingTargetAbilitySplashChance, 100), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 1),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Katar))
        };
        _sequence = new AbilityImpactSequence();
        _areaImpact = true;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDurationSeconds)] = 9;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationPulseSeconds)] = 3;
        Stat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 5);
        _nearby.Add(Other);

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 10);

        _log.Should().Equal(
            $"LoseFP {Defender} {StatType.AbilityResourceDrainFoggyMindFP} 2",
            $"SkillSelected {Attacker} {SkillType.Katar} {StatType.AbilityDamageToBleedingTargetSkillType} {StatType.BleedingTargetAbilityBleedDurationExtensionSeconds} False",
            $"Extend {Defender} BleedStatusEffect {Attacker} 4",
            $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}",
            $"Triggered {Attacker} {Other} 3 {CombatDamageType.Physical} {SkillType.Katar} False null False",
            $"Apply {Attacker}>{Defender} FragmentationStatusEffect 9 {CombatDamageType.Physical}",
            $"Triggered {Attacker} {Defender} 5 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Apply {Attacker}>{Defender} BleedStatusEffect 30 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheBleedSpreadAndSplashDrawOneRollEachInThatOrderAndOnlyWhenTheyCanTrigger()
    {
        var rolls = Rolls(11, 4);
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, rolls[0]);
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 12);
        _impactSources[StatType.BleedingTargetAbilitySplashDamage] = new[]
        {
            Source("splash", (StatType.BleedingTargetAbilitySplashChance, rolls[1] - 1), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 1),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Katar))
        };
        _sequence = new AbilityImpactSequence();
        _nearby.Add(Other);

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar, area: true), SkillType.Katar, CombatDamageType.Physical, 10);

        Lines("Apply").Should().Equal(new[] { $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}" },
            "the spread's roll was its chance");
        Lines("Triggered").Should().BeEmpty("the splash's roll was one above its chance");
        Random.D100(1).Should().Be(rolls[2], "the spread and the splash drew one roll each");

        // The splash rider was consumed by the sequence, so a second impact draws no splash roll; the spread draws again.
        Rolls(11, 4);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar, area: true), SkillType.Katar, CombatDamageType.Physical, 10);
        Random.D100(1).Should().Be(rolls[1], "only the spread drew");

        // A non-physical hit, a non-weapon skill or no chance draws nothing for the spread.
        Rolls(11, 4);
        _sequence = new AbilityImpactSequence();
        _impactSources.Clear();
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Force), SkillType.Force, CombatDamageType.Force, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Fire, 10);
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, 0);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 10);
        Random.D100(1).Should().Be(rolls[0], "nothing drew");
    }

    [Test]
    public void TheBleedSpreadReachesAtLeastOneTargetAndNeedsABleedingTarget()
    {
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, 100);
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 12);
        Stat(StatType.BleedingTargetAbilityBleedSpreadMaxTargets, 0);
        _nearby.AddRange(new[] { Other, Third });
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty("the target is not bleeding");

        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        Stat(StatType.BleedingTargetAbilityBleedSpreadMaxTargets, 2);
        Lines("Apply").Should().Equal(new[] { $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}" },
            "no maximum means one target");
        _log.Clear();
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Third} BleedStatusEffect 12 {CombatDamageType.Physical}");

        _log.Clear();
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 0);
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheBleedRefreshExtendsOnlyTheActivatorsBleedByTheSelectedExtension()
    {
        _skillSelected = 6;
        Private("ApplyBleedingTargetAbilityBleedRefresh", Attacker, Defender, SkillType.Throwing);
        _log.Should().BeEmpty("the target has no bleed from the attacker");

        _sourceStatuses.Add((Defender, typeof(BleedStatusEffect), Attacker));
        _skillSelected = 0;
        Private("ApplyBleedingTargetAbilityBleedRefresh", Attacker, Defender, SkillType.Throwing);
        Lines("Extend").Should().BeEmpty();
        _skillSelected = 6;
        Private("ApplyBleedingTargetAbilityBleedRefresh", Attacker, Defender, SkillType.Throwing);
        Lines("Extend").Should().Equal($"Extend {Defender} BleedStatusEffect {Attacker} 6");
    }

    [Test]
    public void TheBleedSplashSkipsSourcesThatCannotTriggerOrThatTheSequenceConsumed()
    {
        _impactSources[StatType.BleedingTargetAbilitySplashDamage] = new[]
        {
            Source("wrongSkill", (StatType.BleedingTargetAbilitySplashChance, 100), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 1),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Spear)),
            Source("splash", (StatType.BleedingTargetAbilitySplashChance, 100), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 2),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Katar))
        };
        _nearby.AddRange(new[] { Other, Third });
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty("the target is not bleeding");

        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty("without an impact sequence nothing can consume the rider");

        _sequence = new AbilityImpactSequence();
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().Equal(
            $"Triggered {Attacker} {Other} 3 {CombatDamageType.Physical} {SkillType.Katar} False null False",
            $"Triggered {Attacker} {Third} 3 {CombatDamageType.Physical} {SkillType.Katar} False null False");
    }

    [Test]
    public void APistolSingleTargetImpactRicochetsOffCooldownToEveryNearbyTargetButTheFirst()
    {
        Stat(StatType.PistolSkirmisherRicochetDamageBonus, 4);
        Stat(StatType.PistolSkirmisherRicochetMaximumTargets, 3);
        Stat(StatType.PistolSkirmisherRicochetCooldownSeconds, 9);
        _nearby.AddRange(new[] { Defender, Other });
        _hitPoints[Defender] = 30;

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol, CombatDamageType.Physical, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Pistol, area: true), SkillType.Pistol, CombatDamageType.Physical, 10);
        _blockedTriggers.Add(StatType.PistolSkirmisherRicochetDamageBonus);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol, CombatDamageType.Physical, 10);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.PistolSkirmisherRicochetDamageBonus} 9",
            $"Triggered {Attacker} {Other} 4 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Trigger {Attacker} {StatType.PistolSkirmisherRicochetDamageBonus} 9");

        _log.Clear();
        Stat(StatType.PistolSkirmisherRicochetMaximumTargets, 0);
        Private("ApplyRicochetDamage", Attacker, Defender, CombatDamageType.Physical, StatType.PistolSkirmisherRicochetDamageBonus,
            StatType.PistolSkirmisherRicochetMaximumTargets, StatType.PistolSkirmisherRicochetCooldownSeconds);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AThrowingImpactRicochetsFromASingleTargetAndStormsAndTossesFromAnArea()
    {
        Stat(StatType.ThrowingDeadeyeRicochetDamageBonus, 2);
        Stat(StatType.ThrowingDeadeyeRicochetMaximumTargets, 2);
        Stat(StatType.ThrowingBombardierClusterStormDamageBonus, 4);
        Stat(StatType.ThrowingBombardierClusterStormMaximumTargets, 2);
        Stat(StatType.ThrowingBombardierSaturationToss, 1);
        Stat(StatType.ThrowingBombardierSaturationTossDurationSeconds, 6);
        Stat(StatType.ThrowingBombardierSaturationTossDamage, 3);
        Stat(StatType.ThrowingBombardierSaturationTossPulseSeconds, 2);
        _sequence = new AbilityImpactSequence();
        _nearby.AddRange(new[] { Other, Third });

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Throwing), SkillType.Throwing, CombatDamageType.Physical, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing, CombatDamageType.Fire, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing, CombatDamageType.Fire, 10);

        _log.Should().Equal(
            $"Triggered {Attacker} {Other} 2 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Third} 2 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Other} 4 {CombatDamageType.Fire} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Third} 4 {CombatDamageType.Fire} {SkillType.Invalid} False null False",
            $"Apply {Attacker}>{Defender} SaturationTossStatusEffect 6 {CombatDamageType.Fire}",
            $"Apply {Attacker}>{Defender} SaturationTossStatusEffect 6 {CombatDamageType.Fire}");
        Visuals().Should().Equal(VisualEffect.Vfx_Dur_Aura_Fire, VisualEffect.Vfx_Imp_Flame_S, VisualEffect.Vfx_Dur_Aura_Fire, VisualEffect.Vfx_Imp_Flame_S);
    }

    [Test]
    public void TheClusterStormNeedsItsConfigurationAndASequenceAndTheTossNeedsItsStatusToApply()
    {
        Stat(StatType.ThrowingBombardierClusterStormDamageBonus, 4);
        _nearby.Add(Other);
        Private("ApplyClusterStormDamage", Attacker, Defender, CombatDamageType.Physical);
        Stat(StatType.ThrowingBombardierClusterStormMaximumTargets, 2);
        Private("ApplyClusterStormDamage", Attacker, Defender, CombatDamageType.Physical);
        _log.Should().BeEmpty("there is no impact sequence");

        Stat(StatType.ThrowingBombardierSaturationToss, 1);
        Private("ApplySaturationToss", Attacker, Defender);
        Stat(StatType.ThrowingBombardierSaturationTossDurationSeconds, 6);
        Stat(StatType.ThrowingBombardierSaturationTossDamage, 3);
        Stat(StatType.ThrowingBombardierSaturationTossPulseSeconds, 2);
        _statusApplies = false;
        Private("ApplySaturationToss", Attacker, Defender);
        Lines("Apply").Should().ContainSingle();
        Visuals().Should().BeEmpty("the toss did not apply");
    }

    [Test]
    public void AKatarSingleTargetImpactStrikesAgainAndBleedsAPoisonedTarget()
    {
        Stat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 5);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar, area: true), SkillType.Katar, CombatDamageType.Physical, 10);
        _log.Should().Equal($"Triggered {Attacker} {Defender} 5 {CombatDamageType.Physical} {SkillType.Invalid} False null False");
    }

    // ------------------------------------------------------------------ area pulse and fragmentation

    [Test]
    public void ThePulseSourcesNeedTheFirstTargetOfAHostileAreaImpact()
    {
        var sources = new[] { Source("pulse", (StatType.AreaAbilityPulseDamage, 6), (StatType.AreaAbilityPulseRadiusMeters, 4)) };
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = sources;
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, Hostile(), true)!).Should().BeEmpty();
        _areaImpact = true;
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, Hostile(), false)!).Should().BeEmpty();
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, new AbilityDetail(), true)!).Should().BeEmpty();
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, null!, true)!).Should().BeEmpty();
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, Hostile(), true)!).Should().BeSameAs(sources);
    }

    [Test]
    public void ThePulseRunsOncePerTriggerAndSkipsIncompleteSources()
    {
        _areaImpact = true;
        _nearby.Add(Other);
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = new[]
        {
            Source("noRadius", (StatType.AreaAbilityPulseDamage, 6)),
            Source("pulse", (StatType.AreaAbilityPulseDamage, 6), (StatType.AreaAbilityPulseRadiusMeters, 4)),
            Source("noDamage", (StatType.AreaAbilityPulseRadiusMeters, 4)),
            Source("second", (StatType.AreaAbilityPulseDamage, 2), (StatType.AreaAbilityPulseRadiusMeters, 2))
        };

        Private("ApplyAreaAbilityPulse", Attacker, Defender, Hostile(area: true), true);
        _pulseReady = false;
        Private("ApplyAreaAbilityPulse", Attacker, Defender, Hostile(area: true), true);
        _log.Should().Equal(
            $"Pulse {Attacker}",
            $"Triggered {Attacker} {Other} 6 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Other} 2 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Pulse {Attacker}");
        Visuals().Should().Equal(VisualEffect.Vfx_Fnf_Howl_War_Cry);

        _log.Clear();
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = new[] { Source("noRadius", (StatType.AreaAbilityPulseDamage, 6)) };
        Private("ApplyAreaAbilityPulse", Attacker, Defender, Hostile(area: true), true);
        _log.Should().BeEmpty("no source can pulse, so the trigger is not spent");
    }

    [Test]
    public void TheFragmentationFiltersPersistentAndTemporarySourcesByTheirOwnSkill()
    {
        _permanent[(Attacker, StatType.AreaAbilityFragmentationSkillType)] = (int)SkillType.Throwing;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _temporary[StatType.AreaAbilityFragmentationSkillType] = (int)SkillType.Staff;
        _temporary[StatType.AreaAbilityFragmentationDamage] = 4;

        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Throwing, StatType.AreaAbilityFragmentationDamage)!).Should().Be(3);
        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Staff, StatType.AreaAbilityFragmentationDamage)!).Should().Be(4);
        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Rifle, StatType.AreaAbilityFragmentationDamage)!).Should().Be(0);
        _permanent.Remove((Attacker, StatType.AreaAbilityFragmentationSkillType));
        _temporary.Remove(StatType.AreaAbilityFragmentationSkillType);
        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Rifle, StatType.AreaAbilityFragmentationDamage)!).Should().Be(7);
    }

    [Test]
    public void TheFragmentationNeedsAnAreaImpactOnAValidTargetAndAFullConfiguration()
    {
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDurationSeconds)] = 9;
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Physical);
        _areaImpact = true;
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, null!, SkillType.Rifle, CombatDamageType.Physical);
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Physical);
        _permanent[(Attacker, StatType.AreaAbilityFragmentationPulseSeconds)] = 3;
        _invalid.Add(Defender);
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Physical);
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Sonic);
        _log.Should().Equal($"Apply {Attacker}>{Defender} FragmentationStatusEffect 9 {CombatDamageType.Sonic}");
    }

    // ------------------------------------------------------------------ area hit sequence

    [Test]
    public void TheAreaHitSequenceCountsHitsOnATargetWithinItsWindowAndExposesAtTheRequiredCount()
    {
        _areaImpact = true;
        var ability = Hostile(SkillType.Throwing, area: true);
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 3);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);

        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        _now = _now.AddSeconds(6);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        HitSequences().Count.Should().Be(1);
        _now = _now.AddSeconds(7);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        Lines("Apply").Should().BeEmpty("the window lapsed, so the count started again at one");

        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} ExposedStatusEffect 5 {CombatDamageType.Physical}");
        HitSequences().Count.Should().Be(0, "the sequence is spent");
    }

    [TestCase(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Staff)]
    [TestCase(StatType.AreaAbilityTargetHitSequenceCountRequired, 0)]
    [TestCase(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 0)]
    [TestCase(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 0)]
    public void TheAreaHitSequenceNeedsEveryPartOfItsConfiguration(StatType missing, int value)
    {
        _areaImpact = true;
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 1);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        Stat(missing, value);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing);
        _log.Should().BeEmpty();
        HitSequences().Count.Should().Be(0);
    }

    [Test]
    public void TheAreaHitSequenceNeedsAnAreaImpactOnAValidTarget()
    {
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 1);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing);
        _areaImpact = true;
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, null!, SkillType.Throwing);
        _invalid.Add(Defender);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ status-applied stamina drain

    [Test]
    public void TheStatusAppliedStaminaDrainShowsWhatTheTargetActuallyLost()
    {
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _stamina[Defender] = 4;
        _staminaDrained = 4;

        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff | StatusEffectCategory.Control);

        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.StatusAppliedTargetStaminaDrain} 12",
            $"Stamina {Defender}",
            $"Attribute {Defender} {StatType.StatusAppliedTargetStaminaDrain}",
            $"ReduceStamina {Defender} 6",
            $"Stamina {Defender}");
        FloatingText().Should().Equal($"{Defender} {SWLOR.Game.Server.Service.ColorToken.Combat("-4 STM")}");
    }

    [Test]
    public void TheStatusAppliedStaminaDrainNeedsAnotherValidCreatureTheCategoryAndItsTrigger()
    {
        _invalid.Add(Gone);
        _service.ApplyStatusAppliedTargetStaminaDrain(Gone, Defender, StatusEffectCategory.Debuff);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Gone, StatusEffectCategory.Debuff);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Attacker, StatusEffectCategory.Debuff);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Control);
        _log.Should().BeEmpty();

        _blockedTriggers.Add(StatType.StatusAppliedTargetStaminaDrain);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        _log.Should().Equal($"Trigger {Attacker} {StatType.StatusAppliedTargetStaminaDrain} 12");
        FloatingText().Should().BeEmpty();
    }

    // ------------------------------------------------------------------ finished impact effects

    private static AbilityImpactSummary Summary(SkillType skill, int targets, bool area = true) => new()
    {
        Ability = Hostile(skill, area),
        SkillType = skill,
        IsAreaAbility = area,
        IsSingleTargetAbility = !area,
        ImpactedTargetCount = targets
    };

    [Test]
    public void AFinishedImpactNeedsAValidActivatorASummaryAndATarget()
    {
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 1);
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 3);
        _invalid.Add(Gone);
        _service.ApplyAbilityImpactEffects(Gone, Summary(SkillType.Throwing, 3));
        _service.ApplyAbilityImpactEffects(Attacker, null!);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 0));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 3, area: false));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3, area: false));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 3, area: false));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Rifle, 3));
        _log.Should().BeEmpty();
    }

    [Test]
    public void AFinishedAreaImpactGrantsTheAreaHitRewardsBeforeTheThrowingEffects()
    {
        _sources[(Attacker, StatType.AreaAbilityHastePerStack)] = new[]
        {
            Source("haste", (StatType.AreaAbilityHastePerStack, 5), (StatType.AreaAbilityHasteStackMinimumTargets, 2),
                (StatType.AreaAbilityHasteStackDurationSeconds, 10), (StatType.AreaAbilityHasteStackMaximumPercent, 20),
                (StatType.AreaAbilityHasteStacksPerAdditionalTarget, 1))
        };
        _sources[(Attacker, StatType.AreaHitStaminaRestorePerTarget)] = new[]
        {
            Source("stamina", (StatType.AreaHitStaminaRestorePerTarget, 2), (StatType.AreaHitStaminaRestoreMaximum, 5)),
            Source("noMaximum", (StatType.AreaHitStaminaRestorePerTarget, 2))
        };
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 3);
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 4);
        Stat(StatType.ThrowingAreaAbilityAttackPercentPerTarget, 5);
        Stat(StatType.ThrowingAreaAbilityAttackDurationSeconds, 10);
        Stat(StatType.ThrowingAreaAbilityAttackPercentMax, 25);

        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 4));

        _log.Should().Equal(
            $"AddCapped {Attacker} {StatType.AttackDelayReductionPercent} 5 10 20 haste:{(int)StatType.AreaAbilityHastePerStack} 3 False",
            $"HitStamina {Attacker} {StatType.AreaHitStaminaRestorePerTarget} 5 Throwing",
            $"HitStamina {Attacker} {StatType.ThrowingAreaAbilityMinTargetsStaminaRestore} 4 Throwing",
            $"AddCapped {Attacker} {StatType.AttackPercentAdjustment} 5 10 25 {StatType.ThrowingAreaAbilityAttackPercentPerTarget} 4 False");
    }

    [Test]
    public void TheAreaHasteStacksOncePerSourceOrPerAdditionalTargetFromItsThreshold()
    {
        _sources[(Attacker, StatType.AreaAbilityHastePerStack)] = new[]
        {
            Source("flat", (StatType.AreaAbilityHastePerStack, 3), (StatType.AreaAbilityHasteStackMinimumTargets, 2),
                (StatType.AreaAbilityHasteStackDurationSeconds, 6), (StatType.AreaAbilityHasteStackMaximumPercent, 9)),
            Source("unset", (StatType.AreaAbilityHastePerStack, 3)),
            Source("high", (StatType.AreaAbilityHastePerStack, 3), (StatType.AreaAbilityHasteStackMinimumTargets, 5))
        };

        _service.ApplyAreaHitHaste(Attacker, 4);
        _service.ApplyAreaHitHaste(Attacker, 1);
        _log.Should().Equal($"AddCapped {Attacker} {StatType.AttackDelayReductionPercent} 3 6 9 flat:{(int)StatType.AreaAbilityHastePerStack} 1 False");
    }

    [Test]
    public void TheThrowingAreaEffectsNeedTheirThresholdsAndFullConfigurations()
    {
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 5);
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 4);
        Stat(StatType.ThrowingAreaAbilityAttackPercentPerTarget, 5);
        Stat(StatType.ThrowingAreaAbilityAttackDurationSeconds, 10);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 4));
        _log.Should().BeEmpty("too few targets and no attack maximum");
    }

    [Test]
    public void ASaberstaffAreaImpactRestoresResourcesOffCooldownAndBuffsItsActivatorAboveItsThresholds()
    {
        Stat(StatType.AreaAbilityMinTargetsResourceRestoreThreshold, 2);
        Stat(StatType.AreaAbilityFPRestore, 3);
        Stat(StatType.AreaAbilityStaminaRestore, 4);
        Stat(StatType.AreaAbilityResourceRestoreCooldownSeconds, 15);
        Stat(StatType.AreaAbilityMinTargetsBuffThreshold, 3);
        Stat(StatType.AreaAbilityBuffDurationSeconds, 8);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 10);
        Stat(StatType.AreaAbilityAttackDeflection, 1);

        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));

        var deflection = StatRules.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 15",
            $"FP {Attacker} {StatType.AreaAbilityFPRestore} 3",
            $"RestoredFP {Attacker}",
            $"HitStamina {Attacker} {StatType.AreaAbilityStaminaRestore} 4 Saberstaff",
            $"RestoredBoth {Attacker}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 10 8 {StatType.AttackDelayReductionPercent}",
            $"Replace {Attacker} {deflection} 1 8 {StatType.AreaAbilityAttackDeflection}",
            $"GrantedDeflection {Attacker} {StatRules.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection)}");
    }

    [Test]
    public void ASaberstaffAreaImpactRestoresNothingItCannotAndSkipsTheBuffBelowItsThresholdOrWithoutADuration()
    {
        Stat(StatType.AreaAbilityMinTargetsResourceRestoreThreshold, 2);
        Stat(StatType.AreaAbilityStaminaRestore, 4);
        _restoredStamina = 0;
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 0",
            $"HitStamina {Attacker} {StatType.AreaAbilityStaminaRestore} 4 Saberstaff");

        _log.Clear();
        _restoredStamina = -1;
        _restoredFP = 0;
        Stat(StatType.AreaAbilityStaminaRestore, 0);
        Stat(StatType.AreaAbilityFPRestore, 3);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _blockedTriggers.Add(StatType.AreaAbilityFPRestore);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 1));
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 0",
            $"FP {Attacker} {StatType.AreaAbilityFPRestore} 3",
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 0");

        _log.Clear();
        Stat(StatType.AreaAbilityMinTargetsResourceRestoreThreshold, 0);
        Stat(StatType.AreaAbilityMinTargetsBuffThreshold, 3);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 10);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));
        Stat(StatType.AreaAbilityBuffDurationSeconds, 8);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 0);
        Stat(StatType.AreaAbilityAttackDeflection, 0);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));
        _log.Should().BeEmpty();
    }

    [Test]
    public void ASaberstaffAreaDeflectionWithNoGrantedStatGrantsNothingAfterTheHaste()
    {
        Stat(StatType.AreaAbilityMinTargetsBuffThreshold, 3);
        Stat(StatType.AreaAbilityBuffDurationSeconds, 8);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 10);
        Stat(StatType.AreaAbilityAttackDeflection, 1);
        _noGrantedDeflection = true;

        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));

        _log.Should().Equal($"Replace {Attacker} {StatType.AttackDelayReductionPercent} 10 8 {StatType.AttackDelayReductionPercent}");
    }

    [Test]
    public void ASpearAreaImpactRestoresStaminaAtItsMinimumTargets()
    {
        Stat(StatType.SpearDamageCripplingDefenseMinimumTargets, 3);
        Stat(StatType.SpearDamageCripplingDefenseStaminaRestore, 2);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 2));
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 3));
        Stat(StatType.SpearDamageCripplingDefenseStaminaRestore, 0);
        _service.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 3));
        _log.Should().Equal($"HitStamina {Attacker} {StatType.SpearDamageCripplingDefenseStaminaRestore} 2 Spear");
    }

    // ------------------------------------------------------------------ forgetting a creature

    [Test]
    public void ForgettingACreatureDropsEveryHitCountAndSequenceItIsPartOf()
    {
        SameTargetCounts()[(Attacker, Defender)] = 1;
        SameTargetCounts()[(Defender, Other)] = 2;
        SameTargetCounts()[(Other, Third)] = 3;
        _areaImpact = true;
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 3);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceCountRequired, 3);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        var ability = Hostile(SkillType.Throwing, area: true);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Other, Third, ability, SkillType.Throwing);

        _service.ForgetImpactState(Defender);

        SameTargetCounts().Keys.Should().Equal((Other, Third));
        HitSequences().Keys.Cast<(uint, uint)>().Should().Equal((Other, Third));
        _service.ForgetImpactState(Third);
        SameTargetCounts().Should().BeEmpty();
        HitSequences().Count.Should().Be(0);
    }
}
