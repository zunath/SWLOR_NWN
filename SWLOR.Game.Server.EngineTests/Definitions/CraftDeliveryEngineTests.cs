using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A finished minigame craft puts its output in the inventory when there is room. The output is made in the temporary item
    /// storage, and the engine will not move an item out of that container into a creature, so every craft used to be held at the
    /// station with an "inventory is full" message even when the inventory was empty.
    /// </summary>
    public static class CraftDeliveryEngineTests
    {
        private static readonly (RecipeType Recipe, SkillType Skill)[] Crafts =
        {
            (RecipeType.TwinElectroblade1, SkillType.Smithery),
            (RecipeType.DroidArmsMPV1, SkillType.Engineering),
            (RecipeType.BlubberyFishSubstitute1, SkillType.Agriculture)
        };

        [EngineTest("A finished craft goes into an inventory with room and nothing is held", Category = "CraftDelivery")]
        public static async Task FinishedCraftReachesInventory(EngineTestContext ctx)
        {
            foreach (var (recipe, skill) in Crafts)
            {
                using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
                var player = fixture.Creature;
                var detail = Craft.GetRecipe(recipe);
                var session = await StartCraft(ctx, fixture, recipe, skill);

                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    ctx.Assert(CraftSessions.Succeed(player, session, new MinigameMeter(100, 100), 10) != null, $"{recipe} succeeds");
                    var record = DB.Get<Player>(fixture.Id);
                    ctx.AssertEqual(0, record.HeldCraftItems?.Count ?? 0, $"{recipe}: nothing is held at the station");
                    ctx.AssertEqual(System.Math.Max(1, detail.Quantity), CarriedStackTotal(player, detail.Resref),
                        $"{recipe}: the whole output is in the inventory");

                    var made = Snapshot(player).Keys.First(item => GetResRef(item) == detail.Resref);
                    ctx.Assert(GetLocalBool(made, ItemService.PlayerProducedItemVariable), $"{recipe}: the delivered item is marked as player made");
                    ctx.Assert(record.PendingCraftReturns.SelectMany(entry => entry.Items).Any(),
                        $"{recipe}: the delivered output is listed as a pending return until the character file holds it");
                });
            }
        }

        [EngineTest("A finished craft is held when the inventory is full and returns once there is room", Category = "CraftDelivery")]
        public static async Task FinishedCraftHeldWhenFull(EngineTestContext ctx)
        {
            var (recipe, skill) = Crafts[0];
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var detail = Craft.GetRecipe(recipe);
            var session = await StartCraft(ctx, fixture, recipe, skill);

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var fillers = Fill(ctx, player);
                var carried = Snapshot(player);

                ctx.Assert(CraftSessions.Succeed(player, session, new MinigameMeter(100, 100), 10) != null, "The craft succeeds");
                AssertCarriedIntact(ctx, player, carried);
                ctx.AssertEqual(0, CountCarried(player, detail.Resref), "The output is not in the full inventory");
                ctx.AssertEqual(1, DB.Get<Player>(fixture.Id).HeldCraftItems.Count, "The output is held at the station");

                Free(fillers);
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                CraftSessions.SettleLeftover(player);
                ctx.AssertEqual(0, DB.Get<Player>(fixture.Id).HeldCraftItems.Count, "Opening a station with room returns the held output");
                ctx.AssertEqual(1, CountCarried(player, detail.Resref), "The held output reaches the inventory");
            });
        }

        private static async Task<CraftSession> StartCraft(EngineTestContext ctx, PlayerAbilityFixture fixture, RecipeType recipe, SkillType skill)
        {
            var player = fixture.Creature;
            fixture.Update(record => record.Skills[skill].Rank = 10);
            CraftSession session = null;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                foreach (var (resref, quantity) in Craft.GetRecipe(recipe).Components)
                    CreateItemOnObject(resref, player, quantity);
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var start = CraftSessions.Start(player, new CraftSessionStartRequest(recipe, OBJECT_INVALID, new uint[0]));
                ctx.Assert(start.Succeeded, $"{recipe} starts: {start.Message}");
                session = start.Session;
            });

            // The components leave the inventory when the starting script ends.
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.AssertEqual(0, Snapshot(player).Count, $"{recipe}: the inventory is empty once the components are committed"));
            return session;
        }
    }
}
