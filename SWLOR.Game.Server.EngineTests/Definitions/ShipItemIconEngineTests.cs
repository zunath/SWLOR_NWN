using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Ship artwork updates preserve crafted module identity and properties", Category = "ShipItemIcons")]
    public static async Task ShipModuleArtworkPreservesCraftedState(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var item = await CreateItemAsync(ctx, owner, "com_laser_1", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("com_laser_1", (int)GetBaseItemType(item), 4), GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Newly created modules use the reviewed appearance");
            SetName(item, "Custom crafted laser");
            SetLocalInt(item, "CRAFTED_QUALITY", 27);
            SetItemCharges(item, 7);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, 9), item);
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 4);
            var properties = DescribeProperties(item);
            var identity = GetObjectUUID(item);
            ctx.Assert(ItemIconMigration.MigrateObject(owner), "Legacy artwork is normalized");
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("com_laser_1", (int)GetBaseItemType(item), 4), GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Uses the reviewed original icon");
            ctx.AssertEqual(identity, GetObjectUUID(item), "Native item identity is retained");
            ctx.AssertEqual("com_laser_1", GetResRef(item), "Blueprint identity is retained");
            ctx.AssertEqual("Custom crafted laser", GetName(item), "Custom name is retained");
            ctx.AssertEqual(27, GetLocalInt(item, "CRAFTED_QUALITY"), "Crafting metadata is retained");
            ctx.AssertEqual(7, GetItemCharges(item), "Charges are retained");
            ctx.AssertEqual(properties, DescribeProperties(item), "Crafted module bonuses are retained");
            ctx.Assert(!ItemIconMigration.MigrateObject(owner), "A second pass is idempotent");
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 1);
            ctx.Assert(!ItemIconMigration.MigrateObject(item), "A customized appearance is retained");
            ctx.AssertEqual(1, GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Customized model remains intact");
        });
    }

    [EngineTest("Saved nested ship ammunition retains stack state after artwork refresh", Category = "ShipItemIcons")]
    public static async Task SavedShipAmmunitionArtwork(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var ammo = await CreateItemAsync(ctx, owner, "ship_missile", bag);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            SetItemStackSize(ammo, 7);
            SetLocalString(ammo, "CUSTOM_OWNER", "retained");
            ItemPlugin.SetItemAppearance(ammo, ItemAppearanceType.SimpleModel, 0, 131);
            var serialized = ObjectPlugin.Serialize(bag);
            ctx.Assert(ItemIconMigration.MigrateSerializedObject(serialized, out var updated), "Nested ammunition artwork is saved");
            var restored = Deserialize(ctx, updated);
            var restoredAmmo = GetFirstItemInInventory(restored);
            ctx.AssertEqual("ship_missile", GetResRef(restoredAmmo), "Ammunition remains the same item");
            ctx.AssertEqual(7, GetItemStackSize(restoredAmmo), "Ammunition stack is retained");
            ctx.AssertEqual("retained", GetLocalString(restoredAmmo, "CUSTOM_OWNER"), "Metadata survives serialization");
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("ship_missile", (int)GetBaseItemType(restoredAmmo), 131), GetItemAppearance(restoredAmmo, ItemAppearanceType.SimpleModel, 0), "Saved artwork is updated");
            ctx.Assert(!ItemIconMigration.MigrateSerializedObject(updated, out var unchanged), "Saved artwork does not migrate twice");
            ctx.AssertEqual(updated, unchanged, "Unchanged saved data is returned verbatim");
        });
    }

    [EngineTest("Droid storage refreshes ship artwork without changing controller state", Category = "ShipItemIcons")]
    public static async Task DroidStoredShipArtwork(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        var item = await CreateItemAsync(ctx, owner, "hull_rep_1", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 39);
            SetLocalInt(item, "CRAFTED_QUALITY", 41);
            var droid = new ConstructedDroid { Name = "Stored companion", PortraitId = 12 };
            droid.Inventory["ship-part"] = ObjectPlugin.Serialize(item);
            SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(droid));
            ctx.Assert(ItemIconMigration.MigrateObject(controller), "Stored module artwork is refreshed");
            var updated = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
            ctx.AssertEqual(droid.Name, updated.Name, "Droid name is retained");
            ctx.AssertEqual(droid.PortraitId, updated.PortraitId, "Droid appearance is retained");
            var restored = Deserialize(ctx, updated.Inventory["ship-part"]);
            ctx.AssertEqual(41, GetLocalInt(restored, "CRAFTED_QUALITY"), "Stored crafted item state is retained");
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("hull_rep_1", (int)GetBaseItemType(restored), 39), GetItemAppearance(restored, ItemAppearanceType.SimpleModel, 0), "Stored repair module uses its own icon");
            ctx.Assert(!ItemIconMigration.MigrateObject(controller), "Controller migration is idempotent");
        });
    }
}
