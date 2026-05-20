using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BombardierStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Bombardier Stance";
        public override EffectIconType Icon => EffectIconType.BombardierStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public BombardierStanceStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.ThrowingAreaAbilityDamagePercentAdjustment] = 15;
        }

    }
}
