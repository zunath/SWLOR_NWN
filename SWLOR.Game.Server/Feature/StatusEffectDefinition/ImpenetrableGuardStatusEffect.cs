using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImpenetrableGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Impenetrable Guard";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public ImpenetrableGuardStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.AttackDeflection] = 15;
        }

    }
}
