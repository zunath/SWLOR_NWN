using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The shared bench of the ability bonus parts of <c>Combat</c> (the stay-part and the queued ability bonus part). The bonuses
/// they store live in a real temporary stat modifier service, the stats in a table, the other parts are scripted, and the
/// clock is held still.
/// </summary>
public abstract class CombatAbilityBonusTestBase
{
    protected const uint Attacker = 11;
    protected const uint Defender = 22;
    protected const uint Gone = 44;
    protected const string PlayerId = "player-ability-bonus";

    protected readonly List<IDisposable> _scopes = new();
    protected FacadeContainer _facades = null!;
    protected FakeNWScript _engine = null!;
    protected TemporaryStatModifierService _modifiers = null!;
    protected InMemoryDBService _db = null!;
    protected CombatAbilityBonusService _service = null!;
    protected CombatQueuedAbilityBonusService _queued = null!;
    protected DateTime _now;
    protected Dictionary<(uint, StatType), int> _stats = null!;
    protected HashSet<(uint, Type)> _statuses = null!;
    protected HashSet<uint> _invalid = null!;
    protected HashSet<uint> _players = null!;
    protected ScriptedProxy _queriesCalls = null!;
    protected ScriptedProxy _resourcesCalls = null!;
    protected ScriptedProxy _activityCalls = null!;
    protected ScriptedProxy _timingCalls = null!;
    protected ScriptedProxy _statusCalls = null!;
    protected ScriptedProxy _enmityCalls = null!;
    protected DateTime _lastCompletedActivity;
    protected bool _recentAttack;
    protected bool _highFP;
    protected bool _abilityAppliedCategory;
    protected (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) _idleBonuses;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // The temporary stat modifiers aggregate through the Stat facade.
        _scopes.Add(_facades.Install());
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _stats = new();
        _statuses = new();
        _invalid = new();
        _players = new();
        _lastCompletedActivity = default;
        _recentAttack = false;
        _highFP = false;
        _abilityAppliedCategory = false;
        _idleBonuses = default;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetObjectUUID", _ => PlayerId);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out _queriesCalls);
        _queriesCalls
            .On(nameof(ICombatQueriesService.GetTargetedAbilityAdjustment), args => (int)(StatType)args[4]! % 7 + 1)
            .On(nameof(ICombatQueriesService.GetRangedAttackDamageFlatAdjustment), _ => 100)
            .On(nameof(ICombatQueriesService.GetRangedAttackDefenseIgnorePercentAdjustment), _ => 200)
            .On(nameof(ICombatQueriesService.GetPhysicalAndForceAbilityHitChanceAdjustment), _ => 1000)
            .On(nameof(ICombatQueriesService.GetAbilitySkillType), args => ((AbilityDetail)args[1]!).SkillType)
            .On(nameof(ICombatQueriesService.AbilityAppliedAnyStatusCategory), _ => _abilityAppliedCategory);
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out _resourcesCalls);
        _resourcesCalls
            .On(nameof(ICombatResourcesService.GetNonCriticalRangedAbilityStaminaCostFlatAdjustment), _ => 300)
            .On(nameof(ICombatResourcesService.IsCurrentFPAtOrAbovePercent), _ => _highFP);
        var activity = ScriptedProxy.Create<ICombatActivityService>(out _activityCalls);
        _activityCalls
            .On(nameof(ICombatActivityService.GetLastCompletedOffensiveActivityAt), _ => _lastCompletedActivity)
            .On(nameof(ICombatActivityService.TrackCombatActivity), _ => null)
            .On(nameof(ICombatActivityService.HasRecentAttackActivity), _ => _recentAttack)
            .On(nameof(ICombatActivityService.GetIdleSkillAbilityBonuses), _ => _idleBonuses);
        var timing = ScriptedProxy.Create<ICombatAttackTimingService>(out _timingCalls);
        _timingCalls
            .On(nameof(ICombatAttackTimingService.GrantNextAbilityDamageBonus), _ => null)
            .On(nameof(ICombatAttackTimingService.GrantNextSkillAbilityBonuses), _ => null);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetNPCStats), _ => new NPCStats { Level = 40 });
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out _statusCalls);
        _statusCalls
            .On(nameof(IStatusEffectService.RemoveStatusEffect), _ => null)
            .On(nameof(IStatusEffectService.HasStatusEffect), args =>
                ((Type[])args[1]!).Any(type => _statuses.Contains(((uint)args[0]!, type))));
        var enmity = ScriptedProxy.Create<IEnmityChangeService>(out _enmityCalls);
        _enmityCalls.On(nameof(IEnmityChangeService.ModifyEnmity), _ => null);
        _modifiers = new TemporaryStatModifierService(FacadeForwarder.For<IGuiService>());
        _db = new InMemoryDBService();

        _service = new CombatAbilityBonusService(queries, resources, activity, timing, stat, statusEffect, _modifiers, enmity, _db);
        _queued = new CombatQueuedAbilityBonusService(queries, activity, stat, statusEffect, _modifiers, _service);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    protected void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    protected void Store(uint creature, StatType stat, int amount, StatType group) => _modifiers.Replace(creature, stat, amount, 30, group);

    protected int Stored(uint creature, StatType stat, StatType group) => _modifiers.GetStatAdjustment(creature, stat, group);

    protected IEnumerable<string> Removed() => _statusCalls.Calls
        .Where(call => call.Member == nameof(IStatusEffectService.RemoveStatusEffect))
        .Select(call => $"{call.Arguments[0]} {((Type)call.Arguments[1]!).Name}");
}
