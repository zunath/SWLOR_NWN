using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Foundry checkpointed legacy drops and salvage gear migrate together in the existing stored-item pass",
        Category = LootCategory, TimeoutSeconds = 120f)]
    public static async Task FoundryLootStoredCorrection(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        var records = new List<InventoryItem>();
        var identities = new Dictionary<string, string>();
        var enhancements = new Dictionary<(ItemPropertyType, int), int> { [(ItemPropertyType.HPBonus, 0)] = 40 };
        try
        {
            foreach (var resref in new[] { "grapple_bracer", "conveyor_treads", "line_visor", "riveted_mantle", "quality_signet",
                         "lattice_plate", "sorting_harness", "assembly_gaunt", "nullfield_collar" })
            {
                var item = await CreateItemAsync(ctx, owner, resref, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    StampLegacyLoot(item, enhancements);
                    var identity = GetObjectUUID(item);
                    var record = new InventoryItem { Data = ObjectPlugin.Serialize(item), Name = resref, Quantity = 1 };
                    DB.Set(record);
                    records.Add(record);
                    identities[record.Id] = identity;
                    Invoke("MigrationObject", "DestroyTemporaryObject", item);
                });
            }
            for (var cycle = 1; cycle <= 2; cycle++)
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    StoredItemSweepRunner.Run();
                    foreach (var record in records)
                    {
                        var saved = DB.Get<InventoryItem>(record.Id);
                        var restored = Deserialize(ctx, saved.Data);
                        AssertRevisedLoot(ctx, restored, enhancements);
                        ctx.AssertEqual(record.Name, saved.Name, "Storage metadata survives migration");
                        ctx.AssertEqual(1, saved.Quantity, "Storage quantity survives migration");
                        ctx.AssertEqual(1, GetItemStackSize(restored), "Item quantity survives reload");
                        ctx.AssertEqual(identities[record.Id], GetObjectUUID(restored), $"{record.Name} identity survives reload");
                        ctx.AssertEqual(cycle - 1, GetLocalInt(restored, "FOUNDRY_LOOT_TEST_CYCLE"), "Changed state survives the next reload");
                        ctx.Assert(!LootStatRevisionMigration.MigrateObject(restored), "Reloaded items need no further migration");
                        SetLocalInt(restored, "FOUNDRY_LOOT_TEST_CYCLE", cycle);
                        saved.Data = ObjectPlugin.Serialize(restored);
                        DB.Set(saved);
                        Invoke("MigrationObject", "DestroyTemporaryObject", restored);
                    }
                });
        }
        finally
        {
            foreach (var record in records)
                DB.Delete<InventoryItem>(record.Id);
        }
    }

    [EngineTest("Foundry loot migration preserves legacy and revised equipment in the saved character through use and repeated reloads",
        Category = LootCategory, TimeoutSeconds = 300f)]
    public static async Task FoundryLootEquipmentCorrection(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            // Keep the complete HP budget below the one-level fixture's native cap.
            var record = DB.Get<Player>(GetObjectUUID(player));
            record.TemporaryFoodHP = 100 - Stat.BaseHP;
            Stat.AdjustPlayerMaxHP(record, player, 100 - record.MaxHP);
            DB.Set(record);
            SetCurrentHitPoints(player, 100);
        });
        var enhancements = new Dictionary<(ItemPropertyType, int), int>
        {
            [(ItemPropertyType.HPBonus, 0)] = 40,
            [(ItemPropertyType.Attack, 0)] = 2,
        };

        // Cover shipped equipment, the first loot revision, and its rare-stat correction.
        for (var revision = 0; revision <= 2; revision++)
        {
            var item = await CreateItemAsync(ctx, player, "grapple_bracer", player);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                StampLegacyLoot(item, enhancements);
                if (revision >= 1)
                    RestoreRevision(LootStatRevisionCatalog.Changes["grapple_bracer"], LootStatRevisionMigration.RevisionVariable);
                if (revision >= 2)
                    RestoreRevision(RareLootStatRevisionCatalog.Changes["grapple_bracer"], RareLootStatRevisionCatalog.RevisionVariable);
                ctx.AssertEqual(revision == 0 ? 58 : 103, SumProperty(item, ItemPropertyType.HPBonus, 0),
                    "The fixture starts with historical HP and a merged enhancement");
                RemoveRequirement(item);

                void RestoreRevision(IEnumerable<LootStatChange> changes, string marker)
                {
                    foreach (var change in changes)
                    {
                        RemoveMatching(item, change.Type, change.Subtype);
                        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(change.Type, LootSubtype(change),
                            change.Current + enhancements.GetValueOrDefault((change.Type, change.Subtype))), item);
                    }
                    SetLocalInt(item, marker, 1);
                }
            });
            var baseline = ReadEquipmentTotals(player);
            var baselineHp = GetMaxHitPoints(player);
            var baselineAttack = baseline.Attack;
            await EquipAsync(ctx, player, item, InventorySlot.Arms);

            // The last released character checkpoint is 22: the saved file and the record are both behind by one migration.
            SavedCharacterMigration.Upgraded upgraded = null;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var record = DB.Get<Player>(GetObjectUUID(player));
                record.Version = 23;
                DB.Set(record);
                SetLocalInt(player, "PLAYER_MIGRATION_VERSION", 23);
                ctx.AssertEqual(24, SavedCharacterMigration.Versions.Where(version => version > 23).Single(),
                    "The last released character checkpoint schedules exactly one combined item migration");
                var result = SavedCharacterMigration.Upgrade(SavedCharacterMigration.SaveFile(player), record);
                ctx.Assert(result != null, "The character upgrade converts the legacy equipment");
                ctx.AssertEqual(24, result.Record.Version, "The player record checkpoint advances");
                ctx.AssertEqual(baseline.MaxHP + 100, result.Record.MaxHP, "The persisted HP budget matches the equipment change");
                ctx.AssertEqual(baselineAttack + 13, result.Record.Attack, "Legacy Attack is corrected in the same item visit");
                var migrated = SavedCharacterMigration.FindItem(SavedCharacterMigration.Load(ctx, result), "grapple_bracer");
                AssertRevisedLoot(ctx, migrated, enhancements);
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(migrated), "The live revision finds nothing left to change in the upgraded item");
                ctx.Assert(SavedCharacterMigration.Upgrade(result.File, result.Record) == null,
                    "A completed checkpoint does not revisit the items");
                DeleteLocalInt(player, "PLAYER_MIGRATION_VERSION");
                upgraded = result;
            });

            // The player logs in with the upgraded character and plays on with the corrected equipment.
            using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
            var loggedIn = login.Creature;
            item = SavedCharacterMigration.FindItem(loggedIn, "grapple_bracer");
            ctx.Assert(GetIsObjectValid(item), "The upgraded character logs in with the bracer");
            ctx.AssertEqual(item, GetItemInSlot(InventorySlot.Arms, loggedIn), "The bracer is still worn");
            ctx.AssertEqual(baselineHp + 100, GetMaxHitPoints(loggedIn), "Equipped HP includes the corrected base and enhancement once");
            ctx.AssertEqual(baselineAttack + 13, DB.Get<Player>(login.Id).Attack, "Equipped Attack counts the corrected total once");

            for (var cycle = 1; cycle <= 2; cycle++)
            {
                await UnequipAsync(ctx, loggedIn, item);
                ctx.AssertEqual(baselineHp, GetMaxHitPoints(loggedIn), "Unequipping removes exactly the corrected HP");
                ctx.AssertEqual(baseline.MaxHP, DB.Get<Player>(login.Id).MaxHP, "Unequipping restores the persisted HP budget");
                ctx.AssertEqual(baselineAttack, DB.Get<Player>(login.Id).Attack, "Unequipping removes exactly the corrected Attack");
                string saved = null;
                await ctx.ExecuteInCreatureContextAsync(loggedIn, () =>
                {
                    SetLocalInt(item, "FOUNDRY_LOOT_TEST_CYCLE", cycle);
                    saved = ObjectPlugin.Serialize(item);
                    DestroyObject(item);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(loggedIn, () =>
                {
                    item = Deserialize(ctx, saved);
                    ctx.Assert(ObjectPlugin.AcquireItem(loggedIn, item), "The player reacquires the saved equipment");
                });
                await ctx.WaitFrameAsync();
                var matching = SavedCharacterMigration.FindItems(loggedIn, "grapple_bracer");
                ctx.AssertEqual(1, matching.Count, "Reload preserves exactly one bracer");
                item = matching.Single();
                ctx.Track(item);
                ctx.AssertEqual(loggedIn, GetItemPossessor(item), "Ownership survives reload");
                ctx.AssertEqual(1, GetItemStackSize(item), "Quantity survives reload");
                ctx.AssertEqual(cycle, GetLocalInt(item, "FOUNDRY_LOOT_TEST_CYCLE"), "Changed state survives reload");
                AssertRevisedLoot(ctx, item, enhancements);
                await EquipAsync(ctx, loggedIn, item, InventorySlot.Arms);
                ctx.AssertEqual(baselineHp + 100, GetMaxHitPoints(loggedIn), "Reloaded HP counts once");
                ctx.AssertEqual(baselineAttack + 13, DB.Get<Player>(login.Id).Attack, "Reloaded Attack counts once");
            }
            await UnequipAsync(ctx, loggedIn, item);
            DestroyObject(item);
            await ctx.WaitFrameAsync();
            await UnequipAsync(ctx, player, SavedCharacterMigration.FindItem(player, "grapple_bracer"));
            DestroyObject(SavedCharacterMigration.FindItem(player, "grapple_bracer"));
            await ctx.WaitFrameAsync();
        }

    }
}
