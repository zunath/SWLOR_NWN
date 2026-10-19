using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class OverflowStorageEngineTests
    {
        /// <summary>
        /// /overflow marks each claimed item with its record until the record is deleted; the character file keeps the mark
        /// only while the claimed stack stays its own item. A matching stack in the inventory must not absorb it.
        /// </summary>
        [EngineTest("A claimed overflow stack stays separate from a matching stack and keeps its claim mark", Category = "OverflowStorage")]
        public static async Task ClaimedStackKeepsItsMark(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var owner = fixture.Creature;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var existing = CreateItemOnObject("herb_m", owner, 10);
                ctx.Assert(GetIsObjectValid(existing), "The fixture holds a stack");
                var source = CreateItemOnObject("herb_m", GetObjectByTag("TEMP_ITEM_STORAGE"), 5);
                var data = ObjectPlugin.Serialize(source);
                DestroyObject(source);

                var claimed = ObjectPlugin.Deserialize(data);
                SetLocalString(claimed, "OVERFLOW_ITEM_ID", "engine-test-record");
                ctx.Assert(ObjectPlugin.AcquireItem(owner, claimed), "The claimed stack is acquired");
                ctx.Assert(GetIsObjectValid(claimed) && GetItemPossessor(claimed) == owner, "The claimed stack stays its own item");
                ctx.AssertEqual(10, GetItemStackSize(existing), "The matching stack does not absorb the claimed one");
                ctx.AssertEqual("engine-test-record", GetLocalString(claimed, "OVERFLOW_ITEM_ID"), "The claim mark stays on the claimed stack");
            });
        }
    }
}
