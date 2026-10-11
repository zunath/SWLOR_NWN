using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class ArmorAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Armor ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new ArmorAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // Threat-only impacts must produce observable enmity.
                new()
                {
                    Feat = FeatType.Provoke1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsRecast = true,
                    MinimumTargetEnmityAfterImpact = 400,
                    Notes = "Verifies generated threat; deficit recovery and actual attacks are covered by TankEnmityEngineTests.",
                },
                new()
                {
                    Feat = FeatType.Provoke2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsRecast = true,
                    MinimumTargetEnmityAfterImpact = 400,
                    Notes = "Area variant of Provoke1 with the same enmity-only impact on each hostile in range.",
                },
            };
        }
    }
}
