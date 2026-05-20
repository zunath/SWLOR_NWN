using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ConduitStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Conduit Stance";
        public override EffectIconType Icon => EffectIconType.ConduitStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public ConduitStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
        }

    }
}
