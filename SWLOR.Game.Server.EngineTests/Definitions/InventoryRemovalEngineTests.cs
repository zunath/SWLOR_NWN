using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The transfer journal and City Project donations save a character file right after taking items from it. Native destruction waits
    /// for the end of the script, so the items must be out of the inventory (parked in a holding container) before that save, and
    /// destroyed after it.
    /// </summary>
    public static class InventoryRemovalEngineTests
    {
        private const string Resref = "herb_m";

        [EngineTest("A destroyed item stays in the inventory until the script returns", Category = "InventoryRemoval")]
        public static async Task DestroyObjectAloneIsDeferred(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var item = CreateItemOnObject(Resref, owner, 1);
                ctx.Assert(GetIsObjectValid(item), "The fixture holds an item");
                DestroyObject(item);
                ctx.AssertEqual(1, FarmItems.Count(owner, Resref), "A character saved in this script would still hold the destroyed item");
            });

            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.AssertEqual(0, FarmItems.Count(owner, Resref), "The item is gone once the script has returned");
            });
        }

        [EngineTest("Taken items leave the inventory at once, come back on restore, and are destroyed after the save", Category = "InventoryRemoval", TimeoutSeconds = 60)]
        public static async Task TakenItemsLeaveAtOnce(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var stack = OBJECT_INVALID;
            var container = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                stack = CreateItemOnObject(Resref, owner, 5);
                ctx.Assert(GetIsObjectValid(stack), "The fixture holds a stack of 5");

                // Cutting a stack shows at once, and a stack taken whole is out of the inventory at once.
                var removal = new InventoryRemoval(owner);
                ctx.Assert(removal.TryTake(Resref, 3), "Three of five can be taken");
                ctx.AssertEqual(2, FarmItems.Count(owner, Resref), "A cut stack shows its new size at once");
                ctx.Assert(removal.TryTake(Resref, 2), "The rest can be taken");
                ctx.AssertEqual(0, FarmItems.Count(owner, Resref), "A stack taken whole is out of the inventory at once");
                ctx.Assert(GetItemPossessor(stack) != owner, "The parked stack no longer belongs to the character");
                ctx.Assert(!removal.TryTake(Resref, 1), "Nothing is left to take");

                // A failed save puts the same items back.
                removal.Restore();
                ctx.AssertEqual(5, FarmItems.Count(owner, Resref), "Restore puts everything back");
                ctx.AssertEqual(owner, GetItemPossessor(stack), "The same item returns to the character");
                ctx.AssertEqual(5, GetItemStackSize(stack), "The stack has its old size");

                // A save that worked destroys what was parked, after the save.
                var second = new InventoryRemoval(owner);
                ctx.Assert(second.TryTake(Resref, 5), "All five can be taken again");
                container = GetItemPossessor(stack, true);
                ctx.Assert(GetIsObjectValid(container) && container != owner, "The stack waits in a holding container");
                second.Destroy();
                ctx.Assert(GetIsObjectValid(stack), "Destruction is still deferred until this script returns");
            });

            await ctx.DelaySecondsAsync(0.3f);
            ctx.Assert(!GetIsObjectValid(stack), "The parked stack is destroyed after the script returns");
            ctx.Assert(!GetIsObjectValid(container), "The holding container is destroyed after the script returns");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.AssertEqual(0, FarmItems.Count(owner, Resref), "The character has none left");
            });
        }

        [EngineTest("A single item used up leaves the inventory at once", Category = "InventoryRemoval")]
        public static async Task UsingUpOneItem(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var pair = CreateItemOnObject(Resref, owner, 2);
                var removal = new InventoryRemoval(owner);
                ctx.Assert(removal.TryUseUpOne(pair), "One of a stack of two can be used up");
                ctx.AssertEqual(1, GetItemStackSize(pair), "The stack is cut by one");
                ctx.Assert(removal.TryUseUpOne(pair), "The last one can be used up");
                ctx.AssertEqual(0, FarmItems.Count(owner, Resref), "The last item is out of the inventory at once");
                ctx.Assert(!removal.TryUseUpOne(pair), "An item the character no longer holds cannot be used up");
                removal.Restore();
                ctx.AssertEqual(2, FarmItems.Count(owner, Resref), "Restore gives both back");
                var after = new InventoryRemoval(owner);
                after.TryTake(Resref, 2);
                after.Destroy();
            });

            await ctx.DelaySecondsAsync(0.3f);
        }
    }
}
