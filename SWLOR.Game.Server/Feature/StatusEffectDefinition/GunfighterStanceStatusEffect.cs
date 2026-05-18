using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GunfighterStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Gunfighter Stance";
        public override EffectIconType Icon => EffectIconType.GunfighterStanceStatusEffect;
        public GunfighterStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 10;
        }

    }
}
