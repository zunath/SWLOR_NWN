using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDisruptionStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Disruption";
        public override EffectIconType Icon => EffectIconType.ForceDisruptionStatusEffect;
        public override StatusEffectCategory Categories =>
            StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.ForceDisruption;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;

        public ForceDisruptionStatusEffect()
        {
            StatGroup.Stats[StatType.ForceAbilityActivationDisabled] = 1;
        }
    }
}
