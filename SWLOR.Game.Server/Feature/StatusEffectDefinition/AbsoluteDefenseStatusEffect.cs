using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AbsoluteDefenseStatusEffect : StatusEffectBase
    {
        public override string Name => "Absolute Defense";
        public override EffectIconType Icon => EffectIconType.Invulnerable;
        public AbsoluteDefenseStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 40;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 40;
        }

    }
}
