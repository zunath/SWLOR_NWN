using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TwinGuardStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Twin Guard Stance";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public TwinGuardStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
        }

    }
}
