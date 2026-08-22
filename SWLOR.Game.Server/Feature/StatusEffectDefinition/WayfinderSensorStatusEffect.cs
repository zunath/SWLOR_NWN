using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WayfinderSensorStatusEffect : StatusEffectBase
    {
        public override string Name => "Wayfinder Sensor";
        public override EffectIconType Icon => EffectIconType.WayfinderSensorStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public WayfinderSensorStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 5;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 5;
            StatGroup.Stats[StatType.RangedEvasionPercentAdjustment] = 5;
        }
    }
}
