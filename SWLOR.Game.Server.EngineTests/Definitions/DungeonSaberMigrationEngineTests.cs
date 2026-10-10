using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Dungeon saber exchange covers equipment, bags, archives and droids once", Category = "DungeonSaberMigration")]
    public static async Task DungeonSaberInventoryExchange(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        await ctx.EquipItemAsync(owner, "sabstorm_l1", InventorySlot.RightHand);
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        await CreateItemAsync(ctx, owner, "sabcycl_l2", bag);
        var normal = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        var root = await CreateItemAsync(ctx, owner, "eclipse_w1", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            SetLocalString(root, "DROID_ITEM_ID", "dungeon-saber-fixture");
            var saved = ObjectPlugin.Serialize(root);
            ctx.Assert(DungeonSaberMigration.MigrateSerializedObject(saved, out var migrated), "Saved root is converted");
            ctx.AssertEqual("eclipse_w3", GetResRef(Deserialize(ctx, migrated)), "Root becomes same-tier nonweapon gear");
            ctx.Assert(!DungeonSaberMigration.MigrateSerializedObject(migrated, out var retry), "Converted root is idempotent");
            ctx.AssertEqual(migrated, retry, "Unchanged archive stays byte-for-byte intact");
            var archive = ObjectPlugin.Serialize(owner);
            ctx.Assert(DungeonSaberMigration.MigrateSerializedObject(archive, out var creature), "Saved equipment and bags are repaired");
            var restored = Deserialize(ctx, creature);
            ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, restored)), "Replacement cannot stay in weapon slot");
            ctx.AssertEqual("sabcycl_l4", GetResRef(GetFirstItemInInventory(
                GetItemPossessedBy(restored, GetTag(bag)))), "Nested bag item is converted");

            SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
            {
                EquippedItems = { [InventorySlot.RightHand] = saved }
            }));
            new _18_ReplaceDungeonSabers().Migrate(owner);
            ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, owner)), "Live weapon is unequipped");
            ctx.AssertEqual("sabcycl_l4", GetResRef(GetFirstItemInInventory(bag)), "Live nested weapon is exchanged");
            ctx.AssertEqual("b_longsword", GetResRef(normal), "Unrelated weapon remains untouched");
            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
            ctx.AssertEqual(0, droid.EquippedItems.Count, "Droid weapon slot is cleared");
            ctx.AssertEqual("eclipse_w3", GetResRef(Deserialize(ctx, droid.Inventory["dungeon-saber-fixture"])),
                "Droid keeps the replacement under its original inventory ID");
            ctx.Assert(!DungeonSaberMigration.MigrateObject(owner), "Live retry cannot grant another replacement");
        });
    }

    [EngineTest("Dungeon saber repair scans beyond 50 bank rows and updates escrow metadata", Category = "DungeonSaberMigration", TimeoutSeconds = 120)]
    public static async Task DungeonSaberStoredExchange(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var item = await CreateItemAsync(ctx, owner, "guardmst_l1", owner);
        var ids = Enumerable.Range(0, 65).Select(_ => Guid.NewGuid().ToString()).ToArray();
        var marketId = Guid.NewGuid().ToString();
        var propertyId = Guid.NewGuid().ToString();
        var contractId = Guid.NewGuid().ToString();
        var deliveryId = Guid.NewGuid().ToString();
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var saved = ObjectPlugin.Serialize(item);
            try
            {
                foreach (var id in ids)
                    DB.Set(new InventoryItem { Id = id, Resref = "guardmst_l1", Name = "Old saber", Data = saved, Quantity = 1 });
                DB.Set(new MarketItem { Id = marketId, Resref = "guardmst_l1", Name = "Old saber", Data = saved,
                    Quantity = 1, Price = 321, IsListed = true, DateListed = DateTime.UtcNow });
                DB.Set(new WorldPropertyCategory { Id = propertyId, Items = new Dictionary<string, WorldPropertyItem>
                {
                    ["stored"] = new() { Resref = "guardmst_l1", Name = "Old saber", Data = saved, Quantity = 1 }
                }});
                DB.Set(new QuestContract { Id = contractId, RewardItems = new List<QuestContractItem>
                {
                    new() { Resref = "guardmst_l1", Name = "Old saber", Data = saved, StackSize = 1 }
                }});
                DB.Set(new QuestContractDelivery { Id = deliveryId, Items = new List<QuestContractItem>
                {
                    new() { Resref = "guardmst_l1", Name = "Old saber", Data = saved, StackSize = 1 }
                }});
                new _25_ReplaceDungeonSabers().Migrate();
                foreach (var id in ids)
                {
                    var bank = DB.Get<InventoryItem>(id);
                    ctx.AssertEqual("guardmst_l3", bank.Resref, "Every bank row is migrated, including rows beyond 50");
                    ctx.AssertEqual("guardmst_l3", GetResRef(Deserialize(ctx, bank.Data)), "Bank metadata matches payload");
                }
                var market = DB.Get<MarketItem>(marketId);
                ctx.AssertEqual("guardmst_l3", market.Resref, "Market metadata changes with its payload");
                ctx.Assert(!market.IsListed && market.DateListed == null, "Converted listing awaits seller repricing");
                ctx.AssertEqual(321, market.Price, "Seller's recorded price is not silently changed");
                ctx.AssertEqual("guardmst_l3", DB.Get<WorldPropertyCategory>(propertyId).Items["stored"].Resref, "Property storage is migrated");
                ctx.AssertEqual("guardmst_l3", DB.Get<QuestContract>(contractId).RewardItems[0].Resref, "Reward escrow is migrated");
                ctx.AssertEqual("guardmst_l3", DB.Get<QuestContractDelivery>(deliveryId).Items[0].Resref, "Pending delivery is migrated");
                var first = DB.Get<InventoryItem>(ids[0]).Data;
                new _25_ReplaceDungeonSabers().Migrate();
                ctx.AssertEqual(first, DB.Get<InventoryItem>(ids[0]).Data, "Server retry is idempotent");
            }
            finally
            {
                foreach (var id in ids) DB.Delete<InventoryItem>(id);
                DB.Delete<MarketItem>(marketId);
                DB.Delete<WorldPropertyCategory>(propertyId);
                DB.Delete<QuestContract>(contractId);
                DB.Delete<QuestContractDelivery>(deliveryId);
            }
        });
    }
}
