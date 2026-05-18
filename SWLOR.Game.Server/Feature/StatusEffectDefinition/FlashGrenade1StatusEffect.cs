using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlashGrenade1StatusEffect : StatusEffectBase
    {
        private readonly int _hitChancePenaltyPercent;

        public override string Name => "Flash Grenade I";
        public override EffectIconType Icon => EffectIconType.FlashGrenade1StatusEffect;

        public FlashGrenade1StatusEffect()
            : this(8)
        {
        }

        public FlashGrenade1StatusEffect(int hitChancePenaltyPercent)
        {
            _hitChancePenaltyPercent = Math.Abs(hitChancePenaltyPercent);
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -_hitChancePenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new FlashGrenade1StatusEffect(_hitChancePenaltyPercent);
        }
    }
}
