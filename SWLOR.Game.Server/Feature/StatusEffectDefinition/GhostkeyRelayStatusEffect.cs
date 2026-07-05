using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GhostkeyRelayStatusEffect : StatusEffectBase
    {
        public override string Name => "Ghostkey Relay";
        public override EffectIconType Icon => EffectIconType.TacticalUplinkStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public GhostkeyRelayStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 6;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Devices;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 4;
        }
    }
}
