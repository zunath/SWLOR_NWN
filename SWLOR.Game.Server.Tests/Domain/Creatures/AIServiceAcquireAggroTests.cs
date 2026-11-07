#nullable enable
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// <c>AcquireAggroFromNearbyEnemies</c> on the real AI instance: the creatures a target's change of state hands to the
/// normal proximity-aggro path, who is skipped, and the order of the effects.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIServiceAcquireAggroTests : AIServiceTestBase
{
    private AISim _player = null!;

    [SetUp]
    public void PlaceAPlayerNextToTheCreature()
    {
        _player = HostilePlayer("player", 3f);
    }

    private string Proximity(AISim enemy) => $"ModifyProximityEnmity({_player.Id}, {enemy.Id}, 1)";

    private AISim HostileCreature(string name, float x)
    {
        var creature = Bench.AddCreature(name, Area, x);
        Bench.MakeEnemies(_player.Id, creature.Id);
        return creature;
    }

    /// <summary>The creature sees the ally, which fills the ally cache that lets it share the target.</summary>
    private void SeeAsAlly(AISim ally)
    {
        Bench.LastPerceived = ally.Id;
        Bench.PerceptionSeen = true;
        Bench.PerceptionVanished = false;
        Service.CreaturePerception();
    }

    [Test]
    public void A_hostile_creature_with_AI_in_range_acquires_the_target_and_runs_its_aggro_profile()
    {
        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().Equal(Proximity(Npc));
        Service.Proximity[Npc.Id].Should().Contain(_player.Id);
        Bench.Engine.CallsTo("ExecuteScript").Should().ContainSingle(call => (string)call.Arguments[0]! == ScriptedProfiles.DefaultScript);
    }

    [Test]
    public void The_search_is_a_creature_sphere_of_eight_and_a_half_meters_around_the_target()
    {
        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        var search = Bench.Engine.CallsTo("GetFirstObjectInShape").Should().ContainSingle().Subject;
        search.Arguments[0].Should().Be(Shape.Sphere);
        search.Arguments[1].Should().Be(8.5f);
        search.Arguments[3].Should().Be(false);
        search.Arguments[4].Should().Be(ObjectType.Creature);
    }

    [Test]
    public void Every_hostile_creature_in_range_acquires_the_target_in_the_order_the_search_finds_them()
    {
        var second = HostileCreature("second", 4f);
        var third = HostileCreature("third", -2f);

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().Equal(Proximity(Npc), Proximity(second), Proximity(third));
        Bench.Engine.CallsTo("ExecuteScript").Should().HaveCount(3);
    }

    [Test]
    public void A_creature_runs_its_aggro_profile_before_its_allies_gain_enmity_and_before_the_next_creature_acquires()
    {
        var ally = HostileCreature("ally", 4f);
        SeeAsAlly(ally);
        var next = HostileCreature("next", 5f);
        Bench.ClearCalls();

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        var lines = Bench.Trace("ExecuteScript");
        var scripts = lines.Select((line, index) => (line, index))
            .Where(entry => entry.line.StartsWith("ExecuteScript", StringComparison.Ordinal))
            .Select(entry => entry.index)
            .ToList();
        scripts.Should().HaveCount(2, "the ally already holds the enmity its neighbour gave it, so only the creature and the next creature run the profile");
        var acquired = lines.IndexOf(Proximity(Npc));
        var allyGained = lines.IndexOf(Proximity(ally));
        var nextGained = lines.IndexOf(Proximity(next));
        acquired.Should().BeGreaterThanOrEqualTo(0);
        acquired.Should().BeLessThan(scripts[0]);
        scripts[0].Should().BeLessThan(allyGained, "the profile runs before the allies are added");
        allyGained.Should().BeLessThan(nextGained);
        nextGained.Should().BeLessThan(scripts[1]);
    }

    [Test]
    public void A_player_in_range_does_not_acquire_the_target()
    {
        Npc.IsPC = true;

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_creature_without_AI_does_not_acquire_the_target()
    {
        Npc.AILevel = AILevel.VeryLow;

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }

    [Test]
    public void A_creature_that_is_not_hostile_to_the_target_does_not_acquire_it()
    {
        var neutral = Bench.AddPlayer("neutral", Area, 1f);

        Service.AcquireAggroFromNearbyEnemies(neutral.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_creature_beyond_the_aggro_radius_of_the_target_does_not_acquire_it()
    {
        Npc.Position = new Vector3(20f, 0f, 0f);

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_creature_without_line_of_sight_to_the_target_does_not_acquire_it()
    {
        Bench.BlockSight(_player.Id, Npc.Id);

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void Nobody_acquires_a_dead_or_invisible_target()
    {
        _player.Dead = true;
        Service.AcquireAggroFromNearbyEnemies(_player.Id);
        _player.Dead = false;
        _player.Effects.Add(EffectTypeScript.Invisibility);
        Service.AcquireAggroFromNearbyEnemies(_player.Id);
        _player.Effects.Clear();
        _player.Effects.Add(EffectTypeScript.ImprovedInvisibility);
        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
    }

    [Test]
    public void A_creature_that_already_holds_proximity_enmity_for_the_target_does_not_start_again()
    {
        Service.SetProximityEnmity(Npc.Id, _player.Id, 1);

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }

    [Test]
    public void A_creature_that_would_leash_evades_instead_of_acquiring_the_target()
    {
        Flags(AIFlag.ReturnHome);
        Npc.Position = new Vector3(60f, 0f, 0f);
        _player.Position = new Vector3(63f, 0f, 0f);

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().NotContain(note => note.StartsWith("ModifyProximityEnmity", StringComparison.Ordinal));
        Bench.Locals.Int(Npc.Id, "AI_LEASH_EVADE_ACTIVE").Should().Be(1);
    }

    [Test]
    public void Nothing_happens_when_no_creature_is_near_the_target()
    {
        Npc.Destroyed = true;

        Service.AcquireAggroFromNearbyEnemies(_player.Id);

        Bench.Trace().Should().BeEmpty();
        Bench.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }
}
