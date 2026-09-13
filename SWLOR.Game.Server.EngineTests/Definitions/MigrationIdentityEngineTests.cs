using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Blueprint probing preserves the identity of ordinary stored items", Category = "MigrationIdentity")]
        public static async Task OrdinaryStoredItemIdentity(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "aen_recast1", owner);
            string saved = null;
            string uuid = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // A legacy name guarantees a saved conversion without depending
                // on the deployed blueprint's existing property balance.
                SetName(item, "Armor Enhancement - Recast Reduction I");
                uuid = GetObjectUUID(item);
                saved = ObjectPlugin.Serialize(item);
                DestroyObject(item);
            });
            await ctx.DelaySecondsAsync(0.3f);
            ctx.Assert(!GetIsObjectValid(item), "The original fixture releases its UUID before migration");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migration = typeof(SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration._22_CombatSystemReplacement)
                    .Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration");
                var arguments = new object[] { saved, null, null, true };
                var expanded = (bool)migration.GetMethod("TryMigrateRootBlueprintVariants", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, arguments);
                ctx.Assert(!expanded, "An ordinary item is not a blueprint expansion");
                var result = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", saved);
                ctx.AssertEqual(true, (bool)result.GetType().GetProperty("Changed").GetValue(result), "The fixture exercises serialization");
                var migrated = (string)result.GetType().GetProperty("Data").GetValue(result);
                ctx.Assert(Encoding.UTF8.GetString(Convert.FromBase64String(migrated)).Contains(uuid, StringComparison.Ordinal),
                    "The original persistent item UUID survives probing and migration in the same frame");
            });
        }

        [EngineTest("Saved blueprint metadata distinguishes expandable and current recipes", Category = "MigrationIdentity")]
        public static async Task SavedBlueprintMetadata(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var blueprint = await CreateItemAsync(ctx, owner, "blueprint", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalString(blueprint, "BLUEPRINT_RECIPE_ID", "An unrelated string with the same local name");
                SetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID", 3488);
                var saved = ObjectPlugin.Serialize(blueprint);
                ctx.AssertEqual(3488, (int)Invoke("StoredObjectData", "ReadRootInteger", saved, "BLUEPRINT_RECIPE_ID"),
                    "The saved integer is read without confusing a string local of the same name");
                var migration = typeof(SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration._22_CombatSystemReplacement)
                    .Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration");
                var expand = migration.GetMethod("TryMigrateRootBlueprintVariants", BindingFlags.NonPublic | BindingFlags.Static);
                var arguments = new object[] { saved, null, null, true };
                ctx.AssertEqual(true, (bool)expand.Invoke(null, arguments), "A legacy recipe still expands");
                var variants = (List<string>)arguments[1];
                ctx.AssertEqual(3, variants.Count, "The legacy one-handed boost has three replacements");
                for (var index = 0; index < variants.Count; index++)
                {
                    ctx.AssertEqual(4799 + index, (int)Invoke("StoredObjectData", "ReadRootInteger", variants[index], "BLUEPRINT_RECIPE_ID"),
                        "Each saved variant carries its own current recipe");
                    var retry = new object[] { variants[index], null, null, true };
                    ctx.AssertEqual(false, (bool)expand.Invoke(null, retry), "Current recipes are not expanded again");
                }
            });
        }

        [EngineTest("Stored records with a shared archived item identity retain both copies", Category = "MigrationIdentity")]
        public static async Task SharedArchivedItemIdentity(EngineTestContext ctx)
        {
            var executor = ctx.SpawnCreature("nw_rat001");
            var archive = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, executor, "aen_recast1", archive);
            string storedItem = null;
            string storedCreature = null;
            string uuid = null;
            await ctx.ExecuteInCreatureContextAsync(executor, () =>
            {
                SetName(item, "Armor Enhancement - Recast Reduction I");
                uuid = GetObjectUUID(item);
                storedItem = ObjectPlugin.Serialize(item);
                storedCreature = ObjectPlugin.Serialize(archive);
                DestroyObject(archive);
            });
            await ctx.DelaySecondsAsync(0.3f);
            ctx.Assert(!GetIsObjectValid(item), "The original fixture releases its item identity");
            await ctx.ExecuteInCreatureContextAsync(executor, () =>
            {
                foreach (var saved in new[] { storedItem, storedCreature })
                {
                    var result = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", saved);
                    ctx.AssertEqual(true, (bool)result.GetType().GetProperty("Changed").GetValue(result), "Each record exercises a saved conversion");
                    var migrated = (string)result.GetType().GetProperty("Data").GetValue(result);
                    ctx.Assert(Encoding.UTF8.GetString(Convert.FromBase64String(migrated)).Contains(uuid, StringComparison.Ordinal),
                        "A previous record awaiting destruction must not take the archived item's saved UUID");
                }
            });
        }

        [EngineTest("Migrating an archived item preserves an identity held by a live copy", Category = "MigrationIdentity")]
        public static async Task LiveArchivedItemIdentity(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "aen_recast1", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetName(item, "Armor Enhancement - Recast Reduction I");
                var uuid = GetObjectUUID(item);
                var saved = ObjectPlugin.Serialize(item);
                var result = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", saved);
                ctx.AssertEqual(true, (bool)result.GetType().GetProperty("Changed").GetValue(result), "The archived copy requires a saved conversion");
                var migrated = (string)result.GetType().GetProperty("Data").GetValue(result);
                ctx.Assert(Encoding.UTF8.GetString(Convert.FromBase64String(migrated)).Contains(uuid, StringComparison.Ordinal),
                    "A saved droid item must retain its UUID even while a live item holds that identity");
                ctx.AssertEqual(uuid, GetObjectUUID(item), "The live item's identity is unchanged");
                ctx.AssertEqual(item, GetObjectByUUID(uuid), "The live item retains its native registration");
            });
        }
    }
}
