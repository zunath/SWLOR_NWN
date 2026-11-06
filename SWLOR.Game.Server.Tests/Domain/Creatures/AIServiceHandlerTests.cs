#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The creature event entry points of the AI instance (the facade forwards the 18 handlers to them): what each one
/// calls, in what order, and what it leaves alone when the creature has no AI.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIServiceHandlerTests : AIServiceTestBase
{
    private AISim _enemy = null!;

    [SetUp]
    public void PlaceAnEnemy()
    {
        _enemy = HostilePlayer("enemy", 4f);
    }

    private void Threat(int amount) => Service.SetEnmity(Npc.Id, _enemy.Id, amount);

    private List<string> Notes() => Bench.Trace();

    private string Attack => $"AttackHighestEnmityTarget({Npc.Id})";

    private int Scripts(string name) => Bench.Engine.CallsTo("ExecuteScript").Count(call => (string)call.Arguments[0]! == name);

    [Test]
    public void The_heartbeat_does_nothing_for_a_creature_without_AI()
    {
        Npc.AILevel = AILevel.VeryLow;

        Service.CreatureHeartbeat();

        Bench.Engine.Calls.Where(call => call.Member != "OBJECT_SELF" && call.Member != "GetIsObjectValid" && call.Member != "GetAILevel")
            .Should().BeEmpty();
    }

    [Test]
    public void The_heartbeat_restores_stats_checks_the_idle_flags_and_then_attacks_the_highest_enmity_target()
    {
        Service.CreatureHeartbeat();

        Notes().Should().Equal("RestoreNPCStats(true)", Attack);
    }

    [Test]
    public void The_heartbeat_does_not_scan_for_aggro_targets()
    {
        Service.CreatureHeartbeat();

        Bench.Engine.CountOf("GetFirstObjectInShape").Should().Be(0);
    }

    [Test]
    public void The_heartbeat_of_a_creature_with_a_threat_attacks_it_unless_the_creature_is_talking()
    {
        Threat(10);

        Service.CreatureHeartbeat();
        Notes().Should().Equal("RestoreNPCStats(true)", Attack, Attack);

        Bench.ClearCalls();
        Npc.InConversation = true;
        Service.CreatureHeartbeat();
        Notes().Should().Equal("RestoreNPCStats(true)", Attack);
    }

    [Test]
    public void A_creature_with_a_control_effect_does_not_walk_off()
    {
        Flags(AIFlag.RandomWalk);
        Npc.Effects.Add(EffectTypeScript.Petrify);

        Service.CreatureHeartbeat();
        Bench.RunAssigned(Npc.Id);

        Bench.Engine.CountOf("ActionRandomWalk").Should().Be(0);
    }

    [Test]
    public void The_perception_handler_does_nothing_for_a_creature_without_AI()
    {
        Npc.AILevel = AILevel.VeryLow;
        var ally = Bench.AddCreature("ally", Area, 2f);
        Bench.LastPerceived = ally.Id;
        Bench.PerceptionSeen = true;

        Service.CreaturePerception();

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void The_combat_round_end_handler_does_nothing_for_a_creature_without_AI()
    {
        Npc.AILevel = AILevel.VeryLow;

        Service.CreatureCombatRoundEnd();

        Notes().Should().BeEmpty();
    }

    [Test]
    public void The_combat_round_end_runs_the_profile_and_stops_when_it_handled_the_round()
    {
        Service.CreatureCombatRoundEnd();

        Scripts(ScriptedProfiles.DefaultScript).Should().Be(1);
        Notes().Should().NotContain(Attack);
    }

    [Test]
    public void The_combat_round_end_attacks_when_the_profile_did_not_handle_the_round()
    {
        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("never").Score(0));

        Service.CreatureCombatRoundEnd();

        Notes().Should().Contain(Attack);
    }

    [Test]
    public void The_combat_round_end_of_a_busy_creature_goes_straight_to_attacking()
    {
        Bench.Engine.SetLocalBool(Npc.Id, "IS_BUSY", true);

        Service.CreatureCombatRoundEnd();

        Scripts(ScriptedProfiles.DefaultScript).Should().Be(0);
        Notes().Should().Equal(Attack);
    }

    [Test]
    public void The_combat_round_end_of_a_creature_whose_threat_left_its_leash_starts_the_evade_instead()
    {
        Flags(AIFlag.ReturnHome);
        _enemy.Position = new System.Numerics.Vector3(80f, 0f, 0f);
        Threat(10);

        Service.CreatureCombatRoundEnd();

        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
        Scripts(ScriptedProfiles.DefaultScript).Should().Be(0);
    }

    [Test]
    public void A_conversation_without_a_name_does_nothing()
    {
        Bench.LastSpeaker = _enemy.Id;

        Service.CreatureConversation();

        Bench.AssignedTo(_enemy.Id).Should().BeEmpty();
    }

    [Test]
    public void A_conversation_that_no_menu_or_graph_knows_starts_the_native_dialog_with_the_creature_as_owner()
    {
        Bench.Engine.SetLocalString(Npc.Id, "CONVERSATION", "old_man");
        Bench.LastSpeaker = _enemy.Id;

        Service.CreatureConversation();

        // The player runs the queued command, where OBJECT_SELF is the player: the owner must have been captured.
        Bench.RunAssigned(_enemy.Id);
        var start = Bench.Engine.CallsTo("ActionStartConversation").Single();
        start.Arguments[0].Should().Be(Npc.Id);
        start.Arguments[1].Should().Be("old_man");
        start.Arguments[2].Should().Be(true);
        start.Arguments[3].Should().Be(false);
    }

    [Test]
    public void Being_attacked_does_nothing_for_a_creature_without_AI_or_one_that_is_evading()
    {
        Bench.LastAttacker = _enemy.Id;
        Npc.AILevel = AILevel.VeryLow;
        Service.CreaturePhysicalAttacked();
        Notes().Should().BeEmpty();

        Npc.AILevel = AILevel.Medium;
        Bench.Engine.SetLocalBool(Npc.Id, "AI_LEASH_EVADE_ACTIVE", true);
        Service.CreaturePhysicalAttacked();
        Notes().Should().BeEmpty();
    }

    [Test]
    public void Being_attacked_runs_the_profile_with_the_attacker_as_the_event_target()
    {
        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("on_attacked").Score(10)
            .When(context => context.Trigger == AITriggerType.Attacked && context.EventTarget == _enemy.Id));
        Bench.LastAttacker = _enemy.Id;

        Service.CreaturePhysicalAttacked();

        Scripts("on_attacked").Should().Be(1);
        Notes().Should().NotContain(Attack);
    }

    [Test]
    public void Being_attacked_falls_back_to_the_highest_enmity_attack_when_the_profile_does_nothing()
    {
        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("never").Score(0));
        Bench.LastAttacker = _enemy.Id;

        Service.CreaturePhysicalAttacked();

        Notes().Should().Contain(Attack);
    }

    [Test]
    public void Being_attacked_by_something_beyond_the_leash_starts_the_evade_before_the_profile_runs()
    {
        Flags(AIFlag.ReturnHome);
        _enemy.Position = new System.Numerics.Vector3(80f, 0f, 0f);
        Bench.LastAttacker = _enemy.Id;

        Service.CreaturePhysicalAttacked();

        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
        Scripts(ScriptedProfiles.DefaultScript).Should().Be(0);
    }

    [Test]
    public void Being_damaged_does_nothing_for_a_creature_without_AI_or_one_that_is_evading()
    {
        Bench.LastDamager = _enemy.Id;
        Npc.AILevel = AILevel.VeryLow;
        Service.CreatureDamaged();
        Notes().Should().BeEmpty();

        Npc.AILevel = AILevel.Medium;
        Bench.Engine.SetLocalBool(Npc.Id, "AI_LEASH_EVADE_ACTIVE", true);
        Service.CreatureDamaged();
        Notes().Should().BeEmpty();
    }

    [Test]
    public void Being_damaged_runs_the_profile_with_the_damager_as_the_event_target_or_attacks()
    {
        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("on_damaged").Score(10)
            .When(context => context.Trigger == AITriggerType.Damaged && context.EventTarget == _enemy.Id));
        Bench.LastDamager = _enemy.Id;

        Service.CreatureDamaged();
        Scripts("on_damaged").Should().Be(1);
        Notes().Should().NotContain(Attack);

        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("never").Score(0));
        Service.CreatureDamaged();
        Notes().Should().Contain(Attack);
    }

    [Test]
    public void Being_damaged_by_something_beyond_the_leash_starts_the_evade()
    {
        Flags(AIFlag.ReturnHome);
        _enemy.Position = new System.Numerics.Vector3(80f, 0f, 0f);
        Bench.LastDamager = _enemy.Id;

        Service.CreatureDamaged();

        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
    }

    [Test]
    public void The_highest_enmity_target_replaces_the_event_target_when_choosing_what_the_leash_looks_at()
    {
        Flags(AIFlag.ReturnHome);
        var fighter = HostilePlayer("fighter", 8f);
        Service.SetEnmity(Npc.Id, fighter.Id, 50);
        _enemy.Position = new System.Numerics.Vector3(80f, 0f, 0f);
        Bench.LastAttacker = _enemy.Id;

        Service.CreaturePhysicalAttacked();

        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(0, "the target the creature is fighting is inside the leash");
    }

    [Test]
    public void Death_runs_the_death_trigger_for_a_creature_even_without_AI_and_forgets_it()
    {
        Npc.AILevel = AILevel.VeryLow;

        Service.CreatureDeath();

        Scripts(ScriptedProfiles.DefaultScript).Should().Be(0, "the death trigger clears the state and runs no action");
    }

    [Test]
    public void Being_disturbed_runs_the_profile_or_attacks()
    {
        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("on_disturbed").Score(10)
            .When(context => context.Trigger == AITriggerType.Disturbed));
        Service.CreatureDisturbed();
        Scripts("on_disturbed").Should().Be(1);
        Notes().Should().NotContain(Attack);

        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("never").Score(0));
        Service.CreatureDisturbed();
        Notes().Should().Contain(Attack);
    }

    [Test]
    public void Being_disturbed_does_nothing_for_a_creature_without_AI()
    {
        Npc.AILevel = AILevel.VeryLow;

        Service.CreatureDisturbed();

        Notes().Should().BeEmpty();
    }

    [Test]
    public void Spawning_sets_up_the_creature_for_the_AI_before_running_the_spawn_trigger()
    {
        UseProfiles(builder => builder.Create(AIProfileType.Generic).DecisionThrottle(0f).Script("on_spawn").Score(10)
            .When(context => context.Trigger == AITriggerType.Spawn));
        Bench.Engine.SetLocalInt(Npc.Id, "PERMANENT_VFX_ID", 12);
        Bench.Engine.SetLocalInt(Npc.Id, "PARALYZE", 1);
        Bench.Engine.SetLocalInt(Npc.Id, "DAZE", 1);

        Service.CreatureSpawn();

        Bench.Locals.String(Npc.Id, "X2_SPECIAL_COMBAT_AI_SCRIPT").Should().Be("xxx");
        Notes().Should().Equal("LoadNPCStats()", $"ApplyCreatureMovementRate({Npc.Id})");
        Bench.Engine.CallsTo("ApplyEffectToObject").Should().HaveCount(4, "the aggro aura, the visual effect, the paralysis and the daze");
        Bench.Engine.CallsTo("ApplyEffectToObject").Should().OnlyContain(call => (DurationType)call.Arguments[0]! == DurationType.Permanent);
        Bench.Engine.CallsTo("EffectAreaOfEffect").Single().Arguments.Should().Equal(
            AreaOfEffect.CustomAoe, "crea_aggro_enter", string.Empty, "crea_aggro_exit");
        Bench.Engine.CallsTo("TagEffect").Single().Arguments[1].Should().Be("AGGRO_AOE");
        Bench.Engine.CallsTo("SetLocalLocation").Should().ContainSingle(call => (string)call.Arguments[1]! == "HOME_LOCATION");
        Scripts("on_spawn").Should().Be(1);
    }

    [Test]
    public void Spawning_a_plain_creature_applies_only_the_aggro_aura()
    {
        Service.CreatureSpawn();

        Bench.Engine.CallsTo("ApplyEffectToObject").Should().HaveCount(1);
    }

    [Test]
    public void The_rested_spell_cast_and_user_defined_events_do_nothing()
    {
        Service.CreatureRested();
        Service.CreatureSpellCastAt();
        Service.CreatureUserDefined();

        Bench.Engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_blocked_creature_that_met_another_creature_resumes_its_attack()
    {
        var blocker = Bench.AddCreature("blocker", Area, 1f);
        Bench.BlockingDoor = blocker.Id;

        Service.CreatureBlocked();

        Notes().Should().Equal($"ResumeAttackAfterActionsCleared({Npc.Id})");
    }

    [Test]
    public void A_creature_blocked_by_a_door_or_without_AI_or_while_evading_does_not_resume_an_attack()
    {
        var blocker = Bench.AddCreature("blocker", Area, 1f);
        Bench.BlockingDoor = Invalid;
        Service.CreatureBlocked();

        Bench.BlockingDoor = blocker.Id;
        Npc.AILevel = AILevel.VeryLow;
        Service.CreatureBlocked();

        Npc.AILevel = AILevel.Medium;
        Bench.Engine.SetLocalBool(Npc.Id, "AI_LEASH_EVADE_ACTIVE", true);
        Service.CreatureBlocked();

        Notes().Should().BeEmpty();
    }
}
