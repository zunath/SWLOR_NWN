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
}
