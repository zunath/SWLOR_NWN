using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImprovedAttentivenessStatusEffect : StatusEffectBase
    {
        public override string Name => "Improved Attentiveness";
        public override EffectIconType Icon => EffectIconType.ImprovedAttentivenessStatusEffect;
        public ImprovedAttentivenessStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 15;
        }

    }
}
