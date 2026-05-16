using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DuelistStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Duelist Stance";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public DuelistStanceStatusEffect()
        {
            StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment] = 15;
            StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityAttackDeflection] = 10;
            StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityAttackDeflectionDurationSeconds] = 6;
            StatGroup.Stats[StatType.TwinBladeAreaAbilityDamagePercentAdjustment] = -15;
        }

    }
}
