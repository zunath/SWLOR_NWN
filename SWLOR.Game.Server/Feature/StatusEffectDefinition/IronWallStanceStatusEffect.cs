using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronWallStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Iron Wall Stance";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public IronWallStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -25;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 25;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 25;
        }

    }
}
