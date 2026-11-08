#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using AssociateCommand = SWLOR.NWN.API.NWScript.Enum.Associate.Command;
using static SWLOR.NWN.API.NWScript.NWScript;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// Unit tests of the companion control instance and of its facade. The world is the AI bench (areas, positions,
/// masters, hostility, hit points) with the companion-specific engine answers added; the AI instance is a scripted
/// stand-in; the unconverted static services (activity, abilities, perks, combat) are answered by
/// <see cref="TestCompanionControl"/>; the clock is the test's.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CompanionControlServiceTests
{
    private const uint Invalid = AIBench.Invalid;

    private AIBench _bench = null!;
    private ScriptedProxy _aiScript = null!;
    private TestCompanionControl _service = null!;
    private FakePlayerPlugin _player = null!;
    private IDisposable _playerScope = null!;
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private uint _area;
    private AISim _master = null!;
    private AISim _companion = null!;
    private AISim _enemy = null!;
    private int _listenPattern;
    private uint _lastHostileActor = Invalid;
    private uint _goingToBeAttackedBy = Invalid;
    private bool _lastSpellHarmful;
    private uint _lastSpellCaster = Invalid;
    private readonly Dictionary<uint, uint> _attackTargets = new();
    private readonly Dictionary<uint, uint> _associates = new();
    private readonly HashSet<(uint Target, uint Observer)> _unseen = new();
    private readonly List<(AITriggerType Trigger, uint Target, bool Bypass, uint Creature)> _triggers = new();
    private readonly List<uint> _cleared = new();
    private bool _actionIssued;
    private string? _supportAbility;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _bench = new AIBench();
        _player = new FakePlayerPlugin();
        _playerScope = PlayerPluginRuntime.Override(_player);
        _area = _bench.AddArea("field");
        _master = _bench.AddPlayer("Master", _area);
        _companion = _bench.AddCreature("Droid", _area, 1f);
        _companion.Master = _master.Id;
        _enemy = _bench.AddCreature("Raider", _area, 6f);
        _bench.MakeEnemies(_companion.Id, _enemy.Id);
        _bench.LastSpeaker = _master.Id;
        _bench.Self = _master.Id;
        _listenPattern = (int)AssociateCommand.FollowMaster;
        _lastHostileActor = Invalid;
        _goingToBeAttackedBy = Invalid;
        _lastSpellHarmful = false;
        _lastSpellCaster = Invalid;
        _attackTargets.Clear();
        _associates.Clear();
        _associates[_master.Id] = _companion.Id;
        _unseen.Clear();
        _triggers.Clear();
        _cleared.Clear();
        _actionIssued = false;
        _supportAbility = null;
        InstallEngine();

        var ai = ScriptedProxy.Create<IAIService>(out _aiScript);
        _aiScript.On("ClearAIState", arguments => { _cleared.Add((uint)arguments[0]!); return null; });
        _aiScript.On("ProcessTrigger", arguments =>
        {
            _triggers.Add(((AITriggerType)arguments[1]!, (uint)arguments[2]!, (bool)arguments[3]!, (uint)arguments[0]!));
            return _actionIssued;
        });
        _aiScript.On("TryUseBestTargetedSupportAbility", arguments =>
        {
            arguments[2] = _supportAbility ?? string.Empty;
            return _supportAbility != null;
        });

        _service = new TestCompanionControl(ai, _bench.BeastMastery);
        _service.Droids.Add(_companion.Id);
        _bench.Creatures.SetReturnHandler("GetFeatCount", arguments =>
            _service.Feats.TryGetValue((uint)arguments[0]!, out var feats) ? feats.Count : 0);
        _bench.Creatures.SetReturnHandler("GetFeatByIndex", arguments =>
            _service.Feats[(uint)arguments[0]!][(int)arguments[1]!]);
    }

    [TearDown]
    public void TearDown()
    {
        _playerScope.Dispose();
        _bench.Dispose();
        _facadeScope.Dispose();
    }

    private void InstallEngine()
    {
        var engine = _bench.Engine;
        engine.SetReturnHandler("GetListenPatternNumber", _ => _listenPattern);
        engine.SetReturnHandler("GetLastHostileActor", _ => _lastHostileActor);
        engine.SetReturnHandler("GetGoingToBeAttackedBy", _ => _goingToBeAttackedBy);
        engine.SetReturnHandler("GetLastSpellHarmful", _ => _lastSpellHarmful);
        engine.SetReturnHandler("GetLastSpellCaster", _ => _lastSpellCaster);
        engine.SetReturnHandler("GetAttackTarget", arguments =>
            _attackTargets.TryGetValue((uint)arguments[0]!, out var target) ? target : Invalid);
        engine.SetReturnHandler("GetAssociate", arguments =>
            _associates.TryGetValue((uint)arguments[1]!, out var associate) ? associate : Invalid);
        engine.SetReturnHandler("GetObjectSeen", arguments => !_unseen.Contains(((uint)arguments[0]!, (uint)arguments[1]!)));
        engine.SetReturnHandler("GetNearestCreature", arguments =>
        {
            var source = (uint)arguments[2]!;
            var nth = (int)arguments[3]!;
            var sourceSim = _bench.Get(source);
            var enemies = EnemiesOf(source)
                .OrderBy(sim => System.Numerics.Vector3.Distance(sim.Position, sourceSim.Position))
                .ToList();
            return nth >= 1 && nth <= enemies.Count ? enemies[nth - 1].Id : Invalid;
        });
    }

    private IEnumerable<AISim> EnemiesOf(uint source)
    {
        var enemies = new List<AISim>();
        for (var id = 1000u; id < 1100u; id++)
        {
            if (_bench.IsValid(id) && id != source && !_bench.Get(id).IsArea &&
                _bench.Engine.GetIsReactionTypeHostile(id, source) && !_bench.Get(id).Dead)
            {
                enemies.Add(_bench.Get(id));
            }
        }

        return enemies;
    }

    private FakeNWScript Engine => _bench.Engine;

    private List<string> Messages() =>
        Engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private void Register(bool preserve = false)
    {
        _service.Initialize(_companion.Id, preserve);
        Engine.ClearCalls();
        _triggers.Clear();
        _service.InterruptedActivations.Clear();
    }

    private void Handle(AssociateCommand command) => _listenPattern = (int)command;

    private void Say(AssociateCommand command)
    {
        Handle(command);
        _service.HandleConversation(_companion.Id);
    }

    // ----- registry -----

    [Test]
    public void A_companion_is_controlled_only_with_a_player_master_and_a_droid_or_a_beast_body()
    {
        _service.IsControlledCompanion(Invalid).Should().BeFalse();
        _service.IsControlledCompanion(_master.Id).Should().BeFalse("it has no master");
        _service.IsControlledCompanion(_companion.Id).Should().BeTrue();

        _service.Droids.Clear();
        _service.IsControlledCompanion(_companion.Id).Should().BeFalse("it is neither a droid nor a beast");
        _bench.PlayerBeasts.Add(_companion.Id);
        _service.IsControlledCompanion(_companion.Id).Should().BeTrue();

        _companion.Master = _enemy.Id;
        _service.IsControlledCompanion(_companion.Id).Should().BeFalse("its master is not a player");
    }

    [Test]
    public void Initializing_registers_the_companion_and_sends_it_to_follow_its_master()
    {
        _service.IsRegisteredCompanion(_companion.Id).Should().BeFalse();

        _service.Initialize(_companion.Id);

        _service.IsRegisteredCompanion(_companion.Id).Should().BeTrue();
        Engine.CountOf("SetAssociateListenPatterns").Should().Be(1);
        ExpectFollowIssued(2f, cleared: true);
    }

    [Test]
    public void Initializing_can_keep_the_action_queue()
    {
        _service.Initialize(_companion.Id, true);

        ExpectFollowIssued(2f, cleared: false);
    }

    private void ExpectFollowIssued(float distance, bool cleared)
    {
        var assigned = _bench.AssignedTo(_companion.Id);
        assigned.Should().HaveCount(1);
        Engine.ClearCalls();
        assigned[0]();
        Engine.CountOf("ClearAllActions").Should().Be(cleared ? 1 : 0);
        var follow = Engine.CallsTo("ActionForceFollowObject").Single();
        follow.Arguments[0].Should().Be(_master.Id);
        follow.Arguments[1].Should().Be(distance);
    }

    [Test]
    public void Initializing_something_that_is_not_a_controlled_companion_does_nothing()
    {
        _service.Droids.Clear();

        _service.Initialize(_companion.Id);

        _service.IsRegisteredCompanion(_companion.Id).Should().BeFalse();
        Engine.CountOf("SetAssociateListenPatterns").Should().Be(0);
    }

    [Test]
    public void Clearing_forgets_the_companion_and_its_ai_state()
    {
        Register();

        _service.Clear(_companion.Id);

        _service.IsRegisteredCompanion(_companion.Id).Should().BeFalse();
        _cleared.Should().Equal(_companion.Id);
    }

    [Test]
    public void The_mode_defaults_to_follow_and_asking_for_it_registers_the_companion()
    {
        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Follow);

        _service.IsRegisteredCompanion(_companion.Id).Should().BeTrue();
    }

    [Test]
    public void Abilities_are_enabled_for_an_unregistered_companion_and_by_default()
    {
        _service.AreAbilitiesEnabled(_companion.Id).Should().BeTrue();
        Register();
        _service.AreAbilitiesEnabled(_companion.Id).Should().BeTrue();
    }

    // ----- conversation commands -----

    [Test]
    public void Commands_from_someone_other_than_the_master_or_for_a_stranger_are_ignored()
    {
        Register();
        Handle(AssociateCommand.GuardMaster);

        _bench.LastSpeaker = _enemy.Id;
        _service.HandleConversation(_companion.Id).Should().BeFalse();

        _bench.LastSpeaker = _master.Id;
        _service.Droids.Clear();
        _service.HandleConversation(_companion.Id).Should().BeFalse();
        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Follow);
    }

    [Test]
    public void A_listen_pattern_that_is_not_an_associate_command_is_not_handled()
    {
        Register();
        _listenPattern = 5;

        _service.HandleConversation(_companion.Id).Should().BeFalse();
    }

    [Test]
    public void Commands_the_stock_ai_handles_are_not_claimed()
    {
        Register();
        Handle(AssociateCommand.ToggleStealth);

        _service.HandleConversation(_companion.Id).Should().BeFalse();
    }

    [TestCase(AssociateCommand.FollowMaster, CompanionMode.Follow, "Following you.", 2f)]
    [TestCase(AssociateCommand.GuardMaster, CompanionMode.Guard, "Guarding you.", 3f)]
    public void Follow_and_guard_set_the_mode_and_follow_at_their_distance(
        AssociateCommand command, CompanionMode mode, string response, float distance)
    {
        Register();
        Handle(command);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        _service.GetMode(_companion.Id).Should().Be(mode);
        Messages().Should().Equal($"Droid: {response}");
        _service.InterruptedActivations.Should().Equal(_companion.Id);
        var assigned = _bench.AssignedTo(_companion.Id);
        assigned.Should().HaveCount(2, "one clears the queue and one follows");
        Engine.ClearCalls();
        assigned.ForEach(action => action());
        Engine.CallsTo("ActionForceFollowObject").Single().Arguments[1].Should().Be(distance);
    }

    [Test]
    public void Stand_ground_holds_the_position_without_following()
    {
        Register();
        Handle(AssociateCommand.StandGround);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        _service.GetMode(_companion.Id).Should().Be(CompanionMode.StandGround);
        Messages().Should().Equal("Droid: Holding this position.");
        _bench.AssignedTo(_companion.Id).Should().HaveCount(1, "only the clear");
    }

    [Test]
    public void Attack_nearest_with_no_enemy_in_sight_returns_to_follow_and_says_so()
    {
        Register();
        _unseen.Add((_enemy.Id, _companion.Id));
        Handle(AssociateCommand.AttackNearest);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: No visible enemy is within 15 meters.");
        _triggers.Should().BeEmpty();
    }

    [Test]
    public void Attack_nearest_picks_the_closest_visible_enemy_and_fights_it_at_once()
    {
        Register();
        var farEnemy = _bench.AddCreature("Far Raider", _area, 12f);
        _bench.MakeEnemies(_companion.Id, farEnemy.Id);
        Handle(AssociateCommand.AttackNearest);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: Attacking Raider, then returning to Follow.");
        _triggers.Should().ContainSingle().Which.Should().Be((AITriggerType.CombatRound, _enemy.Id, true, _companion.Id));
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
    }

    [Test]
    public void Attack_nearest_gives_up_on_enemies_beyond_its_range()
    {
        Register();
        _enemy.Position = new System.Numerics.Vector3(30f, 0f, 0f);
        Handle(AssociateCommand.AttackNearest);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: No visible enemy is within 15 meters.");
    }

    [Test]
    public void Attack_nearest_skips_an_enemy_far_from_the_master()
    {
        Register();
        _master.Position = new System.Numerics.Vector3(-20f, 0f, 0f);
        _companion.Position = new System.Numerics.Vector3(-10f, 0f, 0f);
        _enemy.Position = new System.Numerics.Vector3(0f, 0f, 0f);
        Handle(AssociateCommand.AttackNearest);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: No visible enemy is within 15 meters.");
    }

    [Test]
    public void Healing_with_a_full_health_master_says_so_and_goes_back_to_following()
    {
        Register();
        Handle(AssociateCommand.HealMaster);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: You do not need healing.");
    }

    [Test]
    public void Healing_needs_the_abilities_to_be_enabled()
    {
        Register();
        _master.HitPoints = 10;
        Say(AssociateCommand.ToggleAbilities);
        Engine.ClearCalls();
        Handle(AssociateCommand.HealMaster);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: Unable to heal: abilities are disabled.");
    }

    [Test]
    public void Healing_uses_the_best_support_ability_when_one_is_ready()
    {
        Register();
        _master.HitPoints = 10;
        _supportAbility = "Medical Probe";
        Handle(AssociateCommand.HealMaster);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: Using Medical Probe on you, then returning to Follow.");
    }

    [Test]
    public void Healing_without_a_ready_ability_says_so()
    {
        Register();
        _master.HitPoints = 10;
        Handle(AssociateCommand.HealMaster);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        Messages().Should().Equal("Droid: Unable to heal you: no compatible healing ability is ready.");
    }

    [Test]
    public void Toggling_abilities_switches_them_off_and_back_on()
    {
        Register();
        Handle(AssociateCommand.ToggleAbilities);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        _service.AreAbilitiesEnabled(_companion.Id).Should().BeFalse();
        _service.Dequeued.Should().Equal(_companion.Id);
        Messages().Should().Equal("Droid: Abilities disabled; basic attacks only.");

        Engine.ClearCalls();
        _service.HandleConversation(_companion.Id).Should().BeTrue();

        _service.AreAbilitiesEnabled(_companion.Id).Should().BeTrue();
        _service.Dequeued.Should().HaveCount(1);
        Messages().Should().Equal("Droid: Abilities enabled.");
    }

    [Test]
    public void The_master_being_attacked_registers_a_defensive_threat_from_the_event()
    {
        Register();
        _lastHostileActor = _enemy.Id;
        Handle(AssociateCommand.MasterUnderAttack);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        _triggers.Should().ContainSingle().Which.Target.Should().Be(_enemy.Id);

        _triggers.Clear();
        _goingToBeAttackedBy = _enemy.Id;
        Handle(AssociateCommand.MasterGoingtobeAttacked);
        _service.HandleConversation(_companion.Id).Should().BeTrue();
        _triggers.Should().ContainSingle().Which.Target.Should().Be(_enemy.Id);
    }

    [Test]
    public void The_master_attacking_something_registers_an_owner_assist_target()
    {
        Register();
        _attackTargets[_master.Id] = _enemy.Id;
        Handle(AssociateCommand.MasterAttackedOther);

        _service.HandleConversation(_companion.Id).Should().BeTrue();

        _triggers.Should().ContainSingle().Which.Target.Should().Be(_enemy.Id);
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
    }

    [TestCase(AssociateCommand.MasterFailedLockpick)]
    [TestCase(AssociateCommand.MasterSawTrap)]
    [TestCase(AssociateCommand.PickLock)]
    [TestCase(AssociateCommand.DisarmTrap)]
    public void Lock_and_trap_commands_begin_an_explicit_order_and_leave_the_rest_to_the_stock_ai(AssociateCommand command)
    {
        Register();
        Handle(command);

        _service.HandleConversation(_companion.Id).Should().BeFalse();

        _service.InterruptedActivations.Should().Equal(_companion.Id);
    }

    // ----- threats and assists -----

    [Test]
    public void A_defensive_threat_that_is_not_a_valid_hostile_is_ignored()
    {
        Register();

        _service.RegisterDefensiveThreat(_companion.Id, Invalid);
        _service.RegisterDefensiveThreat(_companion.Id, _companion.Id);
        _enemy.HitPoints = 0;
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        var neutral = _bench.AddCreature("Neutral", _area);
        _service.RegisterDefensiveThreat(_companion.Id, neutral.Id);
        _enemy.HitPoints = 10;
        _enemy.Area = _bench.AddArea("elsewhere");
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        _service.Droids.Clear();
        _enemy.Area = _area;
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);

        _triggers.Should().BeEmpty();
    }

    [Test]
    public void A_defensive_threat_starts_a_combat_round_that_bypasses_the_decision_throttle()
    {
        Register();
        _actionIssued = true;

        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);

        _triggers.Should().ContainSingle().Which.Should().Be((AITriggerType.CombatRound, _enemy.Id, true, _companion.Id));
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
    }

    [Test]
    public void A_defensive_threat_does_not_interrupt_an_attack_nearest_target()
    {
        Register();
        Say(AssociateCommand.AttackNearest);
        _triggers.Clear();
        var second = _bench.AddCreature("Second", _area, 3f);
        _bench.MakeEnemies(_companion.Id, second.Id);

        _service.RegisterDefensiveThreat(_companion.Id, second.Id);

        _triggers.Should().BeEmpty();
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id, "the attack nearest order comes first");
    }

    [Test]
    public void A_timed_out_target_that_threatens_again_has_its_progress_reset()
    {
        Register();
        _actionIssued = true;
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        _service.Now = _service.Now.AddSeconds(30);

        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
    }

    [Test]
    public void An_owner_attack_assigns_the_target_and_runs_a_combat_round()
    {
        Register();

        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);

        _triggers.Should().ContainSingle().Which.Should().Be((AITriggerType.CombatRound, _enemy.Id, false, _companion.Id));
    }

    [Test]
    public void An_owner_attack_is_ignored_for_a_stranger_an_invalid_target_or_during_attack_nearest()
    {
        Register();
        _service.RegisterOwnerAttack(_companion.Id, Invalid);
        _triggers.Should().BeEmpty();

        Say(AssociateCommand.AttackNearest);
        _triggers.Clear();
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _triggers.Should().BeEmpty();

        _service.Droids.Clear();
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _triggers.Should().BeEmpty();
    }

    // ----- explicit orders and combat rounds -----

    [Test]
    public void An_explicit_order_holds_back_combat_rounds_while_the_companion_is_busy_with_it()
    {
        Register();
        _companion.Action = ActionType.MoveToPoint;

        _service.BeginExplicitOrder(_companion.Id);
        _service.ProcessCombatRound(_companion.Id);

        _triggers.Should().BeEmpty();
        _service.InterruptedActivations.Should().Equal(_companion.Id);

        _service.Now = _service.Now.AddSeconds(31);
        _service.ProcessCombatRound(_companion.Id);
        _triggers.Should().ContainSingle();
    }

    [Test]
    public void An_explicit_order_can_be_cancelled_and_needs_a_controlled_companion()
    {
        Register();
        _companion.Action = ActionType.MoveToPoint;
        _service.BeginExplicitOrder(_companion.Id, 0.1f);
        _service.CancelExplicitOrder(_companion.Id);
        _service.CancelExplicitOrder(_master.Id);

        _service.ProcessCombatRound(_companion.Id);
        _triggers.Should().ContainSingle();

        _service.Droids.Clear();
        _service.InterruptedActivations.Clear();
        _service.BeginExplicitOrder(_companion.Id);
        _service.InterruptedActivations.Should().BeEmpty();
    }

    [Test]
    public void A_combat_round_does_nothing_for_a_stranger_or_a_busy_companion()
    {
        Register();
        _service.Busy.Add(_companion.Id);
        _service.ProcessCombatRound(_companion.Id);
        _triggers.Should().BeEmpty();

        _service.Busy.Clear();
        _service.Droids.Clear();
        _service.ProcessCombatRound(_companion.Id);
        _triggers.Should().BeEmpty();
    }

    [Test]
    public void A_combat_round_with_no_target_sends_a_wandering_companion_back_to_follow()
    {
        Register();
        _companion.Action = ActionType.MoveToPoint;

        _service.ProcessCombatRound(_companion.Id);

        _triggers.Should().ContainSingle().Which.Target.Should().Be(Invalid);
        _bench.AssignedTo(_companion.Id).Should().HaveCount(1);
    }

    [Test]
    public void A_combat_round_with_no_target_leaves_a_following_companion_alone()
    {
        Register();
        _companion.Action = ActionType.Follow;

        _service.ProcessCombatRound(_companion.Id);

        _bench.AssignedTo(_companion.Id).Should().BeEmpty();
    }

    [Test]
    public void A_companion_that_stands_its_ground_stops_what_it_is_walking_to_do()
    {
        Register();
        Say(AssociateCommand.StandGround);
        Engine.ClearCalls();
        _companion.Action = ActionType.MoveToPoint;

        _service.ProcessCombatRound(_companion.Id);
        _bench.AssignedTo(_companion.Id).Should().HaveCount(1);

        Engine.ClearCalls();
        _companion.Action = ActionType.CastSpell;
        _service.ProcessCombatRound(_companion.Id);
        _bench.AssignedTo(_companion.Id).Should().BeEmpty("casting is not interrupted");
    }

    [Test]
    public void A_target_that_cannot_be_reached_in_time_is_dropped_and_the_companion_returns_to_follow()
    {
        Register();
        _actionIssued = true;
        Say(AssociateCommand.AttackNearest);
        _service.Now = _service.Now.AddSeconds(30);
        Engine.ClearCalls();

        _service.ProcessCombatRound(_companion.Id);

        Messages().Should().Contain("Droid: Unable to reach the target; returning to Follow.");
        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Follow);
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void A_defensive_target_that_cannot_be_reached_in_time_is_dropped_and_the_mode_stays()
    {
        Register();
        _actionIssued = true;
        Say(AssociateCommand.GuardMaster);
        _attackTargets[_enemy.Id] = _master.Id;
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        _service.Now = _service.Now.AddSeconds(30);
        Engine.ClearCalls();

        _service.ProcessCombatRound(_companion.Id);

        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Guard);
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
        Messages().Should().Contain("Droid: Unable to reach the target; remaining in Guard.");
    }

    [Test]
    public void An_owner_assist_target_that_cannot_be_reached_in_time_is_dropped()
    {
        Register();
        _actionIssued = true;
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.Now = _service.Now.AddSeconds(30);

        _service.ProcessCombatRound(_companion.Id);
        _service.Now = _service.Now.AddSeconds(30);
        _service.ProcessCombatRound(_companion.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void Offensive_activity_counts_as_progress_and_keeps_the_target()
    {
        Register();
        _actionIssued = true;
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.Now = _service.Now.AddSeconds(4);
        _service.OffensiveAt[_companion.Id] = _service.Now;
        _service.ProcessCombatRound(_companion.Id);
        _service.Now = _service.Now.AddSeconds(4);

        _service.ProcessCombatRound(_companion.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
    }

    [Test]
    public void A_target_out_of_the_tether_is_no_longer_authorized()
    {
        Register();
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _enemy.Position = new System.Numerics.Vector3(40f, 0f, 0f);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void A_threat_that_is_no_longer_attacking_lapses_after_the_timeout()
    {
        Register();
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        _attackTargets[_enemy.Id] = _master.Id;
        _service.ProcessCombatRound(_companion.Id);
        _attackTargets.Remove(_enemy.Id);
        _service.Now = _service.Now.AddSeconds(10);

        _service.ProcessCombatRound(_companion.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void The_threat_attacking_the_master_comes_before_a_closer_one()
    {
        Register();
        var close = _bench.AddCreature("Close", _area, 2f);
        _bench.MakeEnemies(_companion.Id, close.Id);
        _service.RegisterDefensiveThreat(_companion.Id, close.Id);
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        _attackTargets[_enemy.Id] = _master.Id;

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
        _service.ProcessCombatRound(_companion.Id);
        _triggers.Last().Target.Should().Be(_enemy.Id);
    }

    [Test]
    public void A_pending_defensive_reaction_is_processed_once()
    {
        Register();
        _service.TryProcessPendingDefensiveReaction(_master.Id).Should().BeFalse();
        _service.TryProcessPendingDefensiveReaction(_companion.Id).Should().BeFalse();

        _service.Busy.Add(_companion.Id);
        _service.RegisterDefensiveThreat(_companion.Id, _enemy.Id);
        _service.Busy.Clear();

        _service.TryProcessPendingDefensiveReaction(_companion.Id).Should().BeTrue();
        _triggers.Should().ContainSingle().Which.Bypass.Should().BeTrue();
        _service.TryProcessPendingDefensiveReaction(_companion.Id).Should().BeFalse();
    }

    // ----- resting -----

    [Test]
    public void A_companion_resting_with_an_active_master_leaves_the_rest_status_effect()
    {
        Register();
        _service.BusyTypes[_companion.Id] = ActivityStatusType.Resting;
        _service.RestEffect.Add(_companion.Id);

        _service.ProcessCombatRound(_companion.Id);

        _service.RestEffect.Should().BeEmpty();
        _service.ClearedBusy.Should().BeEmpty();
    }

    [Test]
    public void A_companion_resting_without_the_status_effect_has_its_busy_state_cleared()
    {
        Register();
        _service.BusyTypes[_companion.Id] = ActivityStatusType.Resting;

        _service.ProcessCombatRound(_companion.Id);

        _service.ClearedBusy.Should().Equal(_companion.Id);
    }

    [Test]
    public void A_companion_resting_with_its_master_keeps_resting()
    {
        Register();
        _service.BusyTypes[_companion.Id] = ActivityStatusType.Resting;
        _service.BusyTypes[_master.Id] = ActivityStatusType.Resting;

        _service.ProcessCombatRound(_companion.Id);

        _service.ClearedBusy.Should().BeEmpty();
    }

    // ----- heartbeat and areas -----

    [Test]
    public void The_heartbeat_forgets_a_companion_that_is_no_longer_controlled()
    {
        Register();
        _service.Droids.Clear();

        _service.ProcessHeartbeat(_companion.Id);

        _service.IsRegisteredCompanion(_companion.Id).Should().BeFalse();
        _cleared.Should().Equal(_companion.Id);
    }

    [Test]
    public void The_heartbeat_runs_a_combat_round_and_resets_the_mode_when_the_master_changes_area()
    {
        Register();
        Say(AssociateCommand.GuardMaster);
        _triggers.Clear();

        _service.ProcessHeartbeat(_companion.Id);
        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Guard);
        _triggers.Should().ContainSingle();

        _master.Area = _bench.AddArea("other");
        _service.ProcessHeartbeat(_companion.Id);

        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Follow);
    }

    [Test]
    public void Resuming_the_mode_position_sends_a_wandering_companion_back_to_following()
    {
        Register();
        _companion.Action = ActionType.MoveToPoint;

        _service.ResumeModePosition(_companion.Id);

        _bench.AssignedTo(_companion.Id).Should().HaveCount(1);

        Engine.ClearCalls();
        _service.Droids.Clear();
        _service.ResumeModePosition(_companion.Id);
        _bench.AssignedTo(_companion.Id).Should().BeEmpty();
    }

    [Test]
    public void Resuming_the_mode_position_waits_while_busy()
    {
        Register();
        _companion.Action = ActionType.MoveToPoint;
        _service.Busy.Add(_companion.Id);

        _service.ResumeModePosition(_companion.Id);

        _bench.AssignedTo(_companion.Id).Should().BeEmpty();
    }

    [Test]
    public void The_follow_order_does_nothing_when_the_master_is_gone()
    {
        Register();
        _service.Droids.Add(_master.Id);
        _companion.Master = Invalid;
        _companion.Action = ActionType.MoveToPoint;

        _service.ResumeModePosition(_companion.Id);

        _bench.AssignedTo(_companion.Id).Should().BeEmpty();
    }

    [Test]
    public void Resetting_the_companion_of_an_invalid_master_does_nothing()
    {
        Register();

        _service.ResetOwnerCompanionToFollow(Invalid);

        Messages().Should().BeEmpty();
        _bench.AssignedTo(_companion.Id).Should().BeEmpty();
    }

    [Test]
    public void Leaving_an_area_sends_the_masters_companion_back_to_follow()
    {
        Register();
        Say(AssociateCommand.GuardMaster);

        _service.ResetOwnerCompanionToFollow(_master.Id);

        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Follow);

        Say(AssociateCommand.GuardMaster);
        _associates[_master.Id] = _enemy.Id;
        _service.ResetOwnerCompanionToFollow(_master.Id);
        _service.GetMode(_companion.Id).Should().Be(CompanionMode.Guard, "the associate is not a controlled companion");
    }

    // ----- ability authorization -----

    private static AbilityDetail Ability(bool area = false, AbilityTargetingDetail? targeting = null, float maxRange = 0f) =>
        new()
        {
            Name = "Test",
            IsAreaAbility = area,
            Targeting = targeting,
            MaxRange = maxRange,
            IsHostileAbility = true
        };

    private static AbilityTargetingDetail Targeting(AbilityTargetingShapeType shape, AbilityTargetingFlags flags, float size = 5f) =>
        new(default, shape, size, size, flags, null!);

    [Test]
    public void A_hostile_ability_needs_an_authorized_target()
    {
        _service.ResolveHostileAbilityTarget(_companion.Id, Ability(), Invalid, _enemy.Id).Should().Be(Invalid);
        _service.ResolveHostileAbilityTarget(_companion.Id, Ability(), _enemy.Id, _enemy.Id).Should().Be(_enemy.Id);
        _service.ResolveHostileAbilityTarget(_companion.Id, Ability(), _enemy.Id, _master.Id).Should().Be(Invalid);
    }

    [Test]
    public void A_self_origin_area_is_authorized_only_inside_its_reach_and_the_same_area()
    {
        var ability = Ability(true, Targeting(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.OriginOnSelf, 10f));
        _service.ResolveHostileAbilityTarget(_companion.Id, ability, _enemy.Id, _companion.Id).Should().Be(_companion.Id);

        _enemy.Position = new System.Numerics.Vector3(30f, 0f, 0f);
        _service.ResolveHostileAbilityTarget(_companion.Id, ability, _enemy.Id, _companion.Id).Should().Be(Invalid);

        _enemy.Position = new System.Numerics.Vector3(3f, 0f, 0f);
        _enemy.Area = _bench.AddArea("away");
        _service.ResolveHostileAbilityTarget(_companion.Id, ability, _enemy.Id, _companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void An_area_ability_selected_on_the_companion_follows_the_targeting_rules()
    {
        _service.ResolveHostileAbilityTarget(_companion.Id, Ability(true, null, 10f), _enemy.Id, _companion.Id)
            .Should().Be(_companion.Id, "no targeting: the declared max range decides");
        _service.ResolveHostileAbilityTarget(_companion.Id, Ability(true, null, 2f), _enemy.Id, _companion.Id)
            .Should().Be(Invalid);

        var cone = Ability(true, Targeting(AbilityTargetingShapeType.Cone, AbilityTargetingFlags.None));
        _service.ResolveHostileAbilityTarget(_companion.Id, cone, _enemy.Id, _companion.Id).Should().Be(Invalid);

        var placed = Ability(true, Targeting(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.None));
        _service.ResolveHostileAbilityTarget(_companion.Id, placed, _enemy.Id, _companion.Id).Should().Be(Invalid);

        _enemy.Area = _bench.AddArea("away");
        _service.ResolveHostileAbilityTarget(_companion.Id, Ability(true, null, 10f), _enemy.Id, _companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void Target_authorization_follows_the_peeked_target()
    {
        Register();
        var ability = Ability();

        _service.IsHostileAbilityTargetAuthorized(_companion.Id, ability, _enemy.Id).Should().BeFalse();

        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.IsHostileAbilityTargetAuthorized(_companion.Id, ability, _enemy.Id).Should().BeTrue();
        _service.IsHostileAbilityTargetAuthorized(_companion.Id, ability, _master.Id).Should().BeFalse();
    }

    [Test]
    public void An_attack_command_is_allowed_for_an_unregistered_companion_and_only_at_the_authorized_target()
    {
        _service.CanIssueAttackCommand(_companion.Id, _enemy.Id).Should().BeTrue();

        Register();
        _service.CanIssueAttackCommand(_companion.Id, _enemy.Id).Should().BeFalse();

        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.CanIssueAttackCommand(_companion.Id, _enemy.Id).Should().BeTrue();
        _service.CanIssueAttackCommand(_companion.Id, _master.Id).Should().BeFalse();
    }

    // ----- stand ground -----

    private void StandGround()
    {
        Register();
        Say(AssociateCommand.StandGround);
        _service.WeaponRange = 2f;
        Engine.ClearCalls();
        _triggers.Clear();
    }

    [Test]
    public void Standing_ground_authorizes_a_target_inside_weapon_reach_only()
    {
        StandGround();
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid, "the enemy is 5 meters away, beyond reach 2.25");

        _service.WeaponRange = 20f;
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
        _service.CanIssueAttackCommand(_companion.Id, _enemy.Id).Should().BeTrue();

        _service.WeaponRange = 1f;
        _service.CanIssueAttackCommand(_companion.Id, _enemy.Id).Should().BeFalse();
    }

    [Test]
    public void Standing_ground_authorizes_a_target_a_hostile_ability_can_reach()
    {
        StandGround();
        var feat = FeatType.Invalid + 1;
        _service.Feats[_companion.Id] = new List<FeatType> { feat, feat + 1, feat + 2, feat + 3, feat + 4 };
        _service.Registered[feat] = Ability();
        _service.Registered[feat + 1] = Ability(true, Targeting(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.OriginOnSelf, 20f));
        _service.Registered[feat + 2] = Ability(true, null);
        var placed = Ability(true, Targeting(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.None), 2f);
        placed.HasExplicitMaxRange = true;
        _service.Registered[feat + 3] = placed;
        _service.Registered[feat + 4] = new AbilityDetail { Name = "Strike", IsHostileAbility = true, ActivationType = AbilityActivationType.Weapon };
        _service.UsableFeats.Add(feat + 1);

        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);

        _service.UsableFeats.Clear();
        _service.UsableFeats.Add(feat);
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(_enemy.Id);
        _service.UsableFeats.Clear();
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void Standing_ground_skips_placed_areas_out_of_reach_and_self_areas_too_small()
    {
        StandGround();
        var feat = FeatType.Invalid + 1;
        _service.Feats[_companion.Id] = new List<FeatType> { feat, feat + 1, feat + 2 };
        var placed = Ability(true, Targeting(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.None), 2f);
        placed.HasExplicitMaxRange = true;
        _service.Registered[feat] = placed;
        _service.Registered[feat + 1] = Ability(true, Targeting(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.OriginOnSelf, 1f));
        _service.Registered[feat + 2] = Ability();
        _service.UsableFeats.UnionWith(new[] { feat, feat + 1 });
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void Standing_ground_with_abilities_disabled_relies_on_the_weapon()
    {
        StandGround();
        var feat = FeatType.Invalid + 1;
        _service.Feats[_companion.Id] = new List<FeatType> { feat };
        _service.Registered[feat] = Ability();
        _service.UsableFeats.Add(feat);
        Say(AssociateCommand.ToggleAbilities);
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);

        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
    }

    [Test]
    public void The_hostile_ability_list_is_rebuilt_only_when_the_known_feats_change()
    {
        StandGround();
        var feat = FeatType.Invalid + 1;
        _service.Feats[_companion.Id] = new List<FeatType> { feat, feat + 9 };
        _service.Registered[feat] = Ability();
        _service.UsableFeats.Add(feat);
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.AbilityLookups = 0;

        _service.PeekAuthorizedTarget(_companion.Id);
        _service.PeekAuthorizedTarget(_companion.Id);

        _service.AbilityLookups.Should().Be(0, "the list built by the owner attack is still current");

        _service.Feats[_companion.Id] = new List<FeatType> { feat };
        _service.PeekAuthorizedTarget(_companion.Id);
        _service.AbilityLookups.Should().Be(1, "the known feats changed");
    }

    [Test]
    public void The_effective_level_of_an_ability_comes_from_its_perk()
    {
        StandGround();
        var feat = FeatType.Invalid + 1;
        _service.Feats[_companion.Id] = new List<FeatType> { feat };
        var ability = Ability();
        ability.EffectiveLevelPerkType = PerkType.Invalid + 1;
        _service.Registered[feat] = ability;
        _service.UsableFeats.Add(feat);
        _service.PerkLevels[PerkType.Invalid + 1] = 3;
        _service.RegisterOwnerAttack(_companion.Id, _enemy.Id);
        _service.UsedLevels.Clear();

        _service.PeekAuthorizedTarget(_companion.Id);

        _service.UsedLevels.Should().Equal(3);
    }

    // ----- event handlers -----

    [Test]
    public void The_toggle_abilities_command_is_renamed_for_a_player_entering_the_module()
    {
        _bench.EnteringObject = _master.Id;

        CompanionControl.RenameToggleAbilitiesCommand();

        var call = _player.CallsTo("SetTlkOverride").Single();
        call.Arguments[0].Should().Be(_master.Id);
        call.Arguments[1].Should().Be(CompanionControlService.AssociateAbilitiesLabelStrRef);
        call.Arguments[2].Should().Be("abilities");

        _player.ClearCalls();
        _bench.EnteringObject = _enemy.Id;
        CompanionControl.RenameToggleAbilitiesCommand();
        _player.CallsTo("SetTlkOverride").Should().BeEmpty();
    }

    [Test]
    public void The_facade_handlers_forward_the_owner_events_to_the_instance()
    {
        // The facade resolves the container's own instance, whose registry is empty and whose companion is not a droid
        // or a beast here, so the handlers run to the end without registering anything.
        _bench.LastAttacker = _enemy.Id;
        _bench.LastDamager = _enemy.Id;
        _lastSpellCaster = _enemy.Id;
        _attackTargets[_master.Id] = _enemy.Id;
        _bench.ExitingObject = _master.Id;

        _lastSpellHarmful = true;
        CompanionControl.OnOwnerAttacked();
        CompanionControl.OnOwnerDamaged();
        CompanionControl.OnHostileSpellCastAtOwner();
        _lastSpellHarmful = false;
        CompanionControl.OnHostileSpellCastAtOwner();
        CompanionControl.OnOwnerCombatRound();
        CompanionControl.OnOwnerAreaExit();
        _bench.ExitingObject = _enemy.Id;
        CompanionControl.OnOwnerAreaExit();

        CompanionControl.IsRegisteredCompanion(_companion.Id).Should().BeFalse();
    }

    [Test]
    public void The_owner_events_register_threats_and_assists_for_the_masters_companion()
    {
        Register();
        _bench.LastAttacker = _enemy.Id;
        _service.OnOwnerAttacked();
        _triggers.Should().ContainSingle().Which.Target.Should().Be(_enemy.Id);

        _service.ResetOwnerCompanionToFollow(_master.Id);
        _triggers.Clear();
        _bench.LastDamager = _enemy.Id;
        _service.OnOwnerDamaged();
        _triggers.Should().ContainSingle();

        _service.ResetOwnerCompanionToFollow(_master.Id);
        _triggers.Clear();
        _lastSpellHarmful = true;
        _lastSpellCaster = _enemy.Id;
        _service.OnHostileSpellCastAtOwner();
        _triggers.Should().ContainSingle();

        _triggers.Clear();
        _lastSpellHarmful = false;
        _service.OnHostileSpellCastAtOwner();
        _triggers.Should().BeEmpty();

        _service.ResetOwnerCompanionToFollow(_master.Id);
        _triggers.Clear();
        _attackTargets[_master.Id] = _enemy.Id;
        _service.OnOwnerCombatRound();
        _triggers.Should().ContainSingle();

        _bench.ExitingObject = _master.Id;
        _service.OnOwnerAreaExit();
        _service.PeekAuthorizedTarget(_companion.Id).Should().Be(Invalid);
        _bench.ExitingObject = _enemy.Id;
        _service.OnOwnerAreaExit();
    }

    // ----- construction -----

    [Test]
    public void The_container_builds_the_instance_from_the_converted_services_without_touching_the_engine()
    {
        var engine = new TimelineNWScript();
        using var scope = SWLOR.NWN.API.NWScript.NWScriptRuntime.Override(engine);
        using var container = new FacadeContainer();

        container.Get<ICompanionControlService>().Should().BeOfType<CompanionControlService>();
        container.Get<ICompanionControlService>().Should().BeSameAs(container.Get<ICompanionControlService>());
        engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_facade_constant_is_the_instance_constant()
    {
        CompanionControlService.AssociateAbilitiesLabelStrRef.Should().Be(CompanionControlService.AssociateAbilitiesLabelStrRef);
    }
}

/// <summary>
/// The companion control instance with the calls into the services that are not converted yet answered by the test,
/// and the clock set by the test.
/// </summary>
public sealed class TestCompanionControl : CompanionControlService
{
    public TestCompanionControl(IAIService ai, IBeastMasteryService beastMastery)
        : base(ai, beastMastery, FacadeForwarder.LazyFor<IAbilityService>(), FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IPlayerNameService>(), FacadeForwarder.For<IStatusEffectService>())
    {
    }

    public DateTime Now { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public HashSet<uint> Droids { get; } = new();
    public HashSet<uint> Busy { get; } = new();
    public Dictionary<uint, ActivityStatusType> BusyTypes { get; } = new();
    public HashSet<uint> RestEffect { get; } = new();
    public List<uint> ClearedBusy { get; } = new();
    public List<uint> InterruptedActivations { get; } = new();
    public List<uint> Dequeued { get; } = new();
    public Dictionary<uint, DateTime> OffensiveAt { get; } = new();
    public float WeaponRange { get; set; } = 5f;
    public Dictionary<uint, List<FeatType>> Feats { get; } = new();
    public Dictionary<FeatType, AbilityDetail> Registered { get; } = new();
    public HashSet<FeatType> UsableFeats { get; } = new();
    public Dictionary<PerkType, int> PerkLevels { get; } = new();
    public List<int> UsedLevels { get; } = new();
    public int AbilityLookups { get; set; }

    protected override DateTime UtcNow() => Now;

    protected override bool IsDroid(uint creature) => Droids.Contains(creature);

    protected override bool IsBusy(uint target) => Busy.Contains(target);

    protected override ActivityStatusType GetBusyType(uint target) =>
        BusyTypes.GetValueOrDefault(target, ActivityStatusType.Invalid);

    protected override void ClearBusy(uint target) => ClearedBusy.Add(target);

    protected override bool HasRestStatusEffect(uint creature) => RestEffect.Contains(creature);

    protected override void RemoveRestStatusEffect(uint creature) => RestEffect.Remove(creature);

    protected override bool InterruptAbilityActivation(uint activator)
    {
        InterruptedActivations.Add(activator);
        return false;
    }

    protected override void DequeueWeaponAbility(uint creature) => Dequeued.Add(creature);

    protected override string GetDisplayName(uint observer, uint target) => GetName(target);

    protected override DateTime GetLastOffensiveActivityAt(uint creature) =>
        OffensiveAt.GetValueOrDefault(creature);

    protected override SkillType GetEquippedWeaponSkillType(uint creature) => SkillType.Invalid;

    protected override float GetWeaponEngagementRange(SkillType skill) => WeaponRange;

    protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevels.GetValueOrDefault(perkType);

    protected override bool IsFeatRegistered(FeatType feat) => Registered.ContainsKey(feat);

    protected override AbilityDetail GetAbilityDetail(FeatType feat)
    {
        AbilityLookups++;
        return Registered[feat];
    }

    protected override bool CanUseAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation)
    {
        UsedLevels.Add(effectiveLevel);
        return UsableFeats.Contains(feat);
    }
}
