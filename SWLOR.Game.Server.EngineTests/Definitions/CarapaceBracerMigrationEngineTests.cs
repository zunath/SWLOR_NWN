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

    [EngineTest("Previously converted Carapace Bracer ends up like a new drop through two bank equip cycles", Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static Task ConvertedCarapaceBracerLifecycle(EngineTestContext ctx) => VerifyCarapaceBracerLifecycle(ctx, false, true);

    [EngineTest("An older file restored beside a current record corrects its worn bracer without changing the record again",
        Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static async Task RestoredFileBesideCurrentRecord(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = fixture.Creature;
        var current = GetItemInSlot(InventorySlot.Arms, owner);
        if (GetIsObjectValid(current)) await UnequipAsync(ctx, owner, current);
        var item = await CreateItemAsync(ctx, owner, "cara_bracer", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            RemoveRequirement(item);
            // A copy the combat migration converted: its 30-point base Poison Defense became Poison Resistance.
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Resistance, (int)ResistanceType.Poison,
                Resistance.EncodeItemPropertyCostTableValue(30)), item);
        });
        await EquipAsync(ctx, owner, item, InventorySlot.Arms);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            DeleteLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable);
            // The record was upgraded with a newer file and no longer counts the base resistance; this older file still has it.
            var record = SavedCharacterMigration.CopyRecord(DB.Get<Player>(fixture.Id));
            var revised = record.Resistances.GetValueOrDefault(ResistanceType.Poison) - 30;
            record.Resistances[ResistanceType.Poison] = revised;
            record.Version = SavedCharacterMigration.LatestVersion;
            record.CharacterFileVersion = 0;
            var restored = SavedCharacterMigration.WithFileVersion(SavedCharacterMigration.SaveFile(owner), SavedCharacterMigration.LatestVersion - 1);

            var upgraded = SavedCharacterMigration.Upgrade(restored, record);
            ctx.Assert(upgraded != null, "The restored file is behind and is upgraded");
            var migrated = SavedCharacterMigration.FindItem(SavedCharacterMigration.Load(ctx, upgraded), "cara_bracer");
            ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.Resistance, (int)ResistanceType.Poison), "The restored file's bracer is corrected");
            ctx.AssertEqual(revised, upgraded.Record.Resistances.GetValueOrDefault(ResistanceType.Poison),
                "The current record already counts the correction and is not changed again");
        });
    }

    [EngineTest("Migration 24 raises worn pre-fix Tribal Protection and Dantari Hyper Gloves on the record once",
        Category = "CarapaceBracerMigration", TimeoutSeconds = 120)]
    public static async Task RaisedDantooineItemsOnSavedCharacter(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = fixture.Creature;
        foreach (var slot in new[] { InventorySlot.Chest, InventorySlot.Arms })
        {
            var current = GetItemInSlot(slot, owner);
            if (GetIsObjectValid(current)) await UnequipAsync(ctx, owner, current);
        }
        var baseline = ReadEquipmentTotals(owner);

        // Pre-fix copies: Tribal Protection 20 Physical (plus a 3-point enhancement) and Fire Defense 10; Dantari
        // Hyper Gloves 10 Physical, 10 Force, no HP, and Ice Defense 5.
        var armor = await CreateItemAsync(ctx, owner, "dan_heavy_armor", owner);
        var gloves = await CreateItemAsync(ctx, owner, "dan_hyper_gloves", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            foreach (var item in new[] { armor, gloves })
            {
                RemoveRequirement(item);
                RemoveMatching(item, ItemPropertyType.Defense, (int)CombatDamageType.Physical);
                RemoveMatching(item, ItemPropertyType.Defense, (int)CombatDamageType.Force);
                RemoveMatching(item, ItemPropertyType.HPBonus, 0);
            }
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 23), armor);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, 95), armor);
            AddLegacyProperty(armor, ItemPropertyType.Defense, (int)CombatDamageType.Fire, 35, 10);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 10), gloves);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Force, 10), gloves);
            AddLegacyProperty(gloves, ItemPropertyType.Defense, (int)CombatDamageType.Ice, 35, 5);
        });

        // Materialize the native legacy properties through storage; the marker keeps the live revision away meanwhile.
        foreach (var resref in new[] { "dan_heavy_armor", "dan_hyper_gloves" })
        {
            var item = FindHeadgear(owner, resref);
            InventoryItem stored = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable, RetiredDefenseStatRevisionCatalog.CurrentRevision);
                stored = Bank.DepositItem(owner, item);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(item), 5f, $"the pre-fix {resref} to enter storage");
            await ctx.ExecuteInCreatureContextAsync(owner, () => Bank.WithdrawItem(owner, stored.Id));
            await ctx.WaitFrameAsync();
            item = FindHeadgear(owner, resref);
            await ctx.ExecuteInCreatureContextAsync(owner, () => DeleteLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable));
        }
        armor = FindHeadgear(owner, "dan_heavy_armor");
        gloves = FindHeadgear(owner, "dan_hyper_gloves");
        ctx.AssertEqual(10, PropertyValue(armor, ItemPropertyType.Defense, (int)CombatDamageType.Fire), "The pre-fix armor keeps its Fire Defense");
        ctx.AssertEqual(5, PropertyValue(gloves, ItemPropertyType.Defense, (int)CombatDamageType.Ice), "The pre-fix gloves keep their Ice Defense");
        await EquipAsync(ctx, owner, armor, InventorySlot.Chest);
        await EquipAsync(ctx, owner, gloves, InventorySlot.Arms);
        var worn = ReadEquipmentTotals(owner);
        ctx.AssertEqual(baseline.PhysicalDefense + 23 + 10, worn.PhysicalDefense, "Pre-fix Physical Defense counts before the upgrade");
        ctx.AssertEqual(baseline.ForceDefense + 10, worn.ForceDefense, "Pre-fix Force Defense counts before the upgrade");
        ctx.AssertEqual(baseline.MaxHP + 95, worn.MaxHP, "Pre-fix HP counts before the upgrade");

        SavedCharacterMigration.Upgraded upgraded = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            upgraded = SavedCharacterMigration.Run(owner, DB.Get<Player>(fixture.Id), "_24_CorrectCarapaceBracers");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            var migratedArmor = SavedCharacterMigration.FindItem(character, "dan_heavy_armor");
            var migratedGloves = SavedCharacterMigration.FindItem(character, "dan_hyper_gloves");
            ctx.AssertEqual(migratedArmor, GetItemInSlot(InventorySlot.Chest, character), "The armor stays worn");
            ctx.AssertEqual(migratedGloves, GetItemInSlot(InventorySlot.Arms, character), "The gloves stay worn");
            ctx.AssertEqual(28, PropertyValue(migratedArmor, ItemPropertyType.Defense, (int)CombatDamageType.Physical),
                "Physical rises from 20 to 25 and keeps the 3-point enhancement");
            ctx.AssertEqual(20, PropertyValue(migratedArmor, ItemPropertyType.Defense, (int)CombatDamageType.Force), "The armor gains 20 Force Defense");
            ctx.AssertEqual(-1, PropertyValue(migratedArmor, ItemPropertyType.Defense, (int)CombatDamageType.Fire), "Fire Defense is removed");
            ctx.AssertEqual(21, PropertyValue(migratedGloves, ItemPropertyType.Defense, (int)CombatDamageType.Force), "Glove Force Defense rises to 21");
            ctx.AssertEqual(21, PropertyValue(migratedGloves, ItemPropertyType.HPBonus), "The gloves gain 21 HP");
            ctx.AssertEqual(-1, PropertyValue(migratedGloves, ItemPropertyType.Defense, (int)CombatDamageType.Ice), "Ice Defense is removed");

            var record = ReadEquipmentTotals(upgraded.Record);
            ctx.AssertEqual(baseline.PhysicalDefense + 28 + 10, record.PhysicalDefense, "The record counts the raised Physical Defense once");
            ctx.AssertEqual(baseline.ForceDefense + 20 + 21, record.ForceDefense, "The record counts the raised Force Defense once");
            ctx.AssertEqual(baseline.MaxHP + 95 + 21, record.MaxHP, "The record counts the gloves' new HP once");

            var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_24_CorrectCarapaceBracers");
            ctx.Assert(retried.File.SequenceEqual(upgraded.File), "A retry makes no further changes");
            ctx.AssertEqual(record, ReadEquipmentTotals(retried.Record), "A retry counts nothing twice");
        });

        // The player logs in wearing the upgraded items, and the live revision finds nothing left.
        using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
        owner = login.Creature;
        ctx.AssertEqual(ReadEquipmentTotals(upgraded.Record), ReadEquipmentTotals(owner), "Login keeps the upgraded totals");
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(owner), "The live revision finds nothing left to change"));
    }

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
            DeleteLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable);
        });
        var uuid = GetObjectUUID(item);
        if (!storedFirst)
        {
            InventoryItem carried = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // Materialize native legacy properties through storage before testing equipped removal.
                SetLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable, RetiredDefenseStatRevisionCatalog.CurrentRevision);
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
                    // A converted copy above the base: the revision still leaves no Poison Resistance, like a new drop.
                    RemoveMatching(item, ItemPropertyType.Resistance, 2);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Resistance, 2, 35), item);
                });
            }
            DeleteLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable);
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
            ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.Resistance, 2), "No Poison Resistance remains");
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
            ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Resistance, 2), "Reload never restores Poison Resistance");
            await EquipAsync(ctx, owner, item, InventorySlot.Arms);
            ctx.AssertEqual(baseline + 23, ReadEquipmentTotals(owner).PhysicalDefense, "Reloaded equipment remains usable without stat drift");
        }
    }
}
