using System.Threading.Tasks;
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
            ctx.AssertEqual(ItemIconAppearance.GetUpdatedModel("aen_hp1", (int)GetBaseItemType(item), 88), GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Newly created blueprints use the reviewed appearance");
            SetName(item, "Custom vitality enhancement");
            SetLocalInt(item, "CRAFTED_QUALITY", 29);
            SetItemCharges(item, 6);
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 88);
            var properties = DescribeProperties(item);
            var identity = GetObjectUUID(item);
            ctx.Assert(ShipItemIconMigration.MigrateObject(owner), "Enhancement appearance is normalized");
            ctx.AssertEqual(ItemIconAppearance.GetUpdatedModel("aen_hp1", (int)GetBaseItemType(item), 88), GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Uses its reviewed enhancement artwork");
            ctx.AssertEqual(identity, GetObjectUUID(item), "The original native object is retained");
            ctx.AssertEqual("Custom vitality enhancement", GetName(item), "Custom name is retained");
            ctx.AssertEqual(29, GetLocalInt(item, "CRAFTED_QUALITY"), "Crafting metadata is retained");
            ctx.AssertEqual(6, GetItemCharges(item), "Charges are retained");
            ctx.AssertEqual(properties, DescribeProperties(item), "Enhancement properties are retained");
            ctx.Assert(!ShipItemIconMigration.MigrateObject(owner), "Updates are idempotent");
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 254);
            ctx.Assert(!ShipItemIconMigration.MigrateObject(item), "A customized model is preserved");
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
            ctx.AssertEqual(ItemIconAppearance.GetUpdatedModel("imp_cotton_1", (int)GetBaseItemType(material), 91), GetItemAppearance(material, ItemAppearanceType.SimpleModel, 0), "New crafting materials use the reviewed appearance");
            ItemPlugin.SetItemAppearance(material, ItemAppearanceType.SimpleModel, 0, 91);
            SetLocalString(material, "CRAFTED_SOURCE", "retained");
            var properties = DescribeProperties(material);
            var serialized = ObjectPlugin.Serialize(bag);
            ctx.Assert(ShipItemIconMigration.MigrateSerializedObject(serialized, out var updated), "Nested crafting material artwork is saved");
            var restored = Deserialize(ctx, updated);
            var restoredMaterial = GetFirstItemInInventory(restored);
            ctx.AssertEqual("imp_cotton_1", GetResRef(restoredMaterial), "Material identity is retained");
            ctx.AssertEqual(properties, DescribeProperties(restoredMaterial), "Material crafting properties are retained");
            ctx.AssertEqual("retained", GetLocalString(restoredMaterial, "CRAFTED_SOURCE"), "Custom metadata survives serialization");
            ctx.AssertEqual(ItemIconAppearance.GetUpdatedModel("imp_cotton_1", (int)GetBaseItemType(restoredMaterial), 91), GetItemAppearance(restoredMaterial, ItemAppearanceType.SimpleModel, 0), "Saved material artwork is updated");
            ctx.Assert(!ShipItemIconMigration.MigrateSerializedObject(updated, out var unchanged), "Saved material data is idempotent");
            ctx.AssertEqual(updated, unchanged, "Unchanged serialized data is returned verbatim");
        });
    }
}
