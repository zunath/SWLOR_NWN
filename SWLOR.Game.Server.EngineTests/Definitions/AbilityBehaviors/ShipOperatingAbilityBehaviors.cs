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
        public static Task Run(EngineTestContext ctx)=>AbilityBehaviorExecutor.RunAsync(ctx,new ShipOperatingAbilityBehaviors().BuildCases());
    }
}
