using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class UnnervedStatusEffect : StatusEffectBase
    {
        public override string Name => "Unnerved";
        public override EffectIconType Icon => EffectIconType.UnnervedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public UnnervedStatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -30;
        }
    }
}
