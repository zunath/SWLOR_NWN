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
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability activation part of <c>Combat</c>: the riders that run when a creature activates an ability, in their fixed
/// order, the queued weapon ability use, the riders of an HP-cost ability and the hostile ability sequence. Stats come from
/// tables; every other part and service is scripted and writes what it was asked into one log (so a test sees the order),
/// while the questions the riders ask (skill type matches, perk categories, the stamina an ability paid) are answered from
/// tables and recorded apart. The clock is held still.
/// </summary>
[TestFixture]
public class CombatAbilityActivationServiceTests
{
    private const uint Activator = 11;
    private const uint Target = 22;
    private const uint Master = 33;
    private const uint Ally = 44;
    private const uint Bystander = 55;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _log = new();
    private readonly List<string> _asked = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private CombatAbilityActivationService _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    private Dictionary<StatType, StatType> _grantedDeflections = null!;
    private Dictionary<StatType, DeflectionSource> _deflectionSources = null!;
    private Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    private Dictionary<uint, int> _stamina = null!;
    private Dictionary<uint, int> _maxStamina = null!;
    private Dictionary<AbilityType, int> _abilityScores = null!;
    private Dictionary<uint, uint> _masters = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _party = new();
    private List<uint> _inShape = null!;
    private HashSet<StatType> _matchingPerkCategories = null!;
    private HashSet<StatType> _matchingPerkTypes = null!;
    private HashSet<(StatType SkillTypeStat, StatType? RangedOnlyStat)> _matchingTriggerSkills = null!;
    private HashSet<int> _perkCategories = null!;
    private int _shapeIndex;
    private SkillType _abilitySkill;
    private bool _triggerReady;
    private bool _statusApplies;
    private bool _forcePower;
    private bool _areaImpact;
    private int? _staminaCost;
    private int _restoredFP;
    private int? _restoredStamina;
    private int _temporaryTotal;
    private object? _currentForceAttack;

    // The nearby friend search asks the party facade whether a creature in range is in the activator's party.
    [OneTimeSetUp]
    public void BuildFacades()
    {
        var party = ScriptedProxy.Create<IPartyService>(out var partyCalls);
        partyCalls.On(nameof(IPartyService.IsInParty), args => _party.Contains((uint)args[1]!));
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

    private CombatAbilityActivationService BuildService()
    {
        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
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
        var damageTaken = ScriptedProxy.Create<ICombatDamageTakenService>(out var damageTakenCalls);
        damageTakenCalls
            .On(nameof(ICombatDamageTakenService.ApplyAbilityUsedAttackDeflection), args => Log($"UsedDeflection {string.Join(" ", args)}"))
            .On(nameof(ICombatDamageTakenService.ApplyAbilityGrantedAttackDeflectionEffects), args => Log($"GrantedDeflection {args[0]} {args[1]}"));
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
            })
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
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
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityService.IsAreaImpact), args =>
            {
                _asked.Add($"AreaImpact {((AbilityDetail)args[1]!).Name}");
                return _areaImpact;
            })
            .On(nameof(IAbilityService.IsForcePower), args =>
            {
                _asked.Add($"ForcePower {args[0]} {((AbilityDetail)args[1]!).Name}");
                return _forcePower;
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

        return new CombatAbilityActivationService(queries, resources, timing, activity, damageTaken, stat, statusEffect, temporary,
            ability, perk, recast, enmity, recorder);
    }

    [TearDown]
    public void TearDown()
    {
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

    private void Sources(uint creature, StatType payload, params Dictionary<StatType, int>[] sources) =>
        _sources[(creature, payload)] = sources.Select((stats, i) => new StatAdjustmentSource($"source{i}", stats)).ToArray();

    private IEnumerable<string> Lines(string prefix) => _log.Where(line => line.StartsWith(prefix, StringComparison.Ordinal));

    // A status effect by its type and the values it was built with (its own instance fields, in declaration order).
    private static string Describe(object effect)
    {
        if (effect is Type type)
            return type.Name;

        var values = effect.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(field => field.GetValue(effect));
        return $"{effect.GetType().Name}({string.Join(",", values)})";
    }

    private object? Private(string name, params object[] arguments) =>
        typeof(CombatAbilityActivationService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null,
            arguments.Select(argument => argument.GetType()).ToArray(), null)!.Invoke(_service, arguments);

    private System.Collections.IDictionary Sequences() =>
        (System.Collections.IDictionary)typeof(CombatAbilityActivationService)
            .GetField("_hostileAbilitySequenceStates", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;

    private static AbilityDetail Hostile(string name = "Strike", int fp = 0)
    {
        var ability = new AbilityDetail { Name = name, IsHostileAbility = true, IsSingleTargetAbility = true };
        if (fp > 0)
            ability.Requirements.Add(new AbilityRequirementFP(fp));
        return ability;
    }

    private static AbilityImpactSummary Summary(SkillType skillType = SkillType.Invalid, bool singleTarget = false) =>
        new() { SkillType = skillType, IsSingleTargetAbility = singleTarget };

    // ------------------------------------------------------------------ the activation riders, in order

    [Test]
    public void AnInvalidActivatorOrAMissingAbilityOrSummaryRunsNothing()
    {
        Stat(Activator, StatType.HostileAbilityFPRestore, 5);
        _restoredFP = 5;
        _invalid.Add(Activator);

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, Hostile(), Summary());
        _invalid.Clear();
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, null!, Summary());
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, Hostile(), null!);

        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        Sequences().Count.Should().Be(0);
    }

    [Test]
    public void AnActivationWithNothingConfiguredOnlyTracksTheAbilityAndRecordsTheSequence()
    {
        var ability = Hostile();

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.DoubleShot1, ability, Summary());

        _log.Should().Equal($"Track {Activator} Strike");
        Sequences().Contains(Activator).Should().BeTrue("the sequence remembers the last hostile ability even with no bleed configured");
    }

    [Test]
    public void AWeaponActivationIsNotTrackedHereButAQueuedWeaponUseIs()
    {
        var weapon = Hostile("Weapon Strike");
        weapon.ActivationType = AbilityActivationType.Weapon;

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.DoubleShot1, weapon, Summary());
        _log.Should().BeEmpty("the queued weapon attack tracks the use when it lands");

        _service.TrackQueuedWeaponAbilityUse(Activator, weapon);
        _log.Should().Equal($"Track {Activator} Weapon Strike");
    }

    // Every rider configured to fire: the effects come out in the order ApplyAbilityActivatedEffects calls the riders.
    [Test]
    public void EveryRiderRunsInItsFixedOrder()
    {
        const int masterCategory = 7;
        var ability = Hostile("Saber Strike", fp: 10);
        ability.RecastGroup = RecastGroup.Earthshatter;
        ability.EffectiveLevelPerkType = PerkType.DoubleShot;
        _abilitySkill = SkillType.Lightsaber;
        _masters[Activator] = Master;
        _forcePower = true;
        _areaImpact = true;
        _staminaCost = 30;
        _restoredFP = 4;
        _inShape.AddRange(new[] { Activator, Ally });
        _party.Add(Ally);
        _perkCategories.Add(masterCategory);
        _matchingTriggerSkills.Add((StatType.AbilityUsedEvasionPercentAdjustmentSkillType, null));
        _matchingTriggerSkills.Add((StatType.AbilityUsedRangedDeflectionSkillType, StatType.AbilityUsedRangedDeflectionRangedOnly));
        _matchingTriggerSkills.Add((StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType, StatType.AbilityUsedMovementSpeedRangedOnly));
        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId);
        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategorySelfDefenseCategoryId);
        _matchingPerkCategories.Add(StatType.WardAbilityDefenseCategoryId);
        _grantedDeflections[StatType.AreaAbilityUsedAttackDeflection] = StatType.MeleeDeflection;
        _grantedDeflections[StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection] = StatType.MeleeDeflection;
        _grantedDeflections[StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection] = StatType.MeleeDeflection;
        _deflectionSources[StatType.AreaAbilityUsedAttackDeflection] = DeflectionSource.Melee;
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        _temporaryTotal = 6;
        var skill = (int)SkillType.Lightsaber;

        // 1. recast reduction
        Stat(Activator, StatType.AbilityUsedRecastReductionTriggerGroup, (int)RecastGroup.Earthshatter);
        Stat(Activator, StatType.AbilityUsedRecastReductionTargetGroup, (int)RecastGroup.Flash);
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 4);
        // 2. next skill auto-attack damage and 3. next skill FP cost
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus, 3);
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds, 9);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustment, -2);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds, 8);
        // 4. master hit chance
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 5);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 6);
        // 5. Force FP cost riders
        Stat(Activator, StatType.ForcePrecognition, 1);
        Stat(Activator, StatType.ForceConvergence, 1);
        // 6. skill evasion
        Stat(Activator, StatType.AbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.AbilityUsedEvasionDurationSeconds, 5);
        // 7. hostile evasion and 8. costly evasion
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 11);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionMinimumStaminaCost, 20);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionPercentAdjustment, 12);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionDurationSeconds, 5);
        // 9. ranged deflection
        Stat(Activator, StatType.AbilityUsedRangedDeflection, 15);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 6);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.DeflectingAuraStatusEffect);
        // 10. movement speed
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 20);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        // 11. skill attack deflection, 12. perk category attack deflection, 13. single target attack deflection
        Stat(Activator, StatType.AbilityUsedAttackDeflectionSkillType, skill);
        Stat(Activator, StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType, masterCategory);
        Stat(Activator, StatType.SingleTargetAbilityAttackDeflectionSkillType, skill);
        // 14. area evasion and 15. area rewards
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 13);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 5);
        Sources(Activator, StatType.AreaAbilityUsedAttackDeflection, new Dictionary<StatType, int>
        {
            [StatType.AreaAbilityUsedAttackDeflection] = 8,
            [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 6,
        });
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 2);
        // 16. Force Attack and 17. FP spend Force Attack
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 5);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 20);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackPercent, 4);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent, 12);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost, 5);
        // 18. nearby ally defense, 19. nearby ally attack deflection, 20. self defense
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment, 6);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 9);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 7);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment, 3);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds, 20);
        // 21. attack adjustment
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustment, 2);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, 8);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, 10);
        // 22. the Lightsaber riders: Centering, Guardian's Influence and the Ward
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 7);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection, 5);
        Stat(Activator, StatType.WardAbilityDefensePercentAdjustment, 4);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 6);
        // 23. the sequence and 24. the resource restores
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);
        Stat(Activator, StatType.HostileAbilityFPRestore, 4);

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.DoubleShot1, ability, Summary(singleTarget: true));

        _log.Should().Equal(
            $"Recast {Activator} {RecastGroup.Flash} 4",
            $"NextAutoAttack {Activator} {SkillType.Lightsaber} 3 9",
            $"NextFPCost {Activator} {SkillType.Lightsaber} -2 8",
            $"Replace {Master} {StatType.AbilityHitChancePercentAdjustment} 5 6 {StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment}",
            $"Trigger {Activator} {StatType.ForcePrecognition} 12",
            $"Apply {Activator}>{Activator} DangerSenseStatusEffect 30",
            $"Trigger {Activator} {StatType.ForceConvergence} 45",
            $"Apply {Activator}>{Activator} ForceConvergenceStatusEffect 30",
            $"Apply {Activator}>{Activator} EvasiveFootworkStatusEffect(10) 5",
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 11 5 {StatType.HostileAbilityUsedEvasionPercentAdjustment}",
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 12 5 {StatType.CostlyAbilityUsedEvasionPercentAdjustment}",
            $"Apply {Activator}>{Activator} RangedDeflectionStatusEffect(15,{EffectIconType.DeflectingAuraStatusEffect}) 6",
            $"GrantedDeflection {Activator} {DeflectionSource.Ranged}",
            $"Replace {Activator} {StatType.MovementSpeedPercentAdjustment} 20 4 {StatType.AbilityUsedMovementSpeedPercentAdjustment}",
            $"UsedDeflection {Activator} {StatType.AbilityUsedAttackDeflection} {StatType.AbilityUsedAttackDeflectionDurationSeconds} {StatType.AbilityUsedAttackDeflectionFPRestore}",
            $"UsedDeflection {Activator} {StatType.AbilityUsedPerkCategoryAttackDeflection} {StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds} {StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore}",
            $"UsedDeflection {Activator} {StatType.SingleTargetAbilityAttackDeflection} {StatType.SingleTargetAbilityAttackDeflectionDurationSeconds} {StatType.Invalid}",
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 13 5 {StatType.AreaAbilityUsedEvasionPercentAdjustment}",
            $"Replace {Activator} {StatType.MeleeDeflection} 8 6 source0:{(int)StatType.AreaAbilityUsedAttackDeflection}",
            $"GrantedDeflection {Activator} {DeflectionSource.Melee}",
            $"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 2",
            $"RestoredFP {Activator}",
            $"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(5) 10",
            $"AddCapped {Activator} {StatType.ForceAttackPercentAdjustment} 4 10 12 {StatType.HostileAbilityFPSpendForceAttackPercent} 1 True",
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 6 5 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}",
            $"Apply {Activator}>{Activator} SentinelGuardStatusEffect(9,0,{StatType.MeleeDeflection}) 7",
            $"Apply {Activator}>{Ally} SentinelGuardStatusEffect(9,0,{StatType.MeleeDeflection}) 7",
            $"Trigger {Activator} {StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment} 20",
            $"Apply {Activator}>{Activator} GuardingStepStatusEffect(3,0,0) 5",
            $"AddCapped {Activator} {StatType.AttackPercentAdjustment} 2 8 10 {StatType.HostileAbilityUsedAttackPercentAdjustment} 1 True",
            $"Apply {Activator}>{Activator} ButchersTempoStatusEffect(6) 8",
            $"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 0",
            $"Apply {Activator}>{Activator} CenteringStatusEffect(7) 6",
            $"Replace {Ally} {StatType.MeleeDeflection} 5 12 {StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection}",
            $"Replace {Activator} {StatType.PhysicalDefensePercentAdjustment} 4 6 {StatType.WardAbilityDefensePercentAdjustment}",
            $"Replace {Activator} {StatType.WardTargetPhysicalDefensePercentAdjustment} 4 6 {StatType.WardAbilityDefensePercentAdjustment}",
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 4",
            $"RestoredFP {Activator}",
            $"Track {Activator} Saber Strike");
        Sequences().Contains(Activator).Should().BeTrue();
    }

    [Test]
    public void TheSkillTypeTheRidersUseIsTheSummarysOrElseTheAbilitysAndTheSingleTargetFlagFallsBackToTheAbility()
    {
        var ability = Hostile();
        _abilitySkill = SkillType.Vibroknife;
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.SingleTargetAbilityAttackDeflectionSkillType, (int)SkillType.Vibroknife);

        // The summary names the Rifle, so the hostile evasion keyed to the Rifle fires; a summary with a skill type keeps the
        // single target flag it has, so the single target deflection does not fire.
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, ability, Summary(SkillType.Rifle));
        Lines("Replace").Should().ContainSingle();
        Lines("UsedDeflection").Should().BeEmpty();

        // With no skill type on the summary the ability's own skill type (the Vibroknife) is used, and a hostile single target
        // ability counts as single target.
        _log.Clear();
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, ability, Summary());
        Lines("Replace").Should().BeEmpty();
        Lines("UsedDeflection").Should().Equal(
            $"UsedDeflection {Activator} {StatType.SingleTargetAbilityAttackDeflection} {StatType.SingleTargetAbilityAttackDeflectionDurationSeconds} {StatType.Invalid}");

        // A non-hostile ability is not single target by fallback.
        _log.Clear();
        ability.IsHostileAbility = false;
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, ability, Summary());
        Lines("UsedDeflection").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ recast and next-ability grants

    [Test]
    public void TheRecastReductionNeedsATriggerGroupMatchAValidTargetAndSeconds()
    {
        var ability = Hostile();
        ability.RecastGroup = RecastGroup.Flash;
        Stat(Activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup, (int)RecastGroup.Flash);
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 3);

        Private("ApplyAbilityUsedRecastReduction", Activator, ability);
        _log.Should().BeEmpty("no target group");

        Stat(Activator, StatType.AbilityUsedRecastReductionTargetGroup, (int)RecastGroup.Earthshatter);
        Private("ApplyAbilityUsedRecastReduction", Activator, ability);
        _log.Should().Equal(new[] { $"Recast {Activator} {RecastGroup.Earthshatter} 3" }, "the secondary trigger group matches too");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 0);
        Private("ApplyAbilityUsedRecastReduction", Activator, ability);
        ability.RecastGroup = RecastGroup.Invalid;
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 3);
        Private("ApplyAbilityUsedRecastReduction", Activator, ability);
        ability.RecastGroup = RecastGroup.Earthshatter;
        Private("ApplyAbilityUsedRecastReduction", Activator, ability);
        _log.Should().BeEmpty("no seconds, an ability with no recast group, and a group that triggers nothing");
    }

    [Test]
    public void TheNextSkillGrantsNeedTheAbilitysSkillToMatchTheirTrigger()
    {
        var ability = Hostile();
        _abilitySkill = SkillType.Pistol;
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType, (int)SkillType.Rifle);

        Private("ApplyAbilityUsedNextSkillAutoAttackDamageBonus", Activator, ability);
        Private("ApplyAbilityUsedNextSkillFPCostAdjustment", Activator, ability);
        _log.Should().BeEmpty();

        // An unset target skill and amounts pass through as they are: the timing part decides what to grant.
        _abilitySkill = SkillType.Rifle;
        Private("ApplyAbilityUsedNextSkillAutoAttackDamageBonus", Activator, ability);
        Private("ApplyAbilityUsedNextSkillFPCostAdjustment", Activator, ability);
        _log.Should().Equal(
            $"NextAutoAttack {Activator} {SkillType.Invalid} 0 0",
            $"NextFPCost {Activator} {SkillType.Invalid} 0 0");
    }

    [Test]
    public void TheMasterHitChanceNeedsAMasterAnAdjustmentAndADuration()
    {
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, -5);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 6);
        Private("ApplyAbilityUsedMasterAbilityHitChance", Activator);
        _log.Should().BeEmpty("no master");

        _masters[Activator] = Master;
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 0);
        Private("ApplyAbilityUsedMasterAbilityHitChance", Activator);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 6);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 0);
        Private("ApplyAbilityUsedMasterAbilityHitChance", Activator);
        _log.Should().BeEmpty("no duration, then no adjustment");

        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, -5);
        Private("ApplyAbilityUsedMasterAbilityHitChance", Activator);
        _log.Should().Equal(
            $"Replace {Master} {StatType.AbilityHitChancePercentAdjustment} -5 6 {StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment}");
    }

    [Test]
    public void TheForceFPCostRidersNeedAForcePowerThatCostsFPAndAReadyTrigger()
    {
        Stat(Activator, StatType.ForcePrecognition, 1);
        Stat(Activator, StatType.ForceConvergence, 1);
        Private("ApplyForceFPCostActivatedEffects", Activator, Hostile(fp: 5));
        _log.Should().BeEmpty("not a Force power");

        _forcePower = true;
        Private("ApplyForceFPCostActivatedEffects", Activator, Hostile());
        _log.Should().BeEmpty("no FP cost");

        _triggerReady = false;
        Private("ApplyForceFPCostActivatedEffects", Activator, Hostile(fp: 5));
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.ForcePrecognition} 12",
            $"Trigger {Activator} {StatType.ForceConvergence} 45");

        _log.Clear();
        _triggerReady = true;
        Stat(Activator, StatType.ForcePrecognition, 0);
        Private("ApplyForceFPCostActivatedEffects", Activator, Hostile(fp: 5));
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.ForceConvergence} 45",
            $"Apply {Activator}>{Activator} ForceConvergenceStatusEffect 30");
    }

    // ------------------------------------------------------------------ evasion and movement

    [Test]
    public void TheSkillEvasionRunsBothChannelsAndAppliesFootworkWhereTheTriggerSkillMatches()
    {
        _abilitySkill = SkillType.Rifle;
        _matchingTriggerSkills.Add((StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType, StatType.SecondaryAbilityUsedEvasionRangedOnly));
        Stat(Activator, StatType.AbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.AbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionPercentAdjustment, 12);
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionDurationSeconds, 4);

        Private("ApplyAbilityUsedSkillEvasion", Activator, Hostile());

        _asked.Should().Equal(
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.AbilityUsedEvasionPercentAdjustmentSkillType} null",
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType} {StatType.SecondaryAbilityUsedEvasionRangedOnly}");
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveFootworkStatusEffect(12) 4");

        _log.Clear();
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionDurationSeconds, 0);
        Private("ApplyAbilityUsedSkillEvasion", Activator, Hostile());
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionDurationSeconds, 4);
        Stat(Activator, StatType.SecondaryAbilityUsedEvasionPercentAdjustment, 0);
        Private("ApplyAbilityUsedSkillEvasion", Activator, Hostile());
        _log.Should().BeEmpty("no duration, then no evasion");
    }

    [Test]
    public void TheHostileAndCostlyEvasionNeedAHostileAbilityAMatchingSkillAndTheirStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Private("ApplyHostileAbilityUsedEvasion", Activator, ability, SkillType.Pistol);
        _log.Should().BeEmpty("the skill does not match");

        Private("ApplyHostileAbilityUsedEvasion", Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle);
        _log.Should().BeEmpty("not hostile");

        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 0);
        Private("ApplyHostileAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 0);
        Private("ApplyHostileAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        _log.Should().BeEmpty("no duration, then no evasion");

        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, -4);
        Private("ApplyHostileAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        _log.Should().Equal($"Replace {Activator} {StatType.EvasionPercentAdjustment} -4 5 {StatType.HostileAbilityUsedEvasionPercentAdjustment}");

        // The costly evasion needs the stamina this activation paid, at least the minimum, and a matching skill.
        _log.Clear();
        Stat(Activator, StatType.CostlyAbilityUsedEvasionPercentAdjustment, 8);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionDurationSeconds, 4);
        Private("ApplyCostlyAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        _staminaCost = 10;
        Private("ApplyCostlyAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionMinimumStaminaCost, 15);
        Private("ApplyCostlyAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Pistol);
        _staminaCost = 15;
        Private("ApplyCostlyAbilityUsedEvasion", Activator, ability, SkillType.Rifle);
        Private("ApplyCostlyAbilityUsedEvasion", Activator, new AbilityDetail { Name = "Heal" }, SkillType.Pistol);
        _log.Should().BeEmpty("no paid stamina, no minimum, below the minimum, another skill, then not hostile");
        _asked.Count(line => line.StartsWith("StaminaCost", StringComparison.Ordinal)).Should().Be(4, "a non-hostile ability never asks");

        Private("ApplyCostlyAbilityUsedEvasion", Activator, ability, SkillType.Pistol);
        _log.Should().Equal($"Replace {Activator} {StatType.EvasionPercentAdjustment} 8 4 {StatType.CostlyAbilityUsedEvasionPercentAdjustment}");
    }

    [Test]
    public void TheAreaEvasionNeedsAHostileAreaAbilityAMatchingSkillAndItsStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 9);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 3);

        Private("ApplyAreaAbilityUsedEvasion", Activator, ability, SkillType.Rifle, false);
        Private("ApplyAreaAbilityUsedEvasion", Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle, true);
        Private("ApplyAreaAbilityUsedEvasion", Activator, ability, SkillType.Pistol, true);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 0);
        Private("ApplyAreaAbilityUsedEvasion", Activator, ability, SkillType.Rifle, true);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 3);
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 0);
        Private("ApplyAreaAbilityUsedEvasion", Activator, ability, SkillType.Rifle, true);
        _log.Should().BeEmpty("not an area ability, not hostile, another skill, no duration, then no evasion");

        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 9);
        Private("ApplyAreaAbilityUsedEvasion", Activator, ability, SkillType.Rifle, true);
        _log.Should().Equal($"Replace {Activator} {StatType.EvasionPercentAdjustment} 9 3 {StatType.AreaAbilityUsedEvasionPercentAdjustment}");
    }

    [Test]
    public void TheMovementSpeedNeedsAHostileAbilityAMatchingTriggerAndItsStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 15);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        Private("ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        Private("ApplyAbilityUsedMovementSpeed", Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle);
        _log.Should().BeEmpty("no matching trigger, then not hostile");
        _asked.Should().ContainSingle().Which.Should().Be(
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType} {StatType.AbilityUsedMovementSpeedRangedOnly}");

        _matchingTriggerSkills.Add((StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType, StatType.AbilityUsedMovementSpeedRangedOnly));
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 0);
        Private("ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 0);
        Private("ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        _log.Should().BeEmpty("no duration, then no speed");

        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, -10);
        Private("ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        _log.Should().Equal($"Replace {Activator} {StatType.MovementSpeedPercentAdjustment} -10 4 {StatType.AbilityUsedMovementSpeedPercentAdjustment}");
    }

    // ------------------------------------------------------------------ deflection

    [Test]
    public void TheRangedDeflectionNeedsAHostileAbilityAMatchingTriggerItsStatsAndAnIconAndRunsItsRidersOnlyWhenApplied()
    {
        var ability = Hostile();
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        Stat(Activator, StatType.AbilityUsedRangedDeflection, 10);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.DeflectingAuraStatusEffect);

        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, ability);
        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("no matching trigger, then not hostile");

        _matchingTriggerSkills.Add((StatType.AbilityUsedRangedDeflectionSkillType, StatType.AbilityUsedRangedDeflectionRangedOnly));
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, 0);
        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, ability);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.DeflectingAuraStatusEffect);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 0);
        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, ability);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedRangedDeflection, 0);
        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, ability);
        _log.Should().BeEmpty("no icon, no duration, then no deflection");

        Stat(Activator, StatType.AbilityUsedRangedDeflection, 10);
        _statusApplies = false;
        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, ability);
        _log.Should().Equal(
            new[] { $"Apply {Activator}>{Activator} RangedDeflectionStatusEffect(10,{EffectIconType.DeflectingAuraStatusEffect}) 5" },
            "a status that was not applied runs no deflection riders");

        _log.Clear();
        _statusApplies = true;
        Private("ApplyAbilityUsedSkillRangedDeflection", Activator, ability);
        _log.Should().Equal(
            $"Apply {Activator}>{Activator} RangedDeflectionStatusEffect(10,{EffectIconType.DeflectingAuraStatusEffect}) 5",
            $"GrantedDeflection {Activator} {DeflectionSource.Ranged}");
    }

    [Test]
    public void TheAttackDeflectionRidersNeedTheirSkillOrCategoryAndPassTheirOwnStats()
    {
        var ability = Hostile();
        ability.EffectiveLevelPerkType = PerkType.DoubleShot;
        _abilitySkill = SkillType.Pistol;
        Stat(Activator, StatType.AbilityUsedAttackDeflectionSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.SingleTargetAbilityAttackDeflectionSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType, 3);

        Private("ApplyAbilityUsedSkillAttackDeflection", Activator, ability);
        Private("ApplySingleTargetAbilityUsedAttackDeflection", Activator, ability, true);
        Private("ApplyAbilityUsedPerkCategoryAttackDeflection", Activator, ability);
        _log.Should().BeEmpty("another skill and a perk outside the category");
        _asked.Should().Contain($"InCategory {PerkType.DoubleShot} 3");

        _abilitySkill = SkillType.Rifle;
        _perkCategories.Add(3);
        Private("ApplyAbilityUsedSkillAttackDeflection", Activator, new AbilityDetail { Name = "Heal" });
        Private("ApplySingleTargetAbilityUsedAttackDeflection", Activator, ability, false);
        _log.Should().BeEmpty("not hostile, then not single target");

        Private("ApplyAbilityUsedSkillAttackDeflection", Activator, ability);
        Private("ApplySingleTargetAbilityUsedAttackDeflection", Activator, ability, true);
        Private("ApplyAbilityUsedPerkCategoryAttackDeflection", Activator, ability);
        _log.Should().Equal(
            $"UsedDeflection {Activator} {StatType.AbilityUsedAttackDeflection} {StatType.AbilityUsedAttackDeflectionDurationSeconds} {StatType.AbilityUsedAttackDeflectionFPRestore}",
            $"UsedDeflection {Activator} {StatType.SingleTargetAbilityAttackDeflection} {StatType.SingleTargetAbilityAttackDeflectionDurationSeconds} {StatType.Invalid}",
            $"UsedDeflection {Activator} {StatType.AbilityUsedPerkCategoryAttackDeflection} {StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds} {StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore}");
    }

    [Test]
    public void APerkCategoryDeflectionOfAnAbilityWithNoDetailAsksForNoPerk()
    {
        _perkCategories.Add(0);

        _service.GetType().GetMethod("ApplyAbilityUsedPerkCategoryAttackDeflection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_service, new object?[] { Activator, null });

        _asked.Should().Equal($"InCategory {PerkType.Invalid} 0");
        _log.Should().ContainSingle().Which.Should().StartWith($"UsedDeflection {Activator} {StatType.AbilityUsedPerkCategoryAttackDeflection}");
    }

    // ------------------------------------------------------------------ area rewards

    [Test]
    public void TheAreaRewardsGrantEachConfiguredSourcesDeflectionThenTheRidersOnceThenTheFPRestore()
    {
        var ability = Hostile();
        _grantedDeflections[StatType.AreaAbilityUsedAttackDeflection] = StatType.ShieldDeflection;
        _deflectionSources[StatType.AreaAbilityUsedAttackDeflection] = DeflectionSource.Shield;
        Sources(Activator, StatType.AreaAbilityUsedAttackDeflection,
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 5, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 4 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 0, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 4 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 3, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 0 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 7, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 6 });
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 3);

        Private("ApplyAreaAbilityUsedRewards", Activator, ability, false);
        Private("ApplyAreaAbilityUsedRewards", Activator, new AbilityDetail { Name = "Heal" }, true);
        _log.Should().BeEmpty("not an area ability, then not hostile");

        Private("ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().Equal(
            new[]
            {
                $"Replace {Activator} {StatType.ShieldDeflection} 5 4 source0:{(int)StatType.AreaAbilityUsedAttackDeflection}",
                $"Replace {Activator} {StatType.ShieldDeflection} 7 6 source3:{(int)StatType.AreaAbilityUsedAttackDeflection}",
                $"GrantedDeflection {Activator} {DeflectionSource.Shield}",
                $"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 3",
            },
            "nothing was restored, so the restored FP riders do not run");

        _log.Clear();
        _restoredFP = 3;
        _sources.Clear();
        Private("ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().Equal($"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 3", $"RestoredFP {Activator}");

        _log.Clear();
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 0);
        Private("ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().BeEmpty("no restore and no deflection source");
    }

    // ------------------------------------------------------------------ Force Attack and the attack adjustment

    [Test]
    public void TheHostileForceAttackStacksOnTheCurrentStatusUpToItsMaximum()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 6);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 15);
        _currentForceAttack = new HostileAbilityForceAttackStatusEffect(12);

        Private("ApplyHostileAbilityForceAttack", Activator, ability);

        _asked.Should().Equal($"Status {Activator} {nameof(HostileAbilityForceAttackStatusEffect)}");
        _log.Should().Equal($"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(15) 10");

        _log.Clear();
        _currentForceAttack = null;
        Private("ApplyHostileAbilityForceAttack", Activator, ability);
        _log.Should().Equal($"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(6) 10");
    }

    [Test]
    public void TheHostileForceAttackNeedsAHostileAbilityAndAllThreeStats()
    {
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 6);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 15);
        Private("ApplyHostileAbilityForceAttack", Activator, new AbilityDetail { Name = "Heal" });
        foreach (var stat in new[] { StatType.HostileAbilityForceAttackPercentMax, StatType.HostileAbilityForceAttackDurationSeconds, StatType.HostileAbilityForceAttackPercentPerStack })
        {
            Stat(Activator, stat, 0);
            Private("ApplyHostileAbilityForceAttack", Activator, Hostile());
        }

        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
    }

    [Test]
    public void TheFPSpendForceAttackNeedsAHostileAbilityItsStatsAndAnFPCostAtTheMinimum()
    {
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackPercent, 3);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 9);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent, 9);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost, 8);

        var twoCosts = Hostile(fp: 4);
        twoCosts.Requirements.Add(new AbilityRequirementFP(4));
        Private("ApplyHostileAbilityFPSpendForceAttack", Activator, Hostile(fp: 7));
        Private("ApplyHostileAbilityFPSpendForceAttack", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("below the minimum, then not hostile");

        Private("ApplyHostileAbilityFPSpendForceAttack", Activator, twoCosts);
        _log.Should().Equal(
            new[] { $"AddCapped {Activator} {StatType.ForceAttackPercentAdjustment} 3 9 9 {StatType.HostileAbilityFPSpendForceAttackPercent} 1 True" },
            "the FP requirements add up");

        _log.Clear();
        foreach (var stat in new[] { StatType.HostileAbilityFPSpendForceAttackMinFPCost, StatType.HostileAbilityFPSpendForceAttackMaxPercent, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, StatType.HostileAbilityFPSpendForceAttackPercent })
        {
            Stat(Activator, stat, 0);
            Private("ApplyHostileAbilityFPSpendForceAttack", Activator, twoCosts);
        }

        _log.Should().BeEmpty();
    }

    [Test]
    public void TheAttackAdjustmentStacksAndShowsTheTempoOnlyWhileItsTotalIsPositive()
    {
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustment, 2);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, 8);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, 10);

        Private("ApplyHostileAbilityUsedAttackAdjustment", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty();

        Private("ApplyHostileAbilityUsedAttackAdjustment", Activator, Hostile());
        _asked.Should().Equal($"Temporary {Activator} {StatType.AttackPercentAdjustment} {StatType.HostileAbilityUsedAttackPercentAdjustment}");
        _log.Should().Equal(
            new[] { $"AddCapped {Activator} {StatType.AttackPercentAdjustment} 2 8 10 {StatType.HostileAbilityUsedAttackPercentAdjustment} 1 True" },
            "no stacks are held, so no tempo status");

        _log.Clear();
        foreach (var stat in new[] { StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, StatType.HostileAbilityUsedAttackPercentAdjustment })
        {
            Stat(Activator, stat, 0);
            Private("ApplyHostileAbilityUsedAttackAdjustment", Activator, Hostile());
        }

        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ nearby allies and self defense

    [Test]
    public void TheNearbyAllyDefenseGoesToPartyMembersInRangeButNotTheActivator()
    {
        _inShape.AddRange(new[] { Activator, Ally, Bystander });
        _party.Add(Ally);
        Stat(Activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment, 5);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 6);

        Private("ApplyAbilityUsedNearbyAllyDefense", Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.ForceDefensePercentAdjustment} 5 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment, 4);
        Private("ApplyAbilityUsedNearbyAllyDefense", Activator);
        _log.Should().Equal(
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 4 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}",
            $"Replace {Ally} {StatType.ForceDefensePercentAdjustment} 5 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment, 0);
        Private("ApplyAbilityUsedNearbyAllyDefense", Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 4 6 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 0);
        Private("ApplyAbilityUsedNearbyAllyDefense", Activator);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 6);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment, 0);
        Private("ApplyAbilityUsedNearbyAllyDefense", Activator);
        _log.Should().BeEmpty("no duration, then no defense of either kind");
    }

    [Test]
    public void TheNearbyAllyAttackDeflectionNeedsTheCategoryAGrantedTypeADeflectionAndADuration()
    {
        var ability = Hostile();
        _inShape.AddRange(new[] { Ally, Activator });
        _party.Add(Ally);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 6);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment, 25);

        Private("ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection", Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId);
        Private("ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection", Activator, ability);
        _log.Should().BeEmpty("no granted deflection type");

        _grantedDeflections[StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection] = StatType.RangedDeflection;
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 0);
        Private("ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection", Activator, ability);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 0);
        Private("ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection", Activator, ability);
        _log.Should().BeEmpty("no duration, then no deflection");

        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 6);
        Private("ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"Apply {Activator}>{Ally} SentinelGuardStatusEffect(6,25,{StatType.RangedDeflection}) 5",
                $"Apply {Activator}>{Activator} SentinelGuardStatusEffect(6,25,{StatType.RangedDeflection}) 5",
            },
            "the activator is one of the friends this search returns");
    }

    [Test]
    public void TheSelfDefenseNeedsTheCategoryADurationOneDefenseAndAReadyTrigger()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment, 4);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment, 2);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds, 30);

        Private("ApplyAbilityUsedPerkCategorySelfDefense", Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategorySelfDefenseCategoryId);
        _triggerReady = false;
        Private("ApplyAbilityUsedPerkCategorySelfDefense", Activator, ability);
        _log.Should().Equal($"Trigger {Activator} {StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment} 30");

        _log.Clear();
        _triggerReady = true;
        Private("ApplyAbilityUsedPerkCategorySelfDefense", Activator, ability);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment} 30",
            $"Apply {Activator}>{Activator} GuardingStepStatusEffect(0,4,2) 5");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment, 0);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment, 0);
        Private("ApplyAbilityUsedPerkCategorySelfDefense", Activator, ability);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment, 3);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds, 0);
        Private("ApplyAbilityUsedPerkCategorySelfDefense", Activator, ability);
        _log.Should().BeEmpty("no defense of any kind, then no duration");
    }

    // ------------------------------------------------------------------ the weapon line riders

    [Test]
    public void TheActivatedRidersRunOnlyForTheirSkillAndNeedAnAbility()
    {
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, 1);
        var ability = Hostile();

        Private("ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.Rifle);
        _log.Should().BeEmpty();
        typeof(CombatAbilityActivationService).GetMethod("ApplyAbilityActivatedRiders", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_service, new object?[] { Activator, Target, null, SkillType.Vibroknife });
        _log.Should().BeEmpty("no ability");

        Private("ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.Vibroknife);
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveCombatStatusEffect(-15,10,-15) 8");
    }

    [Test]
    public void TheHeavyVibrobladeRidersGrantTheNextAutoAttackBonusAndGuardiansResolveForTheirPerks()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, 6);
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 9);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 8);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds, 30);

        Private("ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.HeavyVibroblade);
        _log.Should().BeEmpty("neither rider's perks match");
        _asked.Should().Equal(
            $"PerkTypes {Activator} {string.Join(",", StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType)}",
            $"PerkTypes {Activator} {string.Join(",", StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType)}");

        _matchingPerkTypes.Add(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType);
        _matchingPerkTypes.Add(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType);
        _hitPoints[Activator] = (100, 100);
        Private("ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.HeavyVibroblade);

        _log.Take(3).Should().Equal(
            $"Replace {Activator} {StatType.NextAutoAttackDamageBonus} 6 9 {StatType.NextAutoAttackDamageBonus}",
            $"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 30",
            $"Attribute {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent}");
        var shield = SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffects.GetGrantedAmount(
            Activator, SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffectKey.GuardiansResolve);
        var generation = SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffects.GetGrantGeneration(
            Activator, SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffectKey.GuardiansResolve);
        shield.Should().Be(10, "10% of 100 HP fits under every cap");
        generation.Should().BeGreaterThan(0);
        _log.Skip(3).Should().Equal($"Apply {Activator}>{Activator} GuardiansResolveStatusEffect({shield},{generation}) 8");
    }

    [Test]
    public void TheNextAutoAttackBonusAndGuardiansResolveNeedTheirStats()
    {
        Private("ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, -3);
        Private("ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        _log.Should().BeEmpty("no bonus, then no duration");

        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 4);
        Private("ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        _log.Should().Equal($"Replace {Activator} {StatType.NextAutoAttackDamageBonus} -3 4 {StatType.NextAutoAttackDamageBonus}");

        _log.Clear();
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        Private("ApplyGuardiansResolve", Activator);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 0);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 8);
        Private("ApplyGuardiansResolve", Activator);
        _log.Should().BeEmpty("no duration, then no shield");

        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        _triggerReady = false;
        Private("ApplyGuardiansResolve", Activator);
        _log.Should().Equal($"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 0");

        // A creature with no hit points gets no pool, so no status follows the grant.
        _log.Clear();
        _triggerReady = true;
        Private("ApplyGuardiansResolve", Activator);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 0",
            $"Attribute {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent}");
    }

    [Test]
    public void TheBalancedRecoveryRestoresTheBeastAndItsMasterWhenTheCategoryMatchesAndTheTriggerIsReady()
    {
        var ability = Hostile();
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestore, 5);
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds, 12);

        Private("ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.BeastMastery);
        _log.Should().BeEmpty("the category does not match");
        _asked.Should().Equal($"PerkCategory {Activator} {StatType.BeastBalancedAbilityStaminaRestoreCategoryId}");

        _matchingPerkCategories.Add(StatType.BeastBalancedAbilityStaminaRestoreCategoryId);
        _triggerReady = false;
        Private("ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal($"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12");

        _log.Clear();
        _triggerReady = true;
        Private("ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12",
                $"RestoreStamina {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 5",
            },
            "a beast with no master restores only itself");

        _log.Clear();
        _masters[Activator] = Master;
        Private("ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12",
            $"RestoreStamina {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 5",
            $"RestoreStamina {Master} {StatType.BeastBalancedAbilityStaminaRestore} 5");

        _log.Clear();
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestore, 0);
        Private("ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().BeEmpty("no restore");
    }

    [TestCase(1, 10, -15)]
    [TestCase(2, 20, -25)]
    public void EvasiveCombatScalesWithItsRankForAreaOrSingleTargetAbilities(int rank, int evasion, int enmity)
    {
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, rank);

        Private("ApplyVibroknifeShadowActivatedEffects", Activator, new AbilityDetail { Name = "Buff" });
        _log.Should().BeEmpty("neither an area nor a single target ability");

        Private("ApplyVibroknifeShadowActivatedEffects", Activator, new AbilityDetail { Name = "Fan", IsAreaAbility = true });
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveCombatStatusEffect(-15,{evasion},{enmity}) 8");

        _log.Clear();
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, 0);
        Private("ApplyVibroknifeShadowActivatedEffects", Activator, Hostile());
        _log.Should().BeEmpty("no rank");
    }

    [Test]
    public void TheSkirmisherRidersGrantEvasionTheNextPistolAbilityAndAnEnmityDropOnAValidTarget()
    {
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds, 6);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 10);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus, 4);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent, 20);

        Private("ApplyAbilityActivatedRiders", Activator, Target, new AbilityDetail { Name = "Buff" }, SkillType.Pistol);
        _log.Should().BeEmpty("neither an area nor a single target ability");

        Private("ApplyAbilityActivatedRiders", Activator, Target, Hostile(), SkillType.Pistol);
        _log.Should().Equal(
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 10 6 {StatType.PistolSkirmisherEvasiveAbilityEvasionPercent}",
            $"NextSkill {Activator} {SkillType.Pistol} 4 0 6 0 {StatusEffectCategory.None}",
            $"ReduceEnmity {Activator} {Target} 20");

        _log.Clear();
        _invalid.Add(Target);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 0);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus, 0);
        Private("ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        _invalid.Clear();
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent, 0);
        Private("ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds, 0);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 10);
        Private("ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        _log.Should().BeEmpty("an invalid target, no reduction, then no duration");
    }

    [Test]
    public void CenteringNeedsItsStatsAndAReadyTriggerAndDropsEnmityOnAValidTarget()
    {
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 8);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberOffenseCenteringCooldownSeconds, 20);
        Stat(Activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent, 15);

        Private("ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 20",
            $"Apply {Activator}>{Activator} CenteringStatusEffect(8) 6",
            $"ReduceEnmity {Activator} {Target} 15");

        _log.Clear();
        _invalid.Add(Target);
        Private("ApplyLightsaberOffenseCentering", Activator, Target);
        _invalid.Clear();
        Stat(Activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent, 0);
        Private("ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Where(line => line.StartsWith("ReduceEnmity", StringComparison.Ordinal)).Should().BeEmpty("an invalid target, then no reduction");

        _log.Clear();
        _triggerReady = false;
        Private("ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().Equal($"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 20");

        _log.Clear();
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 0);
        Private("ApplyLightsaberOffenseCentering", Activator, Target);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 0);
        Private("ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().BeEmpty("no duration, then no accuracy");
    }

    [Test]
    public void SecondWindRestoresAPercentOfMaximumStaminaScaledByItsAbilityAndCappedAtLowStamina()
    {
        Stat(Activator, StatType.LightsaberOffenseSecondWindThresholdPercent, 30);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.LightsaberOffenseSecondWindCooldownSeconds, 60);
        Stat(Activator, StatType.LightsaberOffenseSecondWindScalingAbility, (int)AbilityType.Agility + 1);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, 14);
        _abilityScores[AbilityType.Agility] = 6;
        _maxStamina[Activator] = 200;
        _stamina[Activator] = 60;

        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().Equal(
            new[]
            {
                $"Trigger {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 60",
                $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 28",
            },
            "10% plus the Agility score of 6 is capped at 14% of 200");

        _log.Clear();
        _abilityScores[AbilityType.Agility] = -3;
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, 0);
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 20",
            "a negative score adds nothing and no maximum caps nothing");

        _log.Clear();
        Stat(Activator, StatType.LightsaberOffenseSecondWindScalingAbility, 0);
        _abilityScores[AbilityType.Agility] = 6;
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 20",
            "no scaling ability");

        _log.Clear();
        _triggerReady = false;
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().Equal($"Trigger {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 60");

        _log.Clear();
        _triggerReady = true;
        _stamina[Activator] = 61;
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _maxStamina[Activator] = 0;
        _stamina[Activator] = 0;
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _maxStamina[Activator] = 200;
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 0);
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.LightsaberOffenseSecondWindThresholdPercent, 0);
        Private("ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().BeEmpty("above the threshold, no maximum stamina, no base percent, then no threshold");
    }

    [Test]
    public void GuardiansInfluenceGrantsItsDeflectionToNearbyPartyMembersButNotTheActivator()
    {
        _inShape.AddRange(new[] { Activator, Ally, Bystander });
        _party.Add(Ally);
        _grantedDeflections[StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection] = StatType.MeleeDeflection;

        Private("ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().BeEmpty("no deflection");

        Stat(Activator, StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection, 5);
        Private("ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.MeleeDeflection} 5 12 {StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection}");

        _log.Clear();
        _grantedDeflections.Clear();
        Private("ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().BeEmpty("no granted deflection type");
    }

    [Test]
    public void TheWardGrantsEachConfiguredDefenseAndItsTargetCopyForTheCategory()
    {
        var ability = Hostile();
        Stat(Activator, StatType.WardAbilityForceDefensePercentAdjustment, 3);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 6);

        Private("ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.WardAbilityDefenseCategoryId);
        Private("ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().Equal(
            $"Replace {Activator} {StatType.ForceDefensePercentAdjustment} 3 6 {StatType.WardAbilityForceDefensePercentAdjustment}",
            $"Replace {Activator} {StatType.WardTargetForceDefensePercentAdjustment} 3 6 {StatType.WardAbilityForceDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.WardAbilityForceDefensePercentAdjustment, 0);
        Private("ApplyLightsaberWardActivatedEffects", Activator, ability);
        Stat(Activator, StatType.WardAbilityDefensePercentAdjustment, 2);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 0);
        Private("ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().BeEmpty("no defense, then no duration");
    }

    // ------------------------------------------------------------------ the hostile ability sequence

    [Test]
    public void ADifferentHostileAbilityWithinTheWindowArmsTheNextAttackBleed()
    {
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);

        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("the first ability starts the sequence");

        _now = _now.AddSeconds(6);
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("the same ability again does not continue the sequence");

        _now = _now.AddSeconds(6);
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.PowerAttack, Hostile());
        _log.Should().Equal(
            new[] { $"Replace {Activator} {StatType.NextDamageDealtBleedDurationSeconds} 3 6 {StatType.NextDamageDealtBleedDurationSeconds}" },
            "a different ability at the edge of the window continues it");

        _log.Clear();
        _now = _now.AddSeconds(6.5);
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("past the window");
    }

    [Test]
    public void TheSequenceRecordsTheLastHostileAbilityEvenWithoutABleedAndIgnoresAnythingElse()
    {
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Sequences().Contains(Activator).Should().BeTrue();

        // With the bleed configured now, the ability recorded without it continues the sequence.
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);
        _now = _now.AddSeconds(2);
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.PowerAttack, Hostile());
        _log.Should().ContainSingle();

        // A window with no bleed records the use and arms nothing.
        _log.Clear();
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 0);
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty();

        // A non-hostile ability, a missing ability and an invalid activator are not recorded.
        Sequences().Clear();
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, new AbilityDetail { Name = "Heal" });
        typeof(CombatAbilityActivationService).GetMethod("ApplyHostileAbilitySequenceEffects", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_service, new object?[] { Activator, FeatType.DoubleShot1, null });
        _invalid.Add(Activator);
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Sequences().Count.Should().Be(0);
    }

    [Test]
    public void ForgettingACreatureDropsOnlyItsSequence()
    {
        Private("ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Private("ApplyHostileAbilitySequenceEffects", Ally, FeatType.DoubleShot1, Hostile());

        _service.ForgetActivationState(Activator);
        _service.ForgetActivationState(Bystander);

        Sequences().Contains(Activator).Should().BeFalse();
        Sequences().Contains(Ally).Should().BeTrue();
    }

    // ------------------------------------------------------------------ the resource restores

    [Test]
    public void TheHostileResourceRestoresRunTheirRidersOnlyForWhatWasRestored()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityFPRestore, 3);
        Stat(Activator, StatType.HostileAbilityStaminaRestore, 4);

        Private("ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
                $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4",
            },
            "no FP was restored, so neither the FP nor the both-resources riders run");

        _log.Clear();
        _restoredFP = 3;
        Private("ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
            $"RestoredFP {Activator}",
            $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4",
            $"RestoredBoth {Activator}");

        _log.Clear();
        _restoredStamina = 0;
        Private("ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
            $"RestoredFP {Activator}",
            $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4");

        _log.Clear();
        Stat(Activator, StatType.HostileAbilityFPRestore, 0);
        Stat(Activator, StatType.HostileAbilityStaminaRestore, 0);
        Private("ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        Stat(Activator, StatType.HostileAbilityFPRestore, 3);
        Private("ApplyHostileAbilityResourceRestoreEffects", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("nothing to restore, then not hostile");
    }

    // ------------------------------------------------------------------ the HP-cost riders

    [Test]
    public void AnHPCostAbilityThatPaidNothingTriggersNothing()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice, 1);

        _service.ApplyHitPointSpendAbilityEffects(Activator, 0, 10);
        _service.ApplyHitPointSpendAbilityEffects(Activator, -5);

        _log.Should().BeEmpty();
    }

    [Test]
    public void AnHPCostAbilityRunsSoulSacrificeSoulAscensionTheStaminaRestoreAndTheTemporaryHPInThatOrder()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds, 8);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice, 1);
        Stat(Activator, StatType.HeavyVibrobladeOffenseSoulAscension, 1);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds, 6);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, (int)AbilityType.Might + 1);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent, 13);
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 50);
        Stat(Activator, StatType.HitPointSpendTemporaryHPDurationSeconds, 12);
        _abilityScores[AbilityType.Might] = 5;
        _maxStamina[Activator] = 100;

        _service.ApplyHitPointSpendAbilityEffects(Activator, 20, 30);

        var restore = CombatRules.CalculateHitPointSpendStaminaRestore(100, 13, 20, 30);
        _log.Should().Equal(
            $"Apply {Activator}>{Activator} SoulSacrificeStatusEffect 30",
            $"Replace {Activator} {StatType.HeavyVibrobladeOffenseSoulAscension} 1 8 {StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds}",
            $"Trigger {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} 6",
            $"RestoreStamina {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} {restore}",
            $"Attribute {Activator} {StatType.HitPointSpendTemporaryHPPercentOfSpentHP}");
    }

    [Test]
    public void TheHPCostRidersUseAOneSecondWindowAtLeastAndNeedTheirStats()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseSoulAscension, 1);

        _service.ApplyHitPointSpendAbilityEffects(Activator, 10);

        _log.Should().Equal(
            new[] { $"Replace {Activator} {StatType.HeavyVibrobladeOffenseSoulAscension} 1 1 {StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds}" },
            "no stamina restore and no temporary HP are configured");
    }

    [Test]
    public void TheHPCostStaminaRestoreNeedsAReadyTriggerAndScalesOnlyWithAPositiveScore()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, (int)AbilityType.Might + 1);
        _abilityScores[AbilityType.Might] = -4;
        _maxStamina[Activator] = 100;

        _triggerReady = false;
        Private("ApplyHitPointSpendStaminaRestore", Activator, 20, 0);
        _log.Should().Equal($"Trigger {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} 0");

        _log.Clear();
        _triggerReady = true;
        Private("ApplyHitPointSpendStaminaRestore", Activator, 20, 0);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} {CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 20, 0)}",
            "a negative score adds nothing and no maximum caps nothing");

        _log.Clear();
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, 0);
        Private("ApplyHitPointSpendStaminaRestore", Activator, 20, 0);
        _log.Last().Should().EndWith($" {CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 20, 0)}", "no scaling ability");
    }

    [Test]
    public void TheHPCostTemporaryHPNeedsAPercentAndADuration()
    {
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 50);
        Private("ApplyHitPointSpendTemporaryHitPoints", Activator, 10);
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 0);
        Stat(Activator, StatType.HitPointSpendTemporaryHPDurationSeconds, 12);
        Private("ApplyHitPointSpendTemporaryHitPoints", Activator, 10);
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 50);
        Private("ApplyHitPointSpendTemporaryHitPoints", Activator, 0);
        _log.Should().BeEmpty("no duration, no percent, then nothing spent");

        Private("ApplyHitPointSpendTemporaryHitPoints", Activator, 10);
        _log.Should().Equal($"Attribute {Activator} {StatType.HitPointSpendTemporaryHPPercentOfSpentHP}");
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheConstructorOnlyAssignsAndTheContainerBuildsOneInstanceWithTheConvertedParts()
    {
        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        _engine.Calls.Should().BeEmpty("the constructor makes no engine call");

        var part = _facades.Get<ICombatAbilityActivationService>();
        part.Should().BeOfType<CombatAbilityActivationService>();
        _facades.Get<ICombatAbilityActivationService>().Should().BeSameAs(part);
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CombatAbilityActivationService).GetField("_damageTaken", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatDamageTakenService>());
        typeof(CombatAbilityActivationService).GetField("_combatActivity", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatActivityService>());
        typeof(CombatAbilityActivationService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
    }
}
