#nullable enable
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The profile decision engine of the AI instance: which action a trigger selects, how throttles, cooldowns and phases
/// gate it, how each action type runs, the boss timer, and how the profile of a creature is named and found.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIServiceDecisionTests : AIServiceTestBase
{
    private const string Script = "ai_test";

    private DateTime _now;
    private IDisposable _clock = null!;
    private AISim _enemy = null!;

    private enum Phase
    {
        Opening,
        Frenzy
    }

    [OneTimeSetUp]
    public void LoadTheAbilities()
    {
        Ability.CacheData();
    }

    [SetUp]
    public void FreezeTheClock()
    {
        _now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
        _clock = Clock.OverrideForTests(() => _now);
        _enemy = HostilePlayer("enemy", 3f);
    }

    [TearDown]
    public void ReleaseTheClock()
    {
        _clock.Dispose();
    }

    private AIProfileBuilder Profile(AIProfileBuilder builder, float throttle = 0f) =>
        builder.Create(AIProfileType.Generic).DecisionThrottle(throttle);

    private bool Trigger(AITriggerType trigger = AITriggerType.CombatRound, uint target = Invalid, bool bypass = false) =>
        Service.ProcessTrigger(Npc.Id, trigger, target, bypass);

    private int Executed(string script) =>
        Bench.Engine.CallsTo("ExecuteScript").Count(call => (string)call.Arguments[0]! == script);

    private void Threat(AISim target, int amount = 10) => Service.SetEnmity(Npc.Id, target.Id, amount);

    private static FeatType HostileSingleTargetFeat => FeatType.RendingBite;

    // ---- who is processed -------------------------------------------------------------------------------------------

    [Test]
    public void Players_dms_possessed_creatures_and_invalid_objects_are_never_processed()
    {
        var player = Bench.AddPlayer("pc", Area);
        var dm = Bench.AddCreature("dm", Area);
        dm.IsDM = true;
        var possessed = Bench.AddCreature("possessed", Area);
        possessed.IsDMPossessed = true;

        Service.ProcessTrigger(player.Id, AITriggerType.CombatRound).Should().BeFalse();
        Service.ProcessTrigger(dm.Id, AITriggerType.CombatRound).Should().BeFalse();
        Service.ProcessTrigger(possessed.Id, AITriggerType.CombatRound).Should().BeFalse();
        Service.ProcessTrigger(Invalid, AITriggerType.CombatRound).Should().BeFalse();
        Executed(ScriptedProfiles.DefaultScript).Should().Be(0);
    }

    [Test]
    public void A_creature_with_no_AI_is_not_processed_except_for_its_death()
    {
        Npc.AILevel = AILevel.VeryLow;

        Trigger().Should().BeFalse();
        Trigger(AITriggerType.Death).Should().BeFalse();

        Executed(ScriptedProfiles.DefaultScript).Should().Be(0);
    }

    [Test]
    public void A_petrified_creature_makes_no_decision()
    {
        Npc.Effects.Add(EffectTypeScript.Petrify);

        Trigger().Should().BeFalse();

        Executed(ScriptedProfiles.DefaultScript).Should().Be(0);
    }

    [Test]
    public void A_creature_the_native_effect_list_says_has_no_petrify_effect_is_not_walked_effect_by_effect()
    {
        Npc.Effects.Add(EffectTypeScript.Petrify);
        Service.MayHaveEffect = false;

        Trigger().Should().BeTrue();
    }

    [Test]
    public void A_dead_creature_forgets_its_state_and_makes_no_decision()
    {
        UseProfiles(builder => Profile(builder).Script(Script).Score(10).Cooldown("c", 60f));
        Trigger().Should().BeTrue();
        Trigger().Should().BeFalse("the cooldown blocks the only action");
        Npc.HitPoints = 0;

        Trigger().Should().BeFalse();

        Npc.HitPoints = 10;
        Trigger().Should().BeTrue("the dead creature's cooldown was forgotten");
    }

    [Test]
    public void The_death_trigger_clears_the_state_and_runs_no_action()
    {
        UseProfiles(builder => Profile(builder).Script(Script).Score(10).Cooldown("c", 60f));
        Trigger().Should().BeTrue();

        Trigger(AITriggerType.Death).Should().BeFalse();

        Executed(Script).Should().Be(1);
        Trigger().Should().BeTrue("the cooldown was forgotten");
    }

    [Test]
    public void A_dm_toggling_AI_off_clears_the_creatures_state_and_toggling_it_on_keeps_it()
    {
        UseProfiles(builder => Profile(builder).Script(Script).Score(10).Cooldown("c", 60f));
        Trigger().Should().BeTrue();
        Bench.EventObject = Npc.Id;

        Service.DMToggleAIAfter();
        Trigger().Should().BeFalse("the AI is still on, so the cooldown stays");

        Npc.AILevel = AILevel.VeryLow;
        Service.DMToggleAIAfter();
        Npc.AILevel = AILevel.Medium;
        Trigger().Should().BeTrue("the AI was switched off and its state cleared");
    }

    [Test]
    public void A_dm_toggle_on_something_that_is_not_a_creature_does_nothing()
    {
        Bench.EventObject = Area;
        Service.DMToggleAIAfter();
        Bench.EventObject = Invalid;
        Service.DMToggleAIAfter();

        Bench.Trace().Should().BeEmpty();
    }

    // ---- the profile of a creature ----------------------------------------------------------------------------------

    [Test]
    public void A_creature_with_no_profile_set_uses_the_generic_one_a_beast_the_beast_one_and_a_droid_the_droid_one()
    {
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.Generic);

        Bench.PlayerBeasts.Add(Npc.Id);
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.BeastCompanion);

        Bench.PlayerBeasts.Clear();
        Npc.ResRef = "pc_droid";
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.DroidCompanion);
    }

    [Test]
    public void A_profile_that_was_set_is_stored_by_name_and_by_number_and_found_again()
    {
        Service.SetAIProfile(Npc.Id, AIProfileType.DroidCompanion);

        Bench.Locals.String(Npc.Id, "AI_PROFILE").Should().Be("DroidCompanion");
        Bench.Locals.Int(Npc.Id, "AI_PROFILE_ID").Should().Be((int)AIProfileType.DroidCompanion);
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.DroidCompanion);
    }

    [Test]
    public void The_profile_name_is_matched_without_regard_to_case_and_an_unknown_name_falls_back_to_the_number()
    {
        Bench.Engine.SetLocalString(Npc.Id, "AI_PROFILE", "beastcompanion");
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.BeastCompanion);

        Bench.Engine.SetLocalString(Npc.Id, "AI_PROFILE", "not-a-profile");
        Bench.Engine.SetLocalInt(Npc.Id, "AI_PROFILE_ID", (int)AIProfileType.DroidCompanion);
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.DroidCompanion);

        Bench.Engine.SetLocalInt(Npc.Id, "AI_PROFILE_ID", 99);
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.Generic);

        Bench.Engine.SetLocalString(Npc.Id, "AI_PROFILE", "Invalid");
        Service.GetAIProfile(Npc.Id).Should().Be(AIProfileType.Generic);
    }

    [Test]
    public void A_creature_whose_profile_is_not_registered_is_decided_by_the_generic_profile()
    {
        Service.Definitions.Clear();
        Service.Definitions.Add(typeof(GenericProfileOnly));
        Service.CacheProfiles();
        Bench.Engine.SetLocalString(Npc.Id, "AI_PROFILE", "DroidCompanion");

        Trigger().Should().BeTrue();

        Executed("generic_only").Should().Be(1);
    }

    public sealed class GenericProfileOnly : IAIProfileDefinition
    {
        public Dictionary<AIProfileType, AIProfile> BuildProfiles() =>
            new AIProfileBuilder().Create(AIProfileType.Generic).DecisionThrottle(0f).Script("generic_only").Score(10).Build();
    }

    public sealed class WithoutBeastProfile : IAIProfileDefinition
    {
        public Dictionary<AIProfileType, AIProfile> BuildProfiles() =>
            new AIProfileBuilder()
                .Create(AIProfileType.Generic).Script("s").Score(1)
                .Create(AIProfileType.DroidCompanion).Script("s").Score(1)
                .Build();
    }

    public sealed class WithoutDroidProfile : IAIProfileDefinition
    {
        public Dictionary<AIProfileType, AIProfile> BuildProfiles() =>
            new AIProfileBuilder()
                .Create(AIProfileType.Generic).Script("s").Score(1)
                .Create(AIProfileType.BeastCompanion).Script("s").Score(1)
                .Build();
    }

    [Test]
    public void A_creature_that_changes_profile_starts_over_with_a_fresh_state()
    {
        UseProfiles(builder =>
        {
            Profile(builder).Script(Script).Score(10).Cooldown("c", 60f);
            return builder.Create(AIProfileType.DroidCompanion).DecisionThrottle(0f).Script("droid_script").Score(10).Cooldown("c", 60f);
        });
        Trigger().Should().BeTrue();
        Trigger().Should().BeFalse();

        Service.SetAIProfile(Npc.Id, AIProfileType.DroidCompanion);
        Trigger().Should().BeTrue();

        Executed("droid_script").Should().Be(1);
    }

    // ---- selection -------------------------------------------------------------------------------------------------

    [Test]
    public void The_highest_scoring_action_runs_and_a_tie_goes_to_the_lower_priority_number_and_then_the_earlier_action()
    {
        UseProfiles(builder => Profile(builder)
            .Script("low").Score(5)
            .Script("high").Score(9)
            .Script("tie_late").Score(9).Priority(50)
            .Script("tie_early").Score(9).Priority(10)
            .Script("tie_early_too").Score(9).Priority(10));

        Trigger().Should().BeTrue();

        Executed("tie_early").Should().Be(1);
        Bench.Engine.CallsTo("ExecuteScript").Should().ContainSingle();
    }

    [Test]
    public void An_action_with_a_failing_guard_or_no_score_is_skipped_and_with_nothing_left_the_creature_attacks()
    {
        UseProfiles(builder => Profile(builder)
            .Script("guarded").Score(9).When(_ => false)
            .Script("worthless").Score(0)
            .Script("negative").Score(-3));

        Trigger().Should().BeFalse();

        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
        Bench.Trace().Should().Equal($"AttackHighestEnmityTarget({Npc.Id})");
    }

    [Test]
    public void An_action_with_no_score_function_scores_nothing()
    {
        UseProfiles(builder =>
        {
            Profile(builder).Script("unscored").Score(1);
            builder.Build()[AIProfileType.Generic].Actions[0].Score = null!;
            return builder;
        });

        Trigger().Should().BeFalse();
    }

    [Test]
    public void Only_the_first_candidates_of_a_profile_are_considered()
    {
        UseProfiles(builder => Profile(builder).MaxCandidateActions(2)
            .Script("first").Score(1)
            .Script("second").Score(2)
            .Script("third").Score(99));

        Trigger().Should().BeTrue();

        Executed("second").Should().Be(1);
        Executed("third").Should().Be(0);
    }

    [Test]
    public void A_decision_inside_the_throttle_window_is_refused_unless_the_trigger_bypasses_it_or_is_a_spawn_or_a_death()
    {
        UseProfiles(builder => Profile(builder, throttle: 1f).Script(Script).Score(10));
        Trigger().Should().BeTrue();

        Trigger().Should().BeFalse();
        Trigger(bypass: true).Should().BeTrue();
        Trigger(AITriggerType.Spawn).Should().BeTrue();
        _now = _now.AddSeconds(0.5);
        Trigger().Should().BeFalse();
        _now = _now.AddSeconds(1);
        Trigger().Should().BeTrue();
        Executed(Script).Should().Be(4);
    }

    [Test]
    public void A_profile_without_a_throttle_decides_every_time()
    {
        UseProfiles(builder => Profile(builder, throttle: 0f).Script(Script).Score(10));

        Trigger();
        Trigger();

        Executed(Script).Should().Be(2);
    }

    [Test]
    public void A_cooldown_blocks_the_action_until_it_expires()
    {
        UseProfiles(builder => Profile(builder).Script(Script).Score(10).Cooldown("shout", 30f));

        Trigger().Should().BeTrue();
        _now = _now.AddSeconds(29);
        Trigger().Should().BeFalse();
        _now = _now.AddSeconds(2);
        Trigger().Should().BeTrue();

        Executed(Script).Should().Be(2);
    }

    [Test]
    public void An_action_without_a_cooldown_length_never_starts_one()
    {
        UseProfiles(builder => Profile(builder).Script(Script).Score(10).Cooldown("shout", 0f));

        Trigger();
        Trigger();

        Executed(Script).Should().Be(2);
    }

    [Test]
    public void Actions_in_the_same_cooldown_group_share_it()
    {
        UseProfiles(builder => Profile(builder)
            .Script("first").Score(10).Cooldown("shared", 30f)
            .Script("second").Score(5).Cooldown("shared", 30f));

        Trigger();
        Trigger();

        Executed("first").Should().Be(1);
        Executed("second").Should().Be(0);
    }

    // ---- phases ----------------------------------------------------------------------------------------------------

    [Test]
    public void A_phase_that_the_creature_enters_adds_its_actions_and_the_last_matching_phase_wins()
    {
        UseProfiles(builder => Profile(builder)
            .Script("base").Score(1)
            .Phase(Phase.Opening).EnterWhen(AIPhase.Always()).Script("opening").Score(5)
            .Phase(Phase.Frenzy).EnterWhen(AIPhase.HealthAtOrBelow(50)).Script("frenzy").Score(8));

        Trigger();
        Executed("opening").Should().Be(1);

        Npc.HitPoints = 40;
        Trigger();
        Executed("frenzy").Should().Be(1);
        Executed("opening").Should().Be(1);
    }

    [Test]
    public void An_action_marked_once_per_phase_runs_once_until_the_creature_enters_another_phase()
    {
        UseProfiles(builder => Profile(builder)
            .Phase(Phase.Opening).EnterWhen(AIPhase.HealthAbove(50)).Script("roar").Score(9).OncePerPhase()
            .Script("claw").Score(1)
            .Phase(Phase.Frenzy).EnterWhen(AIPhase.HealthAtOrBelow(50)).Script("roar").Score(9).OncePerPhase());

        Trigger();
        Trigger();
        Executed("roar").Should().Be(1);
        Executed("claw").Should().Be(1);

        Npc.HitPoints = 10;
        Trigger();
        Executed("roar").Should().Be(2, "the same action belongs to the next phase's own once-per-phase list");
    }

    [Test]
    public void A_profile_whose_phases_never_match_runs_only_its_own_actions()
    {
        UseProfiles(builder => Profile(builder)
            .Script("base").Score(1)
            .Phase(Phase.Opening).EnterWhen(AIPhase.HealthAtOrBelow(10)).Script("opening").Score(5));

        Trigger();

        Executed("base").Should().Be(1);
    }

    [Test]
    public void A_phase_can_wait_for_the_combat_to_last_long_enough()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder)
            .Script("base").Score(1)
            .Phase(Phase.Frenzy).EnterWhen(AIPhase.ElapsedCombatSecondsAtLeast(30f)).Script("frenzy").Score(8));

        Trigger();
        Executed("base").Should().Be(1);

        _now = _now.AddSeconds(31);
        Trigger();
        Executed("frenzy").Should().Be(1);
    }

    // ---- action types ----------------------------------------------------------------------------------------------

    [Test]
    public void A_wait_a_speech_and_a_random_walk_are_commands_given_to_the_creature_itself()
    {
        UseProfiles(builder => Profile(builder)
            .Wait(2.5f).Score(9)
            .Speak("Halt!").Score(8).Cooldown("speak", 1f)
            .RandomWalk().Score(7).Cooldown("walk", 1f));

        Trigger();
        Bench.RunAssigned(Npc.Id);

        Bench.Engine.CallsTo("ActionWait").Single().Arguments[0].Should().Be(2.5f);
    }

    [Test]
    public void A_speech_action_speaks_its_text()
    {
        UseProfiles(builder => Profile(builder).Speak("Halt!").Score(9));

        Trigger();
        Bench.RunAssigned(Npc.Id);

        Bench.Engine.CallsTo("SpeakString").Single().Arguments[0].Should().Be("Halt!");
    }

    [Test]
    public void A_random_walk_action_runs_only_for_an_idle_creature_and_clears_its_actions_first()
    {
        UseProfiles(builder => Profile(builder).RandomWalk().Score(9));

        Npc.InCombat = true;
        Trigger().Should().BeFalse();

        Npc.InCombat = false;
        Npc.Action = ActionType.MoveToPoint;
        Trigger().Should().BeFalse();

        Npc.Action = ActionType.Invalid;
        Trigger().Should().BeTrue();
        Bench.RunAssigned(Npc.Id);
        Bench.Engine.CountOf("ClearAllActions").Should().Be(1);
        Bench.Engine.CountOf("ActionRandomWalk").Should().Be(1);
    }

    [Test]
    public void A_return_home_action_runs_only_outside_the_home_radius_and_walks_home()
    {
        UseProfiles(builder => Profile(builder).ReturnHome().Score(9));

        Trigger().Should().BeFalse("the creature is at home");

        Npc.Position = new Vector3(40f, 0f, 0f);
        Trigger().Should().BeTrue();
        Bench.RunAssigned(Npc.Id);
        Bench.Engine.CountOf("ActionForceMoveToLocation").Should().Be(1);
    }

    [Test]
    public void A_target_dependent_action_needs_a_valid_target()
    {
        UseProfiles(builder => Profile(builder).MoveToTarget().Score(9).Flee().Score(8).CallAllies(5f).Score(7).AttackHighestEnmity().Score(6));

        Trigger().Should().BeFalse("nobody has threat against the creature");
        Bench.Engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void Moving_to_the_target_clears_the_actions_and_runs_up_to_it()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).MoveToTarget().Score(9));

        Trigger().Should().BeTrue();
        Bench.RunAssigned(Npc.Id);

        Bench.Engine.CountOf("ClearAllActions").Should().Be(1);
        var move = Bench.Engine.CallsTo("ActionMoveToObject").Single();
        move.Arguments[0].Should().Be(_enemy.Id);
        move.Arguments[1].Should().Be(true);
    }

    [Test]
    public void Fleeing_walks_the_creature_home()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Flee().Score(9));

        Trigger().Should().BeTrue();
        Bench.RunAssigned(Npc.Id);

        Bench.Engine.CountOf("ClearAllActions").Should().Be(1);
        Bench.Engine.CountOf("ActionForceMoveToLocation").Should().Be(1);
    }

    [Test]
    public void The_attack_action_attacks_the_highest_enmity_target_without_clearing_actions_first()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).AttackHighestEnmity().Score(9));

        Trigger().Should().BeTrue();

        Bench.Trace().Should().Equal($"AttackHighestEnmityTarget({Npc.Id})");
        Bench.Engine.CountOf("ClearAllActions").Should().Be(0);
        Bench.Engine.CountOf("ActionAttack").Should().Be(0);
    }

    [Test]
    public void Calling_allies_raises_the_threat_of_the_target_on_each_ally_in_range_that_is_its_enemy()
    {
        var near = Bench.AddCreature("near", Area, 2f);
        var far = Bench.AddCreature("far", Area, 20f);
        var friend = Bench.AddCreature("friend", Area, 1f);
        Bench.MakeEnemies(_enemy.Id, near.Id);
        Bench.MakeEnemies(_enemy.Id, far.Id);
        foreach (var ally in new[] { near, far, friend })
        {
            // Creatures of the NPC's faction that it sees are its allies.
            Bench.LastPerceived = ally.Id;
            Bench.PerceptionSeen = true;
            Service.CreaturePerception();
        }

        Threat(_enemy);
        UseProfiles(builder => Profile(builder).CallAllies(5f).Score(9));

        Trigger().Should().BeTrue();

        Bench.Trace().Should().Equal($"ModifyEnmity({_enemy.Id}, {near.Id}, 1)");
    }

    [Test]
    public void A_creature_with_a_player_master_decides_with_the_players_party_as_its_allies()
    {
        var owner = Bench.AddPlayer("owner", Area, 1f);
        var member = Bench.AddCreature("member", Area, 2f);
        Npc.Master = owner.Id;
        Bench.PartyOf[Npc.Id] = new List<uint> { owner.Id, member.Id };
        Bench.MakeEnemies(_enemy.Id, member.Id);
        Bench.MakeEnemies(_enemy.Id, owner.Id);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).CallAllies(0f).Score(9));

        Trigger().Should().BeTrue();

        Bench.Trace().Should().Equal($"ModifyEnmity({_enemy.Id}, {owner.Id}, 1)", $"ModifyEnmity({_enemy.Id}, {member.Id}, 1)");
    }

    // ---- abilities -------------------------------------------------------------------------------------------------

    [Test]
    public void An_ability_the_creature_does_not_know_is_never_a_candidate()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().NotContain(note => note.StartsWith("TryUseAbility", StringComparison.Ordinal));
    }

    [Test]
    public void A_known_hostile_ability_is_used_on_the_highest_enmity_target()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        Service.AbilityUsed = (_, _, _) => true;
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeTrue();

        Bench.Trace().Should().Contain($"TryUseAbility({Npc.Id}, {_enemy.Id}, {HostileSingleTargetFeat})");
        Bench.Trace().Should().NotContain($"IssueAttackCommand({Npc.Id}, {_enemy.Id})");
    }

    [Test]
    public void An_ability_that_cannot_be_activated_now_is_not_chosen()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        Service.CanActivate = (_, _, _) => false;
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().Contain(note => note.StartsWith("CanActivateAbility", StringComparison.Ordinal));
        Bench.Trace().Should().NotContain(note => note.StartsWith("TryUseAbility", StringComparison.Ordinal));
    }

    [Test]
    public void A_hostile_ability_with_nobody_to_use_it_on_is_not_chosen()
    {
        KnowFeat(HostileSingleTargetFeat);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().NotContain(note => note.StartsWith("CanActivateAbility", StringComparison.Ordinal));
    }

    [Test]
    public void A_hostile_single_target_ability_is_not_used_on_the_creature_itself_or_a_friend()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9).Target(context => context.Self));
        Trigger().Should().BeFalse();

        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9)
            .Target(_ => Bench.AddCreature("friend", Area, 1f).Id));
        Trigger().Should().BeFalse();
    }

    [Test]
    public void A_hostile_area_ability_with_no_selected_target_is_not_used()
    {
        var feat = FindAbility(ability => ability.IsHostileAbility && !ability.IsSingleTargetAbility && !ability.RequiresTarget);
        KnowFeat(feat.Key);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Ability(feat.Key).Score(9).Target(_ => Invalid));

        Trigger().Should().BeFalse("a self-centered area ability that found no enemy must not fall back to the creature");

        Bench.Trace().Should().NotContain(note => note.StartsWith("CanActivateAbility", StringComparison.Ordinal));
    }

    [Test]
    public void A_hostile_area_ability_centered_on_the_creature_by_its_selector_is_used()
    {
        var feat = FindAbility(ability => ability.IsHostileAbility && !ability.IsSingleTargetAbility && !ability.RequiresTarget);
        KnowFeat(feat.Key);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Ability(feat.Key).Score(9).Target(context => context.Self));

        Trigger();

        Bench.Trace().Should().Contain($"CanActivateAbility({Npc.Id}, {Npc.Id}, {feat.Key}, 1)");
    }

    [Test]
    public void A_targeted_friendly_ability_with_no_target_is_not_used()
    {
        var feat = FindAbility(ability => !ability.IsHostileAbility && ability.RequiresTarget && ability.IsSingleTargetAbility);
        KnowFeat(feat.Key);
        UseProfiles(builder => Profile(builder).Ability(feat.Key).Score(9).Target(_ => Invalid));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().NotContain(note => note.StartsWith("CanActivateAbility", StringComparison.Ordinal));
    }

    [Test]
    public void An_ability_with_an_effective_level_perk_asks_for_that_perk_level()
    {
        var feat = FindAbility(ability => ability.EffectiveLevelPerkType != PerkType.Invalid && ability.IsHostileAbility && ability.IsSingleTargetAbility);
        KnowFeat(feat.Key);
        Threat(_enemy);
        Service.PerkLevel = 4;
        UseProfiles(builder => Profile(builder).Ability(feat.Key).Score(9));

        Trigger();

        Bench.Trace().Should().Contain($"CanActivateAbility({Npc.Id}, {_enemy.Id}, {feat.Key}, 4)");
    }

    [Test]
    public void An_ability_without_an_effective_level_perk_is_activated_at_level_one()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger();

        Bench.Trace().Should().Contain($"CanActivateAbility({Npc.Id}, {_enemy.Id}, {HostileSingleTargetFeat}, 1)");
    }

    [Test]
    public void A_friendly_ability_with_no_target_is_used_on_the_creature_itself()
    {
        var feat = FindAbility(ability => !ability.IsHostileAbility && !ability.RequiresTarget && !ability.IsHealingAbility);
        KnowFeat(feat.Key);
        UseProfiles(builder => Profile(builder).Ability(feat.Key).Score(9).Target(_ => Invalid));

        Trigger();

        Bench.Trace().Should().Contain($"CanActivateAbility({Npc.Id}, {Npc.Id}, {feat.Key}, 1)");
    }

    [Test]
    public void An_ability_with_an_explicit_target_selector_uses_it_before_the_defaults()
    {
        KnowFeat(HostileSingleTargetFeat);
        var chosen = Bench.AddCreature("chosen", Area, 2f);
        Bench.MakeEnemies(chosen.Id, Npc.Id);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9).Target(_ => chosen.Id));

        Trigger();

        Bench.Trace().Should().Contain($"CanActivateAbility({Npc.Id}, {chosen.Id}, {HostileSingleTargetFeat}, 1)");
    }

    [Test]
    public void An_ability_whose_feat_has_a_registered_default_target_selector_uses_it()
    {
        KnowFeat(HostileSingleTargetFeat);
        var chosen = Bench.AddCreature("chosen", Area, 2f);
        Bench.MakeEnemies(chosen.Id, Npc.Id);
        Service.RegisterDefaultTargetSelector(HostileSingleTargetFeat, AITarget.Self());
        Service.TryGetDefaultTargetSelector(HostileSingleTargetFeat, out var registered).Should().BeTrue();
        registered.Should().NotBeNull();
        Service.TryGetDefaultTargetSelector(FeatType.Provoke1, out _).Should().BeFalse();
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger();

        Bench.Trace().Should().NotContain(note => note.StartsWith("CanActivateAbility", StringComparison.Ordinal),
            "the registered selector aims a hostile single-target ability at the caster, which is refused");
    }

    [Test]
    public void A_failed_ability_use_falls_back_to_attacking_the_target_it_aimed_at_or_the_best_visible_threat()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeTrue();
        Bench.Trace().Should().Contain($"IssueAttackCommand({Npc.Id}, {_enemy.Id})");

        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9).Target(context => context.Self));
        Bench.ClearCalls();
        Service.CanActivate = (_, _, _) => true;
        Trigger();
        Bench.Trace().Should().NotContain($"IssueAttackCommand({Npc.Id}, {Npc.Id})");
    }

    [Test]
    public void A_failed_ability_use_with_nobody_left_to_attack_does_nothing_more()
    {
        var feat = FindAbility(ability => !ability.IsHostileAbility && !ability.RequiresTarget && !ability.IsHealingAbility);
        KnowFeat(feat.Key);
        UseProfiles(builder => Profile(builder).Ability(feat.Key).Score(9));

        Trigger();

        Bench.Trace().Should().Contain($"TryUseAbility({Npc.Id}, {Npc.Id}, {feat.Key})");
        Bench.Trace().Should().Contain($"GetHighestEnmityAttackTarget({Npc.Id})");
        Bench.Trace().Should().NotContain(note => note.StartsWith("IssueAttackCommand", StringComparison.Ordinal));
    }

    // ---- controlled companions --------------------------------------------------------------------------------------

    [Test]
    public void A_controlled_companion_with_its_abilities_off_uses_none()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        Service.ControlledCompanions.Add(Npc.Id);
        Service.CompanionsWithoutAbilities.Add(Npc.Id);
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().NotContain(note => note.StartsWith("CanActivateAbility", StringComparison.Ordinal));
    }

    [Test]
    public void A_controlled_companions_hostile_ability_target_is_resolved_by_the_companion_rules()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        Service.ControlledCompanions.Add(Npc.Id);
        Service.ResolveHostile = (_, authorized, _) => authorized;
        Service.AbilityUsed = (_, _, _) => true;
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeTrue();

        Bench.Trace().Should().Contain($"ResolveHostileAbilityTarget({Npc.Id}, {_enemy.Id}, {_enemy.Id})");
        Bench.Trace().Should().Contain($"TryUseAbility({Npc.Id}, {_enemy.Id}, {HostileSingleTargetFeat})");
    }

    [Test]
    public void A_controlled_companion_with_nothing_authorized_to_attack_takes_no_hostile_action()
    {
        Service.ControlledCompanions.Add(Npc.Id);
        UseProfiles(builder => Profile(builder).AttackHighestEnmity().Score(9));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().NotContain($"AttackHighestEnmityTarget({Npc.Id})".Replace("Attack", "Issue"));
    }

    [Test]
    public void A_controlled_companions_hostile_ability_with_no_valid_target_is_rejected_before_it_could_fall_back_to_itself()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        Service.ControlledCompanions.Add(Npc.Id);
        Service.ResolveHostile = (_, _, _) => Invalid;
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeFalse();

        Bench.Trace().Should().NotContain(note => note.StartsWith("TryUseAbility", StringComparison.Ordinal));
    }

    [Test]
    public void A_controlled_companion_whose_ability_fails_attacks_only_the_target_it_may_attack()
    {
        KnowFeat(HostileSingleTargetFeat);
        Threat(_enemy);
        Service.ControlledCompanions.Add(Npc.Id);
        Service.ResolveHostile = (_, authorized, _) => authorized;
        UseProfiles(builder => Profile(builder).Ability(HostileSingleTargetFeat).Score(9));

        Trigger().Should().BeTrue();

        Bench.Trace().Should().Contain($"IssueAttackCommand({Npc.Id}, {_enemy.Id})");
        Bench.Trace().Should().NotContain(note => note.StartsWith("GetHighestEnmityAttackTarget", StringComparison.Ordinal));
    }

    // ---- the boss timer and the leash inside a decision ---------------------------------------------------------------

    [Test]
    public void A_boss_in_combat_schedules_a_timer_trigger_once_a_second_and_runs_it_with_its_target()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Boss()
            .Script("tick").Score(9).When(context => context.Trigger == AITriggerType.BossTimer));

        Trigger();
        Trigger();

        var timer = Bench.Delayed().Should().ContainSingle().Subject;
        timer.Seconds.Should().Be(1f);
        Executed("tick").Should().Be(0);
        timer.Action();
        Executed("tick").Should().Be(1);
    }

    [Test]
    public void A_boss_timer_that_fires_after_the_creature_died_or_was_destroyed_clears_the_state()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Boss().Script("tick").Score(9).When(context => context.Trigger == AITriggerType.BossTimer));
        Trigger();
        var timer = Bench.Delayed().Single().Action;
        Npc.HitPoints = 0;

        timer();

        Executed("tick").Should().Be(0, "the timer found the creature dead");
    }

    [Test]
    public void A_boss_timer_that_fires_with_nobody_left_to_fight_resets_the_combat_clock_and_stops()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Boss()
            .Script("tick").Score(9).When(context => context.Trigger == AITriggerType.BossTimer)
            .Script("other").Score(1));
        Trigger();
        var timer = Bench.Delayed().Single().Action;
        Service.Tables.Clear();
        Bench.ClearCalls();

        timer();

        Bench.Engine.CountOf("ExecuteScript").Should().Be(0);
    }

    [Test]
    public void A_boss_timer_for_a_creature_that_no_longer_has_a_state_does_nothing()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Boss().Script("tick").Score(9));
        Trigger();
        var timer = Bench.Delayed().Single().Action;
        Trigger(AITriggerType.Death);
        Bench.ClearCalls();

        timer();

        Bench.Engine.CountOf("ExecuteScript").Should().Be(0);
    }

    [Test]
    public void A_busy_boss_still_schedules_its_timer_but_makes_no_decision()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Boss().Script("tick").Score(9));
        Bench.Engine.SetLocalBool(Npc.Id, "IS_BUSY", true);

        Trigger().Should().BeFalse();

        Bench.Delayed().Should().ContainSingle();
        Executed("tick").Should().Be(0);
    }

    [Test]
    public void A_creature_that_is_not_a_boss_schedules_no_timer()
    {
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Script("tick").Score(9));

        Trigger();

        Bench.Delayed().Should().BeEmpty();
    }

    [Test]
    public void A_creature_whose_target_is_beyond_its_leash_starts_the_evade_instead_of_deciding()
    {
        Flags(AIFlag.ReturnHome);
        _enemy.Position = new Vector3(80f, 0f, 0f);
        Threat(_enemy);
        UseProfiles(builder => Profile(builder).Script("tick").Score(9));

        Trigger().Should().BeFalse();

        Executed("tick").Should().Be(0);
        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
    }

    // ---- healing support --------------------------------------------------------------------------------------------

    [Test]
    public void Support_healing_needs_a_wounded_valid_target_and_a_valid_creature()
    {
        var ally = Bench.AddCreature("ally", Area, 1f);

        Service.TryUseBestTargetedSupportAbility(Npc.Id, ally.Id, out var name).Should().BeFalse("the ally is at full health");
        name.Should().BeEmpty();
        Service.TryUseBestTargetedSupportAbility(Invalid, ally.Id, out _).Should().BeFalse();
        Service.TryUseBestTargetedSupportAbility(Npc.Id, Invalid, out _).Should().BeFalse();
    }

    [Test]
    public void Support_healing_uses_the_strongest_known_healing_ability_that_works_and_names_it()
    {
        var ally = Bench.AddCreature("ally", Area, 1f);
        ally.HitPoints = 10;
        var healing = Ability.GetAllAbilityDetails()
            .Where(entry => entry.Value.IsHealingAbility && !entry.Value.IsHostileAbility && entry.Value.RequiresTarget && entry.Value.IsSingleTargetAbility)
            .OrderByDescending(entry => entry.Value.AbilityLevel)
            .ThenBy(entry => entry.Value.Name)
            .Take(2)
            .ToList();
        healing.Should().HaveCount(2, "the registered abilities include targeted heals");
        foreach (var (feat, _) in healing)
            KnowFeat(feat);
        Service.AbilityUsed = (_, _, feat) => feat == healing[1].Key;

        Service.TryUseBestTargetedSupportAbility(Npc.Id, ally.Id, out var name).Should().BeTrue();

        name.Should().Be(healing[1].Value.Name);
        Bench.Trace().Should().Equal(
            $"TryUseAbility({Npc.Id}, {ally.Id}, {healing[0].Key})",
            $"TryUseAbility({Npc.Id}, {ally.Id}, {healing[1].Key})");
    }

    [Test]
    public void Support_healing_with_no_known_healing_ability_fails()
    {
        var ally = Bench.AddCreature("ally", Area, 1f);
        ally.HitPoints = 10;

        Service.TryUseBestTargetedSupportAbility(Npc.Id, ally.Id, out _).Should().BeFalse();
    }

    // ---- profile loading ---------------------------------------------------------------------------------------------

    [Test]
    public void The_module_cache_loads_every_profile_definition_and_validates_them()
    {
        Service.Definitions.Clear();
        Service.Definitions.Add(typeof(ScriptedProfiles));

        Service.CacheAIData();

        Service.Profiles.Keys.Should().BeEquivalentTo(new[]
        {
            AIProfileType.Generic, AIProfileType.BeastCompanion, AIProfileType.DroidCompanion
        });
    }

    [Test]
    public void Loading_again_replaces_the_profiles()
    {
        UseProfiles(builder => Profile(builder).Script("first").Score(1));
        var first = Service.Profiles[AIProfileType.Generic];

        UseProfiles(builder => Profile(builder).Script("second").Score(1));

        Service.Profiles[AIProfileType.Generic].Should().NotBeSameAs(first);
    }

    [Test]
    public void Two_definitions_of_the_same_profile_fail_the_load()
    {
        Service.Definitions.Add(typeof(ScriptedProfiles));

        FluentActions.Invoking(() => Service.CacheProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
    }

    [Test]
    public void The_default_search_finds_every_profile_definition_of_the_server()
    {
        Service.FindDefinitionsWithTheOriginalSearch()
            .Should().Contain(typeof(SWLOR.Game.Server.Feature.AIDefinition.DefaultAIProfileDefinition))
            .And.NotContain(type => type.IsAbstract || type.IsInterface);
    }

    [Test]
    public void The_real_definitions_load_and_validate()
    {
        Service.Definitions.Clear();
        Service.Definitions.AddRange(Service.FindDefinitionsWithTheOriginalSearch().Where(type => type.Assembly == typeof(IAIProfileDefinition).Assembly));

        Service.CacheAIData();

        Service.Profiles.Keys.Should().Contain(new[]
        {
            AIProfileType.Generic, AIProfileType.DroidCompanion, AIProfileType.BeastCompanion
        });
    }

    [Test]
    public void A_required_profile_that_is_missing_fails_validation()
    {
        Service.Definitions.Clear();
        Service.Definitions.Add(typeof(GenericProfileOnly));
        Service.CacheProfiles();
        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*'BeastCompanion' must be registered*");

        Service.Definitions.Clear();
        Service.Definitions.Add(typeof(WithoutBeastProfile));
        Service.CacheProfiles();
        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*'BeastCompanion' must be registered*");

        Service.Definitions.Clear();
        Service.Definitions.Add(typeof(WithoutDroidProfile));
        Service.CacheProfiles();
        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*'DroidCompanion' must be registered*");

        Service.Definitions.Clear();
        Service.CacheProfiles();
        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*'Generic' must be registered*");
    }

    [Test]
    public void A_profile_phase_without_an_enter_condition_fails_validation()
    {
        UseProfiles(builder => Profile(builder).Script("s").Score(1).Phase(Phase.Opening));

        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*no EnterWhen condition*");
    }

    [Test]
    public void An_action_without_a_score_fails_validation()
    {
        UseProfiles(builder => Profile(builder).Script("s").Score(1));
        Service.Profiles[AIProfileType.Generic].Actions[0].Score = null!;

        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*has no score*");
    }

    [Test]
    public void An_ability_action_with_no_feat_or_an_unregistered_feat_fails_validation()
    {
        UseProfiles(builder => Profile(builder).Ability(FeatType.Invalid).Score(1));
        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*invalid feat*");

        UseProfiles(builder => Profile(builder).Ability((FeatType)30001).Score(1));
        FluentActions.Invoking(() => Service.ValidateProfiles())
            .Should().Throw<InvalidOperationException>().WithMessage("*not registered as an ability*");
    }

    [Test]
    public void A_phase_with_an_enter_condition_and_valid_actions_passes_validation()
    {
        UseProfiles(builder => Profile(builder).Script("s").Score(1)
            .Phase(Phase.Opening).EnterWhen(AIPhase.Always()).Script("p").Score(1));

        FluentActions.Invoking(() => Service.ValidateProfiles()).Should().NotThrow();
    }

    // ---- helpers ---------------------------------------------------------------------------------------------------

    private void KnowFeat(FeatType feat)
    {
        Npc.Feats.Add(feat);
        Bench.Creatures.SetReturnHandler("GetFeatCount", _ => Npc.Feats.Count);
        Bench.Creatures.SetReturnHandler("GetFeatByIndex", arguments => Npc.Feats.ElementAt((int)arguments[1]!));
    }

    private static KeyValuePair<FeatType, AbilityDetail> FindAbility(Func<AbilityDetail, bool> predicate) =>
        Ability.GetAllAbilityDetails().First(entry => predicate(entry.Value));
}
