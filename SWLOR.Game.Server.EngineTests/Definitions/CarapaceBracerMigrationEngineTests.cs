using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Carapace Bracer correction preserves enhancements and two bank equip cycles", Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static Task CarapaceBracerLifecycle(EngineTestContext ctx) => VerifyCarapaceBracerLifecycle(ctx, false);

    [EngineTest("Stored legacy Carapace Bracer correction survives two bank equip cycles", Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static Task StoredCarapaceBracerLifecycle(EngineTestContext ctx) => VerifyCarapaceBracerLifecycle(ctx, true);

    private static async Task VerifyCarapaceBracerLifecycle(EngineTestContext ctx, bool storedFirst)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = fixture.Creature;
        var current = GetItemInSlot(InventorySlot.Arms, owner);
        if (GetIsObjectValid(current)) await UnequipAsync(ctx, owner, current);
        var baseline = ReadEquipmentTotals(owner).PhysicalDefense;
        var item = await CreateItemAsync(ctx, owner, "cara_bracer", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(item), "New drops already carry the correction");
            RemoveMatching(item, ItemPropertyType.Defense, 1);
            RemoveMatching(item, ItemPropertyType.Defense, 2);
            RemoveRequirement(item);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Defense, 1, 23), item);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Defense, 2, 10), item);
            AddLegacyProperty(item, ItemPropertyType.Defense, 4, 35, 30);
            DeleteLocalInt(item, CarapaceBracerStatRevisionCatalog.RevisionVariable);
        });
        var uuid = GetObjectUUID(item);
        if (storedFirst)
        {
            InventoryItem legacy = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () => legacy = Bank.DepositItem(owner, item));
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(item), 5f, "the legacy bracer to enter storage");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var legacyData = DB.Get<InventoryItem>(legacy.Id).Data;
                new _29_CorrectStoredCarapaceBracers().Migrate();
                var migrated = DB.Get<InventoryItem>(legacy.Id);
                ctx.Assert(migrated.Data != legacyData, "The server migration rewrites the stored legacy item before withdrawal");
                new _29_CorrectStoredCarapaceBracers().Migrate();
                ctx.AssertEqual(migrated.Data, DB.Get<InventoryItem>(legacy.Id).Data, "Stored migration retry is stable");
                Bank.WithdrawItem(owner, legacy.Id);
            });
            await ctx.WaitFrameAsync();
            item = FindHeadgear(owner, "cara_bracer");
            ctx.AssertEqual(53, PropertyValue(item, ItemPropertyType.Defense, 1), "Stored legacy data receives the correction");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Defense, 4), "Stored legacy defense is removed");
        }
        await EquipAsync(ctx, owner, item, InventorySlot.Arms);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            new _24_CorrectCarapaceBracers().Migrate(owner);
            ctx.AssertEqual(53, PropertyValue(item, ItemPropertyType.Defense, 1), "The five-point enhancement survives the base correction");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Defense, 4), "Retired defense is removed");
            ctx.AssertEqual(baseline + 53, ReadEquipmentTotals(owner).PhysicalDefense, "Equipped defense updates exactly once");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(owner), "Retry is stable");
        });
        for (var cycle = 0; cycle < 2; cycle++)
        {
            await UnequipAsync(ctx, owner, item);
            ctx.AssertEqual(baseline, ReadEquipmentTotals(owner).PhysicalDefense, "Unequip removes the full corrected contribution");
            InventoryItem stored = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () => stored = Bank.DepositItem(owner, item));
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(item), 5f, "the deposited bracer to leave inventory");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                new _29_CorrectStoredCarapaceBracers().Migrate();
                Bank.WithdrawItem(owner, stored.Id);
            });
            await ctx.WaitFrameAsync();
            item = FindHeadgear(owner, "cara_bracer");
            ctx.AssertEqual(1, CountHeadgear(owner, "cara_bracer"), "Reload retains exactly one bracer");
            ctx.AssertEqual(uuid, GetObjectUUID(item), "Reload preserves identity");
            ctx.AssertEqual(owner, GetItemPossessor(item), "Reload preserves ownership");
            ctx.AssertEqual(1, GetItemStackSize(item), "Reload preserves quantity");
            ctx.AssertEqual(53, PropertyValue(item, ItemPropertyType.Defense, 1), "Reload preserves enhanced physical defense");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Defense, 4), "Reload never restores retired defense");
            await EquipAsync(ctx, owner, item, InventorySlot.Arms);
            ctx.AssertEqual(baseline + 53, ReadEquipmentTotals(owner).PhysicalDefense, "Reloaded equipment remains usable without stat drift");
        }
    }
}
