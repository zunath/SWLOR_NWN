using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
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
        var config = DB.Get<ServerConfiguration>("SWLOR_CONFIG");
        var originalVersion = config.MigrationVersion;
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
                    config.MigrationVersion = 28;
                    DB.Set(config);
                    var pending = (IEnumerable<IServerMigration>)typeof(Migration)
                        .GetMethod("GetMigrations", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { MigrationExecutionType.PostCacheLoad });
                    ctx.Assert(pending.Select(migration => migration.Version)
                        .SequenceEqual(new[] { 29 }),
                        "The last released server checkpoint schedules exactly one combined stored-item migration");
                    Migration.AfterCacheLoaded();
                    ctx.AssertEqual(29, DB.Get<ServerConfiguration>("SWLOR_CONFIG").MigrationVersion,
                        "The stored-item correction advances the server checkpoint");
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
            config.MigrationVersion = originalVersion;
            DB.Set(config);
        }
    }

    [EngineTest("Foundry loot migration preserves legacy and revised equipment through use and repeated reloads",
        Category = LootCategory, TimeoutSeconds = 180f)]
    public static async Task FoundryLootEquipmentCorrection(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            // Keep the complete HP budget below the one-level fixture's native cap.
            var record = DB.Get<Player>(GetObjectUUID(player));
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
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var record = DB.Get<Player>(GetObjectUUID(player));
                record.Version = 23;
                DB.Set(record);
                SetLocalInt(player, "PLAYER_MIGRATION_VERSION", 23);
                var fileVersion = 23;
                var fileSaves = 0;
                var pending = typeof(_23_CorrectEquipmentTypes).Assembly.GetTypes()
                    .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IPlayerMigration).IsAssignableFrom(type))
                    .Select(type => (IPlayerMigration)Activator.CreateInstance(type))
                    .Where(migration => migration.Version > Math.Min(record.Version, fileVersion)).ToArray();
                ctx.AssertEqual(24, pending.Single().Version, "The last released player checkpoint schedules exactly one combined item migration");
                var apply = typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.NonPublic | BindingFlags.Static);
                bool ApplyMigration() => (bool)apply.Invoke(null, new object[] { pending.Single(), player,
                    (Func<Player>)(() => DB.Get<Player>(GetObjectUUID(player))), (Action<Player>)(updated => DB.Set(updated)),
                    (Func<int>)(() => fileVersion), (Action<int>)(version =>
                    {
                        SetLocalInt(player, "PLAYER_MIGRATION_VERSION", version);
                        var saved = ObjectPlugin.Serialize(player);
                        ctx.AssertEqual(version, (int)Invoke("StoredObjectData", "ReadRootInteger", saved, "PLAYER_MIGRATION_VERSION"),
                            "The saved character contains the completed checkpoint");
                        fileVersion = version;
                        fileSaves++;
                    }) });
                ctx.Assert(ApplyMigration(), "The actual player checkpoint path converts the legacy equipment");
                ctx.AssertEqual(24, DB.Get<Player>(GetObjectUUID(player)).Version, "The player database checkpoint advances");
                ctx.Assert(ApplyMigration(), "Retrying the completed checkpoint succeeds");
                ctx.AssertEqual(1, fileSaves, "A completed checkpoint does not revisit or save the items twice");
                AssertRevisedLoot(ctx, item, enhancements);
                ctx.AssertEqual(baselineHp + 100, GetMaxHitPoints(player), "Equipped HP includes the corrected base and enhancement once");
                ctx.AssertEqual(baseline.MaxHP + 100, ReadEquipmentTotals(player).MaxHP, "The persisted HP budget matches the equipment change");
                ctx.AssertEqual(baselineAttack + 13, DB.Get<Player>(GetObjectUUID(player)).Attack, "Legacy Attack is corrected in the same item visit");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(player), "Retrying the existing migration is stable");
            });

            for (var cycle = 1; cycle <= 2; cycle++)
            {
                await UnequipAsync(ctx, player, item);
                ctx.AssertEqual(baselineHp, GetMaxHitPoints(player), "Unequipping removes exactly the corrected HP");
                ctx.AssertEqual(baseline.MaxHP, ReadEquipmentTotals(player).MaxHP, "Unequipping restores the persisted HP budget");
                ctx.AssertEqual(baselineAttack, DB.Get<Player>(GetObjectUUID(player)).Attack, "Unequipping removes exactly the corrected Attack");
                string saved = null;
                string identity = null;
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    SetLocalInt(item, "FOUNDRY_LOOT_TEST_CYCLE", cycle);
                    identity = GetObjectUUID(item);
                    saved = ObjectPlugin.Serialize(item);
                    DestroyObject(item);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    item = Deserialize(ctx, saved);
                    ctx.Assert(ObjectPlugin.AcquireItem(player, item), "The player reacquires the saved equipment");
                });
                await ctx.WaitFrameAsync();
                var matching = new List<uint>();
                for (var carried = GetFirstItemInInventory(player); GetIsObjectValid(carried); carried = GetNextItemInInventory(player))
                    if (GetResRef(carried) == "grapple_bracer") matching.Add(carried);
                ctx.AssertEqual(1, matching.Count, "Reload preserves exactly one bracer");
                item = matching.Single();
                ctx.Track(item);
                ctx.AssertEqual(player, GetItemPossessor(item), "Ownership survives reload");
                ctx.AssertEqual(1, GetItemStackSize(item), "Quantity survives reload");
                ctx.AssertEqual(identity, GetObjectUUID(item), "Identity survives reload");
                ctx.AssertEqual(cycle, GetLocalInt(item, "FOUNDRY_LOOT_TEST_CYCLE"), "Changed state survives reload");
                AssertRevisedLoot(ctx, item, enhancements);
                await EquipAsync(ctx, player, item, InventorySlot.Arms);
                ctx.AssertEqual(baselineHp + 100, GetMaxHitPoints(player), "Reloaded HP counts once");
                ctx.AssertEqual(baselineAttack + 13, DB.Get<Player>(GetObjectUUID(player)).Attack, "Reloaded Attack counts once");
            }
            await UnequipAsync(ctx, player, item);
            DestroyObject(item);
            await ctx.WaitFrameAsync();
        }

    }
}
