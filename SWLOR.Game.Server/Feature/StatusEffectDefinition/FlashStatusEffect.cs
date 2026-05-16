using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlashStatusEffect : StatusEffectBase
    {
        private readonly int _hitChancePenalty;
        public override string Name => "Flash";
        public override EffectIconType Icon => EffectIconType.Blindness;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public FlashStatusEffect()
            : this(8)
        {
        }

        public FlashStatusEffect(int hitChancePenalty)
        {
            _hitChancePenalty = Math.Abs(hitChancePenalty);
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -_hitChancePenalty;
        }

        public override IStatusEffect Clone()
        {
            return new FlashStatusEffect(_hitChancePenalty);
        }
    }
}
