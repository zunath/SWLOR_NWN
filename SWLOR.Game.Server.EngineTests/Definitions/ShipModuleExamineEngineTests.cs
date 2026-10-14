using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShipModuleExamineEngineTests
    {
        [EngineTest("Ship module examine text drops the base item placeholder", Category = "ShipModuleExamine", TimeoutSeconds = 30f)]
        public static async Task ShipModuleDescriptionReplacesPlaceholder(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var item = CreateItemOnObject("com_laser_1", owner);
                try
                {
                    ctx.AssertEqual("Electronic Components 1", GetDescription(item, true), "the blueprint has no description of its own");
                    Space.ApplyExamineDetails(item);
                    ctx.AssertEqual("Deals 18 thermal DMG to your target.", GetDescription(item), "examine shows only the module description");
                    Space.ApplyExamineDetails(item);
                    ctx.AssertEqual("Deals 18 thermal DMG to your target.", GetDescription(item), "repeated examines do not stack the text");
                }
                finally
                {
                    DestroyObject(item);
                }
            });
        }
    }
}
