using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TunicListingMigrationEngineTests
    {
        [EngineTest("Tunic listings saved under the removed Tunic category move to Armor", Category = "Migration", TimeoutSeconds = 30f)]
        public static async Task TunicListingsMoveToArmor(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var marketId = "tunic-migration-market-" + Guid.NewGuid().ToString("N");
            var tunic = NewListing(marketId, "Old Tunic", (MarketCategoryType)_32_MoveTunicListingsToArmor.RemovedTunicCategory);
            var breastplate = NewListing(marketId, "Breastplate", MarketCategoryType.Armor);
            var helmet = NewListing(marketId, "Helmet", MarketCategoryType.Helmet);
            var listings = new[] { tunic, breastplate, helmet };
            try
            {
                foreach (var listing in listings)
                    DB.Set(listing);

                ctx.AssertEqual(_32_MoveTunicListingsToArmor.RemovedTunicCategory, StoredCategory(tunic.Id), "Fixture keeps the removed Tunic id");
                ctx.Assert(!ArmorFilterIds(marketId).Contains(tunic.Id), "Before the migration the Armor filter misses the tunic");

                new _32_MoveTunicListingsToArmor().Migrate();
                AssertMigrated();

                // A second run finds nothing and changes nothing.
                new _32_MoveTunicListingsToArmor().Migrate();
                AssertMigrated();
                ctx.AssertEqual(MarketCategoryType.Armor, DB.Get<MarketItem>(tunic.Id).Category, "Reloaded tunic category");
            }
            finally
            {
                foreach (var listing in listings)
                    DB.Delete<MarketItem>(listing.Id);
            }

            void AssertMigrated()
            {
                ctx.AssertEqual((int)MarketCategoryType.Armor, StoredCategory(tunic.Id), "Stored tunic category");
                ctx.AssertEqual((int)MarketCategoryType.Armor, StoredCategory(breastplate.Id), "Stored breastplate category");
                ctx.AssertEqual((int)MarketCategoryType.Helmet, StoredCategory(helmet.Id), "Stored helmet category");
                var armor = ArmorFilterIds(marketId);
                ctx.Assert(armor.Contains(tunic.Id) && armor.Contains(breastplate.Id) && !armor.Contains(helmet.Id),
                    "Armor filter lists the tunic and breastplate, not the helmet");
                ctx.AssertEqual(0L, DB.SearchCount(_32_MoveTunicListingsToArmor.BuildTunicListingQuery()
                    .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false)), "No listing left under the Tunic id");
            }
        }

        private static MarketItem NewListing(string marketId, string name, MarketCategoryType category) => new()
        {
            MarketId = marketId, MarketName = "Test market", PlayerId = "tunic-migration-seller", SellerName = "Test seller",
            SellerCDKey = "test-key", Price = 1000, IsListed = true, Name = name, Tag = "test", Resref = "test",
            Quantity = 1, IconResref = "iit_chest", Category = category, DateListed = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        private static int StoredCategory(string id) => JObject.Parse(DB.GetRawJson<MarketItem>(id))[nameof(MarketItem.Category)]!.Value<int>();

        // The query the market's buy window runs with only the Armor toggle on.
        private static string[] ArmorFilterIds(string marketId) => DB.SearchIds(new DBQuery<MarketItem>()
            .AddFieldSearch(nameof(MarketItem.IsListed), true)
            .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false)
            .AddFieldSearch(nameof(MarketItem.Category), new[] { (int)MarketCategoryType.Armor })
            .AddPaging(50, 0)).ToArray();
    }
}
