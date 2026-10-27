using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The bank window pages through every stored item. An unpaged search stopped at 50 rows, so a bank holding more than
    /// 50 items hid its newest deposits and players read them as deleted.
    /// </summary>
    public static class BankListingEngineTests
    {
        private const int StoredItemCount = 60;
        private const int LegacyItemCount = 30;
        private const string WoodResref = "wood";

        [EngineTest("The bank pages past 50 items and shows a new Wood deposit on the last page and in search", Category = "BankListing", TimeoutSeconds = 120f)]
        public static async Task BankListsEveryItemPastFiftyRows(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var playerId = GetObjectUUID(player);
            var recordIds = new List<string>();
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    for (var index = 0; index < StoredItemCount; index++)
                    {
                        recordIds.Add(Bank.DepositItem(player, CreateStorageItem(ctx, StackResref)).Id);
                    }

                    // Items deposited before deposit times were recorded index a time of 0.
                    foreach (var id in recordIds.Take(LegacyItemCount))
                    {
                        var legacy = DB.Get<InventoryItem>(id);
                        legacy.DepositedTicks = 0;
                        DB.Set(legacy);
                    }

                    var wood = Bank.DepositItem(player, CreateStorageItem(ctx, WoodResref, 8));
                    recordIds.Add(wood.Id);

                    ctx.AssertEqual((long)StoredItemCount + 1, Bank.GetItemCount(playerId), "Stored bank records");

                    var seen = new HashSet<string>();
                    var firstPage = Bank.SearchItems(playerId, string.Empty, 0);
                    ctx.AssertEqual(4, firstPage.PageNumbers.Count, "Pages for 61 items at 20 per page");
                    for (var pageIndex = 0; pageIndex < firstPage.PageNumbers.Count; pageIndex++)
                    {
                        var page = Bank.SearchItems(playerId, string.Empty, pageIndex);
                        ctx.AssertEqual(pageIndex, page.PageIndex, $"Page {pageIndex + 1} index");
                        ctx.AssertEqual(StoredItemCount + 1L, page.MatchCount, $"Page {pageIndex + 1} match count");
                        foreach (var item in page.Items)
                        {
                            ctx.Assert(seen.Add(item.Id), $"Item '{item.Id}' appears on only one page");
                        }
                    }

                    ctx.AssertEqual(StoredItemCount + 1, seen.Count, "Items reachable across all pages");
                    ctx.Assert(firstPage.Items.TrueForAll(item => item.DepositedTicks == 0), "Page 1 holds the items deposited before deposit times were recorded");

                    var lastPage = Bank.SearchItems(playerId, string.Empty, int.MaxValue);
                    ctx.AssertEqual(3, lastPage.PageIndex, "A page past the end clamps to the last page");
                    ctx.AssertEqual(1, lastPage.Items.Count, "The last page holds the one item past 60");
                    ctx.AssertEqual(wood.Id, lastPage.Items[0].Id, "The newest Wood deposit is on the last page");

                    var woodOnly = Bank.SearchItems(playerId, "Wood", 0);
                    ctx.AssertEqual(1L, woodOnly.MatchCount, "A Wood search matches only the Wood deposit");
                    ctx.AssertEqual(1, woodOnly.PageNumbers.Count, "A Wood search fits on one page");
                    ctx.AssertEqual(wood.Id, woodOnly.Items[0].Id, "A Wood search lists the Wood deposit");
                    ctx.AssertEqual(8, woodOnly.Items[0].Quantity, "The Wood deposit keeps its stack size");

                    ctx.AssertEqual(string.Empty, Bank.WithdrawItem(player, wood.Id), "The Wood withdraws");
                    var afterWithdraw = Bank.SearchItems(playerId, string.Empty, 3);
                    ctx.AssertEqual(2, afterWithdraw.PageIndex, "Emptying the last page moves back a page");
                    ctx.AssertEqual(20, afterWithdraw.Items.Count, "The previous page is full");
                });
            }
            finally
            {
                foreach (var id in recordIds)
                {
                    DB.Delete<InventoryItem>(id);
                }
            }
        }
    }
}
