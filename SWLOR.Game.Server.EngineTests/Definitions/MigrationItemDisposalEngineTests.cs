using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        /// <summary>Retirements share capacity within a pass and release every rotated container on failure.</summary>
        [EngineTest("Disposal containers are reused, rotated when full, and cleaned after failure", Category = "MigrationItemDisposal", TimeoutSeconds = 120)]
        public static async Task PassScopedDisposalContainers(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var type = typeof(SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration._22_CombatSystemReplacement)
                .Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.MigrationItemDisposal");
            var remove = type.GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);
            var containers = new List<uint>();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                try
                {
                    using var pass = (IDisposable)Activator.CreateInstance(type, true);
                    var first = CreateItemOnObject("saber_upg1", owner);
                    var second = CreateItemOnObject("saberstaff_upg1", owner);
                    remove.Invoke(pass, new object[] { first });
                    var container = GetItemPossessor(first, true);
                    containers.Add(container);
                    remove.Invoke(pass, new object[] { second });
                    ctx.AssertEqual(container, GetItemPossessor(second, true), "Consecutive retirements reuse the current container");
                    for (var i = 0; i < 2500; i++)
                    {
                        var filler = CreateItemOnObject("saber_upg1", container, 1, "pass_fixture_" + i);
                        if (!GetIsObjectValid(filler)) break;
                        SetLocalInt(filler, "DISPOSAL_FIXTURE", i + 1);
                    }
                    var third = CreateItemOnObject("saber_upg1", owner);
                    ctx.Assert(GetIsObjectValid(third), "The retirement fixture can be created");
                    ctx.Assert(!ItemPlugin.MoveTo(third, container, true), "The first disposal container is full");
                    remove.Invoke(pass, new object[] { third });
                    var rotated = GetItemPossessor(third, true);
                    containers.Add(rotated);
                    ctx.Assert(GetIsObjectValid(rotated) && rotated != container, "Full capacity rotates to a fresh container");
                    throw new InvalidOperationException("Simulated failure after retirements");
                }
                catch (InvalidOperationException exception) when (exception.Message == "Simulated failure after retirements")
                {
                    // The using scope must have queued cleanup for both containers.
                }

                using var nextPass = (IDisposable)Activator.CreateInstance(type, true);
                var next = CreateItemOnObject("saber_upg1", owner);
                remove.Invoke(nextPass, new object[] { next });
                var nextContainer = GetItemPossessor(next, true);
                ctx.Assert(!containers.Contains(nextContainer), "A separate pass cannot inherit old capacity or retired items");
                containers.Add(nextContainer);
            });
            await ctx.DelaySecondsAsync(0.3f);
            foreach (var container in containers)
                ctx.Assert(!GetIsObjectValid(container), "All pass-owned containers are destroyed after the script returns");
        }

        /// <summary>
        /// Verifies that retired item migration works when shared temporary storage is full.
        /// </summary>
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
