using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Quests;
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
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The hit effects of <c>Combat</c>: the riders of an auto-attack, of landed damage, of a critical and of a non-critical
/// ability, the effects of defeating an enemy, and triggered damage. Stats come from a table, every other part and service
/// is scripted and writes what it was asked into one log (so a test sees the order), the clock is held still and the shared
/// <c>Random</c> is seeded where a chance is rolled. The damage taken part has not moved: its back edge is overridden.
/// </summary>
[TestFixture]
public class CombatHitEffectsServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Other = 33;
    private const uint Gone = 44;
    private const uint Master = 55;
    private const uint Ally = 66;

    private readonly List<IDisposable> _scopes = new();
    private readonly List<string> _log = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private TestHitEffects _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<(uint, StatType), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    private Dictionary<StatType, int> _temporary = null!;
    private HashSet<(uint, Type)> _statuses = null!;
    private HashSet<(uint, Type, uint)> _sourceStatuses = null!;
    private HashSet<(uint, StatusEffectCategory)> _categories = null!;
    private Dictionary<(uint, Type, uint), IStatusEffect> _effects = null!;
    private Dictionary<uint, CreatureStatusEffect> _creatureEffects = null!;
    private Dictionary<(uint, uint), DateTime> _recent = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _dead = null!;
    private HashSet<uint> _players = null!;
    private HashSet<uint> _nearby = null!;
    private Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    private List<uint> _party = null!;
    private ToxicRushStatusEffect? _toxicRush;
    private uint _master;
    private uint _nearest;
    private bool _hostile;
    private bool _beside;
    private bool _side;
    private bool _sourceApplied;
    private bool _triggerReady;
    private bool _queued;
    private bool _directTrait;
    private int _directBonus;
    private int _typeDealtBonus;
    private int _sunderedCritical;
    private int _resisted;
    private Func<int, int> _taken = null!;

    private sealed class TestHitEffects : CombatHitEffectsService
    {
        private readonly List<string> _log;
        private readonly Func<Func<int, int>> _taken;

        public TestHitEffects(
            List<string> log,
            Func<Func<int, int>> taken,
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatDamageModifiersService damageModifiers,
            ICombatAbilityHitService abilityHit,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IPairedRangedTraitsService pairedRangedTraits,
            ICombatRecorderService combatRecorder,
            ICapstoneProofService capstoneProof,
            IResistanceService resistance,
            IPartyService party)
            : base(queries, resources, attackTiming, suppression, combatActivity, damageModifiers, abilityHit, stat, statusEffect,
                temporaryStatModifier, ability, pairedRangedTraits, combatRecorder, capstoneProof, resistance, party, FacadeForwarder.LazyFor<ICombatDamageTakenService>())
        {
            _log = log;
            _taken = taken;
        }

        protected override int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = NWScript.OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false)
        {
            _log.Add($"Taken {defender} {damage} {attacker} {damageType} {deliveryType} {targetStatusDamagePercentAdjustment} {isLandedAttack} {typedLeadershipReductionAlreadyApplied}");
            return _taken()(damage);
        }

        public int BaseApplyDamageTakenModifiers(uint defender, int damage) => base.ApplyDamageTakenModifiers(defender, damage);
    }

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

    private TestHitEffects BuildService()
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
            })
            .On(nameof(ICombatDamageModifiersService.ApplySameTargetPressureDamageEffects), args => Log($"Pressure {string.Join(" ", args)}"));
        var abilityHit = ScriptedProxy.Create<ICombatAbilityHitService>(out var abilityHitCalls);
        abilityHitCalls.On(nameof(ICombatAbilityHitService.GetCriticalRateAgainstSunderedTargetAdjustment), _ => _sunderedCritical);
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
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
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
            })
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args => Log($"Remove {args[0]} {((Type)args[1]!).Name} {args[2]}"))
            .On(nameof(IStatusEffectService.ExtendStatusEffectDuration), args =>
            {
                _log.Add($"Extend {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]}");
                return true;
            })
            .On(nameof(IStatusEffectService.RefreshStatusEffectDuration), args =>
            {
                _log.Add($"Refresh {args[0]} {((Type)args[1]!).Name} {args[2]} {args[3]} {args[5]}");
                return true;
            })
            .On(nameof(IStatusEffectService.GetStatusEffect), args => args.Length == 1
                ? _toxicRush
                : _effects.GetValueOrDefault(((uint)args[0]!, (Type)args[1]!, (uint)args[2]!)))
            .On(nameof(IStatusEffectService.GetCreatureStatusEffects), args =>
                _creatureEffects.TryGetValue((uint)args[0]!, out var effects) ? effects : new CreatureStatusEffect { StatGroup = new StatGroup() })
            .On(nameof(IStatusEffectService.NotifyDamageStatusEffects), args => Log($"Notify {string.Join(" ", args)}"));
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
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityService.TryQueueTrackedDamageEffect), args =>
            {
                _log.Add($"Queue {args[0]} {args[1]} {args[2]} {args[3]}");
                return _queued;
            })
            .On(nameof(IAbilityService.GetActiveAbilityImpactSummary), _ => null);
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
        var capstone = ScriptedProxy.Create<ICapstoneProofService>(out var capstoneCalls);
        capstoneCalls.On(nameof(ICapstoneProofService.RecordBeastDamage), args => Log($"Beast {args[0]} {args[1]}"));
        var resistance = ScriptedProxy.Create<IResistanceService>(out var resistanceCalls);
        resistanceCalls.On(nameof(IResistanceService.ApplyResistanceToDamage), args =>
        {
            _log.Add($"Resist {args[0]} {args[1]} {args[2]}");
            return (int)args[2]! - _resisted;
        });
        var party = ScriptedProxy.Create<IPartyService>(out var partyCalls);
        partyCalls.On(nameof(IPartyService.GetAllPartyMembersWithinRange), args =>
        {
            _log.Add($"Party {args[0]} {args[1]}");
            return _party;
        });

        return new TestHitEffects(_log, () => _taken, queries, resources, timing, suppression, activity, modifiers, abilityHit, stat,
            statusEffect, temporary, ability, traits, recorder, capstone, resistance, party);
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

    private object? Private(string name, params object[] arguments) =>
        typeof(CombatHitEffectsService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_service, arguments);

    // The rolls the shared Random gives after this seed, so a test can tell how many a call drew.
    private static int[] Rolls(int seed, int count)
    {
        Random.SetSeed(seed);
        var rolls = Enumerable.Range(0, count).Select(_ => Random.D100(1)).ToArray();
        Random.SetSeed(seed);
        return rolls;
    }

    private void RunDelayed()
    {
        foreach (var call in _engine.CallsTo("DelayCommand").Concat(_engine.CallsTo("AssignCommand")).ToArray())
            ((Action)call.Arguments[1]!)();
    }

    // ------------------------------------------------------------------ auto-attack damage

    [Test]
    public void ANonPositiveAutoAttackKeepsItsDamageAndTouchesNothing()
    {
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0).Should().Be(0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, -3, SkillType.Rifle).Should().Be(-3);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackAddsItsStoredProcAndNextAttackBonusesAndTheDirectDamageBonus()
    {
        _temporary[StatType.CurrentAutoAttackDamageBonus] = 5;
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Rifle;
        _temporary[StatType.NextSkillAutoAttackDamageBonus] = 4;
        _temporary[StatType.NextAutoAttackDamageBonus] = 3;
        _directBonus = 2;
        Sources(Attacker, StatType.AutoAttackDamageBonus,
            new Dictionary<StatType, int> { [StatType.AutoAttackDamageBonus] = 7, [StatType.AutoAttackDamageBonusChance] = 100 },
            new Dictionary<StatType, int> { [StatType.AutoAttackDamageBonus] = 9, [StatType.AutoAttackDamageBonusChance] = 0 });

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle).Should().Be(10 + 5 + 7 + 4 + 3 + 2);
        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.CurrentAutoAttackDamageBonus} {StatType.CurrentAutoAttackDamageBonus}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonus} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonusSkillType} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextAutoAttackDamageBonus} {StatType.NextAutoAttackDamageBonus}");
    }

    [Test]
    public void TheNextSkillAutoAttackBonusNeedsItsSkill()
    {
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Pistol;
        _temporary[StatType.NextSkillAutoAttackDamageBonus] = 4;

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle).Should().Be(10, "another skill's bonus");
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10).Should().Be(10, "no skill");
        Lines("Consume").Should().NotContain(line => line.Contains(StatType.NextSkillAutoAttackDamageBonus + " "));
    }

    [Test]
    public void TheSuppressedAutoAttackConsumesTheBonusesItWouldHaveAdded()
    {
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Rifle;
        _service.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle);

        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.CurrentAutoAttackDamageBonus} {StatType.CurrentAutoAttackDamageBonus}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonus} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonusSkillType} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextAutoAttackDamageBonus} {StatType.NextAutoAttackDamageBonus}");
    }

    [Test]
    public void AnAutoAttackRestoresStaminaAndFPInTheirOrder()
    {
        Stat(Attacker, StatType.AutoAttackHitStaminaRestore, 1);
        Stat(Attacker, StatType.AutoAttackStaminaRestoreChance, 100);
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 2);
        Stat(Attacker, StatType.AutoAttackFPRestore, 3);
        Stat(Attacker, StatType.AutoAttackFPRestoreCooldownSeconds, 6);
        Stat(Attacker, StatType.SkillAutoAttackFPRestore, 4);
        Stat(Attacker, StatType.SkillAutoAttackFPRestoreSkillType, (int)SkillType.Rifle);

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("FP") || line.StartsWith("Trigger")).Should().Equal(
            $"HitStamina {Attacker} {StatType.AutoAttackHitStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.AutoAttackStaminaRestore} 2",
            $"Trigger {Attacker} {StatType.AutoAttackFPRestore} 6",
            $"FP {Attacker} {StatType.AutoAttackFPRestore} 3",
            $"FP {Attacker} {StatType.SkillAutoAttackFPRestore} 4");

        _log.Clear();
        _triggerReady = false;
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Pistol);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("FP")).Should().Equal(
            $"HitStamina {Attacker} {StatType.AutoAttackHitStaminaRestore} 1");
    }

    [Test]
    public void AnAutoAttackRestoresItsMastersStaminaAndFPOnTheirChances()
    {
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestoreChance, 100);
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 2);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 100);
        Stat(Attacker, StatType.AutoAttackMasterFPRestore, 3);

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Should().NotContain(line => line.StartsWith("Stamina") || line.StartsWith("FP"), "no master");

        _master = Master;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Where(line => line.StartsWith("Stamina") || line.StartsWith("FP")).Should().Equal(
            $"Stamina {Master} {StatType.AutoAttackMasterStaminaRestore} 2",
            $"FP {Master} {StatType.AutoAttackMasterFPRestore} 3");

        _log.Clear();
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 0);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Should().NotContain(line => line.StartsWith("Stamina") || line.StartsWith("FP"));
    }

    [Test]
    public void TheAutoAttackChancesDrawInTheirOrderAndOnlyWhenConfigured()
    {
        // Proc bonus, stamina, the master's stamina and FP, then the accuracy penalty: one roll each, in this order.
        Sources(Attacker, StatType.AutoAttackDamageBonus, new Dictionary<StatType, int> { [StatType.AutoAttackDamageBonus] = 7, [StatType.AutoAttackDamageBonusChance] = 50 });
        Stat(Attacker, StatType.AutoAttackStaminaRestoreChance, 50);
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 2);
        _master = Master;
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestoreChance, 50);
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 2);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 50);
        Stat(Attacker, StatType.AutoAttackMasterFPRestore, 3);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 50);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, -10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);

        for (var seed = 1; seed <= 20; seed++)
        {
            _log.Clear();
            var rolls = Rolls(seed, 6);
            var damage = _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
            Random.D100(1).Should().Be(rolls[5], "five chances drew five rolls");
            damage.Should().Be(rolls[0] <= 50 ? 17 : 10);
            _log.Contains($"HitStamina {Attacker} {StatType.AutoAttackStaminaRestore} 2").Should().Be(rolls[1] <= 50);
            _log.Contains($"Stamina {Master} {StatType.AutoAttackMasterStaminaRestore} 2").Should().Be(rolls[2] <= 50);
            _log.Contains($"FP {Master} {StatType.AutoAttackMasterFPRestore} 3").Should().Be(rolls[3] <= 50);
            _log.Contains($"Replace {Defender} {StatType.AccuracyPercentAdjustment} -10 6 {StatType.AutoAttackTargetAccuracyPercentAdjustment}")
                .Should().Be(rolls[4] <= 50);
        }

        _stats.Clear();
        _sources.Clear();
        var none = Rolls(2, 1);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Random.D100(1).Should().Be(none[0], "no configured chance draws nothing");
    }

    [Test]
    public void AnAutoAttackCanLowerTheTargetsAccuracyOnlyWithEveryValue()
    {
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 100);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, -10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("Replace").Should().Equal($"Replace {Defender} {StatType.AccuracyPercentAdjustment} -10 6 {StatType.AutoAttackTargetAccuracyPercentAdjustment}");

        _log.Clear();
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackHamstringsAValidTargetOfTheConfiguredSkill()
    {
        Stat(Attacker, StatType.AutoAttackHamstringDurationSeconds, 4);
        Stat(Attacker, StatType.AutoAttackHamstringSkillType, (int)SkillType.Rifle);

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(HamstringStatusEffect)} 4 {CombatDamageType.Physical}");

        _log.Clear();
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Pistol);
        _invalid.Add(Gone);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Rifle);
        Stat(Attacker, StatType.AutoAttackHamstringDurationSeconds, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackOnASunderedTargetRestoresFP()
    {
        Stat(Attacker, StatType.AutoAttackSunderedTargetFPRestore, 3);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().BeEmpty("the target is not sundered");

        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().Equal($"FP {Attacker} {StatType.AutoAttackSunderedTargetFPRestore} 3");

        _log.Clear();
        _invalid.Add(Defender);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusStackInfectsThenAddsAStackAndRefreshes()
    {
        Stat(Attacker, StatType.SourceStatusStackRequiredCategory, (int)StatusEffectCategory.Venom);
        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Infection);
        Stat(Attacker, StatType.SourceStatusStackMaximum, 3);
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 12);
        _sourceApplied = true;

        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(InfectionStatusEffect)} stacks 1 12 {CombatDamageType.Poison}");

        _log.Clear();
        var existing = new InfectionStatusEffect(1);
        _effects[(Defender, typeof(InfectionStatusEffect), Attacker)] = existing;
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        existing.Stacks.Should().Be(2);
        Lines("Refresh").Should().Equal($"Refresh {Defender} {nameof(InfectionStatusEffect)} {Attacker} 12 {CombatDamageType.Poison}");
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusStackNeedsEveryValueAndAnInfectionCategory()
    {
        _sourceApplied = true;
        Stat(Attacker, StatType.SourceStatusStackRequiredCategory, (int)StatusEffectCategory.Venom);
        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusStackMaximum, 3);
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 12);
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        _log.Should().BeEmpty("only Infection stacks");

        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Infection);
        _sourceApplied = false;
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        _sourceApplied = true;
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 0);
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ auto-attack cycles

    [Test]
    public void TheCycleWithoutARadiusAddsItsDamageOnTheAttackThatCompletesIt()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 3);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 12);
        Stat(Attacker, StatType.AutoAttackCycleDamageSkillType, (int)SkillType.Vibroblade);

        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(12);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "the cycle starts again");
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle).Should().Be(0, "another skill does not advance it");

        _invalid.Add(Gone);
        _service.ConsumeAutoAttackCycleDamageBonus(Gone, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Invalid).Should().Be(0);

        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 5);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "a radius cycle hits a nearby enemy instead");
        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 0);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 12);
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
    }

    [Test]
    public void TheMeleeCycleAddsItsDamageForAnyWeaponSkillAlongsideTheSkillCycle()
    {
        Stat(Attacker, StatType.MeleeAutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 7);

        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(12);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Force).Should().Be(0, "Force is not a weapon skill and opens a new skill cycle");
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(7, "the skill cycle completes; the melee cycle restarted");

        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 3);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(5, "only the melee cycle when the skill cycle has a radius");
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(0);
    }

    [Test]
    public void TheRadiusCycleHitsTheNearestOtherEnemyWithTriggeredDamage()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 9);
        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 4);
        _nearest = Other;
        _queued = true;

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().BeEmpty("the first attack only counts");
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _log.Should().ContainInOrder(
            $"Nearest {Attacker} {Defender} 4 {Defender}",
            $"Attribute {Attacker} {StatType.AutoAttackCycleDamage}",
            $"TypeDealt {Attacker} 9 {CombatDamageType.Physical}",
            $"Queue {Attacker} {Other} 9 {CombatDamageType.Physical.GetNWScriptDamageType()}");

        _log.Clear();
        _nearest = NWScript.OBJECT_INVALID;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().ContainSingle();
        Lines("Queue").Should().BeEmpty("no enemy nearby");

        _log.Clear();
        _invalid.Add(Gone);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Vibroblade);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.AutoAttackCycleDamageSkillType, (int)SkillType.Rifle);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusCycleHitsTheAfflictedTargetAndResetsWhenTheStatusIsGone()
    {
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamage, 6);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, (int)CombatDamageType.Fire);
        _sourceApplied = true;
        _queued = true;

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _sourceApplied = false;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _sourceApplied = true;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        Lines("Queue").Should().BeEmpty("losing the status reset the count");
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _log.Should().ContainInOrder(
            $"Attribute {Attacker} {StatType.SourceStatusAutoAttackCycleDamage}",
            $"TypeDealt {Attacker} 6 {CombatDamageType.Fire}",
            $"Queue {Attacker} {Defender} 6 {CombatDamageType.Fire.GetNWScriptDamageType()}");

        _log.Clear();
        _invalid.Add(Gone);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        Lines("Queue").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ landed damage

    private void Hit(int damage, SkillType skill = SkillType.Invalid, CombatDamageType type = CombatDamageType.Physical,
        CombatDamageDeliveryType delivery = CombatDamageDeliveryType.Direct, bool ability = false, bool procs = true) =>
        _service.ApplyDamageDealtEffects(Attacker, Defender, damage, skill, type, delivery, ability, procs);

    [Test]
    public void NoDamageTriggersNothing()
    {
        Hit(0);
        Hit(-5);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TriggeredAndPeriodicDamageOnlyTrackActivity()
    {
        _directTrait = true;
        Hit(10, delivery: CombatDamageDeliveryType.Triggered);
        Hit(10, delivery: CombatDamageDeliveryType.DamageOverTime);
        _log.Should().Equal($"Activity {Attacker}", $"Recent {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");
    }

    [Test]
    public void ADirectHitRunsItsRidersInOrder()
    {
        _stats[(Attacker, StatType.DamageDealtHPPercentRestore)] = 5;
        _stats[(Attacker, StatType.PhysicalDamageDealtHPPercentRestore)] = 6;

        Hit(10, SkillType.Rifle);
        _log.Should().ContainInOrder(
            $"Beast {Attacker} {Defender}",
            $"Activity {Attacker}",
            $"Recent {Attacker} {Defender}",
            $"FirstAttack {Attacker}",
            $"Pressure {Attacker} {Defender} {SkillType.Rifle}",
            $"AutoSuppression {Attacker} {Defender} {SkillType.Rifle} {CombatDamageType.Physical}",
            $"Consume {Attacker} {StatType.NextDamageDealtBleedDurationSeconds} {StatType.NextDamageDealtBleedDurationSeconds}",
            $"RangedSuppression {Attacker} {Defender} {SkillType.Rifle} {CombatDamageType.Physical}",
            $"Heal {Attacker} 10 5 False 0",
            $"Heal {Attacker} 10 6 False 0");

        _log.Clear();
        Hit(10, SkillType.Force, CombatDamageType.Force, ability: true);
        Lines("AutoSuppression").Should().BeEmpty("an ability does not stack auto-attack Suppression");
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 5 False 0");
    }

    [Test]
    public void AnAbilityImpactAlreadyDamagedThisTargetRunsNoSecondSetOfRiders()
    {
        var weapon = new WeaponShotProfile(501, SkillType.Pistol, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        var activation = new WeaponImpactActivation();
        using var profile = new WeaponImpactProfile(Attacker, weapon, activation);

        Hit(10, SkillType.Pistol, ability: true);
        Lines("FirstAttack").Should().ContainSingle();
        _log.Clear();
        Hit(10, SkillType.Pistol, ability: true);
        _log.Should().Equal($"Beast {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");
        _log.Clear();
        Hit(10, SkillType.Pistol);
        Lines("FirstAttack").Should().ContainSingle("an auto-attack is not limited by the ability's impact");
    }

    [Test]
    public void ADirectHostileHitIsRecordedForTheTargetsTraitAfterNativeMitigation()
    {
        _directTrait = true;
        Hit(10, SkillType.Rifle);
        Lines("DirectHostile").Should().Equal($"DirectHostile {Defender}");

        _log.Clear();
        _hostile = false;
        Hit(10, SkillType.Rifle);
        _hostile = true;
        _directTrait = false;
        Hit(10, SkillType.Rifle);
        _service.ApplyDamageDealtEffects(Defender, Defender, 10, SkillType.Rifle);
        _invalid.Add(Gone);
        _service.ApplyDamageDealtEffects(Gone, Defender, 10, SkillType.Rifle);
        _directTrait = true;
        _service.ApplyDamageDealtEffects(Attacker, Gone, 10, SkillType.Rifle);
        Lines("DirectHostile").Should().BeEmpty();

        // A physical auto-attack already passed native mitigation; anything else is probed in the engine.
        Private("HasPositiveDirectDamageAfterNativeMitigation", Attacker, Defender, 5, CombatDamageType.Physical, false, SkillType.Rifle)
            .Should().Be(true);
        Private("HasPositiveDirectDamageAfterNativeMitigation", Attacker, Defender, 0, CombatDamageType.Physical, false, SkillType.Rifle)
            .Should().Be(false);
    }

    [Test]
    public void AnAutoAttackSplashHitsTheTargetAndItsNeighboursOnTheNextFrame()
    {
        Sources(Attacker, StatType.AutoAttackSplashDamage,
            new Dictionary<StatType, int>
            {
                [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 100,
                [StatType.AutoAttackSplashRadiusMeters] = 4, [StatType.AutoAttackSplashMaximumTargets] = 3
            },
            new Dictionary<StatType, int> { [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 0 });
        _nearby.Add(Other);
        _nearby.Add(Ally);
        _queued = true;

        Hit(10, SkillType.Vibroblade);
        Lines("Queue").Should().BeEmpty("the splash waits a frame");
        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(0f);

        _log.Clear();
        _dead.Add(Ally);
        RunDelayed();
        _log.Should().ContainInOrder(
            $"Attribute {Attacker} {StatType.AutoAttackSplashDamage}",
            $"Queue {Attacker} {Defender} 8 {CombatDamageType.Physical.GetNWScriptDamageType()}",
            $"Queue {Attacker} {Other} 8 {CombatDamageType.Physical.GetNWScriptDamageType()}");
        Lines("Queue").Should().HaveCount(2, "a dead neighbour takes no damage");
        _engine.CallsTo("ApplyEffectAtLocation").Should().ContainSingle();
    }

    [Test]
    public void ASplashSkipsADeadTargetAGoneAttackerAFriendAndAnAbility()
    {
        Sources(Attacker, StatType.AutoAttackSplashDamage, new Dictionary<StatType, int>
        {
            [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 100,
            [StatType.AutoAttackSplashRadiusMeters] = 4, [StatType.AutoAttackSplashMaximumTargets] = 2
        });
        _queued = true;

        Hit(10, SkillType.Vibroblade);
        _dead.Add(Defender);
        _engine.ClearCalls();
        Hit(10, SkillType.Vibroblade);
        RunDelayed();
        Lines("Queue").Should().BeEmpty("the target died before the splash");

        _engine.ClearCalls();
        _dead.Clear();
        Hit(10, SkillType.Vibroblade);
        _invalid.Add(Attacker);
        RunDelayed();
        Lines("Queue").Should().BeEmpty("the attacker is gone");
        _engine.CallsTo("ApplyEffectAtLocation").Should().BeEmpty();

        _invalid.Clear();
        _engine.ClearCalls();
        _hostile = false;
        Hit(10, SkillType.Vibroblade);
        _hostile = true;
        Hit(10, SkillType.Vibroblade, ability: true);
        Hit(10, SkillType.Force);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        _invalid.Add(Defender);
        Hit(10, SkillType.Vibroblade);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void APredatorsMarkIsPlacedByAnAbilityAndFollowedUpByAnyHit()
    {
        Stat(Attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent, 10);
        Stat(Attacker, StatType.PredatorsMarkDurationSeconds, 12);
        Hit(10);
        Lines("Apply").Should().BeEmpty("an auto-attack does not place the mark");
        Hit(10, ability: true);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(PredatorsMark1StatusEffect)} mark 12 {ResistanceType.Trauma}");

        _log.Clear();
        _sourceStatuses.Add((Defender, typeof(PredatorsMark1StatusEffect), Attacker));
        Stat(Attacker, StatType.PredatorsMarkHastePercentPerStack, 5);
        Stat(Attacker, StatType.PredatorsMarkAbilityHitChancePercentPerStack, 4);
        Stat(Attacker, StatType.PredatorsMarkFollowUpDurationSeconds, 8);
        Stat(Attacker, StatType.PredatorsMarkFollowUpMaximumStacks, 3);
        Hit(10);
        Lines("AddCapped").Should().Equal(
            $"AddCapped {Attacker} {StatType.AttackDelayReductionPercent} 5 8 15 {StatType.PredatorsMarkHastePercentPerStack} 1 False",
            $"AddCapped {Attacker} {StatType.AbilityHitChancePercentAdjustment} 4 8 12 {StatType.PredatorsMarkAbilityHitChancePercentPerStack} 1 False");
        Lines("Replace").Should().Contain($"Replace {Attacker} {StatType.AbilityHitChancePercentAdjustmentSkillType} {(int)SkillType.BeastMastery} 8 {StatType.PredatorsMarkAbilityHitChancePercentPerStack}");
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void APredatorsMarkNeedsLivingCreaturesAndItsValues()
    {
        Stat(Attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent, 10);
        _dead.Add(Defender);
        Hit(10, ability: true);
        _dead.Clear();
        Hit(10, ability: true);
        Lines("Apply").Should().BeEmpty("no duration");

        _sourceStatuses.Add((Defender, typeof(PredatorsMark1StatusEffect), Attacker));
        Stat(Attacker, StatType.PredatorsMarkFollowUpDurationSeconds, 8);
        Stat(Attacker, StatType.PredatorsMarkFollowUpMaximumStacks, 3);
        Hit(10);
        Lines("AddCapped").Should().BeEmpty("no haste or hit chance");
        Stat(Attacker, StatType.PredatorsMarkFollowUpMaximumStacks, 0);
        Stat(Attacker, StatType.PredatorsMarkHastePercentPerStack, 5);
        Hit(10);
        _dead.Add(Attacker);
        Hit(10);
        Lines("AddCapped").Should().BeEmpty();
    }

    [Test]
    public void ASideHitRestoresStaminaOnItsCooldownAndHastensTheAttacker()
    {
        _side = true;
        Stat(Attacker, StatType.SideAttackStaminaRestore, 2);
        Stat(Attacker, StatType.SideAttackStaminaRestoreCooldownSeconds, 6);
        Stat(Attacker, StatType.SideAttackDelayReductionPercent, 10);
        Stat(Attacker, StatType.SideAttackDelayReductionDurationSeconds, 4);

        Hit(10, SkillType.Vibroblade);
        _log.Should().ContainInOrder(
            $"Trigger {Attacker} {StatType.SideAttackStaminaRestore} 6",
            $"HitStamina {Attacker} {StatType.SideAttackStaminaRestore} 2",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 10 4 {StatType.SideAttackDelayReductionPercent}");

        _log.Clear();
        _triggerReady = false;
        Stat(Attacker, StatType.SideAttackDelayReductionDurationSeconds, 0);
        Hit(10, SkillType.Vibroblade);
        Lines("HitStamina").Should().BeEmpty();
        Lines("Replace").Should().BeEmpty();
        _side = false;
        Hit(10, SkillType.Vibroblade);
        Lines("Trigger").Should().ContainSingle();
        Private("ApplySideAttackDamageEffects", Attacker, Defender, SkillType.Vibroblade, 0);
        Lines("Trigger").Should().ContainSingle("no damage");
    }

    [Test]
    public void LandedDamageRestoresStaminaAndHastensForTheConfiguredSkill()
    {
        Stat(Attacker, StatType.DamageDealtStaminaRestore, 3);
        Stat(Attacker, StatType.DamageDealtStaminaRestoreCooldownSeconds, 5);
        Stat(Attacker, StatType.DamageDealtStaminaRestoreSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionPercent, 8);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds, 6);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionSkillType, (int)SkillType.Rifle);

        Hit(10, SkillType.Rifle);
        _log.Should().ContainInOrder(
            $"Trigger {Attacker} {StatType.DamageDealtStaminaRestore} 5",
            $"HitStamina {Attacker} {StatType.DamageDealtStaminaRestore} 3",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 8 6 {StatType.DamageDealtAttackDelayReductionPercent}");

        _log.Clear();
        Hit(10, SkillType.Pistol);
        _triggerReady = false;
        Hit(10, SkillType.Rifle);
        Lines("HitStamina").Should().BeEmpty();
        Lines("Replace").Should().ContainSingle("the haste still applies to the Rifle hit");
        _log.Clear();
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds, 0);
        Hit(10, SkillType.Rifle);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds, 6);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionPercent, 0);
        Stat(Attacker, StatType.DamageDealtStaminaRestore, 0);
        Hit(10, SkillType.Rifle);
        Lines("Replace").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle("no stamina to restore skips the cooldown");
    }

    [Test]
    public void ForceErosionAppliesOnADirectHitWithADuration()
    {
        Stat(Attacker, StatType.DamageDealtForceErosionDurationSeconds, 9);
        Stat(Attacker, StatType.DamageDealtForceErosionFPLossPerTick, 1);
        Stat(Attacker, StatType.DamageDealtForceErosionStaminaLossPerTick, 2);
        Hit(10);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(ForceErosionStatusEffect)} erosion 9 {CombatDamageType.Physical}");

        _log.Clear();
        Private("ApplyDamageDealtForceErosionEffect", Attacker, Defender, CombatDamageDeliveryType.Triggered);
        Stat(Attacker, StatType.DamageDealtForceErosionDurationSeconds, 0);
        Hit(10);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void TheDamageDealtHamstringRollsItsChanceForTheConfiguredSkill()
    {
        Stat(Attacker, StatType.DamageDealtHamstringChance, 100);
        Stat(Attacker, StatType.DamageDealtHamstringDurationSeconds, 5);
        Stat(Attacker, StatType.DamageDealtHamstringSkillType, (int)SkillType.Spear);
        Hit(10, SkillType.Spear, CombatDamageType.Fire);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(HamstringStatusEffect)} 5 {CombatDamageType.Fire}");

        _log.Clear();
        Hit(10, SkillType.Rifle);
        Stat(Attacker, StatType.DamageDealtHamstringDurationSeconds, 0);
        Hit(10, SkillType.Spear);
        Lines("Apply").Should().BeEmpty();
        Stat(Attacker, StatType.DamageDealtHamstringDurationSeconds, 5);
        Stat(Attacker, StatType.DamageDealtHamstringChance, 1);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > 1);
        Rolls(seed, 1);
        Hit(10, SkillType.Spear);
        Lines("Apply").Should().BeEmpty("the roll missed");
        Stat(Attacker, StatType.DamageDealtHamstringChance, 100);
        Private("ApplyDamageDealtHamstringEffect", Attacker, Gone, SkillType.Spear, CombatDamageType.Physical);
        _invalid.Add(Gone);
        Private("ApplyDamageDealtHamstringEffect", Attacker, Gone, SkillType.Spear, CombatDamageType.Physical);
        Lines("Apply").Should().ContainSingle(line => line.Contains($">{Gone} "), "only the valid target is hamstrung");
    }

    [Test]
    public void EachMimicryProcRollsInTheTablesOrderAndUsesItsDuration()
    {
        Stat(Attacker, StatType.DamageDealtBleedChance, 100);
        Stat(Attacker, StatType.DamageDealtShockChance, 100);
        Stat(Attacker, StatType.DamageDealtPoisonChance, 100);
        Stat(Attacker, StatType.DamageDealtPoisonDurationSeconds, 30);

        Hit(10);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} {nameof(BleedStatusEffect)} 12 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} {nameof(ShockStatusEffect)} 10 {CombatDamageType.Electrical}",
            $"Apply {Attacker}>{Defender} {nameof(PoisonStatusEffect)} 30 {CombatDamageType.Poison}");

        _log.Clear();
        Hit(10, procs: false);
        Stat(Attacker, StatType.DamageDealtPoisonDurationSeconds, 0);
        Lines("Apply").Should().BeEmpty("the hit carries no procs");
        Hit(10);
        Lines("Apply").Should().Contain($"Apply {Attacker}>{Defender} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Poison}");
        _log.Clear();
        Private("ApplyDamageDealtMimicryTraitProcs", Attacker, Gone);
        _invalid.Add(Gone);
        Private("ApplyDamageDealtMimicryTraitProcs", Attacker, Gone);
        Lines("Apply").Should().HaveCount(3, "only the valid target");
    }

    [Test]
    public void TheMimicryProcTableKeepsItsOrderDurationsAndDamageTypes()
    {
        var table = typeof(CombatHitEffectsService).GetField("MimicryTraitProcs", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(_service)!;
        var rows = ((System.Collections.IEnumerable)table).Cast<object>().Select(row => row.ToString()).ToArray();
        rows.Should().Equal(
            $"({StatType.DamageDealtBleedChance}, {typeof(BleedStatusEffect)}, {CombatDamageType.Physical}, 12, {StatType.Invalid})",
            $"({StatType.DamageDealtFreezingChance}, {typeof(FreezingStatusEffect)}, {CombatDamageType.Ice}, 6, {StatType.Invalid})",
            $"({StatType.DamageDealtShockChance}, {typeof(ShockStatusEffect)}, {CombatDamageType.Electrical}, 10, {StatType.Invalid})",
            $"({StatType.DamageDealtSunderChance}, {typeof(SunderStatusEffect)}, {CombatDamageType.Physical}, 14, {StatType.Invalid})",
            $"({StatType.DamageDealtHemorrhageChance}, {typeof(HemorrhageStatusEffect)}, {CombatDamageType.Physical}, 12, {StatType.Invalid})",
            $"({StatType.DamageDealtPoisonChance}, {typeof(PoisonStatusEffect)}, {CombatDamageType.Poison}, 12, {StatType.DamageDealtPoisonDurationSeconds})");
    }

    [Test]
    public void TheMimicryProcsDrawOneRollPerConfiguredChanceInTableOrder()
    {
        Stat(Attacker, StatType.DamageDealtFreezingChance, 50);
        Stat(Attacker, StatType.DamageDealtHemorrhageChance, 50);
        for (var seed = 1; seed <= 10; seed++)
        {
            _log.Clear();
            var rolls = Rolls(seed, 3);
            Private("ApplyDamageDealtMimicryTraitProcs", Attacker, Defender);
            Random.D100(1).Should().Be(rolls[2], "two chances drew two rolls");
            Lines("Apply").Should().Equal(new[]
            {
                rolls[0] <= 50 ? $"Apply {Attacker}>{Defender} {nameof(FreezingStatusEffect)} 6 {CombatDamageType.Ice}" : null,
                rolls[1] <= 50 ? $"Apply {Attacker}>{Defender} {nameof(HemorrhageStatusEffect)} 12 {CombatDamageType.Physical}" : null,
            }.Where(line => line != null));
        }
    }

    [Test]
    public void TheNextDamageDealtBleedIsConsumedByTheHit()
    {
        _temporary[StatType.NextDamageDealtBleedDurationSeconds] = 6;
        Hit(10, type: CombatDamageType.Fire);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(BleedStatusEffect)} 6 {CombatDamageType.Fire}");
        _log.Clear();
        Hit(10);
        Lines("Apply").Should().BeEmpty("consumed");
    }

    [Test]
    public void HittingABleedingTargetRestoresStaminaThroughItsThreeChannels()
    {
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestoreChance, 100);
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreChance, 100);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestore, 2);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreCooldownSeconds, 4);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreSkillType, (int)SkillType.Katar);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreChance, 100);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestore, 3);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds, 5);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType, (int)SkillType.Katar);

        Hit(10, SkillType.Katar, ability: true);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("Trigger")).Should().Equal(
            $"Trigger {Attacker} {StatType.DamageDealtBleedingTargetStaminaRestore} 0",
            $"HitStamina {Attacker} {StatType.DamageDealtBleedingTargetStaminaRestore} 1",
            $"Trigger {Attacker} {StatType.SkillDamageBleedingTargetStaminaRestore} 4",
            $"HitStamina {Attacker} {StatType.SkillDamageBleedingTargetStaminaRestore} 2",
            $"Trigger {Attacker} {StatType.SkillAbilityBleedingTargetStaminaRestore} 5",
            $"HitStamina {Attacker} {StatType.SkillAbilityBleedingTargetStaminaRestore} 3");

        _log.Clear();
        Hit(10, SkillType.Rifle);
        Lines("HitStamina").Should().Equal(
            $"HitStamina {Attacker} {StatType.DamageDealtBleedingTargetStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.SkillDamageBleedingTargetStaminaRestore} 2");

        _log.Clear();
        _triggerReady = false;
        Hit(10, SkillType.Katar, ability: true);
        Lines("HitStamina").Should().BeEmpty("every channel is on cooldown");
        _triggerReady = true;
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestore, 0);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreChance, 0);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType, (int)SkillType.Rifle);
        Hit(10, SkillType.Katar, ability: true);
        Lines("HitStamina").Should().BeEmpty();
        _categories.Clear();
        _log.Clear();
        Hit(10, SkillType.Katar, ability: true);
        Lines("Trigger").Should().BeEmpty("the target is not bleeding");
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > 1);
        Rolls(seed, 1);
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestoreChance, 1);
        Private("ApplyBleedingTargetStaminaRestore", Attacker, Defender, SkillType.Rifle, false);
        Lines("Trigger").Should().BeEmpty("the roll missed");
        _invalid.Add(Gone);
        Private("ApplyBleedingTargetStaminaRestore", Attacker, Gone, SkillType.Rifle, false);
    }

    [Test]
    public void ToxicRushStacksAgainstAPoisonedTargetAndRestoresStaminaAtItsMaximum()
    {
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        Stat(Attacker, StatType.KatarToxicRushHastePercentPerStack, 4);
        Stat(Attacker, StatType.KatarToxicRushMaximumStacks, 2);
        Stat(Attacker, StatType.KatarToxicRushDurationSeconds, 10);

        Hit(10, SkillType.Katar);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(ToxicRushStatusEffect)} stacks 1 10");
        Lines("HitStamina").Should().BeEmpty();

        _log.Clear();
        _toxicRush = new ToxicRushStatusEffect(2, 4, 0);
        Hit(10, SkillType.Katar);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(ToxicRushStatusEffect)} stacks 2 10");
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.KatarToxicRushMaximumStacks} 2");

        _log.Clear();
        Private("ApplyToxicRushDamageDealtEffects", Attacker, Defender, CombatDamageDeliveryType.DamageOverTime);
        Stat(Attacker, StatType.KatarToxicRushHastePercentPerStack, 0);
        Hit(10, SkillType.Katar);
        Stat(Attacker, StatType.KatarToxicRushAttackPercentPerStack, 3);
        Stat(Attacker, StatType.KatarToxicRushDurationSeconds, 0);
        Hit(10, SkillType.Katar);
        _statuses.Clear();
        Stat(Attacker, StatType.KatarToxicRushDurationSeconds, 10);
        Hit(10, SkillType.Katar);
        _invalid.Add(Gone);
        Private("ApplyToxicRushDamageDealtEffects", Gone, Defender, CombatDamageDeliveryType.Direct);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void HeavyVibrobladeAndLowHPHealingFollowTheirConditions()
    {
        Stat(Attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow, 1);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 7);
        Stat(Attacker, StatType.LowHPDamageDealtHPRestoreThresholdPercent, 50);
        Stat(Attacker, StatType.LowHPDamageDealtHPPercentRestore, 9);
        _hitPoints[Attacker] = (40, 100);

        Hit(10, type: CombatDamageType.Fire);
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 7 False 0", $"Heal {Attacker} 10 9 False 0");

        _log.Clear();
        _hitPoints[Attacker] = (50, 100);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 0);
        Hit(10, type: CombatDamageType.Fire);
        _hitPoints[Attacker] = (0, 0);
        Hit(10, type: CombatDamageType.Fire);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow, 0);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 7);
        Stat(Attacker, StatType.LowHPDamageDealtHPPercentRestore, 0);
        Hit(10, type: CombatDamageType.Fire);
        Lines("Heal").Should().BeEmpty();
    }

    [Test]
    public void FrenzySlashRefreshesTheHasteItAlreadyHas()
    {
        Stat(Attacker, StatType.FrenzySlashHasteRefreshDurationSeconds, 6);
        Hit(10);
        Lines("RefreshModifier").Should().Equal($"RefreshModifier {Attacker} {StatType.AttackDelayReductionPercent} 6 {StatType.AttackDelayReductionPercent}");
    }

    // ------------------------------------------------------------------ criticals

    private void Critical(int damage = 20, int rating = 2, bool singleTarget = false, SkillType skill = SkillType.Invalid) =>
        _service.ApplyCriticalHitEffects(Attacker, Defender, damage, rating, singleTarget, skill);

    [Test]
    public void ANonCriticalDoesNothingAndAZeroDamageCriticalOnlyEarnsLimitedHaste()
    {
        Stat(Attacker, StatType.CriticalHitLimitedHastePercentAdjustment, 20);
        Stat(Attacker, StatType.CriticalHitLimitedHasteDurationSeconds, 6);
        Stat(Attacker, StatType.CriticalHitLimitedHasteAttackCount, 2);
        Stat(Attacker, StatType.CriticalHitLimitedHasteStatusEffectIcon, (int)EffectIconType.Haste);
        Stat(Attacker, StatType.CriticalStaminaRestore, 3);

        Critical(rating: 0);
        _log.Should().BeEmpty();
        Critical(damage: 0);
        _log.Should().Equal($"Apply {Attacker}>{Attacker} {nameof(LimitedHasteStatusEffect)} haste 6");

        _log.Clear();
        Stat(Attacker, StatType.CriticalHitLimitedHasteTriggerSkillType, (int)SkillType.Rifle);
        Critical(damage: 0, skill: SkillType.Pistol);
        Stat(Attacker, StatType.CriticalHitLimitedHasteTriggerSkillType, 0);
        Stat(Attacker, StatType.CriticalHitLimitedHasteStatusEffectIcon, 0);
        Critical(damage: 0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ACriticalRunsItsRidersInOrder()
    {
        _sunderedCritical = 5;
        Stat(Attacker, StatType.CriticalStaminaRestore, 3);
        Stat(Attacker, StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.CriticalStaminaRestoreCooldownSeconds, 8);
        Stat(Attacker, StatType.CriticalPoisonedTargetStaminaRestore, 1);
        Stat(Attacker, StatType.CriticalMarkedTargetStaminaRestore, 2);
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        _sourceStatuses.Add((Defender, typeof(MarkingTossStatusEffect), Attacker));
        Stat(Attacker, StatType.CriticalTargetFPLossPercentOfDamage, 50);
        Stat(Attacker, StatType.CriticalTargetStaminaLossPercentOfDamage, 25);
        Stat(Attacker, StatType.CriticalHPPercentOfDamageRestore, 10);
        Stat(Attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds, 6);
        Stat(Attacker, StatType.CriticalAccuracyPercentAdjustment, 5);
        Stat(Attacker, StatType.CriticalAccuracyDurationSeconds, 6);
        Stat(Attacker, StatType.CriticalTargetEvasionPercentAdjustment, -5);
        Stat(Attacker, StatType.CriticalTargetEvasionDurationSeconds, 6);
        Stat(Attacker, StatType.CriticalTargetDefensePercentAdjustment, -10);
        Stat(Attacker, StatType.CriticalTargetDefenseDurationSeconds, 6);
        Stat(Attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment, -4);
        Stat(Attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds, 3);

        Critical(singleTarget: true, skill: SkillType.Rifle);
        FloatingText().Should().ContainSingle().Which.Should().Contain("Weak Points");
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CriticalStaminaRestore} 8",
            $"HitStamina {Attacker} {StatType.CriticalStaminaRestore} 3",
            $"HitStamina {Attacker} {StatType.CriticalPoisonedTargetStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.CriticalMarkedTargetStaminaRestore} 2",
            $"LoseFP {Defender} {StatType.CriticalTargetFPLossPercentOfDamage} 10",
            $"LoseStamina {Defender} {StatType.CriticalTargetStaminaLossPercentOfDamage} 5",
            $"Trigger {Attacker} {StatType.CriticalHPPercentOfDamageRestore} 6",
            $"Heal {Attacker} 20 10 False 6",
            $"Replace {Attacker} {StatType.AccuracyPercentAdjustment} 5 6 {StatType.CriticalAccuracyPercentAdjustment}",
            $"Replace {Defender} {StatType.EvasionPercentAdjustment} -5 6 {StatType.CriticalTargetEvasionPercentAdjustment}",
            $"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 6 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 3 {CombatDamageType.Physical}");
    }

    [Test]
    public void ACriticalSkipsTheRidersItIsNotConfiguredFor()
    {
        Stat(Attacker, StatType.CriticalStaminaRestore, 3);
        Stat(Attacker, StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.CriticalPoisonedTargetStaminaRestore, 1);
        Stat(Attacker, StatType.CriticalMarkedTargetStaminaRestore, 2);
        Stat(Attacker, StatType.CriticalHPPercentOfDamageRestore, 10);
        Stat(Attacker, StatType.CriticalAccuracyPercentAdjustment, 5);
        Stat(Attacker, StatType.CriticalTargetEvasionPercentAdjustment, -5);
        Stat(Attacker, StatType.CriticalTargetDefensePercentAdjustment, -10);
        Stat(Attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment, -4);
        _triggerReady = false;

        Critical(skill: SkillType.Pistol, singleTarget: true);
        FloatingText().Should().BeEmpty();
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalHPPercentOfDamageRestore} 0");
        _log.Clear();
        _triggerReady = true;
        Critical(skill: SkillType.Rifle);
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.CriticalStaminaRestore} 3");
    }

    [Test]
    public void AStaffCriticalAddsItsDefenseReductionAndTheLongerDuration()
    {
        Stat(Attacker, StatType.CriticalTargetDefensePercentAdjustment, -10);
        Stat(Attacker, StatType.CriticalTargetDefenseDurationSeconds, 4);
        Stat(Attacker, StatType.StaffCriticalTargetDefensePercentAdjustment, -5);
        Stat(Attacker, StatType.StaffCriticalTargetDefenseDurationSeconds, 9);

        Critical(skill: SkillType.Staff);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 9 {CombatDamageType.Physical}");
        _log.Clear();
        Critical(skill: SkillType.Rifle);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 4 {CombatDamageType.Physical}");
    }

    [Test]
    public void ACriticalGrantsTheNextAbilityItsDamageAndDefenseIgnore()
    {
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusPerkType, (int)PerkType.DoubleShot);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonus, 6);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds, 10);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds, 12);
        Stat(Attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment, 25);
        Stat(Attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds, 8);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Rifle);

        Critical(skill: SkillType.Rifle);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CriticalNextAbilityDamageBonus} 12",
            $"NextAbilityDamage {Attacker} {PerkType.DoubleShot} 6 10",
            $"Replace {Attacker} {StatType.NextSkillAbilitySkillType} {(int)SkillType.Rifle} 8 {StatType.NextSkillAbilitySkillType}",
            $"Replace {Attacker} {StatType.NextSkillAbilityDefenseIgnorePercentAdjustment} 25 8 {StatType.NextSkillAbilitySkillType}",
            $"Replace {Attacker} {StatType.NextSkillAbilityRequiredCategory} 0 8 {StatType.NextSkillAbilitySkillType}");

        _log.Clear();
        Critical();
        _log.Should().BeEmpty("the trigger needs its skill, and the defense ignore needs a skill to grant it to");
        _triggerReady = false;
        Stat(Attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds, 0);
        Critical(skill: SkillType.Rifle);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalNextAbilityDamageBonus} 12");
        _log.Clear();
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Pistol);
        Critical(skill: SkillType.Rifle);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonus, 0);
        Critical(skill: SkillType.Rifle);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ACriticalGrantsAnUndelayedNextAutoAttackForItsSkillOrAny()
    {
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds, 4);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds, 9);
        Critical(skill: SkillType.Pistol);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalNextAutoAttackNoDelaySkillType} 9", $"NoDelay {Attacker} 4");

        _log.Clear();
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, (int)SkillType.Pistol);
        Critical(skill: SkillType.Pistol);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalNextAutoAttackNoDelaySkillType} 9", $"NoDelay {Attacker} {SkillType.Pistol} 4");

        _log.Clear();
        _triggerReady = false;
        Critical(skill: SkillType.Pistol);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType, (int)SkillType.Rifle);
        Critical(skill: SkillType.Pistol);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType, 0);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds, 0);
        Critical(skill: SkillType.Pistol);
        Lines("NoDelay").Should().BeEmpty();
    }

    [Test]
    public void ACriticalBesideTheTargetRollsItsStaminaChance()
    {
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestoreChance, 100);
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestore, 2);
        Critical();
        Lines("HitStamina").Should().BeEmpty("not beside the target");
        _beside = true;
        Critical();
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.CriticalSideAttackStaminaRestore} 2");

        _log.Clear();
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestoreChance, 1);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > 1);
        var rolls = Rolls(seed, 2);
        Critical();
        Random.D100(1).Should().Be(rolls[1], "one roll");
        Lines("HitStamina").Should().BeEmpty();
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestore, 0);
        rolls = Rolls(seed, 1);
        Critical();
        Random.D100(1).Should().Be(rolls[0], "nothing to restore draws nothing");
    }

    [Test]
    public void ACriticalExtendsTheAttackersBleedsOnItsCooldown()
    {
        Stat(Attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, 3);
        Stat(Attacker, StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds, 7);
        Critical();
        _log.Should().BeEmpty("the target has none of the attacker's bleeds");

        _sourceStatuses.Add((Defender, typeof(HemorrhageStatusEffect), Attacker));
        Critical();
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CriticalBleedingStatusDurationExtensionSeconds} 7",
            $"Extend {Defender} {nameof(BleedStatusEffect)} {Attacker} 3",
            $"Extend {Defender} {nameof(HemorrhageStatusEffect)} {Attacker} 3");

        _log.Clear();
        _triggerReady = false;
        _sourceStatuses.Add((Defender, typeof(BleedStatusEffect), Attacker));
        Critical();
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalBleedingStatusDurationExtensionSeconds} 7");
        _log.Clear();
        _invalid.Add(Defender);
        Critical();
        _log.Should().BeEmpty();
    }

    [Test]
    public void ACriticalSequenceRestoresStaminaWhenEnoughLandWithinTheWindow()
    {
        Stat(Attacker, StatType.CriticalHitSequenceCountRequired, 3);
        Stat(Attacker, StatType.CriticalHitSequenceWindowSeconds, 5);
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 4);

        Critical();
        _now = _now.AddSeconds(5);
        Critical();
        Lines("HitStamina").Should().BeEmpty();
        _now = _now.AddSeconds(4);
        Critical();
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.CriticalHitSequenceStaminaRestore} 4");

        _log.Clear();
        Critical();
        Critical();
        _now = _now.AddSeconds(6);
        Critical();
        Lines("HitStamina").Should().BeEmpty("the window lapsed, so the sequence started again");
        _service.ForgetHitEffectState(Attacker);
        Critical();
        Critical();
        Lines("HitStamina").Should().BeEmpty("forgetting the attacker cleared its count");
        Critical();
        Lines("HitStamina").Should().ContainSingle();

        _log.Clear();
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 0);
        Critical();
        Critical();
        Critical();
        Lines("HitStamina").Should().BeEmpty();
    }

    [Test]
    public void ACriticalCanHastenAndHideTheAttacker()
    {
        Stat(Attacker, StatType.CriticalHitSelfEvasionPercentAdjustment, 10);
        Stat(Attacker, StatType.CriticalHitSelfEvasionDurationSeconds, 4);
        Stat(Attacker, StatType.CriticalHitSelfHastePercentAdjustment, 15);
        Stat(Attacker, StatType.CriticalHitSelfHasteDurationSeconds, 5);
        Critical();
        Lines("Replace").Should().Equal(
            $"Replace {Attacker} {StatType.EvasionPercentAdjustment} 10 4 {StatType.CriticalHitSelfEvasionPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 15 5 {StatType.CriticalHitSelfHastePercentAdjustment}");
        _log.Clear();
        Stat(Attacker, StatType.CriticalHitSelfEvasionDurationSeconds, 0);
        Stat(Attacker, StatType.CriticalHitSelfHasteDurationSeconds, 0);
        Critical();
        Lines("Replace").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ non-critical ability

    [Test]
    public void ANonCriticalRangedAbilityStacksTheNextOnesCriticalRateAndShowsItsTracker()
    {
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax, 20);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds, 30);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly, 1);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType, (int)EffectIconType.Haste);
        _temporary[StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment] = 10;

        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        _log.Should().Equal(
            $"Replace {Attacker} {StatType.PersistentNextSkillAbilityCriticalRateSkillType} {(int)SkillType.Rifle} 30 {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"Replace {Attacker} {StatType.PersistentNextSkillAbilityCriticalRateRangedOnly} 1 30 {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"AddCapped {Attacker} {StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment} 5 30 20 {StatType.PersistentNextSkillAbilityCriticalRateSkillType} 1 True",
            $"Remove {Attacker} {nameof(CriticalRateStackTrackerStatusEffect)} False",
            $"Apply {Attacker}>{Attacker} {nameof(CriticalRateStackTrackerStatusEffect)} tracker 30");
        FloatingText().Should().ContainSingle().Which.Should().Contain("Next ranged ability +10% Critical Rate");

        _log.Clear();
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType, 0);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly, 0);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        Lines("Remove").Should().ContainSingle();
        Lines("Apply").Should().BeEmpty("no tracker icon");

        _log.Clear();
        _temporary.Remove(StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        Lines("Remove").Should().BeEmpty("nothing stored");
    }

    [Test]
    public void ANonCriticalAbilityNeedsItsSkillAndEveryValue()
    {
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax, 20);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds, 0);

        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Pistol);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Invalid);
        _invalid.Add(Gone);
        _service.ApplyNonCriticalAbilityEffects(Gone, Defender, SkillType.Rifle);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ defeated enemies

    [Test]
    public void DefeatingAnEnemyRestoresHealsAndBuffsInOrder()
    {
        Stat(Attacker, StatType.DefeatedEnemyStaminaRestore, 2);
        Stat(Attacker, StatType.DefeatedEnemyFPRestore, 3);
        Stat(Attacker, StatType.DefeatedEnemyHPPercentRestore, 5);
        Stat(Attacker, StatType.DefeatedEnemyAttackPercentAdjustment, 10);
        Stat(Attacker, StatType.DefeatedEnemyAttackDurationSeconds, 6);
        Stat(Attacker, StatType.DefeatedEnemyAttackDelayReductionPercent, 12);
        Stat(Attacker, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds, 7);
        Stat(Attacker, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment, 8);
        Stat(Attacker, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds, 9);
        _party.AddRange(new[] { Attacker, Ally });

        _service.ApplyDefeatedEnemyEffects(Attacker);
        _log.Should().Equal(
            $"Attribute {Attacker} {CombatRecordSourceKind.OnKill}",
            $"RestoreStamina {Attacker} 2",
            $"RestoreFP {Attacker} 3",
            $"HealMax {Attacker} 5",
            $"Replace {Attacker} {StatType.AttackPercentAdjustment} 10 6 {StatType.DefeatedEnemyAttackPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 12 7 {StatType.DefeatedEnemyAttackDelayReductionPercent}",
            $"Party {Attacker} 5",
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 8 9 {StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment}");

        _log.Clear();
        _dead.Add(Attacker);
        _service.ApplyDefeatedEnemyEffects(Attacker);
        _invalid.Add(Gone);
        _service.ApplyDefeatedEnemyEffects(Gone);
        _log.Should().BeEmpty();
        _dead.Clear();
        _stats.Clear();
        _service.ApplyDefeatedEnemyEffects(Attacker);
        _log.Should().Equal($"Attribute {Attacker} {CombatRecordSourceKind.OnKill}");
    }

    [Test]
    public void SoulAscensionFollowsAKillInsideTheHitPointSpendWindow()
    {
        Stat(Attacker, StatType.HeavyVibrobladeOffenseSoulAscension, 1);
        _service.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().BeEmpty("no hit points were spent");
        _temporary[StatType.HeavyVibrobladeOffenseSoulAscension] = 1;
        _service.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(SoulAscensionStatusEffect)} 30");
        _log.Clear();
        Stat(Attacker, StatType.HeavyVibrobladeOffenseSoulAscension, 0);
        _service.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ADefeatedEnemysPoisonSpreadsFromItsSourceToTheNearestEnemy()
    {
        var effects = new CreatureStatusEffect { StatGroup = new StatGroup() };
        var poison = new PoisonStatusEffect();
        typeof(StatusEffectBase).GetProperty(nameof(StatusEffectBase.Source))!.SetValue(poison, Ally);
        effects.Add(new BleedStatusEffect());
        effects.Add(poison);
        _creatureEffects[Defender] = effects;
        Stat(Ally, StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 6);
        Stat(Ally, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 12);
        _nearest = Other;

        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _log.Should().ContainInOrder(
            $"Nearest {Ally} {Defender} 6 {Defender}",
            $"Apply {Ally}>{Other} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Physical}");

        _log.Clear();
        _invalid.Add(Ally);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 6);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 12);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Physical}");

        _log.Clear();
        _nearest = NWScript.OBJECT_INVALID;
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 0);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _creatureEffects.Clear();
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ADefeatedBleedingEnemysBleedSpreadsToNearbyEnemies()
    {
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 8);
        _nearby.Add(Other);

        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(BleedStatusEffect)} 8 {CombatDamageType.Physical}");

        _log.Clear();
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 0);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _categories.Clear();
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 8);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _invalid.Add(Gone);
        Private("ApplyDefeatedBleedingEnemySpread", Gone, Defender);
        Private("ApplyDefeatedBleedingEnemySpread", Attacker, Gone);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void EveryRecentDamagerOfTheDefeatedGainsCruelMomentumAndStaleRecordsArePruned()
    {
        Stat(Attacker, StatType.CruelMomentum, 1);
        Stat(Other, StatType.CruelMomentum, 1);
        _recent[(Attacker, Defender)] = _now.AddSeconds(-6);
        _recent[(Other, Defender)] = _now.AddSeconds(-7);
        _recent[(Attacker, Ally)] = _now.AddSeconds(-30);

        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CruelMomentum} 10",
            $"FP {Attacker} {StatType.CruelMomentum} 2",
            $"Apply {Attacker}>{Attacker} {nameof(CruelMomentumStatusEffect)} 30");
        _recent.Keys.Should().BeEquivalentTo(new[] { (Attacker, Defender), (Attacker, Ally) }, "the stale record of the defeated is pruned");

        _log.Clear();
        _triggerReady = false;
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CruelMomentum} 10");
        _log.Clear();
        _dead.Add(Attacker);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _dead.Clear();
        Stat(Attacker, StatType.CruelMomentum, 0);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _invalid.Add(Gone);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Gone);
        _invalid.Add(Attacker);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ToxicPoisonSpreadsFromATargetToNearbyEnemies()
    {
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters, 5);
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 9);
        _nearby.Add(Other);

        _service.SpreadPoisonFromTarget(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(PoisonStatusEffect)} 9 {CombatDamageType.Poison}");
        _log.Clear();
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 0);
        _service.SpreadPoisonFromTarget(Attacker, Defender);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ triggered damage

    [Test]
    public void TriggeredDamageRunsTheOutgoingTargetResistanceAndTakenModifiersThenItsEffects()
    {
        _typeDealtBonus = 10;
        Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, 50);
        _resisted = 5;
        _taken = damage => damage - 1;
        _queued = true;

        var (targeted, adjustment) = CombatRules.ApplyTriggeredDamageTargetAdjustment(30, 50, null);
        adjustment.Should().Be(50);

        _service.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical, SkillType.Rifle).Should().Be(targeted - 6);
        _log.Should().Equal(
            $"TypeDealt {Attacker} 20 {CombatDamageType.Physical}",
            $"Resist {Defender} {CombatDamageType.Physical} {targeted}",
            $"Taken {Defender} {targeted - 5} {Attacker} {CombatDamageType.Physical} {CombatDamageDeliveryType.Triggered} 50 True False",
            $"Queue {Attacker} {Defender} {targeted - 6} {CombatDamageType.Physical.GetNWScriptDamageType()}",
            $"Activity {Attacker}",
            $"Recent {Attacker} {Defender}",
            $"Notify {Attacker} {Defender} {targeted - 6} {CombatDamageType.Physical} {CombatDamageDeliveryType.Triggered}");
    }

    [Test]
    public void TriggeredForceDamageReadsTheForceTakenAdjustmentAndAnUnqueuedHitIsAssigned()
    {
        Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, -50);
        Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, 90);

        var (targeted, adjustment) = CombatRules.ApplyTriggeredDamageTargetAdjustment(20, -50, null);

        _service.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Force, typedLeadershipReductionAlreadyApplied: true)
            .Should().Be(targeted);
        Lines("Taken").Should().Equal($"Taken {Defender} {targeted} {Attacker} {CombatDamageType.Force} {CombatDamageDeliveryType.Triggered} {adjustment} True True");
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Attacker);
        RunDelayed();
        _engine.CallsTo("EffectDamage").Should().ContainSingle().Which.Arguments[0].Should().Be(targeted);
        _engine.CallsTo("ApplyEffectToObject").Should().ContainSingle().Which.Arguments[2].Should().Be(Defender);

        _log.Clear();
        _service.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Fire).Should().Be(20, "fire reads no typed target adjustment");
        _service.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Fire, targetStatusDamagePercentAdjustment: 0, outgoingModifiersAlreadyApplied: true)
            .Should().Be(20);
        Lines("TypeDealt").Should().ContainSingle("the second hit's outgoing modifiers were already applied");
        Lines("Taken").Last().Should().EndWith(" 0 True False");
    }

    [Test]
    public void TriggeredDamageThatComesToNothingStopsBeforeItsEffects()
    {
        _service.ApplyTriggeredDamage(Attacker, Defender, 0, CombatDamageType.Physical).Should().Be(0);
        _log.Should().BeEmpty();

        _resisted = 20;
        _service.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical).Should().Be(0);
        Lines("Taken").Should().BeEmpty("resisted entirely");

        _resisted = 0;
        _taken = _ => 0;
        _service.ApplyTriggeredDamage(Attacker, Defender, 20, CombatDamageType.Physical).Should().Be(0);
        Lines("Taken").Should().ContainSingle();
        Lines("Queue").Should().BeEmpty();
        Lines("Notify").Should().BeEmpty();
    }

    [Test]
    public void TheDamageTakenBackEdgeCallsTheUnmovedPartThroughCombat()
    {
        _service.BaseApplyDamageTakenModifiers(Defender, 0).Should().Be(0, "no damage is returned unchanged by the damage taken part");
        _service.BaseApplyDamageTakenModifiers(Defender, -4).Should().Be(-4);
    }

    [Test]
    public void TheDamageTakenBackEdgeKeepsTheDefaultsOfTheMemberItCalls()
    {
        var wrapper = typeof(CombatHitEffectsService).GetMethod("ApplyDamageTakenModifiers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = typeof(ICombatDamageTakenService).GetMethod("ApplyDamageTakenModifiers")!;
        wrapper.IsVirtual.Should().BeTrue();
        wrapper.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue))
            .Should().Equal(target.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue)));
    }

    // ------------------------------------------------------------------ state

    [Test]
    public void ForgettingACreatureClearsEveryTableThatNamesIt()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.MeleeAutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.CriticalHitSequenceCountRequired, 5);
        Stat(Attacker, StatType.CriticalHitSequenceWindowSeconds, 5);
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, (int)CombatDamageType.Fire);
        _sourceApplied = true;
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar);
        Critical();
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Other, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Other, Attacker, 10, SkillType.Katar);
        Stat(Other, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);

        IDictionary Table(string name) => (IDictionary)typeof(CombatHitEffectsService)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;
        Table("_autoAttackCycleCounts").Count.Should().Be(1);
        Table("_meleeAutoAttackCycleCounts").Count.Should().Be(1);
        Table("_criticalHitSequenceStates").Count.Should().Be(1);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(2);

        _service.ForgetHitEffectState(Defender);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(1, "the record naming the defender as the target goes");
        _service.ForgetHitEffectState(Attacker);
        Table("_autoAttackCycleCounts").Count.Should().Be(0);
        Table("_meleeAutoAttackCycleCounts").Count.Should().Be(0);
        Table("_criticalHitSequenceStates").Count.Should().Be(0);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(0);
    }

    [Test]
    public void TheContainerBuildsThePartWithItsInjectedNeighbours()
    {
        _facades.Get<ICombatHitEffectsService>().Should().BeOfType<CombatHitEffectsService>();
    }
}
