using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DecoyStatusEffect : StatusEffectBase
    {
        public override string Name => "Decoy";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public DecoyStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -25;
        }

    }
}
