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
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The shared bench of the ability activation parts of <c>Combat</c> (the stay-part and the cost, defense, offense and weapon
/// effects parts): stats come from tables; every other part and service is scripted and writes what it was asked into one log
/// (so a test sees the order), while the questions the riders ask (skill type matches, perk categories, the stamina an ability
/// paid) are answered from tables and recorded apart. The clock is held still. The four effects parts are real, built from the
/// same scripted leaves, and the stay-part calls them.
/// </summary>
public abstract class CombatAbilityActivationTestBase
{
    protected const uint Activator = 11;
    protected const uint Target = 22;
    protected const uint Master = 33;
    protected const uint Ally = 44;
    protected const uint Bystander = 55;

    protected readonly List<IDisposable> _scopes = new();
    protected readonly List<string> _log = new();
    protected readonly List<string> _asked = new();
    protected FacadeContainer _facades = null!;
    protected FakeNWScript _engine = null!;
    protected CombatAbilityActivationService _service = null!;
    protected CombatAbilityCostEffectsService _costEffects = null!;
    protected CombatAbilityDefenseEffectsService _defenseEffects = null!;
    protected CombatAbilityOffenseEffectsService _offenseEffects = null!;
    protected CombatAbilityWeaponActivationService _weaponActivation = null!;
    protected DateTime _now;
    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    protected Dictionary<StatType, StatType> _grantedDeflections = null!;
    protected Dictionary<StatType, DeflectionSource> _deflectionSources = null!;
    protected Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    protected Dictionary<uint, int> _stamina = null!;
    protected Dictionary<uint, int> _maxStamina = null!;
    protected Dictionary<AbilityType, int> _abilityScores = null!;
    protected Dictionary<uint, uint> _masters = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _party = new();
    protected List<uint> _inShape = null!;
    protected HashSet<StatType> _matchingPerkCategories = null!;
    protected HashSet<StatType> _matchingPerkTypes = null!;
    protected HashSet<(StatType SkillTypeStat, StatType? RangedOnlyStat)> _matchingTriggerSkills = null!;
    protected HashSet<int> _perkCategories = null!;
    protected int _shapeIndex;
    protected SkillType _abilitySkill;
    protected bool _triggerReady;
    protected bool _statusApplies;
    protected bool _forcePower;
    protected bool _areaImpact;
    protected int? _staminaCost;
    protected int _restoredFP;
    protected int? _restoredStamina;
    protected int _temporaryTotal;
    protected object? _currentForceAttack;

    // The nearby friend search asks the party roster (GameServices.PartyRoster) whether a creature in range is in the
    // activator's party.
    [OneTimeSetUp]
    public void BuildFacades()
    {
        var party = ScriptedProxy.Create<IPartyRoster>(out var partyCalls);
        partyCalls.On(nameof(IPartyRoster.IsInParty), args => _party.Contains((uint)args[1]!));
        _facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton(party)));
    }

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _log.Clear();
        _asked.Clear();
        _stats = new();
        _sources = new();
        _grantedDeflections = new();
        _deflectionSources = new();
        _hitPoints = new();
        _stamina = new();
        _maxStamina = new();
        _abilityScores = new();
        _masters = new();
        _invalid = new();
        _party = new();
        _inShape = new();
        _matchingPerkCategories = new();
        _matchingPerkTypes = new();
        _matchingTriggerSkills = new();
        _perkCategories = new();
        _shapeIndex = 0;
        _abilitySkill = SkillType.Invalid;
        _triggerReady = true;
        _statusApplies = true;
        _forcePower = false;
        _areaImpact = false;
        _staminaCost = null;
        _restoredFP = 0;
        _restoredStamina = null;
        _temporaryTotal = 0;
        _currentForceAttack = null;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetMaster", args => _masters.GetValueOrDefault((uint)args[0]!, NWScript.OBJECT_INVALID));
        _engine.SetReturnHandler("GetMaxHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Max : 0);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : 1);
        _engine.SetReturnHandler("GetAbilityScore", args => _abilityScores.GetValueOrDefault((AbilityType)args[1]!));
        _engine.SetReturnHandler("GetFirstObjectInShape", _ =>
        {
            _shapeIndex = 0;
            return _inShape.Count > 0 ? _inShape[_shapeIndex++] : NWScript.OBJECT_INVALID;
        });
        _engine.SetReturnHandler("GetNextObjectInShape", _ =>
            _shapeIndex < _inShape.Count ? _inShape[_shapeIndex++] : NWScript.OBJECT_INVALID);
        // The temporary HP pools keep their grants and generations in local variables.
        _ = new LocalVariables(_engine);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        // The temporary HP pools read the native current hit points through the object plugin.
        var objects = new FakeObjectPlugin();
        objects.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : 0);
        _scopes.Add(ObjectPluginRuntime.Override(objects));

        _service = BuildService();
    }

    protected CombatAbilityActivationService BuildService()
    {
        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.IsForcePower), args =>
            {
                _asked.Add($"ForcePower {args[0]} {((AbilityDetail)args[1]!).Name}");
                return _forcePower;
            })
            .On(nameof(ICombatQueriesService.GetAbilitySkillType), _ => _abilitySkill)
            .On(nameof(ICombatQueriesService.AbilityUsedTriggerSkillMatches), args =>
            {
                _asked.Add($"TriggerSkill {args[0]} {args[2]} {args[3]} {args[4] ?? "null"}");
                return _matchingTriggerSkills.Contains(((StatType)args[3]!, (StatType?)args[4]));
            })
            .On(nameof(ICombatQueriesService.AbilityMatchesAnyPerkTypeStat), args =>
            {
                var statTypes = (StatType[])args[2]!;
                _asked.Add($"PerkTypes {args[0]} {string.Join(",", statTypes)}");
                return _matchingPerkTypes.Contains(statTypes[0]);
            })
            .On(nameof(ICombatQueriesService.AbilityMatchesPerkCategoryStat), args =>
            {
                _asked.Add($"PerkCategory {args[0]} {args[2]}");
                return _matchingPerkCategories.Contains((StatType)args[2]!);
            });
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _log.Add($"Trigger {args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            })
            .On(nameof(ICombatResourcesService.TryGetAbilityStaminaCostState), args =>
            {
                _asked.Add($"StaminaCost {args[0]} {((AbilityDetail)args[1]!).Name}");
                args[2] = _staminaCost is { } cost ? new CombatAbilityStaminaCostState { Cost = cost } : null;
                return _staminaCost.HasValue;
            })
            .On(nameof(ICombatResourcesService.RestoreFPFromStat), args =>
            {
                _log.Add($"RestoreFP {args[0]} {args[1]} {args[2]}");
                return _restoredFP;
            })
            .On(nameof(ICombatResourcesService.RestoreStaminaFromStat), args =>
            {
                _log.Add($"RestoreStamina {args[0]} {args[1]} {args[2]}");
                return _restoredStamina ?? (int)args[2]!;
            })
            .On(nameof(ICombatResourcesService.ApplyAbilityRestoredFPEffects), args => Log($"RestoredFP {args[0]}"))
            .On(nameof(ICombatResourcesService.ApplyAbilityRestoredBothResourcesEffects), args => Log($"RestoredBoth {args[0]}"));
        var timing = ScriptedProxy.Create<ICombatAttackTimingService>(out var timingCalls);
        timingCalls
            .On(nameof(ICombatAttackTimingService.GrantNextSkillAutoAttackDamageBonus), args => Log($"NextAutoAttack {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextAbilityFPCostAdjustment), args => Log($"NextFPCost {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextSkillAbilityBonuses), args => Log($"NextSkill {string.Join(" ", args)}"));
        var activity = ScriptedProxy.Create<ICombatActivityService>(out var activityCalls);
        activityCalls.On(nameof(ICombatActivityService.TrackCombatAbilityUse), args => Log($"Track {args[0]} {((AbilityDetail)args[1]!).Name}"));
        var damageTaken = ScriptedProxy.Create<ICombatDamageTakenEffectsService>(out var damageTakenCalls);
        damageTakenCalls
            .On(nameof(ICombatDamageTakenEffectsService.ApplyAbilityUsedAttackDeflection), args => Log($"UsedDeflection {string.Join(" ", args)}"))
            .On(nameof(ICombatDamageTakenEffectsService.ApplyAbilityGrantedAttackDeflectionEffects), args => Log($"GrantedDeflection {args[0]} {args[1]}"));
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatSources), args => _sources.TryGetValue(((uint)args[0]!, (StatType)args[1]!), out var found)
                ? found
                : Array.Empty<StatAdjustmentSource>())
            .On(nameof(IStatService.GetGrantedDeflectionStatType), args => _grantedDeflections.GetValueOrDefault((StatType)args[0]!, StatType.Invalid))
            .On(nameof(IStatService.GetStatTypeDeflectionSource), args => _deflectionSources.GetValueOrDefault((StatType)args[0]!))
            .On(nameof(IStatService.GetMaxStamina), args => _maxStamina.GetValueOrDefault((uint)args[0]!))
            .On(nameof(IStatService.GetCurrentStamina), args => _stamina.GetValueOrDefault((uint)args[0]!));
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.GetStatusEffect), args =>
            {
                _asked.Add($"Status {args[0]} {((Type)args[1]!).Name}");
                return _currentForceAttack;
            });
        var statusApplication = ScriptedProxy.Create<IStatusEffectApplicationService>(out var statusApplicationCalls);
        statusApplicationCalls.On(nameof(IStatusEffectApplicationService.ApplyStatusEffect), args =>
            {
                _log.Add($"Apply {args[0]}>{args[1]} {Describe(args[2]!)} {args[3]}");
                return _statusApplies;
            });
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls
            .On(nameof(ITemporaryStatModifierService.Replace), args => Log($"Replace {string.Join(" ", args)}"))
            .On(nameof(ITemporaryStatModifierService.AddCapped), args =>
            {
                _log.Add($"AddCapped {string.Join(" ", args)}");
                return 0;
            })
            .On(nameof(ITemporaryStatModifierService.GetStatAdjustment), args =>
            {
                _asked.Add($"Temporary {args[0]} {args[1]} {args[2]}");
                return _temporaryTotal;
            });
        var ability = ScriptedProxy.Create<IAbilityImpactState>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityImpactState.IsAreaImpact), args =>
            {
                _asked.Add($"AreaImpact {((AbilityDetail)args[1]!).Name}");
                return _areaImpact;
            });
        var perk = ScriptedProxy.Create<IPerkService>(out var perkCalls);
        perkCalls.On(nameof(IPerkService.IsPerkInCategory), args =>
        {
            _asked.Add($"InCategory {args[0]} {args[1]}");
            return _perkCategories.Contains((int)args[1]!);
        });
        var recast = ScriptedProxy.Create<IRecastService>(out var recastCalls);
        recastCalls.On(nameof(IRecastService.ReduceRecastDelay), args => Log($"Recast {args[0]} {args[1]} {args[2]}"));
        var enmity = ScriptedProxy.Create<IEnmityService>(out var enmityCalls);
        enmityCalls.On(nameof(IEnmityService.ReduceEnmity), args => Log($"ReduceEnmity {args[0]} {args[1]} {args[2]}"));
        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out var recorderCalls);
        recorderCalls
            .On("get_" + nameof(ICombatRecorderService.IsRecording), _ => false)
            .On(nameof(ICombatRecorderService.BeginStatTriggerAttribution), args =>
            {
                _log.Add($"Attribute {args[0]} {args[1]}");
                return default(CombatRecordAttributionScope);
            });

        _costEffects = new CombatAbilityCostEffectsService(queries, resources, timing, stat, statusApplication, temporary, recast, recorder);
        _defenseEffects = new CombatAbilityDefenseEffectsService(queries, resources, damageTaken, stat, statusApplication, temporary, perk);
        _offenseEffects = new CombatAbilityOffenseEffectsService(queries, resources, damageTaken, stat, statusEffect, statusApplication, temporary);
        _weaponActivation = new CombatAbilityWeaponActivationService(queries, resources, timing, stat, statusApplication, temporary, enmity, recorder,
            _offenseEffects);
        return new CombatAbilityActivationService(queries, activity, ability, _costEffects, _defenseEffects, _offenseEffects, _weaponActivation);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    protected object? Log(string line)
    {
        _log.Add(line);
        return null;
    }

    protected void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    protected void Sources(uint creature, StatType payload, params Dictionary<StatType, int>[] sources) =>
        _sources[(creature, payload)] = sources.Select((stats, i) => new StatAdjustmentSource($"source{i}", stats)).ToArray();

    protected IEnumerable<string> Lines(string prefix) => _log.Where(line => line.StartsWith(prefix, StringComparison.Ordinal));

    // A status effect by its type and the values it was built with (its own instance fields, in declaration order).
    protected static string Describe(object effect)
    {
        if (effect is Type type)
            return type.Name;

        var values = effect.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(field => field.GetValue(effect));
        return $"{effect.GetType().Name}({string.Join(",", values)})";
    }

    protected static object? Private(object target, string name, params object[] arguments) =>
        target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
            arguments.Select(argument => argument.GetType()).ToArray(), null)!.Invoke(target, arguments);

    protected System.Collections.IDictionary Sequences() =>
        (System.Collections.IDictionary)typeof(CombatAbilityOffenseEffectsService)
            .GetField("_hostileAbilitySequenceStates", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_offenseEffects)!;

    protected static AbilityDetail Hostile(string name = "Strike", int fp = 0)
    {
        var ability = new AbilityDetail { Name = name, IsHostileAbility = true, IsSingleTargetAbility = true };
        if (fp > 0)
            ability.Requirements.Add(new AbilityRequirementFP(fp));
        return ability;
    }

    protected static AbilityImpactSummary Summary(SkillType skillType = SkillType.Invalid, bool singleTarget = false) =>
        new() { SkillType = skillType, IsSingleTargetAbility = singleTarget };
}
