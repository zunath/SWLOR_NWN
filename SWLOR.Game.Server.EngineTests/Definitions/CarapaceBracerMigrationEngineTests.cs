using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Carapace Bracer correction preserves enhancements and two bank equip cycles", Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static Task CarapaceBracerLifecycle(EngineTestContext ctx) => VerifyCarapaceBracerLifecycle(ctx, false);

    [EngineTest("Stored legacy Carapace Bracer correction survives two bank equip cycles", Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static Task StoredCarapaceBracerLifecycle(EngineTestContext ctx) => VerifyCarapaceBracerLifecycle(ctx, true);

    [EngineTest("Previously converted Carapace Bracer keeps crafted resistance through two bank equip cycles", Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static Task ConvertedCarapaceBracerLifecycle(EngineTestContext ctx) => VerifyCarapaceBracerLifecycle(ctx, false, true);

    private static async Task VerifyCarapaceBracerLifecycle(EngineTestContext ctx, bool storedFirst, bool converted = false)
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
        if (!storedFirst)
        {
            InventoryItem carried = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // Materialize native legacy properties through storage before testing equipped removal.
                SetLocalInt(item, CarapaceBracerStatRevisionCatalog.RevisionVariable, CarapaceBracerStatRevisionCatalog.CurrentRevision);
                carried = Bank.DepositItem(owner, item);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(item), 5f, "the legacy fixture to enter storage");
            await ctx.ExecuteInCreatureContextAsync(owner, () => Bank.WithdrawItem(owner, carried.Id));
            await ctx.WaitFrameAsync();
            item = FindHeadgear(owner, "cara_bracer");
            if (converted)
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    ctx.Assert((bool)Invoke("SerializedItemResistanceMigration", "MigrateObject", item), "The old combat conversion runs first");
                    ctx.AssertEqual(30, PropertyValue(item, ItemPropertyType.Resistance, 2), "The old conversion retained the base protection as resistance");
                    RemoveMatching(item, ItemPropertyType.Resistance, 2);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Resistance, 2, 35), item);
                });
            }
            DeleteLocalInt(item, CarapaceBracerStatRevisionCatalog.RevisionVariable);
        }
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
            ctx.AssertEqual(23, PropertyValue(item, ItemPropertyType.Defense, 1), "Stored legacy data receives the correction");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Defense, 4), "Stored legacy defense is removed");
        }
        await EquipAsync(ctx, owner, item, InventorySlot.Arms);
        SavedCharacterMigration.Upgraded upgraded = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            upgraded = SavedCharacterMigration.Run(owner, DB.Get<Player>(fixture.Id), "_24_CorrectCarapaceBracers");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            var migrated = SavedCharacterMigration.FindItem(character, "cara_bracer");
            ctx.AssertEqual(migrated, GetItemInSlot(InventorySlot.Arms, character), "The corrected bracer stays equipped");
            ctx.AssertEqual(23, PropertyValue(migrated, ItemPropertyType.Defense, 1), "The five-point enhancement survives the base correction");
            ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.Defense, 4), "Retired defense is removed");
            ctx.AssertEqual(converted ? 5 : -1, PropertyValue(migrated, ItemPropertyType.Resistance, 2), "Only the converted base resistance is removed");
            ctx.AssertEqual(baseline + 23, ReadEquipmentTotals(upgraded.Record).PhysicalDefense, "Equipped defense updates exactly once");
            var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_24_CorrectCarapaceBracers");
            ctx.Assert(retried.File.SequenceEqual(upgraded.File), "A retry makes no further changes");
            ctx.AssertEqual(upgraded.Record.Defenses.GetValueOrDefault(CombatDamageType.Physical),
                retried.Record.Defenses.GetValueOrDefault(CombatDamageType.Physical), "A retry counts no defense twice");
        });

        // The player logs in with the corrected character and uses the bracer through two bank cycles.
        using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
        owner = login.Creature;
        item = FindHeadgear(owner, "cara_bracer");
        uuid = GetObjectUUID(item);
        ctx.AssertEqual(item, GetItemInSlot(InventorySlot.Arms, owner), "The upgraded character logs in wearing the corrected bracer");
        ctx.AssertEqual(baseline + 23, ReadEquipmentTotals(owner).PhysicalDefense, "Login counts the corrected defense once");
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(owner), "The live revision finds nothing left to change"));
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
            ctx.AssertEqual(23, PropertyValue(item, ItemPropertyType.Defense, 1), "Reload preserves enhanced physical defense");
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Defense, 4), "Reload never restores retired defense");
            ctx.AssertEqual(converted ? 5 : -1, PropertyValue(item, ItemPropertyType.Resistance, 2), "Reload preserves only crafted resistance");
            await EquipAsync(ctx, owner, item, InventorySlot.Arms);
            ctx.AssertEqual(baseline + 23, ReadEquipmentTotals(owner).PhysicalDefense, "Reloaded equipment remains usable without stat drift");
        }
    }
}
