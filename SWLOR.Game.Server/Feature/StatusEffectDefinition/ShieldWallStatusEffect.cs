using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ShieldWallStatusEffect : StatusEffectBase
    {
        private readonly int _damageReductionPercent;

        public override string Name => "Shield Wall";
        public override EffectIconType Icon => EffectIconType.ShieldWallStatusEffect;

        public ShieldWallStatusEffect() : this(20)
        {
        }

        public ShieldWallStatusEffect(int damageReductionPercent)
        {
            _damageReductionPercent = damageReductionPercent;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -_damageReductionPercent;
        }

        public override IStatusEffect Clone()
        {
            return new ShieldWallStatusEffect(_damageReductionPercent);
        }
    }
}
