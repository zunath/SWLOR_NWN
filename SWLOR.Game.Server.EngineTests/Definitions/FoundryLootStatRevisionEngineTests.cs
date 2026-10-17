using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Foundry legacy drops and salvage gear migrate together in the existing stored-item pass",
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
                    new _27_UpdateStoredItems().Migrate();
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

    [EngineTest("Foundry loot migration preserves legacy and revised equipment through use and repeated reloads",
        Category = LootCategory, TimeoutSeconds = 180f)]
    public static async Task FoundryLootEquipmentCorrection(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
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
            var baselineHp = baseline.MaxHP;
            var baselineAttack = baseline.Attack;
            await EquipAsync(ctx, player, item, InventorySlot.Arms);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                new _22_ReviseLootStats().Migrate(player);
                AssertRevisedLoot(ctx, item, enhancements);
                ctx.AssertEqual(baselineHp + 100, GetMaxHitPoints(player), "Equipped HP includes the corrected base and enhancement once");
                ctx.AssertEqual(baselineAttack + 13, DB.Get<Player>(GetObjectUUID(player)).Attack, "Legacy Attack is corrected in the same item visit");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(player), "Retrying the existing migration is stable");
            });

            for (var cycle = 1; cycle <= 2; cycle++)
            {
                await UnequipAsync(ctx, player, item);
                ctx.AssertEqual(baselineHp, GetMaxHitPoints(player), "Unequipping removes exactly the corrected HP");
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
