using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DefensiveStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Defensive Stance";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public DefensiveStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
        }

    }
}
