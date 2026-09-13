using System;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        /// <summary>Malformed droid state must not leave a failed temporary load registered in the native world.</summary>
        [EngineTest("Serialized migration failures release temporary objects and identities", Category = "MigrationCleanup")]
        public static async Task FailedSerializedMigrationCleanup(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            foreach (var helper in new[] { "PistolBaseItemMigration", "LegacySaberMigration" })
            {
                var item = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
                string saved = null;
                string uuid = null;
                var tag = "cleanup_" + Guid.NewGuid().ToString("N");
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    SetTag(item, tag);
                    SetLocalString(item, "CONSTRUCTED_DROID", "{malformed");
                    uuid = GetObjectUUID(item);
                    saved = ObjectPlugin.Serialize(item);
                    DestroyObject(item);
                });
                await ctx.DelaySecondsAsync(0.3f);
                ctx.Assert(!GetIsObjectValid(item), "The fixture releases its original identity");
                uint temporary = OBJECT_INVALID;
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    try
                    {
                        if (helper == "PistolBaseItemMigration")
                            Invoke(helper, "TryNormalizeSerializedItem", saved, null, 0, SWLOR.NWN.API.NWScript.Enum.Item.BaseItem.Invalid);
                        else
                            Invoke(helper, "TryNormalizeSerializedSaber", saved, null);
                        ctx.Assert(false, "Malformed droid data must fail normalization");
                    }
                    catch (TargetInvocationException exception)
                    {
                        ctx.Assert(exception.InnerException is Newtonsoft.Json.JsonException,
                            "Failure comes from the malformed fixture, not reflection or object loading");
                    }
                    temporary = GetObjectByTag(tag);
                    ctx.Assert(GetIsObjectValid(temporary), "Native destruction is deferred until this script returns");
                    ctx.Assert(!GetIsObjectValid(GetObjectByUUID(uuid)), "Failure releases the saved UUID immediately");
                });
                await ctx.DelaySecondsAsync(0.3f);
                ctx.Assert(!GetIsObjectValid(temporary), "The failed temporary object is destroyed after the script returns");
            }
        }
    }
}
