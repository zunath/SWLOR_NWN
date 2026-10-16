using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Stored item migration updates artwork and loot stats inside one saved bag and keeps it stable on retry", Category = LootCategory, TimeoutSeconds = 60f)]
    public static async Task StoredArtworkAndLootStatsInOnePass(EngineTestContext ctx)
    {
        const string enhancementResref = "aen_hp1";
        const int legacyModel = 88;
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var enhancement = await CreateItemAsync(ctx, owner, enhancementResref, bag);
        var gloves = await CreateItemAsync(ctx, owner, RevisedGloves, bag);
        var identities = new Dictionary<string, string>();
        var record = new InventoryItem { PlayerId = "stored-update-owner", StorageId = "stored-update-bank", Resref = "bag_sm", Name = "Keep bank name", Quantity = 1 };
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ItemPlugin.SetItemAppearance(enhancement, ItemAppearanceType.SimpleModel, 0, legacyModel);
            SetLocalInt(enhancement, "CRAFTED_QUALITY", 29);
            StampLegacyLoot(gloves, GloveEnhancements);
            identities["bag_sm"] = GetObjectUUID(bag);
            identities[enhancementResref] = GetObjectUUID(enhancement);
            identities[RevisedGloves] = GetObjectUUID(gloves);
            record.Data = ObjectPlugin.Serialize(bag);
            DestroyObject(bag);
        });
        await ctx.DelaySecondsAsync(0.3f);

        string firstRun = null;
        try
        {
            for (var run = 1; run <= 2; run++)
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    if (run == 1)
                        DB.Set(record);
                    new _27_UpdateStoredItems().Migrate();
                    var stored = ReadPersisted<InventoryItem>(record.Id);
                    if (run == 1)
                    {
                        ctx.Assert(stored.Data != record.Data, "Run 1: the saved bag is rewritten");
                        firstRun = stored.Data;
                    }
                    else
                        ctx.AssertEqual(firstRun, stored.Data, "Run 2: a retry leaves the saved bag byte-for-byte intact");
                    ctx.AssertEqual("stored-update-owner", stored.PlayerId, $"Run {run}: bank ownership survives");
                    ctx.AssertEqual("stored-update-bank", stored.StorageId, $"Run {run}: bank storage survives");
                    ctx.AssertEqual(1, stored.Quantity, $"Run {run}: bank quantity survives");
                    ctx.AssertEqual("Keep bank name", stored.Name, $"Run {run}: bank name survives");

                    var restored = Deserialize(ctx, stored.Data);
                    ctx.AssertEqual(identities["bag_sm"], GetObjectUUID(restored), $"Run {run}: the bag keeps its identity");
                    var contents = new List<uint>();
                    for (var item = GetFirstItemInInventory(restored); GetIsObjectValid(item); item = GetNextItemInInventory(restored))
                        contents.Add(item);
                    ctx.AssertEqual(2, contents.Count, $"Run {run}: the bag still holds both items");
                    var restoredEnhancement = contents.Single(item => GetResRef(item) == enhancementResref);
                    var restoredGloves = contents.Single(item => GetResRef(item) == RevisedGloves);
                    var updatedModel = ItemIconMigration.GetUpdatedModel(enhancementResref, (int)GetBaseItemType(restoredEnhancement), legacyModel);
                    ctx.Assert(updatedModel != legacyModel, "The fixture uses a legacy appearance");
                    ctx.AssertEqual(updatedModel, GetItemAppearance(restoredEnhancement, ItemAppearanceType.SimpleModel, 0), $"Run {run}: nested artwork is updated");
                    ctx.AssertEqual(29, GetLocalInt(restoredEnhancement, "CRAFTED_QUALITY"), $"Run {run}: crafted metadata survives");
                    AssertRevisedLoot(ctx, restoredGloves, GloveEnhancements);
                    ctx.AssertEqual(identities[enhancementResref], GetObjectUUID(restoredEnhancement), $"Run {run}: the enhancement keeps its identity");
                    ctx.AssertEqual(identities[RevisedGloves], GetObjectUUID(restoredGloves), $"Run {run}: the gloves keep their identity");
                    DestroyObject(restored);
                });
                await ctx.DelaySecondsAsync(0.3f);
            }
        }
        finally
        {
            DB.Delete<InventoryItem>(record.Id);
        }
        ctx.SetResultDetail("One saved bag with legacy artwork and legacy loot stats; both updated in one pass, Redis reload, identities, ownership and quantity checked across two runs.");
    }
}
