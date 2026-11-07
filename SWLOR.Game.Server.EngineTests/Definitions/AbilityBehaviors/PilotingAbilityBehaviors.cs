using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Piloting ability definitions
    /// (SWLOR.Game.Server/Feature/AbilityDefinition/Piloting).
    /// </summary>
    public class PilotingAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string SpaceReason =
            "A space ability acts from the pilot's ship against an enemy ship, which the ability fixtures do not " +
            "provide (no ship, weapon module or hostile ship in space). The numbers are unit tested.";

        [EngineTest("Piloting ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new PilotingAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new[]
            {
                FeatType.AceDisablingShot1, FeatType.AceDisablingShot2, FeatType.AceDisablingShot3,
                FeatType.AceDisablingShot4, FeatType.AceDoubleShot1, FeatType.AceDoubleShot2,
                FeatType.AceDoubleShot3, FeatType.AcePointBlankBurst1, FeatType.AcePointBlankBurst2,
                FeatType.AceStance, FeatType.AceInTheHole,
                FeatType.OrdnanceVolley1, FeatType.OrdnanceVolley2, FeatType.OrdnanceVolley3,
                FeatType.OrdnanceVolley4, FeatType.OrdnanceFlareMissile1, FeatType.OrdnanceFlareMissile2,
                FeatType.OrdnanceFlareMissile3, FeatType.OrdnanceConcussionBarrage1,
                FeatType.OrdnanceConcussionBarrage2, FeatType.OrdnanceShipStance,
                FeatType.OrdnanceSaturationStrike,
                FeatType.BulwarkShieldRam1, FeatType.BulwarkShieldRam2, FeatType.BulwarkShieldRam3,
                FeatType.BulwarkShieldRam4, FeatType.BulwarkDefiance1, FeatType.BulwarkDefiance2,
                FeatType.BulwarkDefiance3, FeatType.BulwarkHail1, FeatType.BulwarkHail2,
                FeatType.BulwarkShipStance, FeatType.BulwarkInvincible,
                FeatType.CommandEmergencyRepair1, FeatType.CommandEmergencyRepair2, FeatType.CommandEmergencyRepair3,
                FeatType.CommandEmergencyRepair4, FeatType.CommandFocusFire1, FeatType.CommandFocusFire2,
                FeatType.CommandFocusFire3, FeatType.CommandBreakFormation1, FeatType.CommandBreakFormation2,
                FeatType.CommandFormation, FeatType.CommandFleetAction
            }
            .Select(feat => new AbilityBehaviorCase { Feat = feat, SkipReason = SpaceReason })
            .ToList();
        }
    }
}
