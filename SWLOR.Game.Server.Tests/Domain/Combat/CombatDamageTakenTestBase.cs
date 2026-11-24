using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The shared bench of the damage taken parts of <c>Combat</c> (the stay-part, the effects part, the reflection part and the three
/// guard parts): stats
/// come from tables, every other part and service is scripted and writes what it was asked into one log (so a test sees the
/// order), the clock is held still and the shared <c>Random</c> is seeded where a chance is rolled. The one call that lives on
/// the damage roll part, the guard retaliation damage roll, is overridden on the retaliation part; the defense memory is the real part.
/// </summary>
public abstract class CombatDamageTakenTestBase
{
    protected const uint Attacker = 11;
    protected const uint Defender = 22;
    protected const uint Other = 33;
    protected const uint Gone = 44;
    protected const uint Ally = 55;
    protected const uint Module = 77;
    protected const uint Weapon = 88;
    protected const uint AreaOne = 101;
    protected const uint AreaTwo = 102;

    protected readonly List<IDisposable> _scopes = new();
    protected readonly List<string> _log = new();
    protected FacadeContainer _facades = null!;
    protected FakeNWScript _engine = null!;
    protected CombatDamageTakenService _service = null!;
    protected CombatGuardedHitService _guardedHit = null!;
    protected CombatGuardedHitEffectsService _guardedEffects = null!;
    protected TestGuardRetaliation _retaliation = null!;
    protected CombatDamageTakenEffectsService _effects = null!;
    protected CombatDamageReflectionService _reflection = null!;
    protected CombatDefenseMemoryService _defenseMemory = null!;
    protected DateTime _now;
    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected Dictionary<(uint, StatType), int> _permanent = null!;
    protected Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    protected Dictionary<StatType, int> _temporary = null!;
    protected Dictionary<StatType, uint> _statusSources = null!;
    protected Dictionary<StatType, DeflectionSource> _deflectionSources = null!;
    protected Dictionary<StatType, StatType> _grantedDeflections = null!;
    protected Dictionary<(uint, uint), DateTime> _recent = null!;
    protected Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    protected Dictionary<uint, uint> _areas = null!;
    protected Dictionary<uint, float> _distances = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _dead = null!;
    protected HashSet<uint> _players = null!;
    protected HashSet<uint> _dms = null!;
    protected HashSet<uint> _possessed = null!;
    protected HashSet<uint> _nonCreatures = null!;
    protected HashSet<uint> _immune = null!;
    protected HashSet<(uint, uint)> _hostile = null!;
    protected HashSet<(uint, uint)> _enemies = null!;
    protected HashSet<uint> _party = new();
    protected List<uint> _inShape = null!;
    protected HashSet<uint> _vanishAfterSearch = null!;
    protected int _shapeIndex;
    protected bool _triggerReady;
    protected bool _crossed;
    protected bool _recording;
    protected bool _statusApplies;
    protected int _guardChance;
    protected int _pvpBonus;
    protected int _restoredFP;
    protected int _calculated;
    protected int _defenseBonus;
    protected int _attackBonus;
    protected SkillType _equippedSkill;
    protected uint _weapon;
    protected AbilityType _damageAbility;
    protected Dictionary<uint, int> _weaponDamage = null!;
    protected Func<int, int> _triggered = null!;
    protected BuildMessageDelegate? _message;

    // The ally search of the deflection guard asks the party roster (GameServices.PartyRoster) whether a nearby creature
    // is in the creature's party.
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
        _stats = new();
        _permanent = new();
        _sources = new();
        _temporary = new();
        _statusSources = new();
        _deflectionSources = new();
        _grantedDeflections = new();
        _recent = new();
        _hitPoints = new();
        _areas = new();
        _distances = new();
        _invalid = new();
        _dead = new();
        _players = new();
        _dms = new();
        _possessed = new();
        _nonCreatures = new();
        _immune = new();
        _hostile = new();
        _enemies = new();
        _party = new();
        _inShape = new();
        _vanishAfterSearch = new();
        _shapeIndex = 0;
        _triggerReady = true;
        _crossed = true;
        _recording = false;
        _statusApplies = true;
        _guardChance = 0;
        _pvpBonus = 0;
        _restoredFP = 0;
        _calculated = 0;
        _defenseBonus = 0;
        _attackBonus = 0;
        _equippedSkill = SkillType.Invalid;
        _weapon = NWScript.OBJECT_INVALID;
        _damageAbility = AbilityType.Invalid;
        _weaponDamage = new();
        _triggered = damage => damage;
        _message = null;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDM", args => _dms.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", args => _possessed.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args => _hostile.Contains(((uint)args[0]!, (uint)args[1]!)));
        _engine.SetReturnHandler("GetIsEnemy", args => _enemies.Contains(((uint)args[0]!, (uint)args[1]!)));
        _engine.SetReturnHandler("GetObjectType", args => _nonCreatures.Contains((uint)args[0]!) ? ObjectType.Placeable : ObjectType.Creature);
        _engine.SetReturnHandler("GetMaxHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Max : 0);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : 0);
        _engine.SetReturnHandler("GetArea", args => _areas.GetValueOrDefault((uint)args[0]!, AreaOne));
        _engine.SetReturnHandler("GetAbilityScore", args => 10 + (int)(AbilityType)args[1]!);
        _engine.SetReturnHandler("GetBaseItemType", _ => BaseItem.Longsword);
        _engine.SetReturnHandler("GetModule", _ => Module);
        _engine.SetReturnHandler("GetDistanceBetween", args => _distances.GetValueOrDefault((uint)args[1]!, 1f));
        _engine.SetReturnHandler("GetFirstObjectInShape", _ =>
        {
            _shapeIndex = 0;
            return _inShape.Count > 0 ? _inShape[_shapeIndex++] : NWScript.OBJECT_INVALID;
        });
        _engine.SetReturnHandler("GetNextObjectInShape", _ =>
        {
            if (_shapeIndex < _inShape.Count)
                return _inShape[_shapeIndex++];

            // A creature can be gone by the time the caller walks the finished search.
            _invalid.UnionWith(_vanishAfterSearch);
            return NWScript.OBJECT_INVALID;
        });
        _scopes.Add(NWScriptRuntime.Override(_engine));
        // The temporary HP pools read the native current hit points through the object plugin.
        var objects = new FakeObjectPlugin();
        objects.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : 0);
        _scopes.Add(ObjectPluginRuntime.Override(objects));

        _defenseMemory = new CombatDefenseMemoryService();
        _service = BuildService();
    }

    protected CombatDamageTakenService BuildService()
    {
        var feedback = ScriptedProxy.Create<ICombatFeedbackService>(out var feedbackCalls);
        feedbackCalls.On(nameof(ICombatFeedbackService.SendGuardedHitFeedback), args => Log($"Feedback {args[0]} {args[1]} {args[2]}"));
        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.GetCombatImpactWeaponDamage), args =>
            {
                _log.Add($"WeaponDamage {string.Join(" ", args.Select(arg => arg ?? "null"))}");
                return _weaponDamage.GetValueOrDefault((uint)args[0]!);
            })
            .On(nameof(ICombatQueriesService.GetEquippedWeaponSkillType), _ => _equippedSkill)
            .On(nameof(ICombatQueriesService.GetRelevantSkillWeapon), args =>
            {
                _log.Add($"Weapon {args[0]} {args[1]}");
                return _weapon;
            })
            .On(nameof(ICombatQueriesService.GetWeaponDamageAbilityType), args =>
            {
                _log.Add($"DamageAbility {args[0]} {args[1]}");
                return _damageAbility;
            })
            .On(nameof(ICombatQueriesService.HasDamageImmunity), args => _immune.Contains((uint)args[0]!));
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.ApplyAbilityRestoredFPEffects), args => Log($"RestoredFP {args[0]}"))
            .On(nameof(ICombatResourcesService.DidCrossHPThreshold), args =>
            {
                _log.Add($"Crossed {args[0]} {args[1]} {args[2]}");
                return _crossed;
            })
            .On(nameof(ICombatResourcesService.RefreshDamageDerivedHealingTargetHP), args => Log($"RefreshHealing {args[0]} {args[1]}"))
            .On(nameof(ICombatResourcesService.RestoreStaminaFromStat), args => Count($"Stamina {args[0]} {args[1]} {args[2]}", args[2]))
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _log.Add($"Trigger {args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            });
        var timing = ScriptedProxy.Create<ICombatAttackTimingService>(out var timingCalls);
        timingCalls
            .On(nameof(ICombatAttackTimingService.GrantCuedNextSkillAbilityStaminaCostAdjustment), args => Log($"CuedStamina {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextAbilityNoDelay), args => Log($"NextAbilityNoDelay {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextAutoAttackNoDelay), args => Log($"NoDelay {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.GrantNextSkillAbilityBonuses), args => Log($"NextSkill {string.Join(" ", args)}"))
            .On(nameof(ICombatAttackTimingService.ShowReadyCue), args => Log($"Cue {string.Join(" ", args)}"));
        var activity = ScriptedProxy.Create<ICombatActivityService>(out var activityCalls);
        activityCalls
            .On(nameof(ICombatActivityService.TrackHostileDefensiveCombatEntryActivity), args => Log($"Entry {args[0]} {args[1]}"))
            .On(nameof(ICombatActivityService.TrackRecentDamageTaken), args => Log($"RecentTaken {args[0]}"))
            .On("get_" + nameof(ICombatActivityService.RecentDamageTargets), _ => _recent);
        var modifiers = ScriptedProxy.Create<ICombatDamageModifiersService>(out var modifierCalls);
        modifierCalls
            .On(nameof(ICombatDamageModifiersService.ApplyStatusSourceDefenseModifiers), args =>
            {
                _log.Add($"SourceDefense {args[0]} {args[1]} {args[2]}");
                return (int)args[2]! + _defenseBonus;
            })
            .On(nameof(ICombatDamageModifiersService.ApplyTargetStatusAttackModifiers), args =>
            {
                _log.Add($"TargetAttack {args[0]} {args[1]} {args[2]} {args[3]}");
                return (int)args[2]! + _attackBonus;
            });
        var hitEffects = ScriptedProxy.Create<ICombatHitEffectsService>(out var hitEffectCalls);
        hitEffectCalls.On(nameof(ICombatHitEffectsService.ApplyTriggeredDamage), args =>
        {
            _log.Add($"Triggered {string.Join(" ", args.Select(arg => arg ?? "null"))}");
            return _triggered((int)args[2]!);
        });
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatAdjustmentExcludingTemporaryModifiers), args => _permanent.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatSources), args => _sources.TryGetValue(((uint)args[0]!, (StatType)args[1]!), out var found)
                ? found
                : Array.Empty<StatAdjustmentSource>())
            .On(nameof(IStatService.GetStatTypeDeflectionSource), args => _deflectionSources.GetValueOrDefault((StatType)args[0]!))
            .On(nameof(IStatService.GetGrantedDeflectionStatType), args => _grantedDeflections.GetValueOrDefault((StatType)args[0]!, StatType.Invalid))
            .On(nameof(IStatService.RestoreFP), args =>
            {
                _log.Add($"RestoreFP {args[0]} {args[1]}");
                return _restoredFP;
            });
        var rating = ScriptedProxy.Create<ICombatRatingService>(out var ratingCalls);
        ratingCalls
            .On(nameof(ICombatRatingService.GetGuardChance), _ => _guardChance)
            .On(nameof(ICombatRatingService.GetAttack), args =>
            {
                _log.Add($"Attack {args[0]} {args[1]} {args[2]}");
                return 100;
            })
            .On(nameof(ICombatRatingService.GetDefense), args =>
            {
                _log.Add($"Defense {args[0]} {args[1]} {args[2]}");
                return (CombatDamageType)args[1]! == CombatDamageType.Force ? 40 : 60;
            });
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.GetStatusEffectSourceWithStat), args => _statusSources.GetValueOrDefault((StatType)args[1]!, NWScript.OBJECT_INVALID))
            .On(nameof(IStatusEffectService.RemoveStatusEffectsWithStat), args => Log($"RemoveWithStat {args[0]} {args[1]} {args[2]}"))
            .On(nameof(IStatusEffectService.ConsumeStatusEffectStat), args => Log($"ConsumeStat {args[0]} {args[1]}"));
        var statusCombat = ScriptedProxy.Create<IStatusEffectCombatEventService>(out var statusCombatCalls);
        statusCombatCalls.On(nameof(IStatusEffectCombatEventService.OnGuardedHit), args => Log($"OnGuarded {args[0]} {args[1]} {args[2]}"));
        var statusApplication = ScriptedProxy.Create<IStatusEffectApplicationService>(out var statusApplicationCalls);
        statusApplicationCalls.On(nameof(IStatusEffectApplicationService.ApplyStatusEffect), args =>
            {
                var detail = args[2] switch
                {
                    ForceWardingStatusEffect => "ForceWarding",
                    UnbreakableStatusEffect => "Unbreakable",
                    GuardianReflexesStatusEffect => "GuardianReflexes",
                    RangedDeflectionStatusEffect => "RangedDeflection",
                    _ => args[2]!.GetType().Name
                };
                _log.Add($"Apply {args[0]}>{args[1]} {detail} {args[3]}" + (args.Length > 4 && args[4] is CombatDamageType damageType ? $" {damageType}" : string.Empty));
                return _statusApplies;
            });
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls
            .On(nameof(ITemporaryStatModifierService.Replace), args => Log($"Replace {args[0]} {args[1]} {args[2]} {args[3]} {args[4]}"))
            .On(nameof(ITemporaryStatModifierService.GetStatAdjustment), args => _temporary.GetValueOrDefault((StatType)args[1]!))
            .On(nameof(ITemporaryStatModifierService.Consume), args =>
            {
                var statType = (StatType)args[1]!;
                _log.Add($"Consume {args[0]} {statType} {args[2]}");
                var value = _temporary.GetValueOrDefault(statType);
                _temporary.Remove(statType);
                return value;
            });
        var recorder = ScriptedProxy.Create<ICombatRecorderService>(out var recorderCalls);
        recorderCalls
            .On("get_" + nameof(ICombatRecorderService.IsRecording), _ => _recording)
            .On(nameof(ICombatRecorderService.ExpectDeferredDamage), args => Log($"Expect {args[0]} {args[1]} {args[2]} {args[3]}"))
            .On(nameof(ICombatRecorderService.KeepLethalHitPoints), args => Log($"KeepLethal {args[0]}"))
            .On(nameof(ICombatRecorderService.CaptureAttackAttribution), args =>
            {
                _log.Add($"Capture {args[0]}");
                return default(CombatRecordAttribution);
            })
            .On(nameof(ICombatRecorderService.BeginStatTriggerAttribution), args =>
            {
                _log.Add($"Attribute {args[0]} {args[1]}");
                return default(CombatRecordAttributionScope);
            })
            .On(nameof(ICombatRecorderService.BeginAttribution), args =>
            {
                _log.Add("AttributeCaptured");
                return default(CombatRecordAttributionScope);
            });
        var pvp = ScriptedProxy.Create<IPvpCombatService>(out var pvpCalls);
        pvpCalls
            .On(nameof(IPvpCombatService.ApplyDamageIncrease), args =>
            {
                _log.Add($"Pvp {args[0]} {args[1]} {args[2]} {args[3]}");
                return (int)args[2]! + _pvpBonus;
            })
            .On(nameof(IPvpCombatService.RecordHit), args => Log($"PvpHit {args[0]} {args[1]}"));
        var enmity = ScriptedProxy.Create<IEnmityService>(out var enmityCalls);
        enmityCalls.On(nameof(IEnmityService.ModifyEnmity), args => Log($"Enmity {args[0]} {args[1]} {args[2]}"));
        var messaging = ScriptedProxy.Create<IMessagingService>(out var messagingCalls);
        messagingCalls.On(nameof(IMessagingService.SendMessageNearbyToPlayers), args =>
        {
            _message = (BuildMessageDelegate)args[1]!;
            _log.Add($"Nearby {args[0]} {args[2]}");
            return null;
        });
        var playerName = ScriptedProxy.Create<IPlayerNameService>(out var playerNameCalls);
        playerNameCalls.On(nameof(IPlayerNameService.GetColoredDisplayName), args => $"<{args[0]}:{args[1]}>");

        _effects = new CombatDamageTakenEffectsService(resources, timing, activity, hitEffects, stat, statusEffect, statusApplication, temporary, recorder, _defenseMemory);
        _reflection = new CombatDamageReflectionService(queries, resources, hitEffects, stat, statusApplication, recorder, messaging, playerName);
        var taken = new CombatDamageTakenService(queries, resources, stat, statusEffect, recorder, pvp, enmity);
        _guardedEffects = new CombatGuardedHitEffectsService(queries, timing, stat, temporary, _defenseMemory);
        _retaliation = new TestGuardRetaliation(_log, () => _calculated, queries, modifiers, hitEffects, stat, rating, recorder, enmity, taken);
        _guardedHit = new CombatGuardedHitService(feedback, rating, statusCombat, taken, _guardedEffects, _retaliation, stat, resources, enmity);
        return taken;
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

    protected IEnumerable<VisualEffect> Visuals() => _engine.CallsTo("EffectVisualEffect").Select(call => (VisualEffect)call.Arguments[0]!);

    protected IEnumerable<uint> EffectTargets() => _engine.CallsTo("ApplyEffectToObject").Select(call => (uint)call.Arguments[2]!);

    // A private member of the part itself, found through the test double that derives from it.
    protected object? PrivateOf(object target, string name, params object[] arguments)
    {
        var types = arguments.Select(argument => argument.GetType()).ToArray();
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, types, null);
            if (method != null)
                return method.Invoke(target, arguments);
        }

        throw new MissingMethodException(target.GetType().Name, name);
    }

    protected object? Private(string name, params object[] arguments) =>
        typeof(CombatDamageTakenService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null,
            arguments.Select(argument => argument.GetType()).ToArray(), null)!.Invoke(_service, arguments);

    protected Dictionary<uint, DateTime> GuardedHits() =>
        (Dictionary<uint, DateTime>)typeof(CombatDefenseMemoryService)
            .GetField("_recentGuardedHits", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_defenseMemory)!;

    protected Dictionary<(uint Creature, DeflectionSource Source), DateTime> Deflections() =>
        (Dictionary<(uint Creature, DeflectionSource Source), DateTime>)typeof(CombatDefenseMemoryService)
            .GetField("_recentDeflections", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_defenseMemory)!;

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    protected static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    protected void RunCommands()
    {
        foreach (var call in _engine.CallsTo("AssignCommand").Concat(_engine.CallsTo("DelayCommand")).ToArray())
            ((Action)call.Arguments[1]!)();
    }
}
