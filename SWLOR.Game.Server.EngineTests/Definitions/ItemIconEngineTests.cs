using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Non-ship artwork refresh preserves crafted enhancement state", Category = "ItemIcons")]
    public static async Task EnhancementArtworkPreservesCraftedState(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var item = await CreateItemAsync(ctx, owner, "aen_hp1", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("aen_hp1", (int)GetBaseItemType(item), 88), GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Newly created blueprints use the reviewed appearance");
            SetName(item, "Custom vitality enhancement");
            SetLocalInt(item, "CRAFTED_QUALITY", 29);
            SetItemCharges(item, 6);
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 88);
            var properties = DescribeProperties(item);
            var identity = GetObjectUUID(item);
            new _16_UpdateItemIcons().Migrate(owner);
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("aen_hp1", (int)GetBaseItemType(item), 88), GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Uses its reviewed enhancement artwork");
            ctx.AssertEqual(identity, GetObjectUUID(item), "The original native object is retained");
            ctx.AssertEqual("Custom vitality enhancement", GetName(item), "Custom name is retained");
            ctx.AssertEqual(29, GetLocalInt(item, "CRAFTED_QUALITY"), "Crafting metadata is retained");
            ctx.AssertEqual(6, GetItemCharges(item), "Charges are retained");
            ctx.AssertEqual(properties, DescribeProperties(item), "Enhancement properties are retained");
            ctx.Assert(!ItemIconMigration.MigrateObject(owner), "Updates are idempotent");
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 254);
            ctx.Assert(!ItemIconMigration.MigrateObject(item), "A customized model is preserved");
            ctx.AssertEqual(254, GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Customized appearance remains unchanged");
        });
    }

    [EngineTest("Saved nested crafting materials retain their properties after artwork refresh", Category = "ItemIcons")]
    public static async Task SavedCraftingMaterialArtwork(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var material = await CreateItemAsync(ctx, owner, "imp_cotton_1", bag);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("imp_cotton_1", (int)GetBaseItemType(material), 91), GetItemAppearance(material, ItemAppearanceType.SimpleModel, 0), "New crafting materials use the reviewed appearance");
            ItemPlugin.SetItemAppearance(material, ItemAppearanceType.SimpleModel, 0, 91);
            SetLocalString(material, "CRAFTED_SOURCE", "retained");
            var properties = DescribeProperties(material);
            var serialized = ObjectPlugin.Serialize(bag);
            ctx.Assert(ItemIconMigration.MigrateSerializedObject(serialized, out var updated), "Nested crafting material artwork is saved");
            var restored = Deserialize(ctx, updated);
            var restoredMaterial = GetFirstItemInInventory(restored);
            ctx.AssertEqual("imp_cotton_1", GetResRef(restoredMaterial), "Material identity is retained");
            ctx.AssertEqual(properties, DescribeProperties(restoredMaterial), "Material crafting properties are retained");
            ctx.AssertEqual("retained", GetLocalString(restoredMaterial, "CRAFTED_SOURCE"), "Custom metadata survives serialization");
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("imp_cotton_1", (int)GetBaseItemType(restoredMaterial), 91), GetItemAppearance(restoredMaterial, ItemAppearanceType.SimpleModel, 0), "Saved material artwork is updated");
            ctx.Assert(!ItemIconMigration.MigrateSerializedObject(updated, out var unchanged), "Saved material data is idempotent");
            ctx.AssertEqual(updated, unchanged, "Unchanged serialized data is returned verbatim");
        });
    }
    [EngineTest("New server icon migration preserves every stored-item surface and metadata", Category = "ItemIcons")]
    public static async Task NewServerIconMigrationPreservesStorage(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var item = await CreateItemAsync(ctx, owner, "aen_hp1", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 88);
            SetName(item, "Retained crafted enhancement");
            SetLocalInt(item, "CRAFTED_QUALITY", 29);
            var data = ObjectPlugin.Serialize(item);
            var creatureData = ObjectPlugin.Serialize(owner);
            var inventory = new InventoryItem { Data = data, Resref = "aen_hp1", IconResref = "iit_ess2_088", Quantity = 7, Name = "Retained name" };
            var market = new MarketItem { Data = data, Resref = "aen_hp1", IconResref = "custom_icon", Quantity = 3, Price = 53, IsListed = true };
            var category = new WorldPropertyCategory();
            category.Items["retained-key"] = new WorldPropertyItem { Data = data, Resref = "aen_hp1", IconResref = "iit_ess2_088", Quantity = 11 };
            var property = new WorldProperty { SerializedItem = data };
            var research = new ResearchJob { SerializedItem = data, Level = 27 };
            var outfit = new PlayerOutfit { Data = data, Name = "Retained outfit" };
            var creature = new DMCreature("Retained creature", "retained_tag", creatureData);
            var ship = new PlayerShip { SerializedItem = data, Status = new ShipStatus() };
            foreach (var modules in new[] { ship.Status.HighPowerModules, ship.Status.LowPowerModules, ship.Status.ConfigurationModules })
                modules[2] = new ShipStatus.ShipStatusModule { SerializedItem = data, ItemInstanceId = "retained-id", ModuleBonus = 13, RecastTime = new DateTime(2030, 1, 1) };
            void AssertPayload(string updated, bool isCreature = false)
            {
                var restored = Deserialize(ctx, updated);
                var restoredItem = isCreature ? GetFirstItemInInventory(restored) : restored;
                ctx.AssertEqual(19, GetItemAppearance(restoredItem, ItemAppearanceType.SimpleModel, 0), "Stored native artwork is updated");
                ctx.AssertEqual("Retained crafted enhancement", GetName(restoredItem), "Crafted names survive");
                ctx.AssertEqual(29, GetLocalInt(restoredItem, "CRAFTED_QUALITY"), "Crafted metadata survives");
                ctx.Assert(!ItemIconMigration.MigrateSerializedObject(updated, out var unchanged), "Converted payload is idempotent");
                ctx.AssertEqual(updated, unchanged, "A repeat retains exact serialized data");
            }
            try
            {
                DB.Set(inventory); DB.Set(market); DB.Set(category); DB.Set(property);
                DB.Set(research); DB.Set(outfit); DB.Set(creature); DB.Set(ship);
                new _23_UpdateItemIcons().Migrate();
                var migratedInventory = DB.Get<InventoryItem>(inventory.Id);
                AssertPayload(migratedInventory.Data);
                ctx.AssertEqual("iit_ess2_019", migratedInventory.IconResref, "Stored UI metadata follows the new artwork");
                ctx.AssertEqual(7, migratedInventory.Quantity, "Bank stack size is preserved");
                ctx.AssertEqual("Retained name", migratedInventory.Name, "Bank display name is preserved");
                var migratedMarket = DB.Get<MarketItem>(market.Id);
                AssertPayload(migratedMarket.Data);
                ctx.AssertEqual("custom_icon", migratedMarket.IconResref, "Customized listing artwork is preserved");
                ctx.AssertEqual(53, migratedMarket.Price, "Market price is preserved");
                ctx.AssertEqual(3, migratedMarket.Quantity, "Listing quantity is preserved");
                ctx.Assert(migratedMarket.IsListed, "Listing state is preserved");
                var migratedCategory = DB.Get<WorldPropertyCategory>(category.Id).Items["retained-key"];
                AssertPayload(migratedCategory.Data);
                ctx.AssertEqual("iit_ess2_019", migratedCategory.IconResref, "Property storage UI artwork is updated");
                ctx.AssertEqual(11, migratedCategory.Quantity, "Property stack size is preserved");
                AssertPayload(DB.Get<WorldProperty>(property.Id).SerializedItem);
                AssertPayload(DB.Get<ResearchJob>(research.Id).SerializedItem);
                ctx.AssertEqual(27, DB.Get<ResearchJob>(research.Id).Level, "Research progress is preserved");
                AssertPayload(DB.Get<PlayerOutfit>(outfit.Id).Data);
                AssertPayload(DB.Get<DMCreature>(creature.Id).Data, true);
                var migratedShip = DB.Get<PlayerShip>(ship.Id);
                AssertPayload(migratedShip.SerializedItem);
                foreach (var modules in new[] { migratedShip.Status.HighPowerModules, migratedShip.Status.LowPowerModules, migratedShip.Status.ConfigurationModules })
                {
                    AssertPayload(modules[2].SerializedItem);
                    ctx.AssertEqual("retained-id", modules[2].ItemInstanceId, "Installed module identity is preserved");
                    ctx.AssertEqual(13, modules[2].ModuleBonus, "Installed module bonus is preserved");
                    ctx.AssertEqual(new DateTime(2030, 1, 1), modules[2].RecastTime, "Installed module recast is preserved");
                }
                new _23_UpdateItemIcons().Migrate();
                ctx.AssertEqual(migratedInventory.Data, DB.Get<InventoryItem>(inventory.Id).Data, "A repeat server migration leaves exact payloads intact");
            }
            finally
            {
                DB.Delete<InventoryItem>(inventory.Id); DB.Delete<MarketItem>(market.Id);
                DB.Delete<WorldPropertyCategory>(category.Id); DB.Delete<WorldProperty>(property.Id);
                DB.Delete<ResearchJob>(research.Id); DB.Delete<PlayerOutfit>(outfit.Id);
                DB.Delete<DMCreature>(creature.Id); DB.Delete<PlayerShip>(ship.Id);
            }
        });
    }

}
