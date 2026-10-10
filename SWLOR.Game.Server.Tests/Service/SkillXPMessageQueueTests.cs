using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class SkillXPMessageQueueTests
{
    [Test]
    public void SameKillCombinesAllAwardsIntoOneMessageAfterTheFrame()
    {
        var callbacks = new List<Action>();
        var messages = new List<(uint Player, SkillType Skill, int XP)>();
        var queue = new SkillXPMessageQueue(callbacks.Add, (player, skill, xp) => messages.Add((player, skill, xp)));

        queue.Add(1, 10, SkillType.Mimicry, 263); // Active-use award after its adjustments.
        queue.Add(1, 10, SkillType.Mimicry, 21);  // Independently capped observation award.
        queue.Add(1, 10, SkillType.Mimicry, 350); // New-technique learning award.

        messages.Should().BeEmpty();
        callbacks.Should().ContainSingle();
        callbacks[0]();
        messages.Should().Equal((1u, SkillType.Mimicry, 634));
    }

    [Test]
    public void SeparateKillsPlayersAndSkillsKeepIndependentTotals()
    {
        var callbacks = new List<Action>();
        var messages = new List<(uint Player, SkillType Skill, int XP)>();
        var queue = new SkillXPMessageQueue(callbacks.Add, (player, skill, xp) => messages.Add((player, skill, xp)));

        queue.Add(1, 10, SkillType.Mimicry, 11);
        queue.Add(1, 11, SkillType.Mimicry, 22);
        queue.Add(2, 10, SkillType.Mimicry, 33);
        queue.Add(1, 10, SkillType.Force, 44);
        queue.Add(1, 10, SkillType.Mimicry, 5);

        callbacks.Should().HaveCount(4);
        foreach (var callback in callbacks)
            callback();
        messages.Should().BeEquivalentTo(new[]
        {
            (1u, SkillType.Mimicry, 16), (1u, SkillType.Mimicry, 22),
            (2u, SkillType.Mimicry, 33), (1u, SkillType.Force, 44)
        });
    }

    [Test]
    public void EmptyAwardsDoNotScheduleMessages()
    {
        var callbacks = new List<Action>();
        var queue = new SkillXPMessageQueue(callbacks.Add, (_, _, _) => Assert.Fail("An empty award must not produce feedback."));
        queue.Add(1, 10, SkillType.Mimicry, 0);
        queue.Add(1, 10, SkillType.Mimicry, -1);
        callbacks.Should().BeEmpty();
    }

    [Test]
    public void SendingRemovesThePreviousTotalBeforeAnotherAward()
    {
        var callbacks = new List<Action>();
        var totals = new List<int>();
        var queue = new SkillXPMessageQueue(callbacks.Add, (_, _, xp) => totals.Add(xp));
        queue.Add(1, 10, SkillType.Mimicry, 10);
        callbacks[0]();
        queue.Add(1, 10, SkillType.Mimicry, 20);
        callbacks.Should().HaveCount(2);
        callbacks[1]();
        totals.Should().Equal(10, 20);
    }
}
