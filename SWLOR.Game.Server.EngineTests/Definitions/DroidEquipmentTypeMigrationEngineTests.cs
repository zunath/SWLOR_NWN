using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class DroidInventoryEngineTests
{
    [EngineTest("Legacy droid shawl moves from arms to cloak and remains usable through two resummons", Category = "DroidInventory", TimeoutSeconds = 120)]
    public static Task CorrectedDroidShawl(EngineTestContext ctx) => VerifyDroidEquipmentType(ctx, "hexweave");

    [EngineTest("Legacy droid cowl moves from arms to head and remains usable through two resummons", Category = "DroidInventory", TimeoutSeconds = 120)]
    public static Task CorrectedDroidCowl(EngineTestContext ctx) => VerifyDroidEquipmentType(ctx, "absdef_l3");

    [EngineTest("Legacy droid hand wraps convert to gloves and remain usable through two resummons", Category = "DroidInventory", TimeoutSeconds = 120)]
    public static Task CorrectedDroidHandwraps(EngineTestContext ctx) => VerifyDroidEquipmentType(ctx, "brawl_wraps");

    private static async Task VerifyDroidEquipmentType(EngineTestContext ctx, string resref)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var controller = await CreateController(ctx, owner);
        var legacy = await CreateLoot(ctx, owner, resref);
        var correction = EquipmentTypeMigration.Corrections[resref];
        var uuid = GetObjectUUID(legacy);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            MigrationEngineTests.RemoveRequirement(legacy);
            AddItemProperty(DurationType.Permanent, ItemPropertyLimitUseByRace(RacialType.Droid), legacy);
            ItemPlugin.SetBaseItemType(legacy, correction.LegacyType);
            ItemPlugin.SetItemAppearance(legacy, ItemAppearanceType.SimpleModel, 0, correction.LegacyType == BaseItem.Gloves ? 198 : 176);
            SetLocalString(legacy, "DROID_ITEM_ID", "equipment-type-fixture");
            ctx.AssertEqual(correction.LegacyType, GetBaseItemType(legacy), "Saved fixture begins with the legacy wearable type");
            var archive = new ConstructedDroid
            {
                Name = "Wearable type fixture",
                EquippedItems = { [InventorySlot.Arms] = ObjectPlugin.Serialize(legacy) },
            };
            Droid.SaveConstructedDroid(controller, archive);
            DestroyObject(legacy);
            ctx.Assert(LootStatRevisionMigration.MigrateObject(controller), "Actual stored-controller conversion repairs the legacy item");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(controller), "Stored-controller retry is stable");
            var saved = Droid.LoadConstructedDroid(controller);
            if (correction.Slot != InventorySlot.Arms)
                ctx.Assert(!saved.EquippedItems.ContainsKey(InventorySlot.Arms), "The old arms slot no longer contains the moved garment");
        });
        await ctx.DelaySecondsAsync(0.3f);
        var droid = await Spawn(ctx, owner, controller);
        try
        {
            for (var cycle = 0; cycle <= 2; cycle++)
            {
                var item = MigrationEngineTests.FindHeadgear(droid, resref);
                ctx.Assert(GetIsObjectValid(item), "Each summon restores the corrected wearable");
                ctx.AssertEqual(item, GetItemInSlot(correction.Slot, droid), "Each summon restores the corrected equipment slot");
                ctx.AssertEqual(correction.Type, GetBaseItemType(item), "The corrected type persists");
                ctx.AssertEqual(correction.Model, GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "The corrected model persists");
                ctx.AssertEqual(uuid, GetObjectUUID(item), "Repeated save/reload preserves identity");
                ctx.AssertEqual(droid, GetItemPossessor(item), "Repeated save/reload preserves ownership");
                ctx.AssertEqual(1, GetItemStackSize(item), "Repeated save/reload preserves quantity");
                ctx.AssertEqual(1, Items(droid).Count(x => GetResRef(x) == resref), "Repeated save/reload creates no duplicate");
                await Unequip(ctx, droid, item);
                var baseline = SkinAttack(droid);
                await Equip(ctx, droid, item, correction.Slot);
                var equipped = SkinAttack(droid);
                ctx.Assert(equipped >= baseline, "The converted wearable can still be equipped and used");
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                    ctx.Assert(!LootStatRevisionMigration.MigrateObject(droid), "Live conversion remains idempotent after use"));
                await Unequip(ctx, droid, item);
                ctx.AssertEqual(baseline, SkinAttack(droid), "Unequip removes its contribution exactly once");
                await Equip(ctx, droid, item, correction.Slot);
                ctx.AssertEqual(equipped, SkinAttack(droid), "Re-equip does not accumulate stats");
                if (cycle == 2) break;
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
            }
        }
        finally
        {
            if (GetIsObjectValid(droid)) await Dismiss(ctx, owner, droid);
        }
    }
}
