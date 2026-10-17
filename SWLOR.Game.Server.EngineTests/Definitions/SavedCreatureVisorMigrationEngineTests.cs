using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Saved creature visor repair preserves authored gear through two archive reloads", Category = "ItemIcons", TimeoutSeconds = 90)]
    public static async Task SavedCreatureVisorModels(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var helmet = await CreateItemAsync(ctx, owner, "thermlancercr", owner);
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var nested = await CreateItemAsync(ctx, owner, "ruptorvanecr", bag);
        var custom = await CreateItemAsync(ctx, owner, "precision_optic", bag);
        var reserved = await CreateItemAsync(ctx, owner, "revan_mask", bag);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            RemoveRequirement(helmet);
            ItemPlugin.SetItemAppearance(helmet, ItemAppearanceType.SimpleModel, 0, 223);
            ItemPlugin.SetItemAppearance(nested, ItemAppearanceType.SimpleModel, 0, 223);
            ItemPlugin.SetItemAppearance(custom, ItemAppearanceType.SimpleModel, 0, 33);
            SetLocalString(owner, "PRESERVED_CREATURE_NOTE", "Authored creature state");
            SetLocalInt(helmet, "CRAFTED_QUALITY", 29);
            SetName(helmet, "Authored thermal visor");
        });
        var oldHead = GetItemInSlot(InventorySlot.Head, owner);
        if (GetIsObjectValid(oldHead)) await UnequipAsync(ctx, owner, oldHead);
        await EquipAsync(ctx, owner, helmet, InventorySlot.Head);
        var properties = DescribeProperties(helmet);
        DMCreature archive = null;

        string CreatureState(string data)
        {
            var document = Invoke("StoredObjectData", "ReadCreature", data);
            var root = document.GetType().GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(document);
            var fields = ((IEnumerable)root.GetType().GetField("Fields").GetValue(root)).Cast<object>();
            return string.Join("\n", fields.Where(field =>
                    field.GetType().GetProperty("Name").GetValue(field) is not ("ItemList" or "Equip_ItemList"))
                .Select(field => JsonSerializer.Serialize(field, field.GetType(), new JsonSerializerOptions { IncludeFields = true })));
        }

        uint Find(uint container, string resref)
        {
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
                if (GetResRef(item) == resref) return item;
            ctx.Assert(false, "Saved inventory retains " + resref);
            return OBJECT_INVALID;
        }

        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.AssertEqual(223, GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, 0), "Fixture reproduces the saved reserved helmet");
            archive = new DMCreature("Authored archive", "authored_archive", ObjectPlugin.Serialize(owner));
            DB.Set(archive);
            var originalState = CreatureState(archive.Data);
            new _28_CorrectStoredEquipmentTypes().Migrate();
            archive = DB.Get<DMCreature>(archive.Id);
            ctx.AssertEqual(originalState, CreatureState(archive.Data), "All non-inventory creature fields remain byte-for-byte equivalent");
        });

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            uint restored = OBJECT_INVALID;
            uint restoredHelmet = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                restored = Deserialize(ctx, archive.Data);
                restoredHelmet = GetItemInSlot(InventorySlot.Head, restored);
                ctx.AssertEqual("thermlancercr", GetResRef(restoredHelmet), "Authored Head slot is retained");
                ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(restoredHelmet), "The archived equipment type is retained");
                ctx.AssertEqual(110, GetItemAppearance(restoredHelmet, ItemAppearanceType.SimpleModel, 0), "Equipped reserved model is repaired");
                ctx.AssertEqual(properties, DescribeProperties(restoredHelmet), "Authored item stats remain unchanged");
                ctx.AssertEqual(29, GetLocalInt(restoredHelmet, "CRAFTED_QUALITY"), "Crafting data survives");
                ctx.AssertEqual(restored, GetItemPossessor(restoredHelmet), "The archived creature owns its equipment");
                ctx.AssertEqual(1, GetItemStackSize(restoredHelmet), "Exactly one helmet remains equipped");
                ctx.AssertEqual("Authored creature state", GetLocalString(restored, "PRESERVED_CREATURE_NOTE"), "Authored creature data survives");
                var restoredBag = Find(restored, "bag_sm");
                ctx.AssertEqual(3, Item.GetInventoryItemCount(restoredBag), "Nested item count is unchanged");
                ctx.AssertEqual(110, GetItemAppearance(Find(restoredBag, "ruptorvanecr"), ItemAppearanceType.SimpleModel, 0), "Nested reserved model is repaired");
                ctx.AssertEqual(33, GetItemAppearance(Find(restoredBag, "precision_optic"), ItemAppearanceType.SimpleModel, 0), "Valid customization survives");
                ctx.AssertEqual(223, GetItemAppearance(Find(restoredBag, "revan_mask"), ItemAppearanceType.SimpleModel, 0), "The canonical NPC prop is preserved");
            });
            await UnequipAsync(ctx, restored, restoredHelmet);
            await ctx.ExecuteInCreatureContextAsync(restored, () => SetDescription(restoredHelmet, "Changed archive cycle " + cycle));
            await EquipAsync(ctx, restored, restoredHelmet, InventorySlot.Head);
            await ctx.ExecuteInCreatureContextAsync(restored, () =>
            {
                archive.Data = ObjectPlugin.Serialize(restored);
                DB.Set(archive);
                var saved = archive.Data;
                new _28_CorrectStoredEquipmentTypes().Migrate();
                archive = DB.Get<DMCreature>(archive.Id);
                ctx.AssertEqual(saved, archive.Data, "An archive migration retry retains the saved payload exactly");
                ctx.AssertEqual("Authored archive", archive.Name, "Archive identity metadata survives");
                ctx.AssertEqual("authored_archive", archive.Tag, "Archive tag survives");
                var reloaded = Deserialize(ctx, archive.Data);
                ctx.AssertEqual("Changed archive cycle " + cycle, GetDescription(GetItemInSlot(InventorySlot.Head, reloaded)), "Gameplay changes survive saving and reloading");
            });
        }
    }
}
