using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Headgear migration repairs every persistent storage surface beyond the default search page", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static async Task StoredHeadgearSurfaces(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var item = await CreateItemAsync(ctx, owner, "ruptorvanecr", owner);
        string saved = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyVisor(item);
            saved = ObjectPlugin.Serialize(item);
            DestroyObject(item);
        });
        await ctx.WaitUntilAsync(() => !GetIsObjectValid(item), 5f, "the archived fixture to release its native identity");
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var checks = new List<Action>();
            var cleanups = new List<Action>();
            void Add<T>(T record, Func<T, IEnumerable<string>> payloads) where T : EntityBase
            {
                var before = StorageMetadata(JObject.FromObject(record));
                DB.Set(record);
                cleanups.Add(() => DB.Delete<T>(record.Id));
                checks.Add(() =>
                {
                    var current = DB.Get<T>(record.Id);
                    ctx.Assert(JToken.DeepEquals(before, StorageMetadata(JObject.FromObject(current))), $"{typeof(T).Name} ownership, quantities and metadata survive");
                    foreach (var data in payloads(current))
                    {
                        ctx.Assert(!HeadgearSlotMigration.MigrateSerializedObject(data, out _), $"{typeof(T).Name} payload is stable on retry");
                        var restored = Deserialize(ctx, data);
                        try
                        {
                            ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(restored), $"{typeof(T).Name} legacy armor becomes headgear");
                            ctx.AssertEqual("fixture-crafter", GetLocalString(restored, "CRAFTED_BY"), "Stored crafting identity survives");
                            ctx.AssertEqual(34, SumProperty(restored, ItemPropertyType.Attack, 0), "Slot conversion itself does not rebalance Attack");
                        }
                        finally { Invoke("MigrationObject", "DestroyTemporaryObject", restored); }
                    }
                });
            }
            try
            {
                for (var index = 0; index < 55; index++)
                    Add(new InventoryItem { Data = saved, PlayerId = "stored-owner", StorageId = "GLOBAL_BANK", Quantity = 1, Name = "Custom bank label" }, x => new[] { x.Data });
                Add(new MarketItem { Data = saved, PlayerId = "stored-owner", Price = 123, Quantity = 1, Name = "Custom market label" }, x => new[] { x.Data });
                Add(new WorldProperty { SerializedItem = saved, CustomName = "Custom property" }, x => new[] { x.SerializedItem });
                Add(new ResearchJob { SerializedItem = saved, PlayerId = "stored-owner", Level = 50 }, x => new[] { x.SerializedItem });
                Add(new PlayerOutfit { Data = saved, PlayerId = "stored-owner", Name = "Custom outfit" }, x => new[] { x.Data });
                Add(new WorldPropertyCategory { Items = { ["saved-copy"] = new WorldPropertyItem { Data = saved, Name = "Custom storage label", Quantity = 1 } } }, x => x.Items.Values.Select(y => y.Data));
                var ship = new PlayerShip { OwnerPlayerId = "stored-owner", SerializedItem = saved, Status = new ShipStatus() };
                foreach (var modules in new[] { ship.Status.HighPowerModules, ship.Status.LowPowerModules, ship.Status.ConfigurationModules })
                    modules[0] = new ShipStatus.ShipStatusModule { SerializedItem = saved, ItemInstanceId = "module-id", ModuleBonus = 12 };
                Add(ship, x => new[] { x.SerializedItem }.Concat(x.Status.HighPowerModules.Values.Concat(x.Status.LowPowerModules.Values)
                    .Concat(x.Status.ConfigurationModules.Values).Select(y => y.SerializedItem)));
                Add(new QuestContract { AuthorPlayerId = "stored-owner", RewardItems = { new QuestContractItem { Data = saved, StackSize = 1, Name = "Custom reward" } } }, x => x.RewardItems.Select(y => y.Data));
                Add(new QuestContractDelivery { PlayerId = "stored-owner", Items = { new QuestContractItem { Data = saved, StackSize = 1, Name = "Custom delivery" } } }, x => x.Items.Select(y => y.Data));
                for (var run = 0; run < 2; run++)
                {
                    new _31_RepairHeadgearSlots().Migrate();
                    foreach (var check in checks) check();
                }
            }
            finally { foreach (var cleanup in cleanups) cleanup(); }
        });
    }

    private static JObject StorageMetadata(JObject record)
    {
        foreach (var field in record.Descendants().OfType<JProperty>().Where(x => x.Name is "Data" or "SerializedItem").ToArray())
            field.Value = "payload";
        return record;
    }
}
