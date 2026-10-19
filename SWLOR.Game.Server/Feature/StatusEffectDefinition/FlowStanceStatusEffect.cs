using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlowStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Flow Stance";
        public override EffectIconType Icon => EffectIconType.FlowStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;

        public FlowStanceStatusEffect()
        {
            StatGroup.Stats[StatType.TechniqueLine] = 1;
            StatGroup.Stats[StatType.DefensePercentAdjustment] = -10;
            StatGroup.Stats[StatType.TechniqueOpeningFinisherEvasionPercent] = 20;
            StatGroup.Stats[StatType.TechniqueOpeningFinisherMovementSpeedPercent] = 15;
            StatGroup.Stats[StatType.TechniqueFinishFinisherHastePercent] = 15;
            StatGroup.Stats[StatType.TechniqueFinisherWindowDurationSeconds] = 8;
            StatGroup.Stats[StatType.TechniqueFinisherWindowCooldownSeconds] = 12;
        }
    }
}
