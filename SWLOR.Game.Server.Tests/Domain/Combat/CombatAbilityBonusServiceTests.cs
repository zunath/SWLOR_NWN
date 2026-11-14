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
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability and attack bonuses of <c>Combat</c>: the opening auto-attack, the one-shot next-ability and next-attack
/// bonuses, the queued weapon ability snapshots and the adjustments an ability reads from its user's stats. The bonuses it
/// stores live in a real temporary stat modifier service, the stats in a table, the other parts are scripted, and the
/// clock is held still.
/// </summary>
[TestFixture]
public class CombatAbilityBonusServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Gone = 44;
    private const string PlayerId = "player-ability-bonus";

    private readonly List<IDisposable> _scopes = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private TemporaryStatModifierService _modifiers = null!;
    private InMemoryDBService _db = null!;
    private CombatAbilityBonusService _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private HashSet<(uint, Type)> _statuses = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _players = null!;
    private ScriptedProxy _queriesCalls = null!;
    private ScriptedProxy _resourcesCalls = null!;
    private ScriptedProxy _activityCalls = null!;
    private ScriptedProxy _timingCalls = null!;
    private ScriptedProxy _statusCalls = null!;
    private ScriptedProxy _enmityCalls = null!;
    private DateTime _lastCompletedActivity;
    private bool _recentAttack;
    private bool _highFP;
    private bool _abilityAppliedCategory;
    private (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) _idleBonuses;

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
        var enmity = ScriptedProxy.Create<IEnmityService>(out _enmityCalls);
        _enmityCalls.On(nameof(IEnmityService.ModifyEnmity), _ => null);
        _modifiers = new TemporaryStatModifierService(FacadeForwarder.For<IGuiService>());
        _db = new InMemoryDBService();

        _service = new CombatAbilityBonusService(queries, resources, activity, timing, stat, statusEffect, _modifiers, enmity, _db);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private void Store(uint creature, StatType stat, int amount, StatType group) => _modifiers.Replace(creature, stat, amount, 30, group);

    private int Stored(uint creature, StatType stat, StatType group) => _modifiers.GetStatAdjustment(creature, stat, group);

    private IEnumerable<string> Removed() => _statusCalls.Calls
        .Where(call => call.Member == nameof(IStatusEffectService.RemoveStatusEffect))
        .Select(call => $"{call.Arguments[0]} {((Type)call.Arguments[1]!).Name}");

    [Test]
    public void TheOpeningAutoAttackNeedsAValidWeaponAttackerTheRightSkillAndIdleTime()
    {
        _invalid.Add(Gone);
        _service.PrepareOpeningAutoAttack(Gone, SkillType.Rifle).Should().Be(0);
        _service.PrepareOpeningAutoAttack(Attacker, SkillType.Force).Should().Be(0, "not a weapon skill");

        Stat(Attacker, StatType.OpeningAutoAttackSkillType, (int)SkillType.Pistol);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        _service.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(0, "the opener is for pistols");

        Stat(Attacker, StatType.OpeningAutoAttackSkillType, 0);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 0);
        _service.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(0, "no idle time is configured");

        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        _lastCompletedActivity = _now.AddSeconds(-5);
        _service.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(0, "the attacker fought 5 seconds ago");
        _activityCalls.Calls.Should().NotContain(call => call.Member == nameof(ICombatActivityService.TrackCombatActivity));
    }

    [Test]
    public void AnOpeningAutoAttackAfterIdleTimeStoresItsDamageAndCriticalBonusesAndReturnsItsCriticalRate()
    {
        Stat(Attacker, StatType.OpeningAutoAttackSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackDamageBonus, 4);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalDamagePercentAdjustment, 25);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment, 15);
        _lastCompletedActivity = _now.AddSeconds(-11);

        _service.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(15);

        _activityCalls.Calls.Where(call => call.Member == nameof(ICombatActivityService.TrackCombatActivity))
            .Select(call => call.Arguments[0]).Should().Equal(Attacker);
        Removed().Should().Equal($"{Attacker} {nameof(OpeningAttackReadyStatusEffect)}");
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(4);
        _service.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker).Should().Be(25);
        _service.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker).Should().Be(0, "consumed once");

        _now = _now.AddSeconds(7);
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0, "it lasts 6 seconds");
    }

    [Test]
    public void AnOpeningAutoAttackWithNoStoredBonusesStoresNothingAndANeverActiveAttackerQualifies()
    {
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment, 5);

        _service.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(5, "an attacker with no activity is idle");
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0);
        Stored(Attacker, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
            StatType.CurrentAutoAttackCriticalDamagePercentAdjustment).Should().Be(0);
    }

    [Test]
    public void AQueuedWeaponAbilityCapturesTheOpeningBonusesAtActivationAndClearsTheStalePerSwingPayload()
    {
        _invalid.Add(Gone);
        _service.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Gone, SkillType.Rifle);
        _service.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Invalid);
        _activityCalls.Calls.Should().BeEmpty();

        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 9, StatType.QueuedWeaponAbilityCriticalRateSkillType);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackDamageBonus, 4);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalDamagePercentAdjustment, 25);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment, 15);
        // An earlier activation of the same skill this window adds to what this one captures.
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Rifle,
            StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, 2, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, 5,
            StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, 40,
            StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);

        _service.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle);

        Stored(Attacker, StatType.QueuedWeaponAbilityDamageBonus, StatType.QueuedWeaponAbilityCriticalRateSkillType)
            .Should().Be(0, "the stale per-swing payload is discarded first");
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0, "moved to the snapshot");
        _service.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker).Should().Be(0, "moved to the snapshot");
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(6);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, group).Should().Be(20);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(40, "the larger critical damage wins");

        _now = _now.AddSeconds(29);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(6, "the snapshot lasts 30 seconds");
    }

    [Test]
    public void AQueuedActivationWithNothingToCaptureLeavesTheSnapshotAloneAndAnotherSkillReplacesIt()
    {
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Pistol, group);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, 2, group);

        _service.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Pistol, "nothing to capture");

        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalDamagePercentAdjustment, 10);
        _service.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(0, "a pistol snapshot does not carry over");
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(10);
    }

    [Test]
    public void ClearingTheOpeningModifiersConsumesBothCurrentBonusesAndTheAttemptBonuses()
    {
        Store(Attacker, StatType.CurrentAutoAttackDamageBonus, 3, StatType.CurrentAutoAttackDamageBonus);
        Store(Attacker, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment, 30, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
        _service.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 12);

        _service.ClearOpeningAutoAttackModifiers(Attacker);

        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0);
        Stored(Attacker, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment).Should().Be(0);
        _service.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));
    }

    [Test]
    public void TheNextAbilityIsFreeOnlyForTheStoredSkillAndOnlyOnce()
    {
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Invalid).Should().BeFalse();
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeFalse();
        _service.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeFalse();

        Store(Attacker, StatType.NextAbilityNoStaminaCostSkillType, (int)SkillType.Rifle, StatType.NextAbilityNoStaminaCostSkillType);
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Pistol).Should().BeFalse();
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeTrue();
        _service.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeTrue();
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeFalse("consumed");
    }

    [Test]
    public void TheNextSkillAbilityStaminaDiscountsCountOnlyTheSourcesThatMatchAndConsumeOnlyThose()
    {
        // A global discount, a rifle discount, a hostile-only discount and a pistol discount, each its own source.
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -1, 30, "global");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -2, 30, "rifle");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, (int)SkillType.Rifle, 30, "rifle");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -4, 30, "hostile");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly, 1, 30, "hostile");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -8, 30, "pistol");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, (int)SkillType.Pistol, 30, "pistol");

        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle).Should().Be(-3);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true).Should().Be(-7);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Invalid, true).Should().Be(0);

        _service.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle).Should().Be(-3);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true).Should().Be(-4, "the hostile discount stays");
        _service.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Pistol, true).Should().Be(-12);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Pistol, true).Should().Be(0);
    }

    [Test]
    public void TheNextSkillAbilityBonusesGoOnlyToTheStoredSkillAndItsRequiredCategory()
    {
        var group = StatType.NextSkillAbilitySkillType;
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Invalid).Should().Be((0, 0, 0));
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0), "nothing stored");

        Store(Attacker, StatType.NextSkillAbilitySkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.NextSkillAbilityDamageBonus, 6, group);
        Store(Attacker, StatType.NextSkillAbilityCriticalRatePercentAdjustment, 10, group);
        Store(Attacker, StatType.NextSkillAbilityDefenseIgnorePercentAdjustment, 20, group);
        Store(Attacker, StatType.NextSkillAbilityRequiredCategory, (int)StatusEffectCategory.Control, group);

        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Pistol, StatusEffectCategory.Control).Should().Be((0, 0, 0));
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Debuff).Should().Be((0, 0, 0), "it waits for a control ability");
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Control | StatusEffectCategory.Debuff)
            .Should().Be((6, 10, 20));
        Stored(Attacker, StatType.NextSkillAbilityRequiredCategory, group).Should().Be(0);
        Stored(Attacker, StatType.NextSkillAbilitySkillType, group).Should().Be(0);
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be((0, 0, 0));

        Store(Attacker, StatType.NextSkillAbilitySkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.NextSkillAbilityDamageBonus, 3, group);
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((3, 0, 0), "no category is required");
    }

    [Test]
    public void ThePersistentNextAbilityCriticalRateMatchesItsSkillOrAnyRangedSkillAndIsConsumedWithItsTracker()
    {
        var group = StatType.PersistentNextSkillAbilityCriticalRateSkillType;
        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment, 15, group);
        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRateSkillType, (int)SkillType.Pistol, group);

        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(0);
        _service.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(0);
        Removed().Should().BeEmpty();
        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(15);

        _service.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(15);
        Removed().Should().Equal($"{Attacker} {nameof(CriticalRateStackTrackerStatusEffect)}");
        Stored(Attacker, StatType.PersistentNextSkillAbilityCriticalRateSkillType, group).Should().Be(0);
        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(0);

        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment, 8, group);
        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRateRangedOnly, 1, group);
        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "ranged only");
        _service.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(8);
        Stored(Attacker, StatType.PersistentNextSkillAbilityCriticalRateRangedOnly, group).Should().Be(0);
    }

    [Test]
    public void AQueuedAttemptCriticalRateIsStoredForSixSecondsAndAddsToAnAttemptOfTheSameSkill()
    {
        _invalid.Add(Gone);
        _service.StoreQueuedWeaponAbilityCriticalRateBonus(Gone, SkillType.Rifle, 10);
        _service.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Invalid, 10);
        _service.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 0);
        _service.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));

        var group = StatType.QueuedWeaponAbilityCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 2, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, 5, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, 4, group);
        _service.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 10);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, group).Should().Be(14);
        Stored(Attacker, StatType.QueuedWeaponAbilityDamageBonus, group).Should().Be(2);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, group).Should().Be(5);

        _service.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Pistol, 3);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, group).Should().Be(3, "another skill starts over");
        Stored(Attacker, StatType.QueuedWeaponAbilityDamageBonus, group).Should().Be(0);

        _now = _now.AddSeconds(7);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, group).Should().Be(0, "the attempt lasts 6 seconds");
    }

    [Test]
    public void AnActivationCriticalRateIsStoredOnlyWhenPositiveAndReplacesTheSnapshotSkill()
    {
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        _invalid.Add(Gone);
        _service.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Gone, SkillType.Rifle, 10);
        _service.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Invalid, 10);
        _service.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 0);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be(0);

        _service.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 10);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, group).Should().Be(10);
    }

    [Test]
    public void TheIdleBonusesOfAQueuedActivationAreStoredWhenThereAreAny()
    {
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        _invalid.Add(Gone);
        _service.StoreQueuedWeaponAbilityActivationIdleBonuses(Gone, SkillType.Rifle);
        _service.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Invalid);
        _activityCalls.Calls.Should().BeEmpty();
        _service.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be(0, "no idle bonus");

        _idleBonuses = (5, 12, 30);
        _service.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle);
        _activityCalls.Calls.Last().Arguments.Should().Equal(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(5);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, group).Should().Be(12);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(30);
        _service.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(1012);
        _service.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Pistol).Should().Be(1000, "the idle accuracy is the rifle's");

        _service.ClearQueuedWeaponAbilityActivationBonuses(Attacker);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, group).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(0);
    }

    [Test]
    public void TheWeaponAbilityActivationIdleSnapshotKeepsItsValuesOnlyWhenIdleAndClearsTheOldOneFirst()
    {
        _service.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeFalse();
        _service.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 4, 9, 15);
        _service.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeTrue();
        _service.GetWeaponAbilityActivationIdleDamageBonus(Attacker).Should().Be(4);
        _service.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker).Should().Be(9);
        _service.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker).Should().Be(15);

        _service.StoreWeaponAbilityActivationIdleBonuses(Attacker, false, 4, 9, 15);
        _service.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeTrue("a snapshot is taken even when not idle");
        _service.GetWeaponAbilityActivationIdleDamageBonus(Attacker).Should().Be(0);
        _service.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker).Should().Be(0);
        _service.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker).Should().Be(0);

        _invalid.Add(Attacker);
        _service.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 4, 9, 15);
        _service.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeFalse("cleared, and nothing stored for an invalid creature");

        _invalid.Remove(Attacker);
        _service.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 4, 9, 15);
        _service.ClearWeaponAbilityActivationIdleBonuses(Attacker);
        _service.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeFalse();
        _service.GetWeaponAbilityActivationIdleDamageBonus(Attacker).Should().Be(0);
    }

    [Test]
    public void TheQueuedActivationHitChanceAddsTheStatusCategoryAccuracyOfTheQueuedAbility()
    {
        Stat(Attacker, StatType.AbilityStatusCategoryBonusSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.AbilityStatusCategoryBonusRequiredCategory, (int)StatusEffectCategory.Control);
        Stat(Attacker, StatType.AbilityStatusCategoryHitChancePercentAdjustment, 7);
        var control = new AbilityDetail { ResolveImpactStatusCategories = _ => StatusEffectCategory.Control };
        var plain = new AbilityDetail();

        _service.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, control).Should().Be(1007);
        _service.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, plain).Should().Be(1000);
        _service.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(1000);
        _queriesCalls.Calls.Where(call => call.Member == nameof(ICombatQueriesService.GetPhysicalAndForceAbilityHitChanceAdjustment))
            .Select(call => call.Arguments[1]).Should().AllBeEquivalentTo(SkillType.Rifle);
    }

    [Test]
    public void TheQueuedWeaponAbilityBonusesAreConsumedFromTheAttemptAndTheActivationOfTheSameSkill()
    {
        var attempt = StatType.QueuedWeaponAbilityCriticalRateSkillType;
        var activation = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, (int)SkillType.Rifle, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 2, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, 4, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, 50, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Rifle, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, 3, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, 8, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, 6, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, 30, activation);

        _service.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Pistol).Should().Be((0, 0, 0));
        _service.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((5, 12, 50));
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, attempt).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, activation).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, activation).Should().Be(0);
        _service.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));

        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Rifle, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, 30, activation);
        _service.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 30), "the activation's critical damage alone");
    }

    [Test]
    public void ClearingTheQueuedAttemptBonusesConsumesAllFour()
    {
        var group = StatType.QueuedWeaponAbilityCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 2, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, 4, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, 50, group);

        _service.ClearQueuedWeaponAbilityAttemptBonuses(Attacker);

        foreach (var stat in new[] { StatType.QueuedWeaponAbilityCriticalRateSkillType, StatType.QueuedWeaponAbilityDamageBonus,
                     StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment })
            Stored(Attacker, stat, group).Should().Be(0, stat.ToString());
    }

    [Test]
    public void TheGuardedHitBonusesOfTheNextAttackAreConsumedTogether()
    {
        var group = StatType.NextAttackGuardedHitDMGBonus;
        Store(Attacker, StatType.NextAttackGuardedHitDMGBonus, 7, group);
        Store(Attacker, StatType.NextAttackGuardedHitEnmityBonus, 40, group);
        Store(Attacker, StatType.NextAttackGuardedHitCriticalRatePercentAdjustment, 11, group);

        _service.ConsumeNextAttackGuardedHitBonuses(Attacker).Should().Be((7, 11, 40));
        _service.ConsumeNextAttackGuardedHitBonuses(Attacker).Should().Be((0, 0, 0));

        Store(Attacker, StatType.NextAttackGuardedHitDMGBonus, 2, group);
        Store(Attacker, StatType.NextAttackGuardedHitCriticalRatePercentAdjustment, 3, group);
        _service.ConsumeNextAttackGuardedHitAutoAttackBonuses(Attacker).Should().Be((2, 0));
        _service.ConsumeNextAttackGuardedHitCriticalRateBonus(Attacker).Should().Be(3);
    }

    [Test]
    public void TheStatusAppliedNextAttackDamageBonusIsReadAndConsumed()
    {
        Store(Attacker, StatType.NextAttackStatusAppliedDMGBonus, 5, StatType.NextAttackStatusAppliedDMGBonus);

        _service.GetStatusAppliedNextAttackDamageBonus(Attacker).Should().Be(5);
        _service.ConsumeStatusAppliedNextAttackDamageBonus(Attacker).Should().Be(5);
        _service.GetStatusAppliedNextAttackDamageBonus(Attacker).Should().Be(0);
    }

    [Test]
    public void TheGuardedHitEnmityGoesToAValidDefenderOnlyWhenPositive()
    {
        _invalid.Add(Gone);
        _service.ApplyNextAttackGuardedHitEnmityBonus(Gone, Defender, 10);
        _service.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Gone, 10);
        _service.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 0);
        _enmityCalls.Calls.Should().BeEmpty();

        _service.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 10);
        _enmityCalls.Calls.Single().Arguments.Should().Equal(Attacker, Defender, 10);
    }

    [Test]
    public void TheStatValueGrantsReadTheirPerkAndSkillAndHandThemToTheAttackTimingPart()
    {
        _service.GrantNextAbilityDamageBonus(Attacker, (int)PerkType.DoubleShot, 5, 12);
        _service.GrantNextAbilityDamageBonus(Attacker, -3, 5, 12);
        _service.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 4, 9, 6);
        _service.GrantNextSkillAbilityBonuses(Attacker, 0, 4, 9, 6);

        _timingCalls.Calls.Select(call => $"{call.Member} {string.Join(",", call.Arguments)}").Should().Equal(
            $"GrantNextAbilityDamageBonus {Attacker},{PerkType.DoubleShot},5,12",
            $"GrantNextAbilityDamageBonus {Attacker},{PerkType.Invalid},5,12",
            $"GrantNextSkillAbilityBonuses {Attacker},{SkillType.Rifle},4,9,6,0,{StatusEffectCategory.None}",
            $"GrantNextSkillAbilityBonuses {Attacker},{SkillType.Invalid},4,9,6,0,{StatusEffectCategory.None}");
    }

    [Test]
    public void TheNextAbilityDamageAndStaminaBonusesAreKeptPerPerk()
    {
        var group = $"{nameof(PerkType)}:{(int)PerkType.DoubleShot}";
        _modifiers.Replace(Attacker, StatType.NextAbilityDamageBonus, 6, 30, group);
        _modifiers.Replace(Attacker, StatType.NextAbilityStaminaCostAdjustment, -2, 30, group);

        _service.ConsumeNextAbilityDamageBonus(Attacker, PerkType.Invalid).Should().Be(0);
        _service.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.Invalid).Should().Be(0);
        _service.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.Invalid).Should().Be(0);
        _service.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot).Should().Be(-2);
        _service.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot).Should().Be(-2);
        _service.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot).Should().Be(0);
        _service.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot).Should().Be(6);
        _service.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot).Should().Be(0);
    }

    [Test]
    public void TheTargetedAbilityAdjustmentsReadTheirPerkStatsFromTheQueriesPart()
    {
        // The scripted queries part answers a targeted adjustment with (adjustment stat % 7) + 1, so each stat is told apart.
        int Targeted(StatType adjustment) => (int)adjustment % 7 + 1;

        _service.GetAbilityDamageFlatAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle)
            .Should().Be(Targeted(StatType.AbilityDamageFlatAdjustment) + 100);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, PerkType.DoubleShot).Should().Be(Targeted(StatType.AbilityStaminaCostFlatAdjustment));
        _service.GetAbilityRecastDelayFlatAdjustment(Attacker, PerkType.DoubleShot).Should().Be(Targeted(StatType.AbilityRecastDelayFlatAdjustment));
        _queriesCalls.Calls.Where(call => call.Member == nameof(ICombatQueriesService.GetTargetedAbilityAdjustment))
            .Select(call => string.Join(",", call.Arguments)).Should().Equal(
                $"{Attacker},{PerkType.DoubleShot},{StatType.AbilityDamageFlatAdjustmentPerkType},{StatType.AbilityDamageFlatAdjustmentSecondaryPerkType},{StatType.AbilityDamageFlatAdjustment}",
                $"{Attacker},{PerkType.DoubleShot},{StatType.AbilityStaminaCostFlatAdjustmentPerkType},{StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType},{StatType.AbilityStaminaCostFlatAdjustment}",
                $"{Attacker},{PerkType.DoubleShot},{StatType.AbilityRecastDelayFlatAdjustmentPerkType},{StatType.AbilityRecastDelayFlatAdjustmentPerkType},{StatType.AbilityRecastDelayFlatAdjustment}");
    }

    [Test]
    public void TheStatusCategoryBonusesNeedTheSkillAndAnAppliedRequiredCategory()
    {
        _invalid.Add(Gone);
        _service.GetAbilityStatusCategoryDamageBonus(Gone, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(0);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Invalid, StatusEffectCategory.Control).Should().Be(0);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.None).Should().Be(0);

        Stat(Attacker, StatType.AbilityStatusCategoryDamageBonus, 8);
        Stat(Attacker, StatType.AbilityStatusCategoryHitChancePercentAdjustment, 6);
        Stat(Attacker, StatType.AbilityStatusCategoryBonusSkillType, (int)SkillType.Pistol);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(0, "pistol only");

        Stat(Attacker, StatType.AbilityStatusCategoryBonusSkillType, (int)SkillType.Rifle);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(0, "no category configured");

        Stat(Attacker, StatType.AbilityStatusCategoryBonusRequiredCategory, (int)StatusEffectCategory.Control);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Debuff).Should().Be(0);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(8);
        _service.GetAbilityStatusCategoryHitChancePercentAdjustment(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(6);
    }

    [Test]
    public void TheAbilityStaminaCostAddsTheHostileSkillAndHighResourceAdjustments()
    {
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, (AbilityDetail)null!).Should().Be(0);
        var perk = (int)StatType.AbilityStaminaCostFlatAdjustment % 7 + 1;
        var plain = new AbilityDetail { SkillType = SkillType.Rifle, EffectiveLevelPerkType = PerkType.DoubleShot };
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk);

        var hostile = new AbilityDetail { SkillType = SkillType.Rifle, EffectiveLevelPerkType = PerkType.DoubleShot, IsHostileAbility = true };
        Stat(Attacker, StatType.HostileAbilityStaminaCostFlatAdjustment, -1);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, hostile).Should().Be(perk - 1 + 300);

        Stat(Attacker, StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.SkillAbilityStaminaCostFlatAdjustment, -10);
        Stat(Attacker, StatType.HighResourceAbilityStaminaCostSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.HighResourceAbilityStaminaCostAdjustment, -20);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk - 10, "no threshold is configured");
        Stat(Attacker, StatType.HighResourceAbilityStaminaCostThresholdPercent, 75);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk - 10, "FP is low");
        _highFP = true;
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk - 30);
        _resourcesCalls.Calls.Last().Arguments.Should().Equal(Attacker, 75);

        var pistol = new AbilityDetail { SkillType = SkillType.Pistol, EffectiveLevelPerkType = PerkType.DoubleShot };
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, pistol).Should().Be(perk, "both skill adjustments are the rifle's");
    }

    [Test]
    public void TheIdleStatusDurationBonusNeedsTheSkillIdleTimeAndTheRequiredCategory()
    {
        var targeted = (int)StatType.AbilityStatusDurationPercentAdjustment % 7 + 1;
        int Duration(SkillType skill, Func<IStatusEffect>? factory = null) => _service.GetAbilityStatusDurationPercentAdjustment(
            Attacker, PerkType.DoubleShot, skill, typeof(StunnedStatusEffect), Array.Empty<Type>(), factory!);

        Duration(SkillType.Rifle).Should().Be(targeted, "no idle bonus is configured");
        Stat(Attacker, StatType.IdleStatusDurationPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.IdleStatusDurationPercentAdjustment, 50);
        Duration(SkillType.Rifle).Should().Be(targeted, "no idle time is configured");

        Stat(Attacker, StatType.IdleStatusDurationRequiredIdleSeconds, 6);
        _recentAttack = true;
        Duration(SkillType.Rifle).Should().Be(targeted, "the attacker fought recently");
        _activityCalls.Calls.Last().Arguments.Should().Equal(Attacker, 6f);

        _recentAttack = false;
        Duration(SkillType.Rifle).Should().Be(targeted + 50, "no category is required");
        Duration(SkillType.Pistol).Should().Be(targeted);

        Stat(Attacker, StatType.IdleStatusDurationRequiredCategory, (int)StatusEffectCategory.Control);
        Duration(SkillType.Rifle).Should().Be(targeted, "the ability applies no control status");
        Duration(SkillType.Rifle, () => null!).Should().Be(targeted);
        Duration(SkillType.Rifle, () => new BurnStatusEffect()).Should().Be(targeted, "a burn is not a control status");
        Duration(SkillType.Rifle, () => new StunnedStatusEffect()).Should().Be(targeted + 50);
        _abilityAppliedCategory = true;
        Duration(SkillType.Rifle).Should().Be(targeted + 50, "the ability itself applies a control status");
    }

    [Test]
    public void TheAbilityDefenseIgnoreAddsTheRangedExposedAndDisruptedTargetAdjustments()
    {
        var targeted = (int)StatType.AbilityDefenseIgnorePercentAdjustment % 7 + 1;
        int Ignore(uint defender) => _service.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, defender);
        Stat(Attacker, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment, 10);
        Stat(Attacker, StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment, 20);

        Ignore(Defender).Should().Be(targeted + 200, "the target has neither status");
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        Ignore(Defender).Should().Be(targeted + 200, "the exposed bonus needs its skill selector");
        Stat(Attacker, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType, (int)SkillType.Rifle);
        Ignore(Defender).Should().Be(targeted + 210);
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Ignore(Defender).Should().Be(targeted + 230);
        _service.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Pistol, Defender)
            .Should().Be(targeted + 220, "the exposed bonus is the rifle's");
        _invalid.Add(Gone);
        Ignore(Gone).Should().Be(targeted + 200);

        _statusCalls.Calls.Where(call => call.Member == nameof(IStatusEffectService.HasStatusEffect))
            .Select(call => string.Join(",", ((Type[])call.Arguments[1]!).Select(type => type.Name))).Distinct().Should().Equal(
                "ForceDisruptionStatusEffect,FoggyMindStatusEffect",
                "ExposedStatusEffect,ExposeWeakPointStatusEffect,SunderStatusEffect");
    }

    [Test]
    public void TheHostileRecastDelayAdjustmentScalesTheDelayAndNeverGoesBelowZero()
    {
        var hostile = new AbilityDetail { IsHostileAbility = true };
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f).Should().Be(10f, "no adjustment");
        Stat(Attacker, StatType.HostileAbilityRecastDelayPercentAdjustment, -25);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f).Should().Be(7.5f);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, new AbilityDetail(), 10f).Should().Be(10f, "not hostile");
        _service.ApplyAbilityRecastDelayModifiers(Attacker, null!, 10f).Should().Be(10f);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 0f).Should().Be(0f);
        _invalid.Add(Gone);
        _service.ApplyAbilityRecastDelayModifiers(Gone, hostile, 10f).Should().Be(10f);
        Stat(Attacker, StatType.HostileAbilityRecastDelayPercentAdjustment, -150);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f).Should().Be(0f);
    }

    [Test]
    public void TheAbilityDamageBonusIsFifteenHundredthsOfThePlayersSkillOrTheNpcsLevel()
    {
        _service.GetAbilityDamageBonus(Attacker, SkillType.Rifle).Should().Be(6, "0.15 of level 40");

        _players.Add(Attacker);
        var player = new Player { Id = PlayerId };
        player.Skills[SkillType.Rifle] = new PlayerSkill { Rank = 47 };
        _db.Set(player);
        _service.GetAbilityDamageBonus(Attacker, SkillType.Rifle).Should().Be(7, "0.15 of rank 47");
        _db.Reads.Should().Contain(PlayerId);
    }
}
