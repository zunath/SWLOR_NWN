using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
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
                var description = RevampModuleCatalog.Steps.Single(step => step.Tag == "sm_laser_00").Description;
                var item = CreateItemOnObject("sm_laser_00", owner);
                try
                {
                    ctx.AssertEqual("Electronic Components 1", GetDescription(item, true), "the blueprint has no description of its own");
                    Space.ApplyExamineDetails(item);
                    ctx.AssertEqual(description, GetDescription(item), "examine shows only the module description");
                    Space.ApplyExamineDetails(item);
                    ctx.AssertEqual(description, GetDescription(item), "repeated examines do not stack the text");
                }
                finally
                {
                    DestroyObject(item);
                }
            });
        }
    }
}
