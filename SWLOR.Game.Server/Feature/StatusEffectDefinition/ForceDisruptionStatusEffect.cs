using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDisruptionStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Disruption";
        public override EffectIconType Icon => EffectIconType.ForceDisruptionStatusEffect;
        public override StatusEffectCategory Categories =>
            StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.ForceDisruption | StatusEffectCategory.AbilityDisruption;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;

        public ForceDisruptionStatusEffect()
        {
            StatGroup.Stats[StatType.ForceAbilityActivationDisabled] = 1;
        }
    }
}
