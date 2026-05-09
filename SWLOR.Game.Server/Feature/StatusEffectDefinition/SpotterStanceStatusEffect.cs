using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SpotterStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Spotter Stance";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public SpotterStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 15;
        }

    }
}
