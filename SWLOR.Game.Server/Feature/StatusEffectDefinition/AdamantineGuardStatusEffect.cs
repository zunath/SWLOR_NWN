using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdamantineGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Adamantine Guard";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public AdamantineGuardStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 40;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 40;
        }

    }
}
