using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage taken part of <c>Combat</c>: the modifiers of an incoming hit, the effects of damage taken and of low HP,
/// Guard and its riders, avoided attacks, deflection and Deflecting Return, and the incoming physical-to-Force conversion.
/// Stats come from tables, every other part and service is scripted and writes what it was asked into one log (so a test
/// sees the order), the clock is held still and the shared <c>Random</c> is seeded where a chance is rolled. The one call
/// that stays on <c>Combat</c>, the guard retaliation damage roll, is overridden.
/// </summary>
[TestFixture]
public class CombatDamageTakenServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Other = 33;
    private const uint Gone = 44;
    private const uint Ally = 55;
    private const uint Module = 77;
    private const uint Weapon = 88;
    private const uint AreaOne = 101;
    private const uint AreaTwo = 102;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _log = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private TestDamageTaken _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<(uint, StatType), int> _permanent = null!;
    private Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    private Dictionary<StatType, int> _temporary = null!;
    private Dictionary<StatType, uint> _statusSources = null!;
    private Dictionary<StatType, DeflectionSource> _deflectionSources = null!;
    private Dictionary<StatType, StatType> _grantedDeflections = null!;
    private Dictionary<(uint, uint), DateTime> _recent = null!;
    private Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    private Dictionary<uint, uint> _areas = null!;
    private Dictionary<uint, float> _distances = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _dead = null!;
    private HashSet<uint> _players = null!;
    private HashSet<uint> _dms = null!;
    private HashSet<uint> _possessed = null!;
    private HashSet<uint> _nonCreatures = null!;
    private HashSet<uint> _immune = null!;
    private HashSet<(uint, uint)> _hostile = null!;
    private HashSet<(uint, uint)> _enemies = null!;
    private HashSet<uint> _party = new();
    private List<uint> _inShape = null!;
    private HashSet<uint> _vanishAfterSearch = null!;
    private int _shapeIndex;
    private bool _triggerReady;
    private bool _crossed;
    private bool _recording;
    private bool _statusApplies;
    private int _guardChance;
    private int _pvpBonus;
    private int _restoredFP;
    private int _calculated;
    private int _defenseBonus;
    private int _attackBonus;
    private SkillType _equippedSkill;
    private uint _weapon;
    private AbilityType _damageAbility;
    private Dictionary<uint, int> _weaponDamage = null!;
    private Func<int, int> _triggered = null!;
    private BuildMessageDelegate? _message;

    private sealed class TestDamageTaken : CombatDamageTakenService
    {
        private readonly List<string> _log;
        private readonly Func<int> _calculated;

        public TestDamageTaken(
            List<string> log,
            Func<int> calculated,
            ICombatFeedbackService feedback,
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatDamageModifiersService damageModifiers,
            ICombatAbilityHitService abilityHit,
            ICombatHitEffectsService hitEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatRecorderService combatRecorder,
            IPvpCombatService pvpCombat,
            IEnmityService enmity,
            IMessagingService messaging,
            IPlayerNameService playerName)
            : base(feedback, queries, resources, attackTiming, combatActivity, damageModifiers, abilityHit, hitEffects, stat,
                statusEffect, temporaryStatModifier, combatRecorder, pvpCombat, enmity, messaging, playerName)
        {
            _log = log;
            _calculated = calculated;
        }

        protected override int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            _log.Add($"Calculate {attackerAttack} {attackerDMG} {attackerStat} {defenderDefense} {defenderStat} {critical} {deltaCap}");
            return _calculated();
        }

        public int BaseCalculateDamage(int attack, int dmg, int attackStat, int defense, int defenseStat, int critical) =>
            base.CalculateDamage(attack, dmg, attackStat, defense, defenseStat, critical);
    }

    // The ally search of the deflection guard asks the party facade whether a nearby creature is in the creature's party.
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

        _service = BuildService();
    }

    private TestDamageTaken BuildService()
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
        var abilityHit = ScriptedProxy.Create<ICombatAbilityHitService>(out var abilityHitCalls);
        abilityHitCalls.On(nameof(ICombatAbilityHitService.ApplyGuardedHitNextAttackEffects), args => Log($"GuardedNextAttack {args[0]}"));
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
            .On(nameof(IStatService.GetGuardChance), _ => _guardChance)
            .On(nameof(IStatService.GetAttack), args =>
            {
                _log.Add($"Attack {args[0]} {args[1]} {args[2]}");
                return 100;
            })
            .On(nameof(IStatService.GetDefense), args =>
            {
                _log.Add($"Defense {args[0]} {args[1]} {args[2]}");
                return (CombatDamageType)args[1]! == CombatDamageType.Force ? 40 : 60;
            })
            .On(nameof(IStatService.GetStatTypeDeflectionSource), args => _deflectionSources.GetValueOrDefault((StatType)args[0]!))
            .On(nameof(IStatService.GetGrantedDeflectionStatType), args => _grantedDeflections.GetValueOrDefault((StatType)args[0]!, StatType.Invalid))
            .On(nameof(IStatService.RestoreFP), args =>
            {
                _log.Add($"RestoreFP {args[0]} {args[1]}");
                return _restoredFP;
            });
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.GetStatusEffectSourceWithStat), args => _statusSources.GetValueOrDefault((StatType)args[1]!, NWScript.OBJECT_INVALID))
            .On(nameof(IStatusEffectService.RemoveStatusEffectsWithStat), args => Log($"RemoveWithStat {args[0]} {args[1]} {args[2]}"))
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
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
            })
            .On(nameof(IStatusEffectService.ConsumeStatusEffectStat), args => Log($"ConsumeStat {args[0]} {args[1]}"))
            .On(nameof(IStatusEffectService.OnGuardedHit), args => Log($"OnGuarded {args[0]} {args[1]} {args[2]}"));
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

        return new TestDamageTaken(_log, () => _calculated, feedback, queries, resources, timing, activity, modifiers, abilityHit, hitEffects,
            stat, statusEffect, temporary, recorder, pvp, enmity, messaging, playerName);
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

    private object Count(string line, object? amount)
    {
        _log.Add(line);
        return (int)amount!;
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private void Sources(uint creature, StatType payload, params Dictionary<StatType, int>[] sources) =>
        _sources[(creature, payload)] = sources.Select((stats, i) => new StatAdjustmentSource($"source{i}", stats)).ToArray();

    private IEnumerable<string> Lines(string prefix) => _log.Where(line => line.StartsWith(prefix, StringComparison.Ordinal));

    private IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    private IEnumerable<VisualEffect> Visuals() => _engine.CallsTo("EffectVisualEffect").Select(call => (VisualEffect)call.Arguments[0]!);

    private IEnumerable<uint> EffectTargets() => _engine.CallsTo("ApplyEffectToObject").Select(call => (uint)call.Arguments[2]!);

    private object? Private(string name, params object[] arguments) =>
        typeof(CombatDamageTakenService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null,
            arguments.Select(argument => argument.GetType()).ToArray(), null)!.Invoke(_service, arguments);

    private Dictionary<uint, DateTime> GuardedHits() =>
        (Dictionary<uint, DateTime>)typeof(CombatDamageTakenService)
            .GetField("_recentGuardedHits", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;

    private Dictionary<(uint Creature, DeflectionSource Source), DateTime> Deflections() =>
        (Dictionary<(uint Creature, DeflectionSource Source), DateTime>)typeof(CombatDamageTakenService)
            .GetField("_recentDeflections", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    private static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    private void RunCommands()
    {
        foreach (var call in _engine.CallsTo("AssignCommand").Concat(_engine.CallsTo("DelayCommand")).ToArray())
            ((Action)call.Arguments[1]!)();
    }

    // ------------------------------------------------------------------ the damage taken modifiers

    [Test]
    public void ANonPositiveHitKeepsItsDamageAndTouchesNothing()
    {
        _service.ApplyDamageTakenModifiers(Defender, 0, Attacker).Should().Be(0);
        _service.ApplyDamageTakenModifiers(Defender, -4, Attacker).Should().Be(-4);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnImmuneDefenderTakesNothingAndNothingElseRuns()
    {
        _immune.Add(Defender);

        _service.ApplyDamageTakenModifiers(Defender, 50, Attacker, CombatDamageType.Fire).Should().Be(0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ALandedPhysicalHitStacksThePvpIncreaseTheLeadershipStageTheGenericAndFlatAdjustmentsInThatOrder()
    {
        _pvpBonus = 10;
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -10);
        Stat(Defender, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment, -5);
        Stat(Defender, StatType.LeadershipForceDamageTakenPercentAdjustment, -40);
        Stat(Defender, StatType.LeadershipOtherDamageTakenPercentAdjustment, -40);
        Stat(Defender, StatType.DamageTakenPercentAdjustment, -20);
        Stat(Defender, StatType.DamageTakenFlatAdjustment, 3);

        var expected = CombatRules.ApplyCombinedDamageTakenAdjustment(CombatRules.ApplyPercentDamageAdjustment(110, -15), -10, -20) + 3;
        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, -10)
            .Should().Be(expected);
        _log.Should().Equal($"Pvp {Defender} {Attacker} 100 True", $"RefreshHealing {Attacker} {Defender}");
    }

    [TestCase(CombatDamageType.Force, StatType.LeadershipForceDamageTakenPercentAdjustment, StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment)]
    [TestCase(CombatDamageType.Fire, StatType.LeadershipOtherDamageTakenPercentAdjustment, StatType.Invalid)]
    public void AForceOrOtherHitUsesItsOwnLeadershipChannel(CombatDamageType damageType, StatType channel, StatType recovery)
    {
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -50);
        Stat(Defender, channel, -20);
        if (recovery != StatType.Invalid)
            Stat(Defender, recovery, -10);
        var total = recovery == StatType.Invalid ? -20 : -30;

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, damageType)
            .Should().Be(CombatRules.ApplyCombinedDamageTakenAdjustment(CombatRules.ApplyPercentDamageAdjustment(100, total), 0, 0));
        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, damageType).Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, total));
    }

    [Test]
    public void TheTypedLeadershipStageReadsThePhysicalChannelAndKeepsDamageWithoutAnAdjustment()
    {
        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, CombatDamageType.Physical).Should().Be(100);
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -10);
        Stat(Defender, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment, -10);

        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, CombatDamageType.Physical).Should().Be(80);
        _service.ApplyTypedLeadershipDamageTakenModifier(Defender, 100, CombatDamageType.Force).Should().Be(100);
    }

    [Test]
    public void ALeadershipReductionAlreadyAppliedIsNotAppliedTwice()
    {
        Stat(Defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -50);
        Stat(Defender, StatType.DamageTakenPercentAdjustment, -20);

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, true, true)
            .Should().Be(CombatRules.ApplyCombinedDamageTakenAdjustment(100, 0, -20));
    }

    [Test]
    public void ReductionsNeverTakeALandedHitBelowOne()
    {
        Stat(Defender, StatType.DamageTakenFlatAdjustment, -50);

        _service.ApplyDamageTakenModifiers(Defender, 10, Attacker).Should().Be(1);
    }

    [Test]
    public void ASwingTheEngineMayDiscardStopsAfterTheMathAndSpendsNothing()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 50);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;

        _service.ApplyDamageTakenModifiers(Defender, 40, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, 0, false)
            .Should().Be(40);
        _log.Should().Equal($"Pvp {Defender} {Attacker} 40 False");
    }

    [Test]
    public void TransferredDamageIsNotRaisedAgainButARecordedLandedHitKeepsThePvpFlag()
    {
        _pvpBonus = 99;

        _service.ApplyDamageTakenModifiers(Defender, 40, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred)
            .Should().Be(40);
        _service.ApplyDamageTakenModifiers(Defender, 40, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred, 0, false)
            .Should().Be(40);
        _log.Should().Equal($"PvpHit {Attacker} {Defender}", $"RefreshHealing {Attacker} {Defender}");
    }

    [Test]
    public void ARecordedHitTellsTheRecorderToExpectTheDeferredDamage()
    {
        _recording = true;

        _service.ApplyDamageTakenModifiers(Defender, 30, Attacker, CombatDamageType.Fire, CombatDamageDeliveryType.Triggered).Should().Be(30);
        _log.Should().Equal($"Pvp {Defender} {Attacker} 30 True", $"RefreshHealing {Attacker} {Defender}", $"Expect {Attacker} {Defender} 30 Triggered");
    }

    [Test]
    public void ARedirectSendsItsShareToTheStatusSourceAsTransferredDamageUnderTheAttacker()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 40);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;
        _hostile.Add((Attacker, Ally));

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Fire).Should().Be(60);

        _log.Should().Equal(
            $"Pvp {Defender} {Attacker} 100 True",
            $"RemoveWithStat {Defender} {StatType.DamageTakenRedirectToStatusSourcePercent} False",
            $"PvpHit {Attacker} {Ally}",
            $"RefreshHealing {Attacker} {Ally}",
            $"Enmity {Ally} {Attacker} 40",
            $"RefreshHealing {Attacker} {Defender}");
        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Attacker);
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Holy_Aid);
        EffectTargets().Should().Equal(Ally);

        RunCommands();
        _engine.CallsTo("EffectDamage").Should().ContainSingle().Which.Arguments[0].Should().Be(40);
        EffectTargets().Should().Equal(Ally, Ally);
    }

    [Test]
    public void ARedirectIsCappedAtTheWholeHitAndTheDefenderTakesNothingWhenItIsAllRedirected()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 250);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;
        _invalid.Add(Gone);

        _service.ApplyDamageTakenModifiers(Defender, 50, Gone).Should().Be(0);

        // The attacker is gone, so the redirected hit is dealt under the defender and nobody gains enmity.
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Defender);
        Lines("Enmity").Should().BeEmpty();
        Lines("RefreshHealing").Should().Equal($"RefreshHealing {Gone} {Ally}");
    }

    [Test]
    public void ARedirectTheSourceIsImmuneToStillShowsTheAidButDealsAndAngersNothing()
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 50);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = Ally;
        _immune.Add(Ally);
        _hostile.Add((Attacker, Ally));

        _service.ApplyDamageTakenModifiers(Defender, 20, Attacker).Should().Be(10);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Holy_Aid);
        Lines("Enmity").Should().BeEmpty();
    }

    [TestCase("missing")]
    [TestCase("dead")]
    [TestCase("self")]
    public void ARedirectNeedsALivingStatusSourceOtherThanTheDefender(string source)
    {
        Stat(Defender, StatType.DamageTakenRedirectToStatusSourcePercent, 50);
        _statusSources[StatType.DamageTakenRedirectToStatusSourcePercent] = source switch
        {
            "missing" => NWScript.OBJECT_INVALID,
            "self" => Defender,
            _ => Ally
        };
        if (source == "dead")
            _dead.Add(Ally);

        _service.ApplyDamageTakenModifiers(Defender, 20, Attacker).Should().Be(20);
        Lines("RemoveWithStat").Should().BeEmpty();
    }

    [Test]
    public void AShareCarriesItsPartToTheBondedSourceAsForceDamage()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;
        Stat(Ally, StatType.LeadershipForceDamageTakenPercentAdjustment, -50);
        _hostile.Add((Attacker, Ally));

        var shared = GameMath.PercentOf(100, 30);
        var taken = CombatRules.ApplyCombinedDamageTakenAdjustment(CombatRules.ApplyPercentDamageAdjustment(shared, -50), 0, 0);
        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Fire).Should().Be(100 - shared);

        _log.Should().Equal(
            $"Pvp {Defender} {Attacker} 100 True",
            $"PvpHit {Attacker} {Ally}",
            $"RefreshHealing {Attacker} {Ally}",
            $"Enmity {Ally} {Attacker} {taken}",
            $"RefreshHealing {Attacker} {Defender}");
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Attacker);
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Holy_Aid);
        RunCommands();
        var damage = _engine.CallsTo("EffectDamage").Should().ContainSingle().Subject;
        damage.Arguments[0].Should().Be(taken);
        damage.Arguments[1].Should().Be(CombatDamageType.Force.GetNWScriptDamageType());
    }

    [Test]
    public void AShareTheBondedSourceTakesNothingFromShowsNoAidAndAngersNobody()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;
        _immune.Add(Ally);
        _invalid.Add(Gone);

        _service.ApplyDamageTakenModifiers(Defender, 100, Gone).Should().Be(100 - GameMath.PercentOf(100, 30));
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
        Visuals().Should().BeEmpty();
        Lines("Enmity").Should().BeEmpty();
    }

    [Test]
    public void AShareUnderAMissingAttackerIsDealtUnderTheDefender()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;
        _invalid.Add(Gone);

        _service.ApplyDamageTakenModifiers(Defender, 100, Gone).Should().Be(100 - GameMath.PercentOf(100, 30));
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Defender);
        Lines("Enmity").Should().BeEmpty();
    }

    [TestCase("missing")]
    [TestCase("dead")]
    [TestCase("self")]
    [TestCase("elsewhere")]
    public void AShareNeedsALivingBondedSourceInTheSameArea(string source)
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = source switch
        {
            "missing" => NWScript.OBJECT_INVALID,
            "self" => Defender,
            _ => Ally
        };
        if (source == "dead")
            _dead.Add(Ally);
        if (source == "elsewhere")
            _areas[Ally] = AreaTwo;

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker).Should().Be(100);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void TransferredDamageIsNotSharedAgain()
    {
        Stat(Defender, StatType.DamageTakenShareToStatusSourcePercent, 30);
        _statusSources[StatType.DamageTakenShareToStatusSourcePercent] = Ally;

        _service.ApplyDamageTakenModifiers(Defender, 100, Attacker, CombatDamageType.Physical, CombatDamageDeliveryType.Transferred)
            .Should().Be(100);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void TheRedirectAndShareStagesKeepADamageAlreadyGone()
    {
        Private("ApplyDamageTakenRedirectToStatusSource", Defender, Attacker, 0, CombatDamageType.Physical).Should().Be(0);
        Private("ApplyDamageTakenShareToStatusSource", Defender, Attacker, -2, CombatDamageType.Physical).Should().Be(-2);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AHitTheFatalDamageSaveCatchesDealsNothing()
    {
        _hitPoints[Defender] = (20, 100);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);

        _service.ApplyDamageTakenModifiers(Defender, 30, Attacker).Should().Be(0);
        Lines("RefreshHealing").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle();
    }

    [Test]
    public void ALandedHitThatWouldDropTheDefenderBelowTheThresholdGrantsTheLowHPShieldFirst()
    {
        _hitPoints[Defender] = (60, 100);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 20);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 6);
        Stat(Defender, StatType.LowHPTemporaryHPCooldownSeconds, 30);

        _service.ApplyDamageTakenModifiers(Defender, 70, Attacker).Should().Be(70);
        _log.Should().Equal(
            $"Pvp {Defender} {Attacker} 70 True",
            $"Trigger {Defender} {StatType.LowHPTemporaryHPPercent} 30",
            $"Attribute {Defender} {StatType.LowHPTemporaryHPPercent}",
            $"RefreshHealing {Attacker} {Defender}");
    }

    [TestCase(70, 60, true)]
    [TestCase(30, 60, false)]
    [TestCase(70, 40, false)]
    [TestCase(50, 60, false)]
    public void TheLowHPShieldBeforeFatalDamageNeedsAFatalHitFromAboveTheThreshold(int damage, int currentHP, bool grants)
    {
        _hitPoints[Defender] = (currentHP, 100);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 20);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 6);

        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, damage);
        Lines("Attribute").Should().HaveCount(grants ? 1 : 0);
    }

    [Test]
    public void TheLowHPShieldBeforeFatalDamageNeedsItsStatsALivingDefenderAndAReadyTrigger()
    {
        _hitPoints[Defender] = (60, 100);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 20);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 6);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 0);
        _invalid.Add(Gone);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Gone, 70);
        _dead.Add(Other);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Other, 70);
        _hitPoints[Defender] = (0, 100);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        _hitPoints[Defender] = (60, 0);
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        _hitPoints[Defender] = (60, 100);
        _log.Should().BeEmpty();

        _triggerReady = false;
        Private("ApplyLowHPTemporaryHPBeforeFatalDamage", Defender, 70);
        _log.Should().Equal($"Trigger {Defender} {StatType.LowHPTemporaryHPPercent} 0");
    }

    // ------------------------------------------------------------------ reflection, damage over time, fatal damage

    [TestCase(CombatDamageType.Physical, StatType.PhysicalDamageReflectionPercentAdjustment)]
    [TestCase(CombatDamageType.Force, StatType.ForceDamageReflectionPercentAdjustment)]
    [TestCase(CombatDamageType.Fire, StatType.ElementalDamageReflectionPercentAdjustment)]
    public void DamageIsReflectedByTheChannelOfItsType(CombatDamageType damageType, StatType channel)
    {
        Stat(Defender, channel, 25);

        _service.ApplyDamageReflectionEffects(Attacker, Defender, 40, damageType);
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} 10 {damageType} Invalid False null False");
        Lines("Attribute").Should().Equal($"Attribute {Defender} {channel}");
    }

    [Test]
    public void AForceHitWardsTheDefenderBeforeReflecting()
    {
        Stat(Defender, StatType.ForceDamageTakenForceDefense, 5);
        Stat(Defender, StatType.ForceDamageTakenForceDefenseDurationSeconds, 12);

        _service.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Force);
        _log.Should().Equal($"Apply {Defender}>{Defender} ForceWarding 12 Force");

        _log.Clear();
        Stat(Defender, StatType.ForceDamageTakenForceDefenseDurationSeconds, 0);
        _service.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Force);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ReflectionNeedsTwoDifferentValidCreaturesAndAPositiveHit()
    {
        Stat(Defender, StatType.PhysicalDamageReflectionPercentAdjustment, 25);
        _invalid.Add(Gone);
        _nonCreatures.Add(Other);

        _service.ApplyDamageReflectionEffects(Attacker, Defender, 0, CombatDamageType.Physical);
        _service.ApplyDamageReflectionEffects(Defender, Defender, 40, CombatDamageType.Physical);
        _service.ApplyDamageReflectionEffects(Gone, Defender, 40, CombatDamageType.Physical);
        _service.ApplyDamageReflectionEffects(Attacker, Gone, 40, CombatDamageType.Physical);
        _service.ApplyDamageReflectionEffects(Other, Defender, 40, CombatDamageType.Physical);
        _service.ApplyDamageReflectionEffects(Attacker, Other, 40, CombatDamageType.Physical);
        _service.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Fire);
        _log.Should().BeEmpty();
    }

    [Test]
    public void DamageOverTimeIsAdjustedByItsTypeAndReportsTheAdjustment()
    {
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 0, CombatDamageType.Physical, out var none).Should().Be(0);
        none.Should().Be(0);

        Stat(Defender, StatType.PhysicalDamageOverTimeTakenPercentAdjustment, -30);
        Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, -200);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Physical, out var physical).Should().Be(70);
        physical.Should().Be(-30);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Force, out var force)
            .Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, -CombatRules.MaximumCombinedDamageReductionPercent));
        force.Should().Be(-CombatRules.MaximumCombinedDamageReductionPercent);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Fire).Should().Be(100);
        _service.ApplyDamageOverTimeTakenModifiers(Defender, 100, CombatDamageType.Physical).Should().Be(70);
    }

    [Test]
    public void TheFatalDamageSaveSpendsItsTriggerAndGrantsTemporaryHPFromItsSource()
    {
        _hitPoints[Defender] = (20, 200);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);
        Stat(Defender, StatType.FatalDamageTemporaryHPCooldownSeconds, 60);
        Stat(Defender, StatType.FatalDamageTemporaryHPScalingAbilityScore, 20);
        _statusSources[StatType.FatalDamageTemporaryHPPercent] = Ally;

        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 20, false).Should().BeTrue();
        _log.Should().Equal(
            $"Trigger {Defender} {StatType.FatalDamageTemporaryHPPercent} 60",
            $"RemoveWithStat {Defender} {StatType.FatalDamageTemporaryHPPercent} False",
            $"Attribute {Ally} {StatType.FatalDamageTemporaryHPPercent}");
        Visuals().Last().Should().Be(VisualEffect.Vfx_Imp_Ac_Bonus);
        _engine.CallsTo("SetCurrentHitPoints").Should().BeEmpty();
    }

    [Test]
    public void ADyingDefenderIsStoodUpAtOneHPWhenTheSaveRestoresThem()
    {
        _recording = true;
        _hitPoints[Defender] = (0, 200);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);

        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, true).Should().BeTrue();
        _log.Should().Equal(
            $"Trigger {Defender} {StatType.FatalDamageTemporaryHPPercent} 0",
            $"RemoveWithStat {Defender} {StatType.FatalDamageTemporaryHPPercent} False",
            $"KeepLethal {Defender}",
            $"Attribute {Defender} {StatType.FatalDamageTemporaryHPPercent}");
        _engine.CallsTo("SetCurrentHitPoints").Should().ContainSingle().Which.Arguments[1].Should().Be(1);
    }

    [Test]
    public void ADyingDefenderIsStoodUpWithoutARecordingToo()
    {
        _hitPoints[Defender] = (0, 200);
        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);

        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, true).Should().BeTrue();
        Lines("KeepLethal").Should().BeEmpty();
        _engine.CallsTo("SetCurrentHitPoints").Should().ContainSingle();
    }

    [Test]
    public void TheFatalDamageSaveNeedsAValidDefenderItsStatsAFatalHitAndAReadyTrigger()
    {
        _invalid.Add(Gone);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Gone, 50, false).Should().BeFalse();
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, false).Should().BeFalse();
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();

        Stat(Defender, StatType.FatalDamageTemporaryHPPercent, 10);
        Stat(Defender, StatType.FatalDamageTemporaryHPDurationSeconds, 6);
        _hitPoints[Defender] = (60, 100);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();
        _hitPoints[Defender] = (0, 100);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();
        _hitPoints[Defender] = (20, 100);
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 0, true).Should().BeFalse();
        _log.Should().BeEmpty();

        _triggerReady = false;
        _service.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 50, false).Should().BeFalse();
        _log.Should().Equal($"Trigger {Defender} {StatType.FatalDamageTemporaryHPPercent} 0");
    }

    // ------------------------------------------------------------------ damage taken effects

    [Test]
    public void TheDamageHandlerRunsTheEffectsForTheEventsDefenderDamagerAndTotal()
    {
        _engine.SetReturn("OBJECT_SELF", Defender);
        _engine.SetReturn("GetLastDamager", Attacker);
        _engine.SetReturn("GetTotalDamageDealt", 12);

        _service.ApplyDamageTakenEffects();
        Lines("RecentTaken").Should().Equal($"RecentTaken {Defender}");
        _engine.CallsTo("GetLastDamager").Should().ContainSingle().Which.Arguments[0].Should().Be(Defender);
    }

    [Test]
    public void DamageTakenRunsItsRidersInOrderAndEntersCombatAgainstAHostileDamager()
    {
        _hostile.Add((Attacker, Defender));
        Stat(Defender, StatType.DamageTakenAttackPercentAdjustment, 15);
        Stat(Defender, StatType.DamageTakenAttackDurationSeconds, 6);

        _service.ApplyDamageTakenEffects(Defender, Attacker, 12);
        _log.Should().Equal(
            $"Replace {Defender} {StatType.AttackPercentAdjustment} 15 6 {StatType.DamageTakenAttackPercentAdjustment}",
            $"RecentTaken {Defender}",
            $"Entry {Defender} {Attacker}");
    }

    [Test]
    public void DamageTakenNeedsAValidLivingDefenderAndPositiveDamage()
    {
        _invalid.Add(Gone);
        _dead.Add(Other);
        Stat(Defender, StatType.DamageTakenAttackPercentAdjustment, 15);

        _service.ApplyDamageTakenEffects(Gone, Attacker, 12);
        _service.ApplyDamageTakenEffects(Defender, Attacker, 0);
        _service.ApplyDamageTakenEffects(Other, Attacker, 12);
        _log.Should().BeEmpty();

        _service.ApplyDamageTakenEffects(Defender, Attacker, 12);
        _log.Should().Equal($"RecentTaken {Defender}");
    }

    [Test]
    public void BreakerReversalArmsTheNextSkillAbilityWithItsRequiredCategoryAndCue()
    {
        Stat(Defender, StatType.DamageTakenNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.DamageTakenNextSkillAbilityDamageBonus, 8);
        Stat(Defender, StatType.DamageTakenNextSkillAbilityWindowSeconds, 6);
        Stat(Defender, StatType.DamageTakenNextSkillAbilityRequiredCategory, (int)StatusEffectCategory.Bleeding);
        _temporary[StatType.NextSkillAbilityDamageBonus] = 8;

        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} Vibroblade 8 0 6 0 {StatusEffectCategory.Bleeding}");
        Lines("Cue").Should().Equal($"Cue {Defender} True 8 {ReadyCue.Damage("Breaker Reversal", 8)}");

        _log.Clear();
        Stat(Defender, StatType.DamageTakenNextSkillAbilityWindowSeconds, 0);
        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextSkill").Should().BeEmpty();
    }

    [Test]
    public void ReversalCutStoresItsBonusesForTheWindow()
    {
        Stat(Defender, StatType.TwinBladeDuelistReversalCut, 1);
        Stat(Defender, StatType.TwinBladeDuelistReversalCutWindowSeconds, 4);
        _permanent[(Defender, StatType.TwinBladeDuelistReversalCutDamageBonus)] = 9;
        _permanent[(Defender, StatType.TwinBladeDuelistReversalCutDazedDurationSeconds)] = 2;

        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("Replace").Should().Equal(
            $"Replace {Defender} {StatType.TwinBladeDuelistReversalCutDamageBonus} 9 4 {StatType.TwinBladeDuelistReversalCut}",
            $"Replace {Defender} {StatType.TwinBladeDuelistReversalCutDazedDurationSeconds} 2 4 {StatType.TwinBladeDuelistReversalCut}");

        _log.Clear();
        _permanent.Clear();
        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("Replace").Should().BeEmpty();

        Stat(Defender, StatType.TwinBladeDuelistReversalCutWindowSeconds, 0);
        _permanent[(Defender, StatType.TwinBladeDuelistReversalCutDamageBonus)] = 9;
        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void AHitFromARecentTargetCanReadyTheNextAbilityWithoutDelay()
    {
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, 100);
        Stat(Defender, StatType.DamageTakenRecentTargetWindowSeconds, 6);
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType, (int)SkillType.Rifle);
        _recent[(Defender, Attacker)] = _now.AddSeconds(-5);

        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextAbilityNoDelay").Should().Equal($"NextAbilityNoDelay {Defender} Rifle 6 0");

        _log.Clear();
        _recent[(Defender, Attacker)] = _now.AddSeconds(-7);
        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        _recent.Clear();
        _service.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextAbilityNoDelay").Should().BeEmpty();
    }

    [Test]
    public void TheRecentTargetRollIsOneDrawAgainstTheChance()
    {
        Stat(Defender, StatType.DamageTakenRecentTargetWindowSeconds, 6);
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType, (int)SkillType.Rifle);
        _recent[(Defender, Attacker)] = _now;
        var roll = Rolls(7, 2);
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, roll[0] - 1);

        Private("ApplyRecentDamageTargetHitEffects", Defender, Attacker);
        Lines("NextAbilityNoDelay").Should().BeEmpty();
        Random.D100(1).Should().Be(roll[1], "the recent target effect drew once");
    }

    [Test]
    public void TheRecentTargetEffectNeedsTwoDifferentValidCreaturesAndItsStats()
    {
        _invalid.Add(Gone);
        _recent[(Defender, Attacker)] = _now;
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, 100);
        Stat(Defender, StatType.DamageTakenRecentTargetWindowSeconds, 6);

        Private("ApplyRecentDamageTargetHitEffects", Defender, Gone);
        Private("ApplyRecentDamageTargetHitEffects", Gone, Attacker);
        Private("ApplyRecentDamageTargetHitEffects", Defender, Defender);
        Private("ApplyRecentDamageTargetHitEffects", Defender, Attacker);
        _log.Should().BeEmpty("the skill selector is missing");
    }

    // ------------------------------------------------------------------ low HP

    [Test]
    public void LowHPEffectsRunInOrderWhenTheHitCrossesEachThreshold()
    {
        _players.Add(Defender);
        _hitPoints[Defender] = (30, 200);
        Stat(Defender, StatType.LowHPPhysicalDefenseThresholdPercent, 50);
        Stat(Defender, StatType.LowHPPhysicalDefensePercentAdjustment, 10);
        Stat(Defender, StatType.LowHPPhysicalDefenseDurationSeconds, 6);
        Stat(Defender, StatType.LowHPPhysicalDefenseCooldownSeconds, 60);
        Stat(Defender, StatType.LowHPEvasionThresholdPercent, 50);
        Stat(Defender, StatType.LowHPEvasionPercentAdjustment, 15);
        Stat(Defender, StatType.LowHPEvasionDurationSeconds, 7);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, (int)SkillType.Rifle);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds, 8);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 10);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 9);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPPercent, 5);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds, 10);
        Stat(Defender, StatType.LowHPGuardThresholdPercent, 50);
        Stat(Defender, StatType.LowHPGuard, 20);
        Stat(Defender, StatType.LowHPGuardDurationSeconds, 11);

        _service.ApplyLowHPDamageTakenEffects(Defender, 10);
        _log.Where(line => !line.StartsWith("Crossed", StringComparison.Ordinal)).Should().Equal(
            $"Trigger {Defender} {StatType.LowHPPhysicalDefensePercentAdjustment} 60",
            $"Apply {Defender}>{Defender} Unbreakable 6",
            $"Trigger {Defender} {StatType.LowHPEvasionPercentAdjustment} 0",
            $"Replace {Defender} {StatType.EvasionPercentAdjustment} 15 7 {StatType.LowHPEvasionPercentAdjustment}",
            $"Trigger {Defender} {StatType.LowHPNextAbilityNoStaminaCostSkillType} 0",
            $"Replace {Defender} {StatType.NextAbilityNoStaminaCostSkillType} {(int)SkillType.Rifle} 8 {StatType.NextAbilityNoStaminaCostSkillType}",
            $"Trigger {Defender} {StatType.LowHPTemporaryHPPercent} 0",
            $"Attribute {Defender} {StatType.LowHPTemporaryHPPercent}",
            $"Trigger {Defender} {StatType.LowHPNoSaveTemporaryHPPercent} 0",
            $"Attribute {Defender} {StatType.LowHPNoSaveTemporaryHPPercent}",
            $"Trigger {Defender} {StatType.LowHPGuard} 0",
            $"Apply {Defender}>{Defender} GuardianReflexes 11");
        Lines("Crossed").Should().HaveCount(6).And.AllBe($"Crossed {Defender} 10 50");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Guardian Reflexes")}");
        Visuals().Last().Should().Be(VisualEffect.Vfx_Imp_Ac_Bonus);
    }

    [Test]
    public void LowHPEffectsStopAtTheThresholdOrTheTrigger()
    {
        Stat(Defender, StatType.LowHPPhysicalDefenseThresholdPercent, 50);
        Stat(Defender, StatType.LowHPPhysicalDefensePercentAdjustment, 10);
        Stat(Defender, StatType.LowHPPhysicalDefenseDurationSeconds, 6);
        Stat(Defender, StatType.LowHPEvasionThresholdPercent, 50);
        Stat(Defender, StatType.LowHPEvasionPercentAdjustment, 15);
        Stat(Defender, StatType.LowHPEvasionDurationSeconds, 7);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, (int)SkillType.Rifle);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds, 8);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 10);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 9);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPPercent, 5);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds, 10);
        Stat(Defender, StatType.LowHPGuardThresholdPercent, 50);
        Stat(Defender, StatType.LowHPGuard, 20);
        Stat(Defender, StatType.LowHPGuardDurationSeconds, 11);

        _crossed = false;
        _service.ApplyLowHPDamageTakenEffects(Defender, 10);
        Lines("Trigger").Should().BeEmpty();

        _crossed = true;
        _triggerReady = false;
        _service.ApplyLowHPDamageTakenEffects(Defender, 10);
        Lines("Trigger").Should().HaveCount(6);
        _log.Where(line => !line.StartsWith("Crossed", StringComparison.Ordinal) && !line.StartsWith("Trigger", StringComparison.Ordinal))
            .Should().BeEmpty();
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void LowHPEffectsNeedAValidLivingDefenderAPositiveHitAndTheirStats()
    {
        _invalid.Add(Gone);
        _dead.Add(Other);

        _service.ApplyLowHPDamageTakenEffects(Gone, 10);
        _service.ApplyLowHPDamageTakenEffects(Other, 10);
        _service.ApplyLowHPDamageTakenEffects(Defender, 0);
        _service.ApplyLowHPDamageTakenEffects(Defender, 10);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AProtectorsLowHPGuardReadsTheProtectedTargetsThresholdAndItsOwnStats()
    {
        Stat(Ally, StatType.LowHPGuardThresholdPercent, 40);
        Stat(Ally, StatType.LowHPGuard, 25);
        Stat(Ally, StatType.LowHPGuardDurationSeconds, 9);
        Stat(Ally, StatType.LowHPGuardCooldownSeconds, 30);

        _service.ApplyLowHPGuardEffectFromProtectedTarget(Ally, Defender, 15);
        _log.Should().Equal(
            $"Crossed {Defender} 15 40",
            $"Trigger {Ally} {StatType.LowHPGuard} 30",
            $"Apply {Ally}>{Ally} GuardianReflexes 9");
        FloatingText().Should().BeEmpty("the protector is not a player");

        _log.Clear();
        _invalid.Add(Gone);
        _service.ApplyLowHPGuardEffectFromProtectedTarget(Gone, Defender, 15);
        _service.ApplyLowHPGuardEffectFromProtectedTarget(Ally, Gone, 15);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ guard

    [Test]
    public void AGuardedHitLosesItsGuardShareAndRunsEveryRiderInOrder()
    {
        _guardChance = 100;
        _hostile.Add((Attacker, Defender));
        Stat(Defender, StatType.GuardDamageReductionPercentAdjustment, 10);
        Stat(Defender, StatType.GuardStaminaRestore, 4);
        Stat(Defender, StatType.GuardStaminaRestoreCooldownSeconds, 12);
        Stat(Defender, StatType.GuardEnmityPercentAdjustment, 50);

        var prevented = Math.Min(100, GameMath.PercentOf(100, 30));
        _service.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100 - prevented);
        _log.Should().Equal(
            $"GuardedNextAttack {Defender}",
            $"NextSkill {Defender} Invalid 0 0 0 0 None",
            $"OnGuarded {Defender} {Attacker} {prevented}",
            $"Trigger {Defender} {StatType.GuardStaminaRestore} 12",
            $"Stamina {Defender} {StatType.GuardStaminaRestore} 4",
            $"Enmity {Defender} {Attacker} 150",
            $"Feedback {Defender} {Attacker} {prevented}");
        GuardedHits()[Defender].Should().Be(_now);
    }

    [Test]
    public void AGuardedHitIsOneRollAgainstTheGuardChance()
    {
        _hostile.Add((Attacker, Defender));
        var rolls = Rolls(11, 2);
        _guardChance = rolls[0] - 1;

        _service.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _log.Should().BeEmpty();
        Random.D100(1).Should().Be(rolls[1], "the guard drew once");
    }

    [Test]
    public void GuardOnlyAppliesToALandedPositivePhysicalHitFromAHostileSource()
    {
        _guardChance = 100;
        _invalid.Add(Gone);

        _service.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, false).Should().Be(100);
        _service.ApplyGuardedHitModifiers(Gone, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _service.ApplyGuardedHitModifiers(Defender, Gone, 100, CombatDamageType.Physical, true).Should().Be(100);
        _service.ApplyGuardedHitModifiers(Defender, Defender, 100, CombatDamageType.Physical, true).Should().Be(100);
        _service.ApplyGuardedHitModifiers(Defender, Attacker, 0, CombatDamageType.Physical, true).Should().Be(0);
        _service.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Fire, true).Should().Be(100);
        _service.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _guardChance = 0;
        _hostile.Add((Attacker, Defender));
        _service.ApplyGuardedHitModifiers(Defender, Attacker, 100, CombatDamageType.Physical, true).Should().Be(100);
        _log.Should().BeEmpty();
    }

    [TestCase("pc", true)]
    [TestCase("dm", true)]
    [TestCase("possessed", true)]
    [TestCase("hostile", true)]
    [TestCase("hostileBack", true)]
    [TestCase("enemy", true)]
    [TestCase("enemyBack", true)]
    [TestCase("neutral", false)]
    public void AHostileAttackSourceIsAPlayerADMOrAnyHostileOrEnemyRelation(string relation, bool hostile)
    {
        switch (relation)
        {
            case "pc": _players.Add(Attacker); break;
            case "dm": _dms.Add(Attacker); break;
            case "possessed": _possessed.Add(Attacker); break;
            case "hostile": _hostile.Add((Attacker, Defender)); break;
            case "hostileBack": _hostile.Add((Defender, Attacker)); break;
            case "enemy": _enemies.Add((Attacker, Defender)); break;
            case "enemyBack": _enemies.Add((Defender, Attacker)); break;
        }

        _service.IsHostileAttackSource(Defender, Attacker).Should().Be(hostile);
    }

    [TestCase(0, 20)]
    [TestCase(15, 35)]
    [TestCase(100, CombatDamageTakenService.MaximumGuardDamageReductionPercent)]
    [TestCase(-50, 0)]
    public void TheGuardReductionStartsAtItsBaseAndStaysWithinItsBounds(int adjustment, int expected)
    {
        Stat(Defender, StatType.GuardDamageReductionPercentAdjustment, adjustment);

        _service.GetGuardDamageReductionPercent(Defender).Should().Be(expected);
        CombatDamageTakenService.BaseGuardDamageReductionPercent.Should().Be(20);
    }

    [Test]
    public void GuardRecoveryNeedsItsStaminaAndAReadyTrigger()
    {
        Private("ApplyGuardedHitRecovery", Defender);
        Stat(Defender, StatType.GuardStaminaRestore, 4);
        _triggerReady = false;
        Private("ApplyGuardedHitRecovery", Defender);
        _log.Should().Equal($"Trigger {Defender} {StatType.GuardStaminaRestore} 0");
    }

    [Test]
    public void GuardEnmityIsAtLeastOneAndScaledByItsAdjustment()
    {
        Private("ApplyGuardedHitEnmity", Attacker, Defender, 0);
        Private("ApplyGuardedHitEnmity", Attacker, Defender, 9);
        Stat(Defender, StatType.GuardEnmityPercentAdjustment, -99);
        Private("ApplyGuardedHitEnmity", Attacker, Defender, 9);
        _log.Should().Equal($"Enmity {Defender} {Attacker} 1", $"Enmity {Defender} {Attacker} 9", $"Enmity {Defender} {Attacker} 1");
    }

    [Test]
    public void GuardRetaliationResolvesItsDamageRangeAndDealsItAsTriggeredPhysicalDamage()
    {
        _equippedSkill = SkillType.Vibroblade;
        _weapon = Weapon;
        _damageAbility = AbilityType.Perception;
        _calculated = 17;
        _defenseBonus = 5;
        _attackBonus = 3;
        Stat(Defender, StatType.GuardRetaliationDMG, 8);

        Private("ApplyGuardedHitRetaliation", Attacker, Defender, 50);
        var perception = 10 + (int)AbilityType.Perception;
        var vitality = 10 + (int)CombatDamageType.Physical.GetDefenseAbilityType();
        _log.Should().Equal(
            $"Weapon {Defender} Vibroblade",
            $"DamageAbility {Defender} {BaseItem.Longsword}",
            $"Attack {Defender} {AbilityType.Perception} Vibroblade",
            $"TargetAttack {Defender} {Attacker} 100 Vibroblade",
            $"Defense {Attacker} Physical {CombatDamageType.Physical.GetDefenseAbilityType()}",
            $"SourceDefense {Defender} {Attacker} 60",
            $"Calculate 103 8 {perception} 65 {vitality} 0 0",
            $"Attribute {Defender} {StatType.GuardRetaliationDMG}",
            $"Triggered {Defender} {Attacker} 17 Physical Vibroblade False null False");
    }

    [Test]
    public void AGuardRetaliationAgainstAForceConvertingTargetBlendsInItsForceDefense()
    {
        _calculated = 4;
        Stat(Defender, StatType.GuardRetaliationDMG, 8);
        Stat(Attacker, StatType.IncomingPhysicalToForceConversionPercent, 50);

        Private("ApplyGuardedHitRetaliation", Attacker, Defender, 50);
        Lines("Defense").Should().Equal(
            $"Defense {Attacker} Physical {CombatDamageType.Physical.GetDefenseAbilityType()}",
            $"Defense {Attacker} Force {CombatDamageType.Force.GetDefenseAbilityType()}");
        Lines("SourceDefense").Should().Equal($"SourceDefense {Defender} {Attacker} 60", $"SourceDefense {Defender} {Attacker} 40");
        // Without a weapon the retaliation uses Might.
        Lines("Attack").Should().Equal($"Attack {Defender} {AbilityType.Might} Invalid");
        Lines("Calculate").Should().ContainSingle().Which.Should().StartWith("Calculate 100 8 ");
        Lines("Calculate").Single().Split(' ')[4].Should().Be(((60 + 40) / 2).ToString());
    }

    [Test]
    public void AWeaponWithoutADamageAbilityRetaliatesWithMight()
    {
        _weapon = Weapon;
        Stat(Defender, StatType.GuardRetaliationDMG, 8);

        Private("ApplyGuardedHitRetaliation", Attacker, Defender, 50);
        Lines("Attack").Should().Equal($"Attack {Defender} {AbilityType.Might} Invalid");
    }

    [Test]
    public void ARetaliationAgainstAMissingTargetResolvesNothing()
    {
        _invalid.Add(Gone);
        Stat(Defender, StatType.GuardRetaliationDMG, 8);

        Private("ApplyGuardedHitRetaliation", Gone, Defender, 50);
        Lines("Calculate").Should().BeEmpty();
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Gone} 0 Physical Invalid False null False");
        Private("ResolveGuardRetaliationDamage", Gone, Attacker, 8, SkillType.Invalid).Should().Be(0);
        Private("ResolveGuardRetaliationDamage", Defender, Attacker, 0, SkillType.Invalid).Should().Be(0);
    }

    [Test]
    public void AGuardPulseWithoutARadiusHitsOnlyTheAttacker()
    {
        _calculated = 6;
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);

        Private("ApplyGuardedHitRetaliation", Attacker, Defender, 50);
        Lines("Attribute").Should().Equal($"Attribute {Defender} {StatType.GuardedHitPulseDMG}");
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} 6 Physical Invalid False null False");
    }

    [Test]
    public void AGuardPulseHitsTheAttackerAndEveryHostileCreatureAroundAndTellsAPlayer()
    {
        _players.Add(Defender);
        _calculated = 6;
        Stat(Defender, StatType.GuardRetaliationDMG, 0);
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);
        Stat(Defender, StatType.GuardedHitPulseRadiusMeters, 4);
        Stat(Defender, StatType.GuardedHitPulseEnmityPercentOfIncomingDamage, 10);
        _hitPoints[Attacker] = (50, 50);
        _hitPoints[Other] = (50, 50);
        _hitPoints[Ally] = (50, 50);
        _hostile.Add((Attacker, Defender));
        _hostile.Add((Other, Defender));
        _inShape.AddRange(new[] { Attacker, Gone, Other, Ally });
        _vanishAfterSearch.Add(Gone);

        Private("ApplyGuardedHitRetaliation", Attacker, Defender, 50);
        _log.Where(line => line.StartsWith("Triggered", StringComparison.Ordinal) || line.StartsWith("Enmity", StringComparison.Ordinal)).Should().Equal(
            $"Triggered {Defender} {Attacker} 6 Physical Invalid False null False",
            $"Triggered {Defender} {Other} 6 Physical Invalid False null False",
            $"Enmity {Defender} {Other} 5");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Retaliation Pulse")}");
    }

    [Test]
    public void AGuardPulseThatFindsNoLivingHostileTargetDoesNothing()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);
        Stat(Defender, StatType.GuardedHitPulseRadiusMeters, 4);
        _hitPoints[Other] = (0, 50);
        _hitPoints[Ally] = (50, 50);
        _dead.Add(Attacker);
        _hostile.Add((Other, Defender));
        _hostile.Add((Attacker, Defender));
        _inShape.AddRange(new[] { Other, Ally, Attacker });

        Private("ApplyGuardedHitRetaliation", Attacker, Defender, 50);
        Lines("Triggered").Should().BeEmpty();
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void AGuardPulseAddsNoEnmityWithoutItsPercentAndNoTextForANonPlayer()
    {
        Stat(Defender, StatType.GuardedHitPulseDMG, 5);
        Stat(Defender, StatType.GuardedHitPulseRadiusMeters, 4);
        _hitPoints[Other] = (50, 50);
        _hostile.Add((Other, Defender));
        _inShape.Add(Other);
        _invalid.Add(Gone);

        Private("ApplyGuardedHitRetaliation", Gone, Defender, 50);
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Other} 0 Physical Invalid False null False");
        Lines("Enmity").Should().BeEmpty();
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void TrackingAGuardedHitRecordsItAndArmsTheCounterRiders()
    {
        _service.TrackGuardedHit(Defender);
        GuardedHits()[Defender].Should().Be(_now);
        _log.Should().Equal($"GuardedNextAttack {Defender}", $"NextSkill {Defender} Invalid 0 0 0 0 None");

        _log.Clear();
        _invalid.Add(Gone);
        _service.TrackGuardedHit(Gone);
        _log.Should().BeEmpty();
        GuardedHits().Should().NotContainKey(Gone);
    }

    [Test]
    public void ARecentGuardedHitIsReadWithinItsWindowAndForgottenAfter()
    {
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeFalse();
        _service.TrackGuardedHit(Defender);
        _invalid.Add(Gone);
        _service.HasRecentGuardedHit(Gone, 6f).Should().BeFalse();
        _service.HasRecentGuardedHit(Defender, 0f).Should().BeFalse();

        _now = _now.AddSeconds(6);
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeTrue();
        _now = _now.AddSeconds(1);
        _service.HasRecentGuardedHit(Defender, 6f).Should().BeFalse();
        GuardedHits().Should().NotContainKey(Defender);
    }

    [Test]
    public void ACounterOfOneSkillStacksBothGuardedHitSourcesAndTellsAPlayer()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityDamageBonus, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 6);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus, 3);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityCriticalRatePercentAdjustment, 2);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds, 9);

        _service.TrackGuardedHit(Defender);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} Vibroblade 7 7 9 0 None");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Counter Ready: +7 DMG, +7% Crit")}");
    }

    [Test]
    public void ACounterWithOnlyTheSecondarySourceUsesItAndOmitsAZeroCritical()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilitySkillType, (int)SkillType.Spear);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus, 3);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds, 9);

        _service.TrackGuardedHit(Defender);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} Spear 3 0 9 0 None");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Counter Ready: +3 DMG")}");
    }

    [TestCase(SkillType.Spear, "Spear 3 0 9")]
    [TestCase(SkillType.Vibroblade, "Vibroblade 4 0 6")]
    public void CountersOfTwoSkillsPreferTheEquippedWeaponsAndOtherwiseThePrimary(SkillType equipped, string expected)
    {
        _equippedSkill = equipped;
        Stat(Defender, StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityDamageBonus, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 6);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilitySkillType, (int)SkillType.Spear);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus, 3);
        Stat(Defender, StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds, 9);

        _service.TrackGuardedHit(Defender);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} {expected} 0 None");
        FloatingText().Should().BeEmpty("the defender is not a player");
    }

    [Test]
    public void ACounterWithoutAWindowOrBonusShowsNoText()
    {
        _players.Add(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 6);

        _service.TrackGuardedHit(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityDamageBonus, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityWindowSeconds, 0);
        _service.TrackGuardedHit(Defender);
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void GuardReversalArmsTheExposedRiderForItsWindow()
    {
        Stat(Defender, StatType.GuardedHitNextSkillAbilityStatusSkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds, 4);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 6);
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDamageBonus] = 6;

        _service.TrackGuardedHit(Defender);
        _log.Skip(2).Should().Equal(
            $"Cue {Defender} True 6 {ReadyCue.Damage("Guard Reversal", 6)}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityStatusSkillType} {(int)SkillType.Vibroblade} 4 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds} 4 4 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDamageBonus} 6 4 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");
    }

    [Test]
    public void GuardReversalWithOnlyADamageBonusKeepsAOneSecondWindow()
    {
        Stat(Defender, StatType.GuardedHitNextSkillAbilityStatusSkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 6);

        _service.TrackGuardedHit(Defender);
        _log.Skip(2).Should().Equal(
            $"Cue {Defender} False 6 {ReadyCue.Damage("Guard Reversal", 6)}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityStatusSkillType} {(int)SkillType.Vibroblade} 1 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDamageBonus} 6 1 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");

        _log.Clear();
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 0);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds, 3);
        _service.TrackGuardedHit(Defender);
        _log.Skip(2).Should().Equal(
            $"Cue {Defender} False 0 {ReadyCue.Damage("Guard Reversal", 0)}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityStatusSkillType} {(int)SkillType.Vibroblade} 3 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Replace {Defender} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds} 3 3 {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");
    }

    [Test]
    public void GuardReversalNeedsASkillAndADurationOrBonus()
    {
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 6);
        _service.TrackGuardedHit(Defender);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityExposedDamageBonus, 0);
        Stat(Defender, StatType.GuardedHitNextSkillAbilityStatusSkillType, (int)SkillType.Vibroblade);
        _service.TrackGuardedHit(Defender);
        Lines("Cue").Should().BeEmpty();
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void TheExposedBonusIsConsumedOnlyByItsSkill()
    {
        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Vibroblade;
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDamageBonus] = 6;

        _service.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Spear).Should().Be(0);
        _service.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(Defender, SkillType.Vibroblade).Should().Be(6);
        _log.Should().Equal($"Consume {Defender} {StatType.GuardedHitNextSkillAbilityExposedDamageBonus} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}");
    }

    // ------------------------------------------------------------------ avoided attacks

    [Test]
    public void AnAvoidedAttackEntersCombatAndGrantsItsCuedAbilityBonuses()
    {
        Sources(Defender, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment, new Dictionary<StatType, int>
        {
            [StatType.AvoidedAttackNextSkillAbilitySkillType] = (int)SkillType.Rifle,
            [StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment] = -3,
            [StatType.AvoidedAttackNextSkillAbilityWindowSeconds] = 6,
            [StatType.AvoidedAttackNextSkillAbilityRangedOnly] = 1,
        });
        Sources(Defender, StatType.AvoidedAttackNextSkillAbilityDamageBonus, new Dictionary<StatType, int>
        {
            [StatType.AvoidedAttackNextSkillAbilitySkillType] = (int)SkillType.Pistol,
            [StatType.AvoidedAttackNextSkillAbilityDamageBonus] = 5,
            [StatType.AvoidedAttackNextSkillAbilityWindowSeconds] = 4,
        });

        _service.TrackAvoidedAttack(Defender, Attacker);
        _log.Should().Equal(
            $"Entry {Defender} {Attacker}",
            $"CuedStamina {Defender} Rifle -3 6 source0:{(int)StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment} True True",
            $"NextSkill {Defender} Pistol 5 0 4 0 None",
            $"NoDelay {Defender} 0");
    }

    [Test]
    public void AnAvoidedAttackRollsItsStaminaRestoreThenItsSingleRestoreInThatOrder()
    {
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreChance, 100);
        Stat(Defender, StatType.AvoidedAttackStaminaRestore, 3);
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreCooldownSeconds, 10);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestoreChance, 100);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestore, 2);
        var rolls = Rolls(5, 3);

        _service.TrackAvoidedAttack(Defender, Attacker);
        _log.Skip(1).Take(4).Should().Equal(
            $"Trigger {Defender} {StatType.AvoidedAttackStaminaRestore} 10",
            $"Stamina {Defender} {StatType.AvoidedAttackStaminaRestore} 3",
            $"ConsumeStat {Defender} {StatType.AvoidedAttackSingleStaminaRestore}",
            $"Stamina {Defender} {StatType.AvoidedAttackSingleStaminaRestore} 2");
        Random.D100(1).Should().Be(rolls[2], "the avoided attack drew twice");
    }

    [Test]
    public void AnAvoidedAttackDrawsNothingForUnconfiguredChancesAndStopsOnAFailedRollOrTrigger()
    {
        var rolls = Rolls(9, 3);
        _service.TrackAvoidedAttack(Defender, Attacker);
        Random.D100(1).Should().Be(rolls[0], "nothing was configured, so nothing was drawn");

        _log.Clear();
        rolls = Rolls(9, 3);
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreChance, rolls[0] - 1);
        Stat(Defender, StatType.AvoidedAttackStaminaRestore, 3);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestoreChance, rolls[1] - 1);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestore, 2);
        _service.TrackAvoidedAttack(Defender, Attacker);
        Lines("Stamina").Should().BeEmpty();
        Lines("Trigger").Should().BeEmpty();
        Random.D100(1).Should().Be(rolls[2]);

        _log.Clear();
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreChance, 100);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestoreChance, 0);
        _triggerReady = false;
        _service.TrackAvoidedAttack(Defender, Attacker);
        Lines("Stamina").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle();
    }

    [Test]
    public void AnAvoidedAttackRefreshesTheRangedDeflectionItsAbilityGrantsAndItsRiders()
    {
        Stat(Defender, StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds, 8);
        Stat(Defender, StatType.AbilityUsedRangedDeflection, 12);
        Stat(Defender, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.ACIncrease);
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Ranged;
        Stat(Defender, StatType.AbilityGrantedAttackDeflectionFPRestore, 3);
        _restoredFP = 3;
        Stat(Defender, StatType.AvoidedAttackNextAutoAttackNoDelaySkillType, (int)SkillType.Pistol);
        Stat(Defender, StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds, 5);
        Stat(Defender, StatType.AvoidedAttackAccuracyPercentAdjustment, 7);
        Stat(Defender, StatType.AvoidedAttackAccuracyDurationSeconds, 6);

        _service.TrackAvoidedAttack(Defender, Attacker);
        _log.Should().Equal(
            $"Entry {Defender} {Attacker}",
            $"Apply {Defender}>{Defender} RangedDeflection 8",
            $"Trigger {Defender} {StatType.AbilityGrantedAttackDeflectionFPRestore} 0",
            $"Attribute {Defender} {StatType.AbilityGrantedAttackDeflectionFPRestore}",
            $"RestoreFP {Defender} 3",
            $"RestoredFP {Defender}",
            $"NoDelay {Defender} Pistol 5",
            $"Replace {Defender} {StatType.AccuracyPercentAdjustment} 7 6 {StatType.AvoidedAttackAccuracyPercentAdjustment}");
    }

    [Test]
    public void TheRangedDeflectionRefreshNeedsADeflectionAnIconAndAnAppliedStatus()
    {
        Stat(Defender, StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds, 8);
        _service.TrackAvoidedAttack(Defender, Attacker);
        Stat(Defender, StatType.AbilityUsedRangedDeflection, 12);
        _service.TrackAvoidedAttack(Defender, Attacker);
        Lines("Apply").Should().BeEmpty();

        Stat(Defender, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.ACIncrease);
        _statusApplies = false;
        _service.TrackAvoidedAttack(Defender, Attacker);
        Lines("Apply").Should().ContainSingle();
        Lines("Trigger").Should().BeEmpty();

        _log.Clear();
        _invalid.Add(Gone);
        _service.TrackAvoidedAttack(Gone, Attacker);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ melee damage taken

    [Test]
    public void AMeleeHitRollsThePoisonBackThenTheStaminaRestoreAndEvasion()
    {
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 6);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestoreChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestore, 2);
        Stat(Defender, StatType.MeleeDamageTakenEvasionPercentAdjustment, 5);
        Stat(Defender, StatType.MeleeDamageTakenEvasionDurationSeconds, 4);
        var rolls = Rolls(3, 3);

        _service.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        _log.Should().Equal(
            $"Attribute {Defender} {StatType.MeleeDamageTakenPoisonDamage}",
            $"Triggered {Defender} {Attacker} 6 Poison Invalid False null False",
            $"Stamina {Defender} {StatType.MeleeDamageTakenStaminaRestore} 2",
            $"Replace {Defender} {StatType.EvasionPercentAdjustment} 5 4 {StatType.MeleeDamageTakenEvasionPercentAdjustment}");
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Poison_S);
        EffectTargets().Should().Equal(Attacker);
        Random.D100(1).Should().Be(rolls[2], "the poison and the restore drew once each");
    }

    [Test]
    public void APoisonBackScaledByAnAbilityGrowsWithTheDefendersScore()
    {
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 6);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageScalingAbility, (int)AbilityType.Agility + 1);
        _triggered = _ => 0;

        _service.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        var expected = SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ScaleDirectEffect(
            6, 10 + (int)AbilityType.Agility, source: Defender);
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} {expected} Poison Invalid False null False");
        Visuals().Should().BeEmpty("nothing was dealt");
    }

    [Test]
    public void AMeleeHitStopsAtAFailedRollAndSkipsUnconfiguredRiders()
    {
        var rolls = Rolls(4, 3);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageChance, rolls[0] - 1);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 6);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestoreChance, rolls[1] - 1);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestore, 2);

        _service.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        _log.Should().BeEmpty();
        Random.D100(1).Should().Be(rolls[2]);

        Stat(Defender, StatType.MeleeDamageTakenStaminaRestoreChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestore, 0);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 0);
        _service.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        _log.Should().BeEmpty();

        _invalid.Add(Gone);
        _service.ApplyMeleeDamageTakenEffects(Gone, Attacker);
        _service.ApplyMeleeDamageTakenEffects(Defender, Gone);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ deflection

    [Test]
    public void ADeflectionIsRecordedAndCanGuardTheNearestAlly()
    {
        _deflectionSources[StatType.DeflectionNearbyAllyGuard] = DeflectionSource.Melee;
        Stat(Defender, StatType.DeflectionNearbyAllyGuard, 15);
        Stat(Defender, StatType.DeflectionNearbyAllyGuardDurationSeconds, 6);
        _hitPoints[Ally] = (10, 10);
        _hitPoints[Other] = (10, 10);
        _hitPoints[Defender] = (10, 10);
        _party.UnionWith(new[] { Ally, Other });
        _distances[Ally] = 4f;
        _distances[Other] = 2f;
        _inShape.AddRange(new[] { Defender, Ally, Other });

        _service.TrackDeflection(Defender, DeflectionSource.Melee);
        Deflections()[(Defender, DeflectionSource.Melee)].Should().Be(_now);
        _log.Should().Equal($"Replace {Other} {StatType.Guard} 15 6 {StatType.DeflectionNearbyAllyGuard}");
    }

    [Test]
    public void TheDeflectionGuardNeedsItsSourceItsStatsAndAnAlly()
    {
        _deflectionSources[StatType.DeflectionNearbyAllyGuard] = DeflectionSource.Melee;
        _service.TrackDeflection(Defender, DeflectionSource.Ranged);
        _service.TrackDeflection(Defender, DeflectionSource.Melee);
        Stat(Defender, StatType.DeflectionNearbyAllyGuard, 15);
        Stat(Defender, StatType.DeflectionNearbyAllyGuardDurationSeconds, 6);
        _service.TrackDeflection(Defender, DeflectionSource.Melee);
        _log.Should().BeEmpty();

        _invalid.Add(Gone);
        _service.TrackDeflection(Gone, DeflectionSource.Melee);
        _service.TrackDeflection(Defender, DeflectionSource.None);
        Deflections().Keys.Should().BeEquivalentTo(new[] { (Defender, DeflectionSource.Ranged), (Defender, DeflectionSource.Melee) });
    }

    [Test]
    public void ARecentDeflectionIsReadPerSourceWithinItsWindowAndForgottenAfter()
    {
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse();
        _service.TrackDeflection(Defender, DeflectionSource.Melee);
        _invalid.Add(Gone);
        _service.HasRecentDeflection(Gone, DeflectionSource.Melee, 6f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.None, 6f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 0f).Should().BeFalse();
        _service.HasRecentDeflection(Defender, DeflectionSource.Ranged, 6f).Should().BeFalse();

        _now = _now.AddSeconds(6);
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeTrue();
        _now = _now.AddSeconds(1);
        _service.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse();
        Deflections().Should().BeEmpty();
    }

    [Test]
    public void ForgettingACreatureDropsOnlyItsGuardedHitAndDeflections()
    {
        _service.TrackGuardedHit(Defender);
        _service.TrackGuardedHit(Other);
        _service.TrackDeflection(Defender, DeflectionSource.Melee);
        _service.TrackDeflection(Defender, DeflectionSource.Ranged);
        _service.TrackDeflection(Other, DeflectionSource.Melee);

        _service.ForgetDamageTakenState(Defender);
        GuardedHits().Keys.Should().Equal(Other);
        Deflections().Keys.Should().Equal((Other, DeflectionSource.Melee));
    }

    [Test]
    public void AnAbilityGrantedDeflectionRestoresFPOnceItsTriggerIsReady()
    {
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Melee;
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestore, 4);
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds, 9);

        _service.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Ranged);
        _log.Should().BeEmpty();
        _service.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Melee);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore} 9",
            $"Attribute {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore}",
            $"RestoreFP {Attacker} 4");

        _log.Clear();
        _triggerReady = false;
        _service.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Melee);
        _log.Should().Equal($"Trigger {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore} 9");

        _log.Clear();
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestore, 0);
        _service.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Melee);
        _log.Should().BeEmpty();
    }

    [TestCase(DeflectionSource.Melee, StatType.MeleeDeflectionFPRestore)]
    [TestCase(DeflectionSource.Ranged, StatType.DeflectionFPRestore)]
    public void AnAbilityUsedDeflectionGrantsItsDeflectionAndItsFPRestoreOfTheSameSource(DeflectionSource source, StatType restoreTarget)
    {
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionFPRestore, 2);
        _deflectionSources[StatType.AbilityUsedAttackDeflection] = source;
        _deflectionSources[StatType.AbilityUsedAttackDeflectionFPRestore] = source;
        _grantedDeflections[StatType.AbilityUsedAttackDeflection] = StatType.RangedDeflection;

        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds,
            StatType.AbilityUsedAttackDeflectionFPRestore);
        _log.Take(2).Should().Equal(
            $"Replace {Attacker} {StatType.RangedDeflection} 12 6 {StatType.AbilityUsedAttackDeflection}",
            $"Replace {Attacker} {restoreTarget} 2 6 {StatType.AbilityUsedAttackDeflectionFPRestore}");
        _log.Should().HaveCount(2, "the granted deflection FP restore is not configured");
    }

    [Test]
    public void AnAbilityUsedDeflectionSkipsAnFPRestoreOfAnotherOrUnknownSource()
    {
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionFPRestore, 2);
        _deflectionSources[StatType.AbilityUsedAttackDeflection] = DeflectionSource.None;
        _deflectionSources[StatType.AbilityUsedAttackDeflectionFPRestore] = DeflectionSource.None;
        _grantedDeflections[StatType.AbilityUsedAttackDeflection] = StatType.RangedDeflection;

        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds,
            StatType.AbilityUsedAttackDeflectionFPRestore);
        _deflectionSources[StatType.AbilityUsedAttackDeflectionFPRestore] = DeflectionSource.Melee;
        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds,
            StatType.AbilityUsedAttackDeflectionFPRestore);
        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        Lines("Replace").Should().HaveCount(3).And.AllBe($"Replace {Attacker} {StatType.RangedDeflection} 12 6 {StatType.AbilityUsedAttackDeflection}");
    }

    [Test]
    public void AnAbilityUsedDeflectionNeedsItsStatsAndAGrantedStat()
    {
        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ARangedAbilityUsedDeflectionAlsoRunsTheGrantedDeflectionRiders()
    {
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        _deflectionSources[StatType.AbilityUsedAttackDeflection] = DeflectionSource.Ranged;
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Ranged;
        _grantedDeflections[StatType.AbilityUsedAttackDeflection] = StatType.RangedDeflection;
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestore, 3);

        _service.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        _log.Should().Equal(
            $"Replace {Attacker} {StatType.RangedDeflection} 12 6 {StatType.AbilityUsedAttackDeflection}",
            $"Trigger {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore} 0",
            $"Attribute {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore}",
            $"RestoreFP {Attacker} 3");
    }

    // ------------------------------------------------------------------ Deflecting Return

    [Test]
    public void DeflectingReturnReflectsACappedShareOfWeaponDamageAsForceAndTellsNearbyPlayers()
    {
        Stat(Defender, StatType.RangedDeflectionReflectionPercent, 50);
        Stat(Defender, StatType.RangedDeflectionReflectionCapPercent, 100);
        _equippedSkill = SkillType.Vibroblade;
        _weaponDamage[Attacker] = 40;
        _weaponDamage[Defender] = 30;
        var reflected = CombatRules.GetRangedDeflectionReflectionAmount(40, 50, 30, 100);
        _triggered = damage => damage - 1;

        _service.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(reflected - 1);
        _log.Should().Equal(
            $"WeaponDamage {Attacker} Rifle False True null False",
            $"WeaponDamage {Defender} Vibroblade False False null False",
            $"Trigger {Defender} {StatType.RangedDeflectionReflectionPercent} 6",
            $"Attribute {Defender} {StatType.RangedDeflectionReflectionPercent}",
            $"Triggered {Defender} {Attacker} {reflected} Force Invalid False null False",
            $"Nearby {Defender} 60");
        _message!(Other).Should().Be(_service.BuildDeflectingReturnCombatLogMessage(Other, Defender, Attacker, reflected - 1));
    }

    [Test]
    public void TheDeflectingReturnLineNamesBothSidesAsTheObserverSeesThem()
    {
        _service.BuildDeflectingReturnCombatLogMessage(Other, Defender, Attacker, 9).Should().Be(
            ColorToken.Combat($"<{Other}:{Defender}>'s Deflecting Return reflects 9 Force damage to <{Other}:{Attacker}>."));
    }

    [Test]
    public void DeflectingReturnNeedsBothCreaturesARateDamageAReadyTriggerAndALandedReflection()
    {
        _invalid.Add(Gone);
        _service.ApplyRangedDeflectionReflection(Gone, Attacker, SkillType.Rifle).Should().Be(0);
        _service.ApplyRangedDeflectionReflection(Defender, Gone, SkillType.Rifle).Should().Be(0);
        _service.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        _log.Should().BeEmpty();

        Stat(Defender, StatType.RangedDeflectionReflectionPercent, 50);
        Stat(Defender, StatType.RangedDeflectionReflectionCapPercent, 100);
        _service.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        Lines("Trigger").Should().BeEmpty("no weapon damage, nothing to reflect");

        _weaponDamage[Attacker] = 40;
        _weaponDamage[Defender] = 30;
        _triggerReady = false;
        _service.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        Lines("Triggered").Should().BeEmpty();

        _triggerReady = true;
        _triggered = _ => 0;
        _service.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle).Should().Be(0);
        Lines("Nearby").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ physical-to-Force conversion

    [Test]
    public void TheForceDefenseBlendFollowsTheConversionPercentAndReadsForceDefenseOnlyWhenItConverts()
    {
        var reads = 0;
        int Force()
        {
            reads++;
            return 20;
        }

        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Fire, 100, Force).Should().Be(100);
        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, Force).Should().Be(100);
        reads.Should().Be(0);

        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 25);
        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, Force).Should().Be((100 * 75 + 20 * 25) / 100);
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 150);
        _service.ApplyIncomingPhysicalToForceDefenseConversion(Defender, CombatDamageType.Physical, 100, Force).Should().Be(20);
        reads.Should().Be(2);
    }

    [Test]
    public void TheConvertedShareLeavesThePhysicalHitAndArrivesAsAForceHitOnTheNextFrame()
    {
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 30);
        var physical = 100;
        var portion = CombatRules.GetIncomingPhysicalToForceConversionPortion(100, 30);

        _service.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref physical, -15).Should().Be(portion);
        physical.Should().Be(100 - portion);
        _log.Should().Equal($"Capture {Attacker}");
        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Module);

        ((Action)assigned.Arguments[1]!)();
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(0f);
        ((Action)delayed.Arguments[1]!)();
        _log.Should().Equal($"Capture {Attacker}", "AttributeCaptured", $"Triggered {Attacker} {Defender} {portion} Force Invalid False -15 True");
    }

    [Test]
    public void AProjectileCarriesTheConvertedHitUntilItArrivesAndAGoneCreatureTakesNothing()
    {
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 30);
        var physical = 100;
        Action? carried = null;

        _service.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref physical, 0, action => carried = action)
            .Should().BeGreaterThan(0);
        _engine.CallsTo("AssignCommand").Should().BeEmpty();

        _invalid.Add(Defender);
        carried!();
        _log.Should().Equal($"Capture {Attacker}", "AttributeCaptured");
        _invalid.Clear();
        _invalid.Add(Attacker);
        carried();
        Lines("Triggered").Should().BeEmpty();
    }

    [Test]
    public void NothingConvertsFromANonPhysicalOrEmptyHitOrWithoutAConversion()
    {
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 30);
        var damage = 100;
        _service.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Fire, ref damage).Should().Be(0);
        var none = 0;
        _service.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref none).Should().Be(0);
        Stat(Defender, StatType.IncomingPhysicalToForceConversionPercent, 0);
        _service.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref damage).Should().Be(0);
        damage.Should().Be(100);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ the call that stays on Combat

    [Test]
    public void TheDamageRollRunsOnCombatWithTheSharedRandom()
    {
        Random.SetSeed(21);
        var expected = SWLOR.Game.Server.Service.Combat.CalculateDamage(100, 8, 14, 60, 12, 0);
        Random.SetSeed(21);

        _service.BaseCalculateDamage(100, 8, 14, 60, 12, 0).Should().Be(expected);
    }

    [Test]
    public void TheDamageRollWrapperKeepsTheDefaultsOfCombat()
    {
        var wrapper = typeof(CombatDamageTakenService).GetMethod("CalculateDamage", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = typeof(SWLOR.Game.Server.Service.Combat).GetMethod("CalculateDamage", BindingFlags.Public | BindingFlags.Static)!;

        wrapper.IsVirtual.Should().BeTrue();
        wrapper.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue))
            .Should().Equal(target.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue)));
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheContainerBuildsThePartWithEmptyTables()
    {
        var part = _facades.Get<ICombatDamageTakenService>();
        part.Should().BeOfType<CombatDamageTakenService>();
        part.HasRecentGuardedHit(Defender, 6f).Should().BeFalse();
        part.HasRecentDeflection(Defender, DeflectionSource.Melee, 6f).Should().BeFalse();
        SWLOR.Game.Server.Service.Combat.BaseGuardDamageReductionPercent.Should().Be(CombatDamageTakenService.BaseGuardDamageReductionPercent);
        SWLOR.Game.Server.Service.Combat.MaximumGuardDamageReductionPercent.Should().Be(CombatDamageTakenService.MaximumGuardDamageReductionPercent).And.Be(55);
    }
}
