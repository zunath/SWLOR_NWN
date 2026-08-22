using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImmovableStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Immovable Stance";
        public override EffectIconType Icon => EffectIconType.ImmovableStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public ImmovableStanceStatusEffect()
        {
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 30;
            StatGroup.Stats[StatType.MobilityResistance] = 8;
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -25;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = -25;
        }
    }
}
