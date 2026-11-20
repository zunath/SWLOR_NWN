using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NReJSON;
using StackExchange.Redis;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TunicListingMarketCategoryEngineTests
    {
        private const int Tunic = _32_NextReleaseServerMigration.RemovedTunicCategory;

        // Every tunic blueprint on the Armor base item; the two tunic recipes are not armor.
        private static readonly string[] TunicResrefs =
        {
            "agent_tunic", "asc_chn_tunic", "asc_skm_tunic", "biolab_tunic", "cara_tunic", "com_tunic", "fld_chn_tunic",
            "fld_skm_tunic", "flesh_tunic", "for_tunic", "gantry_tunic", "gr_tunic", "hunter_tunic", "imm_tunic", "inq_tunic",
            "kwi_hyper_tunic", "kwi_light_tunic", "leather_tunic003", "leather_tunic004", "leather_tunic005", "leather_tunic014",
            "mag_tunic", "npctunic_brown", "outlaw_tunic", "prm_chn_tunic", "prm_skm_tunic", "reg_tunic", "sc_tunic",
            "seeker_tunic", "sm_tunic", "sup_tunic", "sur_tunic", "tran_tunic", "tunic_byysk", "val_tunic", "vet_chn_tunic",
            "vet_skm_tunic", "viv_tunic",
        };

        [EngineTest("Newly listed tunics use the Armor market category", Category = "Market", TimeoutSeconds = 30f)]
        public static async Task NewTunicListingsUseArmor(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            foreach (var resref in TunicResrefs)
            {
                var item = CreateObject(ObjectType.Item, resref, ctx.GetArenaLocation(1f, 1f));
                ctx.Track(item);
                ctx.Assert(GetIsObjectValid(item), $"{resref} creates an item");
                ctx.AssertEqual(MarketCategoryType.Armor, PlayerMarket.GetItemMarketCategory(item), $"{resref} market category");
            }
        }

        [EngineTest("Tunic listings saved under the removed Tunic category move to Armor", Category = "Migration", TimeoutSeconds = 30f)]
        public static async Task TunicListingsMoveToArmor(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var marketId = "tunic-migration-market-" + Guid.NewGuid().ToString("N");
            var tunic = NewListing(marketId, "Old Tunic", (MarketCategoryType)Tunic);
            var breastplate = NewListing(marketId, "Breastplate", MarketCategoryType.Armor);
            var helmet = NewListing(marketId, "Helmet", MarketCategoryType.Helmet);
            var listings = new[] { tunic, breastplate, helmet };
            try
            {
                foreach (var listing in listings)
                    DB.Set(listing);

                ctx.AssertEqual(Tunic, StoredCategory(tunic.Id), "Fixture keeps the removed Tunic id");
                ctx.Assert(!ArmorFilterIds(marketId).Contains(tunic.Id), "Before the migration the Armor filter misses the tunic");

                new _32_NextReleaseServerMigration().Migrate();
                AssertMigrated(ctx, marketId, tunic, breastplate, helmet);

                // A second run finds nothing and changes nothing.
                new _32_NextReleaseServerMigration().Migrate();
                AssertMigrated(ctx, marketId, tunic, breastplate, helmet);
                ctx.AssertEqual(MarketCategoryType.Armor, DB.Get<MarketItem>(tunic.Id).Category, "Reloaded tunic category");
            }
            finally
            {
                foreach (var listing in listings)
                    DB.Delete<MarketItem>(listing.Id);
            }
        }

        [EngineTest("Tunic listings whose search entry already says Armor still move to Armor", Category = "Migration", TimeoutSeconds = 30f)]
        public static async Task TunicListingWithArmorSearchEntryMovesToArmor(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var marketId = "tunic-migration-market-" + Guid.NewGuid().ToString("N");
            var tunic = NewListing(marketId, "Old Tunic", MarketCategoryType.Armor);
            var breastplate = NewListing(marketId, "Breastplate", MarketCategoryType.Armor);
            var helmet = NewListing(marketId, "Helmet", MarketCategoryType.Helmet);
            var listings = new[] { tunic, breastplate, helmet };
            try
            {
                foreach (var listing in listings)
                    DB.Set(listing);

                // A save whose search entry was written but whose record write was lost: the search entry says Armor
                // while the saved record still holds the Tunic id.
                SetStoredCategoryOnly(tunic.Id, Tunic);
                ctx.AssertEqual(Tunic, StoredCategory(tunic.Id), "Fixture record keeps the removed Tunic id");
                ctx.Assert(ArmorFilterIds(marketId).Contains(tunic.Id), "Fixture search entry already says Armor");

                new _32_NextReleaseServerMigration().Migrate();
                AssertMigrated(ctx, marketId, tunic, breastplate, helmet);

                new _32_NextReleaseServerMigration().Migrate();
                AssertMigrated(ctx, marketId, tunic, breastplate, helmet);
            }
            finally
            {
                foreach (var listing in listings)
                    DB.Delete<MarketItem>(listing.Id);
            }
        }

        [EngineTest("Tunic listings whose saved record already says Armor get their search entry moved to Armor", Category = "Migration", TimeoutSeconds = 30f)]
        public static async Task TunicListingWithTunicSearchEntryMovesToArmor(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var marketId = "tunic-migration-market-" + Guid.NewGuid().ToString("N");
            var tunic = NewListing(marketId, "Old Tunic", MarketCategoryType.Armor);
            var breastplate = NewListing(marketId, "Breastplate", MarketCategoryType.Armor);
            var helmet = NewListing(marketId, "Helmet", MarketCategoryType.Helmet);
            var listings = new[] { tunic, breastplate, helmet };
            try
            {
                foreach (var listing in listings)
                    DB.Set(listing);

                // A save whose record was written but whose search entry write was lost: the record says Armor
                // while the search entry still holds the Tunic id.
                SetSearchEntryCategoryOnly(tunic.Id, Tunic);
                ctx.AssertEqual((int)MarketCategoryType.Armor, StoredCategory(tunic.Id), "Fixture record already says Armor");
                ctx.Assert(!ArmorFilterIds(marketId).Contains(tunic.Id), "Fixture search entry keeps the removed Tunic id");

                new _32_NextReleaseServerMigration().Migrate();
                AssertMigrated(ctx, marketId, tunic, breastplate, helmet);

                new _32_NextReleaseServerMigration().Migrate();
                AssertMigrated(ctx, marketId, tunic, breastplate, helmet);
            }
            finally
            {
                foreach (var listing in listings)
                    DB.Delete<MarketItem>(listing.Id);
            }
        }

        private static void AssertMigrated(EngineTestContext ctx, string marketId, MarketItem tunic, MarketItem breastplate, MarketItem helmet)
        {
            ctx.AssertEqual((int)MarketCategoryType.Armor, StoredCategory(tunic.Id), "Stored tunic category");
            ctx.AssertEqual((int)MarketCategoryType.Armor, StoredCategory(breastplate.Id), "Stored breastplate category");
            ctx.AssertEqual((int)MarketCategoryType.Helmet, StoredCategory(helmet.Id), "Stored helmet category");
            var armor = ArmorFilterIds(marketId);
            ctx.Assert(armor.Contains(tunic.Id) && armor.Contains(breastplate.Id) && !armor.Contains(helmet.Id),
                "Armor filter lists the tunic and breastplate, not the helmet");
            ctx.AssertEqual(0L, DB.SearchCount(new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false)
                .AddFieldSearch(nameof(MarketItem.Category), Tunic)), "No search entry left under the Tunic id");
        }

        private static MarketItem NewListing(string marketId, string name, MarketCategoryType category) => new()
        {
            MarketId = marketId, MarketName = "Test market", PlayerId = "tunic-migration-seller", SellerName = "Test seller",
            SellerCDKey = "test-key", Price = 1000, IsListed = true, Name = name, Tag = "test", Resref = "test",
            Quantity = 1, IconResref = "iit_chest", Category = category, DateListed = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        private static int StoredCategory(string id) => JObject.Parse(DB.GetRawJson<MarketItem>(id))[nameof(MarketItem.Category)]!.Value<int>();

        // Writes the saved record's category without touching its search entry, then drops DB's cached copies.
        private static void SetStoredCategoryOnly(string id, int category)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            var multiplexer = (ConnectionMultiplexer)typeof(DB).GetField("_multiplexer", flags)!.GetValue(null)!;
            var keyPrefix = ((Dictionary<Type, string>)typeof(DB).GetField("_keyPrefixByType", flags)!.GetValue(null)!)[typeof(MarketItem)];
            var key = $"{keyPrefix}:{id}";
            var result = multiplexer.GetDatabase().JsonSet(key, category.ToString(), path: $".{nameof(MarketItem.Category)}");
            if (!result.IsSuccess)
                throw new InvalidOperationException($"Could not set the stored category of {id}.");
            ((Dictionary<string, EntityBase>)typeof(DB).GetField("_cachedEntities", flags)!.GetValue(null)!).Remove(id);
            ((Dictionary<string, string>)typeof(DB).GetField("_jsonAcrossFrames", flags)!.GetValue(null)!).Remove(key);
        }

        // Writes the search entry's category without touching the saved record.
        private static void SetSearchEntryCategoryOnly(string id, int category)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            var multiplexer = (ConnectionMultiplexer)typeof(DB).GetField("_multiplexer", flags)!.GetValue(null)!;
            var keyPrefix = ((Dictionary<Type, string>)typeof(DB).GetField("_keyPrefixByType", flags)!.GetValue(null)!)[typeof(MarketItem)];
            multiplexer.GetDatabase().HashSet(SearchIndexDefinition.KeyPrefix(keyPrefix) + id, nameof(MarketItem.Category), category);
        }

        // The query the market's buy window runs with only the Armor toggle on.
        private static string[] ArmorFilterIds(string marketId) => DB.SearchIds(new DBQuery<MarketItem>()
            .AddFieldSearch(nameof(MarketItem.IsListed), true)
            .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false)
            .AddFieldSearch(nameof(MarketItem.Category), new[] { (int)MarketCategoryType.Armor })
            .AddPaging(50, 0)).ToArray();
    }
}
