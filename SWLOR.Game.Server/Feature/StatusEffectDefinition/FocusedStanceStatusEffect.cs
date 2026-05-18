using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FocusedStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Focused Stance";
        public override EffectIconType Icon => EffectIconType.FocusedStanceStatusEffect;
        public FocusedStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 10;
        }

    }
}
