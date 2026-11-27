using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The scripted neighbours the three damage modifier parts share: stats from a table, the status effects from a scripted
/// status service (also behind the <c>StatusEffect</c> facade, which the melee chain's status uses), the other parts
/// scripted, the temporary modifiers real and the clock held still. The critical rate and repeated-target parts are the
/// real classes on the same neighbours, so the stay-part composes them as the container does.
/// </summary>
public abstract class CombatModifierTestBase
{
    protected const uint Attacker = 11;
    protected const uint Defender = 22;
    protected const uint Other = 33;
    protected const uint Gone = 44;

    protected readonly List<IDisposable> _scopes = new();
    protected readonly List<string> _statusLog = new();
    protected readonly List<IStatusEffect> _applied = new();
    protected readonly List<string> _feedback = new();
    protected readonly List<string> _triggers = new();
    protected FacadeContainer _facades = null!;
    protected IStatusEffectService _statusEffect = null!;
    protected IStatusEffectApplicationService _statusApplication = null!;
    protected FakeNWScript _engine = null!;
    protected TemporaryStatModifierService _modifiers = null!;
    private protected TestDamageModifiers _service = null!;
    protected CombatRateModifiersService _rate = null!;
    protected CombatRepeatedTargetService _repeated = null!;
    protected DateTime _now;
    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected HashSet<(uint, Type)> _statuses = null!;
    protected HashSet<(uint, StatusEffectCategory)> _categories = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _players = null!;
    protected Dictionary<uint, (int Current, int Max)> _hitPoints = null!;
    protected Dictionary<StatType, int> _statusSource = null!;
    protected int _abilityScore;
    protected int _maxFP;
    protected int _currentFP;
    protected bool _sideAttack;
    protected bool _backAttack;
    protected bool _immune;
    protected bool _weaponOrForce;
    protected bool _longRange;
    protected bool _nearby;
    protected bool _area;
    protected bool _singleTarget;
    protected bool _targetHasCategory;
    protected bool _triggerReady;
    protected int _partySource;
    protected int _sourceApplied;
    protected int _abilitySourceApplied;
    protected int _abilitySourceAppliedBonus;
    protected int _skillSelected;
    protected int _highResource;
    protected int _rangedIgnore;
    protected DeflectionSource _deflectionSource;

    [OneTimeSetUp]
    public void BuildFacades()
    {
        // One scripted status service for the fixture, so the melee chain's status (which goes through the StatusEffect
        // facade) and the part see the same statuses; its answers read this fixture's fields.
        _statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.HasStatusEffect), args => args[1] switch
            {
                Type[] types => types.Any(type => _statuses.Contains(((uint)args[0]!, type))),
                Type type => _statuses.Contains(((uint)args[0]!, type)),
                _ => false
            })
            .On(nameof(IStatusEffectService.HasStatusEffectCategory), args =>
                _categories.Contains(((uint)args[0]!, (StatusEffectCategory)args[1]!)))
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args =>
            {
                var type = (Type)args[1]!;
                _statusLog.Add($"Remove {args[0]} {type.Name}" + (args[2] is uint source ? $" from {source}" : string.Empty));
                _statuses.Remove(((uint)args[0]!, type));
                return null;
            });
        _statusApplication = ScriptedProxy.Create<IStatusEffectApplicationService>(out var statusApplicationCalls);
        statusApplicationCalls.On(nameof(IStatusEffectApplicationService.ApplyStatusEffect), args =>
            {
                var type = args[2] as Type ?? args[2]!.GetType();
                if (args[2] is IStatusEffect effect)
                    _applied.Add(effect);
                _statusLog.Add($"Apply {args[0]}>{args[1]} {type.Name} {args[3]}" + (args[4] is CombatDamageType damage ? $" {damage}" : string.Empty));
                _statuses.Add(((uint)args[1]!, type));
                return true;
            });
        _facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton(_statusEffect)).Replace(ServiceDescriptor.Singleton(_statusApplication)));
    }

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // The temporary modifiers aggregate through the Stat facade; the melee chain's status goes through StatusEffect.
        _scopes.Add(_facades.Install());
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _statusLog.Clear();
        _applied.Clear();
        _feedback.Clear();
        _triggers.Clear();
        _stats = new();
        _statuses = new();
        _categories = new();
        _invalid = new();
        _players = new();
        _hitPoints = new();
        _statusSource = new();
        _abilityScore = 0;
        _maxFP = 0;
        _currentFP = 0;
        _sideAttack = _backAttack = _immune = _weaponOrForce = _longRange = _nearby = _area = _singleTarget = false;
        _targetHasCategory = false;
        _triggerReady = true;
        _partySource = _sourceApplied = _abilitySourceApplied = _abilitySourceAppliedBonus = _skillSelected = _highResource = _rangedIgnore = 0;
        _deflectionSource = DeflectionSource.None;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetMaxHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Max : 0);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.TryGetValue((uint)args[0]!, out var hp) ? hp.Current : 0);
        _engine.SetReturnHandler("GetAbilityScore", _ => _abilityScore);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.HasDamageImmunity), _ => _immune)
            .On(nameof(ICombatQueriesService.IsMatchingSideAttack), _ => _sideAttack)
            .On(nameof(ICombatQueriesService.IsMatchingBackAttack), _ => _backAttack)
            .On(nameof(ICombatQueriesService.IsWeaponOrForceDamage), _ => _weaponOrForce)
            .On(nameof(ICombatQueriesService.IsRangedAbilityLongRangeTarget), _ => _longRange)
            .On(nameof(ICombatQueriesService.IsNearbyTargetWithinDistance), args => _nearby && (float)args[2]! == 8f)
            .On(nameof(ICombatQueriesService.TargetHasAnyStatusEffectCategory), _ => _targetHasCategory)
            .On(nameof(ICombatQueriesService.GetRangedAttackDefenseIgnorePercentAdjustment), _ => _rangedIgnore)
            .On(nameof(ICombatQueriesService.GetStatusSourceStatAdjustment), args => _statusSource.GetValueOrDefault((StatType)args[2]!))
            .On(nameof(ICombatQueriesService.GetStatusSourcePartyStatAdjustment), _ => _partySource)
            .On(nameof(ICombatQueriesService.GetDamageToSourceAppliedStatusTargetAdjustment), _ => _sourceApplied)
            .On(nameof(ICombatQueriesService.GetAbilityDamageToSourceAppliedStatusTargetAdjustment), _ => _abilitySourceApplied)
            .On(nameof(ICombatQueriesService.GetAbilityDamageToSourceAppliedStatusTargetBonus), _ => _abilitySourceAppliedBonus)
            .On(nameof(ICombatQueriesService.GetSkillSelectedStatAdjustment), _ => _skillSelected);
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _triggers.Add($"{args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            })
            .On(nameof(ICombatResourcesService.GetHighResourceStatAdjustment), _ => _highResource);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetMaxFP), _ => _maxFP)
            .On(nameof(IStatService.GetCurrentFP), _ => _currentFP)
            .On(nameof(IStatService.GetStatTypeDeflectionSource), _ => _deflectionSource);
        var ability = ScriptedProxy.Create<IAbilityImpactState>(out var abilityCalls);
        abilityCalls
            .On(nameof(IAbilityImpactState.IsAreaImpact), _ => _area)
            .On(nameof(IAbilityImpactState.IsSingleTargetImpact), _ => _singleTarget);
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out var feedbackCalls);
        feedbackCalls.On(nameof(IPlayerFeedbackService.ShowDiagnosticFloatingText), args =>
        {
            _feedback.Add($"{args[1]} {args[0]}");
            return null;
        });
        _modifiers = new TemporaryStatModifierService(FacadeForwarder.For<IGuiService>());

        _rate = new CombatRateModifiersService(queries, stat, _statusEffect, _statusApplication, _modifiers, feedback);
        _repeated = new CombatRepeatedTargetService(stat, _statusEffect, _statusApplication);
        _service = new TestDamageModifiers(queries, resources, stat, _statusEffect, _modifiers, ability, feedback, _rate, _repeated);
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

    protected void HP(uint creature, int current, int max) => _hitPoints[creature] = (current, max);

    // A percent stage's result, with the rule's float rounding (100 + 30% is 131).
    protected static int Pct(int damage, int percent) => CombatRules.ApplyPercentDamageAdjustment(damage, percent);

    protected IEnumerable<string> FloatingText() => _engine.CallsTo("FloatingTextStringOnCreature")
        .Select(call => $"{call.Arguments[1]} {call.Arguments[0]}");

    protected static AbilityDetail Hostile(SkillType skill = SkillType.Rifle) => new() { IsHostileAbility = true, SkillType = skill };
    protected int Dealt(
        out int targetStatus,
        int damage = 100,
        SkillType skill = SkillType.Invalid,
        CombatDamageType type = CombatDamageType.Physical,
        bool isAbility = false,
        bool canRandom = true,
        bool landed = false,
        AbilityDetail ability = null!,
        int abilityPercent = 0,
        int lowHPPercent = 0,
        bool ordinary = false) =>
        _service.ApplyDamageDealtModifiers(Attacker, Defender, damage, skill, type, isAbility, canRandom, landed, ability,
            out targetStatus, abilityPercent, lowHPPercent, ordinary);

}
