using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class InfiniteConduitStatusEffect : StatusEffectBase
    {
        public override string Name => "Infinite Conduit";
        public override EffectIconType Icon => EffectIconType.InfiniteConduitStatusEffect;

        public InfiniteConduitStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityStaminaCostFPRestorePercentSkillType] = (int)SkillType.Saberstaff;
            StatGroup.Stats[StatType.AbilityStaminaCostFPRestorePercent] = 50;
            StatGroup.Stats[StatType.AbilityFPCostStaminaRestorePercentSkillType] = (int)SkillType.Force;
            StatGroup.Stats[StatType.AbilityFPCostStaminaRestorePercent] = 50;
            StatGroup.Stats[StatType.TemporaryHighFPAndStaminaAbilityDamageBonusThresholdPercent] = 70;
            StatGroup.Stats[StatType.TemporaryHighFPAndStaminaAbilityDamageBonus] = 20;
        }
    }
}
