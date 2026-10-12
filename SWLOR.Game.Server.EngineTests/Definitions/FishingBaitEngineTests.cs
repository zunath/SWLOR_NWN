using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class FishingBaitEngineTests
    {
        private const string BaitResref = "little_worm";

        [EngineTest("Unloading partly used bait onto a partial stack returns only the remaining bait", Category = "Fishing")]
        public static async Task UnloadOntoPartialStack(EngineTestContext ctx)
        {
            var apply = new FishingRodItemDefinition().BuildItems()[Fishing.FishingRodTag].ApplyAction;
            var user = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();

            var rod = OBJECT_INVALID;
            var loaded = OBJECT_INVALID;
            var replacement = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(user, () =>
            {
                rod = CreateItemOnObject("bamboo_rod", user);
                loaded = CreateItemOnObject(BaitResref, user, 99);
                ctx.Assert(GetIsObjectValid(rod), "Fixture rod exists");
                ctx.AssertEqual(99, GetItemStackSize(loaded), "Fixture bait stack contains 99");
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(user, () =>
            {
                apply(user, rod, loaded, GetLocation(user), -1);
                SetLocalInt(rod, Fishing.RemainingBaitVariable, 40);
                replacement = CreateItemOnObject(BaitResref, user, 60);
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(user, () =>
            {
                ctx.AssertEqual(60, CountBait(user), "Only the partial stack is in the inventory while bait is loaded");
                apply(user, rod, rod, GetLocation(user), -1);
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(user, () =>
            {
                ctx.AssertEqual(100, CountBait(user), "Unloading returns the 40 remaining bait, no more");
                ctx.AssertEqual(0, GetLocalInt(rod, Fishing.RemainingBaitVariable), "The rod is empty after unloading");
            });
        }

        private static int CountBait(uint creature)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
                if (GetResRef(item) == BaitResref)
                    count += GetItemStackSize(item);
            return count;
        }
    }
}
