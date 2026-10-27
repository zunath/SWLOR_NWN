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
    /// The bank window lists every stored item. An unpaged search stops at 50 rows, so a bank holding more than
    /// 50 items hid its newest deposits and players read them as deleted.
    /// </summary>
    public static class BankListingEngineTests
    {
        private const int StoredItemCount = 60;
        private const string WoodResref = "wood";

        [EngineTest("The bank lists a new Wood deposit when it already holds more than 50 items", Category = "BankListing")]
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

                    var wood = Bank.DepositItem(player, CreateStorageItem(ctx, WoodResref, 8));
                    recordIds.Add(wood.Id);

                    ctx.AssertEqual((long)StoredItemCount + 1, Bank.GetItemCount(playerId), "Stored bank records");

                    var listed = Bank.SearchItems(playerId, string.Empty);
                    ctx.AssertEqual(StoredItemCount + 1, listed.Count, "Items the bank window lists");
                    ctx.Assert(listed.Any(item => item.Id == wood.Id), "The bank window lists the newest Wood deposit");
                    ctx.AssertEqual(recordIds.Count, listed.Select(item => item.Id).Distinct().Count(), "Each listed item is a distinct record");

                    var woodOnly = Bank.SearchItems(playerId, "Wood");
                    ctx.AssertEqual(1, woodOnly.Count, "A Wood search lists only the Wood deposit");
                    ctx.AssertEqual(8, woodOnly[0].Quantity, "The Wood deposit keeps its stack size");
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
