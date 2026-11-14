using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat activity records of <c>Combat</c>: when a creature last attacked, used a hostile ability, took or dealt damage
/// and entered combat, the First Strike stacks and stealth opening window those records drive, and the idle-readiness
/// markers and idle bonuses that read them. The engine is a recording fake whose scheduled closures the tests run by hand,
/// the clock is held still, and the neighbouring services are scripted proxies over tables the test fills.
/// </summary>
[TestFixture]
public class CombatActivityServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Gone = 44;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private CombatActivityService _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<(uint, StatType, StatType), int> _temporary = null!;
    private List<string> _temporaryCalls = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _players = null!;
    private HashSet<uint> _inCombat = null!;
    private HashSet<(uint, Type)> _activeStatuses = null!;
    private List<string> _statusCalls = null!;
    private List<string> _resourceCalls = null!;
    private List<string> _suppressionCalls = null!;
    private bool _triggerReady;
    private bool _stealthed;
    private bool _hostile;
    private bool _areaImpact;
    private bool _targetHasCategory;
    private ActivityStatusType _busy;
    private ScriptedProxy _feedbackCalls = null!;
    private int _scheduledRun;

    [SetUp]
    public void SetUp()
    {
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _stats = new();
        _temporary = new();
        _temporaryCalls = new();
        _invalid = new();
        _players = new();
        _inCombat = new();
        _activeStatuses = new();
        _statusCalls = new();
        _resourceCalls = new();
        _suppressionCalls = new();
        _triggerReady = true;
        _stealthed = false;
        _hostile = true;
        _areaImpact = false;
        _targetHasCategory = false;
        _busy = ActivityStatusType.Invalid;
        _scheduledRun = 0;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsInCombat", args => _inCombat.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => _hostile);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls
            .On(nameof(ICombatQueriesService.GetAbilitySkillType), args => ((AbilityDetail?)args[1])?.SkillType ?? SkillType.Invalid)
            .On(nameof(ICombatQueriesService.IsStealthedOrInvisible), _ => _stealthed)
            .On(nameof(ICombatQueriesService.TargetHasAnyStatusEffectCategory), _ => _targetHasCategory);
        var resources = ScriptedProxy.Create<ICombatResourcesService>(out var resourceCalls);
        resourceCalls
            .On(nameof(ICombatResourcesService.TryUseStatTrigger), args =>
            {
                _resourceCalls.Add($"Trigger {args[0]} {args[1]} {args[2]}");
                return _triggerReady;
            })
            .On(nameof(ICombatResourcesService.RestoreAbilityHitStaminaFromStat), args =>
            {
                _resourceCalls.Add($"Restore {args[0]} {args[1]} {args[2]}");
                return (int)args[2]!;
            });
        var suppression = ScriptedProxy.Create<ICombatSuppressionService>(out var suppressionCalls);
        suppressionCalls.On(nameof(ICombatSuppressionService.TrackSuppressionAbilityUse), args =>
        {
            _suppressionCalls.Add($"Track {args[0]} {(DateTime)args[1]! == _now}");
            return null;
        });
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls.On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)));
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args =>
            {
                _statusCalls.Add($"Remove {args[0]} {((Type)args[1]!).Name}");
                _activeStatuses.Remove(((uint)args[0]!, (Type)args[1]!));
                return null;
            })
            .On(nameof(IStatusEffectService.HasStatusEffect), args => _activeStatuses.Contains(((uint)args[0]!, (Type)args[1]!)))
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
            {
                var type = args[2]!.GetType();
                _statusCalls.Add($"Apply {args[0]} {args[1]} {type.Name} {args[3]}");
                _activeStatuses.Add(((uint)args[1]!, type));
                return true;
            });
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls
            .On(nameof(ITemporaryStatModifierService.GetStatAdjustment), args =>
                _temporary.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!, (StatType)args[2]!)))
            .On(nameof(ITemporaryStatModifierService.Consume), args =>
            {
                _temporaryCalls.Add($"Consume {args[1]}");
                _temporary.Remove(((uint)args[0]!, (StatType)args[1]!, (StatType)args[2]!), out var value);
                return value;
            })
            .On(nameof(ITemporaryStatModifierService.Replace), args =>
            {
                _temporaryCalls.Add($"Replace {args[1]} {args[2]} {args[3]} {args[4]}");
                _temporary[((uint)args[0]!, (StatType)args[1]!, (StatType)args[4]!)] = (int)args[2]!;
                return null;
            });
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out _feedbackCalls);
        _feedbackCalls.On(nameof(IPlayerFeedbackService.ShowDiagnosticFloatingText), _ => null);
        var ability = ScriptedProxy.Create<IAbilityService>(out var abilityCalls);
        abilityCalls.On(nameof(IAbilityService.IsAreaImpact), _ => _areaImpact);
        var activity = ScriptedProxy.Create<IActivityService>(out var activityCalls);
        activityCalls.On(nameof(IActivityService.GetBusyType), _ => _busy);

        _service = new CombatActivityService(queries, resources, suppression, stat, statusEffect, temporary, feedback, ability, activity);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private void Advance(double seconds) => _now = _now.AddSeconds(seconds);

    private static AbilityDetail Hostile(SkillType skill = SkillType.Rifle) => new() { IsHostileAbility = true, SkillType = skill };

    private IEnumerable<string> Messages() => _engine.CallsTo("SendMessageToPC").Select(call => call.Arguments[1]!.ToString()!);

    private IEnumerable<string> FloatingTexts() => _engine.CallsTo("FloatingTextStringOnCreature").Select(call => call.Arguments[0]!.ToString()!);

    // Patience-style idle channel on the rifle, plus the opening auto-attack window.
    private void ConfigureIdleChannels(uint creature, int idleSeconds = 3, int openingSeconds = 3)
    {
        Stat(creature, StatType.IdleSkillAbilitySkillType, (int)SkillType.Rifle);
        Stat(creature, StatType.IdleSkillAbilityRequiredIdleSeconds, idleSeconds);
        Stat(creature, StatType.IdleSkillAbilityDamageBonus, 14);
        Stat(creature, StatType.IdleSkillAbilityHitChancePercentAdjustment, 8);
        Stat(creature, StatType.AlternateIdleSkillAbilitySkillType, (int)SkillType.Rifle);
        Stat(creature, StatType.AlternateIdleSkillAbilityRequiredIdleSeconds, idleSeconds);
        Stat(creature, StatType.AlternateIdleSkillAbilityCriticalDamagePercentAdjustment, 15);
        Stat(creature, StatType.OpeningAutoAttackIdleSeconds, openingSeconds);
    }

    private static string Combat(string text) => SWLOR.Game.Server.Domain.Foundation.ColorToken.Combat(text);

    // Runs every closure the engine was handed (assigned and delayed) and not run yet, in order, until none is left.
    private void RunScheduled()
    {
        while (true)
        {
            var pending = _engine.Calls.Where(call => call.Member is "AssignCommand" or "DelayCommand").Skip(_scheduledRun).ToList();
            if (pending.Count == 0)
                return;
            foreach (var call in pending)
            {
                _scheduledRun++;
                ((Action)call.Arguments[^1]!)();
            }
        }
    }

    private IReadOnlyList<float> Delays() => _engine.CallsTo("DelayCommand").Select(call => (float)call.Arguments[0]!).ToList();

    [Test]
    public void DamageRecordsAreKeptPerPairAndExpireOnTheirFirstLateRead()
    {
        _invalid.Add(Gone);
        _service.TrackRecentDamageTarget(Attacker, Attacker);
        _service.TrackRecentDamageTarget(Attacker, Gone);
        _service.TrackRecentDamageTarget(Gone, Defender);
        _service.TrackRecentDamageTaken(Gone);
        _service.RecentDamageTargets.Should().BeEmpty();

        _service.TrackRecentDamageTarget(Attacker, Defender);
        _service.TrackRecentDamageTaken(Defender);
        _service.RecentDamageTargets.Should().Equal(new Dictionary<(uint, uint), DateTime> { [(Attacker, Defender)] = _now });

        _service.HasRecentDamageTarget(Attacker, Defender, 0f).Should().BeFalse();
        _service.HasRecentDamageTarget(Attacker, Attacker, 5f).Should().BeFalse();
        _service.HasRecentDamageTarget(Gone, Defender, 5f).Should().BeFalse();
        _service.HasRecentDamageTarget(Defender, Attacker, 5f).Should().BeFalse("the record is directional");
        _service.HasRecentDamageTaken(Defender, 0f).Should().BeFalse();
        _service.HasRecentDamageTaken(Gone, 5f).Should().BeFalse();
        _service.HasRecentDamageTaken(Attacker, 5f).Should().BeFalse();

        Advance(5);
        _service.HasRecentDamageTarget(Attacker, Defender, 5f).Should().BeTrue();
        _service.HasRecentDamageTaken(Defender, 5f).Should().BeTrue();
        Advance(0.5);
        _service.HasRecentDamageTarget(Attacker, Defender, 5f).Should().BeFalse();
        _service.HasRecentDamageTaken(Defender, 5f).Should().BeFalse();
        _service.HasRecentDamageTarget(Attacker, Defender, 10f).Should().BeFalse("an expired record is removed when read");
        _service.HasRecentDamageTaken(Defender, 10f).Should().BeFalse();
    }

    [Test]
    public void AttacksAndHostileAbilitiesKeepSeparateCompletedAndAttemptedTimes()
    {
        _service.TrackAttackActivity(Gone);
        _invalid.Add(Gone);
        _service.TrackAttackActivity(Gone);
        _service.TrackHostileAbilityActivity(Gone);
        _service.GetLastCompletedOffensiveActivityAt(Gone).Should().Be(_now, "it was valid on the first call");

        var attackedAt = _now;
        _service.TrackAttackActivity(Attacker);
        _service.HasRecentAttackActivity(Attacker, 0f).Should().BeFalse();
        _service.HasRecentAttackActivity(Defender, 5f).Should().BeFalse();
        _service.HasRecentAttackActivity(Gone, 5f).Should().BeFalse();
        _service.HasRecentAttackActivity(Attacker, 5f).Should().BeTrue();
        _statusCalls.Should().Equal(
            $"Remove {Gone} OpeningAttackReadyStatusEffect", $"Remove {Gone} IdleSkillAbilityReadyStatusEffect",
            $"Remove {Attacker} OpeningAttackReadyStatusEffect", $"Remove {Attacker} IdleSkillAbilityReadyStatusEffect");

        Advance(2);
        _service.TrackHostileAbilityActivity(Attacker);
        _service.GetLastOffensiveActivityAt(Attacker).Should().Be(_now, "the cast attempt is later than the attack");
        _service.GetLastCompletedOffensiveActivityAt(Attacker).Should().Be(attackedAt, "an attempt is not a completed use");
        _service.HasRecentCombatAbilityUse(Attacker, 10f).Should().BeFalse();

        Advance(2);
        _service.TrackHostileAbilityActivity(Attacker, countAsCompletedOffensiveActivity: true);
        _service.GetLastCompletedOffensiveActivityAt(Attacker).Should().Be(_now);
        _service.HasRecentCombatAbilityUse(Attacker, 1f).Should().BeTrue();
        Advance(5);
        _service.HasRecentAttackActivity(Attacker, 5f).Should().BeFalse("the attack was nine seconds ago");
        _service.GetLastOffensiveActivityAt(Attacker).Should().Be(_now.AddSeconds(-5));
        _service.HasRecentCombatAbilityUse(Attacker, 1f).Should().BeFalse();
        _service.HasRecentCombatAbilityUse(Defender, 1f).Should().BeFalse();
        _service.GetLastOffensiveActivityAt(Defender).Should().Be(default);
    }

    [Test]
    public void CombatEntryLastsThirtySecondsFromAnyOfItsThreeSources()
    {
        _service.HasRecentHostileCombatActivity(Attacker).Should().BeFalse();
        _inCombat.Add(Attacker);
        _service.HasRecentHostileCombatActivity(Attacker).Should().BeTrue("native combat counts");
        _inCombat.Clear();
        _invalid.Add(Gone);
        _service.HasRecentHostileCombatActivity(Gone).Should().BeFalse();
        _service.TrackCombatActivity(Gone);

        _service.TrackCombatActivity(Attacker);
        Advance(30);
        _service.HasRecentHostileCombatActivity(Attacker).Should().BeTrue();
        Advance(1);
        _service.HasRecentHostileCombatActivity(Attacker).Should().BeFalse();

        _service.TrackHostileAbilityActivity(Attacker);
        _service.HasRecentHostileCombatActivity(Attacker).Should().BeTrue("a cast attempt enters combat");
        Advance(31);

        _hostile = false;
        _service.TrackHostileDefensiveCombatEntryActivity(Defender, Attacker);
        _service.TrackHostileDefensiveCombatEntryActivity(Gone, Attacker);
        _service.TrackHostileDefensiveCombatEntryActivity(Defender, Gone);
        _service.HasRecentHostileCombatActivity(Defender).Should().BeFalse("only a hostile attacker starts combat");
        _hostile = true;
        _service.TrackHostileDefensiveCombatEntryActivity(Defender, Attacker);
        _service.HasRecentHostileCombatActivity(Defender).Should().BeTrue("incoming hostile actions also enter combat");
        _service.GetLastOffensiveActivityAt(Defender).Should().Be(default, "being attacked is not offensive activity");
    }

    [Test]
    public void TheFirstAttackOfAnEngagementRestoresStaminaOnce()
    {
        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        Stat(Attacker, StatType.FirstCombatAttackStaminaRestore, 6);
        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        _resourceCalls.Should().BeEmpty("no cooldown means the rider is not configured");

        Stat(Attacker, StatType.FirstCombatAttackStaminaRestoreCooldownSeconds, 20);
        _triggerReady = false;
        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        _resourceCalls.Should().Equal($"Trigger {Attacker} FirstCombatAttackStaminaRestore 20");

        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        _resourceCalls.Should().HaveCount(1, "the engagement's first attack was already spent, even on cooldown");

        // A new engagement after thirty quiet seconds re-arms the rider.
        _service.TrackCombatActivity(Attacker);
        Advance(31);
        _service.TrackCombatActivity(Attacker);
        _triggerReady = true;
        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        _resourceCalls.Skip(1).Should().Equal(
            $"Trigger {Attacker} FirstCombatAttackStaminaRestore 20",
            $"Restore {Attacker} FirstCombatAttackStaminaRestore 6");

        // Inside the engagement it stays spent.
        Advance(5);
        _service.TrackCombatActivity(Attacker);
        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        _resourceCalls.Should().HaveCount(3);
    }

    [Test]
    public void TheHeartbeatPurgeDropsOnlyTheOffensiveRecordsOfCreaturesThatAreGone()
    {
        _service.TrackAttackActivity(Attacker);
        _service.TrackAttackActivity(Gone);
        _service.TrackHostileAbilityActivity(Gone, countAsCompletedOffensiveActivity: true);
        _service.TrackHostileAbilityActivity(Defender, countAsCompletedOffensiveActivity: true);
        _service.TrackRecentDamageTaken(Gone);
        _invalid.Add(Gone);

        _service.PurgeInvalidOffensiveActivity();

        var attacks = Field<Dictionary<uint, DateTime>>("_lastAttackActivity");
        var abilities = Field<Dictionary<uint, DateTime>>("_lastCombatAbilityUse");
        attacks.Keys.Should().Equal(Attacker);
        abilities.Keys.Should().Equal(Defender);
        Field<Dictionary<uint, DateTime>>("_recentDamageTaken").Keys.Should().Equal(new[] { Gone }, "the purge covers only the two offensive tables");
    }

    private T Field<T>(string name) =>
        (T)typeof(CombatActivityService).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;

    [Test]
    public void TheStealthOpeningWindowLastsSixSecondsAndIsSpentByTheBonus()
    {
        Stat(Attacker, StatType.DirectDamageToStatusCategoryOrStealthBonus, 9);
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Defender, Attacker).Should().Be(0, "no bonus configured");

        _service.TrackStealthOpeningWindow(Attacker);
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender).Should().Be(0, "only a stealthed creature opens a window");

        _stealthed = true;
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender).Should().Be(9, "still stealthed");
        _service.TrackStealthOpeningWindow(Attacker);
        _stealthed = false;
        Advance(6);
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender).Should().Be(9);
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender).Should().Be(0, "the window is spent");

        _stealthed = true;
        _service.TrackStealthOpeningWindow(Attacker);
        _stealthed = false;
        Advance(6.5);
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender).Should().Be(0, "the window ran out");

        _invalid.Add(Gone);
        Stat(Gone, StatType.DirectDamageToStatusCategoryOrStealthBonus, 9);
        _service.TrackStealthOpeningWindow(Gone);
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Gone, Defender).Should().Be(0);

        // The status-category half of the rider needs no window.
        Stat(Attacker, StatType.DirectDamageToStatusCategoryOrStealthBonusCategory, 1);
        _targetHasCategory = true;
        _service.GetDirectDamageToStatusCategoryOrStealthBonus(Attacker, Defender).Should().Be(9);
    }

    [Test]
    public void FirstStrikeSpendsItsStacksAndRechargesOnItsCooldown()
    {
        _players.Add(Attacker);
        var ability = Hostile();
        Stat(Attacker, StatType.FirstHostileAbilityHitDamageBonus, 5);
        Stat(Attacker, StatType.FirstHostileAbilityHitMaximumCount, 2);
        Stat(Attacker, StatType.FirstHostileAbilityHitCooldownSeconds, 60);

        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, new AbilityDetail()).Should().Be(0, "a friendly ability gets nothing");
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, null!).Should().Be(0);
        Messages().Should().BeEmpty();

        _service.TrackCombatActivity(Attacker);
        Messages().Should().Equal(Combat("First Strike ready: 2 stacks (+5 DMG each)."));
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, ability).Should().Be(5);

        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Messages().Last().Should().Be(Combat("First Strike deals +5 DMG (1 stack remaining)."));
        Advance(2);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Messages().Last().Should().Be(Combat("First Strike deals +5 DMG (0 stacks remaining; recharges in 60 seconds)."));
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, ability).Should().Be(0);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Messages().Should().HaveCount(3, "an exhausted rider counts nothing more");

        // A new engagement while exhausted reports the time left; the cooldown, not combat, recharges the stacks.
        Advance(31);
        _service.TrackCombatActivity(Attacker);
        Messages().Last().Should().Be(Combat("First Strike is recharging (29 seconds remaining)."));
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, ability).Should().Be(0);
        Advance(29);
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, ability).Should().Be(5, "the cooldown recharged every stack");
        Messages().Last().Should().Be(Combat("First Strike ready: 2 stacks (+5 DMG each)."));
        _feedbackCalls.Calls.Should().HaveCount(4, "the ready and spent notices also float, the recharge notice does not");
    }

    [Test]
    public void FirstStrikeCountsAnAreaImpactOnceAndRefillsAfterCombatWithoutACooldown()
    {
        var ability = Hostile();
        Stat(Attacker, StatType.FirstHostileAbilityHitMaximumCount, 3);
        _service.TrackCombatActivity(Attacker);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, new AbilityDetail());

        _areaImpact = true;
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Advance(0.5);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Advance(1);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        _areaImpact = false;
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Messages().Should().BeEmpty("a zero bonus reports nothing");

        Stat(Attacker, StatType.FirstHostileAbilityHitDamageBonus, 4);
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, ability).Should().Be(0,
            "the hits of one area impact count once, so three impacts exhaust three stacks");

        Advance(31);
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, ability).Should().Be(4, "without a cooldown the set refills out of combat");

        // A new engagement announces the refilled set; stacks left over also refill once the wielder is out of combat.
        _players.Add(Attacker);
        _service.TrackCombatActivity(Attacker);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Advance(31);
        _service.TrackCombatActivity(Attacker);
        Messages().Should().Equal(
            Combat("First Strike ready: 3 stacks (+4 DMG each)."),
            Combat("First Strike deals +4 DMG (2 stacks remaining)."),
            Combat("First Strike ready: 3 stacks (+4 DMG each)."));

        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, ability);
        Messages().Last().Should().Be(Combat("First Strike deals +4 DMG (0 stacks remaining; recharges after combat)."));
    }

    [Test]
    public void ANonPlayerHearsNothingAboutFirstStrike()
    {
        Stat(Attacker, StatType.FirstHostileAbilityHitMaximumCount, 1);
        _service.TrackCombatActivity(Attacker);
        Stat(Attacker, StatType.FirstHostileAbilityHitDamageBonus, 3);
        Stat(Attacker, StatType.FirstHostileAbilityHitCooldownSeconds, 60);
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, Hostile()).Should().Be(3);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, Hostile());
        Advance(31);
        _service.TrackCombatActivity(Attacker);

        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, Hostile()).Should().Be(0, "still recharging");
        Messages().Should().BeEmpty();
        _feedbackCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void AbilityUseRecordsOnlyHostileUsesAndArmsSuppressionWindows()
    {
        _invalid.Add(Gone);
        _service.TrackCombatAbilityUse(Gone, Hostile());
        _service.TrackCombatAbilityUse(Attacker, null!);
        _service.TrackCombatAbilityUse(Attacker, new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Invalid });
        _suppressionCalls.Should().BeEmpty();

        _service.TrackCombatAbilityUse(Attacker, new AbilityDetail { SkillType = SkillType.Rifle });
        _suppressionCalls.Should().Equal($"Track {Attacker} True");
        _service.HasRecentCombatAbilityUse(Attacker, 5f).Should().BeFalse("a friendly ability is not combat activity");
        _engine.CallsTo("AssignCommand").Should().BeEmpty();

        ConfigureIdleChannels(Attacker);
        _service.TrackCombatAbilityUse(Attacker, Hostile());
        _service.HasRecentCombatAbilityUse(Attacker, 5f).Should().BeTrue();
        _engine.CallsTo("AssignCommand").Should().ContainSingle("readiness is rescheduled from the completed use");
    }

    [Test]
    public void UsingAnAbilityMeansActivatingOneOrHavingJustUsedOne()
    {
        _service.IsUsingAbility(Attacker).Should().BeFalse();
        _busy = ActivityStatusType.AbilityActivation;
        _service.IsUsingAbility(Attacker).Should().BeTrue();
        _invalid.Add(Gone);
        _service.IsUsingAbility(Gone).Should().BeFalse();

        _busy = ActivityStatusType.Invalid;
        _service.TrackCombatAbilityUse(Attacker, Hostile());
        _service.IsUsingAbility(Attacker).Should().BeTrue();
        Advance(2.5);
        _service.IsUsingAbility(Attacker).Should().BeFalse();
    }

    [Test]
    public void IdleBonusesSumEveryReadyChannelOfTheSkill()
    {
        ConfigureIdleChannels(Attacker, idleSeconds: 3);
        Stat(Attacker, StatType.AlternateIdleSkillAbilityRequiredIdleSeconds, 6);

        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((14, 8, 15), "a creature that never attacked is idle");
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Invalid).Should().Be((0, 0, 0));
        _invalid.Add(Gone);
        _service.GetIdleSkillAbilityBonuses(Gone, SkillType.Rifle).Should().Be((0, 0, 0));
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Pistol).Should().Be((0, 0, 0));

        _service.TrackAttackActivity(Attacker);
        Advance(2);
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));
        _service.GetIdleAbilityHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(0);
        Advance(1);
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((14, 8, 0), "only the three-second channel is ready");
        _service.GetIdleAbilityHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(8);
        Advance(3);
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((14, 8, 15));
    }

    [Test]
    public void AnActivationKeepsTheIdleBonusesItStartedWith()
    {
        ConfigureIdleChannels(Attacker);
        _service.StoreAbilityActivationIdleBonuses(Attacker, SkillType.Rifle);
        _temporaryCalls.Should().Equal(
            "Consume AbilityActivationIdleDamageBonus", "Consume AbilityActivationIdleHitChancePercentAdjustment",
            "Consume AbilityActivationIdleCriticalDamagePercentAdjustment", "Consume AbilityActivationIdleSkillType",
            $"Replace AbilityActivationIdleSkillType {(int)SkillType.Rifle} 30 AbilityActivationIdleSkillType",
            "Replace AbilityActivationIdleDamageBonus 14 30 AbilityActivationIdleSkillType",
            "Replace AbilityActivationIdleHitChancePercentAdjustment 8 30 AbilityActivationIdleSkillType",
            "Replace AbilityActivationIdleCriticalDamagePercentAdjustment 15 30 AbilityActivationIdleSkillType");

        // The cast itself is offensive activity, but the stored bonuses still apply to it.
        _service.TrackAttackActivity(Attacker);
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((14, 8, 15));

        _temporaryCalls.Clear();
        _service.ClearAbilityActivationIdleBonuses(Attacker);
        _temporaryCalls.Should().HaveCount(4);
        _service.GetIdleSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));

        _temporaryCalls.Clear();
        _service.StoreAbilityActivationIdleBonuses(Attacker, SkillType.Invalid);
        _invalid.Add(Gone);
        _service.StoreAbilityActivationIdleBonuses(Gone, SkillType.Rifle);
        _temporaryCalls.Should().HaveCount(8).And.OnlyContain(call => call.StartsWith("Consume"), "an invalid skill or creature only clears");
    }

    [Test]
    public void ReadinessMarkersAreAppliedOnceWhenTheIdleWindowsPass()
    {
        _players.Add(Attacker);
        ConfigureIdleChannels(Attacker, idleSeconds: 4, openingSeconds: 3);
        _service.TrackAttackActivity(Attacker);
        _statusCalls.Clear();
        _engine.CallsTo("AssignCommand").Should().HaveCount(2, "one callback per distinct delay");

        Advance(5);
        RunScheduled();

        Delays().Should().Equal(4f, 3f);
        _statusCalls.Should().Equal(
            $"Apply {Attacker} {Attacker} OpeningAttackReadyStatusEffect -1",
            $"Apply {Attacker} {Attacker} IdleSkillAbilityReadyStatusEffect -1");
        FloatingTexts().Should().Equal(Combat("Opening Attack Ready"), Combat("Idle Skill Ability Ready"));
    }

    [Test]
    public void AReadinessCallbackWaitsOutTheRestOfItsWindowAndIsDroppedByNewerActivity()
    {
        ConfigureIdleChannels(Attacker, idleSeconds: 5, openingSeconds: 0);
        _service.TrackAttackActivity(Attacker);
        ((Action)_engine.CallsTo("AssignCommand").Single().Arguments[1]!)();
        var refresh = (Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!;

        Advance(2);
        refresh();
        Delays().Last().Should().BeApproximately(3f, 0.001f, "a callback that fires early waits out the rest of the window");
        _statusCalls.Should().NotContain(call => call.StartsWith("Apply"));

        _service.TrackAttackActivity(Attacker);
        _statusCalls.Clear();
        Advance(5);
        refresh();
        _statusCalls.Should().BeEmpty("a newer attack replaced the window this callback was waiting for");

        _invalid.Add(Attacker);
        ((Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!)();
        _statusCalls.Should().BeEmpty("a creature that is gone gets nothing");
    }

    [Test]
    public void ACreatureThatNeverAttackedIsReadyAtOnce()
    {
        ConfigureIdleChannels(Defender, idleSeconds: 3, openingSeconds: 0);
        _service.ScheduleIdleReadinessRefresh(Defender);
        RunScheduled();

        _statusCalls.Should().Equal($"Apply {Defender} {Defender} IdleSkillAbilityReadyStatusEffect -1");
        FloatingTexts().Should().BeEmpty("an NPC gets no floating text");

        // Already marked: refreshing again applies nothing.
        _statusCalls.Clear();
        _service.ScheduleIdleReadinessRefresh(Defender);
        RunScheduled();
        _statusCalls.Should().BeEmpty();
    }

    [Test]
    public void ReadinessIsRebuiltAfterAPlayerEntersButNotForDungeonMasters()
    {
        _players.Add(Attacker);
        ConfigureIdleChannels(Attacker, idleSeconds: 3, openingSeconds: 3);
        _engine.SetReturnHandler("GetEnteringObject", _ => Defender);
        _service.RestoreIdleReadinessOnPlayerEnter();
        _engine.SetReturnHandler("GetEnteringObject", _ => Attacker);
        _engine.SetReturnHandler("GetIsDM", _ => true);
        _service.RestoreIdleReadinessOnPlayerEnter();
        _engine.CallsTo("DelayCommand").Should().BeEmpty();

        _engine.SetReturnHandler("GetIsDM", _ => false);
        _service.RestoreIdleReadinessOnPlayerEnter();
        _engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(0f);
        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();

        _statusCalls.Should().Equal(
            $"Apply {Attacker} {Attacker} OpeningAttackReadyStatusEffect -1",
            $"Apply {Attacker} {Attacker} IdleSkillAbilityReadyStatusEffect -1");
        _engine.CallsTo("AssignCommand").Should().ContainSingle("the login refresh also schedules the next check");
    }

    [Test]
    public void ACreatureThatIsGoneBeforeItsCallbacksRunGetsNoMarkers()
    {
        _players.Add(Attacker);
        ConfigureIdleChannels(Attacker, idleSeconds: 3, openingSeconds: 3);
        _engine.SetReturnHandler("GetEnteringObject", _ => Attacker);
        _service.RestoreIdleReadinessOnPlayerEnter();
        _service.ScheduleIdleReadinessRefresh(Attacker);
        _invalid.Add(Attacker);

        RunScheduled();

        _statusCalls.Should().BeEmpty();
        FloatingTexts().Should().BeEmpty();
    }

    [Test]
    public void AChannelIsConfiguredOnlyWithASkillAndAnIdleTime()
    {
        _service.HasConfiguredIdleSkillAbilityChannel(Attacker).Should().BeFalse();
        Stat(Attacker, StatType.AlternateIdleSkillAbilitySkillType, (int)SkillType.Staff);
        _service.HasConfiguredIdleSkillAbilityChannel(Attacker).Should().BeFalse();
        Stat(Attacker, StatType.AlternateIdleSkillAbilityRequiredIdleSeconds, 3);
        _service.HasConfiguredIdleSkillAbilityChannel(Attacker).Should().BeTrue();
        Stat(Attacker, StatType.AlternateIdleSkillAbilitySkillType, 0);
        _service.HasConfiguredIdleSkillAbilityChannel(Attacker).Should().BeFalse();
    }

    [Test]
    public void ForgettingACreatureClearsItsRecordsAndTheDamagePairsItIsIn()
    {
        _service.TrackRecentDamageTarget(Attacker, Defender);
        _service.TrackRecentDamageTarget(Defender, Attacker);
        _service.TrackRecentDamageTarget(Defender, Gone);
        _service.TrackRecentDamageTaken(Attacker);
        _service.TrackAttackActivity(Attacker);
        _service.TrackHostileAbilityActivity(Attacker, countAsCompletedOffensiveActivity: true);
        _service.TrackHostileDefensiveCombatEntryActivity(Attacker, Defender);
        _stealthed = true;
        _service.TrackStealthOpeningWindow(Attacker);
        _stealthed = false;
        Stat(Attacker, StatType.FirstHostileAbilityHitDamageBonus, 2);
        Stat(Attacker, StatType.FirstHostileAbilityHitMaximumCount, 1);
        Stat(Attacker, StatType.FirstHostileAbilityHitCooldownSeconds, 60);
        _service.ApplyFirstHostileAbilityHitCount(Attacker, Hostile());
        Stat(Attacker, StatType.FirstCombatAttackStaminaRestore, 5);
        Stat(Attacker, StatType.FirstCombatAttackStaminaRestoreCooldownSeconds, 10);
        _service.ApplyFirstCombatAttackStaminaRestore(Attacker);
        Field<HashSet<uint>>("_firstCombatAttackConsumed").Should().Contain(Attacker);

        _service.ForgetActivityState(Attacker);

        _service.RecentDamageTargets.Keys.Should().Equal((Defender, Gone));
        _service.HasRecentDamageTaken(Attacker, 5f).Should().BeFalse();
        _service.GetLastOffensiveActivityAt(Attacker).Should().Be(default);
        _service.GetLastCompletedOffensiveActivityAt(Attacker).Should().Be(default);
        _service.HasRecentHostileCombatActivity(Attacker).Should().BeFalse();
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, Hostile()).Should().Be(0, "First Strike is opening state, still recharging");
        Field<Dictionary<uint, DateTime>>("_stealthOpeningWindows").Should().ContainKey(Attacker);
        Field<HashSet<uint>>("_firstCombatAttackConsumed").Should().BeEmpty();

        _service.ForgetOpeningActivityState(Attacker);

        Field<Dictionary<uint, DateTime>>("_stealthOpeningWindows").Should().BeEmpty();
        _service.GetFirstHostileAbilityHitDamageBonus(Attacker, Hostile()).Should().Be(2, "a fresh set");
    }
}
