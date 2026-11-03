#nullable enable
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// What an idle creature does on its heartbeat: walk home when it strayed past the return-home radius, wander when it
/// may, and leave alone a creature that is talking, fighting, walking or held by a control effect.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AIServiceIdleTests : AIServiceTestBase
{
    private const float ReturnHomeRadius = 15f;
    private const int WanderChancePercent = 40;

    private AISim _fighter = null!;

    [SetUp]
    public void PlaceAFighterNearby()
    {
        _fighter = HostilePlayer("fighter", 8f);
    }

    [TearDown]
    public void ResetTheDice()
    {
        GameRandom.ResetSeed();
    }

    private sealed record Commands(int Clears, int Homes, int Wanders, int Attacks);

    /// <summary>Runs one heartbeat of the NPC standing <paramref name="distance"/> meters from home and what it queued.</summary>
    private Commands Heartbeat(
        float distance,
        AIFlag flags = AIFlag.RandomWalk | AIFlag.ReturnHome,
        ActionType action = ActionType.RandomWalk,
        bool combat = false,
        bool conversation = false,
        bool threat = false,
        EffectTypeScript? effect = null)
    {
        Flags(flags);
        Npc.Position = new Vector3(distance, 0f, 0f);
        Npc.Action = action;
        Npc.InCombat = combat;
        Npc.InConversation = conversation;
        Npc.Effects.Clear();
        if (effect.HasValue)
            Npc.Effects.Add(effect.Value);
        Service.Tables.Clear();
        if (threat)
            Service.SetEnmity(Npc.Id, _fighter.Id, 10);
        Bench.ClearCalls();

        Service.CreatureHeartbeat();
        Bench.RunAssigned(Npc.Id);

        return new Commands(
            Bench.Engine.CountOf("ClearAllActions"),
            Bench.Engine.CountOf("ActionForceMoveToLocation"),
            Bench.Engine.CountOf("ActionRandomWalk"),
            // The heartbeat always ends with one attack call of its own.
            Bench.Trace().Count(note => note == $"AttackHighestEnmityTarget({Npc.Id})") - 1);
    }

    private static int SeedWhere(Func<int, bool> roll)
    {
        for (var seed = 1; seed < 10000; seed++)
        {
            GameRandom.SetSeed(seed);
            if (roll(GameRandom.D100(1)))
                return seed;
        }

        throw new InvalidOperationException("No seed gives that roll.");
    }

    [Test]
    public void Wandering_beyond_the_home_radius_clears_the_endless_walk_before_returning()
    {
        Heartbeat(ReturnHomeRadius + 0.1f).Should().Be(new Commands(1, 1, 0, 0));
    }

    [TestCase(0f)]
    [TestCase(14.9f)]
    [TestCase(15f)]
    public void Wandering_inside_the_home_radius_keeps_the_current_walk(float distance)
    {
        Heartbeat(distance).Should().Be(new Commands(0, 0, 0, 0));
    }

    [Test]
    public void Wandering_without_the_return_home_flag_is_not_interrupted()
    {
        Heartbeat(30f, flags: AIFlag.RandomWalk).Should().Be(new Commands(0, 0, 0, 0));
    }

    [Test]
    public void Existing_movement_outside_the_home_radius_is_not_restarted()
    {
        Heartbeat(30f, action: ActionType.MoveToPoint).Should().Be(new Commands(0, 0, 0, 0));
    }

    [Test]
    public void An_idle_creature_at_home_with_the_wander_flag_wanders_on_a_roll_of_forty_or_less()
    {
        GameRandom.SetSeed(SeedWhere(roll => roll <= WanderChancePercent));

        Heartbeat(0f, action: ActionType.Invalid).Should().Be(new Commands(0, 0, 1, 0));
    }

    [Test]
    public void An_idle_creature_at_home_stays_put_on_a_roll_above_forty()
    {
        GameRandom.SetSeed(SeedWhere(roll => roll > WanderChancePercent));

        Heartbeat(0f, action: ActionType.Invalid).Should().Be(new Commands(0, 0, 0, 0));
    }

    [Test]
    public void The_wander_decision_takes_exactly_one_roll_and_a_creature_that_returns_home_takes_none()
    {
        var seed = SeedWhere(roll => roll <= WanderChancePercent);
        GameRandom.SetSeed(seed);
        var firstRoll = GameRandom.D100(1);
        var secondRoll = GameRandom.D100(1);

        GameRandom.SetSeed(seed);
        Heartbeat(0f, action: ActionType.Invalid);
        GameRandom.D100(1).Should().Be(secondRoll, "the heartbeat drew one die");

        GameRandom.SetSeed(seed);
        Heartbeat(30f, action: ActionType.Invalid);
        GameRandom.D100(1).Should().Be(firstRoll, "a creature walking home does not roll for a wander");
    }

    [Test]
    public void An_idle_creature_outside_the_home_radius_queues_the_walk_home_without_clearing_anything()
    {
        Heartbeat(30f, action: ActionType.Invalid).Should().Be(new Commands(0, 1, 0, 0));
    }

    [Test]
    public void A_creature_without_the_wander_flag_never_wanders()
    {
        GameRandom.SetSeed(SeedWhere(roll => roll <= WanderChancePercent));

        Heartbeat(0f, flags: AIFlag.ReturnHome, action: ActionType.Invalid).Should().Be(new Commands(0, 0, 0, 0));
    }

    [Test]
    public void Conversation_and_combat_keep_their_actions_outside_the_home_radius()
    {
        Heartbeat(30f, conversation: true).Should().Be(new Commands(0, 0, 0, 0));
        Heartbeat(30f, combat: true).Should().Be(new Commands(0, 0, 0, 0));
        Heartbeat(30f, threat: true).Should().Be(new Commands(0, 0, 0, 1), "the creature fights the threat it has");
        Heartbeat(30f, conversation: true, threat: true).Should().Be(new Commands(0, 0, 0, 0), "a talking creature is not pulled into the fight");
    }

    [TestCase(EffectTypeScript.Dazed)]
    [TestCase(EffectTypeScript.Petrify)]
    public void A_creature_held_by_a_control_effect_gets_no_idle_commands(EffectTypeScript effect)
    {
        Heartbeat(30f, effect: effect).Should().Be(new Commands(0, 0, 0, 0));
    }

    [TestCase(EffectTypeScript.Dazed)]
    [TestCase(EffectTypeScript.Petrify)]
    public void A_creature_held_by_a_control_effect_does_not_resume_its_fight_early(EffectTypeScript effect)
    {
        Heartbeat(30f, threat: true, effect: effect).Should().Be(new Commands(0, 0, 0, 0));
    }

    [Test]
    public void An_effect_that_does_not_hold_the_creature_does_not_stop_the_idle_walk_home()
    {
        Heartbeat(30f, action: ActionType.Invalid, effect: EffectTypeScript.Invisibility).Should().Be(new Commands(0, 1, 0, 0));
    }
}
