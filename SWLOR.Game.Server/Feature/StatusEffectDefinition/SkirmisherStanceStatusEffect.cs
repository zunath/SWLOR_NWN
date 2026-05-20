using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SkirmisherStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Skirmisher Stance";
        public override EffectIconType Icon => EffectIconType.SkirmisherStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public SkirmisherStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -10;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 15;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = -20;
        }

    }
}
