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
                // ProvokeAbilityDefinition - hostile single-target/area enmity modifier. Enmity is
                // not observable via these case fields and neither tier declares a status effect,
                // damage, or an FP/Stamina cost - the recast is the only observable outcome.
                new()
                {
                    Feat = FeatType.Provoke1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsRecast = true,
                    OutcomeAssertionWaiverReason = "The impact only changes the private enmity table and plays a VFX; the harness has no read-only enmity observation seam.",
                    Notes = "Impact only calls Enmity.ModifyEnmity and plays a visual effect; enmity is not observable via these case fields, and no FP/Stamina cost is declared.",
                },
                new()
                {
                    Feat = FeatType.Provoke2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsRecast = true,
                    OutcomeAssertionWaiverReason = "The area impact only changes private enmity entries; the harness has no read-only enmity observation seam.",
                    Notes = "Area variant of Provoke1 with the same enmity-only impact on each hostile in range.",
                },
            };
        }
    }
}
