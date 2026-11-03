#nullable enable
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The aggro aura, the ally cache and the proximity enmity of the AI instance: who the aura ignores, what an enemy
/// entering it does, how allies help, and what leaving it undoes.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIServiceAggroTests : AIServiceTestBase
{
    private AISim _player = null!;

    [SetUp]
    public void PlaceAPlayerAtTheEdgeOfTheAura()
    {
        _player = HostilePlayer("player", 3f);
        Bench.AreaOfEffectCreator = Npc.Id;
    }

    private string Begin(AISim player) => $"TryBeginInfiltration({player.Id}, {Npc.Id})";
    private string Gate(AISim player) => $"CanAcquireAggro({Npc.Id}, {player.Id})";
    private string Proximity(AISim target, AISim enemy, int amount = 1) => $"ModifyProximityEnmity({target.Id}, {enemy.Id}, {amount})";
    private string Remove(AISim target, AISim enemy) => $"RemoveTrackedProximityEnmity({target.Id}, {enemy.Id})";

    private void Perceive(AISim creature, bool seen, bool vanished)
    {
        Bench.LastPerceived = creature.Id;
        Bench.PerceptionSeen = seen;
        Bench.PerceptionVanished = vanished;
        Service.CreaturePerception();
    }

    /// <summary>A creature of the NPC's faction that the NPC sees, which makes it an ally that is hostile to the player.</summary>
    private AISim AddAlly(string name, float x)
    {
        var ally = Bench.AddCreature(name, Area, x);
        Bench.MakeEnemies(_player.Id, ally.Id);
        Perceive(ally, seen: true, vanished: false);
        return ally;
    }

    private void Enter(AISim entering)
    {
        Bench.EnteringObject = entering.Id;
        Service.CreatureAggroEnter();
    }

    private void Exit(AISim exiting)
    {
        Bench.ExitingObject = exiting.Id;
        Service.CreatureAggroExit();
    }

    [Test]
    public void An_enemy_entering_the_aura_begins_an_infiltration_attempt_before_the_stealth_check_and_gains_proximity_enmity()
    {
        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player), Proximity(_player, Npc));
        Bench.Engine.CallsTo("ExecuteScript").Should().ContainSingle(call => (string)call.Arguments[0]! == ScriptedProfiles.DefaultScript,
            "the aggro trigger runs the profile");
    }

    [Test]
    public void An_undetected_stealthed_player_never_enters_the_enmity_table()
    {
        Service.HiddenFromAggro.Add(_player.Id);

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player));
        Service.Tables.Should().BeEmpty();
    }

    [Test]
    public void The_aura_ignores_everything_when_the_creature_has_no_AI_or_is_evading()
    {
        Npc.AILevel = AILevel.VeryLow;
        Enter(_player);

        Npc.AILevel = AILevel.Medium;
        Bench.Engine.SetLocalBool(Npc.Id, "AI_LEASH_EVADE_ACTIVE", true);
        Enter(_player);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void The_aura_ignores_an_invisible_creature()
    {
        _player.Effects.Add(EffectTypeScript.Invisibility);
        Enter(_player);
        _player.Effects.Clear();
        _player.Effects.Add(EffectTypeScript.ImprovedInvisibility);
        Enter(_player);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void The_aura_enforces_the_aggro_range_and_line_of_sight_before_acquiring_anything()
    {
        _player.Position = new Vector3(9f, 0f, 0f);
        Enter(_player);
        _player.Position = new Vector3(8.5f, 0f, 0f);
        Bench.BlockSight(_player.Id, Npc.Id);
        Enter(_player);
        Bench.EnteringObject = Invalid;
        Service.CreatureAggroEnter();

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void The_aggro_range_includes_exactly_eight_and_a_half_meters()
    {
        _player.Position = new Vector3(8.5f, 0f, 0f);

        Service.IsInAggroRange(Npc.Id, _player.Id).Should().BeTrue();
        _player.Position = new Vector3(8.6f, 0f, 0f);
        Service.IsInAggroRange(Npc.Id, _player.Id).Should().BeFalse();
        Service.IsInAggroRange(Npc.Id, Invalid).Should().BeFalse();
        _player.Position = new Vector3(2f, 0f, 0f);
        _player.Area = Bench.AddArea("elsewhere");
        Service.IsInAggroRange(Npc.Id, _player.Id).Should().BeFalse();
    }

    [Test]
    public void The_aggro_range_needs_line_of_sight()
    {
        Service.IsInAggroRange(Npc.Id, _player.Id).Should().BeTrue();

        Bench.BlockSight(_player.Id, Npc.Id);

        Service.IsInAggroRange(Npc.Id, _player.Id).Should().BeFalse();
    }

    [Test]
    public void A_friendly_creature_in_the_aura_who_is_fighting_an_enemy_in_range_pulls_that_enemy_toward_this_creature()
    {
        var friend = Bench.AddCreature("friend", Area, 2f);
        Service.SetEnmity(friend.Id, _player.Id, 20);
        Bench.MakeEnemies(_player.Id, Npc.Id);

        Enter(friend);

        Bench.Trace().Should().Equal(Proximity(_player, Npc));
    }

    [Test]
    public void A_friendly_creature_is_ignored_when_it_is_in_another_faction_not_fighting_or_its_enemy_is_out_of_range()
    {
        var stranger = Bench.AddCreature("stranger", Area, 2f);
        stranger.Faction = 2;
        Service.SetEnmity(stranger.Id, _player.Id, 20);
        Enter(stranger);

        var idler = Bench.AddCreature("idler", Area, 2f);
        Enter(idler);

        var friend = Bench.AddCreature("friend", Area, 2f);
        Service.SetEnmity(friend.Id, _player.Id, 20);
        _player.Position = new Vector3(40f, 0f, 0f);
        Enter(friend);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void Allies_who_see_the_target_nearby_gain_proximity_enmity_too()
    {
        var ally = AddAlly("ally", 4f);

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player), Proximity(_player, Npc), Proximity(_player, ally));
    }

    [Test]
    public void Allies_without_line_of_sight_to_the_target_far_away_asleep_or_friendly_to_it_do_not_assist()
    {
        var blocked = AddAlly("blocked", 4f);
        Bench.BlockSight(_player.Id, blocked.Id);
        var far = AddAlly("far", 4f);
        far.Position = new Vector3(30f, 0f, 0f);
        var asleep = AddAlly("asleep", 4f);
        asleep.AILevel = AILevel.VeryLow;
        var friendly = Bench.AddCreature("friendly", Area, 4f);
        Perceive(friendly, seen: true, vanished: false);

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player), Proximity(_player, Npc));
    }

    [Test]
    public void A_creature_that_is_already_hunting_the_player_by_proximity_does_not_start_the_aggro_trigger_again()
    {
        Service.SetProximityEnmity(Npc.Id, _player.Id, 1);

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player));
        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }

    [Test]
    public void The_aura_does_not_acquire_a_dead_player()
    {
        _player.Dead = true;

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player));
    }

    [Test]
    public void The_aura_does_not_acquire_a_player_that_would_leash_the_creature()
    {
        Flags(AIFlag.ReturnHome);
        Npc.Position = new Vector3(60f, 0f, 0f);
        _player.Position = new Vector3(63f, 0f, 0f);
        Bench.AreaOfEffectCreator = Npc.Id;

        Enter(_player);

        Bench.Trace().Should().NotContain(note => note.StartsWith("ModifyProximityEnmity", StringComparison.Ordinal));
        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
    }

    [Test]
    public void A_stealth_detection_acquires_aggro_like_the_aura_does_and_only_for_creatures_with_AI()
    {
        Service.TryAcquireAggroAfterDetection(Npc.Id, _player.Id);
        Bench.Trace().Should().Contain(Proximity(_player, Npc));

        Bench.ClearCalls();
        Npc.AILevel = AILevel.VeryLow;
        Service.TryAcquireAggroAfterDetection(Npc.Id, _player.Id);
        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_stealth_detection_of_a_player_the_creature_cannot_acquire_does_nothing()
    {
        _player.Dead = true;

        Service.TryAcquireAggroAfterDetection(Npc.Id, _player.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_stealth_detection_of_the_creature_itself_or_an_invisible_player_does_nothing()
    {
        Service.TryAcquireAggroAfterDetection(Npc.Id, Npc.Id);
        _player.Effects.Add(EffectTypeScript.Invisibility);
        Service.TryAcquireAggroAfterDetection(Npc.Id, _player.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void Leaving_the_aura_completes_the_infiltration_and_drops_the_proximity_enmity_before_the_fight_resumes()
    {
        Service.SetProximityEnmity(Npc.Id, _player.Id, 1);
        var other = HostilePlayer("other", 5f);
        Service.SetEnmity(Npc.Id, other.Id, 5);

        Exit(_player);

        Bench.Trace().Should().Equal(
            $"CompleteInfiltration({_player.Id}, {Npc.Id})",
            Remove(_player, Npc),
            $"AttackHighestEnmityTarget({Npc.Id})");
        Service.Attacked[Npc.Id].Should().Be(other.Id);
    }

    [Test]
    public void Leaving_the_aura_with_nobody_else_to_fight_stops_the_chase_after_clearing_the_state()
    {
        Service.SetProximityEnmity(Npc.Id, _player.Id, 1);
        Npc.Position = new Vector3(10f, 0f, 0f);

        Exit(_player);

        Bench.Trace().Should().Equal($"CompleteInfiltration({_player.Id}, {Npc.Id})", Remove(_player, Npc));
        Bench.AssignedTo(Npc.Id).Should().ContainSingle();
        Bench.RunAssigned(Npc.Id);
        Bench.Engine.CountOf("ClearAllActions").Should().Be(1);
        Bench.Trace().Should().NotContain($"AttackHighestEnmityTarget({Npc.Id})");
    }

    [Test]
    public void Leaving_the_aura_when_the_proximity_threat_cannot_be_removed_changes_nothing_more()
    {
        Exit(_player);

        Bench.Trace().Should().Equal($"CompleteInfiltration({_player.Id}, {Npc.Id})", Remove(_player, Npc));
        Bench.AssignedTo(Npc.Id).Should().BeEmpty();
    }

    [Test]
    public void Leaving_the_aura_does_nothing_for_a_creature_without_AI_or_when_nobody_valid_left()
    {
        Npc.AILevel = AILevel.VeryLow;
        Exit(_player);
        Npc.AILevel = AILevel.Medium;
        Bench.ExitingObject = Invalid;
        Service.CreatureAggroExit();

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void Leaving_the_aura_of_an_enemy_that_would_leash_the_leaver_starts_the_leash_instead_of_removing_threat()
    {
        Flags(AIFlag.ReturnHome);
        Service.SetProximityEnmity(Npc.Id, _player.Id, 1);
        _player.Position = new Vector3(80f, 0f, 0f);

        Exit(_player);

        Bench.Trace().Should().NotContain(Remove(_player, Npc));
        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
    }

    [Test]
    public void Leaving_the_aura_also_releases_allies_that_can_no_longer_reach_the_leaver()
    {
        var ally = AddAlly("ally", 4f);
        var close = AddAlly("close", 3f);
        var asleep = AddAlly("asleep", 2f);
        asleep.AILevel = AILevel.VeryLow;
        Service.SetProximityEnmity(ally.Id, _player.Id, 1);
        _player.Position = new Vector3(30f, 0f, 0f);
        close.Position = new Vector3(29f, 0f, 0f);

        Exit(_player);

        Bench.Trace().Should().Contain(Remove(_player, ally));
        Bench.Trace().Should().NotContain(Remove(_player, close), "the leaver is still inside that ally's aggro range");
        Bench.Trace().Should().NotContain(Remove(_player, asleep));
    }

    [Test]
    public void A_dead_ally_leaves_the_cache_of_everyone_who_counted_it()
    {
        var ally = AddAlly("ally", 4f);
        // The ally keeps a cache of its own once it perceives something; its death removes it from every cache.
        Bench.Self = ally.Id;
        Perceive(Npc, seen: true, vanished: false);

        Service.CreatureDeath();
        Bench.Self = Npc.Id;
        Enter(_player);

        Bench.Trace().Should().NotContain(Proximity(_player, ally));
    }

    [Test]
    public void Destroying_an_object_that_was_never_cached_changes_nothing()
    {
        Bench.Self = Bench.AddCreature("stranger", Area).Id;

        Service.RemoveFromAlliesCache();

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_vanished_ally_stops_helping_and_a_heard_one_does_not_join()
    {
        var ally = AddAlly("ally", 4f);
        Perceive(ally, seen: false, vanished: true);
        var heard = Bench.AddCreature("heard", Area, 3f);
        Bench.MakeEnemies(_player.Id, heard.Id);
        Perceive(heard, seen: false, vanished: false);

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player), Proximity(_player, Npc));
    }

    [Test]
    public void Perception_ignores_the_creature_itself_players_the_dead_and_other_factions()
    {
        void Perceive(AISim perceived)
        {
            Bench.LastPerceived = perceived.Id;
            Bench.PerceptionSeen = true;
            Service.CreaturePerception();
        }

        Perceive(Npc);
        Perceive(_player);
        var dead = Bench.AddCreature("dead", Area, 2f);
        dead.Dead = true;
        Perceive(dead);
        var stranger = Bench.AddCreature("stranger", Area, 2f);
        stranger.Faction = 2;
        Perceive(stranger);
        Bench.MakeEnemies(_player.Id, dead.Id);
        Bench.MakeEnemies(_player.Id, stranger.Id);

        Enter(_player);

        Bench.Trace().Should().Equal(Begin(_player), Gate(_player), Proximity(_player, Npc));
    }
}
