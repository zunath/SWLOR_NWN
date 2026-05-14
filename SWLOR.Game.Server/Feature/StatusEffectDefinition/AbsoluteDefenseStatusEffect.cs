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
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -25;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -25;
            StatGroup.Stats[StatType.MindResistance] = 100;
            StatGroup.Stats[StatType.MobilityResistance] = 100;
        }

    }
}
