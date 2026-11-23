using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Rare loot correction updates already-nerfed bank items without repeating enhancements",
        Category = LootCategory, TimeoutSeconds = 60f)]
    public static async Task RareLootStoredCorrection(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        var weapon = await CreateItemAsync(ctx, owner, "stw_zerostate", owner);
        InventoryItem record = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampRareLootBeforeCorrection(weapon, true);
            ctx.AssertEqual(8, SumProperty(weapon, ItemPropertyType.Attack, 0), "The saved fixture starts at revised Attack 3 plus enhancement 5");
            record = new InventoryItem { Data = ObjectPlugin.Serialize(weapon), Name = "Saved Zero-State", Quantity = 1 };
            DB.Set(record);
        });
        try
        {
            for (var cycle = 1; cycle <= 2; cycle++)
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    StoredItemSweepRunner.Run();
                    var saved = DB.Get<InventoryItem>(record.Id);
                    var restored = Deserialize(ctx, saved.Data);
                    AssertRevisedLoot(ctx, restored, new() { [(ItemPropertyType.Attack, 0)] = 5 });
                    ctx.AssertEqual(1, saved.Quantity, "Bank quantity is preserved");
                    ctx.AssertEqual("Saved Zero-State", saved.Name, "Bank metadata is preserved");
                    ctx.AssertEqual(22, SumProperty(restored, ItemPropertyType.DMG, 0), "Bank DMG stays normalized");
                    ctx.Assert(!LootStatRevisionMigration.MigrateObject(restored), "A stored retry is stable");
                });
        }
        finally
        {
            DB.Delete<InventoryItem>(record.Id);
        }
    }

    private static void StampRareLootBeforeCorrection(uint item, bool alreadyRebalanced)
    {
        var enhancements = new Dictionary<(ItemPropertyType, int), int>
        {
            [(ItemPropertyType.Attack, 0)] = 5,
        };
        StampLegacyLoot(item, enhancements);
        if (!alreadyRebalanced)
            return;
        foreach (var change in LootStatRevisionCatalog.Changes[GetResRef(item)])
        {
            RemoveMatching(item, change.Type, change.Subtype);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(change.Type, LootSubtype(change),
                change.Current + enhancements.GetValueOrDefault((change.Type, change.Subtype))), item);
        }
        SetLocalInt(item, LootStatRevisionMigration.RevisionVariable, LootStatRevisionMigration.CurrentRevision);
    }
}
