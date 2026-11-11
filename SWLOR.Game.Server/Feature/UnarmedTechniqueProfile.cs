using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature
{
    public static class UnarmedTechniqueProfile
    {
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void Register()
        {
            MeleeTechnique.RegisterProfile(SkillType.Unarmed, new MeleeTechniqueProfile
            {
                SetupMarkerFactory = stage => stage == MeleeTechniqueStage.Finish
                    ? new FinishStatusEffect() : new OpeningStatusEffect(),
                OpeningFinisherWindowFactory = (evasion, movement) => new SlipAwayStatusEffect(evasion, movement),
                FinishFinisherWindowFactory = haste => new PressForwardStatusEffect(haste),
                StaminaDiscountMarkerFactory = () => new PaybackStatusEffect(),
                RetaliationMarkerFactory = () => new RetaliationStatusEffect()
            });
        }
    }
}
