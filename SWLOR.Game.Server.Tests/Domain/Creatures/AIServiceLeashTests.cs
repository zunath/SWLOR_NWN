#nullable enable
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The combat leash of the AI instance: when an enemy that returns home resets because its target left the leash,
/// how the evade runs and ends, and what an evading creature ignores. The enemy stands inside its leash; its home is
/// the origin of the fight area.
/// </summary>
[TestFixture]
public class AIServiceLeashTests : AIServiceTestBase
{
    private const string Active = "AI_LEASH_EVADE_ACTIVE";
    private const string RestorePlot = "AI_LEASH_EVADE_RESTORE_PLOT_FLAG";
    private const string RestoreMovement = "AI_LEASH_EVADE_RESTORE_MOVEMENT_RATE";
    private const string Queued = "AI_LEASH_EVADE_RETURN_QUEUED";

    private AISim _fighter = null!;
    private AISim _respawner = null!;
    private uint _medicalCenter;

    [SetUp]
    public void PlaceTheFight()
    {
        Flags(AIFlag.ReturnHome);
        Npc.Position = new Vector3(5f, 0f, 0f);
        _fighter = HostilePlayer("fighter", 8f);
        _medicalCenter = Bench.AddArea("medical center");
        _respawner = Bench.AddPlayer("respawner", _medicalCenter);
    }

    private bool Evading => Bench.Locals.Int(Npc.Id, Active) != 0;

    private void Threat(AISim creature, int amount) => Service.SetEnmity(Npc.Id, creature.Id, amount);

    private bool TryStart(AISim target) => Service.TryStartCombatLeashEvade(Npc.Id, target.Id);

    private static Vector3 At(float x) => new(x, 0f, 0f);

    private void MoveBeyondTheLeash(AISim player)
    {
        player.Area = Area;
        player.Position = At(80f);
    }

    private int Notes(string prefix) => Bench.Trace().Count(note => note.StartsWith(prefix, StringComparison.Ordinal));

    [Test]
    public void A_party_member_who_respawns_elsewhere_does_not_reset_an_enemy_still_fighting_another_player()
    {
        Threat(_fighter, 40);
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeTrue("the attack on the departed player is cancelled");

        Evading.Should().BeFalse("the enemy must not reset while another player fights it inside its leash");
        Service.Tables[Npc.Id].Should().ContainKey(_fighter.Id).And.NotContainKey(_respawner.Id);
        Service.Attacked[Npc.Id].Should().Be(_fighter.Id);
    }

    [Test]
    public void An_aggro_exit_from_a_respawner_who_is_not_on_the_table_does_not_reset_the_enemy()
    {
        Threat(_fighter, 40);

        TryStart(_respawner).Should().BeTrue();

        Evading.Should().BeFalse();
        Service.Tables[Npc.Id].Should().ContainKey(_fighter.Id);
        Service.Attacked[Npc.Id].Should().Be(_fighter.Id);
    }

    [Test]
    public void A_player_who_pulls_the_enemy_past_its_leash_is_dropped_while_another_player_inside_keeps_fighting()
    {
        MoveBeyondTheLeash(_respawner);
        Threat(_fighter, 10);
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeTrue();

        Evading.Should().BeFalse();
        Service.Tables[Npc.Id].Should().ContainKey(_fighter.Id).And.NotContainKey(_respawner.Id);
        Service.Attacked[Npc.Id].Should().Be(_fighter.Id);
    }

    [Test]
    public void A_dead_entry_does_not_count_as_keeping_the_fight()
    {
        _fighter.Dead = true;
        Threat(_fighter, 40);
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeTrue();

        Evading.Should().BeTrue();
    }

    [Test]
    public void When_the_only_target_leaves_the_area_the_leash_still_resets_the_enemy()
    {
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeTrue();

        Evading.Should().BeTrue("the leash resets an enemy once nobody fighting it remains inside the leash");
    }

    [Test]
    public void When_every_target_is_beyond_the_leash_the_enemy_resets()
    {
        _fighter.Position = At(70f);
        MoveBeyondTheLeash(_respawner);
        Threat(_fighter, 10);
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeTrue();

        Evading.Should().BeTrue();
    }

    [Test]
    public void A_target_inside_the_leash_is_not_handled_by_the_leash()
    {
        Threat(_fighter, 10);

        TryStart(_fighter).Should().BeFalse();

        Evading.Should().BeFalse();
        Service.Attacked.Should().NotContainKey(Npc.Id);
        Service.Tables[Npc.Id].Should().ContainKey(_fighter.Id);
    }

    [Test]
    public void A_target_that_is_not_a_valid_object_resets_the_enemy_only_when_the_enemy_itself_is_beyond_its_leash()
    {
        Service.TryStartCombatLeashEvade(Npc.Id, Invalid).Should().BeFalse("the enemy is inside its leash");

        Npc.Position = At(60f);
        Service.TryStartCombatLeashEvade(Npc.Id, Invalid).Should().BeTrue();
        Evading.Should().BeTrue();
    }

    [Test]
    public void A_companion_whose_master_is_inside_the_leash_does_not_pull_the_enemy_past_it()
    {
        var companion = Bench.AddCreature("companion", Area, 80f);
        companion.Master = _fighter.Id;

        Service.TryStartCombatLeashEvade(Npc.Id, companion.Id).Should().BeFalse(
            "the companion is beyond the leash but its master, a player, is not, and the enemy has not left it either");
        Evading.Should().BeFalse();
    }

    [Test]
    public void A_companion_beyond_the_leash_resets_an_enemy_that_has_left_its_own_leash_too()
    {
        var companion = Bench.AddCreature("companion", Area, 80f);
        companion.Master = _fighter.Id;
        Npc.Position = At(60f);

        Service.TryStartCombatLeashEvade(Npc.Id, companion.Id).Should().BeTrue();
    }

    [Test]
    public void A_companion_whose_master_is_not_a_player_counts_as_beyond_the_leash()
    {
        var companion = Bench.AddCreature("companion", Area, 80f);
        companion.Master = Bench.AddCreature("handler", Area, 2f).Id;

        Service.TryStartCombatLeashEvade(Npc.Id, companion.Id).Should().BeTrue();
    }

    [Test]
    public void Without_the_return_home_flag_the_enemy_is_never_leashed()
    {
        Flags(AIFlag.None);
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeFalse();

        Evading.Should().BeFalse();
    }

    [Test]
    public void Without_a_hostile_player_in_its_area_the_enemy_is_not_leashed()
    {
        _fighter.Area = _medicalCenter;
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeFalse();
    }

    [Test]
    public void A_hostile_player_controlled_by_a_dm_does_not_count_for_the_leash()
    {
        Bench.DMControlled.Add(_fighter.Id);
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeFalse();
    }

    [Test]
    public void A_target_beyond_the_plain_leash_resets_the_enemy()
    {
        var sniper = HostilePlayer("sniper", 50f);
        Threat(sniper, 90);

        TryStart(sniper).Should().BeTrue("fifty meters is beyond a plain leash of forty five");
        Evading.Should().BeTrue();
    }

    [Test]
    public void The_hit_distance_of_both_creatures_keeps_a_target_just_inside_the_wider_leash()
    {
        var sniper = HostilePlayer("sniper", 50f);
        Npc.HitDistance = 3f;
        sniper.HitDistance = 3f;
        Threat(sniper, 90);

        TryStart(sniper).Should().BeFalse("with both hit distances the leash is fifty one meters");
    }

    [Test]
    public void The_leash_is_longer_than_a_standard_rifle_shot()
    {
        var sniper = HostilePlayer("sniper", 44.9f);
        Threat(sniper, 90);

        TryStart(sniper).Should().BeFalse();
    }

    [Test]
    public void Starting_the_evade_makes_the_creature_untouchable_heals_it_and_clears_its_fight()
    {
        Npc.Plot = false;
        Npc.HitPoints = 20;
        Npc.MovementRate = 7;
        Threat(_respawner, 90);

        TryStart(_respawner).Should().BeTrue();

        Bench.Locals.Int(Npc.Id, RestorePlot).Should().Be(0, "the plot flag the creature had is remembered");
        Bench.Locals.Int(Npc.Id, RestoreMovement).Should().Be(7);
        Bench.Trace("SetPlotFlag", "SetCurrentHitPoints", "ExecuteScript").Should().ContainInOrder(
            $"SetPlotFlag({Npc.Id}, true)",
            $"SetCurrentHitPoints({Npc.Id}, 100)",
            $"ClearEnmityTable({Npc.Id})",
            $"ExecuteScript({ScriptName.OnCreatureLeashStarted}, {Npc.Id})");
        Bench.Creatures.CallsTo("SetMovementRate").Last().Arguments[1].Should().Be(MovementRate.DMFast);
        Bench.Creatures.CallsTo("SetMovementRateFactor").Last().Arguments[1].Should().Be(3f);
    }

    [Test]
    public void The_fast_return_is_reapplied_shortly_after_the_evade_starts_only_while_it_lasts()
    {
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.Creatures.ClearCalls();

        var delayed = Bench.Delayed().Single();
        delayed.Seconds.Should().Be(0.2f);
        delayed.Action();
        Bench.Creatures.CountOf("SetMovementRate").Should().Be(1);

        Bench.Creatures.ClearCalls();
        Npc.Effects.Clear();
        Bench.Locals.Int(Npc.Id, Active).Should().Be(1);
        Bench.Get(Npc.Id).Destroyed = true;
        delayed.Action();
        Bench.Creatures.CountOf("SetMovementRate").Should().Be(0);
    }

    [Test]
    public void The_return_walk_is_queued_once_and_not_restarted_while_the_creature_is_still_walking()
    {
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.Locals.Int(Npc.Id, Queued).Should().Be(1);
        Bench.AssignedTo(Npc.Id).Should().HaveCount(1);

        Npc.Action = ActionType.MoveToPoint;
        Service.CreatureHeartbeat();
        Bench.AssignedTo(Npc.Id).Should().HaveCount(1, "the creature is already walking home");

        Npc.Action = ActionType.Invalid;
        Service.CreatureHeartbeat();
        Bench.AssignedTo(Npc.Id).Should().HaveCount(2, "a stopped walk is queued again");
    }

    [Test]
    public void The_queued_return_walk_clears_the_actions_walks_home_and_finishes_the_evade()
    {
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);

        Bench.RunAssigned(Npc.Id);

        Bench.Engine.CallsTo("ClearAllActions").Last().Arguments[0].Should().Be(true);
        var move = Bench.Engine.CallsTo("ActionForceMoveToLocation").Last();
        Bench.Describe((Location)move.Arguments[0]!).Should().Be((Area, At(0f)));
        move.Arguments[1].Should().Be(true);
        move.Arguments[2].Should().Be(60f);
        Bench.QueuedActions().Should().HaveCount(1);
    }

    [Test]
    public void Arriving_home_ends_the_evade_and_gives_the_creature_back_what_it_had()
    {
        Npc.Plot = true;
        Npc.MovementRate = 7;
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.RunAssigned(Npc.Id);
        Npc.Position = At(1f);
        Npc.HitPoints = 40;
        Bench.ClearCalls();

        Service.CreatureHeartbeat();

        Evading.Should().BeFalse();
        Bench.Locals.Has(Npc.Id, RestorePlot).Should().BeFalse();
        Bench.Locals.Has(Npc.Id, Queued).Should().BeFalse();
        Bench.Trace("SetPlotFlag", "SetCurrentHitPoints", "ExecuteScript").Should().ContainInOrder(
            $"SetCurrentHitPoints({Npc.Id}, 100)",
            $"SetPlotFlag({Npc.Id}, true)");
        Bench.Creatures.CallsTo("SetMovementRate").Single().Arguments[1].Should().Be((MovementRate)7);
        Bench.Trace().Should().Contain($"ApplyCreatureMovementRate({Npc.Id})");
        Bench.Engine.CallsTo("ExecuteScript").Should().ContainSingle(call => (string)call.Arguments[0]! == ScriptName.OnCreatureLeashEnded);
    }

    [Test]
    public void The_queued_completion_at_home_ends_the_evade()
    {
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.RunAssigned(Npc.Id);
        Npc.Position = At(2f);

        Bench.QueuedActions().Single()();

        Evading.Should().BeFalse();
        Bench.Locals.Has(Npc.Id, Queued).Should().BeFalse();
    }

    [Test]
    public void The_queued_completion_far_from_home_jumps_home_and_then_ends_the_evade()
    {
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.RunAssigned(Npc.Id);

        Bench.QueuedActions().First()();

        Evading.Should().BeTrue("the jump is queued and the evade ends when it finishes");
        Bench.Engine.CallsTo("ActionJumpToLocation").Should().ContainSingle();
        var finish = Bench.QueuedActions().Last();
        finish();
        Evading.Should().BeFalse();
    }

    [Test]
    public void A_completion_that_arrives_after_the_evade_ended_changes_nothing()
    {
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.RunAssigned(Npc.Id);
        Npc.Position = At(2f);
        var completion = Bench.QueuedActions().Single();
        completion();
        Bench.ClearCalls();

        completion();

        Evading.Should().BeFalse();
        Bench.Engine.CountOf("SetPlotFlag").Should().Be(0);
        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }

    [Test]
    public void A_heartbeat_far_from_home_keeps_an_evading_creature_walking()
    {
        Npc.Position = At(60f);
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.Creatures.ClearCalls();

        Service.CreatureHeartbeat();

        Evading.Should().BeTrue();
        Bench.Creatures.CountOf("SetMovementRate").Should().Be(1, "the fast return is applied again");
        Notes("AttackHighestEnmityTarget").Should().Be(1, "only the call that ends every heartbeat");
    }

    [Test]
    public void A_creature_that_stops_chasing_beyond_its_home_radius_hurries_home_like_a_leash()
    {
        Npc.Position = At(20f);

        Service.StopCombatAfterProximityLoss(Npc.Id);

        Evading.Should().BeTrue();
        Bench.Locals.Int(Npc.Id, Queued).Should().Be(1);
    }

    [Test]
    public void A_creature_that_stops_chasing_near_home_only_clears_its_actions()
    {
        Npc.Position = At(10f);

        Service.StopCombatAfterProximityLoss(Npc.Id);

        Evading.Should().BeFalse();
        Bench.AssignedTo(Npc.Id).Should().ContainSingle();
        Bench.RunAssigned(Npc.Id);
        Bench.Engine.CountOf("ClearAllActions").Should().Be(1);
    }

    [Test]
    public void A_creature_without_the_return_home_flag_that_stops_chasing_only_clears_its_actions()
    {
        Flags(AIFlag.None);
        Npc.Position = At(30f);

        Service.StopCombatAfterProximityLoss(Npc.Id);

        Evading.Should().BeFalse();
    }

    [Test]
    public void Control_removal_during_the_cleanup_cannot_restart_it_or_resume_combat()
    {
        Threat(_respawner, 90);
        var reentered = false;
        Service.WhenEnmityIsCleared = _ =>
        {
            // Removing a control effect requests another attack while the evade is still being set up.
            reentered = Service.TryStartCombatLeashEvade(Npc.Id, _respawner.Id);
        };

        TryStart(_respawner).Should().BeTrue();

        reentered.Should().BeTrue();
        Notes("ClearEnmityTable").Should().Be(1);
        Bench.Count("SetCurrentHitPoints").Should().Be(1);
        Bench.Engine.CallsTo("ExecuteScript").Count(call => (string)call.Arguments[0]! == ScriptName.OnCreatureLeashStarted).Should().Be(1);
        Bench.AssignedTo(Npc.Id).Should().HaveCount(1);
        Service.Attacked.Should().BeEmpty();
    }

    [Test]
    public void Starting_the_evade_again_while_it_is_being_set_up_does_not_repeat_the_initialization()
    {
        Npc.Plot = false;
        Npc.Position = At(20f);
        Service.WhenEnmityIsCleared = _ => Service.StopCombatAfterProximityLoss(Npc.Id);
        Threat(_respawner, 90);

        TryStart(_respawner);

        Notes("ClearEnmityTable").Should().Be(1);
        Bench.AssignedTo(Npc.Id).Should().HaveCount(1);
        Bench.Locals.Int(Npc.Id, RestorePlot).Should().Be(0, "the original non-plot flag must survive reentrant callbacks");
    }

    [Test]
    public void An_active_evade_still_blocks_attacks_when_the_target_moves_back_inside_the_leash()
    {
        Threat(_respawner, 90);
        TryStart(_respawner).Should().BeTrue();

        TryStart(_fighter).Should().BeTrue();
        Notes("ClearEnmityTable").Should().Be(1);

        var other = Bench.AddCreature("other", Area);
        Service.TryStartCombatLeashEvade(other.Id, _fighter.Id).Should().BeFalse(
            "a different creature that does not need leashing can still attack");
    }

    [Test]
    public void An_evading_creature_ignores_what_attacks_and_damages_it()
    {
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.ClearCalls();
        Bench.LastAttacker = _fighter.Id;
        Bench.LastDamager = _fighter.Id;

        Service.CreaturePhysicalAttacked();
        Service.CreatureDamaged();
        Service.CreatureAggroEnter();
        Bench.EnteringObject = _fighter.Id;
        Bench.AreaOfEffectCreator = Npc.Id;
        Service.CreatureAggroEnter();

        Bench.Trace().Should().BeEmpty("an evading creature starts no fight");
    }

    [Test]
    public void An_evading_creature_does_not_start_a_new_combat_round()
    {
        Threat(_respawner, 90);
        TryStart(_respawner);
        Bench.ClearCalls();

        Service.CreatureCombatRoundEnd();

        Bench.Trace().Should().BeEmpty();
        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }

    [Test]
    public void IsLeashEvading_is_false_for_a_creature_that_is_not_evading_and_for_an_invalid_object()
    {
        Service.IsLeashEvading(Npc.Id).Should().BeFalse();
        Service.IsLeashEvading(Invalid).Should().BeFalse();
    }

    [Test]
    public void A_dazed_creature_that_left_its_leash_still_starts_the_full_evade_before_the_idle_checks()
    {
        Npc.Effects.Add(EffectTypeScript.Dazed);
        Npc.Position = At(60f);
        Threat(_respawner, 90);

        Service.CreatureHeartbeat();

        Evading.Should().BeTrue();
        Notes("AttackHighestEnmityTarget").Should().Be(1, "only the call that ends every heartbeat");
    }

    [Test]
    public void A_creature_in_combat_without_enmity_checks_the_leash_with_no_target()
    {
        Npc.InCombat = true;
        Npc.Position = At(60f);

        Service.CreatureHeartbeat();

        Evading.Should().BeTrue();
    }
}
