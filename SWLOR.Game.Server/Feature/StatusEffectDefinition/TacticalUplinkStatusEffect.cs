using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TacticalUplinkStatusEffect : StatusEffectBase
    {
        public override string Name => "Tactical Uplink";
        public override EffectIconType Icon => EffectIconType.TacticalUplinkStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public TacticalUplinkStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Devices;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 5;
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustmentSkillType] = (int)SkillType.Devices;
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment] = 5;
        }
    }
}
