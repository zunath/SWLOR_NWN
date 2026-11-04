using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

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

        /// <summary>The output items currently in the inventory.</summary>
        public int Created { get; private set; }

        /// <summary>When true, the inventory has no room for the made item.</summary>
        public bool InventoryFull { get; set; }

        /// <summary>When true, taking the components fails even though <see cref="HasComponents"/> said they were there.</summary>
        public bool ConsumeFails { get; set; }

        public bool IsPlayerAvailable(uint player) => PlayerAvailable;

        public void BeginBatch(uint player) => Events.Add("begin");

        public void PlayStationWork(uint player, SkillType skill) => Events.Add($"loop:{skill}");

        public void EndBatch(uint player) => Events.Add("end");

        public void PlayItemAnimation(uint player, float seconds) => Events.Add($"animate:{seconds}");

        public bool HasComponents(uint player, RecipeDetail recipe) =>
            recipe.Components.All(c => Inventory.TryGetValue(c.Key, out var have) && have >= c.Value);

        public bool CreateItem(uint player, RecipeDetail recipe)
        {
            if (InventoryFull)
            {
                Events.Add("create-failed");
                return false;
            }

            Created++;
            Events.Add($"create:{recipe.Resref}");
            return true;
        }

        public bool TryConsumeComponents(uint player, RecipeDetail recipe)
        {
            if (ConsumeFails || !HasComponents(player, recipe))
            {
                Events.Add("consume-failed");
                return false;
            }

            foreach (var (resref, quantity) in recipe.Components)
                Inventory[resref] -= quantity;

            Events.Add("consume");
            return true;
        }

        public void RemoveCreatedItem(uint player, RecipeDetail recipe)
        {
            Created--;
            Events.Add("remove-created");
        }

        public List<string> Logged { get; } = new();

        public void LogCraft(uint player, string text) => Logged.Add(text);

        public void Schedule(float seconds, Action action) => Scheduled.Enqueue(action);

        public void Message(uint player, string text) => Messages.Add(text);

        /// <summary>Lets the animation for the item in progress finish.</summary>
        public void FinishAnimation() => Scheduled.Dequeue().Invoke();
    }

    private static RecipeDetail Recipe() => new()
    {
        Resref = "gadget",
        Skill = SkillType.Smithery,
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
        QuickCraftRules.Evaluate(Recipe(), "Smithery", rank, craftedByHand).IsEligible.Should().Be(expected);
    }

    [Test]
    public void Eligibility_SaysWhyItIsRefused()
    {
        QuickCraftRules.Evaluate(Recipe(), "Smithery", 24, true).Reason.Should().Be("Quick craft needs Smithery rank 25.");
        QuickCraftRules.Evaluate(Recipe(), "Smithery", 25, false).Reason.Should().Be("Craft this recipe by hand once to unlock quick craft.");
        QuickCraftRules.Evaluate(Recipe(), "Smithery", 24, false).Reason.Should().Be("Quick craft needs Smithery rank 25.");
        QuickCraftRules.Evaluate(Recipe(), "Smithery", 25, true).Reason.Should().BeEmpty();
    }

    [Test]
    public void Eligibility_WaitsForThePracticeLimitOfARecipeThatKeepsTraining()
    {
        // A level 3 recipe would open at rank 8, but it trains until rank 15.
        var practice = new RecipeDetail { Level = 3, PracticeRankLimit = 15 };

        QuickCraftRules.GetRequiredRank(practice).Should().Be(15);
        QuickCraftRules.Evaluate(practice, "Espionage", 14, true).Reason.Should().Be("Quick craft needs Espionage rank 15.");
        QuickCraftRules.Evaluate(practice, "Espionage", 15, true).IsEligible.Should().BeTrue();

        // A limit below the usual margin changes nothing.
        QuickCraftRules.GetRequiredRank(new RecipeDetail { Level = 20, PracticeRankLimit = 10 }).Should().Be(25);
    }

    [Test]
    public void Eligibility_OpensOnlyWhereTheRecipeGivesNoXp()
    {
        foreach (var limit in new int?[] { null, 5, 15, 30, 55 })
        for (var level = 0; level <= 50; level += 3)
        {
            var recipe = new RecipeDetail { Level = level, PracticeRankLimit = limit };
            for (var rank = 0; rank <= 60; rank++)
            {
                if (rank >= QuickCraftRules.GetRequiredRank(recipe))
                {
                    Craft.GetBaseRecipeXP(recipe, rank).Should().Be(0, $"level {level}, limit {limit}, rank {rank}");
                }
            }
        }
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
        world.Events.Should().Equal("begin", "loop:Smithery", $"animate:{QuickCraftBatch.SecondsPerItem}");
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

    [TestCase(SkillType.Smithery)]
    [TestCase(SkillType.Engineering)]
    [TestCase(SkillType.Fabrication)]
    [TestCase(SkillType.Agriculture)]
    [TestCase(SkillType.Espionage)]
    public void Batch_StartsTheStationClipOfTheRecipesSkillOnceForTheWholeBatch(SkillType skill)
    {
        var world = WorldWith(10, 10);
        var recipe = Recipe();
        recipe.Skill = skill;
        var batch = new QuickCraftBatch(world, Player, recipe, 3);

        batch.Start();
        world.FinishAnimation();
        world.FinishAnimation();
        world.FinishAnimation();

        world.Events.Where(e => e.StartsWith("loop")).Should().Equal($"loop:{skill}");
        world.Events.IndexOf($"loop:{skill}").Should().Be(1, "the loop starts right after the player is held at the station, before the first item");
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
    public void Batch_MakesTheItemBeforeTakingTheComponents()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 1);
        batch.Start();

        world.FinishAnimation();

        world.Events.Where(e => e is "consume" or "create:gadget").Should().Equal("create:gadget", "consume");
    }

    [Test]
    public void Batch_WhenTheItemDoesNotFit_TakesNothing_DoesNotCountIt_AndStopsWithAMessage()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        batch.Start();
        world.FinishAnimation();
        world.InventoryFull = true;

        world.FinishAnimation();

        batch.Completed.Should().Be(1, "the item that could not be made is not counted");
        batch.Ended.Should().Be(QuickCraftEnd.InventoryFull);
        batch.IsRunning.Should().BeFalse();
        world.Created.Should().Be(1);
        world.Inventory["ore"].Should().Be(8, "only the first item's components are spent");
        world.Inventory["wire"].Should().Be(9);
        world.Events.Count(e => e == "consume").Should().Be(1, "the failed item takes nothing");
        world.Events.Should().ContainInOrder("create-failed", "end");
        world.Scheduled.Should().BeEmpty("no further item is started");
        world.Messages.Should().ContainSingle().Which
            .Should().Be("Quick craft stopped: your inventory is full after 1 item. Your materials were not used; make room and try again.");
        world.Logged.Should().ContainSingle().Which.Should().Contain("could not fit").And.Contain("gadget");
    }

    [Test]
    public void Batch_WhenTheFirstItemDoesNotFit_MakesNothingAndSpendsNothing()
    {
        var world = WorldWith(10, 10);
        world.InventoryFull = true;
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        batch.Start();

        world.FinishAnimation();

        batch.Completed.Should().Be(0);
        batch.Ended.Should().Be(QuickCraftEnd.InventoryFull);
        world.Created.Should().Be(0);
        world.Inventory["ore"].Should().Be(10);
        world.Inventory["wire"].Should().Be(10);
        world.Events.Should().NotContain("consume").And.NotContain("consume-failed");
        world.Messages.Should().ContainSingle().Which.Should().Contain("inventory is full after 0 items");
    }

    [Test]
    public void Batch_WhenTheComponentsCannotBeTakenAfterTheItemIsMade_RemovesTheItemAndGainsNothing()
    {
        var world = WorldWith(10, 10);
        var batch = new QuickCraftBatch(world, Player, Recipe(), 3);
        batch.Start();
        world.FinishAnimation();
        world.ConsumeFails = true;

        world.FinishAnimation();

        batch.Completed.Should().Be(1, "the item made without its components is not counted");
        batch.Ended.Should().Be(QuickCraftEnd.OutOfMaterials);
        world.Created.Should().Be(1, "the free item is removed again, leaving only the first item");
        world.Inventory["ore"].Should().Be(8);
        world.Inventory["wire"].Should().Be(9);
        world.Events.Should().ContainInOrder("create:gadget", "consume-failed", "remove-created", "end");
        world.Scheduled.Should().BeEmpty("no further item is started");
        world.Logged.Should().ContainSingle().Which.Should().Contain("gadget").And.Contain("removed");
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
