using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SniperStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Sniper Stance";
        public override EffectIconType Icon => EffectIconType.SniperStanceStatusEffect;
        public SniperStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 20;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = 15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
        }

    }
}
