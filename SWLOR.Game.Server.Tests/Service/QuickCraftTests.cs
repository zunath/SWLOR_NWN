using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers quick craft: who may use it, how large a batch is, and how a batch runs, cancels and stops.</summary>
public class QuickCraftTests
{
    private const uint Player = 1;

    private sealed class FakeQuickCraftWorld : IQuickCraftWorld
    {
        public Dictionary<string, int> Inventory { get; } = new();
        public bool PlayerAvailable { get; set; } = true;
        public Queue<Action> Scheduled { get; } = new();
        public List<string> Events { get; } = new();
        public List<string> Messages { get; } = new();
        public int Created { get; private set; }

        public bool IsPlayerAvailable(uint player) => PlayerAvailable;

        public void BeginBatch(uint player) => Events.Add("begin");

        public void EndBatch(uint player) => Events.Add("end");

        public void PlayItemAnimation(uint player, float seconds) => Events.Add($"animate:{seconds}");

        public bool TryConsumeComponents(uint player, RecipeDetail recipe)
        {
            if (recipe.Components.Any(c => !Inventory.TryGetValue(c.Key, out var have) || have < c.Value))
                return false;

            foreach (var (resref, quantity) in recipe.Components)
                Inventory[resref] -= quantity;

            Events.Add("consume");
            return true;
        }

        public void CreateItem(uint player, RecipeDetail recipe)
        {
            Created++;
            Events.Add($"create:{recipe.Resref}");
        }

        public void Schedule(float seconds, Action action) => Scheduled.Enqueue(action);

        public void Message(uint player, string text) => Messages.Add(text);

        /// <summary>Lets the animation for the item in progress finish.</summary>
        public void FinishAnimation() => Scheduled.Dequeue().Invoke();
    }

    private static RecipeDetail Recipe() => new()
    {
        Resref = "gadget",
        Level = 20,
        Components = new Dictionary<string, int> { ["ore"] = 2, ["wire"] = 1 }
    };

    private static FakeQuickCraftWorld WorldWith(int ore, int wire)
    {
        var world = new FakeQuickCraftWorld();
        world.Inventory["ore"] = ore;
        world.Inventory["wire"] = wire;
        return world;
    }

    [TestCase(25, true, true)]
    [TestCase(24, true, false)]
    [TestCase(50, true, true)]
    [TestCase(25, false, false)]
    [TestCase(10, false, false)]
    public void Eligibility_NeedsRankFiveAboveTheRecipeAndOneManualCraft(int rank, bool craftedByHand, bool expected)
    {
        QuickCraftRules.Evaluate(20, "Smithery", rank, craftedByHand).IsEligible.Should().Be(expected);
    }

    [Test]
    public void Eligibility_SaysWhyItIsRefused()
    {
        QuickCraftRules.Evaluate(20, "Smithery", 24, true).Reason.Should().Be("Quick craft needs Smithery rank 25.");
        QuickCraftRules.Evaluate(20, "Smithery", 25, false).Reason.Should().Be("Craft this recipe by hand once to unlock quick craft.");
        QuickCraftRules.Evaluate(20, "Smithery", 24, false).Reason.Should().Be("Quick craft needs Smithery rank 25.");
        QuickCraftRules.Evaluate(20, "Smithery", 25, true).Reason.Should().BeEmpty();
    }

    [Test]
    public void Eligibility_UsesTheSharedMarginAndBatchLimit()
    {
        QuickCraftRules.RankMargin.Should().Be(5);
        QuickCraftRules.MaxBatchSize.Should().Be(10);
    }

    [TestCase(10, 100, 100, 10)]
    [TestCase(25, 100, 100, 10)]
    [TestCase(4, 100, 100, 4)]
    [TestCase(10, 7, 100, 3)]
    [TestCase(10, 100, 2, 2)]
    [TestCase(10, 1, 100, 0)]
    [TestCase(0, 100, 100, 0)]
    [TestCase(-3, 100, 100, 0)]
    public void BatchSize_IsLimitedByTheRequestTheCapAndTheMaterials(int requested, int ore, int wire, int expected)
    {
        var world = WorldWith(ore, wire);

        QuickCraftRules.ResolveBatchSize(requested, Recipe(), resref => world.Inventory[resref]).Should().Be(expected);
    }

    [Test]
    public void Batch_MakesEachItemAfterItsAnimationAndKeepsThePlayerAtTheStation()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        QuickCraftEnd? ended = null;
        batch.OnEnded += finished => ended = finished.Ended;

        batch.Start();
        world.Events.Should().Equal("begin", $"animate:{QuickCraftBatch.SecondsPerItem}");
        world.Created.Should().Be(0, "the first item takes its animation to make");

        world.FinishAnimation();
        world.FinishAnimation();
        world.FinishAnimation();

        batch.Completed.Should().Be(3);
        batch.IsRunning.Should().BeFalse();
        ended.Should().Be(QuickCraftEnd.Completed);
        world.Inventory["ore"].Should().Be(4);
        world.Inventory["wire"].Should().Be(7);
        world.Events.Count(e => e == "begin").Should().Be(1);
        world.Events.Count(e => e == "end").Should().Be(1);
        world.Events.Count(e => e.StartsWith("animate")).Should().Be(3);
        world.Events.Where(e => e.StartsWith("create")).Should().OnlyContain(e => e == "create:gadget");
        world.Messages.Should().ContainSingle().Which.Should().Be("Quick craft finished: 3 items made.");
    }

    [Test]
    public void Batch_CancelStopsAfterTheItemsAlreadyMadeAndLeavesTheRestOfTheMaterials()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 5);
        batch.Start();
        world.FinishAnimation();

        batch.Cancel();
        world.FinishAnimation();

        batch.Completed.Should().Be(1);
        batch.Ended.Should().Be(QuickCraftEnd.Cancelled);
        world.Created.Should().Be(1, "the animation that was running when the batch was cancelled makes nothing");
        world.Inventory["ore"].Should().Be(8);
        world.Inventory["wire"].Should().Be(9);
        world.Events.Count(e => e == "end").Should().Be(1);
        world.Messages.Should().ContainSingle().Which.Should().Contain("cancelled").And.Contain("1 item made");
    }

    [Test]
    public void Batch_CancelledDuringTheFirstAnimation_MakesAndTakesNothing()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 5);
        batch.Start();

        batch.Cancel();
        world.FinishAnimation();

        batch.Completed.Should().Be(0);
        world.Inventory["ore"].Should().Be(10);
        world.Events.Should().NotContain("consume");
        batch.Cancel();
        world.Events.Count(e => e == "end").Should().Be(1, "cancelling twice does nothing");
    }

    [Test]
    public void Batch_StopsWhenTheMaterialsRunOutBetweenItems()
    {
        var world = WorldWith(4, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        batch.Start();

        world.FinishAnimation();
        world.FinishAnimation();
        world.FinishAnimation();

        batch.Completed.Should().Be(2);
        batch.Ended.Should().Be(QuickCraftEnd.OutOfMaterials);
        world.Messages.Should().ContainSingle().Which.Should().Contain("ran out of materials");
    }

    [Test]
    public void Batch_StopsWhenThePlayerCanNoLongerCraft()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        batch.Start();
        world.FinishAnimation();
        world.PlayerAvailable = false;

        world.FinishAnimation();

        batch.Completed.Should().Be(1);
        batch.Ended.Should().Be(QuickCraftEnd.Interrupted);
        world.Messages.Should().BeEmpty("a player who cannot be reached is not messaged");
    }

    [Test]
    public void Batch_InterruptEndsItLikeACancel()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        batch.Start();

        batch.Interrupt();

        batch.Ended.Should().Be(QuickCraftEnd.Interrupted);
        world.Events.Count(e => e == "end").Should().Be(1);
    }

    [Test]
    public void Batch_StartsOnlyOnce()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 2);

        batch.Start();
        batch.Start();

        world.Events.Count(e => e == "begin").Should().Be(1);
        world.Scheduled.Should().HaveCount(1);
    }

    [Test]
    public void Batch_OfNothing_EndsAtOnceWithoutHoldingThePlayer()
    {
        var world = WorldWith(0, 0);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 0);

        batch.Start();

        batch.IsRunning.Should().BeFalse();
        world.Events.Should().BeEmpty();
    }
}
