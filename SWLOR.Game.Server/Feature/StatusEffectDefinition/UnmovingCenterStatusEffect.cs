using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class UnmovingCenterStatusEffect : StatusEffectBase
    {
        public override string Name => "Unmoving Center";
        public override EffectIconType Icon => EffectIconType.UnmovingCenterStatusEffect;
        public UnmovingCenterStatusEffect()
        {
            StatGroup.Stats[StatType.MeleeDeflection] = 20;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 30;
        }

    }
}
