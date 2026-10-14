using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
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
                HasValidEquipment = EquipmentPredicates.HasUnarmedGrip,
                EquipmentError = "Equip a Power Glove and leave your off hand available.",
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
