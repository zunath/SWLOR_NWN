using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlashGrenade2StatusEffect : StatusEffectBase
    {
        private readonly int _hitChancePenaltyPercent;

        public override string Name => "Flash Grenade II";
        public override EffectIconType Icon => EffectIconType.FlashGrenade2StatusEffect;

        public FlashGrenade2StatusEffect()
            : this(14)
        {
        }

        public FlashGrenade2StatusEffect(int hitChancePenaltyPercent)
        {
            _hitChancePenaltyPercent = Math.Abs(hitChancePenaltyPercent);
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -_hitChancePenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new FlashGrenade2StatusEffect(_hitChancePenaltyPercent);
        }
    }
}
