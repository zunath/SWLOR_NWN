using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
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
                // ProvokeAbilityDefinition - hostile single-target/area enmity modifier. Enmity is
                // not observable via these case fields and neither tier declares a status effect,
                // damage, or an FP/Stamina cost - the recast is the only observable outcome.
                new()
                {
                    Feat = FeatType.Provoke1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsRecast = true,
                    Notes = "Impact only calls Enmity.ModifyEnmity and plays a visual effect; enmity is not observable via these case fields, and no FP/Stamina cost is declared.",
                },
                new()
                {
                    Feat = FeatType.Provoke2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsRecast = true,
                    Notes = "Area variant of Provoke1 with the same enmity-only impact on each hostile in range.",
                },
            };
        }
    }
}
