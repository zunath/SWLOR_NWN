using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CrusherStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Crusher Stance";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public CrusherStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
        }

    }
}
