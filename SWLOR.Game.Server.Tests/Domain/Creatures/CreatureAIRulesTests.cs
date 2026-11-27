#nullable enable
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The engine-only creature AI checks Combat, Character, Space and the feature scripts call without an AI interface:
/// they read the same engine state the creature AI reads.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CreatureAIRulesTests : AIServiceTestBase
{
    [Test]
    public void A_creature_runs_AI_unless_it_is_invalid_or_at_the_lowest_AI_level()
    {
        CreatureAIRules.IsCreatureAIEnabled(Npc.Id).Should().BeTrue();
        CreatureAIRules.IsCreatureAIEnabled(Invalid).Should().BeFalse();

        Npc.AILevel = AILevel.VeryLow;

        CreatureAIRules.IsCreatureAIEnabled(Npc.Id).Should().BeFalse();
    }

    [Test]
    public void A_creature_is_leash_evading_while_its_evade_flag_is_set()
    {
        CreatureAIRules.IsLeashEvading(Npc.Id).Should().BeFalse();

        Bench.Engine.SetReturnHandler("GetLocalBool", arguments =>
            (string)arguments[1]! == CreatureAIRules.LeashEvadeActiveVariable && (uint)arguments[0]! == Npc.Id);

        CreatureAIRules.IsLeashEvading(Npc.Id).Should().BeTrue();
        CreatureAIRules.IsLeashEvading(Invalid).Should().BeFalse();
    }

    [Test]
    public void A_creature_is_outside_its_home_radius_beyond_fifteen_meters_or_in_another_area()
    {
        var home = Bench.SetHome(Npc.Id, Area);

        Npc.Position = new Vector3(CreatureAIRules.ReturnHomeRadius, 0f, 0f);
        CreatureAIRules.IsOutsideHomeRadius(Npc.Id, home).Should().BeFalse();

        Npc.Position = new Vector3(CreatureAIRules.ReturnHomeRadius + 0.5f, 0f, 0f);
        CreatureAIRules.IsOutsideHomeRadius(Npc.Id, home).Should().BeTrue();
        CreatureAIRules.IsOutsideHomeRadius(Npc.Id, home, 30f).Should().BeFalse();

        Npc.Position = Vector3.Zero;
        Npc.Area = Bench.AddArea("elsewhere");
        CreatureAIRules.IsOutsideHomeRadius(Npc.Id, home).Should().BeTrue();
    }
}
