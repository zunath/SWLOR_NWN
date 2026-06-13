using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DuelistStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Duelist Stance";
        public override EffectIconType Icon => EffectIconType.DuelistStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public DuelistStanceStatusEffect()
        {
            StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment] = 15;
            StatGroup.Stats[StatType.SingleTargetAbilityAttackDeflectionSkillType] = (int)SkillType.TwinBlade;
            StatGroup.Stats[StatType.SingleTargetAbilityAttackDeflection] = 10;
            StatGroup.Stats[StatType.SingleTargetAbilityAttackDeflectionDurationSeconds] = 6;
            StatGroup.Stats[StatType.TwinBladeAreaAbilityDamagePercentAdjustment] = -15;
        }

    }
}
