using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The shared bench of the hit effects parts of <c>Combat</c> (the stay-part and the dealt effects, triggered damage, auto-attack, critical hit and defeated enemy parts):
/// stats come from a table, every other part and service is scripted and writes what it was asked into one log (so a test sees the
/// order), the clock is held still and the shared <c>Random</c> is seeded where a chance is rolled. The parts are real and
/// run each other; the damage taken part behind the triggered damage is overridden on the triggered part.
/// </summary>
public abstract class CombatHitEffectsTestBase
{
    protected const uint Attacker = 11;
    protected const uint Defender = 22;
    protected const uint Other = 33;
    protected const uint Gone = 44;
    protected const uint Master = 55;
    protected const uint Ally = 66;

    protected readonly List<IDisposable> _scopes = new();
    protected readonly List<string> _log = new();
    protected FacadeContainer _facades = null!;
    protected FakeNWScript _engine = null!;
    protected TestHitEffects _service = null!;
    protected CombatDealtEffectsService _dealt = null!;
    protected CombatAutoAttackEffectsService _autoAttack = null!;
    protected CombatCriticalHitEffectsService _critical = null!;
    protected CombatDefeatedEnemyEffectsService _defeated = null!;
    protected TestTriggeredDamage _triggered = null!;
    protected DateTime _now;
    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    protected Dictionary<StatType, int> _temporary = null!;
    protected HashSet<(uint, Type)> _statuses = null!;
    protected HashSet<(uint, Type, uint)> _sourceStatuses = null!;
    protected HashSet<(uint, StatusEffectCategory)> _categories = null!;
    protected Dictionary<(uint, Type, uint), IStatusEffect> _effects = null!;
    protected Dictionary<uint, CreatureStatusEffect> _creatureEffects = null!;
    protected Dictionary<(uint, uint), DateTime> _recent = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _dead = null!;
    protected HashSet<uint> _players = null!;
    protected HashSet<uint> _nearby = null!;
    protected Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    protected List<uint> _party = null!;
    protected ToxicRushStatusEffect? _toxicRush;
    protected uint _master;
    protected uint _nearest;
    protected bool _hostile;
    protected bool _beside;
    protected bool _side;
    protected bool _sourceApplied;
    protected bool _triggerReady;
    protected bool _queued;
    protected bool _directTrait;
    protected int _directBonus;
    protected int _typeDealtBonus;
    protected int _sunderedCritical;
    protected int _resisted;
    protected Func<int, int> _taken = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

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
        _sources = new();
        _temporary = new();
        _statuses = new();
        _sourceStatuses = new();
        _categories = new();
        _effects = new();
        _creatureEffects = new();
        _recent = new();
        _invalid = new();
        _dead = new();
        _players = new();
        _nearby = new();
        _hitPoints = new();
        _party = new();
        _toxicRush = null;
        _master = NWScript.OBJECT_INVALID;
        _nearest = NWScript.OBJECT_INVALID;
        _hostile = true;
        _beside = _side = _sourceApplied = _queued = _directTrait = false;
        _triggerReady = true;
        _directBonus = _typeDealtBonus = _sunderedCritical = _resisted = 0;
        _taken = damage => damage;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => _hostile);
        _engine.SetReturnHandler("GetMaster", _ => _master);
        _engine.SetReturnHandler("GetMaxHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Max : 0);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : _nearby.Contains((uint)args[0]!) ? 10 : 0);
        // AbilityTargeting finds the nearby creatures one by one; these are alive, hostile and in range.
        _engine.SetReturnHandler("GetNearestCreatureToLocation", args => _nearby.ElementAtOrDefault((int)args[3]! - 1) is var found and not 0
            ? found
            : NWScript.OBJECT_INVALID);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _service = BuildService();
    }

    protected TestHitEffects BuildService()
    {
        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.GetNearestHostileCreatureWithinRange), args =>
            {
                _log.Add($"Nearest {args[0]} {args[1]} {args[2]} {args[3]}");
                return _nearest;
            })
            .On(nameof(ICombatQueriesService.IsAttackerBesideTarget), _ => _beside)
            .On(nameof(ICombatQueriesService.IsMatchingSideAttack), _ => _side)
            .On(nameof(ICombatQueriesService.TargetHasSourceAppliedStatusCategory), _ => _sourceApplied);
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _log.Add($"Trigger {args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            })
            .On(nameof(ICombatResourcesService.ApplyDamageDerivedHealing), args =>
            {
                _log.Add($"Heal {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}");
                return 0;
            })
            .On(nameof(ICombatResourcesService.HealFromMaxHP), args => Log($"HealMax {args[0]} {args[1]}"))
            .On(nameof(ICombatResourcesService.ReduceFPFromStat), args => Log($"LoseFP {args[0]} {args[1]} {args[2]}"))
            .On(nameof(ICombatResourcesService.ReduceStaminaFromStat), args => Log($"LoseStamina {args[0]} {args[1]} {args[2]}"))
            .On(nameof(ICombatResourcesService.RestoreAbilityHitStaminaFromStat), args => Count($"HitStamina {args[0]} {args[1]} {args[2]}", args[2]))
            .On(nameof(ICombatResourcesService.RestoreFPFromStat), args => Count($"FP {args[0]} {args[1]} {args[2]}", args[2]))
            .On(nameof(ICombatResourcesService.RestoreStaminaFromStat), args => Count($"Stamina {args[0]} {args[1]} {args[2]}", args[2]));
        var timing = ScriptedProxy.Create<ICombatAttackTimingService>(out var timingCalls);
        timingCalls
            .On(nameof(ICombatAttackTimingService.GrantNextAbilityDamageBonus), args => Log($"NextAbilityDamage {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextAutoAttackNoDelay), args => Log($"NoDelay {string.Join(" ", args)}"));
        var suppression = ScriptedProxy.Create<ICombatSuppressionService>(out var suppressionCalls);
        suppressionCalls
            .On(nameof(ICombatSuppressionService.ApplyAutoAttackSuppressionStack), args => Log($"AutoSuppression {string.Join(" ", args)}"))
            .On(nameof(ICombatSuppressionService.ApplyRangedHitSuppressionStack), args => Log($"RangedSuppression {string.Join(" ", args)}"));
        var activity = ScriptedProxy.Create<ICombatActivityService>(out var activityCalls);
        activityCalls
            .On(nameof(ICombatActivityService.ApplyFirstCombatAttackStaminaRestore), args => Log($"FirstAttack {args[0]}"))
            .On(nameof(ICombatActivityService.GetDirectDamageToStatusCategoryOrStealthBonus), _ => _directBonus)
            .On(nameof(ICombatActivityService.TrackCombatActivity), args => Log($"Activity {args[0]}"))
            .On(nameof(ICombatActivityService.TrackRecentDamageTarget), args => Log($"Recent {args[0]} {args[1]}"))
            .On("get_" + nameof(ICombatActivityService.RecentDamageTargets), _ => _recent);
        var modifiers = ScriptedProxy.Create<ICombatDamageModifiersService>(out var modifierCalls);
        modifierCalls
            .On(nameof(ICombatDamageModifiersService.ApplyDamageTypeDealtModifiers), args =>
            {
                _log.Add($"TypeDealt {args[0]} {args[1]} {args[2]}");
                return (int)args[1]! + _typeDealtBonus;
            });
        var repeatedTarget = ScriptedProxy.Create<ICombatRepeatedTargetService>(out var repeatedTargetCalls);
        repeatedTargetCalls.On(nameof(ICombatRepeatedTargetService.ApplySameTargetPressureDamageEffects), args => Log($"Pressure {string.Join(" ", args)}"));
        var abilityHit = ScriptedProxy.Create<ICombatAbilityHitAdjustmentService>(out var abilityHitCalls);
        abilityHitCalls.On(nameof(ICombatAbilityHitAdjustmentService.GetCriticalRateAgainstSunderedTargetAdjustment), _ => _sunderedCritical);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatSources), args => _sources.TryGetValue(((uint)args[0]!, (StatType)args[1]!), out var found)
                ? found
                : Array.Empty<StatAdjustmentSource>())
            .On(nameof(IStatService.RestoreStamina), args => Count($"RestoreStamina {args[0]} {args[1]}", args[1]))
            .On(nameof(IStatService.RestoreFP), args => Count($"RestoreFP {args[0]} {args[1]}", args[1]));
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.HasStatusEffect), args => args.Length == 3 && args[2] is uint source
                ? _sourceStatuses.Contains(((uint)args[0]!, (Type)args[1]!, source))
                : _statuses.Contains(((uint)args[0]!, (Type)args[1]!)))
            .On(nameof(IStatusEffectService.HasStatusEffectCategory), args => _categories.Contains(((uint)args[0]!, (StatusEffectCategory)args[1]!)))
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args => Log($"Remove {args[0]} {((Type)args[1]!).Name} {args[2]}"))
            .On(nameof(IStatusEffectService.GetStatusEffect), args => args.Length == 1
                ? _toxicRush
                : _effects.GetValueOrDefault(((uint)args[0]!, (Type)args[1]!, (uint)args[2]!)))
            .On(nameof(IStatusEffectService.GetCreatureStatusEffects), args =>
                _creatureEffects.TryGetValue((uint)args[0]!, out var effects) ? effects : new CreatureStatusEffect { StatGroup = new StatGroup() });
        var statusDuration = ScriptedProxy.Create<IStatusEffectDurationService>(out var statusDurationCalls);
        statusDurationCalls
            .On(nameof(IStatusEffectDurationService.ExtendStatusEffectDuration), args =>
            {
                _log.Add($"Extend {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]}");
                return true;
            })
            .On(nameof(IStatusEffectDurationService.RefreshStatusEffectDuration), args =>
            {
                _log.Add($"Refresh {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]} {args[5]}");
                return true;
            });
        var statusCombat = ScriptedProxy.Create<IStatusEffectCombatEventService>(out var statusCombatCalls);
        statusCombatCalls.On(nameof(IStatusEffectCombatEventService.NotifyDamageStatusEffects), args => Log($"Notify {string.Join(" ", args)}"));
        var statusApplication = ScriptedProxy.Create<IStatusEffectApplicationService>(out var statusApplicationCalls);
        statusApplicationCalls.On(nameof(IStatusEffectApplicationService.ApplyStatusEffect), args =>
            {
                var type = args[2] as Type ?? args[2]!.GetType();
                var detail = args[2] switch
                {
                    PredatorsMark1StatusEffect => " mark",
                    ForceErosionStatusEffect => " erosion",
                    ExposedStatusEffect => " exposed",
                    InfectionStatusEffect infection => $" stacks {infection.Stacks}",
                    ToxicRushStatusEffect rush => $" stacks {rush.Stacks}",
                    CriticalRateStackTrackerStatusEffect => " tracker",
                    LimitedHasteStatusEffect => " haste",
                    _ => string.Empty
                };
                _log.Add($"Apply {args[0]}>{args[1]} {type.Name}{detail} {args[3]}" + (args.Length > 4 && args[4] is not (null or ResistanceType.Invalid) ? $" {args[4]}" : string.Empty));
                return true;
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
            .On(nameof(ITemporaryStatModifierService.Refresh), args => Log($"RefreshModifier {args[0]} {args[1]} {args[2]} {args[3]}"))
            .On(nameof(ITemporaryStatModifierService.AddCapped), args =>
            {
                _log.Add($"AddCapped {string.Join(" ", args)}");
                return (int)args[2]!;
            });
        var ability = ScriptedProxy.Create<IAbilityImpactState>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityImpactState.TryQueueTrackedDamageEffect), args =>
            {
                _log.Add($"Queue {args[0]} {args[1]} {args[2]} {args[3]}");
                return _queued;
            })
            .On(nameof(IAbilityImpactState.GetActiveAbilityImpactSummary), _ => null);
        var traits = ScriptedProxy.Create<IPairedRangedTraitsService>(out var traitCalls);
        traitCalls
            .On(nameof(IPairedRangedTraitsService.HasDirectHostileDamageTrait), _ => _directTrait)
            .On(nameof(IPairedRangedTraitsService.RecordDirectHostileDamage), args => Log($"DirectHostile {args[0]}"));
        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out var recorderCalls);
        recorderCalls
            .On(nameof(ICombatRecorderService.BeginStatTriggerAttribution), args =>
            {
                _log.Add($"Attribute {args[0]} {args[1]}");
                return default(CombatRecordAttributionScope);
            })
            .On(nameof(ICombatRecorderService.BeginAttribution), args =>
            {
                _log.Add($"Attribute {args[0]} {args[1]}");
                return default(CombatRecordAttributionScope);
            });
        var capstone = ScriptedProxy.Create<IBeastDamageRecording>(out var capstoneCalls);
        capstoneCalls.On(nameof(IBeastDamageRecording.RecordBeastDamage), args => Log($"Beast {args[0]} {args[1]}"));
        var resistance = ScriptedProxy.Create<IResistanceService>(out var resistanceCalls);
        resistanceCalls.On(nameof(IResistanceService.ApplyResistanceToDamage), args =>
        {
            _log.Add($"Resist {args[0]} {args[1]} {args[2]}");
            return (int)args[2]! - _resisted;
        });
        var party = ScriptedProxy.Create<IPartyRoster>(out var partyCalls);
        partyCalls.On(nameof(IPartyRoster.GetAllPartyMembersWithinRange), args =>
        {
            _log.Add($"Party {args[0]} {args[1]}");
            return _party;
        });

        _dealt = new CombatDealtEffectsService(queries, resources, activity, capstone, stat, statusEffect, statusApplication, temporary, traits);
        _triggered = new TestTriggeredDamage(_log, () => _taken, modifiers, _dealt, stat, statusCombat, ability, recorder, resistance);
        _critical = new CombatCriticalHitEffectsService(queries, resources, timing, abilityHit, stat, statusEffect, statusApplication,
            statusDuration, temporary, ability);
        var service = new TestHitEffects(queries, suppression, activity, repeatedTarget, stat, recorder, _dealt, _triggered, _critical);
        _autoAttack = new CombatAutoAttackEffectsService(queries, resources, activity, stat, statusEffect, statusApplication, statusDuration,
            temporary, service);
        _defeated = new CombatDefeatedEnemyEffectsService(queries, resources, activity, stat, statusEffect, statusApplication, temporary,
            recorder, party);
        return service;
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    protected object? Log(string line)
    {
        _log.Add(line);
        return null;
    }

    protected object Count(string line, object? amount)
    {
        _log.Add(line);
        return (int)amount!;
    }

    protected void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    protected void Sources(uint creature, StatType payload, params Dictionary<StatType, int>[] sources) =>
        _sources[(creature, payload)] = sources.Select((stats, i) => new StatAdjustmentSource($"source{i}", stats)).ToArray();

    protected IEnumerable<string> Lines(string prefix) => _log.Where(line => line.StartsWith(prefix, StringComparison.Ordinal));

    protected IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    protected object? Private(string name, params object[] arguments)
    {
        foreach (var part in new object[] { _service, _dealt, _triggered, _autoAttack, _critical, _defeated })
        {
            var type = part is TestHitEffects ? typeof(CombatHitEffectsService) : part.GetType();
            var method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (method != null)
                return method.Invoke(part, arguments);
        }

        throw new MissingMethodException(name);
    }

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    protected static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    protected void RunDelayed()
    {
        foreach (var call in _engine.CallsTo("DelayCommand").Concat(_engine.CallsTo("AssignCommand")).ToArray())
            ((Action)call.Arguments[1]!)();
    }

    protected void Critical(int damage = 20, int rating = 2, bool singleTarget = false, SkillType skill = SkillType.Invalid) =>
        _critical.ApplyCriticalHitEffects(Attacker, Defender, damage, rating, singleTarget, skill);

    protected void Hit(int damage, SkillType skill = SkillType.Invalid, CombatDamageType type = CombatDamageType.Physical,
        CombatDamageDeliveryType delivery = CombatDamageDeliveryType.Direct, bool ability = false, bool procs = true) =>
        _service.ApplyDamageDealtEffects(Attacker, Defender, damage, skill, type, delivery, ability, procs);

    [Test]
    public void NoDamageTriggersNothing()
    {
        Hit(0);
        Hit(-5);
        _log.Should().BeEmpty();
    }
}
