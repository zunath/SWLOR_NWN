using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public sealed class ShipOperatingAbilityBehaviors : IAbilityBehaviorSource
    {
        public List<AbilityBehaviorCase> BuildCases()=>ShipTechniqueCatalog.Default.Profiles.Where(x=>x.Kind!=ShipPerkKind.Trait)
            .Select(x=>new AbilityBehaviorCase{Feat=x.Feat,RequiresShipOperatingFixture=true,ExpectsRecast=true}).ToList();
        [EngineTest("Ship operating technique behaviors",Category="SpaceOperating",TimeoutSeconds=360f)]
        public static async Task Run(EngineTestContext ctx)
        {
            // Keep native player identities stable across this ship-only sweep. Every case
            // replaces its fitting, perks, skills, preparation and personal recasts.
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);
            using var ally=await PlayerAbilityFixture.CreateAsync(ctx,2f);
            await AbilityBehaviorExecutor.RunAsync(ctx,new ShipOperatingAbilityBehaviors().BuildCases(),
                behavior=>ShipTechniqueEngineTests.RunCaseAsync(ctx,behavior.Feat,actor,ally));
        }
    }
}
