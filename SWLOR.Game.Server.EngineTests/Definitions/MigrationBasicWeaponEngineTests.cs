using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Migration repairs Qy's retired basic vibroknife without replacing customization", Category = "MigrationBasicWeapon")]
    public static async Task RetiredBasicVibroknife(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var item = await CreateItemAsync(ctx, owner, "b_knife", bag);
        string savedContainer = null;
        string savedMigratedContainer = null;
        string bagIdentity = null;
        string itemIdentity = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                properties.Add(property);
            foreach (var property in properties)
                Invoke("MigrationObject", "RemoveProperty", item, property);
            // No dagger_b UTI remains in the module. Build the old serialized row
            // from today's Dagger blueprint, then stamp its retired template resref.
            var nativeItem = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                .GetGameObject(item).AsNWSItem();
            using var legacyResref = new global::NWN.Native.API.CExoString("dagger_b");
            nativeItem.m_sTemplate = legacyResref;
            SetTag(item, "LEGACY_ITEM");
            SetName(item, "Qy's custom vibroknife");
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 22), item);
            SetLocalFloat(item, "DURABILITY_MAX", 5.0f);
            SetLocalInt(item, "CUSTOM_ITEM_PROPERTY_TYPE", 4);

            ctx.AssertEqual(BaseItem.Dagger, GetBaseItemType(item), "The saved item's Dagger base type");
            ctx.AssertEqual("dagger_b", GetResRef(item), "The retired template resref");
            ctx.AssertEqual(22, PropertyValue(item, ItemPropertyType.Delay), "The saved delay");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.DMG), "The saved item has no DMG");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.RequiresSkill), "The saved item has no skill requirement");
            bagIdentity = GetObjectUUID(bag);
            itemIdentity = GetObjectUUID(item);
            ctx.Assert(!string.IsNullOrWhiteSpace(bagIdentity), "The saved container has a native identity");
            ctx.Assert(!string.IsNullOrWhiteSpace(itemIdentity), "The saved dagger has a native identity");
            savedContainer = ObjectPlugin.Serialize(bag);
            DestroyObject(item);
            DestroyObject(bag);
        });
        await ctx.DelaySecondsAsync(0.3f);
        ctx.Assert(!GetIsObjectValid(item), "The original dagger releases its native UUID before migration");
        ctx.Assert(!GetIsObjectValid(bag), "The original container releases its native UUID before migration");

        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var (changed, migratedData) = MigrateStoredData(savedContainer);
            ctx.Assert(changed, "The nested retired dagger requires migration");
            ctx.Assert(ContainsIdentity(migratedData, bagIdentity), "Migration preserves the container identity");
            ctx.Assert(ContainsIdentity(migratedData, itemIdentity), "Migration preserves the dagger identity");

            var migrated = Deserialize(ctx, migratedData);
            var migratedItem = GetFirstItemInInventory(migrated);
            ctx.AssertEqual(bagIdentity, GetObjectUUID(migrated), "Reloaded container retains its identity");
            AssertRepairedDagger(ctx, migratedItem, itemIdentity);
            savedMigratedContainer = ObjectPlugin.Serialize(migrated);
            DestroyObject(migratedItem);
            DestroyObject(migrated);
        });
        await ctx.DelaySecondsAsync(0.3f);

        // Reload the saved repair only after the original native UUIDs have been
        // released, then run the full stored-item migration again for retry.
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var reloaded = Deserialize(ctx, savedMigratedContainer);
            var reloadedItem = GetFirstItemInInventory(reloaded);
            ctx.AssertEqual(bagIdentity, GetObjectUUID(reloaded), "Save/reload preserves the container identity");
            AssertRepairedDagger(ctx, reloadedItem, itemIdentity);
            DestroyObject(reloadedItem);
            DestroyObject(reloaded);
        });
        await ctx.DelaySecondsAsync(0.3f);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var (changed, retryData) = MigrateStoredData(savedMigratedContainer);
            ctx.Assert(!changed, "A saved repaired dagger needs no further migration");
            ctx.AssertEqual(savedMigratedContainer, retryData, "Retry preserves the exact serialized container");
            var retriedContainer = Deserialize(ctx, retryData);
            var retriedItem = GetFirstItemInInventory(retriedContainer);
            ctx.AssertEqual(bagIdentity, GetObjectUUID(retriedContainer), "Retry preserves the container identity");
            AssertRepairedDagger(ctx, retriedItem, itemIdentity);
        });
    }

    private static void AssertRepairedDagger(EngineTestContext ctx, uint item, string identity)
    {
        ctx.AssertEqual(identity, ObjectPlugin.PeekUUID(item), "Migration preserves item identity");
        ctx.AssertEqual(BaseItem.Dagger, GetBaseItemType(item), "Migration preserves the Dagger base type");
        ctx.AssertEqual("dagger_b", GetResRef(item), "Migration preserves the retired resref");
        ctx.AssertEqual("LEGACY_ITEM", GetTag(item), "Migration preserves the tag");
        ctx.AssertEqual("Qy's custom vibroknife", GetName(item), "Migration preserves the custom name");
        ctx.AssertEqual(5, PropertyValue(item, ItemPropertyType.DMG), "Missing basic damage is restored");
        ctx.AssertEqual(22, PropertyValue(item, ItemPropertyType.Delay), "Existing delay is preserved");
        ctx.AssertEqual(0, PropertyValue(item, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroknife),
            "Missing rank-zero Vibroknife requirement is restored");
        ctx.AssertEqual(5.0f, GetLocalFloat(item, "DURABILITY_MAX"), "Durability local variable is preserved");
        ctx.AssertEqual(4, GetLocalInt(item, "CUSTOM_ITEM_PROPERTY_TYPE"), "Legacy custom-property local variable is preserved");
    }

    private static (bool Changed, string Data) MigrateStoredData(string serializedObject)
    {
        var result = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", serializedObject);
        return (
            (bool)result.GetType().GetProperty("Changed").GetValue(result),
            (string)result.GetType().GetProperty("Data").GetValue(result));
    }

    private static bool ContainsIdentity(string serializedObject, string identity) =>
        System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(serializedObject))
            .Contains(identity, System.StringComparison.Ordinal);
}
