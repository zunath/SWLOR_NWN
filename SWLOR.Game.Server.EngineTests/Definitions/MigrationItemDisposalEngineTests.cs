using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Retired item migration works when shared temporary storage is full", Category = "MigrationItemDisposal", TimeoutSeconds = 120)]
        public static async Task FullTemporaryItemStorage(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await CreateItemAsync(ctx, owner, "saber_upg1", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
                var fillers = new List<uint>();
                try
                {
                    // Cache construction and migrations share one startup script;
                    // queued destruction cannot free storage until it returns.
                    for (var i = 0; i < 2500; i++)
                    {
                        var filler = CreateItemOnObject("saber_upg1", storage, 1, "disposal_fixture_" + i);
                        if (!GetIsObjectValid(filler)) break;
                        SetLocalInt(filler, "DISPOSAL_FIXTURE", i + 1);
                        fillers.Add(filler);
                    }
                    var original = GetFirstItemInInventory(owner);
                    ctx.Assert(!ItemPlugin.MoveTo(original, storage, true), "Fixture reproduces full shared storage");
                    var migrated = MigrateSerialized(ctx, owner);
                    var replacement = GetFirstItemInInventory(migrated);
                    ctx.AssertEqual("saber_upg2", GetResRef(replacement), "The replacement survives migration");
                    ctx.Assert(!GetIsObjectValid(GetNextItemInInventory(migrated)), "The retired item is absent immediately");
                    var retry = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", ObjectPlugin.Serialize(migrated));
                    ctx.AssertEqual(false, (bool)retry.GetType().GetProperty("Changed").GetValue(retry), "Retry does not duplicate the replacement");
                }
                finally
                {
                    foreach (var filler in fillers) DestroyObject(filler);
                }
            });
        }
    }
}
