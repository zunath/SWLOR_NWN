using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceCapacitorStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Capacitor";
        public override EffectIconType Icon => EffectIconType.ForceCapacitorStatusEffect;

        public ForceCapacitorStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityStaminaCostFPRestorePercentSkillType] = (int)SkillType.Saberstaff;
            StatGroup.Stats[StatType.AbilityStaminaCostFPRestorePercent] = 25;
            StatGroup.Stats[StatType.AbilityFPCostStaminaRestorePercentSkillType] = (int)SkillType.Force;
            StatGroup.Stats[StatType.AbilityFPCostStaminaRestorePercent] = 25;
        }
    }
}
